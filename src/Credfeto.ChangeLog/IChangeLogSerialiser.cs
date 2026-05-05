using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogSerialiser
{
    ValueTask<string> SerialiseAsync(ChangeLogDocument document, CancellationToken cancellationToken);
}
