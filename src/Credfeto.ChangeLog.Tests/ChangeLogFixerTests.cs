using System.Collections.Generic;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

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
        ### Removed
        ### Deployment Changes

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        ## [0.0.0] - Project created
        """;

    [Fact]
    public void AlreadyValidChangelog_RemainsValid()
    {
        string result = ChangeLogFixer.Fix(VALID_CHANGE_LOG);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(result);
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

        string result = ChangeLogFixer.Fix(changeLog);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(result);
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

        string result = ChangeLogFixer.Fix(changeLog);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(result);
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

        string result = ChangeLogFixer.Fix(changeLog);

        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(result);
        Assert.Empty(errors);
    }
}
