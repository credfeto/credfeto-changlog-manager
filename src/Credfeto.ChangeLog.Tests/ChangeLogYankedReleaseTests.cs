using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
public sealed class ChangeLogYankedReleaseTests : TestBase
{
    private const string YANKED_CHANGE_LOG = """
        # Changelog

        ## [Unreleased]
        ### Security
        ### Added
        ### Fixed
        ### Changed
        ### Deprecated
        ### Removed
        ### Deployment Changes

        ## [1.1.0] - 2024-02-01 [YANKED]
        ### Added
        - Yanked feature

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
    public void ParseYankedRelease_SetsIsYankedTrue()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);

        Assert.True(document.Releases[0].IsYanked, userMessage: "Expected release 1.1.0 to have IsYanked = true");
    }

    [Fact]
    public void ParseYankedRelease_StripsDatesYankedSuffix()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);

        Assert.Equal(expected: "2024-02-01", actual: document.Releases[0].Date);
    }

    [Fact]
    public void ParseNonYankedRelease_SetsIsYankedFalse()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);

        Assert.False(document.Releases[1].IsYanked, userMessage: "Expected release 1.0.0 to have IsYanked = false");
    }

    [Theory]
    [InlineData("## [1.0.0] - 2024-01-01 [YANKED]")]
    [InlineData("## [1.0.0] - 2024-01-01 [yanked]")]
    [InlineData("## [1.0.0] - 2024-01-01 [Yanked]")]
    public void ParseYankedRelease_CaseInsensitive(string header)
    {
        string content = $"""
            # Changelog

            ## [Unreleased]
            ### Security
            ### Added
            ### Fixed
            ### Changed
            ### Deprecated
            ### Removed
            ### Deployment Changes

            {header}
            ### Added
            - Something

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = Parse(content);

        Assert.True(
            document.Releases[0].IsYanked,
            userMessage: $"Expected release parsed from '{header}' to have IsYanked = true"
        );
        Assert.Equal(expected: "2024-01-01", actual: document.Releases[0].Date);
    }

    [Fact]
    public void SerialiseYankedRelease_IncludesYankedMarker()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);
        string serialised = Serialise(document);

        Assert.True(
            serialised.Contains("## [1.1.0] - 2024-02-01 [YANKED]", StringComparison.Ordinal),
            userMessage: $"Expected serialised output to contain yanked header, but got:{Environment.NewLine}{serialised}"
        );
    }

    [Fact]
    public void SerialiseNonYankedRelease_DoesNotIncludeYankedMarker()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);
        string serialised = Serialise(document);

        Assert.True(
            serialised.Contains("## [1.0.0] - 2024-01-01", StringComparison.Ordinal),
            userMessage: $"Expected serialised output to contain non-yanked header, but got:{Environment.NewLine}{serialised}"
        );
        Assert.False(
            serialised.Contains("## [1.0.0] - 2024-01-01 [YANKED]", StringComparison.Ordinal),
            userMessage: $"Expected serialised output NOT to contain [YANKED] for non-yanked release, but got:{Environment.NewLine}{serialised}"
        );
    }

    [Fact]
    public void RoundTrip_YankedRelease_PreservesMarker()
    {
        ChangeLogDocument document = Parse(YANKED_CHANGE_LOG);
        string serialised = Serialise(document);
        ChangeLogDocument reparsed = Parse(serialised);

        Assert.True(
            reparsed.Releases[0].IsYanked,
            userMessage: "Expected round-tripped release to still have IsYanked = true"
        );
        Assert.Equal(expected: "2024-02-01", actual: reparsed.Releases[0].Date);
    }

    [Fact]
    public void LintYankedRelease_ReturnsNoVersionErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(YANKED_CHANGE_LOG), Language);

        Assert.DoesNotContain(
            errors,
            e =>
                e.Message.Contains(value: "1.1.0", comparisonType: StringComparison.Ordinal)
                && e.Message.Contains(value: "Invalid version", comparisonType: StringComparison.Ordinal)
        );
    }

    [Fact]
    public void LintYankedRelease_ReturnsNoOrderErrors()
    {
        IReadOnlyList<LintError> errors = ChangeLogLinter.Lint(Parse(YANKED_CHANGE_LOG), Language);

        Assert.DoesNotContain(
            errors,
            e => e.Message.Contains(value: "descending order", comparisonType: StringComparison.Ordinal)
        );
    }
}
