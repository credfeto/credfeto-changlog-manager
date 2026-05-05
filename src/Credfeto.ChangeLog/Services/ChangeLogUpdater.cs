using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed partial class ChangeLogUpdater : IChangeLogUpdater
{
    private readonly IChangeLogStorage _loader;

    public ChangeLogUpdater(IChangeLogStorage loader)
    {
        this._loader = loader;
    }

    public async Task AddEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = AddEntry(changeLog: textBlock, type: type, message: message);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async Task RemoveEntryAsync(string changeLogFileName, string type, string message, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = RemoveEntry(changeLog: textBlock, type: type, message: message);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async Task CreateReleaseAsync(string changeLogFileName, string version, bool pending, CancellationToken cancellationToken)
    {
        string textBlock = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        string content = CreateRelease(changeLog: textBlock, version: version, pending: pending);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public async ValueTask EnsureUnreleasedSectionsAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        string textBlock = await this.ReadChangeLogAsync(changeLogFileName, cancellationToken);
        string content = EnsureUnreleasedSections(textBlock);

        await this._loader.SaveTextAsync(changeLogFileName, contents: content, cancellationToken: cancellationToken);
    }

    public ValueTask CreateEmptyAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        return this._loader.SaveTextAsync(changeLogFileName, contents: TemplateFile.Initial, cancellationToken: cancellationToken);
    }

    internal static string AddEntry(string changeLog, string type, string message)
    {
        return AddEntryCommon(changeLog: changeLog, type: type, message: message);
    }

    internal static string RemoveEntry(string changeLog, string type, string message)
    {
        return RemoveEntryCommon(changeLog: changeLog, type: type, message: message);
    }

    internal static string CreateRelease(string changeLog, string version, bool pending)
    {
        return CreateReleaseCommon(changeLog: changeLog, version: version, pending: pending);
    }

    internal static string EnsureUnreleasedSections(string changeLog)
    {
        return EnsureUnreleasedSectionsCommon(changeLog);
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

    private static string EnsureChangelog(string changeLog)
    {
        if (string.IsNullOrWhiteSpace(changeLog))
        {
            return TemplateFile.Initial;
        }

        return changeLog;
    }

    private static List<string> ChangeLogAsLines(string changeLog)
    {
        return [.. EnsureChangelog(changeLog).SplitToLines()];
    }
}
