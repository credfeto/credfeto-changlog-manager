using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogFileOperationsTests : TestBase, IDisposable
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

    private readonly ServiceProvider _serviceProvider;

    public ChangeLogFileOperationsTests()
    {
        ServiceCollection services = new();
        services.AddChangeLog();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    [Fact]
    public async Task ExtractReleaseNotesFromFileAsyncReadsChangeLogFromDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        string fileName = await CreateTempFileAsync(SIMPLE_CHANGE_LOG, cancellationTokenSource.Token);

        try
        {
            IChangeLogReader reader = this._serviceProvider.GetRequiredService<IChangeLogReader>();

            string result = await reader.ExtractReleaseNotesFromFileAsync(
                changeLogFileName: fileName,
                version: string.Empty,
                cancellationToken: cancellationTokenSource.Token
            );

            Assert.True(
                result.Contains("### Added", StringComparison.Ordinal),
                userMessage: $"Expected release notes to contain the Added heading, but got:{Environment.NewLine}{result}"
            );
            Assert.True(
                result.Contains("- Added item", StringComparison.Ordinal),
                userMessage: $"Expected release notes to contain the Added item, but got:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task FindFirstReleaseVersionPositionAsyncReadsLinesFromDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        string fileName = await CreateTempFileAsync(SIMPLE_CHANGE_LOG, cancellationTokenSource.Token);

        try
        {
            IChangeLogReader reader = this._serviceProvider.GetRequiredService<IChangeLogReader>();

            int? result = await reader.FindFirstReleaseVersionPositionAsync(
                changeLogFileName: fileName,
                cancellationToken: cancellationTokenSource.Token
            );

            Assert.Equal(expected: 13, actual: result);
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task LintFileAsyncReadsChangeLogFromDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        const string invalidChangeLog =
            """
            # Changelog
            All notable changes to this project will be documented in this file.
            """;
        string fileName = await CreateTempFileAsync(invalidChangeLog, cancellationTokenSource.Token);

        try
        {
            IChangeLogLinter linter = this._serviceProvider.GetRequiredService<IChangeLogLinter>();

            IReadOnlyList<LintError> result = await linter.LintFileAsync(
                changeLogFileName: fileName,
                additionalSections: null,
                cancellationToken: cancellationTokenSource.Token
            );

            LintError error = Assert.Single(result);
            Assert.Equal(expected: "Missing [Unreleased] section", actual: error.Message);
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task FixFileAsyncUpdatesChangeLogOnDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        const string invalidChangeLog =
            """
            ## [Unreleased]
            ### Added

            - Added item
            ### Fixed
            ### Changed
            ### Removed
            """;
        string fileName = await CreateTempFileAsync(invalidChangeLog, cancellationTokenSource.Token);

        try
        {
            IChangeLogFixer fixer = this._serviceProvider.GetRequiredService<IChangeLogFixer>();

            await fixer.FixFileAsync(
                changeLogFileName: fileName,
                additionalSections: null,
                cancellationToken: cancellationTokenSource.Token
            );

            string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            string invalidSection = "### Added" + Environment.NewLine + Environment.NewLine + "- Added item";
            string validSection = "### Added" + Environment.NewLine + "- Added item";
            Assert.False(
                result.Contains(invalidSection, StringComparison.Ordinal),
                userMessage: $"Expected fixer to remove the blank line after the heading, but file contained:{Environment.NewLine}{result}"
            );
            Assert.True(
                result.Contains(validSection, StringComparison.Ordinal),
                userMessage: $"Expected fixer to keep the heading and item together, but file contained:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task AddEntryAsyncCreatesChangeLogWhenFileDoesNotExist()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        string fileName = CreateTempFilePath();

        try
        {
            IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

            await updater.AddEntryAsync(
                changeLogFileName: fileName,
                type: "Added",
                message: "Created from missing file",
                cancellationToken: cancellationTokenSource.Token
            );

            string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            Assert.True(
                result.Contains("Created from missing file", StringComparison.Ordinal),
                userMessage: $"Expected the created changelog to contain the new entry, but got:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task AddEntryAsyncReadsExistingChangeLogFromDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        string fileName = await CreateTempFileAsync(SIMPLE_CHANGE_LOG, cancellationTokenSource.Token);

        try
        {
            IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

            await updater.AddEntryAsync(
                changeLogFileName: fileName,
                type: "Added",
                message: "Existing file entry",
                cancellationToken: cancellationTokenSource.Token
            );

            string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            Assert.True(
                result.Contains("Existing file entry", StringComparison.Ordinal),
                userMessage: $"Expected the existing changelog to contain the new entry, but got:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    [Fact]
    public async Task CreateReleaseAsyncReadsExistingChangeLogFromDisk()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        string fileName = await CreateTempFileAsync(SIMPLE_CHANGE_LOG, cancellationTokenSource.Token);

        try
        {
            IChangeLogUpdater updater = this._serviceProvider.GetRequiredService<IChangeLogUpdater>();

            await updater.CreateReleaseAsync(
                changeLogFileName: fileName,
                version: "1.2.3",
                pending: true,
                cancellationToken: cancellationTokenSource.Token
            );

            string result = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationTokenSource.Token);
            Assert.True(
                result.Contains("## [1.2.3] - TBD", StringComparison.Ordinal),
                userMessage: $"Expected the created release header to exist, but got:{Environment.NewLine}{result}"
            );
            Assert.True(
                result.Contains("- Added item", StringComparison.Ordinal),
                userMessage: $"Expected release contents to retain the Added item, but got:{Environment.NewLine}{result}"
            );
        }
        finally
        {
            DeleteIfExists(fileName);
        }
    }

    private static string CreateTempFilePath()
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");
    }

    private static async Task<string> CreateTempFileAsync(string content, CancellationToken cancellationToken)
    {
        string fileName = CreateTempFilePath();

        await File.WriteAllTextAsync(fileName, content, Encoding.UTF8, cancellationToken);

        return fileName;
    }

    private static void DeleteIfExists(string fileName)
    {
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
    }
}
