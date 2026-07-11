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

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812: Avoid uninstantiated internal classes",
    Justification = "Registered in DI"
)]
internal sealed class ChangeLogParser : IChangeLogParser
{
    public ValueTask<ChangeLogDocument> ParseAsync(string content, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Parse(content.SplitToLines()));

    private static ChangeLogDocument Parse(IReadOnlyList<string> lines)
    {
        int unreleasedStart = lines.FindUnreleasedStart();
        if (unreleasedStart < 0)
        {
            return new(HeaderLines: [.. lines], Unreleased: null, Releases: [], TrailingLines: []);
        }

        int unreleasedEnd = lines.FindUnreleasedEnd(unreleasedStart);
        (ImmutableArray<ChangeLogRelease> releases, ImmutableArray<string> trailingLines) = ParseReleases(
            lines,
            start: unreleasedEnd
        );
        return new(
            HeaderLines: CollectLines(lines, start: 0, end: unreleasedStart),
            Unreleased: ParseUnreleased(lines, start: unreleasedStart, end: unreleasedEnd),
            Releases: releases,
            TrailingLines: trailingLines
        );
    }

    private static ChangeLogUnreleased ParseUnreleased(IReadOnlyList<string> lines, int start, int end)
    {
        List<ChangeLogSection> sections = [];
        List<string> trailer = [];
        string? currentName = null;
        List<string> currentEntries = [];

        for (int i = start + 1; i < end; i++)
        {
            if (
                !ProcessUnreleasedLine(
                    lines: lines,
                    lineIndex: i,
                    end: end,
                    sections: sections,
                    currentName: ref currentName,
                    currentEntries: currentEntries,
                    trailer: trailer
                )
            )
            {
                break;
            }
        }

        FlushSection(sections: sections, name: currentName, entries: currentEntries);
        return new(LineNumber: start + 1, Sections: [.. sections], TrailingLines: [.. trailer]);
    }

    private static bool ProcessUnreleasedLine(
        IReadOnlyList<string> lines,
        int lineIndex,
        int end,
        List<ChangeLogSection> sections,
        ref string? currentName,
        List<string> currentEntries,
        List<string> trailer
    )
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
        int end = source.Count;
        int start = end;

        while (start > 0 && string.IsNullOrWhiteSpace(source[start - 1]))
        {
            --start;
        }

        for (int i = start; i < end; ++i)
        {
            destination.Add(source[i]);
        }

        source.RemoveRange(start, end - start);
    }

    private static void CollectTrailer(IReadOnlyList<string> lines, int from, int to, List<string> trailer)
    {
        for (int j = from; j < to; j++)
        {
            trailer.Add(lines[j]);
        }
    }

    private static (ImmutableArray<ChangeLogRelease> Releases, ImmutableArray<string> TrailingLines) ParseReleases(
        IReadOnlyList<string> lines,
        int start
    )
    {
        List<ChangeLogRelease> releases = [];
        ReleaseParseState state = new();

        for (int i = start; i < lines.Count; i++)
        {
            ProcessReleaseLine(line: lines[i], lineIndex: i, releases: releases, state: state);
        }

        state.Flush(releases);
        return ([.. releases], [.. state.TrailingLines]);
    }

    private static void ProcessReleaseLine(
        string line,
        int lineIndex,
        List<ChangeLogRelease> releases,
        ReleaseParseState state
    )
    {
        if (state.InTrailerMode)
        {
            state.TrailingLines.Add(line);
        }
        else if (line.IsComparisonLink())
        {
            state.EnterTrailerMode();
            state.TrailingLines.Add(line);
        }
        else if (line.IsVersionHeader() && !Unreleased.IsUnreleasedHeader(line))
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
        public List<string> TrailingLines { get; } = [];
        public bool InTrailerMode { get; private set; }
        private List<ChangeLogSection> CurrentSections { get; } = [];

        public bool CurrentIsYanked { get; private set; }

        public void StartRelease(string line, int lineNumber)
        {
            this.InTrailerMode = false;
            this.TrailingLines.Clear();
            (this.CurrentVersion, this.CurrentDate, this.CurrentIsYanked) = ParseVersionHeader(line);
            this.CurrentReleaseLineNumber = lineNumber;
        }

        public void EnterTrailerMode()
        {
            while (this.CurrentEntries.Count > 0 && string.IsNullOrWhiteSpace(this.CurrentEntries[^1]))
            {
                this.CurrentEntries.RemoveAt(this.CurrentEntries.Count - 1);
            }

            this.InTrailerMode = true;
        }

        private const string YANKED_SUFFIX = "[YANKED]";

        private static (string Version, string Date, bool IsYanked) ParseVersionHeader(string line)
        {
            int closeBracket = line.IndexOf(value: ']', comparisonType: StringComparison.Ordinal);

            if (closeBracket == -1)
            {
                return Throws.MalformedVersionHeader(line);
            }

            string version = line[4..closeBracket];
            string rest = line[(closeBracket + 1)..].Trim();

            bool isYanked = rest.EndsWith(value: YANKED_SUFFIX, comparisonType: StringComparison.OrdinalIgnoreCase);
            if (isYanked)
            {
                rest = rest[..^YANKED_SUFFIX.Length].Trim();
            }

            if (rest.StartsWith(value: '-'))
            {
                rest = rest[1..].Trim();
            }

            return (version, rest, isYanked);
        }

        public void FlushSection()
        {
            if (this.CurrentSectionName is not null)
            {
                TrimTrailingBlanks(this.CurrentEntries);
                this.CurrentSections.Add(
                    new(
                        Name: this.CurrentSectionName,
                        LineNumber: this.CurrentSectionLine,
                        Entries: [.. this.CurrentEntries]
                    )
                );
            }

            this.CurrentSectionName = null;
            this.CurrentEntries.Clear();
        }

        private static void TrimTrailingBlanks(List<string> entries)
        {
            while (entries.Count > 0 && string.IsNullOrWhiteSpace(entries[^1]))
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }

        public void Flush(List<ChangeLogRelease> releases)
        {
            if (this.CurrentVersion is null)
            {
                return;
            }

            this.FlushSection();
            releases.Add(
                new(
                    Version: this.CurrentVersion,
                    Date: this.CurrentDate ?? string.Empty,
                    LineNumber: this.CurrentReleaseLineNumber,
                    Sections: [.. this.CurrentSections],
                    IsYanked: this.CurrentIsYanked
                )
            );
            this.CurrentSections.Clear();
            this.CurrentVersion = null;
            this.CurrentIsYanked = false;
        }
    }
}
