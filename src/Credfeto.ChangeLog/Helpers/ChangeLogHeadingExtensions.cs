using System;

namespace Credfeto.ChangeLog.Helpers;

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
}
