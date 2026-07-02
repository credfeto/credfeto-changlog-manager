using System.Text;
using Credfeto.ChangeLog;

namespace Credfeto.ChangeLog.Constants;

internal static class TemplateFile
{
    private const string KEEP_A_CHANGELOG = "https://keepachangelog.com/en/1.1.0/";
    private const string SEMANTIC_VERSIONING = "https://semver.org/spec/v2.0.0.html";

    public const string PreambleLine1 = "The format is based on [Keep a Changelog](" + KEEP_A_CHANGELOG + "),";

    public const string PreambleLine2 =
        "and this project adheres to [Semantic Versioning](" + SEMANTIC_VERSIONING + ").";

    public static readonly string Initial = BuildInitialContent();

    private static string BuildInitialContent()
    {
        StringBuilder sb = new();
        sb.Append("# Changelog\n");
        sb.Append("All notable changes to this project will be documented in this file.\n");
        sb.Append('\n');
        sb.Append(PreambleLine1);
        sb.Append('\n');
        sb.Append(PreambleLine2);
        sb.Append('\n');
        sb.Append('\n');
        sb.Append("<!--\n");
        sb.Append("Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release\n");
        sb.Append("-->\n");
        sb.Append('\n');
        sb.Append("## [");
        sb.Append(FileConstants.Unreleased);
        sb.Append("]\n");

        foreach (string section in ChangeLogLanguageFactory.DefaultSectionOrder)
        {
            sb.Append("### ");
            sb.Append(section);
            sb.Append('\n');
        }

        sb.Append('\n');
        sb.Append("<!--\n");
        sb.Append(
            "Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch\n"
        );
        sb.Append("-->\n");
        sb.Append("## [0.0.0] - Project created");

        return sb.ToString();
    }
}
