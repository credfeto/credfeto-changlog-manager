using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Helpers;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812: Avoid uninstantiated internal classes",
    Justification = "Registered in DI"
)]
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

    private static bool TryFindChangeLogInRepository(
        Repository repository,
        [NotNullWhen(true)] out string? changeLogFileName
    )
    {
        string repoRoot = repository.Info.WorkingDirectory;

        string rootChangelog = Path.Combine(repoRoot, FileConstants.ChangeLogFileName);

        if (File.Exists(rootChangelog))
        {
            changeLogFileName = rootChangelog;

            return true;
        }

        return TryFindSingleNestedChangeLog(repoRoot: repoRoot, changeLogFileName: out changeLogFileName);
    }

    private static bool TryFindSingleNestedChangeLog(string repoRoot, [NotNullWhen(true)] out string? changeLogFileName)
    {
        EnumerationOptions dirOptions = new() { IgnoreInaccessible = true };

        EnumerationOptions fileOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        string? found = null;

        foreach (string subDir in Directory.EnumerateDirectories(repoRoot, searchPattern: "*", dirOptions))
        {
            if (StringComparer.Ordinal.Equals(x: Path.GetFileName(subDir), y: ".git"))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(subDir, FileConstants.ChangeLogFileName, fileOptions))
            {
                if (found is not null)
                {
                    changeLogFileName = null;

                    return false;
                }

                found = file;
            }
        }

        if (found is not null)
        {
            changeLogFileName = found;

            return true;
        }

        changeLogFileName = null;

        return false;
    }
}
