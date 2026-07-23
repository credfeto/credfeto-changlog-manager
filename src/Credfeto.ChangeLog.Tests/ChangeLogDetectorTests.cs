using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using FunFair.Test.Infrastructure.Mocks;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogDetectorTests : LoggingFolderCleanupTestBase
{
    public ChangeLogDetectorTests(ITestOutputHelper output)
        : base(output) { }

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
        await CreateRepoWithChangeLogAtRootAsync(this.TempFolder, SIMPLE_CHANGE_LOG, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
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

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenNoChangeLogInRepo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await CreateRepoWithReadmeOnlyAsync(this.TempFolder, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
            ChangeLogDetector detector = new();
            bool found = detector.TryFindChangeLog(out string? changeLogFileName);

            Assert.False(found, userMessage: "Expected TryFindChangeLog to return false when no CHANGELOG.md exists");
            Assert.Null(changeLogFileName);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
        }
    }

    [Fact]
    public async Task TryFindChangeLogReturnsTrueWhenMultipleChangeLogsAndOneAtRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await CreateRepoWithRootAndSubChangeLogAsync(this.TempFolder, SIMPLE_CHANGE_LOG, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
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

    [Fact]
    public async Task TryFindChangeLogReturnsTrueWhenSingleNestedChangeLogAndNoneAtRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await CreateRepoWithSingleNestedChangeLogAsync(this.TempFolder, SIMPLE_CHANGE_LOG, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
            ChangeLogDetector detector = new();
            bool found = detector.TryFindChangeLog(out string? changeLogFileName);

            Assert.True(
                found,
                userMessage: "Expected TryFindChangeLog to return true when single CHANGELOG.md exists in a subdirectory"
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

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenMultipleChangeLogsAndNoneAtRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await CreateRepoWithSubdirectoryChangeLogsOnlyAsync(this.TempFolder, SIMPLE_CHANGE_LOG, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
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

    [Fact]
    public async Task TryFindChangeLogReturnsFalseWhenDirectoryIsNotAGitRepo()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string changeLogPath = Path.Combine(this.TempFolder, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLogPath, SIMPLE_CHANGE_LOG, Encoding.UTF8, cancellationToken);

        string originalDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = this.TempFolder;
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
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
        repo.Commit("Initial commit", author, author);
    }

    private static async Task CreateRepoWithSingleNestedChangeLogAsync(
        string tempDir,
        string changeLogContent,
        CancellationToken cancellationToken
    )
    {
        Directory.CreateDirectory(tempDir);

        string subDir = Path.Combine(tempDir, "project");
        Directory.CreateDirectory(subDir);
        string changeLog = Path.Combine(subDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLog, changeLogContent, Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        using Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLog);
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
        repo.Commit("Initial commit", author, author);
    }
}
