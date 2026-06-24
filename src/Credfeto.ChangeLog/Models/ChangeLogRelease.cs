using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("[{Version}] - {Date}{IsYanked ? \" [YANKED]\" : string.Empty,nq} (line {LineNumber})")]
public sealed record ChangeLogRelease(
    string Version,
    string Date,
    int LineNumber,
    ImmutableArray<ChangeLogSection> Sections,
    bool IsYanked = false
);
