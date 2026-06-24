using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

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

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default);
    }

    private static ValueTask<string> SerialiseAsync(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default);
    }

    [Fact]
    public async Task ParseCapturesComparisonLinksInTrailingLinesAsync()
    {
        ChangeLogDocument document = await ParseAsync(ChangeLogWithComparisonLinks);

        Assert.Equal(expected: 3, actual: document.TrailingLines.Length);
        Assert.Contains(
            document.TrailingLines,
            line => line.StartsWith("[unreleased]:", System.StringComparison.Ordinal)
        );
        Assert.Contains(document.TrailingLines, line => line.StartsWith("[1.1.0]:", System.StringComparison.Ordinal));
        Assert.Contains(document.TrailingLines, line => line.StartsWith("[1.0.0]:", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task ComparisonLinksAreNotIncludedInReleaseSectionsAsync()
    {
        ChangeLogDocument document = await ParseAsync(ChangeLogWithComparisonLinks);

        foreach (ChangeLogRelease release in document.Releases)
        {
            foreach (ChangeLogSection section in release.Sections)
            {
                foreach (string entry in section.Entries)
                {
                    Assert.False(
                        entry.StartsWith("[unreleased]:", System.StringComparison.OrdinalIgnoreCase),
                        userMessage: $"Comparison link found in section entries: {entry}"
                    );
                }
            }
        }
    }

    [Fact]
    public async Task RoundTripPreservesComparisonLinksAsync()
    {
        string serialised = await SerialiseAsync(await ParseAsync(ChangeLogWithComparisonLinks));

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
    public async Task RoundTripWithBlankLineBeforeLinksPreservesLinksAsync()
    {
        ChangeLogDocument document = await ParseAsync(ChangeLogWithComparisonLinksAndBlankLine);
        string serialised = await SerialiseAsync(document);

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
    public async Task DocumentWithoutComparisonLinksHasEmptyTrailingLinesAsync()
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

        ChangeLogDocument document = await ParseAsync(changeLogWithoutLinks);

        Assert.Empty(document.TrailingLines);
    }

    [Fact]
    public async Task ComparisonLinksAppearAfterAllReleasesInSerialisedOutputAsync()
    {
        string serialised = await SerialiseAsync(await ParseAsync(ChangeLogWithComparisonLinks));

        int lastReleasePos = serialised.LastIndexOf("## [1.0.0]", comparisonType: System.StringComparison.Ordinal);
        int firstLinkPos = serialised.IndexOf("[unreleased]:", comparisonType: System.StringComparison.Ordinal);

        Assert.True(
            lastReleasePos < firstLinkPos,
            userMessage: "Comparison links should appear after all release sections"
        );
    }
}
