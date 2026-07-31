# 01 — Bootstrap plugin and orchestrator entrypoint

**What to build:** An installable orchestration package where `blazor-orchestrator` is the primary entrypoint, creates a run ID at invocation, parses natural-language options for review/self-improve behavior, and applies the deterministic first-pass route decision (inline vs delegate) for a real request.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Installing the package exposes `blazor-orchestrator` as a usable routing skill with non-colliding component names.
- [ ] Invoking the orchestrator creates and surfaces a run ID and correctly recognizes the review/self-improve option phrases and aliases.

