using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Exceptions;
using LibGit2Sharp;

namespace Credfeto.ChangeLog.Helpers;

internal static class Throws
{
    [DoesNotReturn]
    public static string EmptyChangeLogNoUnreleasedSection()
    {
        throw new EmptyChangeLogException("Could not find [" + Constants.Unreleased + "] section of file");
    }

    [DoesNotReturn]
    public static Dictionary<string, int> CouldNotFindUnreleasedSectionDictioonary()
    {
        throw new EmptyChangeLogException("Could not find [" + Constants.Unreleased + "] section of file");
    }

    [DoesNotReturn]
    public static int CouldNotFindUnreleasedSectionInt()
    {
        throw new InvalidChangeLogException("Could not find [" + Constants.Unreleased + "] section of file");
    }

    [DoesNotReturn]
    public static string CouldNotFindUnreleasedSectionString()
    {
        throw new InvalidChangeLogException("Could not find [" + Constants.Unreleased + "] section of file");
    }

    [DoesNotReturn]
    public static List<string> NoChangesForTheRelease()
    {
        throw new EmptyChangeLogException("No changes for the release");
    }

    [DoesNotReturn]
    public static int CouldNotFindTypeHeading(string type)
    {
        throw new InvalidChangeLogException($"Could not find {type} heading");
    }

    [DoesNotReturn]
    public static int ReleaseTooOld(string releaseVersionToFind, string latestRelease)
    {
        throw new ReleaseTooOldException(
            $"Release {latestRelease} already exists and is newer than {releaseVersionToFind}"
        );
    }

    [DoesNotReturn]
    public static int ReleaseAlreadyExists(string releaseVersionToFind)
    {
        throw new ReleaseAlreadyExistsException($"Release {releaseVersionToFind} already exists");
    }

    [DoesNotReturn]
    public static Branch CouldNotFindBranch(string originBranchName)
    {
        throw new BranchMissingException($"Could not find branch {originBranchName}");
    }

    [DoesNotReturn]
    public static (List<string> before, List<string> after) CouldNotProcessDiffLine(string line)
    {
        throw new DiffException($"Could not process diff line: {line}");
    }

    [DoesNotReturn]
    public static string CouldNotFindChangeLog(string changeLogFileName)
    {
        throw new InvalidChangeLogException($"Could not find {changeLogFileName}");
    }
}
