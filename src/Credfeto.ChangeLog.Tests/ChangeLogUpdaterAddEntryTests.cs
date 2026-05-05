using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Constants;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0045:Use async overload", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.VisualStudio.Threading.Analyzers", checkId: "VSTHRD002", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
[SuppressMessage(category: "Microsoft.Reliability", checkId: "CA2012:UseValueTasksCorrectly", Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks")]
public sealed class ChangeLogUpdaterAddEntryTests : TestBase
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
    public void AddToEmptyChangelog()
    {
        string result = Serialise(ChangeLogUpdater.AddEntry(ParseOrCreate(string.Empty), "Added", "Added a new entry"));

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
- Added a new entry
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

        string result = Serialise(ChangeLogUpdater.AddEntry(ParseOrCreate(existing), "Added", "Another entry"));

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

        string result = Serialise(ChangeLogUpdater.AddEntry(ParseOrCreate(existing), "Added", "Added a new entry"));

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

        string result = Serialise(ChangeLogUpdater.AddEntry(ParseOrCreate(existing), "Added", "Another entry"));

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
            ChangeLogUpdater.AddEntry(ParseOrCreate(existing), "Added", "Another entry")
        );
    }
}
