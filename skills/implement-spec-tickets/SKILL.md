---
name: implement-spec-tickets
description: Implement every ticket belonging to one spec by discovering its dependency graph, dispatching parallel worktree agents, and coordinating merge and closure until the full spec is complete.
disable-model-invocation: true
argument-hint: "<SPEC-TICKET-ID>"
---

# Implement Spec Tickets

The user invokes this skill as:

```text
/implement-spec-tickets 29
```

Treat the argument as the parent spec ticket. Run its complete implementation graph unattended.
This session is the **orchestrator**: it reads tracker state, delegates one ticket per agent,
controls integration, and advances the dependency frontier. It never implements or reviews
application code itself.

## 1. Establish the run

1. Require exactly one numeric ticket ID, `#ID`, or full tracker URL.
2. Verify the repository root and HEAD, and choose the **parent branch**: the branch this spec's
   work collects on. A milestone ticket or a project memory can name a long-lived effort branch
   (for example `feature/<name>`); use that over the default branch. Search both
   before choosing, and ask once when they disagree with the checked-out branch.
3. Create the **integration worktree**, where every merge of this run happens:
   `git worktree add <worktree-folder>/spec-<SPEC-ID> <parent-branch>` (see Worktrees). Git
   refuses when the parent branch is checked out in another folder. Show the user git's message
   and the fix, `git -C <that folder> switch --detach` (same commit, files and uncommitted edits
   untouched), and ask once: (a) run it for them now, or (b) they run it themselves. Another
   session may be working in that folder, so the switch waits for their answer. Then create the
   integration worktree.
4. Read the repository's issue-tracker instructions, normally
   `docs/agents/issue-tracker.md`, and use the documented CLI.
5. Fetch the full spec ticket, including comments or notes.
6. Record your tracker username (`glab api user`, field `username`); section 4 claims tickets with
   it.

This step is complete when the spec, tracker, repository root, parent branch, starting HEAD,
username and integration worktree are known.

## 2. Generate the graph in a subtask

Launch one synchronous graph-discovery subagent. Give it the spec ID, repository root, and tracker
instructions. It performs tracker research only and returns a compact graph; it does not edit code,
mutate tickets, create worktrees, or launch implementation agents.

### GitLab ticket convention

For this repository, discover the child set as follows:

1. Read the spec with `glab issue view <SPEC-ID> --comments -F json`.
2. Collect candidate IDs from automatic notes shaped like `mentioned in issue #<ID>`. Also include
   native child relationships if the tracker exposes them.
3. Fetch every candidate once.
4. Include a candidate only when it declares the requested spec as its parent, either in a
   `## Parent` section or an inline `Part of #<SPEC-ID>` line. The reference is `#<SPEC-ID>` or a
   URL ending in `/work_items/<SPEC-ID>` or `/issues/<SPEC-ID>`. A backlink alone does not make an
   implementation ticket.
5. Exclude referenced audits, decisions, research, or background issues that do not declare that
   parent.
6. Parse each included ticket's blockers, from a `## Blocked by` section or an inline
   `**Blocked by:**` line:
   - `None - can start immediately` means no blockers.
   - Markdown links ending in `/work_items/<ID>` or `/issues/<ID>`, and local `#<ID>` references,
     are blocking edges.
   - Build only explicit edges. Never infer dependencies from ticket order, titles, or likely code
     ownership.

SPEC #29 demonstrates the convention: issue #21 appears as a backlink but has no `## Parent`
section, while #30-#43 declare #29 as their parent and define their dependencies in
`## Blocked by`.

The graph subagent returns:

```text
SPEC #<ID> dependency map - A -> B means A must finish before B.

<compact edge list>

Execution order

Wave 1: <tickets>
Wave 2: <tickets>

Excluded references: <ticket and reason>
```

It also returns a node table containing ticket ID, title, state, URL, acceptance criteria, and
blockers. It writes every ticket's full body and comments to a file in the scratchpad and returns
the paths, so bodies reach workers without passing through the orchestrator's context.

Finally it returns the **corrections**: every fact in a ticket body or comment that contradicts
the spec's text, with its source and the spec text it replaces. Closed blockers are the usual
source: a spec named one host for the package feed, a closed blocker ticket had moved it to
another host, and the user was asked for the wrong credentials.

Validate before execution:

- every included ticket points back to the requested spec;
- every included open ticket appears once in the topological waves;
- closed tickets satisfy dependencies but are not dispatched;
- every blocker is an included ticket or an explicitly identified external blocker;
- there are no missing tickets, self-edges, conflicting blocker declarations, or cycles.

If validation fails, report the exact evidence and stop. Otherwise print the graph, freeze it as
the run's source of truth, and continue immediately without requesting approval.

Then check the spec's preconditions (a "Waits for" line, a required credential, tool or workload)
against the corrections and against this machine, before the first dispatch. A precondition the
user must meet goes to them as a **human step** (see Failure boundaries). Frontier tickets that do
not need it start meanwhile.

