---
name: implement-spec-tickets
description: Implements every ticket belonging to one spec by discovering its dependency graph, dispatching parallel worktree agents, and coordinating merge and closure until the full spec is complete.
disable-model-invocation: true
argument-hint: "<SPEC-TICKET-ID or spec folder>"
# written for: Opus 5.5 / Sonnet 5.5; not tested on Haiku
---

# Implement Spec Tickets

Treat the argument as the parent spec ticket. Run its complete implementation graph unattended.
This session is the **orchestrator**: it reads tracker state, delegates one ticket per agent,
controls integration, and advances the dependency frontier. It never implements or reviews
application code itself.

Each subagent gets its own brief in `references/`. Paths are relative to this skill's folder;
give agents the absolute path, built from the folder shown when this skill loaded.

Copy this checklist into the first status message and tick lines as they finish:

- [ ] 1 Run established
- [ ] 2 Graph validated and frozen; preconditions met or parked
- [ ] 3 Frontier table built
- [ ] 4 Frontier claimed and dispatched
- [ ] 5 Each finished ticket merged and closed, or its failure sent back
- [ ] Completion: full suite passed on the final integrated HEAD
- [ ] Publish

After every merge in step 5, go back to step 3: recompute the frontier and dispatch it. Move to
Completion only when no ticket is pending, implementing or integrating.

Terms used throughout:

- **Parent branch**: the branch this spec's work collects on.
- **Integrated HEAD**: the parent branch's commit after the last verified merge of this run. At
  the start it is the starting HEAD.
- **Full suite**: the repository's full integration command. It runs once, at completion.
- **SPEC-ID**: the spec's ticket number; for a local spec folder, the folder name.
- **Tracker instructions**: the repository's issue-tracker document, normally
  `docs/agents/issue-tracker.md`. Every tracker operation goes through it. Its "Wayfinding
  operations" section says how this repository claims, blocks and closes a ticket. When the
  repository has no tracker document, use local markdown: one file per ticket in
  `.scratch/<spec>/issues/`, with a `Status:` line and a `Blocked by` line or section.

## 1. Establish the run

1. Require exactly one spec reference: a ticket ID, `#ID`, a tracker URL, or, for a local
   markdown tracker, the spec's folder or file.
2. Verify the repository root and HEAD, and choose the parent branch. A milestone ticket or a
   project memory can name a long-lived effort branch (for example `feature/<name>`); use that
   over the default branch. Search both before choosing, and ask once when they disagree with
   the checked-out branch.
   List `git log <remote>/<parent-branch>..<starting-HEAD>`. Those commits go out with this run
   and cannot be left out later, so when the list is not empty, name each one and ask once
   whether the run may build on them.
3. Create the **integration worktree**, where every merge of this run happens:
   `git worktree add <worktree-folder>/spec-<SPEC-ID> <parent-branch>` (see Worktrees). Git
   refuses when the parent branch is checked out in another folder, usually the repository root.
   Then run `git -C <that folder> switch --detach` without asking, say so in one status line, and
   create the integration worktree. The worktree folder rules (see Worktrees) apply from this first worktree on.
4. Read the tracker instructions.
5. Fetch the full spec ticket, including comments or notes.
6. When the tracker claims by assignee, record your tracker username, so the frontier can tell
   this run's claims from other sessions'.

**Complete when** the spec, tracker instructions, repository root, parent branch, starting HEAD,
username and integration worktree are known.

## 2. Generate the graph in a subtask

Launch one graph-discovery subagent and wait for its result before step 3. The orchestrator
reads no ticket and researches no package feed or host itself; graph discovery covers both. Give it the spec reference, the repository root,
the tracker instructions, and the path of `references/graph-discovery.md` as its brief. It
returns the dependency map, the waves, a node table, the paths of the ticket files it wrote, and
the **corrections**: facts in tickets that contradict the spec.

Validate the returned graph yourself before execution:

- every included ticket points back to the requested spec;
- every included open ticket appears once in the waves;
- closed tickets satisfy dependencies but are not dispatched;
- every blocker is an included ticket or an explicitly identified external blocker;
- there are no missing tickets or conflicting blocker declarations, and the graph is a DAG.

If validation fails, report the exact evidence and stop. Otherwise print the graph, freeze it as
the run's source of truth, and continue immediately without requesting approval.

Then check the spec's preconditions (a "Waits for" line, a required credential, tool or workload)
against the corrections and against this machine, before the first dispatch. A precondition the
user must meet goes to them as a **human step** (see `references/human-steps.md`). Frontier
tickets that do not need it start meanwhile.

When a correction makes an acceptance criterion impossible (for example, the field it checks was
removed from the package), pick a substitute criterion, post it on the spec at once as an open
decision with the correction as its reason, and continue with the substitute.

