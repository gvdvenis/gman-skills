# 19 — `POST /api/ship-prompt` middleware chain

**What to build:** "Copy for LLM" in the report shell posts the assembled readable prompt to `POST /api/ship-prompt`. The C# server runs the prompt through a two-pass syntactic compression middleware (strip markdown, collapse whitespace — deterministic regex, no external model). The compressed result is persisted to the `shipped_prompt` field of `improvement-report-data.json` atomically (temp-rename), written to the desktop clipboard, and returned in the response body for optional browser inspection. If a middleware step fails, the pipeline passes through the unchanged prompt, continues with persist and clipboard, and returns a `warnings` array in the response. The "Copy for LLM" button in the UI is activated once the server endpoint is wired.

**Blocked by:** 17 — Report HTML shell: severity-grouped findings, queue, and dual copy.

**Status:** ready-for-agent

- [ ] `POST /api/ship-prompt` accepts the assembled readable prompt body
- [ ] Syntactic compression runs two deterministic passes (strip markdown, collapse whitespace)
- [ ] Compressed result is persisted to `shipped_prompt` in `improvement-report-data.json` atomically
- [ ] `shipped_prompt` includes `readable`, `transformed`, and `shipped_at` fields
- [ ] Desktop clipboard receives the compressed output
- [ ] Response body includes the transformed prompt and any `warnings`
- [ ] Pipeline step failure passes through unchanged prompt; persist and clipboard still run
- [ ] "Copy for LLM" button in the UI is active and calls this endpoint