## 3. Track the frontier

Persist one row per implementation ticket:

```text
ticket | blockers | state | claimed | agent | worktree | branch | commit | integrated | closed | note
```

Use states `satisfied`, `pending`, `implementing`, `ready-to-integrate`, `integrating`, `closed`,
`blocked`, and `failed`.

The **frontier** is every pending open ticket whose blockers are all:

- already closed before this run; or
- verified merged into the parent branch and closed during this run;

and that is **unclaimed**, or claimed by this run. As in wayfinder, the assignee _is_ the claim: an
open ticket with no assignee is unclaimed. A ticket claimed outside this run is deferred: report it
as `claimed by <user>` and leave it alone.

Re-fetch tracker and Git evidence after an interruption. Fresh evidence overrides stale persisted
state.

## 4. Implement the frontier in parallel

For every frontier:

1. **Claim** each ticket first, before any other work on it, so concurrent sessions skip it.
   Re-fetch it, confirm it is still open and unclaimed, then
   `glab issue update <ID> --assignee <username>`. Record the claim in the ticket's row.
2. Create one disposable worktree per ticket (see Worktrees).
3. Spawn all frontier tickets together as background general-purpose agents. Assign exactly one
   ticket to each agent.
4. Give each agent its worktree, branch, ticket body path and acceptance criteria, parent spec,
   blocker list, what its merged blockers delivered, the corrections that touch its work, tracker
   instructions, repository instructions, and the worker brief below. Put the corrections under
   their own heading, above the brief. They override the spec wherever the two disagree.
5. Require a commit containing the ticket number.
6. The agent's work ends at `ready-to-integrate`. Merging, closing the ticket, removing the
   worktree and touching the integration worktree belong to integration (section 5).

The orchestrator performs no application-code exploration, edits, tests, or review while ticket
agents work.

### Worktrees

Every ticket gets a **disposable worktree**: a checkout only its agent writes to, so parallel
agents and the user's other sessions never share files. The worktree folder is the one the
repository's `AGENTS.md` names, `.worktrees/` in the repository root when it names none.

- The orchestrator creates it, serially, from the integrated parent HEAD:
  `git worktree add -b agent/spec-<SPEC-ID>-ticket-<ID> <worktree-folder>/ticket-<ID> <integrated-HEAD>`.
- The agent explores, builds, tests, reviews, updates the ticket and commits only there.
- The branch outlives the worktree. After a verified merge, remove both. After `blocked` or
  `failed`, remove the worktree, keep the branch, and name it in the claim-release note.
- After an interruption, `git worktree list` shows what the last run left. Continue from the
  ticket's branch, not from the persisted table.

### Worker brief

The brief carries the whole workflow. `/implement` is user-only (`disable-model-invocation`), so
an agent cannot start it; the rules below replace it. Pass them to every worker:

- **Test-first** at the seams the ticket names. The `mattpocock-skills:tdd` skill is available.
- **Small context.** Read code with `mcp__graphmind__gm_fn` / `gm_outline`, or Read with
  offset+limit on files over ~500 lines. graphmind indexes the main checkout, so Read the worktree
  copy of any file this branch has changed. Pipe build and test output through `tail -40`.
- **Affected tests only.** Affected tests are the test files you changed, plus the test classes
  that reference a type, file or script you changed. Find them with
  `mcp__graphmind__gm_diff_impact` or a grep for the changed names. Run them as one filtered
  command (`dotnet test --filter "FullyQualifiedName~A|FullyQualifiedName~B"`). After a fix,
  rerun only what failed, then the whole filter once before committing.
- **The full acceptance suite runs once per spec**, by the orchestrator, at completion. Parallel
  full runs in worktrees exhausted machine memory in an earlier run and cost minutes per run.
- **Review gate.** List changed files with `git diff --name-only <base>...HEAD`.
  - Skip review when every file is a test, doc, fixture, sample data, CI file or generated file.
    Report `review: skipped (<reason>)`.
  - Otherwise run `mattpocock-skills:code-review` against `<base>`. Project files (`.csproj`,
    `package.json`) and build scripts count as production code. Tell the reviewers they are
    read-only: they read the diff and the ticket, and run no build or test. Fix real findings,
    then rerun the affected tests.
- **Secrets stay with the user.** Work only with credentials already in the process
  environment. When a step needs one that is missing, stop and report it as a blocker.
- **Hand-back, at most 30 lines:** state, commit SHA, changed files, the exact test command with
  pass/fail counts, review done or skipped, each acceptance criterion met / not met / blocked /
  **deferred proof** (a criterion only a later run can prove, such as the full suite or a CI
  pipeline; name that run) with one line of evidence, fact corrections found in the ticket or
  spec, **follow-ups** (docs or glossary text the change made stale, or that the spec asks for
  after the build), blockers.

## 5. Integrate serially

