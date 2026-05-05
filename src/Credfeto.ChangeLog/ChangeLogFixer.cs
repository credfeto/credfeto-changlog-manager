using System.Collections.Generic;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog;

public static class ChangeLogFixer
{
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
