using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class ChangeLogTestHelper
{
    internal static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        return new ChangeLogParser().ParseAsync(content, default);
    }
}
