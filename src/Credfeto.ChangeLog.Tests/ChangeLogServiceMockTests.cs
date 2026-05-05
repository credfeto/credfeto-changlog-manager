using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0045:Use async overload", Justification = "ValueTask mock returns are synchronous")]
[SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2012:UseValueTasksCorrectly", Justification = "NSubstitute mock setup and verification necessarily handles ValueTask instances outside normal await patterns")]
public sealed class ChangeLogServiceMockTests : TestBase
{
    private const string SIMPLE_CHANGE_LOG =
        """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        - Added item
        ### Fixed
        ### Changed
        ### Removed
        ### Deployment Changes

        ## [0.0.0] - Project created
        """;

    // ─── ChangeLogReader ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReaderExtractReleaseNotesCallsLoadTextAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(SIMPLE_CHANGE_LOG));

        ChangeLogReader reader = new(storage);

        string result = await reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: "CHANGELOG.md",
            version: string.Empty,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.Contains("### Added", result, System.StringComparison.Ordinal);
        Assert.Contains("- Added item", result, System.StringComparison.Ordinal);

        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReaderFindFirstReleaseVersionPositionCallsLoadLinesAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IReadOnlyList<string> lines =
        [
            "# Changelog",
            string.Empty,
            "## [Unreleased]",
            "### Added",
            string.Empty,
            "## [1.0.0] - 2024-01-01",
            "### Added",
            "- First release"
        ];

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadLinesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(lines));

        ChangeLogReader reader = new(storage);

        int? result = await reader.FindFirstReleaseVersionPositionAsync(
            changeLogFileName: "CHANGELOG.md",
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.NotNull(result);
        // Line index 5 is "## [1.0.0]..." so position returned is 5+1 = 6
        Assert.Equal(expected: 6, actual: result.Value);

        await storage.Received(1).LoadLinesAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogFixer ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FixerFixFileAsyncCallsSaveTextAsync()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string contentWithBlankLines =
            """
            ## [Unreleased]
            ### Added

            - Added item
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes
            """;

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(contentWithBlankLines));
        storage.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.CompletedTask);

        ChangeLogFixer fixer = new(storage);

        await fixer.FixFileAsync(
            changeLogFileName: "CHANGELOG.md",
            additionalSections: null,
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
        await storage.Received(1).SaveTextAsync(
            "CHANGELOG.md",
            Arg.Is<string>(s => !s.Contains("### Added\r\n\r\n", System.StringComparison.Ordinal)
                                && !s.Contains("### Added\n\n", System.StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()
        );
    }

    // ─── ChangeLogLinter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LinterLintFileAsyncWithValidChangeLogReturnsNoErrors()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(SIMPLE_CHANGE_LOG));

        ChangeLogLinter linter = new(storage);

        IReadOnlyList<LintError> errors = await linter.LintFileAsync(
            changeLogFileName: "CHANGELOG.md",
            additionalSections: null,
            cancellationToken: cancellationTokenSource.Token
        );

        Assert.Empty(errors);
        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinterLintFileAsyncWithMissingUnreleasedSectionReturnsError()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string invalidContent =
            """
            # Changelog
            All notable changes to this project will be documented in this file.
            """;

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(invalidContent));

        ChangeLogLinter linter = new(storage);

        IReadOnlyList<LintError> errors = await linter.LintFileAsync(
            changeLogFileName: "CHANGELOG.md",
            additionalSections: null,
            cancellationToken: cancellationTokenSource.Token
        );

        LintError error = Assert.Single(errors);
        Assert.Equal(expected: "Missing [Unreleased] section", actual: error.Message);
        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogUpdater ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdaterAddEntryAsyncWhenFileExistsCallsSaveTextAsyncOnce()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.Exists(Arg.Any<string>()).Returns(true);
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(SIMPLE_CHANGE_LOG));
        storage.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        await updater.AddEntryAsync(
            changeLogFileName: "CHANGELOG.md",
            type: "Added",
            message: "New feature added",
            cancellationToken: cancellationTokenSource.Token
        );

        storage.Received(1).Exists("CHANGELOG.md");
        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
        await storage.Received(1).SaveTextAsync(
            "CHANGELOG.md",
            Arg.Is<string>(s => s.Contains("- New feature added", System.StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdaterAddEntryAsyncWhenFileDoesNotExistCallsSaveTextAsyncTwice()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.Exists(Arg.Any<string>()).Returns(false);
        storage.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        await updater.AddEntryAsync(
            changeLogFileName: "CHANGELOG.md",
            type: "Added",
            message: "Created from missing file",
            cancellationToken: cancellationTokenSource.Token
        );

        storage.Received(1).Exists("CHANGELOG.md");
        // First call: CreateEmptyAsync saves TemplateFile.Initial
        // Second call: AddEntryAsync saves the updated content
        await storage.Received(2).SaveTextAsync("CHANGELOG.md", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdaterEnsureUnreleasedSectionsAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.Exists(Arg.Any<string>()).Returns(true);
        storage.LoadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.FromResult(SIMPLE_CHANGE_LOG));
        storage.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        await updater.EnsureUnreleasedSectionsAsync(
            changeLogFileName: "CHANGELOG.md",
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadTextAsync("CHANGELOG.md", Arg.Any<CancellationToken>());
        await storage.Received(1).SaveTextAsync("CHANGELOG.md", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
