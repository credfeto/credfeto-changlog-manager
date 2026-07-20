# Coverage Ratchet Instructions

> Load when: acting as Orchestrator running the **AI Coverage** board phase, capturing/posting a coverage baseline as part of issue-to-PR creation, or re-baselining a coverage comment after a rebase.

[Back to Global Instructions Index](index.md)

## Overview

The AI Coverage phase is a whole-repo ratchet: each orchestrated language's overall line-coverage percentage on the PR branch must be **>=** that language's percentage on `main`, recomputed live every time (no stored/committed baseline file, no Codecov or similar external service). This is **not** a repeat of the diff-coverage check already performed by the Code Tester role ([agent-roles.instructions.md](agent-roles.instructions.md#code-tester)), which only verifies new/changed lines are covered; the ratchet also catches a deleted test or a refactor that drops coverage of code the diff never touched.

Percentages are compared **per language**, never blended into one combined figure (a .NET percentage and a Python percentage are not commensurable). A language with no code or tests present in the repo is skipped. Shell is out of scope (see [Shell](#shell-excluded)).

There is no persisted state between phase invocations beyond what is written into a GitHub PR comment: every AI Coverage phase invocation is a fresh, memoryless session. The only place the main-branch numbers survive between the baseline capture and the later comparison is the `## Coverage Baseline (main)` PR comment defined below.

## Main Coverage Baseline Capture (MANDATORY)

### Initial Capture (Pre-Work Baseline Check)

Capture `main`'s coverage numbers **while still checked out on `main`**, as part of the existing [Pre-Work Baseline Check](git.instructions.md#pre-work-baseline-check-mandatory-before-starting-any-work), before the work branch is created:

1. Complete the normal Pre-Work Baseline Check (hook run, auto-fix/failure handling) on `main`.
2. For each orchestrated language present in the repo (.NET, Node, Python), run that language's [overall coverage extraction](#per-language-overall-coverage-extraction) procedure and record the resulting percentage plus the current `main` commit SHA (`git -C <repodir> rev-parse HEAD`).
3. Create the work branch as normal.
4. Once the PR exists (PR Submitter step, [agent-roles.instructions.md](agent-roles.instructions.md#pr-submitter)), post the numbers captured in step 2 as a [Coverage Baseline PR comment](#coverage-baseline-pr-comment-format-mandatory).

Steps 1-4 happen within one continuous session, per the memorylessness rule above.

### Re-Baseline After a Rebase (Checkout-Swap)

When [When to Rebase](git-rebasing.instructions.md#when-to-rebase) determines `origin/main` has advanced and the branch is rebased, the previously posted baseline comment is stale (it no longer reflects current `main`). Refresh it immediately after the rebase completes, using a plain checkout swap, never `git worktree` (banned repo-wide, see [Avoid git worktree](git.instructions.md#avoid-git-worktree)):

1. Confirm the working tree is clean (the rebase has already completed and been verified) so nothing is lost by switching branches.
2. `git -C <repodir> checkout main`
3. `git -C <repodir> merge --ff-only origin/main` (fast-forward the local `main` ref using the `origin/main` already fetched by the rebase that triggered this re-baseline, without a redundant network fetch).
4. Run the [per-language extraction](#per-language-overall-coverage-extraction) procedure again.
5. `git -C <repodir> checkout <branch>` to switch back to the work branch.
6. Post a fresh [Coverage Baseline PR comment](#coverage-baseline-pr-comment-format-mandatory). Do not edit or delete the previous baseline comment; the AI Coverage phase always uses the **most recent** `## Coverage Baseline (main)` comment on the PR.

## Coverage Baseline PR Comment Format (MANDATORY)

Post exactly this structure (values illustrative):

```text
## Coverage Baseline (main)

| Language | Coverage |
| --- | --- |
| .NET | 82.1% |
| Node | n/a (no code) |
| Python | 74.3% |
| Shell | excluded |

Captured at commit `<main-sha>` on <ISO-8601 date>.
```

- Include every orchestrated language (.NET, Node, Python, Shell) as a row, even when skipped, so the table is unambiguous about what was and was not measured.
- Use `n/a (no code)` for a language with no code/tests in the repo.
- Use `excluded` for Shell (always; see [Shell](#shell-excluded)).
- `<main-sha>` is the full commit SHA captured in step 2 of the initial capture (or the re-measured SHA after a re-baseline).

## Per-Language Overall Coverage Extraction

### .NET

Collect each unit test project's `.cobertura.xml` as normal ([Code Coverage](dotnet.instructions.md#code-coverage-mandatory)), then generate the combined report and read its `summary.linecoverage` field as described in [Coverage Reporting with reportgenerator](dotnet.instructions.md#coverage-reporting-with-reportgenerator).

Skip .NET entirely if the repo has no `*.Tests` project ([Identifying Test Projects](dotnet.instructions.md#identifying-test-projects-mandatory)).

### Node

Pinned as **Vitest** with the **`@vitest/coverage-v8`** provider (no Node test runner or coverage tool was previously pinned anywhere in these instructions; this follows credfeto's direction on [credfeto/cs-template#992](https://github.com/credfeto/cs-template/issues/992) to "pick a tool and specify it in ai instructions"). Adding these packages to a specific repo's `package.json` for the first time still goes through the normal [Third-Party Packages Require Human Approval](packages.instructions.md#third-party-packages-require-human-approval-mandatory) review; this section only fixes which tool to propose, not a blanket pre-approval to install it.

Configure the `json-summary` reporter alongside whatever other reporters the repo already uses, so it is calculated in the ratchet's terms in every language:

```typescript
// vitest.config.ts
export default defineConfig({
  test: {
    coverage: {
      provider: "v8",
      reporter: ["text", "json-summary"],
    },
  },
});
```

Run and extract:

```bash
npx vitest run --coverage
jq '.total.lines.pct' coverage/coverage-summary.json
```

`coverage/coverage-summary.json` is written by the `json-summary` reporter (relative to `coverage.reportsDirectory`, which defaults to `coverage/`); `.total.lines.pct` is the overall line-coverage percentage across the whole run.

Skip Node entirely if the repo has no `package.json` with a configured test runner.

### Python

Pinned as **`coverage.py`**, run via `pytest` (the already-standard Python test runner referenced elsewhere in these instructions):

```bash
coverage run -m pytest
coverage report --format=total
```

`coverage report --format=total` prints only the overall percentage as a single number (no other text), so no further extraction step is needed.

Skip Python entirely if the repo has no Python test suite.

### Shell (Excluded)

Shell is excluded from the coverage ratchet entirely, per credfeto's explicit direction on [credfeto/cs-template#992](https://github.com/credfeto/cs-template/issues/992). Do not attempt to measure shell/bats coverage for this phase; always record it as `excluded` in the [baseline comment](#coverage-baseline-pr-comment-format-mandatory) and never include it in the [phase decision](#ai-coverage-phase-decision-procedure-mandatory) comparison.

## AI Coverage Phase Decision Procedure (MANDATORY)

Run this only when the Workflow board is at **AI Coverage** (see [agent-roles.instructions.md](agent-roles.instructions.md#pr-workflow-ai-review-loop)):

1. Find the most recent `## Coverage Baseline (main)` comment on the PR (`gh pr view <number> --repo <owner/repo> --json comments --jq '[.comments[] | select(.body | startswith("## Coverage Baseline (main)"))] | last'`). If none exists, treat this as a bug in an earlier phase: post a comment explaining no baseline was ever captured, add `Blocked`, and stop.
2. For each language present in that comment as a real percentage (not `n/a` or `excluded`), run the same [extraction procedure](#per-language-overall-coverage-extraction) against the branch's current (already checked out) working tree.
3. Compare branch vs. baseline per language:
   - Any language where branch **<** baseline: the ratchet fails.
   - All present languages branch **>=** baseline: the ratchet passes.
4. **On failure**: post a status comment in the form `<lang> <branch-pct>% < main <baseline-pct>% - returning to Development` (one line per failing language), move the board back to **Development**, and stop.
5. **On success**: move the board to **Human Review**, post a one-line status comment (`Coverage ratchet passed - advancing to Human Review`), and stop.
6. **Round cap**: see [Phase D step 5](agent-roles.instructions.md#phase-d-ai-coverage-up-to-max_review_iterations-rounds) for the round-counting and `Blocked` label escalation rule.
