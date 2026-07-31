# Routing-explanation contract

Type: grilling
Status: resolved
Assignee: @copilot

## Answer

**Surface: conversational + dispatch event; not a separate artifact.**

### Conversational output

The orchestrator emits one sentence per delegated lane immediately after the routing decision:

> *"Routing to component-architect: multi-file component extraction across 4 components."*

This is mandatory when delegating, silent when routing inline (inline work is self-evident from
the response itself). The line is addressed to the user, not appended to `events.jsonl`.

### Persisted field: `routing_reason` inside `dispatch`

The rationale lives in the existing mandatory `dispatch` event (decided in ticket 05) as a new
required string field `routing_reason`. No new file, no new event type.

```jsonc
{
  "event": "dispatch",
  "run_id": "...",
  "lane": "component-architect",
  "routing_reason": "Multi-file component extraction; 4 files, exceeds 2-file inline threshold.",
  ...
}
```

**Format:** plain prose, one sentence, ≤120 characters. Human-readable, not structured tags — the
classifier table already encodes the full logic; `routing_reason` captures the specific signals
that triggered the decision for *this* run, not the general rule.

**Rationale for this choice:**
- Reusing `dispatch` avoids a new artifact and keeps all lane-level facts in one event.
- The self-improvement sibling map can correlate `routing_reason` with outcomes without parsing
  a separate file.
- The 120-char cap prevents the field from becoming a verbose trace; the classifier table is the
  canonical source for the general rule.

### Inline routing

No `routing_reason` field and no conversational note when routing inline — the response *is*
the explanation. If the user later enables telemetry, the `dispatch` event is emitted with
`"lane": "inline"` and `routing_reason` still populated (e.g. `"Single-lane, ≤2 files, no
broad discovery."`).

### Summary

| Axis | Decision |
|---|---|
| Shown to user | Yes — one sentence per delegated lane, mandatory on delegation |
| Persisted | Yes — `routing_reason: string` field in the `dispatch` event |
| New artifact | No — piggybacks on existing `events.jsonl` / `dispatch` |
| Detail level | Lane name + one-sentence reason, ≤120 chars |
| Inline routing | Silent conversationally; `dispatch` still emitted with `"lane": "inline"` when telemetry is on |



How does the orchestrator explain its routing choice to the user, and what is the persistence story for that explanation?

Decide:

- Whether routing rationale is shown to the user conversationally, written to the run artifact, or both.
- If it is persisted, in which file and under which field/format.
- The level of detail: just the lane choice, or the reasoning behind it.
- Whether the explanation is mandatory or conditional (e.g. only when delegating, or only on request).
