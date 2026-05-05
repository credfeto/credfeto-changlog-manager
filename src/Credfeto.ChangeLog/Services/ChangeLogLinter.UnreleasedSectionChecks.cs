using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

internal sealed partial class ChangeLogLinter
{
    private static void CheckUnreleasedSectionPresent(
        string[] lines,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections
    )
    {
        int unreleasedLineIndex = FindUnreleasedLine(lines);

        if (unreleasedLineIndex == -1)
        {
            errors.Add(new(LineNumber: 1, Message: "Missing [Unreleased] section"));

            return;
        }

        CheckRequiredSectionsInUnreleased(
            lines: lines,
            unreleasedLineIndex: unreleasedLineIndex,
            errors: errors,
            additionalSections: additionalSections
        );
    }

    private static int FindUnreleasedLine(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (Unreleased.IsUnreleasedHeader(lines[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static void CheckRequiredSectionsInUnreleased(
        string[] lines,
        int unreleasedLineIndex,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections
    )
    {
        int unreleasedEnd = lines.FindUnreleasedEnd(unreleasedLineIndex);

        List<(string Name, int LineNumber)> foundHeadings = CollectSubHeadings(
            lines: lines,
            start: unreleasedLineIndex + 1,
            end: unreleasedEnd
        );

        CheckDuplicateSections(foundHeadings: foundHeadings, errors: errors);

        CheckUnknownSections(foundHeadings: foundHeadings, errors: errors, additionalSections: additionalSections);

        CheckRequiredSectionOrderAndPresence(
            foundHeadings: foundHeadings,
            unreleasedLineNumber: unreleasedLineIndex + 1,
            errors: errors
        );
    }

    private static void CheckDuplicateSections(
        List<(string Name, int LineNumber)> foundHeadings,
        List<LintError> errors
    )
    {
        HashSet<string> seenSections = new(StringComparer.Ordinal);

        foreach ((string name, int lineNumber) in foundHeadings)
        {
            if (!seenSections.Add(name))
            {
                errors.Add(
                    new(
                        LineNumber: lineNumber,
                        Message: $"Section '### {name}' is duplicated in [Unreleased]"
                    )
                );
            }
        }
    }

    private static void CheckUnknownSections(
        List<(string Name, int LineNumber)> foundHeadings,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections
    )
    {
        foreach ((string name, int lineNumber) in foundHeadings)
        {
            if (!IsKnownSection(name: name, additionalSections: additionalSections))
            {
                errors.Add(
                    item: new(
                        LineNumber: lineNumber,
                        Message: $"Unknown section '### {name}' in [Unreleased]"
                    )
                );
            }
        }
    }

    private static void CheckRequiredSectionOrderAndPresence(
        List<(string Name, int LineNumber)> foundHeadings,
        int unreleasedLineNumber,
        List<LintError> errors
    )
    {
        List<(string Name, int LineNumber)> foundRequired = GetFirstOccurrencesOfRequired(foundHeadings);
        int lastFoundIndex = -1;

        foreach (string requiredSection in RequiredSections)
        {
            int foundPos = FindInList(list: foundRequired, name: requiredSection);

            if (foundPos == -1)
            {
                errors.Add(
                    new(
                        LineNumber: unreleasedLineNumber,
                        Message: $"Missing required section '### {requiredSection}' in [Unreleased]"
                    )
                );
            }
            else if (foundPos < lastFoundIndex)
            {
                errors.Add(
                    new(
                        LineNumber: foundRequired[foundPos].LineNumber,
                        Message: $"Section '### {requiredSection}' is out of order in [Unreleased]"
                    )
                );
            }
            else
            {
                lastFoundIndex = foundPos;
            }
        }
    }

    private static List<(string Name, int LineNumber)> GetFirstOccurrencesOfRequired(
        List<(string Name, int LineNumber)> foundHeadings
    )
    {
        List<(string Name, int LineNumber)> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach ((string name, int lineNumber) in foundHeadings)
        {
            if (IsRequiredSection(name) && seen.Add(name))
            {
                result.Add((name, lineNumber));
            }
        }

        return result;
    }

    private static bool IsRequiredSection(string name)
    {
        return RequiredSections.Any(required => required.EqualsOrdinal(name));
    }

    private static int FindInList(List<(string Name, int LineNumber)> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Name.EqualsOrdinal(name))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsKnownSection(string name, IReadOnlyCollection<string>? additionalSections)
    {
        if (IsRequiredSection(name))
        {
            return true;
        }

        if (KnownOptionalSections.Any(optional => optional.EqualsOrdinal(name)))
        {
            return true;
        }

        return additionalSections?.Any(additional => additional.EqualsOrdinal(name)) == true;
    }

    private static List<(string Name, int LineNumber)> CollectSubHeadings(string[] lines, int start, int end)
    {
        List<(string Name, int LineNumber)> result = [];

        for (int i = start; i < end; i++)
        {
            if (lines[i].IsChangeTypeHeading())
            {
                string name = lines[i].GetChangeTypeName();
                result.Add((name, i + 1));
            }
        }

        return result;
    }
}
