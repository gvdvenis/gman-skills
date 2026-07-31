# Enforcing orchestrator exclusivity

Type: grilling
Status: resolved
Assignee: @gvdvenis

## Question

Research established that a skill is injected instructions, not a permissioned identity, so a skill
**cannot technically guarantee** that it alone spawns sub-agents. The charting decision assumed it could.

Decide:

- Whether policy-only exclusivity is acceptable — the skill says it, and drift is tolerated.
- Whether a plugin **hook** can enforce it, and whether that is worth the complexity.
- Whether the orchestrator should after all be an **agent** with its own tool allowlist, with a thin
  skill in front of it purely for discovery and triggering.
- What actually breaks if the main agent delegates directly without the orchestrator — is the cost
  lost telemetry, inconsistent routing, or nothing meaningful?

## Why this matters

This partially reopens the orchestrator-form decision recorded in
[Charting session: destination and orchestrator form](00-charting-session.md). It must be settled
before the invocation surface is locked.

## Answer

Policy-only exclusivity is **not** sufficient. The package should keep the orchestrator as a
discoverable **skill** for routing UX, but enforce exclusivity structurally in the components it
controls:

1. Specialists must keep hard tool allowlists that exclude `agent`/`Task`, so only the main session
   can spawn sub-agents on their behalf.
2. The orchestrator skill remains the single documented delegation lane and emits the run metadata.
3. A plugin hook should audit/session-log delegation events and flag runs where delegation happened
   outside the orchestrator lane; this is worth the complexity because it makes policy drift visible.

Do **not** convert the orchestrator into a standalone agent as the primary interface. That adds a
second invocation surface and weakens discoverability without materially improving enforcement over
specialist allowlists + hook auditing.

If the main agent delegates directly, what breaks is meaningful: routing consistency and telemetry
fidelity are lost (missing lane choice rationale, inconsistent report envelopes, and weaker
self-improvement input). It is therefore treated as contract drift, not equivalent behavior.