**Complete when** the graph is printed and frozen, and every precondition is met or parked as a
human step.

## 3. Track the frontier

Keep one row per implementation ticket in `frontier.md` in the session's scratchpad folder
(without one: `<worktree-folder>/frontier-<SPEC-ID>.md`), and rewrite it after every state change:

```text
ticket | blockers | state | claimed | agent | worktree | branch | commit | integrated | closed | note
```

Use states `satisfied`, `pending`, `implementing`, `ready-to-integrate`, `integrating`, `closed`,
`blocked`, and `failed`.

The **frontier** is every pending open ticket whose blockers are all:

- already closed before this run; or
- verified merged into the parent branch and closed during this run;

and that is **unclaimed**, or claimed by this run. The claim is what the tracker's claim
operation writes: an assignee on GitLab or GitHub, `Status: claimed` on local markdown. An open
ticket without one is unclaimed. A ticket claimed outside this run is deferred: report it as
`claimed by <user>` and leave it alone.

The tracker and Git are the source of truth; the table is a cache. After an interruption, rebuild
it from them: `git worktree list` shows what the last run left.

### Tickets kept as files in the repository

This applies only to a tracker whose tickets are files in this repository, such as local
markdown. A tracker on a server (GitLab, GitHub) is unaffected. Every ticket file has one copy
that counts: the one on the parent branch.

- The orchestrator makes its ticket edits (claim, release, notes on the spec) in the integration
  worktree and commits each one (`tracker: claim <ID>`). Each such commit becomes the new
  integrated HEAD.
- Workers leave ticket files alone; their hand-back carries what goes on the ticket.
- The integrator writes the summary and closes the ticket inside the merge commit.

## 4. Implement the frontier in parallel

For every frontier:

1. **Claim** each ticket first, before any other work on it, so concurrent sessions skip it.
   Re-fetch it, confirm it is still open and unclaimed, then claim it with the tracker's claim
   operation. Record the claim in the ticket's row.
2. Create one disposable worktree per ticket (see Worktrees).
3. Spawn all frontier tickets together as background general-purpose agents. Assign exactly one
   ticket to each agent.
4. Give each agent its worktree, branch, the ticket file graph discovery wrote, acceptance
   criteria, parent spec, blocker list, what its merged blockers delivered, the corrections that touch its work, the
   tracker and repository instructions, and the path of `references/worker-brief.md` as its
   brief. Put the corrections under their own heading in the prompt.
5. A worker's job ends at `ready-to-integrate`, with a commit containing the ticket number and a
   hand-back in the brief's format.

The orchestrator performs no application-code exploration, edits, tests, or review while ticket
agents work. `/implement` is user-only (`disable-model-invocation`), so a worker cannot start it;
the worker brief replaces it.

**Complete when** every frontier ticket is claimed and has a running agent.

### Worktrees

Every ticket gets a **disposable worktree**: a checkout only its agent writes to, so parallel
agents and the user's other sessions never share files. The worktree folder is the one the
repository's `AGENTS.md` names, `.worktrees/` in the repository root when it names none. When
git does not ignore that folder yet, add it to `.git/info/exclude` (git's local ignore list,
never committed) before creating the first worktree.

- The orchestrator creates it, serially, from the integrated HEAD:
  `git worktree add -b agent/spec-<SPEC-ID>-ticket-<ID> <worktree-folder>/ticket-<ID> <integrated-HEAD>`.
- The branch outlives the worktree. After a verified merge, remove both. After `blocked` or
  `failed`, remove the worktree, keep the branch, and name it in the claim-release note. Remove
  a worktree with `git worktree remove <worktree-folder>/ticket-<ID>`; without `--force`, git
  refuses when it still holds changes.

## 5. Integrate through the merge queue

Finished tickets enter a merge queue: one merge at a time, in ascending ticket number.

Integration is mechanical, so it runs in a **fresh context**. Spawn one new integrator agent per
ticket. Give it the integration worktree, the integrated HEAD, the ticket branch, the worker's
hand-back, the tracker instructions, and the path of `references/integrator-brief.md` as its brief.

When the integrator reports a failed merge or failing tests, the parent branch is still at the
integrated HEAD. Send the failure to the original worker as its one focused follow-up.

Otherwise, independently verify the merge commit (its two parents) and the closed ticket. Then
mark the node `closed` and record the new integrated HEAD in `frontier.md`, remove its worktree
(`git worktree remove <worktree-folder>/ticket-<ID>`), delete its local
branch (confirm with `git merge-base --is-ancestor <branch> <parent-branch>`, then
`git branch -D`; plain `-d` compares against the detached checkout and refuses), recompute the frontier, and dispatch newly eligible tickets immediately. A dependent does
not wait for unrelated ready tickets once all of its own blockers are closed.

