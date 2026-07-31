# 06 — Hook-level delegation drift audit

**What to build:** Hook-based auditing that detects and flags delegation occurring outside the orchestrator lane so policy drift is visible and telemetry consistency expectations are enforceable.

**Blocked by:** 03 — Expand lanes and routing explanations.

**Status:** done

- [x] Delegation events outside orchestrator flow are detected and recorded with enough context to diagnose drift.
- [x] Legitimate orchestrator-managed delegation does not produce false drift flags under normal routed execution.

