using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Helpers;
using LibGit2Sharp;
using ZLinq;

namespace Credfeto.ChangeLog;

public static class ChangeLogChecker
{
    public static async Task<bool> ChangeLogModifiedInReleaseSectionAsync(
        string changeLogFileName,
        string originBranchName,
        CancellationToken cancellationToken
    )
    {
        changeLogFileName = GetFullChangeLogFilePath(changeLogFileName);
        int? position = await ChangeLogReader.FindFirstReleaseVersionPositionAsync(
            changeLogFileName: changeLogFileName,
            cancellationToken: cancellationToken
        );

        if (position is null)
        {
            return false;
        }

        string changelogDir = GetChangeLogDirectory(changeLogFileName);
        Console.WriteLine($"Changelog Folder: {changelogDir}");

        using (Repository repo = GitRepository.OpenRepository(changelogDir))
        {
            string sha = HeadSha(repo);

            Branch originBranch = FindOriginBranch(repo: repo, originBranchName: originBranchName);

            if (StringComparer.Ordinal.Equals(x: originBranch.Tip.Sha, y: sha))
            {
                // same branch/commit
                return false;
            }

            string changeLogInRepoPath = FindChangeLogPositionInRepo(repo: repo, changeLogFileName: changeLogFileName);
            Console.WriteLine($"Relative to Repo Root: {changeLogInRepoPath}");

            int firstReleaseVersionIndex = position.Value;

            Patch changes = repo.Diff.Compare<Patch>(
                BranchTree(originBranch),
                HeadTree(repo),
                compareOptions: CompareSettings.BuildCompareOptions
            );

            PatchEntryChanges? change = changes.FirstOrDefault(candidate =>
                StringComparer.Ordinal.Equals(x: candidate.Path, y: changeLogInRepoPath)
            );

            if (change is not null)
            {
                return CheckForChangesAfterFirstRelease(
                    change: change,
                    firstReleaseVersionIndex: firstReleaseVersionIndex
                );
            }

            Console.WriteLine("Could not find change in diff");
        }

        return true;
    }

    private static Branch FindOriginBranch(Repository repo, string originBranchName)
    {
        return repo.Branches.FirstOrDefault(b => StringComparer.Ordinal.Equals(x: b.FriendlyName, y: originBranchName))
            ?? Throws.CouldNotFindBranch(originBranchName);
    }

    private static Tree BranchTree(Branch branch)
    {
        return branch.Tip.Tree;
    }

    private static Tree HeadTree(Repository repo)
    {
        return BranchTree(repo.Head);
    }

    private static string BranchSha(Branch branch)
    {
        return branch.Tip.Sha;
    }

    private static string HeadSha(Repository repo)
    {
        return BranchSha(repo.Head);
    }

    private static bool CheckForChangesAfterFirstRelease(PatchEntryChanges change, int firstReleaseVersionIndex)
    {
        Console.WriteLine("Change Details");
        string patchDetails = ExtractPatchDetails(change.Patch);
        Console.WriteLine(patchDetails);

        MatchCollection matches = CommonRegex.GitHunkPosition.Matches(patchDetails);

        foreach (Match? match in matches.OfType<Match?>())
        {
            if (match is null)
            {
                continue;
            }

            int changeStart = Convert.ToInt32(
                value: match.Groups["CurrentFileStart"].Value,
                provider: CultureInfo.InvariantCulture
            );

            if (
                !int.TryParse(
                    s: match.Groups["CurrentFileChangeLength"].Value,
                    style: NumberStyles.Integer,
                    provider: CultureInfo.InvariantCulture,
                    out int changeLength
                )
            )
            {
                changeLength = 1;
            }

            int changeEnd = changeStart + changeLength;

            if (changeEnd >= firstReleaseVersionIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static string ExtractPatchDetails(string patch)
    {
        Console.WriteLine(patch);
        List<string> lines = [.. patch.Split('\n')];

        RemoveLastLineIfBlank(lines);

        int lastHunk = lines.FindLastIndex(CommonRegex.GitHunkPosition.IsMatch);

        if (lastHunk != -1)
        {
            (List<string> before, List<string> after) = CompareHunk(lines: lines, lastHunk: lastHunk);

            if (before.SequenceEqual(second: after, comparer: StringComparer.Ordinal))
            {
                lines.RemoveRange(index: lastHunk, lines.Count - lastHunk);
            }
        }

        return string.Join(separator: Environment.NewLine, values: lines);
    }

    private static (List<string> before, List<string> after) CompareHunk(List<string> lines, int lastHunk)
    {
        List<string> before = [];
        List<string> after = [];

        foreach (string line in lines.Skip(lastHunk + 1))
        {
            switch (line[0])
            {
                case '+':
                    after.Add(line[1..]);

                    break;

                case '-':
                    before.Add(line[1..]);

                    break;

                case '\\':
                    if (StringComparer.Ordinal.Equals(x: line, y: @"\ No newline at end of file"))
                    {
                        break;
                    }

                    return Throws.CouldNotProcessDiffLine(line);

                default:
                    return Throws.CouldNotProcessDiffLine(line);
            }
        }

        return (before, after);
    }

    private static void RemoveLastLineIfBlank(List<string> lines)
    {
        int lastLine = lines.Count - 1;

        if (lastLine < 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(lines[lastLine]))
        {
            lines.RemoveAt(lastLine);
        }
    }

    private static string GetFullChangeLogFilePath(string changeLogFileName)
    {
        FileInfo changeLog = new(changeLogFileName);

        if (!changeLog.Exists)
        {
            return Throws.CouldNotFindChangeLog(changeLogFileName);
        }

        return changeLog.FullName;
    }

    private static string GetChangeLogDirectory(string changeLogFileName)
    {
        string? path = Path.GetDirectoryName(changeLogFileName);

        return path ?? Directory.GetCurrentDirectory();
    }

    private static string FindChangeLogPositionInRepo(Repository repo, string changeLogFileName)
    {
        return changeLogFileName[repo.Info.WorkingDirectory.Length..].Replace(oldChar: '\\', newChar: '/');
    }
}
