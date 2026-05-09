using System;
using System.Collections.Immutable;

namespace Credfeto.ChangeLog;

public sealed class ChangeLogLanguageFactory : IChangeLogLanguageFactory
{
    public const string English = "en";

    private static readonly ChangeLogLanguage EnglishLanguage = new(
        DocumentTitle: "Changelog",
        UnreleasedSectionName: "Unreleased",
        SectionOrder:
        [
            "Security",
            "Added",
            "Fixed",
            "Changed",
            "Deprecated",
            "Removed",
            "Deployment Changes",
        ],
        DateFormat: "yyyy-MM-dd"
    );

    public ChangeLogLanguage Get(string languageCode)
    {
        return string.Equals(languageCode, English, StringComparison.Ordinal)
            ? EnglishLanguage
            : throw new ArgumentException(
                message: $"Unknown language code: {languageCode}",
                paramName: nameof(languageCode)
            );
    }
}
