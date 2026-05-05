using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0045:Use async overload", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.VisualStudio.Threading.Analyzers", checkId: "VSTHRD002", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2012:UseValueTasksCorrectly", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
public sealed class ChangeLogFixerTests : TestBase
{
    private const string VALID_CHANGE_LOG =
        """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        ## [0.0.0] - Project created
        """;

    private static readonly ChangeLogLanguage Language = ChangeLogLanguageFactory.Get(ChangeLogLanguageFactory.KeepAChangelog);

    private static ChangeLogDocument Parse(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default).GetAwaiter().GetResult();
    }

    private static string Serialise(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default).GetAwaiter().GetResult();
    }

    [Fact]
    public void AlreadyValidChangelog_RemainsValid()
    {
        string result = Serialise(ChangeLogFixer.Fix(Parse(VALID_CHANGE_LOG), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), null, Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void BlankLineAfterHeading_IsRemoved()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added

            - an entry
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), null, Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingRequiredSection_IsAdded()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), null, Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void OutOfOrderSections_AreReordered()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            ### Security

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), null, Language);
        Assert.Empty(errors);
    }
}
