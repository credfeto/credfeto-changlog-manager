using LibGit2Sharp;

namespace Credfeto.ChangeLog.Services;

internal static class CompareSettings
{
    public static CompareOptions BuildCompareOptions { get; } =
        new()
        {
            ContextLines = 0,
            InterhunkLines = 0,
            IncludeUnmodified = false,
        };
}
