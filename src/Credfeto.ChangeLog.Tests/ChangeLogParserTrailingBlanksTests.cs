using System.Collections.Immutable;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogParserTrailingBlanksTests : TestBase
{
    private const string ChangeLogWithTrailingBlanksBeforeComment = """
        # Changelog

        ## [Unreleased]
        ### Added
        - Some unreleased item



        <!--
        Releases that have at least been deployed to staging.
        -->

        ## [0.0.0] - Project created
        """;

    [Fact]
    public async Task ParseMovesTrailingBlankLinesIntoUnreleasedTrailerInOrderAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(ChangeLogWithTrailingBlanksBeforeComment);

        ImmutableArray<string> trailingLines = document.Unreleased?.TrailingLines ?? [];

        Assert.Equal(
            expected: ["", "", "", "<!--", "Releases that have at least been deployed to staging.", "-->", ""],
            actual: trailingLines
        );
    }

    [Fact]
    public async Task ParseKeepsNonBlankEntriesOutOfTheTrailerAsync()
    {
        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(ChangeLogWithTrailingBlanksBeforeComment);

        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        ChangeLogSection added = Assert.Single(sections);
        Assert.Equal(expected: "Added", actual: added.Name);
        Assert.Equal(expected: ["- Some unreleased item"], actual: added.Entries);
    }
}
