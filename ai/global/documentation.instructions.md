# Documentation Instructions

[Back to Global Instructions Index](index.md)

## README

- `README.md` must contain a clear description of what the repository is for and be kept up to date as things change.
- If the `README.md` still contains template placeholder content (e.g. describes itself as a template), it must be rewritten automatically to reflect the actual repository when work begins.
- `README.md` must include the following links and badges where applicable:
  - GitHub Actions workflow badges for **debug** and **release** builds/tests (as appropriate for the repository).
  - Link to the head of any project documentation (e.g. `docs/`).
  - Link to build instructions in the documentation.
  - Link to the contributing guide — use the standard filename (`CONTRIBUTING.md`).
  - Link to the security policy — use the standard filename (`SECURITY.md`).
  - Link to `CHANGELOG.md`.

## CHANGELOG

- Maintain `CHANGELOG.md` in [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format to track changes and updates to the project.
- Purely documentation changes should not be added to the changelog.
- Use the `Credfeto.Changelog.Cmd` dotnet tool to update the changelog, regardless of the project's primary language — this ensures consistency:
  ```
  dotnet changelog -f CHANGELOG.md -a <ChangeType> -m "<reason for change>"
  ```
- Each task should be updated in `CHANGELOG.md`, **unless**:
  - This is the `git@github.com:credfeto/cs-template.git` repository — the `CHANGELOG.md` there is expected to be kept blank, ready for new repositories using the template.
  - It is purely a documentation change and does not affect any production code.
  - It is an AI instructions change.

## Additional Documentation

- Documentation should be generated and maintained for repositories other than `git@github.com:credfeto/cs-template.git`.
- The `git@github.com:credfeto/cs-template.git` repository must not contain a `docs` folder — documentation is explicitly for derived repositories only.
- Documentation and architecture diagrams other than `README.md` and `CHANGELOG.md` should be placed in the `docs` folder.
- Documentation should be kept up-to-date as changes are applied and be created when missing.
- Do not specify every detail (e.g. package version numbers, every single file) in architecture diagrams — folder structure is sufficient.
- Where diagrams are generated, prefer vector formats (e.g. SVG) over raster formats (e.g. PNG, JPEG).
