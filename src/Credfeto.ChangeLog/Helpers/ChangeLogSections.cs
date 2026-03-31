using System.Collections.Immutable;

namespace Credfeto.ChangeLog.Helpers;

public static class ChangeLogSections
{
    public static readonly ImmutableArray<string> Order =
    [
        "Security",
        "Added",
        "Fixed",
        "Changed",
        "Deprecated",
        "Removed",
        "Deployment Changes",
    ];
}
