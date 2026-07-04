using System.Text;
using Credfeto.ChangeLog;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.Constants;

internal static class TemplateFile
{
    private const string KEEP_A_CHANGELOG = "https://keepachangelog.com/en/1.1.0/";
    private const string SEMANTIC_VERSIONING = "https://semver.org/spec/v2.0.0.html";

    public const string PreambleLine1 = "The format is based on [Keep a Changelog](" + KEEP_A_CHANGELOG + "),";

    public const string PreambleLine2 =
        "and this project adheres to [Semantic Versioning](" + SEMANTIC_VERSIONING + ").";

    public static string Build(ChangeLogLanguage language)
    {
        return new StringBuilder()
            .Append("# Changelog\n")
            .Append("All notable changes to this project will be documented in this file.\n")
            .Append('\n')
            .Append(PreambleLine1)
            .Append('\n')
            .Append(PreambleLine2)
            .Append('\n')
            .Append('\n')
            .Append("<!--\n")
            .Append("Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release\n")
            .Append("-->\n")
            .Append('\n')
            .Append("## [")
            .Append(FileConstants.Unreleased)
            .Append("]\n")
            .AppendSectionHeadings(language.SectionOrder)
            .Append('\n')
            .Append("<!--\n")
            .Append(
                "Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch\n"
            )
            .Append("-->\n")
            .Append("## [0.0.0] - Project created")
            .ToString();
    }
}
