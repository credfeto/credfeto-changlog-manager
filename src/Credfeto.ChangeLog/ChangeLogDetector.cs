using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Credfeto.ChangeLog.Helpers;
using LibGit2Sharp;

namespace Credfeto.ChangeLog;

public static class ChangeLogDetector
{
    public static bool TryFindChangeLog([NotNullWhen(true)] out string? changeLogFileName)
    {
        try
        {
            using (Repository repository = GitRepository.OpenRepository(Environment.CurrentDirectory))
            {
                return TryFindChangeLog(repository: repository, changeLogFileName: out changeLogFileName);
            }
        }
        catch (Exception)
        {
            // Couldn't
            changeLogFileName = null;

            return false;
        }
    }

    public static bool TryFindChangeLog(Repository repository, [NotNullWhen(true)] out string? changeLogFileName)
    {
        string repoRoot = repository.Info.WorkingDirectory;

        IReadOnlyList<string> changelogs = Directory.GetFiles(
            path: repoRoot,
            searchPattern: Constants.ChangeLogFileName,
            searchOption: SearchOption.AllDirectories
        );

        switch (changelogs.Count)
        {
            case 0:
                changeLogFileName = null;

                return false;

            case 1:
                changeLogFileName = changelogs[0];

                return true;

            default:
            {
                string changeLogAtRepoRoot = Path.Combine(path1: repoRoot, path2: Constants.ChangeLogFileName);

                if (changelogs.Contains(value: changeLogAtRepoRoot, comparer: StringComparer.Ordinal))
                {
                    changeLogFileName = changeLogAtRepoRoot;

                    return true;
                }

                changeLogFileName = null;

                return false;
            }
        }
    }
}
