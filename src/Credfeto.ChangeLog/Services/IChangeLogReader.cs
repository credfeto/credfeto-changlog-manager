using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Services;

public interface IChangeLogReader
{
    ValueTask<string> ExtractReleaseNotesFromFileAsync(string changeLogFileName, string version, CancellationToken cancellationToken);

    ValueTask<int?> FindFirstReleaseVersionPositionAsync(string changeLogFileName, CancellationToken cancellationToken);
}
