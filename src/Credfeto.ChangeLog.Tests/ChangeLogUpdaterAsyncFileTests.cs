using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "NSubstitute .Returns() for ValueTask is idiomatic test pattern"
)]
public sealed class ChangeLogUpdaterAsyncFileTests : LoggingFolderCleanupTestBase, IDisposable
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private readonly ServiceProvider _serviceProvider;

    public ChangeLogUpdaterAsyncFileTests(ITestOutputHelper output)
        : base(output)
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
    public async Task CreateReleaseWithPendingFalseUsesCurrentDate()
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

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);
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

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(simpleChangeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        string tempFile = Path.Combine(this.TempFolder, "test.md");
        await File.WriteAllTextAsync(tempFile, string.Empty, cancellationTokenSource.Token);

        await updater.RemoveEntryAsync(
            changeLogFileName: tempFile,
            language: Language,
            type: "Added",
            message: "Item to remove",
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

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

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(simpleChangeLog);
        IChangeLogStorage storage = GetSubstitute<IChangeLogStorage>();
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        ChangeLogUpdater updater = new(storage);

        string tempFile = Path.Combine(this.TempFolder, "test-release.md");

        await updater.CreateReleaseAsync(
            changeLogFileName: tempFile,
            language: Language,
            version: "1.0.0",
            pending: true,
            cancellationToken: cancellationTokenSource.Token
        );

        await storage.Received(1).LoadAsync(tempFile, Arg.Any<CancellationToken>());
        await storage.Received(1).SaveAsync(tempFile, Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdaterCreateEmptyAsyncWritesEmptyChangeLog()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

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

        string fileName = Path.Combine(this.TempFolder, $"{Guid.NewGuid():N}.md");

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
}
