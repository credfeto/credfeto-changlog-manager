using System;
using System.Collections.Immutable;

namespace Credfeto.ChangeLog;

public static class ChangeLogLanguageFactory
{
    public const string KeepAChangelog = "keep-a-changelog";

    private static readonly ChangeLogLanguage DefaultLanguage = new(
        DocumentTitle: "Changelog",
        UnreleasedSectionName: "Unreleased",
        SectionOrder: ["Security", "Added", "Fixed", "Changed", "Deprecated", "Removed", "Deployment Changes"],
        DateFormat: "yyyy-MM-dd");

    public static ChangeLogLanguage Get(string languageCode)
    {
        return string.Equals(languageCode, KeepAChangelog, StringComparison.Ordinal)
            ? DefaultLanguage
            : throw new ArgumentException(message: $"Unknown language code: {languageCode}", paramName: nameof(languageCode));
    }
}
