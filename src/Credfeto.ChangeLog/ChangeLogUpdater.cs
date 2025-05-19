using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Helpers;
using ZLinq;

namespace Credfeto.ChangeLog;

public static class ChangeLogUpdater
{
    public static async Task AddEntryAsync(
        string changeLogFileName,
        string type,
        string message,
        CancellationToken cancellationToken
    )
    {
        string textBlock = await ReadChangeLogAsync(
            changeLogFileName: changeLogFileName,
            cancellationToken: cancellationToken
        );

        string content = AddEntryCommon(changeLog: textBlock, type: type, message: message);

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    public static async Task RemoveEntryAsync(
        string changeLogFileName,
        string type,
        string message,
        CancellationToken cancellationToken
    )
    {
        string textBlock = await ReadChangeLogAsync(
            changeLogFileName: changeLogFileName,
            cancellationToken: cancellationToken
        );

        string content = RemoveEntryCommon(changeLog: textBlock, type: type, message: message);

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    private static async Task<string> ReadChangeLogAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        if (File.Exists(changeLogFileName))
        {
            return await File.ReadAllTextAsync(
                path: changeLogFileName,
                encoding: Encoding.UTF8,
                cancellationToken: cancellationToken
            );
        }

        await CreateEmptyAsync(changeLogFileName: changeLogFileName, cancellationToken: cancellationToken);

        return TemplateFile.Initial;
    }

    public static string AddEntry(string changeLog, string type, string message)
    {
        return AddEntryCommon(changeLog: changeLog, type: type, message: message);
    }

    private static string AddEntryCommon(string changeLog, string type, string message)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        string entryText = CreateEntryText(message);
        int index = FindInsertPosition(changeLog: text, type: type, entryText: entryText);

        if (index != -1)
        {
            text.Insert(index: index, item: entryText);
        }

        return string.Join(separator: Environment.NewLine, values: text).Trim();
    }

    private static List<string> ChangeLogAsLines(string changeLog)
    {
        return [.. EnsureChangelog(changeLog).SplitToLines()];
    }

    public static string RemoveEntry(string changeLog, string type, string message)
    {
        return RemoveEntryCommon(changeLog: changeLog, type: type, message: message);
    }

    private static string RemoveEntryCommon(string changeLog, string type, string message)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        string entryText = CreateEntryText(message);
        int index = FindRemovePosition(changeLog: text, type: type, entryText: entryText);

        while (index != -1)
        {
            text.RemoveAt(index: index);

            // check for another item to remove
            index = FindRemovePosition(changeLog: text, type: type, entryText: entryText);
        }

