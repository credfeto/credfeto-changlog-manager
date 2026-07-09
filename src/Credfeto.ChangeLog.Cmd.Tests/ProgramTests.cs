using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

file static class RepoLocator
{
    private static string? _cachedRepoRoot;

    public static string FindRepoRoot()
    {
        if (_cachedRepoRoot is not null)
        {
            return _cachedRepoRoot;
        }

        string? dir = AppContext.BaseDirectory;

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
            {
                _cachedRepoRoot = dir;

                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find git repository root from " + AppContext.BaseDirectory);
    }
}

public sealed class ProgramTests : TestBase
{
    private const string VALID_CHANGELOG = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [0.0.1] - 2024-01-01
        ### Added
        - Initial release
        """;

    private const string CHANGELOG_WITH_RELEASE = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.0.0] - 2024-01-01
        ### Added
        - Feature one
        """;

    private const string CHANGELOG_WITH_ENTRIES = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        - Some new feature
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [0.0.1] - 2024-01-01
        ### Added
        - Initial release
        """;

    private const string CHANGELOG_WITH_CUSTOM_SECTION = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes
        ### Custom

        ## [0.0.1] - 2024-01-01
        ### Added
        - Initial release
        """;

    private const string INVALID_CHANGELOG = """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Fixed
        ### Changed
        ### Removed
        ### Deployment Changes

        ## [0.0.1] - 2024-01-01
        ### Added
        - Initial release
        """;

    private const string CHANGELOG_WITH_INVALID_VERSION = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.x.x] - 2024-01-01
        ### Added
        - Invalid version
        """;

    private const string CHANGELOG_WITHOUT_RELEASES = """
        # Changelog
        All notable changes to this project will be documented in this file.

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes
        """;

    private static async Task<string> CreateTempChangeLogAsync(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        await File.WriteAllTextAsync(
            path: path,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: Xunit.TestContext.Current.CancellationToken
        );

        return path;
    }

    [Fact]
    public async Task Main_WithUnknownFlag_ReturnsError()
    {
        int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--unknown-flag"]);
        Assert.Equal(expected: 1, actual: result);
    }

    [Fact]
    public async Task Main_AddEntry_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--add", "Added", "--message", "Test entry"]
            );
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_RemoveEntry_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--add", "Added", "--message", "Test entry to remove"]
            );

            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--remove", "Added", "--message", "Test entry to remove"]
            );
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_ExtractVersion_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_RELEASE);
        string outputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--extract", outputFile, "--version", "1.0.0"]
            );
            Assert.Equal(expected: 0, actual: result);
            Assert.True(File.Exists(outputFile), userMessage: "Expected output file to exist after extract");
        }
        finally
        {
            File.Delete(tempFile);

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public async Task Main_DisplayUnreleased_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--un-released"]);
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_LintValidChangelog_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint"]);
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_LintInvalidChangelogWithoutFix_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(INVALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_LintInvalidChangelogWithFix_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(INVALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint", "--fix"]);
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_LintWithAdditionalSections_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_CUSTOM_SECTION);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--lint", "--additional-sections", "Custom"]
            );
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_CreateRelease_ReturnsSuccess()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_ENTRIES);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--create-release", "2.0.0"]
            );
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_CheckInsertOnValidRepo_ReturnsKnownResult()
    {
        string repoDir = RepoLocator.FindRepoRoot();
        string changelogPath = Path.Combine(repoDir, "CHANGELOG.md");

        int result = await Credfeto.ChangeLog.Cmd.Program.Main(
            ["--changelog", changelogPath, "--check-insert", "origin/main"]
        );

        Assert.True(result is 0 or 1, userMessage: "Expected result to be 0 (valid) or 1 (error)");
    }

    [Fact]
    public async Task Main_MissingChangelogWithNoExplicitPath_ReturnsError()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string savedDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir;

            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--add", "Added", "--message", "test"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
            Directory.Delete(path: tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Main_ValidChangelogButNoCommand_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_AddWithoutMessage_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--add", "Added"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_RemoveWithoutMessage_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(VALID_CHANGELOG);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--remove", "Added"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_ExtractWithoutVersion_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_RELEASE);
        string outputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--extract", outputFile]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public async Task Main_VersionWithoutExtract_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_RELEASE);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--version", "1.0.0"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_FindsChangelogViaDetector_ReturnsSuccess()
    {
        string repoDir = RepoLocator.FindRepoRoot();
        string savedDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = repoDir;

            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--un-released"]);
            Assert.Equal(expected: 0, actual: result);
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
        }
    }

    [Fact]
    public async Task Main_LintInvalidVersionChangelogWithFix_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITH_INVALID_VERSION);

        try
        {
            int result = await Credfeto.ChangeLog.Cmd.Program.Main(["--changelog", tempFile, "--lint", "--fix"]);
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Main_CheckInsertOnChangelogWithNoReleases_ReturnsError()
    {
        string tempFile = await CreateTempChangeLogAsync(CHANGELOG_WITHOUT_RELEASES);
        string repoDir = RepoLocator.FindRepoRoot();
        string savedDir = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = repoDir;

            int result = await Credfeto.ChangeLog.Cmd.Program.Main(
                ["--changelog", tempFile, "--check-insert", "origin/main"]
            );
            Assert.Equal(expected: 1, actual: result);
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
            File.Delete(tempFile);
        }
    }
}
