using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

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
        return RemoveBlankLinesAfterHeadings(ensured);
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
