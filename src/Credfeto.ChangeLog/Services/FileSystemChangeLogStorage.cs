using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812: Avoid uninstantiated internal classes",
    Justification = "Registered in DI"
)]
internal sealed class FileSystemChangeLogStorage : IChangeLogStorage
{
    private readonly IChangeLogParser _parser;
    private readonly IChangeLogSerialiser _serialiser;

    public FileSystemChangeLogStorage(IChangeLogParser parser, IChangeLogSerialiser serialiser)
    {
        this._parser = parser;
        this._serialiser = serialiser;
    }

    public async ValueTask<ChangeLogDocument> LoadAsync(string changeLogFileName, CancellationToken cancellationToken)
    {
        string content = await File.ReadAllTextAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );

        return await this._parser.ParseAsync(content: content, cancellationToken: cancellationToken);
    }

    public async ValueTask SaveAsync(
        string changeLogFileName,
        ChangeLogDocument document,
        CancellationToken cancellationToken
    )
    {
        string content = await this._serialiser.SerialiseAsync(
            document: document,
            cancellationToken: cancellationToken
        );

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }
}
