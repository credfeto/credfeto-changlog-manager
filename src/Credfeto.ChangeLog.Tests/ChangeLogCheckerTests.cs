using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogCheckerTests : TestBase, IDisposable
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

    private readonly ServiceProvider _serviceProvider;
    private readonly IChangeLogChecker _checker;

    public ChangeLogCheckerTests()
    {
        ServiceCollection services = new();
        services.AddChangeLog();
        this._serviceProvider = services.BuildServiceProvider();
        this._checker = this._serviceProvider.GetRequiredService<IChangeLogChecker>();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

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

    private const string CHANGE_LOG_ONLY_UNRELEASED = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Added
        - Some unreleased item
        """;

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsFalseWhenNoReleasesExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string branchName) = await CreateRepoWithChangeLogAsync(
                tempDir,
                CHANGE_LOG_ONLY_UNRELEASED,
                cancellationToken
            );

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: branchName,
                cancellationToken: cancellationToken
            );

            Assert.False(result, userMessage: "Expected false when changelog has no releases");
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public Task ChangeLogModifiedInReleaseSectionThrowsWhenFileDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        return Assert.ThrowsAsync<InvalidChangeLogException>(async () =>
        {
            await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: Path.Combine(
                    Path.GetTempPath(),
                    Path.GetRandomFileName(),
                    "nonexistent-CHANGELOG.md"
                ),
                originBranchName: "origin/main",
                cancellationToken: cancellationToken
            );
        });
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsFalseWhenHeadIsAtSameShaAsOrigin()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string branchName) = await CreateRepoWithChangeLogAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            // HEAD equals origin tip → should return false
            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: branchName,
                cancellationToken: cancellationToken
            );

            Assert.False(result, userMessage: "Expected false when HEAD is at the same SHA as origin branch");
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionThrowsWhenBranchDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, _) = await CreateRepoWithChangeLogAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            await Assert.ThrowsAsync<BranchMissingException>(async () =>
            {
                await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                    changeLogFileName: changeLogPath,
                    originBranchName: "nonexistent/branch",
                    cancellationToken: cancellationToken
                );
            });
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenChangeLogNotModifiedSinceOrigin()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string originBranchName, Repository repo) = await CreateRepoWithOriginBranchAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            using (repo)
            {
                // Add a second commit that changes a different file (not the changelog)
                string readmePath = Path.Combine(tempDir, "README.md");
                await File.WriteAllTextAsync(readmePath, "# Updated README", Encoding.UTF8, cancellationToken);
                Commands.Stage(repo, readmePath);
                Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
                repo.Commit("Add README", author, author);
            }

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: originBranchName,
                cancellationToken: cancellationToken
            );

            Assert.True(result, userMessage: "Expected true when changelog was not modified at all since origin");
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenOnlyUnreleasedSectionChanged()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string originBranchName, Repository repo) = await CreateRepoWithOriginBranchAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            using (repo)
            {
                const string UPDATED_CHANGE_LOG = """
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

                await File.WriteAllTextAsync(changeLogPath, UPDATED_CHANGE_LOG, Encoding.UTF8, cancellationToken);
                Commands.Stage(repo, changeLogPath);
                Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
                repo.Commit("Add unreleased item", author, author);
            }

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: originBranchName,
                cancellationToken: cancellationToken
            );

            Assert.True(
                result,
                userMessage: "Expected true (no release-section modification) when only the unreleased section was changed"
            );
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsFalseWhenReleaseSectionModified()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string originBranchName, Repository repo) = await CreateRepoWithOriginBranchAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            using (repo)
            {
                // Modify the RELEASE section (lines after ## [1.0.0])
                const string UPDATED_CHANGE_LOG_RELEASE_MODIFIED = """
                    # Changelog
                    All notable changes to this project will be documented in this file.

                    ## [Unreleased]
                    ### Added
                    - Some unreleased item

                    ## [1.0.0] - 2024-01-01
                    ### Added
                    - First release item
                    - Sneaked in item

                    ## [0.0.0] - Project created
                    """;

                await File.WriteAllTextAsync(
                    changeLogPath,
                    UPDATED_CHANGE_LOG_RELEASE_MODIFIED,
                    Encoding.UTF8,
                    cancellationToken
                );
                Commands.Stage(repo, changeLogPath);
                Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
                repo.Commit("Modify release section", author, author);
            }

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: originBranchName,
                cancellationToken: cancellationToken
            );

            Assert.False(result, userMessage: "Expected false when the release section was modified");
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenUnreleasedItemRemoved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string originBranchName, Repository repo) = await CreateRepoWithOriginBranchAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            using (repo)
            {
                // Remove the unreleased item (- line in diff)
                const string UPDATED_CHANGE_LOG_ITEM_REMOVED = """
                    # Changelog
                    All notable changes to this project will be documented in this file.

                    ## [Unreleased]
                    ### Added

                    ## [1.0.0] - 2024-01-01
                    ### Added
                    - First release item

                    ## [0.0.0] - Project created
                    """;

                await File.WriteAllTextAsync(
                    changeLogPath,
                    UPDATED_CHANGE_LOG_ITEM_REMOVED,
                    Encoding.UTF8,
                    cancellationToken
                );
                Commands.Stage(repo, changeLogPath);
                Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
                repo.Commit("Remove unreleased item", author, author);
            }

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: originBranchName,
                cancellationToken: cancellationToken
            );

            Assert.True(
                result,
                userMessage: "Expected true (no release-section modification) when an unreleased item was removed"
            );
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenChangelogModifiedWithoutTrailingNewline()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            (string changeLogPath, string originBranchName, Repository repo) = await CreateRepoWithOriginBranchAsync(
                tempDir,
                CHANGE_LOG_WITH_RELEASES,
                cancellationToken
            );

            using (repo)
            {
                // Write file without trailing newline - this triggers "\ No newline at end of file" in git diff
                const string UPDATED_CHANGE_LOG_NO_NEWLINE =
                    "# Changelog\nAll notable changes to this project will be documented in this file.\n\n"
                    + "## [Unreleased]\n### Added\n- Some unreleased item\n- Another item\n\n"
                    + "## [1.0.0] - 2024-01-01\n### Added\n- First release item\n\n"
                    + "## [0.0.0] - Project created";

                byte[] bytes = Encoding.UTF8.GetBytes(UPDATED_CHANGE_LOG_NO_NEWLINE);
                await File.WriteAllBytesAsync(changeLogPath, bytes, cancellationToken);
                Commands.Stage(repo, changeLogPath);
                Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
                repo.Commit("Update changelog without trailing newline", author, author);
            }

            bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
                changeLogFileName: changeLogPath,
                originBranchName: originBranchName,
                cancellationToken: cancellationToken
            );

            // The change is in the unreleased section, should return true (OK)
            Assert.True(
                result,
                userMessage: "Expected true when changelog modification is in unreleased section (no trailing newline case)"
            );
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    /// <summary>
    /// Creates a temp git repo with a changelog. HEAD == origin tip (same commit).
    /// Returns changelog path and the branch name to use as originBranchName.
    /// </summary>
    private static async Task<(string ChangeLogPath, string BranchName)> CreateRepoWithChangeLogAsync(
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

        string branchName = repo.Head.FriendlyName;
        return (changeLogPath, branchName);
    }

    /// <summary>
    /// Creates a temp git repo with an initial commit, then creates a named "origin" branch
    /// pointing to that commit. Returns an open Repository so the caller can add more commits.
    /// HEAD stays on the original branch (master/main).
    /// </summary>
    private static async Task<(
        string ChangeLogPath,
        string OriginBranchName,
        Repository Repo
    )> CreateRepoWithOriginBranchAsync(string tempDir, string changeLogContent, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDir);
        string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllTextAsync(changeLogPath, changeLogContent, Encoding.UTF8, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLogPath);
        Signature author = new("Test User", "test@example.com", FIXED_COMMIT_TIME);
        Commit initialCommit = repo.Commit("Initial commit", author, author);

        const string ORIGIN_BRANCH_NAME = "origin/main";
        repo.Branches.Add(ORIGIN_BRANCH_NAME, initialCommit);

        return (changeLogPath, ORIGIN_BRANCH_NAME, repo);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        ForceDeleteDirectory(path);
    }

    private static void ForceDeleteDirectory(string path)
    {
        DirectoryInfo di = new(path);

        foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        foreach (DirectoryInfo dir in di.GetDirectories("*", SearchOption.AllDirectories))
        {
            dir.Attributes = FileAttributes.Normal;
        }

        di.Delete(true);
    }
}
