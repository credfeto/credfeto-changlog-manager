using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

internal sealed partial class ChangeLogLinter
{
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

            int nextNonEmpty = i + 2;

            while (nextNonEmpty < lines.Length && string.IsNullOrWhiteSpace(lines[nextNonEmpty]))
            {
                nextNonEmpty++;
            }

            if (nextNonEmpty >= lines.Length)
            {
                continue;
            }

            if (lines[nextNonEmpty].IsChangeTypeHeading() || lines[nextNonEmpty].IsVersionHeader())
            {
                continue;
            }

            string name = lines[i].GetChangeTypeName();
            errors.Add(
                new(
                    LineNumber: i + 1,
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

            int lineNumber = i + 1;

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

        return parsed.Build >= 0;
    }
}
