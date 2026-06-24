using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

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

    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private static readonly ChangeLogDocument CorrectOrderDocument = ParseSync(CORRECT_ORDER_CHANGELOG);
    private static readonly ChangeLogDocument OutOfOrderDocument = ParseSync(OUT_OF_ORDER_CHANGELOG);

    [Benchmark]
    public ChangeLogDocument EnsureUnreleasedSections_AllSectionsCorrect()
    {
        return ChangeLogUpdater.EnsureUnreleasedSections(document: CorrectOrderDocument, language: Language);
    }

    [Benchmark]
    public ChangeLogDocument EnsureUnreleasedSections_OutOfOrderAndMissing()
    {
        return ChangeLogUpdater.EnsureUnreleasedSections(document: OutOfOrderDocument, language: Language);
    }

    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0045:Use async overload",
        Justification = "Benchmark setup requires synchronous parse of a pure ValueTask.FromResult"
    )]
    [SuppressMessage(
        category: "Microsoft.VisualStudio.Threading.Analyzers",
        checkId: "VSTHRD002",
        Justification = "Benchmark setup requires synchronous parse of a pure ValueTask.FromResult"
    )]
    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2012:UseValueTasksCorrectly",
        Justification = "ChangeLogParser.ParseAsync always returns ValueTask.FromResult — already completed"
    )]
    private static ChangeLogDocument ParseSync(string content) =>
        new ChangeLogParser()
            .ParseAsync(content: content, cancellationToken: CancellationToken.None)
            .GetAwaiter()
            .GetResult();
}
