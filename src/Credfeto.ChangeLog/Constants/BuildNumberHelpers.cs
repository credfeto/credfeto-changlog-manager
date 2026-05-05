using System;

namespace Credfeto.ChangeLog.Constants;

internal static class BuildNumberHelpers
{
    public static Version? DetermineVersionForChangeLog(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        if (version.Contains(value: '-', comparisonType: StringComparison.Ordinal))
        {
            return null;
        }

        if (!Version.TryParse(input: version, out Version? parsedVersion))
        {
            return null;
        }

        return parsedVersion;
    }
}
