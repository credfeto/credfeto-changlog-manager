using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public interface IChangeLogUpdater
{
    Task AddEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken);

    Task RemoveEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken);

    Task CreateReleaseAsync(string changeLogFileName, string version, bool pending, CancellationToken cancellationToken);

    ValueTask EnsureUnreleasedSectionsAsync(string changeLogFileName, CancellationToken cancellationToken);

    ValueTask CreateEmptyAsync(string changeLogFileName, CancellationToken cancellationToken);
}
