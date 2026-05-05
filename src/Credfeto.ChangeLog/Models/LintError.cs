using System.Diagnostics;

namespace Credfeto.ChangeLog.Models;

[DebuggerDisplay("Line {LineNumber}: {Message}")]
public sealed record LintError(int LineNumber, string Message);
