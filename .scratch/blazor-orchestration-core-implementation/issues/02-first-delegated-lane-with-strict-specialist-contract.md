# 02 — First delegated lane with strict specialist contract

**What to build:** One delegated specialist lane that runs end-to-end from orchestrator routing through completion, returns strict schema-compliant JSON, and is handled deterministically when malformed output is returned.

**Blocked by:** 01 — Bootstrap plugin and orchestrator entrypoint.

**Status:** ready-for-agent

- [ ] A delegated lane executes with bounded specialist capabilities and reports using the required report contract fields.
- [ ] Malformed specialist output triggers exactly one schema-repair retry and then lands in explicit failed-report semantics if still invalid.