**Complete when** the ticket is verified merged and closed, or its failure has gone back to the
worker.

## Failure boundaries

- Open, unmerged, failed, blocked, or unvalidated tickets never satisfy dependencies. A ticket is
  validated by its verified merge and its passing affected tests; a deferred proof still waiting
  for the full suite or a pipeline does not hold back its dependents.
- Send one focused follow-up for an actionable implementation or validation failure. A second
  failure stops that dependency branch.
- When a ticket stops as `blocked` or `failed`, release its claim and post one note saying why
  and naming its kept branch, so the next session can take it.
- Continue unrelated branches whose dependencies remain satisfied.
- Stop affected work on tracker failure, parent-branch movement this run did not make, unresolved merge
  conflict, missing blocker, or graph cycle.
- If unfinished tickets remain but there is no frontier and no active agent, report the deadlock
  and each ticket's unsatisfied blockers.
- A precondition only the user can meet: follow `references/human-steps.md`.

## Keep the orchestrator context small

- A task-notification that only says an agent has not reported yet is not news: end the turn
  with one line, `waiting on <agent>`. A turn that dispatches, merges or closes is news: it ends with one status line.
- At the end of Completion, post the open decisions, corrections and follow-ups as a note on the
  spec ticket. After Publish, add the publish state to that note (for ticket files in the
  repository: write the chosen route into the note before the push, so it goes out in that
  push), then tell the user in one
  sentence to /clear and continue from it. A run that stops for a human step posts the same note first, since a restart loses this
  context.

## Completion

Continue until every child ticket is either verified merged and closed or was already closed before
the run. Run the full suite once on the final integrated HEAD (when the repository has none, say so in
the final report), in the strictest variant any
acceptance criterion names (for example a release or as-shipped build; a stricter variant usually
covers the plain one).

When the machine kills the suite (out of memory, not a failing test), stop the build servers
(.NET: `dotnet build-server shutdown`) and rerun it once in a fresh agent. Killed again: post
the hand-off note on the spec and stop.

When tests fail, measure before fixing:

1. Rerun only the failing tests, twice, on the final integrated HEAD, in the suite's variant (the
   as-shipped build, not a plain `dotnet test`). A test that passes both
   reruns is flaky: post it on the spec as a follow-up, with the three results, and count the
   suite as passed.
2. Run a repeating failure once on the starting HEAD, in a throwaway worktree
   (`git worktree add --detach <worktree-folder>/start-<SPEC-ID> <starting-HEAD>`), so the
   integration worktree stays on the parent branch. Failing there too, it predates this run:
   skip the bisect.
3. Otherwise find the merge that broke it with
   `git bisect start --first-parent <final-HEAD> <starting-HEAD>` and
   `git bisect run <failing tests>` (Git 2.29+).

Give the fix to a fresh worker (see step 4) in a new worktree from the final integrated HEAD. Its
ticket is the one the bisect named; when the bisect named none, it is the spec, so the commit
says `(#<SPEC-ID>)` and the integrator posts on the spec. Its prompt holds measured facts only:
the failing tests, their output, the rerun results and the bisect result. Leave out unmeasured
suspects; an agent spends its time ruling them out. The fix branch goes through an integrator
(step 5) like any ticket.

Once the suite passes, post the result **and tick the box** on every ticket in the graph whose
deferred proof it settles, including tickets closed before this run. Leave the parent spec open
unless the tracker instructions explicitly require closing it.

Finish with the dependency map, per-ticket final state, merge SHAs, full-suite result, and any
remaining blocker.

## Publish

Everything goes out in one push, because each new push cancels the pipeline of the one before.
First offer to apply the doc follow-ups from the hand-backs in a commit on the integrated HEAD.

Then ask the user how the work goes out: (a) a branch and a merge request (pull request) into the
parent branch, or (b) a direct push. Name the commits step 1 listed again. For each option, name
every acceptance criterion it leaves unmet, for example "#154: the merge request states the
local as-shipped run passed" stays unmet under (b).

- Push with `git push origin HEAD:<branch>`. Plain `-u` would move the local branch's upstream.
- The merge request states whatever its pipeline cannot prove, such as a local release-build run
  when the merge-request pipeline only builds Debug.
- Wait for the pipeline of that push before ending the run: poll its status in an until-loop (in
  Claude Code, through the Monitor tool). Check each job, then post its
  evidence and tick the box on every ticket whose deferred proof it settles. Name the pipeline ID.
- Clean up: stop the build servers (they hold files open, and `git worktree remove` then fails
  with "Permission denied"), run `git worktree remove <worktree-folder>/spec-<SPEC-ID>`, and run
  `git -C <root> switch <parent-branch>` in the folder step 1 detached. Delete `frontier.md` when
  it lives in the worktree folder.
