using System;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

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

    private static ValueTask<ChangeLogDocument> ParseAsync(string content)
    {
        ChangeLogParser parser = new();
        return parser.ParseAsync(content, default);
    }

    private static ValueTask<string> SerialiseAsync(ChangeLogDocument document)
    {
        ChangeLogSerialiser serialiser = new();
        return serialiser.SerialiseAsync(document, default);
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
    public async Task AddEntry_WithMissingPreamble_PreambleIsAdded()
    {
        string result = await SerialiseAsync(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.AddEntry(await ParseAsync(ChangeLogWithoutPreamble), "Added", "Another entry")
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public async Task RemoveEntry_WithMissingPreamble_PreambleIsAdded()
    {
        string result = await SerialiseAsync(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.RemoveEntry(await ParseAsync(ChangeLogWithoutPreamble), "Added", "Existing entry")
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public async Task CreateRelease_WithMissingPreamble_PreambleIsAdded()
    {
        string result = await SerialiseAsync(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.CreateRelease(
                    await ParseAsync(ChangeLogWithoutPreamble),
                    "1.0.0",
                    pending: true,
                    Language
                )
            )
        );

        AssertContainsPreamble(result);
    }

    [Fact]
    public async Task EnsureUnreleasedSections_WithMissingPreamble_PreambleIsAdded()
    {
        string result = await SerialiseAsync(
            ChangeLogFixer.EnsurePreamble(
                ChangeLogUpdater.EnsureUnreleasedSections(await ParseAsync(ChangeLogWithoutPreamble), Language)
            )
        );

        AssertContainsPreamble(result);
    }
}
