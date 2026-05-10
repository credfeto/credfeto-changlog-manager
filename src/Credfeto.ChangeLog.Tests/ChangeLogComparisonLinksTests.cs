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
public sealed class ChangeLogComparisonLinksTests : TestBase
{
    private const string ChangeLogWithComparisonLinks = """
        # Changelog

        ## [Unreleased]
        ### Added
        - Work in progress

        ## [1.1.0] - 2024-02-01
        ### Added
        - New feature

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        [unreleased]: https://github.com/owner/repo/compare/v1.1.0...HEAD
        [1.1.0]: https://github.com/owner/repo/compare/v1.0.0...v1.1.0
        [1.0.0]: https://github.com/owner/repo/releases/tag/v1.0.0
        """;

    private const string ChangeLogWithComparisonLinksAndBlankLine = """
        # Changelog

        ## [Unreleased]
        ### Added
        - Work in progress

        ## [1.0.0] - 2024-01-01
        ### Added
        - Initial release

        [unreleased]: https://github.com/owner/repo/compare/v1.0.0...HEAD
        [1.0.0]: https://github.com/owner/repo/releases/tag/v1.0.0
        """;

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
    public void ParseCapturesComparisonLinksInTrailingLines()
    {
        ChangeLogDocument document = Parse(ChangeLogWithComparisonLinks);

        Assert.Equal(expected: 3, actual: document.TrailingLines.Length);
        Assert.Contains(
            document.TrailingLines,
            line => line.StartsWith("[unreleased]:", System.StringComparison.Ordinal)
        );
        Assert.Contains(
            document.TrailingLines,
            line => line.StartsWith("[1.1.0]:", System.StringComparison.Ordinal)
        );
        Assert.Contains(
            document.TrailingLines,
            line => line.StartsWith("[1.0.0]:", System.StringComparison.Ordinal)
        );
    }

    [Fact]
    public void ComparisonLinksAreNotIncludedInReleaseSections()
    {
        ChangeLogDocument document = Parse(ChangeLogWithComparisonLinks);

        foreach (ChangeLogRelease release in document.Releases)
        {
            foreach (ChangeLogSection section in release.Sections)
            {
                foreach (string entry in section.Entries)
                {
                    Assert.False(
                        entry.StartsWith(
                            "[unreleased]:",
                            System.StringComparison.OrdinalIgnoreCase
                        ),
                        userMessage: $"Comparison link found in section entries: {entry}"
                    );
                }
            }
        }
    }

    [Fact]
    public void RoundTripPreservesComparisonLinks()
    {
        string serialised = Serialise(Parse(ChangeLogWithComparisonLinks));

        Assert.Contains(
            "[unreleased]: https://github.com/owner/repo/compare/v1.1.0...HEAD",
            serialised,
            comparisonType: System.StringComparison.Ordinal
        );
        Assert.Contains(
            "[1.1.0]: https://github.com/owner/repo/compare/v1.0.0...v1.1.0",
            serialised,
            comparisonType: System.StringComparison.Ordinal
        );
        Assert.Contains(
            "[1.0.0]: https://github.com/owner/repo/releases/tag/v1.0.0",
            serialised,
            comparisonType: System.StringComparison.Ordinal
        );
    }

    [Fact]
    public void RoundTripWithBlankLineBeforeLinksPreservesLinks()
    {
        ChangeLogDocument document = Parse(ChangeLogWithComparisonLinksAndBlankLine);
        string serialised = Serialise(document);

        Assert.Contains(
            "[unreleased]: https://github.com/owner/repo/compare/v1.0.0...HEAD",
            serialised,
            comparisonType: System.StringComparison.Ordinal
        );
        Assert.Contains(
            "[1.0.0]: https://github.com/owner/repo/releases/tag/v1.0.0",
            serialised,
            comparisonType: System.StringComparison.Ordinal
        );
    }

    [Fact]
    public void DocumentWithoutComparisonLinksHasEmptyTrailingLines()
    {
        const string changeLogWithoutLinks = """
            # Changelog

            ## [Unreleased]
            ### Added
            - Work in progress

            ## [1.0.0] - 2024-01-01
            ### Added
            - Initial release

            """;

        ChangeLogDocument document = Parse(changeLogWithoutLinks);

        Assert.Empty(document.TrailingLines);
    }

    [Fact]
    public void ComparisonLinksAppearAfterAllReleasesInSerialisedOutput()
    {
        string serialised = Serialise(Parse(ChangeLogWithComparisonLinks));

        int lastReleasePos = serialised.LastIndexOf(
            "## [1.0.0]",
            comparisonType: System.StringComparison.Ordinal
        );
        int firstLinkPos = serialised.IndexOf(
            "[unreleased]:",
            comparisonType: System.StringComparison.Ordinal
        );

        Assert.True(
            lastReleasePos < firstLinkPos,
            userMessage: "Comparison links should appear after all release sections"
        );
    }
}
