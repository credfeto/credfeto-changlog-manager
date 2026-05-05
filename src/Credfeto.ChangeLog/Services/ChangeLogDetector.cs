using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Helpers;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed class ChangeLogDetector : IChangeLogDetector
{
    public bool TryFindChangeLog([NotNullWhen(true)] out string? changeLogFileName)
    {
        try
        {
            using (Repository repository = GitRepository.OpenRepository(Environment.CurrentDirectory))
            {
                return TryFindChangeLogInRepository(repository: repository, changeLogFileName: out changeLogFileName);
            }
        }
        catch (Exception)
        {
            changeLogFileName = null;

            return false;
        }
    }

    private static bool TryFindChangeLogInRepository(Repository repository, [NotNullWhen(true)] out string? changeLogFileName)
    {
        string repoRoot = repository.Info.WorkingDirectory;

        IReadOnlyList<string> changelogs = Directory.GetFiles(
            path: repoRoot,
            searchPattern: FileConstants.ChangeLogFileName,
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
                string changeLogAtRepoRoot = Path.Combine(path1: repoRoot, path2: FileConstants.ChangeLogFileName);

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
