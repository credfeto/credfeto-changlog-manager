using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;
using ZLinq;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed class ChangeLogLinter : IChangeLogLinter
{
    private readonly IChangeLogParser _parser;
    private readonly ChangeLogLanguage _language;
    private readonly IChangeLogStorage _loader;

    public ChangeLogLinter(IChangeLogStorage loader, IChangeLogParser parser, ChangeLogLanguage language)
    {
        this._loader = loader;
        this._parser = parser;
        this._language = language;
    }

    public async ValueTask<IReadOnlyList<LintError>> LintFileAsync(
        string changeLogFileName,
        IReadOnlyCollection<string>? additionalSections,
        CancellationToken cancellationToken)
    {
        string content = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);
        ChangeLogDocument document = await this._parser.ParseAsync(content: content, cancellationToken: cancellationToken);
        return Lint(document: document, additionalSections: additionalSections, language: this._language);
    }

    internal static IReadOnlyList<LintError> Lint(
        ChangeLogDocument document,
        IReadOnlyCollection<string>? additionalSections,
        ChangeLogLanguage language)
    {
        List<LintError> errors = [];
        CheckUnreleased(document: document, errors: errors, additionalSections: additionalSections, language: language);
        CheckVersionHeaders(releases: document.Releases, errors: errors);
        return errors;
    }

    private static void CheckUnreleased(
        ChangeLogDocument document,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections,
        ChangeLogLanguage language)
    {
        if (document.Unreleased is null)
        {
            errors.Add(new(LineNumber: 1, Message: "Missing [Unreleased] section"));
            return;
        }

        CheckUnreleasedSections(unreleased: document.Unreleased, errors: errors, additionalSections: additionalSections, language: language);
    }

    private static void CheckUnreleasedSections(
        ChangeLogUnreleased unreleased,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections,
        ChangeLogLanguage language)
    {
        CheckDuplicateSections(sections: unreleased.Sections, errors: errors);
        CheckUnknownSections(sections: unreleased.Sections, errors: errors, additionalSections: additionalSections, language: language);
        CheckSectionOrderAndPresence(sections: unreleased.Sections, unreleasedLineNumber: unreleased.LineNumber, errors: errors, language: language);
        CheckBlankLinesAfterHeadings(sections: unreleased.Sections, errors: errors);
    }

    private static void CheckDuplicateSections(in ImmutableArray<ChangeLogSection> sections, List<LintError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (ChangeLogSection section in sections.Where(s => !seen.Add(s.Name)))
        {
            errors.Add(new(LineNumber: section.LineNumber, Message: $"Section '### {section.Name}' is duplicated in [Unreleased]"));
        }
    }

    private static void CheckUnknownSections(
        in ImmutableArray<ChangeLogSection> sections,
        List<LintError> errors,
        IReadOnlyCollection<string>? additionalSections,
        ChangeLogLanguage language)
    {
        foreach (ChangeLogSection section in sections.Where(s => !IsKnownSection(name: s.Name, additionalSections: additionalSections, language: language)))
        {
            errors.Add(new(LineNumber: section.LineNumber, Message: $"Unknown section '### {section.Name}' in [Unreleased]"));
        }
    }

    private static bool IsKnownSection(string name, IReadOnlyCollection<string>? additionalSections, ChangeLogLanguage language)
    {
        if (language.SectionOrder.Any(s => s.EqualsOrdinal(name)))
        {
            return true;
        }

        return additionalSections?.Any(s => s.EqualsOrdinal(name)) == true;
    }

    private static void CheckSectionOrderAndPresence(
        in ImmutableArray<ChangeLogSection> sections,
        int unreleasedLineNumber,
        List<LintError> errors,
        ChangeLogLanguage language)
    {
        int lastFoundIndex = -1;

        foreach (string required in language.SectionOrder)
        {
            CheckRequiredSection(
                sections: sections,
                required: required,
                unreleasedLineNumber: unreleasedLineNumber,
                errors: errors,
                lastFoundIndex: ref lastFoundIndex);
        }
    }

    private static void CheckRequiredSection(
        in ImmutableArray<ChangeLogSection> sections,
        string required,
        int unreleasedLineNumber,
        List<LintError> errors,
        ref int lastFoundIndex)
    {
        int pos = FindSectionIndex(sections: sections, name: required);

        if (pos < 0)
        {
            errors.Add(new(LineNumber: unreleasedLineNumber, Message: $"Missing required section '### {required}' in [Unreleased]"));
        }
        else if (pos < lastFoundIndex)
        {
            errors.Add(new(LineNumber: sections[pos].LineNumber, Message: $"Section '### {required}' is out of order in [Unreleased]"));
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

    private static void CheckBlankLinesAfterHeadings(in ImmutableArray<ChangeLogSection> sections, List<LintError> errors)
    {
        foreach (ChangeLogSection section in sections.Where(HasLeadingBlank))
        {
            errors.Add(new(LineNumber: section.LineNumber, Message: $"Blank line after heading '### {section.Name}'"));
        }
    }

    private static bool HasLeadingBlank(ChangeLogSection section)
        => section.Entries.Length > 0 && string.IsNullOrWhiteSpace(section.Entries[0]);

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
        List<LintError> errors)
    {
        if (!Version.TryParse(input: release.Version, result: out Version? parsed) || parsed.Build < 0)
        {
            errors.Add(new(LineNumber: release.LineNumber, Message: $"Invalid version '{release.Version}' — must be a valid semver version"));
            return;
        }

        valid.Add((parsed, release.LineNumber, release.Version));
    }

    private static void CheckVersionsDescendingOrder(
        List<(Version Parsed, int LineNumber, string Original)> versions,
        List<LintError> errors)
    {
        for (int i = 1; i < versions.Count; i++)
        {
            if (versions[i].Parsed >= versions[i - 1].Parsed)
            {
                errors.Add(new(LineNumber: versions[i].LineNumber, Message: $"Version '{versions[i].Original}' is not in descending order"));
            }
        }
    }
}
