using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Credfeto.ChangeLog;
using FunFair.Test.Infrastructure.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog.BenchMark.Tests.Bench;

[SimpleJob]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class ChangeLogCheckerBenchmark
{
    private const int OTHER_FILE_COUNT = 25;

    private const string ORIGIN_BRANCH_NAME = "origin/main";

    private const string CHANGE_LOG_WITH_RELEASES = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Added
        - Some unreleased item

        ## [1.0.0] - 2024-01-01
        ### Added
        - First release item

        ## [0.0.0] - Project created
        """;

    private const string CHANGE_LOG_WITH_RELEASES_UNRELEASED_UPDATED = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Added
        - Some unreleased item
        - Another unreleased item

        ## [1.0.0] - 2024-01-01
        ### Added
        - First release item

        ## [0.0.0] - Project created
        """;

    private ServiceProvider? _serviceProvider;
    private IChangeLogChecker? _checker;

    private string? _unchangedTempDir;
    private string? _unchangedChangeLogPath;

    private string? _modifiedTempDir;
    private string? _modifiedChangeLogPath;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ServiceCollection services = new();
        services.AddChangeLog();
        this._serviceProvider = services.BuildServiceProvider();
        this._checker = this._serviceProvider.GetRequiredService<IChangeLogChecker>();

        (this._unchangedTempDir, this._unchangedChangeLogPath) = await CreateRepoAsync(
            changeChangeLogInSecondCommit: false
        );
        (this._modifiedTempDir, this._modifiedChangeLogPath) = await CreateRepoAsync(
            changeChangeLogInSecondCommit: true
        );
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        this._serviceProvider?.Dispose();
        DeleteDirectory(this._unchangedTempDir);
        DeleteDirectory(this._modifiedTempDir);
    }

    [Benchmark]
    public Task<bool> ChangeLogModifiedInReleaseSection_ChangeLogUnchangedAsync()
    {
        IChangeLogChecker checker = this._checker ?? throw new InvalidOperationException("Setup has not run");
        string changeLogPath = this._unchangedChangeLogPath ?? throw new InvalidOperationException("Setup has not run");

        return checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLogPath,
            originBranchName: ORIGIN_BRANCH_NAME,
            cancellationToken: CancellationToken.None
        );
    }

    [Benchmark]
    public Task<bool> ChangeLogModifiedInReleaseSection_UnreleasedSectionChangedAsync()
    {
        IChangeLogChecker checker = this._checker ?? throw new InvalidOperationException("Setup has not run");
        string changeLogPath = this._modifiedChangeLogPath ?? throw new InvalidOperationException("Setup has not run");

        return checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLogPath,
            originBranchName: ORIGIN_BRANCH_NAME,
            cancellationToken: CancellationToken.None
        );
    }

    private static async Task<(string TempDir, string ChangeLogPath)> CreateRepoAsync(
        bool changeChangeLogInSecondCommit
    )
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "clc-bench-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLogPath, CHANGE_LOG_WITH_RELEASES, Encoding.UTF8, CancellationToken.None);

        string[] otherFilePaths = await CreateOtherFilesAsync(tempDir);

        string repoPath = Repository.Init(tempDir);

        using (Repository repo = new(repoPath))
        {
            repo.Config.Set("user.name", "Benchmark User");
            repo.Config.Set("user.email", "benchmark@example.com");

            Signature author = new("Benchmark User", "benchmark@example.com", MockDateTimeSources.Past.GetUtcNow());
            Commit initialCommit = CommitInitial(
                repo: repo,
                changeLogPath: changeLogPath,
                otherFilePaths: otherFilePaths,
                author: author
            );

            repo.Branches.Add(ORIGIN_BRANCH_NAME, initialCommit);

            await CommitSecondAsync(
                repo: repo,
                changeLogPath: changeLogPath,
                otherFilePaths: otherFilePaths,
                author: author,
                changeChangeLogInSecondCommit: changeChangeLogInSecondCommit
            );
        }

        return (tempDir, changeLogPath);
    }

    private static async Task<string[]> CreateOtherFilesAsync(string tempDir)
    {
        string[] otherFilePaths = new string[OTHER_FILE_COUNT];

        for (int i = 0; i < OTHER_FILE_COUNT; ++i)
        {
            string otherFilePath = Path.Combine(tempDir, "file-" + i.ToString(CultureInfo.InvariantCulture) + ".txt");
            await File.WriteAllTextAsync(
                otherFilePath,
                "Initial content " + i.ToString(CultureInfo.InvariantCulture),
                Encoding.UTF8,
                CancellationToken.None
            );
            otherFilePaths[i] = otherFilePath;
        }

        return otherFilePaths;
    }

    private static Commit CommitInitial(
        Repository repo,
        string changeLogPath,
        string[] otherFilePaths,
        Signature author
    )
    {
        Commands.Stage(repo, changeLogPath);

        foreach (string otherFilePath in otherFilePaths)
        {
            Commands.Stage(repo, otherFilePath);
        }

        return repo.Commit("Initial commit", author, author);
    }

    private static async Task CommitSecondAsync(
        Repository repo,
        string changeLogPath,
        string[] otherFilePaths,
        Signature author,
        bool changeChangeLogInSecondCommit
    )
    {
        foreach (string otherFilePath in otherFilePaths)
        {
            await File.WriteAllTextAsync(otherFilePath, "Updated content", Encoding.UTF8, CancellationToken.None);
            Commands.Stage(repo, otherFilePath);
        }

        if (changeChangeLogInSecondCommit)
        {
            await File.WriteAllTextAsync(
                changeLogPath,
                CHANGE_LOG_WITH_RELEASES_UNRELEASED_UPDATED,
                Encoding.UTF8,
                CancellationToken.None
            );
            Commands.Stage(repo, changeLogPath);
        }

        repo.Commit("Second commit", author, author);
    }

    private static void DeleteDirectory(string? path)
    {
        if (path is null || !Directory.Exists(path))
        {
            return;
        }

        DirectoryInfo directoryInfo = new(path);

        foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directoryInfo.Delete(true);
    }
}
