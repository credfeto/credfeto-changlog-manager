using System;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Testing the bit that changes the file rather than reading/writing"
)]
public sealed class ChangeLogUpdaterCreateRelease : TestBase
{
    private readonly ITestOutputHelper _output;

    public ChangeLogUpdaterCreateRelease(ITestOutputHelper output)
    {
        this._output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact]
    public void EmptyUnreleasedDoesNotCreateARelease()
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
## [0.0.0] - Project created";

        Assert.Throws<EmptyChangeLogException>(() =>
            ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true)
        );
    }

    [Fact]
    public void CannotCreateAReleaseThatAlreadyExists()
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
- Something.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.0.0] - 2020-11-23
## Added
- An Item

## [0.0.0] - Project created";

        Assert.Throws<ReleaseAlreadyExistsException>(() =>
            ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true)
        );
    }

    [Fact]
    public void CannotCreateAReleaseOlderThanLatest()
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
- Something.
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [2.0.0] - 2020-11-23
## Added
- An Item

## [0.0.0] - Project created";

        Assert.Throws<ReleaseTooOldException>(() =>
            ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true)
        );
    }

    [Fact]
    public void ChangeLogWithOnlyAddedInUnreleasedProducesReleaseWithJustAdded()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Some Content
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true);

        const string expected =
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
## [1.0.0] - TBD
### Added
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyFixedInUnreleasedProducesReleaseWithJustAdded()
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
- Some Content
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true);

        const string expected =
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
## [1.0.0] - TBD
### Fixed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyChangedInUnreleasedProducesReleaseWithJustAdded()
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
- Some Content
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true);

        const string expected =
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
## [1.0.0] - TBD
### Changed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void ChangeLogWithOnlyRemovedInUnreleasedProducesReleaseWithJustAdded()
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
- Some Content
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string updated = ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true);

        const string expected =
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
## [1.0.0] - TBD
### Removed
- Some Content

## [0.0.0] - Project created";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }

    [Fact]
    public void NoPreviousReleaseAddsReleaseAtEndOfFile()
    {
        const string changeLog =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Some Content
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->";

        string updated = ChangeLogUpdater.CreateRelease(changeLog: changeLog, version: "1.0.0", pending: true);

        const string expected =
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
## [1.0.0] - TBD
### Added
- Some Content";

        this._output.WriteLine(updated);
        Assert.Equal(expected.ToLocalEndLine(), actual: updated);
    }
}
