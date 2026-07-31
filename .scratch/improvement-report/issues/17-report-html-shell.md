# 17 — Report HTML shell: severity-grouped findings, queue, and dual copy

**What to build:** The C# server serves an HTML shell that fetches data from `GET /api/report` on load. The shell renders findings grouped by severity (high → medium → low), each as a card the user can add to or remove from a queue with one click. A live prompt workspace shows the assembled readable prompt, including an origin preamble (skill, run_id, repo) injected from the API response. "Copy prompt (readable)" writes the assembled text via the browser's clipboard API. "Copy for LLM" is present but inactive until the ship-prompt endpoint is wired (ticket 19). Both copy buttons appear only when the queue is non-empty.

**Blocked by:** 16 — Fixed-port C# server startup and lifecycle.

**Status:** ready-for-agent

- [ ] HTML shell loads and fetches data from `GET /api/report`
- [ ] Findings are rendered in severity-grouped cards (high → medium → low)
- [ ] One-click add/remove queues a finding into the prompt workspace
- [ ] Readable prompt assembles live from queued findings including origin preamble
- [ ] "Copy prompt (readable)" writes via browser clipboard API
- [ ] "Copy for LLM" button renders but is inactive until ship-prompt is wired
- [ ] Both copy buttons only appear when the queue is non-empty
