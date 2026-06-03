using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class GitRepositoryHelpers
{
    private static readonly DateTimeOffset FixedCommitTime = new(
        year: 2024,
        month: 1,
        day: 1,
        hour: 0,
        minute: 0,
        second: 0,
        offset: TimeSpan.Zero
    );

    public static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        ForceDeleteDirectory(path);
    }

    public static void ForceDeleteDirectory(string path)
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

    /// <summary>
    /// Creates a temp git repo with an initial commit containing a changelog file written from raw bytes,
    /// then creates a named "origin/main" branch pointing to that commit.
    /// Returns an open <see cref="Repository" /> so the caller can add more commits.
    /// HEAD stays on the original branch (master/main).
    /// </summary>
    public static async Task<(
        string ChangeLogPath,
        string OriginBranchName,
        Repository Repo
    )> CreateRepoWithOriginBranchAsync(string tempDir, byte[] changeLogBytes, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDir);
        string changeLogPath = Path.Combine(tempDir, "CHANGELOG.md");
        await File.WriteAllBytesAsync(changeLogPath, changeLogBytes, cancellationToken);

        string repoPath = Repository.Init(tempDir);

        Repository repo = new(repoPath);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Commands.Stage(repo, changeLogPath);
        Signature author = new("Test User", "test@example.com", FixedCommitTime);
        Commit initialCommit = repo.Commit("Initial commit", author, author);

        const string ORIGIN_BRANCH_NAME = "origin/main";
        repo.Branches.Add(ORIGIN_BRANCH_NAME, initialCommit);

        return (changeLogPath, ORIGIN_BRANCH_NAME, repo);
    }
}
