using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogReaderTests : TestBase
{
    private const string MULTI_RELEASE_CHANGE_LOG =
        @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.1.1] - TBD
### Added
- Something was added here.

## [1.1.0] - TBD
### Added
- This is release 1.1.0.

## [1.0] - TBD
### Added
- This is release 1.0.0.

## [0.0.0] - Project created
### Added
- Initial version
";

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    [InlineData("1.0.0.1")]
    public void ReadEmptyChangeLogReturnsEmpty(string version)
    {
        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: string.Empty, version: version);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithNoContentReturnsEmpty(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithJustAddedReturnsAddedSectionOnly(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Something was added.
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        const string expected =
            @"### Added
- Something was added.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithJustFixedReturnsAddedSectionOnly(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
- Something was fixed.
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        const string expected =
            @"### Fixed
- Something was fixed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithJustChangedReturnsChangedSectionOnly(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- Something was changed.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        const string expected =
            @"### Changed
- Something was changed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithJustRemovedReturnsRemovedSectionOnly(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
- Something was removed.
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        const string expected =
            @"### Removed
- Something was removed.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0.0.1-master")]
    public void ReadUnReleasedSectionWithJustDeploymentChangesReturnsDeploymentChangesSectionOnly(string version)
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes
- Need to do something special here on the next deployment.

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created
";

        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: changeLog, version: version);
        const string expected =
            @"### Deployment Changes
- Need to do something special here on the next deployment.";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("1.1.1")]
    [InlineData("1.1.1.1")]
    [InlineData("1.1.1.3000")]
    public void ReadASpecificReleaseReturnsThatReleaseOnly(string version)
    {
        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);
        const string expected =
            @"### Added
- Something was added here.";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0")]
    [InlineData("1.0.0.0")]
    [InlineData("1.0.0.3000")]
    public void ReadASpecificReleaseReturnsThatReleaseOnlyIgnoringZeroVersionParts(string version)
    {
        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);
        const string expected =
            @"### Added
- This is release 1.0.0.";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("0.0.0")]
    [InlineData("0.0.0.0")]
    [InlineData("0.0.0.3000")]
    public void ReadASpecificReleaseAtEndOfFile(string version)
    {
        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);

        const string expected =
            @"### Added
- Initial version";
        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Theory]
    [InlineData("7.0")]
    [InlineData("8.1.0")]
    [InlineData("9.2.3.0")]
    [InlineData("10.3.4.3000")]
    public void ReadNonExistentVersion(string version)
    {
        string result = ChangeLogReader.ExtractReleaseNotes(changeLog: MULTI_RELEASE_CHANGE_LOG, version: version);

        Assert.Equal(expected: string.Empty, actual: result);
    }
}
