using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Services;

public sealed class FileSystemChangeLogStorage : IChangeLogStorage
{
    public bool Exists(string changeLogFileName)
    {
        return File.Exists(changeLogFileName);
    }

    public async ValueTask<string> LoadTextAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask<IReadOnlyList<string>> LoadLinesAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        return await File.ReadAllLinesAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask SaveTextAsync(string changeLogFileName, string contents, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: contents,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }
}
