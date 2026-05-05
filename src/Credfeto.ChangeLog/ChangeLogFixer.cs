using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog;

public static class ChangeLogFixer
{
    private static readonly IChangeLogLoader ChangeLogLoader = FileSystemChangeLogLoader.Instance;

    public static async ValueTask FixFileAsync(
        string changeLogFileName,
        IReadOnlyCollection<string>? additionalSections,
        CancellationToken cancellationToken
    )
    {
        string content = await ChangeLogLoader.LoadTextAsync(changeLogFileName, cancellationToken);

        string @fixed = Fix(content: content, additionalSections: additionalSections);

        await ChangeLogLoader.SaveTextAsync(changeLogFileName, contents: @fixed, cancellationToken: cancellationToken);
    }

    public static string Fix(string content, IReadOnlyCollection<string>? additionalSections = null)
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
