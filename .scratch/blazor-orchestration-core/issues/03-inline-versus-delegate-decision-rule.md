# Inline-versus-delegate decision rule

Type: grilling
Status: resolved
Assignee: @gvdvenis

## Question

What concrete, repeatable rule decides whether the orchestrator does the work inline or spawns a
specialist sub-agent?

Decide:

- The observable signals the rule uses — number of lanes, number of files, whether the task needs
  broad codebase reading, whether isolation protects the main context.
- The default when the signals are ambiguous.
- Whether parallel fan-out is ever allowed, and the minimum benefit that justifies its coordination cost.
- How the same task category is guaranteed to route the same way across runs.

## Answer

Use a deterministic **route classifier** with explicit thresholds:

1. **Inline** when the request is a single lane, expected to touch at most 2 files, and can be
   completed without broad repository discovery.
2. **Delegate** when any of these is true: more than 1 lane is needed, expected scope is more than
   2 files, broad cross-module reading is required, or isolation materially protects the main
   context (long logs, tool-heavy investigation, or independent reasoning thread).
3. **Ambiguous default** is delegate, because token-control and context isolation are core package
   goals and false-inline decisions are costlier than false-delegate decisions.
4. **Parallel fan-out** is allowed only when lanes are independent and each lane is expected to
   save at least one full specialist turn versus serial execution; otherwise delegate serially.
5. **Run-to-run consistency** comes from a fixed classifier table in the orchestrator contract:
   each task category maps to the same lane decision criteria and chosen route, with any override
   recorded in the run artifact.
