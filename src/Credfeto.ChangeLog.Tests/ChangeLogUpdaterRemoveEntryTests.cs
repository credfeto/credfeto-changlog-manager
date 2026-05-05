using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0045:Use async overload", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.VisualStudio.Threading.Analyzers", checkId: "VSTHRD002", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2012:UseValueTasksCorrectly", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
public sealed class ChangeLogUpdaterRemoveEntryTests : TestBase
{
    private static ChangeLogDocument ParseOrCreate(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(string.IsNullOrEmpty(content) ? TemplateFile.Initial : content, default).GetAwaiter().GetResult();
    }

    private static string Serialise(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default).GetAwaiter().GetResult();
    }


    [Fact]
    public void RemoveFromEmptyChangelog()
    {
        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(string.Empty), "Added", "Added a new entry"));

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Security
### Added
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void RemoveOnlyLineFromSection()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Remove Me
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

        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(existing), "Added", "Remove Me"));

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
    public void RemoveFirstLineFromSection()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Remove Me
- Do Not Remove Me
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(existing), "Added", "Remove Me"));

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do Not Remove Me
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
    public void RemoveLastLineFromSection()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do Not Remove Me
- Remove Me
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(existing), "Added", "Remove Me"));

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do Not Remove Me
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
    public void DoesNotRemoveLineFromReleasedSection()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do Not Remove Me
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.0.0]
### Added
- Remove Me

## [0.0.0] - Project created";

        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(existing), "Added", "Remove Me"));

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do Not Remove Me
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.0.0]
### Added
- Remove Me

## [0.0.0] - Project created";

        Assert.Equal(expected.ToLocalEndLine(), actual: result);
    }

    [Fact]
    public void RemovesMultipleMatchingFromUnreleased()
    {
        const string existing =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Remove Me
- Do not remove me
- Remove Me
### Fixed
### Changed
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

        string result = Serialise(ChangeLogUpdater.RemoveEntry(ParseOrCreate(existing), "Added", "Remove Me"));

        const string expected =
            @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
- Do not remove me
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
}
