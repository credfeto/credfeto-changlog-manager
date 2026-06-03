using Credfeto.ChangeLog.Extensions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogHeadingExtensionsTests : TestBase
{
    [Theory]
    [InlineData("## [1.0.0] - 2024-01-01", "1.0.0")]
    [InlineData("## [Unreleased]", "Unreleased")]
    [InlineData("## [0.0.0] - Project created", "0.0.0")]
    public void GetVersionStringExtractsVersionFromHeader(string line, string expectedVersion)
    {
        string? version = line.GetVersionString();
        Assert.Equal(expected: expectedVersion, actual: version);
    }

    [Fact]
    public void GetVersionStringReturnsNullWhenNoCloseBracket()
    {
        const string line = "## [no-close-bracket";
        string? version = line.GetVersionString();
        Assert.Null(version);
    }

    [Theory]
    [InlineData("<!-- comment -->", true)]
    [InlineData("Some text <!-- comment -->", true)]
    [InlineData("No comment here", false)]
    public void ContainsHtmlCommentStartDetectsOpeningTag(string line, bool expected)
    {
        bool result = line.ContainsHtmlCommentStart();
        Assert.Equal(expected: expected, actual: result);
    }

    [Theory]
    [InlineData("<!-- comment -->", true)]
    [InlineData("Some text -->", true)]
    [InlineData("No comment here", false)]
    public void ContainsHtmlCommentEndDetectsClosingTag(string line, bool expected)
    {
        bool result = line.ContainsHtmlCommentEnd();
        Assert.Equal(expected: expected, actual: result);
    }
}
