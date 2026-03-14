using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog;

internal static class TemplateFile
{
    private const string KEEP_A_CHANGELOG = "https://keepachangelog.com/en/1.1.0/";
    private const string SEMANTIC_VERSIONING = "https://semver.org/spec/v2.0.0.html";


    public const string Initial =
        @"# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog]("+KEEP_A_CHANGELOG+@"),
and this project adheres to [Semantic Versioning]("+SEMANTIC_VERSIONING+@").

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## ["
        + Constants.Unreleased
        + @"]
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
}
