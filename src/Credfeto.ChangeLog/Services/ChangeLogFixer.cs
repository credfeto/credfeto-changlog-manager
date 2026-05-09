using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812: Avoid uninstantiated internal classes",
    Justification = "Registered in DI"
)]
internal sealed class ChangeLogFixer : IChangeLogFixer
{
    private readonly IChangeLogStorage _storage;

    public ChangeLogFixer(IChangeLogStorage storage)
    {
        this._storage = storage;
    }

    public async ValueTask FixAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(
            changeLogFileName,
            cancellationToken
        );
        ChangeLogDocument corrected = Fix(document: document, language: language);
        await this._storage.SaveAsync(
            changeLogFileName,
            document: corrected,
            cancellationToken: cancellationToken
        );
    }

    internal static ChangeLogDocument Fix(ChangeLogDocument document, ChangeLogLanguage language)
    {
        ChangeLogDocument ensured = ChangeLogUpdater.EnsureUnreleasedSections(
            document: document,
            language: language
        );
        ChangeLogDocument withPreamble = EnsurePreamble(ensured);
        return RemoveBlankLinesAfterHeadings(withPreamble);
    }

    internal static ChangeLogDocument EnsurePreamble(ChangeLogDocument document)
    {
        if (HasPreamble(document.HeaderLines))
        {
            return document;
        }

        return document with
        {
            HeaderLines = InsertPreamble(document.HeaderLines),
        };
    }

    private static bool HasPreamble(in ImmutableArray<string> headerLines) =>
        headerLines
            .AsValueEnumerable()
            .Any(line =>
                line.Contains(
                    value: TemplateFile.PreambleLine1,
                    comparisonType: System.StringComparison.Ordinal
                )
            );

    private static ImmutableArray<string> InsertPreamble(in ImmutableArray<string> headerLines)
    {
        int commentStart = FindHtmlCommentStart(headerLines);

        ImmutableArray<string> before =
            commentStart >= 0 ? headerLines[..commentStart] : headerLines;
        ImmutableArray<string> after = commentStart >= 0 ? headerLines[commentStart..] : [];

        List<string> result = TrimTrailingBlanks([.. before]);
        result.Add(string.Empty);
        result.Add(TemplateFile.PreambleLine1);
        result.Add(TemplateFile.PreambleLine2);
        result.Add(string.Empty);
        result.AddRange(after);

        return [.. result];
    }

    private static int FindHtmlCommentStart(in ImmutableArray<string> headerLines)
    {
        for (int i = 0; i < headerLines.Length; i++)
        {
            if (
                headerLines[i]
                    .StartsWith(value: "<!--", comparisonType: System.StringComparison.Ordinal)
            )
            {
                return i;
            }
        }

        return -1;
    }

    private static List<string> TrimTrailingBlanks(List<string> lines)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static ChangeLogDocument RemoveBlankLinesAfterHeadings(ChangeLogDocument document)
    {
        if (document.Unreleased is null)
        {
            return document;
        }

        return document with
        {
            Unreleased = RemoveBlankLinesFromSections(document.Unreleased),
        };
    }

    private static ChangeLogUnreleased RemoveBlankLinesFromSections(ChangeLogUnreleased unreleased)
    {
        ImmutableArray<ChangeLogSection>.Builder builder =
            ImmutableArray.CreateBuilder<ChangeLogSection>(unreleased.Sections.Length);

        foreach (ChangeLogSection section in unreleased.Sections)
        {
            builder.Add(RemoveLeadingBlank(section));
        }

        return unreleased with
        {
            Sections = builder.ToImmutable(),
        };
    }

    private static ChangeLogSection RemoveLeadingBlank(ChangeLogSection section) =>
        section.Entries.Length > 0 && string.IsNullOrWhiteSpace(section.Entries[0])
            ? section with
            {
                Entries = section.Entries[1..],
            }
            : section;
}
