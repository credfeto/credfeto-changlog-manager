using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("Unreleased (line {LineNumber}): {Sections.Length} sections")]
public sealed record ChangeLogUnreleased(int LineNumber, ImmutableArray<ChangeLogSection> Sections, ImmutableArray<string> TrailingLines);
