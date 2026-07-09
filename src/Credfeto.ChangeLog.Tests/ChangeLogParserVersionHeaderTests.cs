using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogParserVersionHeaderTests : TestBase
{
    private static string BuildChangeLog(string versionHeader) =>
        $"""
            # Changelog

            ## [Unreleased]
            ### Added

            {versionHeader}
            ### Added
            - Something

            ## [0.0.0] - Project created
            """;

    [Fact]
    public Task ParseAsync_MissingClosingBracket_ThrowsInvalidChangeLogException()
    {
        string changeLog = BuildChangeLog("## [1.2.3 - 2024-01-01");

        return Assert.ThrowsAsync<InvalidChangeLogException>(() => ChangeLogTestHelper.ParseAsync(changeLog).AsTask());
    }

    [Fact]
    public async Task ParseAsync_NoDateNotYanked_ParsesEmptyDate()
    {
        string changeLog = BuildChangeLog("## [1.2.3]");

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        ChangeLogRelease release = document.Releases[0];
        Assert.Equal(expected: "1.2.3", actual: release.Version);
        Assert.Equal(expected: string.Empty, actual: release.Date);
        Assert.False(release.IsYanked, userMessage: "Expected release to not be yanked");
    }

    [Fact]
    public async Task ParseAsync_DateOnlyNotYanked_ParsesDate()
    {
        string changeLog = BuildChangeLog("## [1.2.3] - 2024-01-01");

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        ChangeLogRelease release = document.Releases[0];
        Assert.Equal(expected: "1.2.3", actual: release.Version);
        Assert.Equal(expected: "2024-01-01", actual: release.Date);
        Assert.False(release.IsYanked, userMessage: "Expected release to not be yanked");
    }

    [Fact]
    public async Task ParseAsync_YankedWithDate_ParsesDateAndYanked()
    {
        string changeLog = BuildChangeLog("## [1.2.3] - 2024-01-01 [YANKED]");

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        ChangeLogRelease release = document.Releases[0];
        Assert.Equal(expected: "1.2.3", actual: release.Version);
        Assert.Equal(expected: "2024-01-01", actual: release.Date);
        Assert.True(release.IsYanked, userMessage: "Expected release to be yanked");
    }

    [Fact]
    public async Task ParseAsync_YankedWithoutDate_ParsesEmptyDateAndYanked()
    {
        string changeLog = BuildChangeLog("## [1.2.3] [YANKED]");

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);

        ChangeLogRelease release = document.Releases[0];
        Assert.Equal(expected: "1.2.3", actual: release.Version);
        Assert.Equal(expected: string.Empty, actual: release.Date);
        Assert.True(release.IsYanked, userMessage: "Expected release to be yanked");
    }

    [Fact]
    public async Task RoundTrip_YankedWithoutDate_PreservesYankedAndEmptyDate()
    {
        string changeLog = BuildChangeLog("## [1.2.3] [YANKED]");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);
        string serialised = await new ChangeLogSerialiser().SerialiseAsync(document, cancellationToken);
        ChangeLogDocument reparsed = await ChangeLogTestHelper.ParseAsync(serialised);

        ChangeLogRelease release = reparsed.Releases[0];
        Assert.Equal(expected: "1.2.3", actual: release.Version);
        Assert.Equal(expected: string.Empty, actual: release.Date);
        Assert.True(release.IsYanked, userMessage: "Expected round-tripped release to still be yanked");
    }
}
