using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogLinterService : IChangeLogLinter
{
    private readonly IChangeLogLoader _loader;

    public ChangeLogLinterService(IChangeLogLoader loader)
    {
        this._loader = loader;
    }

    public async ValueTask<IReadOnlyList<LintError>> LintFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken)
    {
        string content = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);

        return ChangeLogLinter.Lint(content: content, additionalSections: additionalSections);
    }
}
