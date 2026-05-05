using System;
using System.Collections.Generic;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

internal sealed partial class ChangeLogUpdater
{
    private static string EnsureUnreleasedSectionsCommon(string changeLog)
    {
        List<string> text = ChangeLogAsLines(changeLog);

        int unreleasedStart = text.FindUnreleasedStart();

        if (unreleasedStart == -1)
        {
            return Throws.CouldNotFindUnreleasedSectionString();
        }

        int unreleasedEnd = text.FindUnreleasedEnd(unreleasedStart);

        (List<string> sectionOrder, Dictionary<string, List<string>> sections, List<string> trailer) =
            ParseUnreleasedSections(text: text, unreleasedStart: unreleasedStart, unreleasedEnd: unreleasedEnd);

        List<string> newContent = BuildNewUnreleasedContent(
            sectionOrder: sectionOrder,
            sections: sections,
            trailer: trailer
        );

        text.RemoveRange(index: unreleasedStart + 1, count: unreleasedEnd - unreleasedStart - 1);
        text.InsertRange(index: unreleasedStart + 1, collection: newContent);

        return text.LinesToText();
    }

    private static (List<string> sectionOrder, Dictionary<string, List<string>> sections, List<string> trailer)
        ParseUnreleasedSections(IReadOnlyList<string> text, int unreleasedStart, int unreleasedEnd)
    {
        List<string> sectionOrder = [];
        Dictionary<string, List<string>> sections = new(StringComparer.Ordinal);
        string? currentSection = null;
        List<string> trailer = [];

        for (int i = unreleasedStart + 1; i < unreleasedEnd; i++)
        {
            string line = text[i];

            if (line.StartsWithHtmlComment())
            {
                ExtractTrailingBlanksIntoTrailer(sections: sections, currentSection: currentSection, trailer: trailer);

                for (int j = i; j < unreleasedEnd; j++)
                {
                    trailer.Add(text[j]);
                }

                break;
            }

            if (line.IsChangeTypeHeading())
            {
                string sectionName = line.GetChangeTypeName();
                currentSection = sectionName;

                if (!sections.ContainsKey(sectionName))
                {
                    sectionOrder.Add(sectionName);
                    sections[sectionName] = [];
                }
            }
            else if (currentSection is not null)
            {
                sections[currentSection].Add(line);
            }
        }

        return (sectionOrder, sections, trailer);
    }

    private static void ExtractTrailingBlanksIntoTrailer(
        Dictionary<string, List<string>> sections,
        string? currentSection,
        List<string> trailer
    )
    {
        if (currentSection is null)
        {
            return;
        }

        List<string> content = sections[currentSection];
        List<string> extracted = [];

        while (content.Count > 0 && string.IsNullOrWhiteSpace(content[^1]))
        {
            extracted.Insert(index: 0, item: content[^1]);
            content.RemoveAt(content.Count - 1);
        }

        trailer.AddRange(extracted);
    }

    private static List<string> BuildNewUnreleasedContent(
        IReadOnlyList<string> sectionOrder,
        Dictionary<string, List<string>> sections,
        IReadOnlyList<string> trailer
    )
    {
        List<string> newContent = [];

        for (int i = 0; i < ChangeLogSections.Order.Length; i++)
        {
            string sectionName = ChangeLogSections.Order[i];
            newContent.Add(ChangeLogSections.Headings[i]);

            if (sections.TryGetValue(sectionName, out List<string>? content))
            {
                newContent.AddRange(content);
            }
        }

        foreach (string sectionName in sectionOrder.Where(s => !ChangeLogSections.KnownSections.Contains(s)))
        {
            newContent.Add(sectionName.AsChangeTypeHeading());
            newContent.AddRange(sections[sectionName]);
        }

        newContent.AddRange(trailer);

        RemoveTrailingBlankLines(newContent);

        return newContent;
    }

    private static void RemoveTrailingBlankLines(List<string> lines)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }
    }
}
