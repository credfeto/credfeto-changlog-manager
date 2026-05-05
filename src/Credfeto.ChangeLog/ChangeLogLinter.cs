using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public static class ChangeLogLinter
{
    private static readonly string[] RequiredSections =
    [
        "Security",
        "Added",
        "Fixed",
        "Changed",
        "Removed"
    ];

    private static readonly string[] KnownOptionalSections = ["Deprecated", "Deployment Changes"];

    public static IReadOnlyList<LintError> Lint(string content, IReadOnlyCollection<string>? additionalSections = null)
    {
        List<LintError> errors = [];

        string[] lines = NormalizeLines(content);

        CheckUnreleasedSectionPresent(lines: lines, errors: errors, additionalSections: additionalSections);

        CheckBlankLineAfterHeadings(lines: lines, errors: errors);

        CheckVersionHeaders(lines: lines, errors: errors);

        return errors;
    }

    private static string[] NormalizeLines(string content)
    {
        return [.. content.SplitToLines()];
    }

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
        int unreleasedEnd = FindUnreleasedEnd(lines: lines, unreleasedStart: unreleasedLineIndex);

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
        return RequiredSections.Any(required => StringComparer.Ordinal.Equals(x: required, y: name));
    }

    private static int FindInList(List<(string Name, int LineNumber)> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (StringComparer.Ordinal.Equals(x: list[i].Name, y: name))
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

        if (KnownOptionalSections.Any(optional => StringComparer.Ordinal.Equals(x: optional, y: name)))
        {
            return true;
        }

        return additionalSections?.Any(additional => StringComparer.Ordinal.Equals(x: additional, y: name)) == true;
    }

    private static List<(string Name, int LineNumber)> CollectSubHeadings(string[] lines, int start, int end)
    {
        List<(string Name, int LineNumber)> result = [];

        for (int i = start; i < end; i++)
        {
            if (lines[i].IsChangeTypeHeading())
            {
                string name = lines[i].GetChangeTypeName();
                result.Add((name, i + 1)); // 1-based line number
            }
        }

        return result;
    }

    private static int FindUnreleasedEnd(string[] lines, int unreleasedStart)
    {
        for (int i = unreleasedStart + 1; i < lines.Length; i++)
        {
            if (
                lines[i].IsVersionHeader()
                && !Unreleased.IsUnreleasedHeader(lines[i])
            )
            {
                return i;
            }
        }

        return lines.Length;
    }

    private static void CheckBlankLineAfterHeadings(string[] lines, List<LintError> errors)
    {
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (!lines[i].IsChangeTypeHeading())
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lines[i + 1]))
            {
                continue;
            }

            // Find the next non-empty line after the blank
            int nextNonEmpty = i + 2;

            while (nextNonEmpty < lines.Length && string.IsNullOrWhiteSpace(lines[nextNonEmpty]))
            {
                nextNonEmpty++;
            }

            if (nextNonEmpty >= lines.Length)
            {
                continue;
            }

            // A blank line between a ### heading and another section (### or ##) is acceptable formatting
            if (lines[nextNonEmpty].IsChangeTypeHeading() || lines[nextNonEmpty].IsVersionHeader())
            {
                continue;
            }

            string name = lines[i].GetChangeTypeName();
            errors.Add(
                new(
                    LineNumber: i + 1, // 1-based
                    Message: $"Blank line after heading '### {name}'"
                )
            );
        }
    }

    private static void CheckVersionHeaders(string[] lines, List<LintError> errors)
    {
        List<(Version ParsedVersion, int LineNumber, string OriginalVersion)> versions = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (
                !line.IsVersionHeader()
                || Unreleased.IsUnreleasedHeader(line)
            )
            {
                continue;
            }

            string? versionStr = line.GetVersionString();

            if (versionStr is null)
            {
                continue;
            }
            int lineNumber = i + 1; // 1-based

            if (!IsValidVersion(versionStr))
            {
                errors.Add(
                    new(
                        LineNumber: lineNumber,
                        Message: $"Invalid version '{versionStr}' — must be a valid semver version"
                    )
                );

                continue;
            }

            if (Version.TryParse(input: versionStr, result: out Version? parsed))
            {
                versions.Add((parsed, lineNumber, versionStr));
            }
        }

        CheckVersionsDescendingOrder(versions: versions, errors: errors);
    }

    private static void CheckVersionsDescendingOrder(
        List<(Version ParsedVersion, int LineNumber, string OriginalVersion)> versions,
        List<LintError> errors
    )
    {
        for (int i = 1; i < versions.Count; i++)
        {
            if (versions[i].ParsedVersion >= versions[i - 1].ParsedVersion)
            {
                errors.Add(
                    new(
                        LineNumber: versions[i].LineNumber,
                        Message: $"Version '{versions[i].OriginalVersion}' is not in descending order"
                    )
                );
            }
        }
    }

    private static bool IsValidVersion(string versionStr)
    {
        if (!Version.TryParse(input: versionStr, result: out Version? parsed))
        {
            return false;
        }

        // Require at least 3 parts (Major.Minor.Build)
        return parsed.Build >= 0;
    }
}
