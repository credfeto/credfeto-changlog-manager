# Changelog manager .net tool

Changelog manager is a .net tool that works on all supported .net platforms (windows/linux/mac).

## Build Status

| Branch  | Status                                                                                                                                                                                                                                  |
|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| main    | [![Build: Pre-Release](https://github.com/credfeto/changelog-manager/actions/workflows/build-and-publish-pre-release.yml/badge.svg)](https://github.com/credfeto/changelog-manager/actions/workflows/build-and-publish-pre-release.yml) |
| release | [![Build: Release](https://github.com/credfeto/changelog-manager/actions/workflows/build-and-publish-release.yml/badge.svg)](https://github.com/credfeto/changelog-manager/actions/workflows/build-and-publish-release.yml)             |

## Release Notes

See [CHANGELOG.md](CHANGELOG.md) managed by this tool.

## Installation

### Install as a global tool

```shell
dotnet tool install Credfeto.ChangeLog.Cmd
```

To update to latest released version
```shell
dotnet tool update Credfeto.ChangeLog.Cmd
```

### Install as a local tool

```shell
dotnet new tool-manifest
dotnet tool install Credfeto.ChangeLog.Cmd --local
```

To update to latest released version
```shell
dotnet tool update Credfeto.ChangeLog.Cmd --local
```

## Usage

Common notes

- The parameter `-changelog CHANGELOG.md` is optional and the changelog will be searched for in the current git repo.  Its is given explicitly in the examples assuming CHANGELOG.md is in the current directory.
- If CHANGELOG.md doesn't exist calling `-add` with and when using `-changelog CHANGELOG.md` will create a file at that location.  It will error if `-changelog CHANGELOG.md` is not used.


### Extracting Release Notes

#### Extract the release notes for a pre-release build
```shell
dotnet changelog --changelog CHANGELOG.md --extract RELEASE_NOTES.md --version 1.0.1.27-master
```

#### Extract the release notes for a release build
```shell
dotnet changelog --changelog CHANGELOG.md --extract RELEASE_NOTES.md --version 1.0.2.77
```

### Add a new entry to the [Unreleased] section

Note the value of the `--add` parameter matches exactly the heading section in the [Unreleased] section.  It will not create/update a new section if it that section does not exist.

```shell
dotnet changelog --changelog CHANGELOG.md --add Added --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Changed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Fixed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Removed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add "Deployment Changes" --message "Change description"
```

### Remove an entry from the [Unreleased] section

Note the value of the `--add` parameter matches exactly the heading section in the [Unreleased] section. It will not
create/update a new section if it that section does not exist.

```shell
dotnet changelog --changelog CHANGELOG.md --remove Added --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Changed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Fixed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Removed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove "Deployment Changes" --message "Change description"
```

### Create a release

This pulls out all the changes in the [Unreleased] section and adds them to to the release given by the version number
and sets the release date to be today.

```shell
dotnet changelog -changelog CHANGELOG.md --create-release 1.2.3
```

This pulls out all the changes in the [Unreleased] section and adds them to to the release given by the version number
and sets the release date to pending.

```shell
dotnet changelog -changelog CHANGELOG.md --create-release 1.2.3 --pending
```


Notes:
- If the specified version already exists then an error will occur.
- If the specified version is older than the latest release then an error will occur.

### Print Unreleased Content to Console

Prints the current [Unreleased] section to stdout.

```shell
dotnet changelog --changelog CHANGELOG.md --un-released
```

### Lint the Changelog

Validates the changelog format and reports any errors.

```shell
dotnet changelog --changelog CHANGELOG.md --lint
```

Lint with additional allowed sections (comma-separated):

```shell
dotnet changelog --changelog CHANGELOG.md --lint --additional-sections "Security,Performance"
```

Lint and automatically fix formatting issues (rewrites the file):

```shell
dotnet changelog --changelog CHANGELOG.md --lint --fix
```

Lint with additional sections and auto-fix:

```shell
dotnet changelog --changelog CHANGELOG.md --lint --additional-sections "Security,Performance" --fix
```

Notes:
- `--lint` exits with a non-zero exit code if any errors are found.
- `--fix` rewrites the changelog to correct ordering and missing sections in `[Unreleased]`; it then re-lints to confirm the file is valid.

### Check insert position of changes are all in [Unreleased] section

Where `origin/target` is the name of the branch a PR is to be merged into.

```shell
dotnet changelog --changelog CHANGELOG.md --check-insert origin/target
```

Notes

- Assumes that the branch to be merged into is up-to-date with the latest changes.
- Assumes that all entries have been committed to the local repo.
- All changes in non-committed (staged and unstaged) files are ignored.

## Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->