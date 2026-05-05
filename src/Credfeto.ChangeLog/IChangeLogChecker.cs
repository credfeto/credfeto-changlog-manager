using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public interface IChangeLogChecker
{
    Task<bool> ChangeLogModifiedInReleaseSectionAsync(string changeLogFileName, string originBranchName, CancellationToken cancellationToken);
}
