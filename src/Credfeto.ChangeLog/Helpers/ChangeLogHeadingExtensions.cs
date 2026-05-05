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
}
