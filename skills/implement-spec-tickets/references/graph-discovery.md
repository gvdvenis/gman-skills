# Graph discovery

Brief for the graph-discovery subagent. You do tracker research only and return a compact graph.
You edit no code, change no ticket, create no worktree and launch no implementation agent.

You receive the spec reference, the repository root and the repository's issue-tracker
instructions (normally `docs/agents/issue-tracker.md`). Every tracker operation below goes
through the CLI or file layout those instructions describe.

## Find the child tickets

1. Read the spec, including its comments or notes.
2. Collect candidate tickets: backlink notes on the spec (such as `mentioned in issue #<ID>`),
   native child links when the tracker has them, and, for a local markdown tracker, every ticket
   file in the spec's issues folder.
3. Fetch every candidate once.
4. Include a candidate only when it declares the requested spec as its parent, either in a
   `## Parent` section or an inline `Part of <spec>` line. The reference can be `#<SPEC-ID>`, a
   tracker URL that ends in the spec's ID, or the path of the spec file. A backlink alone does
   not make an implementation ticket.
5. Exclude referenced audits, decisions, research or background tickets that do not declare that
   parent.
6. Parse each included ticket's blockers: the tracker's native blocking links, a `## Blocked by`
   section, or a `Blocked by:` line (plain or bold):
   - `None - can start immediately` means no blockers.
   - Every ticket reference there is a blocking edge: `#<ID>`, a tracker URL that ends in the
     ticket's ID, or, on a local tracker, the ticket's number or file name.
   - Build only explicit edges. Never infer dependencies from ticket order, titles, or likely
     code ownership.

Example: tickets 12-18 all link back to the spec. Ticket 11 also mentions it, but it is a
research note with no `## Parent` section, so it is excluded. Tickets 12-18 declare the spec as
their parent and list their dependencies under `## Blocked by`.

## Return

```text
SPEC #<ID> dependency map - A -> B means A must finish before B.

<compact edge list>

Execution order

Wave 1: <tickets>
Wave 2: <tickets>

Excluded references: <ticket and reason>
```

Also return:

- A node table: ticket ID, title, state, URL or path, acceptance criteria, blockers.
- The paths of the ticket files you wrote: every ticket's full body and comments, one file per
  included ticket, in a temporary folder outside the repository (the session's scratchpad folder
  when it has one). Excluded tickets get no file. Bodies then reach workers without passing through the orchestrator's
  context.
- The **corrections**: every fact in a ticket body or comment that contradicts the spec's text,
  with its source and the spec text it replaces. Closed blockers are the usual source: a spec
  names one host for a package feed, a closed blocker ticket moved it to another host, and the
  user gets asked for the wrong credentials.

## Validate before returning

- every included ticket points back to the requested spec;
- every included open ticket appears once in the waves;
- closed tickets satisfy dependencies but are not in a wave;
- every blocker is an included ticket or an explicitly identified external blocker;
- there are no missing tickets or conflicting blocker declarations, and the graph is a DAG.

When a check fails, return the exact evidence instead of the graph.
