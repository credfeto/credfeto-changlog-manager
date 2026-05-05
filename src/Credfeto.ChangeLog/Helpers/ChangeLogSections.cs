using System;
using System.Collections.Frozen;
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

    public static readonly ImmutableArray<string> Headings =
    [
        "### Security",
        "### Added",
        "### Fixed",
        "### Changed",
        "### Deprecated",
        "### Removed",
        "### Deployment Changes",
    ];

    public static readonly FrozenSet<string> KnownSections = Order.ToFrozenSet(StringComparer.Ordinal);
}
