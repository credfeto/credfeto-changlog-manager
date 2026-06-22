using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public interface IChangeLogFixer
{
    ValueTask FixAsync(string changeLogFileName, ChangeLogLanguage language, CancellationToken cancellationToken);
}
