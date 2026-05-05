using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Services;

internal interface IChangeLogLoader
{
    bool Exists(string changeLogFileName);

    ValueTask<string> LoadTextAsync(string changeLogFileName, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<string>> LoadLinesAsync(string changeLogFileName, CancellationToken cancellationToken);

    ValueTask SaveTextAsync(string changeLogFileName, string contents, CancellationToken cancellationToken);
}
