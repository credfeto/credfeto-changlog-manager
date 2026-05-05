using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

internal sealed partial class ChangeLogUpdater
{
    private static string CreateReleaseCommon(string changeLog, string version, bool pending)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        Dictionary<string, int> releases = FindReleasePositions(text);

        if (!releases.TryGetValue(key: FileConstants.Unreleased, out int unreleasedIndex))
        {
            return Throws.EmptyChangeLogNoUnreleasedSection();
        }

        int releaseInsertPos = FindVersionInsertPosition(
            releaseVersionToFind: version,
            releases: releases,
            endOfFilePosition: text.Count
        );

        MoveUnreleasedToRelease(
            version: version,
            unreleasedIndex: unreleasedIndex,
            releaseInsertPos: releaseInsertPos,
            text: text,
            pending: pending
        );

        return text.LinesToText();
    }

    private static void MoveUnreleasedToRelease(
        string version,
        int unreleasedIndex,
        int releaseInsertPos,
        List<string> text,
        bool pending
    )
    {
        List<string> newRelease = GenerateNewReleaseContents(
            unreleasedIndex: unreleasedIndex,
            releaseInsertPos: releaseInsertPos,
            text: text,
            out List<int> removeIndexes
        );

        string releaseVersionHeader = CreateReleaseVersionHeader(version: version, pending: pending);

        PrependReleaseVersionHeader(newRelease: newRelease, releaseVersionHeader: releaseVersionHeader);

        text.InsertRange(index: releaseInsertPos, collection: newRelease);

        RemoveItems(text: text, removeIndexes: removeIndexes);
    }

    private static List<string> GenerateNewReleaseContents(
        int unreleasedIndex,
        int releaseInsertPos,
        List<string> text,
        out List<int> removeIndexes
    )
    {
        string previousLine = string.Empty;

        List<string> newRelease = [];

        removeIndexes = [];

        bool inComment = false;

        for (int i = unreleasedIndex + 1; i < releaseInsertPos; i++)
        {
            if (
                SkipComments(text: text, i: i, removeIndexes: removeIndexes, inComment: ref inComment)
                || SkipEmptyLine(text: text, i: i, removeIndexes: removeIndexes)
                || SkipEmptyHeadingSections(text: text, i: i, previousLine: ref previousLine)
                || SkipHeadingLine(text: text, i: i, previousLine: ref previousLine)
            )
            {
                continue;
            }

            previousLine = AddLineToRelease(
                text: text,
                previousLine: previousLine,
                newRelease: newRelease,
                removeIndexes: removeIndexes,
                i: i
            );
        }

        if (newRelease.Count == 0)
        {
            return Throws.NoChangesForTheRelease();
        }

        return newRelease;
    }

    private static void PrependReleaseVersionHeader(List<string> newRelease, string releaseVersionHeader)
    {
        newRelease.Insert(index: 0, item: releaseVersionHeader);
        newRelease.Add(string.Empty);
    }

    private static void RemoveItems(List<string> text, List<int> removeIndexes)
    {
        foreach (int item in removeIndexes.OrderDescending())
        {
            text.RemoveAt(item);
        }
    }

    private static string CreateReleaseVersionHeader(string version, bool pending)
    {
        string releaseDate = CreateReleaseDate(pending);
        string releaseVersionHeader = string.Concat(str0: "## [", str1: version, str2: "] - ", str3: releaseDate);

        return releaseVersionHeader;
    }

    private static string CreateReleaseDate(bool pending)
    {
        return pending ? "TBD" : CurrentDate();
    }

    private static string AddLineToRelease(
        List<string> text,
        string previousLine,
        List<string> newRelease,
        List<int> removeIndexes,
        int i
    )
    {
        if (previousLine.IsChangeTypeHeading())
        {
            newRelease.Add(previousLine);
        }

        removeIndexes.Add(i);
        newRelease.Add(text[i]);
        previousLine = text[i];

        return previousLine;
    }

    private static bool SkipHeadingLine(List<string> text, int i, ref string previousLine)
    {
        if (text[i].IsChangeTypeHeading())
        {
            previousLine = text[i];

            return true;
        }

        return false;
    }

    private static bool SkipEmptyHeadingSections(List<string> text, int i, ref string previousLine)
    {
        if (text[i].IsChangeTypeHeading() && previousLine.IsChangeTypeHeading())
        {
            previousLine = text[i];

            return true;
        }

        return false;
    }

    private static bool SkipEmptyLine(List<string> text, int i, List<int> removeIndexes)
    {
        if (string.IsNullOrEmpty(text[i]))
        {
            removeIndexes.Add(i);

            return true;
        }

        return false;
    }

    private static bool SkipComments(List<string> text, int i, List<int> removeIndexes, ref bool inComment)
    {
        if (text[i].ContainsHtmlCommentStart() && !text[i].ContainsHtmlCommentEnd())
        {
            if (string.IsNullOrWhiteSpace(text[i - 1]))
            {
                removeIndexes.Remove(i - 1);
            }

            inComment = true;

            return true;
        }

        if (inComment)
        {
            if (text[i].ContainsHtmlCommentEnd())
            {
                inComment = false;
            }

            return true;
        }

        return false;
    }

    [SuppressMessage(
        category: "FunFair.CodeAnalysis",
        checkId: "FFS0001",
        Justification = "Should always use the local time."
    )]
    private static string CurrentDate()
    {
        return DateTime.Now.ToString(format: "yyyy-MM-dd", provider: CultureInfo.InvariantCulture);
    }

    private static int FindVersionInsertPosition(
        string releaseVersionToFind,
        IReadOnlyDictionary<string, int> releases,
        int endOfFilePosition
    )
    {
        string? latestRelease = GetLatestRelease(releases);

        int releaseInsertPos;

        if (latestRelease is not null)
        {
            Console.WriteLine($"Latest release: {latestRelease}");

            Version numericalVersion = new(releaseVersionToFind);
            Version latestNumeric = new(latestRelease);

            if (latestNumeric == numericalVersion)
            {
                return Throws.ReleaseAlreadyExists(releaseVersionToFind);
            }

            if (latestNumeric > numericalVersion)
            {
                return Throws.ReleaseTooOld(releaseVersionToFind: releaseVersionToFind, latestRelease: latestRelease);
            }

            releaseInsertPos = releases[latestRelease];
        }
        else
        {
            releaseInsertPos = endOfFilePosition;
        }

        return releaseInsertPos;
    }

    private static string? GetLatestRelease(IReadOnlyDictionary<string, int> releases)
    {
        return releases
            .Keys.Where(x => !x.EqualsOrdinal(FileConstants.Unreleased))
            .OrderByDescending(x => new Version(x))
            .FirstOrDefault();
    }

    private static Dictionary<string, int> FindReleasePositions(IReadOnlyList<string> text)
    {
        Dictionary<string, int> releases = GetReleasePositions(text);

        if (releases.Count == 0)
        {
            return Throws.CouldNotFindUnreleasedSectionDictionary();
        }

        return releases;
    }

    private static Dictionary<string, int> GetReleasePositions(IReadOnlyList<string> text)
    {
        return text.Select((line, index) => new { line, index })
            .Where(i => i.line.IsVersionHeader())
            .ToDictionary(
                keySelector: i => ExtractRelease(i.line),
                elementSelector: i => i.index,
                comparer: StringComparer.Ordinal
            );
    }

    private static string ExtractRelease(string line)
    {
        if (Unreleased.IsUnreleasedHeader(line))
        {
            return FileConstants.Unreleased;
        }

        Match match = CommonRegex.VersionHeader.Match(line);

        return match.Groups["version"].Value;
    }
}
