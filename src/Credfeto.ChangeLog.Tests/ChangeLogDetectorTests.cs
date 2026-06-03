using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[Collection("Sequential")]
public sealed class ChangeLogDetectorTests : TestBase
{
    private static readonly DateTimeOffset FIXED_COMMIT_TIME = new(
        year: 2024,
        month: 1,
        day: 1,
        hour: 0,
        minute: 0,
        second: 0,
        offset: TimeSpan.Zero
    );

    private const string SIMPLE_CHANGE_LOG = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Added

        ## [0.0.0] - Project created
        """;

    [Fact]
    public void TryFindChangeLogDoesNotThrow()
    {
        ChangeLogDetector detector = new();

        // Verify the method completes without throwing even when called from any directory
        detector.TryFindChangeLog(out _);
    }

    [Fact]
    public async Task TryFindChangeLogReturnsTrueAndChangeLogPathWhenChangeLogExistsAtRepoRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await CreateRepoWithChangeLogAtRootAsync(tempDir, SIMPLE_CHANGE_LOG, cancellationToken);

            string originalDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = tempDir;
                ChangeLogDetector detector = new();
                bool found = detector.TryFindChangeLog(out string? changeLogFileName);

                Assert.True(
                    found,
                    userMessage: "Expected TryFindChangeLog to return true when CHANGELOG.md exists at repo root"
                );
                Assert.NotNull(changeLogFileName);
                Assert.True(
                    File.Exists(changeLogFileName),
                    userMessage: $"Expected changelog to exist at: {changeLogFileName}"
                );
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
        }
        finally
        {
            GitRepositoryHelpers.DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenNoChangeLogInRepo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await CreateRepoWithReadmeOnlyAsync(tempDir, cancellationToken);

            string originalDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = tempDir;
                ChangeLogDetector detector = new();
                bool found = detector.TryFindChangeLog(out string? changeLogFileName);

                Assert.False(
                    found,
                    userMessage: "Expected TryFindChangeLog to return false when no CHANGELOG.md exists"
                );
                Assert.Null(changeLogFileName);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
        }
        finally
        {
            GitRepositoryHelpers.DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task TryFindChangeLogReturnsTrueWhenMultipleChangeLogsAndOneAtRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await CreateRepoWithRootAndSubChangeLogAsync(tempDir, SIMPLE_CHANGE_LOG, cancellationToken);

            string originalDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = tempDir;
                ChangeLogDetector detector = new();
                bool found = detector.TryFindChangeLog(out string? changeLogFileName);

                Assert.True(
                    found,
                    userMessage: "Expected TryFindChangeLog to return true when root CHANGELOG.md exists among multiple changelogs"
                );
                Assert.NotNull(changeLogFileName);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
        }
        finally
        {
            GitRepositoryHelpers.DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenMultipleChangeLogsAndNoneAtRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            await CreateRepoWithSubdirectoryChangeLogsOnlyAsync(tempDir, SIMPLE_CHANGE_LOG, cancellationToken);

            string originalDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = tempDir;
                ChangeLogDetector detector = new();
                bool found = detector.TryFindChangeLog(out string? changeLogFileName);

                Assert.False(
                    found,
                    userMessage: "Expected TryFindChangeLog to return false when no CHANGELOG.md at repo root"
                );
                Assert.Null(changeLogFileName);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
        }
        finally
        {
            GitRepositoryHelpers.DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenDirectoryIsNotAGitRepo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
            await File.WriteAllTextAsync(changeLogPath, SIMPLE_CHANGE_LOG, Encoding.UTF8, cancellationToken);

            string originalDir = Environment.CurrentDirectory;

            try
            {
                Environment.CurrentDirectory = tempDir;
                ChangeLogDetector detector = new();
                bool found = detector.TryFindChangeLog(out string? changeLogFileName);

                Assert.False(found, userMessage: "Expected TryFindChangeLog to return false when not in a git repo");
                Assert.Null(changeLogFileName);
            }
            finally
            {
                Environment.CurrentDirectory = originalDir;
            }
        }
        finally
        {
            GitRepositoryHelpers.DeleteDirectoryIfExists(tempDir);
        }
    }

    private static async Task CreateRepoWithChangeLogAtRootAsync(
        string tempDir,
        string changeLogContent,
        CancellationToken cancellationToken
    )
    {
        Directory.CreateDirectory(tempDir);
        string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLogPath, changeLogContent, Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        using Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLogPath);
        Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
        repo.Commit("Initial commit", author, author);
    }

    private static async Task CreateRepoWithReadmeOnlyAsync(string tempDir, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDir);
        string readmePath = Path.Combine(tempDir, "README.md");
        await File.WriteAllTextAsync(readmePath, "# Test Repo", Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        using Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, readmePath);
        Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
        repo.Commit("Initial commit", author, author);
    }

    private static async Task CreateRepoWithRootAndSubChangeLogAsync(
        string tempDir,
        string changeLogContent,
        CancellationToken cancellationToken
    )
    {
        Directory.CreateDirectory(tempDir);

        string rootChangeLog = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(rootChangeLog, changeLogContent, Encoding.UTF8, cancellationToken);

        string subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        string subChangeLog = Path.Combine(subDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(subChangeLog, changeLogContent, Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        using Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, rootChangeLog);
        Commands.Stage(repo, subChangeLog);
        Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
        repo.Commit("Initial commit", author, author);
    }

    private static async Task CreateRepoWithSubdirectoryChangeLogsOnlyAsync(
        string tempDir,
        string changeLogContent,
        CancellationToken cancellationToken
    )
    {
        Directory.CreateDirectory(tempDir);

        string subDir1 = Path.Combine(tempDir, "project1");
        string subDir2 = Path.Combine(tempDir, "project2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        string changeLog1 = Path.Combine(subDir1, "CHANGELOG.md");
        string changeLog2 = Path.Combine(subDir2, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLog1, changeLogContent, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(changeLog2, changeLogContent, Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        using Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLog1);
        Commands.Stage(repo, changeLog2);
        Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
        repo.Commit("Initial commit", author, author);
    }
}
