using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Helpers;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed class ChangeLogParser : IChangeLogParser
{
    public ValueTask<ChangeLogDocument> ParseAsync(string content, CancellationToken cancellationToken)
        => ValueTask.FromResult(Parse(content.SplitToLines()));

    private static ChangeLogDocument Parse(IReadOnlyList<string> lines)
    {
        int unreleasedStart = lines.FindUnreleasedStart();
        if (unreleasedStart < 0)
        {
            return new(HeaderLines: [.. lines], Unreleased: null, Releases: []);
        }

        int unreleasedEnd = lines.FindUnreleasedEnd(unreleasedStart);
        return new(
            HeaderLines: CollectLines(lines, start: 0, end: unreleasedStart),
            Unreleased: ParseUnreleased(lines, start: unreleasedStart, end: unreleasedEnd),
            Releases: ParseReleases(lines, start: unreleasedEnd));
    }

    private static ChangeLogUnreleased ParseUnreleased(IReadOnlyList<string> lines, int start, int end)
    {
        List<ChangeLogSection> sections = [];
        List<string> trailer = [];
        string? currentName = null;
        List<string> currentEntries = [];

        for (int i = start + 1; i < end; i++)
        {
            if (!ProcessUnreleasedLine(lines: lines, lineIndex: i, end: end, sections: sections,
                    currentName: ref currentName, currentEntries: currentEntries, trailer: trailer))
            {
                break;
            }
        }

        FlushSection(sections: sections, name: currentName, entries: currentEntries);
        return new(LineNumber: start + 1, Sections: [.. sections], TrailingLines: [.. trailer]);
    }

    private static bool ProcessUnreleasedLine(
        IReadOnlyList<string> lines, int lineIndex, int end,
        List<ChangeLogSection> sections,
        ref string? currentName, List<string> currentEntries, List<string> trailer)
    {
        string line = lines[lineIndex];

        if (line.StartsWithHtmlComment())
        {
            MoveTrailingBlanks(source: currentEntries, destination: trailer);
            CollectTrailer(lines: lines, from: lineIndex, to: end, trailer: trailer);
            return false;
        }

        if (line.IsChangeTypeHeading())
        {
            FlushSection(sections: sections, name: currentName, entries: currentEntries);
            currentName = line.GetChangeTypeName();
            currentEntries.Clear();
        }
        else if (currentName is not null)
        {
            currentEntries.Add(line);
        }

        return true;
    }

    private static void FlushSection(List<ChangeLogSection> sections, string? name, List<string> entries)
    {
        if (name is not null)
        {
            sections.Add(new(Name: name, LineNumber: 0, Entries: [.. entries]));
        }
    }

    private static void MoveTrailingBlanks(List<string> source, List<string> destination)
    {
        while (source.Count > 0 && string.IsNullOrWhiteSpace(source[^1]))
        {
            destination.Insert(index: 0, item: source[^1]);
            source.RemoveAt(source.Count - 1);
        }
    }

    private static void CollectTrailer(IReadOnlyList<string> lines, int from, int to, List<string> trailer)
    {
        for (int j = from; j < to; j++)
        {
            trailer.Add(lines[j]);
        }
    }

    private static ImmutableArray<ChangeLogRelease> ParseReleases(IReadOnlyList<string> lines, int start)
    {
        List<ChangeLogRelease> releases = [];
        ReleaseParseState state = new();

        for (int i = start; i < lines.Count; i++)
        {
            ProcessReleaseLine(line: lines[i], lineIndex: i, releases: releases, state: state);
        }

        state.Flush(releases);
        return [.. releases];
    }

    private static void ProcessReleaseLine(string line, int lineIndex, List<ChangeLogRelease> releases, ReleaseParseState state)
    {
        if (line.IsVersionHeader() && !Unreleased.IsUnreleasedHeader(line))
        {
            state.Flush(releases);
            state.StartRelease(line: line, lineNumber: lineIndex + 1);
        }
        else if (line.IsChangeTypeHeading())
        {
            state.FlushSection();
            state.CurrentSectionName = line.GetChangeTypeName();
            state.CurrentSectionLine = lineIndex + 1;
        }
        else if (state.CurrentSectionName is not null)
        {
            state.CurrentEntries.Add(line);
        }
    }

    private static ImmutableArray<string> CollectLines(IReadOnlyList<string> lines, int start, int end)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(end - start);
        for (int i = start; i < end; i++)
        {
            builder.Add(lines[i]);
        }

        return builder.ToImmutable();
    }

    private sealed class ReleaseParseState
    {
        public string? CurrentVersion { get; private set; }
        public string? CurrentDate { get; private set; }
        public int CurrentReleaseLineNumber { get; private set; }
        public string? CurrentSectionName { get; set; }
        public int CurrentSectionLine { get; set; }
        public List<string> CurrentEntries { get; } = [];
        private List<ChangeLogSection> CurrentSections { get; } = [];

        public void StartRelease(string line, int lineNumber)
        {
            (this.CurrentVersion, this.CurrentDate) = ParseVersionHeader(line);
            this.CurrentReleaseLineNumber = lineNumber;
        }

        private static (string Version, string Date) ParseVersionHeader(string line)
        {
            int closeBracket = line.IndexOf(value: ']', comparisonType: StringComparison.Ordinal);
            string version = line[4..closeBracket];
            string date = closeBracket + 4 < line.Length ? line[(closeBracket + 4)..] : string.Empty;
            return (version, date);
        }

        public void FlushSection()
        {
            if (this.CurrentSectionName is not null)
            {
                this.CurrentSections.Add(new(Name: this.CurrentSectionName, LineNumber: this.CurrentSectionLine, Entries: [.. this.CurrentEntries]));
            }

            this.CurrentSectionName = null;
            this.CurrentEntries.Clear();
        }

        public void Flush(List<ChangeLogRelease> releases)
        {
            if (this.CurrentVersion is null)
            {
                return;
            }

            this.FlushSection();
            releases.Add(new(Version: this.CurrentVersion, Date: this.CurrentDate ?? string.Empty, LineNumber: this.CurrentReleaseLineNumber, Sections: [.. this.CurrentSections]));
            this.CurrentSections.Clear();
            this.CurrentVersion = null;
        }
    }
}
