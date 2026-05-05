using System.Collections.Immutable;
using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("{Name}: {Entries.Length} entries (line {LineNumber})")]
public sealed record ChangeLogSection(string Name, int LineNumber, ImmutableArray<string> Entries);
