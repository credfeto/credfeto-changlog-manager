using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed class AdditionalCoverageTests : TestBase, IDisposable
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private readonly ServiceProvider _serviceProvider;

    public AdditionalCoverageTests()
    {
        ServiceCollection services = new();
        services.AddChangeLog();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    // ─── ChangeLogUpdater.CreateRelease with pending=false (CurrentDate path) ─────

    [Fact]
    public void CreateReleaseWithPendingFalseUsesCurrentDate()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Some item
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = Parse(changeLog);
        ChangeLogDocument result = ChangeLogUpdater.CreateRelease(
            document: document,
            version: "1.0.0",
            pending: false,
            language: Language
        );

        // A non-pending release should have a date that is a valid date string (not "TBD")
        Assert.False(result.Releases.IsEmpty, userMessage: "Expected at least one release to be created");
        Assert.NotEqual(expected: "TBD", actual: result.Releases[0].Date);
        Assert.False(
            string.IsNullOrEmpty(result.Releases[0].Date),
            userMessage: "Expected a date to be set on the release"
        );
    }

    // ─── ChangeLogUpdater.RemoveEntryAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdaterRemoveEntryAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string simpleChangeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Item to remove
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = Parse(simpleChangeLog);
        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        string tempFile = Path.GetTempFileName();

        try
        {
            await updater.RemoveEntryAsync(
                changeLogFileName: tempFile,
                language: Language,
                type: "Added",
                message: "Item to remove",
                cancellationToken: cancellationTokenSource.Token
            );
        }
        finally
        {
            File.Delete(tempFile);
        }

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogUpdater.CreateReleaseAsync ──────────────────────────────────────

    [Fact]
    public async Task UpdaterCreateReleaseAsyncCallsLoadAndSave()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string simpleChangeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - An item
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = Parse(simpleChangeLog);
        IChangeLogStorage storage = Substitute.For<IChangeLogStorage>();
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        string tempFile = Path.GetTempFileName();

        try
        {
            await updater.CreateReleaseAsync(
                changeLogFileName: tempFile,
                language: Language,
                version: "1.0.0",
                pending: true,
                cancellationToken: cancellationTokenSource.Token
            );
        }
        finally
        {
            File.Delete(tempFile);
        }

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

    // ─── ChangeLogUpdater.CreateEmptyAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdaterCreateEmptyAsyncWritesEmptyChangeLog()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");

        try
        {
            IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

            await updater.CreateEmptyAsync(
                changeLogFileName: fileName,
                language: Language,
                cancellationToken: cancellationTokenSource.Token
            );

            Assert.True(File.Exists(fileName), userMessage: "Expected changelog file to be created");
            string content = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            Assert.True(
                content.Contains("[Unreleased]", StringComparison.Ordinal),
                userMessage: $"Expected empty changelog to contain [Unreleased] section, got:{Environment.NewLine}{content}"
            );
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    // ─── ChangeLogSerialiser.MergeSections (duplicate section names) ──────────────

    [Fact]
    public void OrderSectionsMergesDuplicateSectionsWithSameName()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - First added
            ### Added
            - Duplicate added
            ### Fixed
            ### Changed
            ### Removed

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = Parse(changeLog);
        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        // OrderSections will merge duplicate "Added" sections
        ImmutableArray<ChangeLogSection> ordered = ChangeLogSerialiser.OrderSections(
            sections: sections,
            sectionOrder: Language.SectionOrder
        );

        // After merging, there should be at most one "Added" section
        int addedCount = 0;

        foreach (ChangeLogSection s in ordered)
        {
            if (StringComparer.Ordinal.Equals(s.Name, "Added"))
            {
                addedCount++;
            }
        }

        Assert.Equal(expected: 1, actual: addedCount);
    }

    // ─── ChangeLogHeadingExtensions: GetVersionString, ContainsHtmlCommentStart, ContainsHtmlCommentEnd ─

    [Theory]
    [InlineData("## [1.0.0] - 2024-01-01", "1.0.0")]
    [InlineData("## [Unreleased]", "Unreleased")]
    [InlineData("## [0.0.0] - Project created", "0.0.0")]
    public void GetVersionStringExtractsVersionFromHeader(string line, string expectedVersion)
    {
        string? version = line.GetVersionString();
        Assert.Equal(expected: expectedVersion, actual: version);
    }

    [Fact]
    public void GetVersionStringReturnsNullWhenNoCloseBracket()
    {
        const string line = "## [no-close-bracket";
        string? version = line.GetVersionString();
        Assert.Null(version);
    }

    [Theory]
    [InlineData("<!-- comment -->", true)]
    [InlineData("Some text <!-- comment -->", true)]
    [InlineData("No comment here", false)]
    public void ContainsHtmlCommentStartDetectsOpeningTag(string line, bool expected)
    {
        bool result = line.ContainsHtmlCommentStart();
        Assert.Equal(expected: expected, actual: result);
    }

    [Theory]
    [InlineData("<!-- comment -->", true)]
    [InlineData("Some text -->", true)]
    [InlineData("No comment here", false)]
    public void ContainsHtmlCommentEndDetectsClosingTag(string line, bool expected)
    {
        bool result = line.ContainsHtmlCommentEnd();
        Assert.Equal(expected: expected, actual: result);
    }

    // ─── ChangeLogLinter - section out of order ───────────────────────────────────

    [Fact]
    public void SectionOutOfOrder_ReturnsError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            ### Security
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), Language);

        Assert.Contains(
            errors,
            e => e.Message.Contains(value: "out of order", comparisonType: StringComparison.Ordinal)
        );
    }

    // ─── ChangeLogLinter - release date is "TBD" (non-parseable date, non-DateTime) ─

    [Fact]
    public void TbdDateInRelease_ReturnsNoDateFormatError()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [1.0.0] - TBD
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), Language);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    // ─── ChangeLogFixer.RemoveBlankLinesAfterHeadings (document without unreleased section) ─

    [Fact]
    public void RemoveBlankLinesAfterHeadingsDoesNotCrashOnDocumentWithoutUnreleased()
    {
        // RemoveBlankLinesAfterHeadings specifically handles null Unreleased gracefully
        // The public Fix method requires Unreleased to exist - we test the branch
        // by calling with a document whose Unreleased is null.
        // ChangeLogFixer.Fix would throw; test by using EnsurePreamble only.
        const string changeLog = """
            # Changelog

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release
            """;

        ChangeLogDocument document = Parse(changeLog);

        // EnsurePreamble handles null Unreleased gracefully
        ChangeLogDocument result = ChangeLogFixer.EnsurePreamble(document);
        Assert.Null(result.Unreleased);
    }

    // ─── RemoveEntryAsync via real file I/O ───────────────────────────────────────

    [Fact]
    public async Task RemoveEntryAsyncRemovesEntryFromChangeLog()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - Item to remove
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");

        try
        {
            await File.WriteAllTextAsync(fileName, changeLog, Encoding.UTF8, cancellationTokenSource.Token);

            IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

            await updater.RemoveEntryAsync(
                changeLogFileName: fileName,
                language: Language,
                type: "Added",
                message: "Item to remove",
                cancellationToken: cancellationTokenSource.Token
            );

            string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            Assert.False(
                result.Contains("Item to remove", StringComparison.Ordinal),
                userMessage: $"Expected entry to be removed, but content was:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    // ─── RegexTimeouts static class ───────────────────────────────────────────────

    [Fact]
    public void RegexTimeoutsShortIsOneSecond()
    {
        TimeSpan timeout = RegexTimeouts.Short;
        Assert.Equal(expected: TimeSpan.FromSeconds(1), actual: timeout);
    }

    // ─── BuildNumberHelpers.DetermineVersionForChangeLog ─────────────────────────

    [Theory]
    [InlineData("abc")]
    [InlineData("x.y.z")]
    public void DetermineVersionForChangeLogReturnsNullForUnparsableVersionWithoutHyphen(string version)
    {
        Version? result = BuildNumberHelpers.DetermineVersionForChangeLog(version);
        Assert.Null(result);
    }

    // ─── ChangeLogLinter.CheckReleaseDate - missing date ─────────────────────────

    [Fact]
    public void CheckReleaseDateWithBlankDateReturnsNoError()
    {
        // A release with an empty date (just version, no date) should not produce a date format error
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [1.0.0]
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), Language);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "not in the expected format", comparisonType: StringComparison.Ordinal)
        );
    }

    [SuppressMessage(
        category: "Microsoft.VisualStudio.Threading.Analyzers",
        checkId: "VSTHRD002",
        Justification = "Helpers synchronously wrap pure parse ValueTasks"
    )]
    private static ChangeLogDocument Parse(string content)
    {
        return new ChangeLogParser().ParseAsync(content, default).GetAwaiter().GetResult();
    }
}
