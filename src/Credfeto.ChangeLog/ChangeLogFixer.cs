using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public static class ChangeLogFixer
{
    private const string SUB_HEADING_PREFIX = "### ";
    private static readonly IChangeLogLoader ChangeLogLoader = FileSystemChangeLogLoader.Instance;

    public static async ValueTask FixFileAsync(
        string changeLogFileName,
        IReadOnlyCollection<string>? additionalSections,
        CancellationToken cancellationToken
    )
    {
        string content = await ChangeLogLoader.LoadTextAsync(changeLogFileName, cancellationToken);

        string @fixed = Fix(content: content, additionalSections: additionalSections);

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: @fixed,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    public static string Fix(string content, IReadOnlyCollection<string>? additionalSections = null)
    {
        string result = ChangeLogUpdater.EnsureUnreleasedSections(content);

        return RemoveBlankLinesAfterHeadings(result);
    }

    private static string RemoveBlankLinesAfterHeadings(string content)
    {
        string[] lines = content.Split('\n');
        List<string> output = new(lines.Length);
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i].TrimEnd('\r');
            output.Add(line);
            i++;

            if (
                line.StartsWith(value: SUB_HEADING_PREFIX, comparisonType: StringComparison.Ordinal)
                && i < lines.Length
                && string.IsNullOrWhiteSpace(lines[i])
            )
            {
                i++;
            }
        }

        return string.Join(separator: Environment.NewLine, values: output).Trim();
    }
}
