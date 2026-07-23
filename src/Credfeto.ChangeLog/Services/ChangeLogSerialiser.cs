using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogSerialiser : IChangeLogSerialiser
{
    public ValueTask<string> SerialiseAsync(ChangeLogDocument document, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Serialise(document));

    private static string Serialise(ChangeLogDocument document)
    {
        List<string> lines = [];
        lines.AddRange(document.HeaderLines);

        if (document.Unreleased is not null)
        {
            SerialiseUnreleased(document.Unreleased, lines);
        }

        foreach (ChangeLogRelease release in document.Releases)
        {
            SerialiseRelease(release, lines);
        }

        lines.AddRange(document.TrailingLines);

        return lines.LinesToText();
    }

    private static void SerialiseUnreleased(ChangeLogUnreleased unreleased, List<string> lines)
    {
        lines.Add("## [Unreleased]");

        foreach (ChangeLogSection section in unreleased.Sections)
        {
            SerialiseSection(section, lines);
        }

        lines.AddRange(unreleased.TrailingLines);
    }

    private static void SerialiseRelease(ChangeLogRelease release, List<string> lines)
    {
        string header = string.IsNullOrEmpty(release.Date)
            ? $"## [{release.Version}]"
            : $"## [{release.Version}] - {release.Date}";

        if (release.IsYanked)
        {
            header += " [YANKED]";
        }

        lines.Add(header);

        foreach (ChangeLogSection section in release.Sections)
        {
            if (section.Entries.Length > 0)
            {
                SerialiseSection(section, lines);
            }
        }

        lines.Add(string.Empty);
    }

    private static void SerialiseSection(ChangeLogSection section, List<string> lines)
    {
        lines.Add(section.Name.AsChangeTypeHeading());
        lines.AddRange(section.Entries);
    }

    public static ImmutableArray<ChangeLogSection> OrderSections(
        in ImmutableArray<ChangeLogSection> sections,
        in ImmutableArray<string> sectionOrder
    )
    {
        List<ChangeLogSection> result = [];
        Dictionary<string, ChangeLogSection> byName = BuildSectionMap(sections);

        foreach (string name in sectionOrder)
        {
            result.Add(
                byName.TryGetValue(name, out ChangeLogSection? existing)
                    ? existing
                    : new(Name: name, LineNumber: 0, Entries: [])
            );
        }

        AddUnknownSections(sections: sections, sectionOrder: sectionOrder, result: result);
        return [.. result];
    }

    private static Dictionary<string, ChangeLogSection> BuildSectionMap(in ImmutableArray<ChangeLogSection> sections)
    {
        Dictionary<string, ChangeLogSection> map = new(System.StringComparer.Ordinal);

        foreach (ChangeLogSection section in sections)
        {
            if (map.TryGetValue(section.Name, out ChangeLogSection? existing))
            {
                map[section.Name] = MergeSections(existing, section);
            }
            else
            {
                map[section.Name] = section;
            }
        }

        return map;
    }

    private static ChangeLogSection MergeSections(ChangeLogSection first, ChangeLogSection second) =>
        first with
        {
            Entries = [.. first.Entries, .. second.Entries],
        };

    private static void AddUnknownSections(
        in ImmutableArray<ChangeLogSection> sections,
        in ImmutableArray<string> sectionOrder,
        List<ChangeLogSection> result
    )
    {
        HashSet<string> known = new(sectionOrder, System.StringComparer.Ordinal);
        HashSet<string> added = new(System.StringComparer.Ordinal);

        foreach (
            ChangeLogSection section in sections
                .AsValueEnumerable()
                .Where(s => !known.Contains(s.Name) && added.Add(s.Name))
        )
        {
            result.Add(section);
        }
    }
}
