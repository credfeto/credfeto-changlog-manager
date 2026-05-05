using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Services;

public interface IChangeLogFixer
{
    ValueTask FixFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken);
}
