using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class ChangeLogTestHelper
{
    [SuppressMessage(
        category: "Microsoft.VisualStudio.Threading.Analyzers",
        checkId: "VSTHRD002",
        Justification = "Helpers synchronously wrap pure parse ValueTasks"
    )]
    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0045:Use async overload",
        Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
    )]
    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2012:UseValueTasksCorrectly",
        Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
    )]
    public static ChangeLogDocument Parse(string content)
    {
        return new ChangeLogParser().ParseAsync(content, default).GetAwaiter().GetResult();
    }
}
