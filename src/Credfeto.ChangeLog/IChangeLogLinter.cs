using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogLinter
{
    ValueTask<IReadOnlyList<LintError>> LintAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );
}
