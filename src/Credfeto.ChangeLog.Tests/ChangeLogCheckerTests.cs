using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using FunFair.Test.Infrastructure.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogCheckerTests : LoggingFolderCleanupTestBase, IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IChangeLogChecker _checker;

    public ChangeLogCheckerTests(ITestOutputHelper output)
        : base(output)
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

        (string changeLogPath, string branchName) = await CreateRepoWithChangeLogAsync(
            this.TempFolder,
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

        (string changeLogPath, string branchName) = await CreateRepoWithChangeLogAsync(
            this.TempFolder,
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

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionThrowsWhenBranchDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (string changeLogPath, _) = await CreateRepoWithChangeLogAsync(
            this.TempFolder,
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

    [Fact]
    public async Task ChangeLogNotInDiffReturnsTrueForReleaseSectionCheck()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] changeLogBytes = Encoding.UTF8.GetBytes(CHANGE_LOG_WITH_RELEASES);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                changeLogBytes,
                cancellationToken
            );

        using (repo)
        {
            // Add a second commit that changes a different file (not the changelog)
            string readmePath = Path.Combine(this.TempFolder, "README.md");
            await File.WriteAllTextAsync(readmePath, "# Updated README", Encoding.UTF8, cancellationToken);
            Commands.Stage(repo, readmePath);
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
            repo.Commit("Add README", author, author);
        }

        bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLogPath,
            originBranchName: originBranchName,
            cancellationToken: cancellationToken
        );

        Assert.True(result, userMessage: "Expected true when changelog was not modified at all since origin");
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenOnlyUnreleasedSectionChanged()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] changeLogBytes = Encoding.UTF8.GetBytes(CHANGE_LOG_WITH_RELEASES);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                changeLogBytes,
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
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsFalseWhenReleaseSectionModified()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] changeLogBytes = Encoding.UTF8.GetBytes(CHANGE_LOG_WITH_RELEASES);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                changeLogBytes,
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
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
            repo.Commit("Modify release section", author, author);
        }

        bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLogPath,
            originBranchName: originBranchName,
            cancellationToken: cancellationToken
        );

        Assert.False(result, userMessage: "Expected false when the release section was modified");
    }

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenUnreleasedItemRemoved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] changeLogBytes = Encoding.UTF8.GetBytes(CHANGE_LOG_WITH_RELEASES);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                changeLogBytes,
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
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenChangelogModifiedWithoutTrailingNewline()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] changeLogBytes = Encoding.UTF8.GetBytes(CHANGE_LOG_WITH_RELEASES);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                changeLogBytes,
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
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
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

    [Fact]
    public async Task ChangeLogModifiedInReleaseSectionReturnsTrueWhenOnlyUnreleasedSectionAddedAndTrailingNewlineRemoved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Initial content WITH trailing newline (to establish baseline)
        const string INITIAL_CHANGE_LOG =
            "# Changelog\n\n## [Unreleased]\n### Added\n- Item\n\n## [1.0.0] - 2024-01-01\n### Added\n- First\n\n## [0.0.0] - Created\n";
        byte[] initialBytes = Encoding.UTF8.GetBytes(INITIAL_CHANGE_LOG);
        (string changeLogPath, string originBranchName, Repository repo) =
            await GitRepositoryHelpers.CreateRepoWithOriginBranchAsync(
                this.TempFolder,
                initialBytes,
                cancellationToken
            );

        using (repo)
        {
            // Updated content WITHOUT trailing newline, with an extra item in unreleased
            const string UPDATED_CHANGE_LOG =
                "# Changelog\n\n## [Unreleased]\n### Added\n- Item\n- Extra item\n\n## [1.0.0] - 2024-01-01\n### Added\n- First\n\n## [0.0.0] - Created";
            byte[] bytes = Encoding.UTF8.GetBytes(UPDATED_CHANGE_LOG);
            await File.WriteAllBytesAsync(changeLogPath, bytes, cancellationToken);
            Commands.Stage(repo, changeLogPath);
            Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
            repo.Commit("Add item and remove trailing newline", author, author);
        }

        bool result = await this._checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLogPath,
            originBranchName: originBranchName,
            cancellationToken: cancellationToken
        );

        Assert.True(result, userMessage: "Expected true when unreleased section modified and trailing newline removed");
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
        Signature author = new("Test User", "test@example.com", MockDateTimeSources.Past.GetUtcNow());
        repo.Commit("Initial commit", author, author);

        string branchName = repo.Head.FriendlyName;
        return (changeLogPath, branchName);
    }
}