        return string.Join(separator: Environment.NewLine, values: text).Trim();
    }

    private static string CreateEntryText(string message)
    {
        return "- " + message;
    }

    private static int FindInsertPosition(List<string> changeLog, string type, string entryText)
    {
        return FindMatchPosition(
            changeLog: changeLog,
            type: type,
            isMatch: s => StringComparer.OrdinalIgnoreCase.Equals(x: s, y: entryText),
            exactMatchAction: _ => -1,
            emptySectionAction: line => line,
            findSection: true
        );
    }

    private static int FindRemovePosition(List<string> changeLog, string type, string entryText)
    {
        return FindMatchPosition(
            changeLog: changeLog,
            type: type,
            isMatch: s => s.StartsWith(value: entryText, comparisonType: StringComparison.Ordinal),
            exactMatchAction: line => line,
            emptySectionAction: _ => -1,
            findSection: false
        );
    }

    private static int FindMatchPosition(
        IReadOnlyList<string> changeLog,
        string type,
        Func<string, bool> isMatch,
        Func<int, int> exactMatchAction,
        Func<int, int> emptySectionAction,
        bool findSection
    )
    {
        bool foundUnreleased = false;

        string search = BuildSubHeaderSection(type);

        for (int index = 0; index < changeLog.Count; index++)
        {
            string line = changeLog[index];

            if (!foundUnreleased)
            {
                if (Unreleased.IsUnreleasedHeader(line))
                {
                    foundUnreleased = true;
                }
            }
            else
            {
                if (IsRelease(line))
                {
                    return Throws.CouldNotFindTypeHeading(type);
                }

                if (!StringComparer.Ordinal.Equals(x: line, y: search))
                {
                    continue;
                }

                return FindNext(
                    changeLog: changeLog,
                    isMatch: isMatch,
                    exactMatchAction: exactMatchAction,
                    emptySectionAction: emptySectionAction,
                    findSection: findSection,
                    index: index
                );
            }
        }

        return Throws.CouldNotFindUnreleasedSectionInt();
    }

    private static int FindNext(
        IReadOnlyList<string> changeLog,
        Func<string, bool> isMatch,
        Func<int, int> exactMatchAction,
        Func<int, int> emptySectionAction,
        bool findSection,
        int index
    )
    {
        int next = index + 1;

        while (next < changeLog.Count)
        {
            if (isMatch(changeLog[next]))
            {
                // Found matching text
                return exactMatchAction(next);
            }

            if (IsNextItem(changeLog[next]))
            {
                if (findSection)
                {
                    return FindPreviousNonBlankEntry(changeLog: changeLog, earliest: index, latest: next);
                }

                return -1;
            }

            ++next;
        }

        return emptySectionAction(index + 1);
    }

    private static string BuildSubHeaderSection(string type)
    {
        return "### " + type;
    }

    public static async Task CreateReleaseAsync(
        string changeLogFileName,
        string version,
        bool pending,
        CancellationToken cancellationToken
    )
    {
        string originalChangeLog = await File.ReadAllTextAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );

        string newChangeLog = CreateReleaseCommon(changeLog: originalChangeLog, version: version, pending: pending);

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: newChangeLog,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    public static string CreateRelease(string changeLog, string version, bool pending)
    {
        return CreateReleaseCommon(changeLog: changeLog, version: version, pending: pending);
    }

    private static string CreateReleaseCommon(string changeLog, string version, bool pending)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        Dictionary<string, int> releases = FindReleasePositions(text);

        if (!releases.TryGetValue(key: Constants.Unreleased, out int unreleasedIndex))
        {
            return Throws.EmptyChangeLogNoUnreleasedSection();
        }

        int releaseInsertPos = FindInsertPosition(
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

        return string.Join(separator: Environment.NewLine, values: text).Trim();
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
        if (IsSubHeading(previousLine))
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
        if (IsSubHeading(text[i]))
        {
            previousLine = text[i];

            return true;
        }

        return false;
    }

    private static bool SkipEmptyHeadingSections(List<string> text, int i, ref string previousLine)
    {
        if (IsSubHeading(text[i]) && IsSubHeading(previousLine))
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
        if (ContainsHtmlCommentStart(text[i]) && !ContainsHtmlCommentEnd(text[i]))
        {
            if (string.IsNullOrWhiteSpace(text[i - 1]))
            {
                // if line before was blank then don't delete it
                removeIndexes.Remove(i - 1);
            }

            inComment = true;

            return true;
        }

        if (inComment)
        {
            if (ContainsHtmlCommentEnd(text[i]))
            {
                inComment = false;
            }

            return true;
        }

        return false;
    }

    private static bool ContainsHtmlCommentStart(string line)
    {
        return line.Contains(value: "<!--", comparisonType: StringComparison.Ordinal);
    }

    private static bool ContainsHtmlCommentEnd(string line)
    {
        return line.Contains(value: "-->", comparisonType: StringComparison.Ordinal);
    }

    private static bool IsSubHeading(string line)
    {
        return line.StartsWith(value: "### ", comparisonType: StringComparison.Ordinal);
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

    private static int FindInsertPosition(
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
            .Keys.Where(x => !StringComparer.Ordinal.Equals(x: x, y: Constants.Unreleased))
            .OrderByDescending(x => new Version(x))
            .FirstOrDefault();
    }

    private static Dictionary<string, int> FindReleasePositions(IReadOnlyList<string> text)
    {
        Dictionary<string, int> releases = GetReleasePositions(text);

        if (releases.Count == 0)
        {
            return Throws.CouldNotFindUnreleasedSectionDictioonary();
        }

        return releases;
    }

    private static Dictionary<string, int> GetReleasePositions(IReadOnlyList<string> text)
    {
        return text.Select((line, index) => new { line, index })
            .Where(i => IsRelease(i.line))
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
            return Constants.Unreleased;
        }

        Match match = CommonRegex.VersionHeader.Match(line);

        return match.Groups["version"].Value;
    }

    private static bool IsRelease(string line)
    {
        return Unreleased.IsUnreleasedHeader(line) || CommonRegex.VersionHeader.IsMatch(line);
    }

    private static int FindPreviousNonBlankEntry(IReadOnlyList<string> changeLog, int earliest, int latest)
    {
        int previous = latest - 1;

        while (previous > earliest && string.IsNullOrWhiteSpace(changeLog[previous]))
        {
            --previous;

            if (!string.IsNullOrWhiteSpace(changeLog[previous]))
            {
                return previous + 1;
            }
        }

        return latest;
    }

    private static bool IsNextItem(string line)
    {
        return line.StartsWith('#') || line.StartsWith(value: "<!--", comparisonType: StringComparison.Ordinal);
    }

    private static string EnsureChangelog(string changeLog)
    {
        if (string.IsNullOrWhiteSpace(changeLog))
        {
            return TemplateFile.Initial;
        }

        return changeLog;
    }

    public static Task CreateEmptyAsync(string changeLogFileName, in CancellationToken cancellationToken)
    {
        return File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: TemplateFile.Initial,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }
}
