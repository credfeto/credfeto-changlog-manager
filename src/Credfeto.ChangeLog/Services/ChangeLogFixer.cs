using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed class ChangeLogFixer : IChangeLogFixer
{
    private readonly IChangeLogStorage _loader;

    public ChangeLogFixer(IChangeLogStorage loader)
    {
        this._loader = loader;
    }

    public async ValueTask FixFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken)
    {
        string content = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        string @fixed = Fix(content: content, additionalSections: additionalSections);

        await this._loader.SaveTextAsync(changeLogFileName, contents: @fixed, cancellationToken: cancellationToken);
    }

    internal static string Fix(string content, IReadOnlyCollection<string>? additionalSections = null)
    {
        string result = ChangeLogUpdater.EnsureUnreleasedSections(content);

        return RemoveBlankLinesAfterHeadings(result);
    }

    private static string RemoveBlankLinesAfterHeadings(string content)
    {
        IReadOnlyList<string> lines = content.SplitToLines();
        List<string> output = new(lines.Count);
        int i = 0;

        while (i < lines.Count)
        {
            string line = lines[i];
            output.Add(line);
            i++;

            if (
                line.IsChangeTypeHeading()
                && i < lines.Count
                && string.IsNullOrWhiteSpace(lines[i])
            )
            {
                i++;
            }
        }

        return output.LinesToText();
    }
}
