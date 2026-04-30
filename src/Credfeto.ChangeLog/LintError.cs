using System.Diagnostics;

namespace Credfeto.ChangeLog;

[DebuggerDisplay("Line {LineNumber}: {Message}")]
public sealed record LintError(int LineNumber, string Message);
