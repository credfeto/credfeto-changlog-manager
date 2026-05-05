using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace Credfeto.ChangeLog.BenchMark.Tests.Bench;

[SimpleJob]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class EnsureUnreleasedSectionsBenchmark
{
    private const string CORRECT_ORDER_CHANGELOG =
        @"# Changelog
All notable changes to this project will be documented in this file.

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

    private const string OUT_OF_ORDER_CHANGELOG =
        @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Deployment Changes
### Removed
### Added
- A new feature was added.
### Fixed
- A bug was fixed.
### Changed

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

    [Benchmark]
    public string EnsureUnreleasedSections_AllSectionsCorrect()
    {
        return ChangeLogUpdater.EnsureUnreleasedSections(CORRECT_ORDER_CHANGELOG);
    }

    [Benchmark]
    public string EnsureUnreleasedSections_OutOfOrderAndMissing()
    {
        return ChangeLogUpdater.EnsureUnreleasedSections(OUT_OF_ORDER_CHANGELOG);
    }
}
