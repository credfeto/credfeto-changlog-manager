using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0045:Use async overload", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.VisualStudio.Threading.Analyzers", checkId: "VSTHRD002", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2012:UseValueTasksCorrectly", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
public sealed class ChangeLogLinterTests : TestBase
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

    [Fact]
    public void ValidChangelog_ReturnsNoErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(VALID_CHANGE_LOG), null, Language);

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingUnreleasedSection_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(errors, e => e.Message.Contains(value: "[Unreleased]", comparisonType: StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequiredSection_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e =>
                e.Message.Contains(value: "### Added", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "Missing", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void DuplicateSection_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - First added
            ### Added
            - Duplicate added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e =>
                e.Message.Contains(value: "### Added", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "duplicated", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void UnknownSection_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes
            ### Custom

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e =>
                e.Message.Contains(value: "### Custom", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "Unknown", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void UnknownSection_AllowedViaAdditionalSections_ReturnsNoError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes
            ### Custom

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), ["Custom"], Language);

        Assert.DoesNotContain(
            errors,
            e =>
                e.Message.Contains(value: "### Custom", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "Unknown", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void BlankLineAfterHeading_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added

            - item
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e => e.Message.Contains(value: "Blank line after heading '### Added'", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void NoBlankLineAfterHeading_ReturnsNoError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            - item
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "Blank line after heading", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void InvalidVersionHeader_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [not-a-version] - 2024-01-01
            ### Added
            - Initial release

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e =>
                e.Message.Contains(value: "not-a-version", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "Invalid version", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void ValidVersionsInOrder_ReturnsNoErrors()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [2.0.0] - 2024-03-01
            ### Added
            - Release 2

            ## [1.0.0] - 2024-02-01
            ### Added
            - Release 1

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }

    private static readonly ChangeLogLanguage Language = ChangeLogLanguageFactory.Get(ChangeLogLanguageFactory.KeepAChangelog);

    private static ChangeLogDocument Parse(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default).GetAwaiter().GetResult();
    }

    [Fact]
    public void VersionsOutOfOrder_ReturnsError()
    {
        const string changeLog =
            """
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Removed
            ### Deployment Changes

            ## [1.0.0] - 2024-02-01
            ### Added
            - Release 1

            ## [2.0.0] - 2024-03-01
            ### Added
            - Release 2

            ## [0.0.0] - Project created
            """;

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(changeLog), null, Language);

        Assert.Contains(
            errors,
            e =>
                e.Message.Contains(value: "2.0.0", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }
}
