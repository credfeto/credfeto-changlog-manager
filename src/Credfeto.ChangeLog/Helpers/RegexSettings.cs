using System.Text.RegularExpressions;

namespace Credfeto.ChangeLog.Helpers;

internal static class RegexSettings
{
    public const short TimeoutMilliseconds = 1000;

    public const RegexOptions RemoveCommentsOptions =
        RegexOptions.Compiled | RegexOptions.Multiline;

    public const string RemoveCommentsRegex = "<!--[\\s\\S]*?-->";

    public const RegexOptions VersionHeaderOptions =
        RegexOptions.Compiled | RegexOptions.ExplicitCapture;

    public const string VersionHeaderRegex = @"^##\s\[(?<version>\d+(.\d+)+)\]";

    public const RegexOptions GitHunkPositionOptions =
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture;

    public const string GitHunkPositionRegex =
        @"^@@\s*\-(?<OriginalFileStart>\d*)(,(?<OriginalFileEnd>\d*))?\s*\+(?<CurrentFileStart>\d*)(,(?<CurrentFileChangeLength>\d*))?\s*@@";
}
