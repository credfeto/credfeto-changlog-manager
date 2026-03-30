using System.Collections.Generic;

namespace Credfeto.ChangeLog.Helpers;

public static class ChangeLogSections
{
    public static readonly IReadOnlyList<string> Order =
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
