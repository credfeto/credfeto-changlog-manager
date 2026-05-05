using System;
using System.Collections.Generic;
using System.Text;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog;

public static class ChangeLogReader
{
    public static string ExtractReleaseNotes(string changeLog, string version)
    {
        Version? releaseVersion = BuildNumberHelpers.DetermineVersionForChangeLog(version);

        IReadOnlyList<string> text = RemoveComments(changeLog);

        FindSectionForBuild(text: text, version: releaseVersion, out int foundStart, out int foundEnd);

        if (foundStart == -1)
        {
            return string.Empty;
        }

        if (foundEnd == -1)
        {
            foundEnd = text.Count;
        }

        string previousLine = string.Empty;

        StringBuilder releaseNotes = new();

        for (int i = foundStart; i < foundEnd; i++)
        {
            if (string.IsNullOrEmpty(text[i]))
            {
                continue;
            }

            if (
                text[i].IsChangeTypeHeading()
                && previousLine.IsChangeTypeHeading()
            )
            {
                previousLine = text[i];

                continue;
            }

            if (text[i].IsChangeTypeHeading())
            {
                previousLine = text[i];

                continue;
            }

            if (previousLine.IsChangeTypeHeading())
            {
                releaseNotes = releaseNotes.AppendLine(previousLine);
            }

            releaseNotes = releaseNotes.AppendLine(text[i]);
            previousLine = text[i];
        }

        return releaseNotes.ToString().Trim();
    }

    private static IReadOnlyList<string> RemoveComments(string changeLog)
    {
        return CommonRegex.RemoveComments.Replace(input: changeLog, replacement: string.Empty).Trim().SplitToLines();
    }

    private static void FindSectionForBuild(
        IReadOnlyList<string> text,
        Version? version,
        out int foundStart,
        out int foundEnd
    )
    {
        foundStart = -1;
        foundEnd = -1;

        for (int i = 1; i < text.Count; i++)
        {
            string line = text[i];

            if (IsMatchingVersion(version: version, line: line))
            {
                foundStart = i + 1;

                continue;
            }

            if (foundStart != -1 && line.IsVersionHeader())
            {
                foundEnd = i;

                break;
            }
        }
    }

    private static bool IsMatchingVersion(Version? version, string line)
    {
        if (version is null)
        {
            return Unreleased.IsUnreleasedHeader(line);
        }

        return Candidates(version)
            .Any(candidate => line.StartsWith(value: candidate, comparisonType: StringComparison.OrdinalIgnoreCase));

        static IEnumerable<string> Candidates(Version expected)
        {
            int build = expected.Build is 0 or -1 ? 0 : expected.Build;

            yield return $"## [{expected.Major}.{expected.Minor}.{build}]";

            if (build == 0)
            {
                yield return $"## [{expected.Major}.{expected.Minor}]";
            }
        }
    }
}
