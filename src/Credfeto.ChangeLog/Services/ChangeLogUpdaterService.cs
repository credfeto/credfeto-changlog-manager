using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog;

namespace Credfeto.ChangeLog.Services;

internal sealed class ChangeLogUpdaterService : IChangeLogUpdater
{
    private readonly IChangeLogLoader _loader;

    public ChangeLogUpdaterService(IChangeLogLoader loader)
    {
        this._loader = loader;
    }

    public async Task AddEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = ChangeLogUpdater.AddEntry(changeLog: textBlock, type: type, message: message);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async Task RemoveEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = ChangeLogUpdater.RemoveEntry(changeLog: textBlock, type: type, message: message);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async Task CreateReleaseAsync(string changeLogFileName, string version, bool pending, CancellationToken cancellationToken)
    {
        string textBlock = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        string content = ChangeLogUpdater.CreateRelease(changeLog: textBlock, version: version, pending: pending);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async ValueTask EnsureUnreleasedSectionsAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = ChangeLogUpdater.EnsureUnreleasedSections(textBlock);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public ValueTask CreateEmptyAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        return this._loader.SaveTextAsync(changeLogFileName, contents: TemplateFile.Initial, cancellationToken: cancellationToken);
    }

    private async Task<string> ReadChangeLogAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        if (this._loader.Exists(changeLogFileName))
        {
            return await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        }

        await this.CreateEmptyAsync(changeLogFileName, cancellationToken);

        return TemplateFile.Initial;
    }
}
