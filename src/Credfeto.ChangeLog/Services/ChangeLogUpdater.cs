using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogUpdater : IChangeLogUpdater
{
    private readonly IChangeLogParser _parser;
    private readonly IChangeLogStorage _storage;

    public ChangeLogUpdater(IChangeLogStorage storage, IChangeLogParser parser)
    {
        this._storage = storage;
        this._parser = parser;
    }

    public async ValueTask CreateEmptyAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument empty = await this._parser.ParseAsync(TemplateFile.Build(language), cancellationToken);
        await this._storage.SaveAsync(changeLogFileName, document: empty, cancellationToken: cancellationToken);
    }

    public async Task AddEntryAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string type,
        string message,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this.LoadOrCreateAsync(
            changeLogFileName: changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );
        ChangeLogDocument updated = AddEntry(document: document, type: type, message: message);
        ChangeLogDocument withPreamble = ChangeLogFixer.EnsurePreamble(updated);
        await this._storage.SaveAsync(changeLogFileName, document: withPreamble, cancellationToken: cancellationToken);
    }

    public async Task RemoveEntryAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string type,
        string message,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this.LoadOrCreateAsync(
            changeLogFileName: changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );
        ChangeLogDocument updated = RemoveEntry(document: document, type: type, message: message);
        ChangeLogDocument withPreamble = ChangeLogFixer.EnsurePreamble(updated);
        await this._storage.SaveAsync(changeLogFileName, document: withPreamble, cancellationToken: cancellationToken);
    }

    public async Task CreateReleaseAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        string version,
        bool pending,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(changeLogFileName, cancellationToken);
        ChangeLogDocument updated = CreateRelease(
            document: document,
            version: version,
            pending: pending,
            language: language
        );
        ChangeLogDocument withPreamble = ChangeLogFixer.EnsurePreamble(updated);
        await this._storage.SaveAsync(changeLogFileName, document: withPreamble, cancellationToken: cancellationToken);
    }

    public async ValueTask EnsureUnreleasedSectionsAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this.LoadOrCreateAsync(
            changeLogFileName: changeLogFileName,
            language: language,
            cancellationToken: cancellationToken
        );
        ChangeLogDocument updated = EnsureUnreleasedSections(document: document, language: language);
        ChangeLogDocument withPreamble = ChangeLogFixer.EnsurePreamble(updated);
        await this._storage.SaveAsync(changeLogFileName, document: withPreamble, cancellationToken: cancellationToken);
    }

    public static ChangeLogDocument AddEntry(ChangeLogDocument document, string type, string message)
    {
        ChangeLogUnreleased unreleased = RequireUnreleased(document);
        ChangeLogSection section = RequireSection(unreleased: unreleased, type: type);
        string entry = "- " + message;

        if (section.Entries.AsValueEnumerable().Any(e => e.EqualsOrdinal(entry)))
        {
            return document;
        }

        ChangeLogSection updated = InsertEntry(section: section, entry: entry);
        return ReplaceSection(document: document, unreleased: unreleased, updated: updated);
    }

    public static ChangeLogDocument RemoveEntry(ChangeLogDocument document, string type, string message)
    {
        ChangeLogUnreleased unreleased = RequireUnreleased(document);
        ChangeLogSection section = RequireSection(unreleased: unreleased, type: type);
        string prefix = "- " + message;
        ImmutableArray<string> filtered =
        [
            .. section
                .Entries.AsValueEnumerable()
                .Where(e => !e.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal)),
        ];
        ChangeLogSection updated = section with { Entries = filtered };
        return ReplaceSection(document: document, unreleased: unreleased, updated: updated);
    }

    public static ChangeLogDocument CreateRelease(
        ChangeLogDocument document,
        string version,
        bool pending,
        ChangeLogLanguage language
    )
    {
        ChangeLogUnreleased unreleased = RequireUnreleased(document);
        ValidateVersionNotExists(releases: document.Releases, version: version);
        ImmutableArray<ChangeLogSection> releaseSections = BuildReleaseSections(unreleased.Sections);

        if (releaseSections.IsEmpty)
        {
            throw new EmptyChangeLogException("No changes for the release");
        }

        string date = pending ? "TBD" : CurrentDate(language);
        ChangeLogRelease newRelease = new(Version: version, Date: date, LineNumber: 0, Sections: releaseSections);
        ImmutableArray<ChangeLogRelease> releases = [newRelease, .. document.Releases];
        ChangeLogUnreleased cleared = ClearUnreleasedEntries(unreleased);
        return document with { Unreleased = cleared, Releases = releases };
    }

    public static ChangeLogDocument EnsureUnreleasedSections(ChangeLogDocument document, ChangeLogLanguage language)
    {
        ChangeLogUnreleased unreleased = RequireUnreleased(document);
        ImmutableArray<ChangeLogSection> ordered = ChangeLogSerialiser.OrderSections(
            sections: unreleased.Sections,
            sectionOrder: language.SectionOrder
        );
        return document with { Unreleased = unreleased with { Sections = ordered } };
    }

    private async ValueTask<ChangeLogDocument> LoadOrCreateAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(changeLogFileName))
        {
            await this.CreateEmptyAsync(changeLogFileName, language: language, cancellationToken: cancellationToken);
        }

        return await this._storage.LoadAsync(changeLogFileName, cancellationToken);
    }

    private static ChangeLogUnreleased RequireUnreleased(ChangeLogDocument document)
    {
        return document.Unreleased
            ?? throw new InvalidChangeLogException("Could not find [Unreleased] section of file");
    }

    private static ChangeLogSection RequireSection(ChangeLogUnreleased unreleased, string type)
    {
        int idx = FindSectionIndex(sections: unreleased.Sections, name: type);

        return idx >= 0
            ? unreleased.Sections[idx]
            : throw new InvalidChangeLogException($"Could not find {type} heading");
    }

    private static int FindSectionIndex(in ImmutableArray<ChangeLogSection> sections, string name)
    {
        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].Name.EqualsOrdinal(name))
            {
                return i;
            }
        }

        return -1;
    }

    private static ChangeLogSection InsertEntry(ChangeLogSection section, string entry)
    {
        int insertAt = FindInsertPosition(section.Entries);
        ImmutableArray<string>.Builder builder = section.Entries.ToBuilder();
        builder.Insert(index: insertAt, item: entry);
        return section with { Entries = builder.ToImmutable() };
    }

    private static int FindInsertPosition(in ImmutableArray<string> entries)
    {
        int i = entries.Length - 1;

        while (i >= 0 && string.IsNullOrWhiteSpace(entries[i]))
        {
            i--;
        }

        return i + 1;
    }

    private static ChangeLogDocument ReplaceSection(
        ChangeLogDocument document,
        ChangeLogUnreleased unreleased,
        ChangeLogSection updated
    )
    {
        ImmutableArray<ChangeLogSection> sections =
        [
            .. unreleased.Sections.Select(s => s.Name.EqualsOrdinal(updated.Name) ? updated : s),
        ];
        return document with { Unreleased = unreleased with { Sections = sections } };
    }

    [SuppressMessage(
        category: "SonarAnalyzer.CSharp",
        checkId: "S3267",
        Justification = "Projection not applicable: full release object needed for exception messages"
    )]
    private static void ValidateVersionNotExists(in ImmutableArray<ChangeLogRelease> releases, string version)
    {
        foreach (ChangeLogRelease release in releases)
        {
            if (release.Version.EqualsOrdinal(version))
            {
                throw new ReleaseAlreadyExistsException($"Release {version} already exists");
            }

            if (
                Version.TryParse(release.Version, out Version? existing)
                && Version.TryParse(version, out Version? requested)
                && existing > requested
            )
            {
                throw new ReleaseTooOldException(
                    $"Release {release.Version} already exists and is newer than {version}"
                );
            }
        }
    }

    private static ImmutableArray<ChangeLogSection> BuildReleaseSections(in ImmutableArray<ChangeLogSection> sections)
    {
        List<ChangeLogSection> result = [];

        foreach (ChangeLogSection s in sections)
        {
            ImmutableArray<string> entries = [.. s.Entries.AsValueEnumerable().Where(e => !string.IsNullOrEmpty(e))];

            if (entries.Length > 0)
            {
                result.Add(s with { Entries = entries });
            }
        }

        return [.. result];
    }

    private static ChangeLogUnreleased ClearUnreleasedEntries(ChangeLogUnreleased unreleased)
    {
        ImmutableArray<ChangeLogSection> cleared = [.. unreleased.Sections.Select(s => s with { Entries = [] })];
        return unreleased with { Sections = cleared };
    }

    [SuppressMessage(
        category: "FunFair.CodeAnalysis",
        checkId: "FFS0001",
        Justification = "Should always use the local time."
    )]
    private static string CurrentDate(ChangeLogLanguage language) =>
        DateTime.Now.ToString(format: language.DateFormat, provider: CultureInfo.InvariantCulture);
}
