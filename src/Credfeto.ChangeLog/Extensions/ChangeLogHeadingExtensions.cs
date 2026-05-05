using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog.Extensions;

internal static class ChangeLogHeadingExtensions
{
    private const string CHANGE_TYPE_HEADING_PREFIX = "### ";
    private const string VERSION_HEADER_PREFIX = "## [";

    public static bool IsChangeTypeHeading(this string line)
    {
        return line.StartsWith(value: CHANGE_TYPE_HEADING_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    public static bool IsVersionHeader(this string line)
    {
        return line.StartsWith(value: VERSION_HEADER_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    public static string GetChangeTypeName(this string line)
    {
        return line[CHANGE_TYPE_HEADING_PREFIX.Length..];
    }

    public static string AsChangeTypeHeading(this string name)
    {
        return CHANGE_TYPE_HEADING_PREFIX + name;
    }

    public static string? GetVersionString(this string line)
    {
        int closeBracket = line.IndexOf(value: ']', startIndex: VERSION_HEADER_PREFIX.Length);

        if (closeBracket == -1)
        {
            return null;
        }

        return line[VERSION_HEADER_PREFIX.Length..closeBracket];
    }

    public static int FindUnreleasedEnd(this IReadOnlyList<string> lines, int unreleasedStart)
    {
        for (int i = unreleasedStart + 1; i < lines.Count; i++)
        {
            if (lines[i].IsVersionHeader() && !Unreleased.IsUnreleasedHeader(lines[i]))
            {
                return i;
            }
        }

        return lines.Count;
    }

    public static bool ContainsHtmlCommentStart(this string line)
    {
        return line.Contains(value: "<!--", comparisonType: StringComparison.Ordinal);
    }

    public static bool ContainsHtmlCommentEnd(this string line)
    {
        return line.Contains(value: "-->", comparisonType: StringComparison.Ordinal);
    }

    public static bool StartsWithHtmlComment(this string line)
    {
        return line.StartsWith(value: "<!--", comparisonType: StringComparison.Ordinal);
    }

    public static bool EqualsOrdinal(this string str, string other)
    {
        return StringComparer.Ordinal.Equals(x: str, y: other);
    }
}
