using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog.Services;

internal sealed partial class ChangeLogUpdater
{
    private static string AddEntryCommon(string changeLog, string type, string message)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        string entryText = CreateEntryText(message);
        int index = FindInsertPosition(changeLog: text, type: type, entryText: entryText);

        if (index != -1)
        {
            text.Insert(index: index, item: entryText);
        }

        return text.LinesToText();
    }

    private static string RemoveEntryCommon(string changeLog, string type, string message)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        string entryText = CreateEntryText(message);
        int index = FindRemovePosition(changeLog: text, type: type, entryText: entryText);

        while (index != -1)
        {
            text.RemoveAt(index: index);

            index = FindRemovePosition(changeLog: text, type: type, entryText: entryText);
        }

        return text.LinesToText();
    }

    private static string CreateEntryText(string message)
    {
        return "- " + message;
    }

    private static string BuildSubHeaderSection(string type)
    {
        return type.AsChangeTypeHeading();
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

                if (!line.EqualsOrdinal(search))
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
        return line.StartsWith('#') || line.StartsWithHtmlComment();
    }
}
