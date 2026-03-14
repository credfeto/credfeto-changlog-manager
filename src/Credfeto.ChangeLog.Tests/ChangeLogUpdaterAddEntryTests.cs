using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Testing the bit that changes the file rather than reading/writing"
)]
public sealed class ChangeLogUpdaterAddEntryTests : TestBase
{
    [Fact]
    public void AddToEmptyChangelog()
    {
        string result = ChangeLogUpdater.AddEntry(changeLog: string.Empty, type: "Added", message: "Added a new entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Deprecated
### Removed
### Security
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void AddToExistingChangelog()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = ChangeLogUpdater.AddEntry(changeLog: existing, type: "Added", message: "Another entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
- Another entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void AddingDuplicateToExistingChangelogDoesNotAdd()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = ChangeLogUpdater.AddEntry(changeLog: existing, type: "Added", message: "Added a new entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void AddToExistingChangelogWithTrailingBlanks()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry

### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = ChangeLogUpdater.AddEntry(changeLog: existing, type: "Added", message: "Another entry");

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Added a new entry
- Another entry

### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void AddToExistingChangelogForSectionThatDoesNotExistFails()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.1] - 2020-12-29
### Added
- Added a new entry

## [0.0.0] - Project created";

        Assert.Throws<InvalidChangeLogException>(() =>
            ChangeLogUpdater.AddEntry(changeLog: existing, type: "Added", message: "Another entry")
        );
    }
}
