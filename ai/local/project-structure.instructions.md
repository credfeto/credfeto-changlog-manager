# Project Structure Instructions

[Back to index](index.md)

## Credfeto.ChangeLog Folder Conventions

Files within the `Credfeto.ChangeLog` project must be placed in the appropriate folder based on their role.

### Root (namespace `Credfeto.ChangeLog`)

Public-facing static classes that form the library's API surface:

- `ChangeLogChecker` — checks changelog content against branch/git state
- `ChangeLogDetector` — detects changelog files in a working directory
- `ChangeLogFixer` — fixes formatting issues in a changelog
- `ChangeLogLinter` — lints changelog content and returns `LintError` results
- `ChangeLogReader` — reads structured data from a changelog
- `ChangeLogUpdater` — adds, removes, and promotes changelog entries

`AssemblySettings.cs` also lives at the root.

### `Exceptions/`

All custom exception types. Every class must inherit (directly or indirectly) from `Exception`. No logic — throw-site helpers go in `Helpers/Throws.cs`.

Examples: `BranchMissingException`, `InvalidChangeLogException`, `ReleaseAlreadyExistsException`.

### `Extensions/`

Static classes whose sole purpose is to declare extension methods. The class name must end in `Extensions`. Each class should extend a single type or tightly related group of types.

Examples: `ChangeLogHeadingExtensions` (extensions on `string` for changelog headings), `TextBlockToLines` (extensions for splitting/joining text lines).

### `Helpers/`

Internal static utility classes that are not extension methods. Permitted contents: constants, frozen/static lookup data, regex definitions, pure computation helpers, and throw-site helpers.

Examples: `ChangeLogSections` (frozen section name sets), `CommonRegex`, `Constants`, `GitRepository`, `RegexSettings`, `RegexTimeouts`, `Throws`, `Unreleased`.

Classes here must not depend on anything in `Services/` — they are lower-level utilities.

### `Models/`

Record, struct, or class types that carry data with no or minimal behaviour. All types here should be public.

Examples: `LintError` (a lint result with a line number and message).

Types that are only ever used as private implementation details within a single class should be nested inside that class instead of placed here.

### `Services/`

Internal service interfaces and their implementations. An interface and its concrete implementation(s) belong in the same folder.

Examples: `IChangeLogLoader` (interface), `FileSystemChangeLogLoader` (implementation).

Internal helpers scoped to a single service (e.g. `CompareSettings`, `BuildNumberHelpers`, `TemplateFile`) live here alongside the service they support rather than in `Helpers/`.
