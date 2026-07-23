using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

public sealed class ChangeLogLinter : IChangeLogLinter
{
    private readonly IChangeLogStorage _storage;

    public ChangeLogLinter(IChangeLogStorage storage)
    {
        this._storage = storage;
    }

    public async ValueTask<IReadOnlyList<LintError>> LintAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        ChangeLogDocument document = await this._storage.LoadAsync(changeLogFileName, cancellationToken);

        return Lint(document: document, language: language);
    }

    public static IReadOnlyList<LintError> Lint(ChangeLogDocument document, ChangeLogLanguage language)
    {
        List<LintError> errors = [];
        CheckUnreleased(document: document, errors: errors, language: language);
        CheckVersionHeaders(releases: document.Releases, errors: errors);
        CheckReleaseDates(releases: document.Releases, errors: errors, language: language);
        return errors;
    }

    private static void CheckUnreleased(ChangeLogDocument document, List<LintError> errors, ChangeLogLanguage language)
    {
        if (document.Unreleased is null)
        {
            errors.Add(new(LineNumber: 1, Message: "Missing [Unreleased] section"));
            return;
        }

        CheckUnreleasedSections(unreleased: document.Unreleased, errors: errors, language: language);
    }

    private static void CheckUnreleasedSections(
        ChangeLogUnreleased unreleased,
        List<LintError> errors,
        ChangeLogLanguage language
    )
    {
        CheckDuplicateSections(sections: unreleased.Sections, errors: errors);
        CheckUnknownSections(sections: unreleased.Sections, errors: errors, language: language);
        CheckSectionOrderAndPresence(
            sections: unreleased.Sections,
            unreleasedLineNumber: unreleased.LineNumber,
            errors: errors,
            language: language
        );
        CheckBlankLinesAfterHeadings(sections: unreleased.Sections, errors: errors);
    }

    private static void CheckDuplicateSections(in ImmutableArray<ChangeLogSection> sections, List<LintError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (ChangeLogSection section in sections.AsValueEnumerable().Where(s => !seen.Add(s.Name)))
        {
            errors.Add(
                new(
                    LineNumber: section.LineNumber,
                    Message: $"Section '### {section.Name}' is duplicated in [Unreleased]"
                )
            );
        }
    }

    private static void CheckUnknownSections(
        in ImmutableArray<ChangeLogSection> sections,
        List<LintError> errors,
        ChangeLogLanguage language
    )
    {
        foreach (
            ChangeLogSection section in sections
                .AsValueEnumerable()
                .Where(s => !language.SectionOrder.AsValueEnumerable().Any(n => n.EqualsOrdinal(s.Name)))
        )
        {
            errors.Add(
                new(LineNumber: section.LineNumber, Message: $"Unknown section '### {section.Name}' in [Unreleased]")
            );
        }
    }

    private static void CheckSectionOrderAndPresence(
        in ImmutableArray<ChangeLogSection> sections,
        int unreleasedLineNumber,
        List<LintError> errors,
        ChangeLogLanguage language
    )
    {
        int lastFoundIndex = -1;

        foreach (string required in language.SectionOrder)
        {
            CheckRequiredSection(
                sections: sections,
                required: required,
                unreleasedLineNumber: unreleasedLineNumber,
                errors: errors,
                lastFoundIndex: ref lastFoundIndex
            );
        }
    }

    private static void CheckRequiredSection(
        in ImmutableArray<ChangeLogSection> sections,
        string required,
        int unreleasedLineNumber,
        List<LintError> errors,
        ref int lastFoundIndex
    )
    {
        int pos = FindSectionIndex(sections: sections, name: required);

        if (pos < 0)
        {
            errors.Add(
                new(
                    LineNumber: unreleasedLineNumber,
                    Message: $"Missing required section '### {required}' in [Unreleased]"
                )
            );
        }
        else if (pos < lastFoundIndex)
        {
            errors.Add(
                new(
                    LineNumber: sections[pos].LineNumber,
                    Message: $"Section '### {required}' is out of order in [Unreleased]"
                )
            );
        }
        else
        {
            lastFoundIndex = pos;
        }
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

    private static void CheckBlankLinesAfterHeadings(
        in ImmutableArray<ChangeLogSection> sections,
        List<LintError> errors
    )
    {
        foreach (ChangeLogSection section in sections.AsValueEnumerable().Where(HasLeadingBlank))
        {
            errors.Add(new(LineNumber: section.LineNumber, Message: $"Blank line after heading '### {section.Name}'"));
        }
    }

    private static bool HasLeadingBlank(ChangeLogSection section) =>
        section.Entries.Length > 0
        && string.IsNullOrWhiteSpace(section.Entries[0])
        && section.Entries.AsValueEnumerable().Any(e => !string.IsNullOrWhiteSpace(e));

    private static void CheckVersionHeaders(in ImmutableArray<ChangeLogRelease> releases, List<LintError> errors)
    {
        List<(Version Parsed, int LineNumber, string Original)> valid = [];

        foreach (ChangeLogRelease release in releases)
        {
            CheckReleaseVersion(release: release, valid: valid, errors: errors);
        }

        CheckVersionsDescendingOrder(versions: valid, errors: errors);
    }

    private static void CheckReleaseVersion(
        ChangeLogRelease release,
        List<(Version Parsed, int LineNumber, string Original)> valid,
        List<LintError> errors
    )
    {
        if (!Version.TryParse(input: release.Version, result: out Version? parsed) || parsed.Build < 0)
        {
            errors.Add(
                new(
                    LineNumber: release.LineNumber,
                    Message: $"Invalid version '{release.Version}' — must be a valid semver version"
                )
            );
            return;
        }

        valid.Add((parsed, release.LineNumber, release.Version));
    }

    private static void CheckVersionsDescendingOrder(
        List<(Version Parsed, int LineNumber, string Original)> versions,
        List<LintError> errors
    )
    {
        for (int i = 1; i < versions.Count; i++)
        {
            if (versions[i].Parsed >= versions[i - 1].Parsed)
            {
                errors.Add(
                    new(
                        LineNumber: versions[i].LineNumber,
                        Message: $"Version '{versions[i].Original}' is not in descending order"
                    )
                );
            }
        }
    }

    private static void CheckReleaseDates(
        in ImmutableArray<ChangeLogRelease> releases,
        List<LintError> errors,
        ChangeLogLanguage language
    )
    {
        foreach (ChangeLogRelease release in releases.AsValueEnumerable())
        {
            CheckReleaseDate(release: release, language: language, errors: errors);
        }
    }

    private static void CheckReleaseDate(ChangeLogRelease release, ChangeLogLanguage language, List<LintError> errors)
    {
        if (string.IsNullOrWhiteSpace(release.Date))
        {
            return;
        }

        if (
            DateTime.TryParseExact(
                release.Date,
                language.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _
            )
        )
        {
            return;
        }

        if (DateTime.TryParse(release.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            errors.Add(
                new(
                    LineNumber: release.LineNumber,
                    Message: $"Release date '{release.Date}' for version '{release.Version}' is not in the expected format '{language.DateFormat}'"
                )
            );
        }
    }
}
