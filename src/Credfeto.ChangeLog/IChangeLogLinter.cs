using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogLinter
{
    ValueTask<IReadOnlyList<LintError>> LintFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken);
}
