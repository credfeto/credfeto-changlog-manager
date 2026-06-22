using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogStorage
{
    ValueTask<ChangeLogDocument> LoadAsync(string changeLogFileName, CancellationToken cancellationToken);

    ValueTask SaveAsync(string changeLogFileName, ChangeLogDocument document, CancellationToken cancellationToken);
}