Only one ticket may touch the integration worktree at a time. Among ready tickets, use ascending numeric
ticket order.

Integration is mechanical, so it runs in a **fresh context**. The worker already holds 150k–500k
tokens, and every integration turn would resend all of it. Spawn one new integrator agent per
ticket and give it the integration worktree, the recorded integrated HEAD, the ticket branch, and the
worker's hand-back (test command, summary, acceptance-criteria state). It:

1. Confirms the parent branch is at the recorded integrated HEAD.
2. Stages the merge with `git merge --no-ff --no-commit <branch>`.
3. Resolves mechanical conflicts only, such as two tickets appending to the same list (keep both
   sides).
4. Builds, then runs the worker's test command plus the affected tests of any file it touched
   while resolving a conflict.
5. Commits the merge only when build and tests pass; the message contains the ticket number. On
   any other conflict or a failing test: `git merge --abort` and report it. The parent stays at
   the recorded HEAD, and the orchestrator sends the failure to the original worker as its one
   focused follow-up.
6. Posts the worker's summary and final acceptance-criteria state on the ticket, then closes it.
   Each deferred proof stays an unticked box that names the run that will prove it.
7. Returns the merge SHA, test result, and closure evidence in at most 15 lines.

Independently verify the merge commit (its two parents) and the closed ticket. Then mark the node
`closed`, remove its worktree, delete its local branch, recompute the frontier, and dispatch newly
eligible tickets immediately. A dependent does not wait for unrelated ready tickets once all of its
own blockers are closed.

## Failure boundaries

- Open, unmerged, failed, blocked, or unvalidated tickets never satisfy dependencies. A ticket is
  validated by its verified merge and its passing affected tests; a deferred proof still waiting
  for the final run or a pipeline does not hold back its dependents.
- Send one focused follow-up for an actionable implementation or validation failure. A second
  failure stops that dependency branch.
- When a ticket stops as `blocked` or `failed`, release its claim with
  `glab issue update <ID> --unassign` and post one note saying why and naming its kept branch, so
  the next session can take it.
- Continue unrelated branches whose dependencies remain satisfied.
- Stop affected work on tracker failure, unexpected parent-branch movement, unresolved merge
  conflict, missing blocker, or graph cycle.
- If unfinished tickets remain but there is no frontier and no active agent, report the deadlock
  and each ticket's unsatisfied blockers.

### Human steps

Some preconditions only the user can meet: a token, an allowlist entry, a workload install. Name
the exact thing needed and where it comes from, checked against the corrections. Give the command
for the user to run in a terminal of their own. The token then stays out of the conversation, and
the session's safety checks block agents from reading one anyway. The command reads the token from
its variable when it runs (`%VAR%` in a `nuget.config`, `$env:VAR` in a script), so no file on
disk ever holds it. Two facts save round trips:

- A user-level environment variable reaches only processes started after it was set. Claude Code
  and an IDE terminal that were already open do not see it. A script the user runs, which reads the
  variable from the user scope, works at once.
- A package restore the user runs once fills the machine's package cache. Agents then build from
  the cache with no credential.

Park only the tickets that need the step; the rest of the frontier keeps going.

## Keep the orchestrator context small

- A task-notification that only says an agent has not reported yet is not news: end the turn
  without text. A turn that dispatches, merges or closes is news: it ends with one status line.
- After the final report, post the open decisions, corrections, follow-ups and publish state as a
  note on the spec ticket, then tell the user in one sentence to /clear and continue from that
  note. A run that stops for a human step posts the same note first, since a restart loses this
  context.

## Completion

Continue until every child ticket is either verified merged and closed or was already closed before
the run. Run the repository's full integration command once on the final parent HEAD, in the
strictest variant any acceptance criterion names (for example `-AsShipped`; a stricter variant
usually covers the plain one). If it fails, run only the failing tests on each merge commit from
`git log --first-parent --merges` to find the merge that broke them, and give that fix to a fresh
agent. Post the passing result on every ticket whose deferred proof it settles. Leave the parent
spec open unless repository tracker instructions explicitly require closing it.

Finish with the dependency map, per-ticket final state, merge SHAs, final validation result, and any
remaining blocker.

## Publish

Pushing is outward-facing, so ask the user how the work goes out: (a) a branch and a merge request
into the parent branch, or (b) a direct push. Before asking, list `git log <remote>/<parent>..HEAD`
and name every commit this run did not make. A local commit the user is holding back would
otherwise ride along.

- Push with `git push origin HEAD:<branch>`. Plain `-u` would move the local branch's upstream.
- The merge request states whatever its pipeline cannot prove, such as a local `-AsShipped` run
  when merge-request pipelines publish Debug.
- When the pipeline finishes, check each job and post its evidence on every ticket whose deferred
  proof it settles. Name the pipeline ID.
- After the push, remove the integration worktree. The user sees the result by checking out the
  parent branch.
