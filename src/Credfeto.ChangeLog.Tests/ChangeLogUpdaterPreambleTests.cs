using System;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed class ChangeLogUpdaterPreambleTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    private const string ChangeLogWithoutPreamble =
        @"# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Security
### Added
- Existing entry
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [0.0.0] - Project created";

    private static ChangeLogDocument Parse(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default).GetAwaiter().GetResult();
    }

    private static string Serialise(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default).GetAwaiter().GetResult();
    }

    private static void AssertContainsPreamble(string result)
    {
        Assert.Contains(
            "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),",
            result,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).",
            result,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void AddEntry_WithMissingPreamble_PreambleIsAdded()
    {
        string result = Serialise(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.AddEntry(Parse(ChangeLogWithoutPreamble), "Added", "Another entry")
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public void RemoveEntry_WithMissingPreamble_PreambleIsAdded()
    {
        string result = Serialise(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.RemoveEntry(
                    Parse(ChangeLogWithoutPreamble),
                    "Added",
                    "Existing entry"
                )
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public void CreateRelease_WithMissingPreamble_PreambleIsAdded()
    {
        string result = Serialise(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.CreateRelease(
                    Parse(ChangeLogWithoutPreamble),
                    "1.0.0",
                    pending: true,
                    Language
                )
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public void EnsureUnreleasedSections_WithMissingPreamble_PreambleIsAdded()
    {
        string result = Serialise(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.EnsureUnreleasedSections(Parse(ChangeLogWithoutPreamble), Language)
            )
        );

        AssertContainsPreamble(result);
    }
}
