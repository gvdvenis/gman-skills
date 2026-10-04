# Worker brief

You implement one ticket of a spec, in your own worktree. The orchestrator gave you the worktree,
branch, ticket file path, acceptance criteria, parent spec, blockers, what those blockers
delivered, the corrections that touch your work, and the tracker and repository instructions.
The corrections override the spec wherever the two disagree.

Explore, build, test, review, update the ticket and commit only in your worktree. When the
tracker keeps tickets as files in the repository, leave the ticket file alone: the integrator
records your hand-back on it. Your work ends
at `ready-to-integrate`: merging, closing the ticket, removing the worktree and touching the
integration worktree belong to the integrator.

## Rules

- **Test-first** at the seams the ticket names. Use the `mattpocock-skills:tdd` skill when it is
  installed.
- **Small context.** Read code through the repository's code-index tool when one is set up (for
  example graphmind's `mcp__graphmind__gm_fn` and `mcp__graphmind__gm_outline`). Otherwise Read
  with offset+limit on files over ~500 lines. A code index built from the main checkout does not
  see this branch's changes, so Read the worktree copy of any file this branch has changed. Pipe
  build and test output through `tail -40`.
- **Affected tests only** (test impact analysis). Find the tests that reference what you changed
  with an LSP find-references on each changed type or member, run in the worktree. Fall back to
  the code index's impact query when there is one (graphmind: `mcp__graphmind__gm_diff_impact`),
  then to a grep for the changed names. Run them as one filtered command (for example
  `dotnet test --filter "FullyQualifiedName~A|FullyQualifiedName~B"`). After a fix, rerun only
  what failed, then the whole filter once before committing.
- **No tests for the change?** Report `tests: none` and check each acceptance criterion directly,
  for example with a grep.
- **No full suite.** The orchestrator runs it once per spec, at completion. Parallel full runs
  across worktrees can exhaust machine memory and cost minutes each.
- **Review gate.** List changed files with `git diff --name-only <base>...HEAD`.
  - Skip review when every file is a test, doc, fixture, sample data, CI file or generated file.
    Report `review: skipped (<reason>)`.
  - Otherwise call `Skill(skill: "mattpocock-skills:code-review", args: "<base>")`. Spawn your
    own read-only review agent on the diff and the ticket only when that skill is missing from
    your skill list. Project files
    (`.csproj`, `package.json`) and build scripts count as production code. Tell the reviewers
    they are read-only: they read the diff and the ticket, and run no build or test. Fix real
    findings, then rerun the affected tests.
- **Wait inside the turn.** Start a long build or test so it writes its exit code when done,
  with its files in the temp folder, outside the repository:
  `(<command> > "$TMP/t<ID>.log" 2>&1; echo $? > "$TMP/t<ID>.exit") &`. Then wait on that file
  with `until [ -f "$TMP/t<ID>.exit" ]; do sleep 30; done` (in Claude Code, run it through the
  Monitor tool; repeat the wait when it times out), and read `tail -40 "$TMP/t<ID>.log"`. End a
  turn only after every background command you started has finished; each one still running
  sends the orchestrator a notification.
- **Secrets stay with the user.** Work only with credentials already in the process environment.
  When a step needs one that is missing, stop and report it as a blocker.
- **Commit** with the ticket number in the message.

## Hand-back

At most 30 lines:

```text
state: ready-to-integrate | blocked | failed
commit: <SHA>
changed: <files>
tests: <exact command> - <passed>/<failed>
review: done | skipped (<reason>)
criteria:
  - <criterion>: met | not met | blocked | deferred proof (<run that proves it>) - <one line of evidence>
corrections: <facts in the ticket or spec that turned out wrong>
follow-ups: <docs or glossary text the change made stale, or that the spec asks for after the build>
blockers: <none | what and why>
```

A **deferred proof** is a criterion only a later run can prove, such as the full suite or a CI
pipeline.
