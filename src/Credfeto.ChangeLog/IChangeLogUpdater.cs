using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public interface IChangeLogUpdater
{
    ValueTask CreateEmptyAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );

    Task AddEntryAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string type,
        string message,
        CancellationToken cancellationToken
    );

    Task RemoveEntryAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string type,
        string message,
        CancellationToken cancellationToken
    );

    Task CreateReleaseAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string version,
        bool pending,
        CancellationToken cancellationToken
    );

    ValueTask EnsureUnreleasedSectionsAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );
}
