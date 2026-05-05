using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

internal sealed class ChangeLogFixerService : IChangeLogFixer
{
    private readonly IChangeLogLoader _loader;

    public ChangeLogFixerService(IChangeLogLoader loader)
    {
        this._loader = loader;
    }

    public async ValueTask FixFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken)
    {
        string content = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        string @fixed = ChangeLogFixer.Fix(content: content, additionalSections: additionalSections);

        await this._loader.SaveTextAsync(changeLogFileName, contents: @fixed, cancellationToken: cancellationToken);
    }
}
