using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("{Releases.Length} releases")]
public sealed record ChangeLogDocument(
    ImmutableArray<string> HeaderLines,
    ChangeLogUnreleased? Unreleased,
    ImmutableArray<ChangeLogRelease> Releases);
