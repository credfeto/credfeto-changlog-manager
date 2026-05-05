using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogReaderService : IChangeLogReader
{
    private readonly IChangeLogLoader _loader;

    public ChangeLogReaderService(IChangeLogLoader loader)
    {
        this._loader = loader;
    }

    public async ValueTask<string> ExtractReleaseNotesFromFileAsync(string changeLogFileName, string version, CancellationToken cancellationToken)
    {
        string textBlock = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);

        return ChangeLogReader.ExtractReleaseNotes(changeLog: textBlock, version: version);
    }

    public async ValueTask<int?> FindFirstReleaseVersionPositionAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> changelog = await this._loader.LoadLinesAsync(changeLogFileName, cancellationToken);

        for (int lineIndex = 0; lineIndex < changelog.Count; ++lineIndex)
        {
            if (CommonRegex.VersionHeader.IsMatch(changelog[lineIndex]))
            {
                return lineIndex + 1;
            }
        }

        return null;
    }
}
