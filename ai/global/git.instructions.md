# Git Instructions

[Back to Global Instructions Index](index.md)

## Branching

- All new work must be done in a branch. Never commit directly to `main`.
- Before making any changes, ensure the current branch is `main` and is up-to-date with `origin`.
- Until there is an explicit change in task, continue working in the same branch.

## Branch Naming

- Branch names must follow the format `<type>/<name>`, mirroring Conventional Commits types, e.g.:
  - `feature/add-user-auth`
  - `fix/null-pointer-on-login`
  - `chore/update-dependencies`
  - `refactor/simplify-payment-flow`
- If the branch relates to a GitHub issue, include the issue number in the name, e.g. `fix/123-null-pointer-on-login`.
- If working on a branch that fixes multiple GitHub issues (e.g. a feature with several related bug fixes), each individual commit that fixes an issue should reference that issue number in the commit message body.

## Commits

- Changes should be worked on in small increments and committed one by one.
- Commits should be as small as possible — one logical change per commit:
  - When achieving 100% coverage, commit and push each file as soon as it reaches 100% coverage individually; do not batch multiple files into one commit.
  - Dead/unreachable code removal must be a separate commit from test changes, made only after running tests on the entire handler or app to confirm no code path accesses it; removing several methods or functions should be done one per commit.
  - For shared code removal, first verify the entire codebase does not access it — this can only be done once all apps and handlers have 100% code coverage; each shared removal is its own commit.
  - Never amend an existing commit; always create a new one.
- After every commit, push the changes to `origin` immediately.

## Commit Message Format

- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format for all commit messages.
- Always include the user's original prompt verbatim in the commit description (body), prefixed with `Prompt: `. Do not include it in the commit title.
