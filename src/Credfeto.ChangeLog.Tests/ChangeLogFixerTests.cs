using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed partial class ChangeLogFixerTests : TestBase
{
    private const string VALID_CHANGE_LOG = """
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

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

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

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void BlankLineAfterHeading_IsRemoved()
    {
        const string changeLog = """
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

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingRequiredSection_IsAdded()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void OutOfOrderSections_AreReordered()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - an entry
            ### Security

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(result), Language);
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingPreamble_IsAdded()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        Assert.Contains(
            "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),",
            result,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).",
            result,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ExistingPreamble_IsNotDuplicated()
    {
        const string changeLog = """
            # Changelog
            All notable changes to this project will be documented in this file.

            The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
            and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        int count = PreambleFormatLineRegex().Count(result);
        Assert.Equal(expected: 1, actual: count);
    }

    [Fact]
    public void MissingPreamble_IsInsertedBeforeHtmlComment()
    {
        const string changeLog = """
            # Changelog

            <!--
            Please ADD ALL Changes to the UNRELEASED SECTION
            -->

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        string result = Serialise(ChangeLogFixer.Fix(Parse(changeLog), Language));

        int preambleIndex = result.IndexOf("The format is based on", StringComparison.Ordinal);
        int commentIndex = result.IndexOf("<!--", StringComparison.Ordinal);
        Assert.True(preambleIndex < commentIndex, "Preamble should appear before HTML comment");
    }

    [Fact]
    public void RemoveBlankLinesAfterHeadingsReturnsDocumentUnchangedWhenUnreleasedIsNull()
    {
        const string changeLog = """
            # Changelog

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release
            """;

        ChangeLogDocument document = Parse(changeLog);
        Assert.Null(document.Unreleased);

        ChangeLogDocument result = ChangeLogFixer.RemoveBlankLinesAfterHeadings(document);
        Assert.Null(result.Unreleased);
        Assert.Same(expected: document, actual: result);
    }

    [GeneratedRegex(
        pattern: @"The format is based on \[Keep a Changelog\]\(https://keepachangelog\.com/en/1\.1\.0/\),",
        options: RegexOptions.None,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex PreambleFormatLineRegex();
}
