# Map: Self-improvement report

Labels: `wayfinder:map`

## Destination

Every design decision for the self-improvement report is locked: what it consumes, how a reviewer
turns findings into a chosen improvement prompt with a few clicks, and how that prompt is emitted
both as readable markdown and as a compressed form tailored for pasting into an LLM. This map
produces decisions and a spec — not a working report.

## Notes

- The report is a **prompt factory** first. Review and navigation exist to serve the act of choosing
  which improvements to actually implement; the payload is the assembled prompt.
- Two distinct outputs are required: a readable markdown copy for humans, and a succinct compressed
  copy tailored for an LLM prompt.
- Local-first and privacy-safe. Repository content, prompts, and credentials must not be sent to an
  external model from a browser artifact.
- The report is only produced when `--self-improve` is set.
- Recommendation-only: nothing the report emits mutates agents or skills automatically.
- Skills every session should consult: `/grilling`, `/prototype`, `/frontend-design`, `/codebase-design`.
- Sibling map: [Map: Blazor orchestration core package](../blazor-orchestration-core/map.md) —
  supplies telemetry and specialist report contracts this map consumes.

## Decisions so far

- [Charting session: report purpose](issues/00-charting-session.md) — the report is a prompt factory
  with review as navigation, emitting both a readable markdown prompt and a compressed LLM-tailored prompt.
- [Local-first delivery constraints](issues/04-local-first-delivery-constraints.md) — documents the
  `file://` constraints (opaque origin, clipboard limits, no browser persistence) that informed the
  delivery decision; the single-file `file://` model was subsequently superseded by the C# server
  delivery model (see below).
- [Review-to-prompt interaction model](issues/02-review-to-prompt-interaction-model.md) — Variant A
  is locked as canonical (severity-grouped card list with one-click queueing and a live prompt
  workspace); B and C were rejected for added interaction friction and delayed feedback.
- [Dual output: readable markdown and compressed LLM prompt](issues/03-dual-output-readable-and-compressed.md) — readable and compressed keep identical semantics; readable is proper markdown, compressed is a terse LLM-optimised rewrite, and both copy buttons appear when the queue is non-empty.
- [Suggestion history and scoring feedback](issues/05-suggestion-history-and-scoring-feedback.md) — suggestion history is CLI-owned with deterministic suggestion keys; dismissals deprioritize by explainable weights (with optional never-again), preventing noisy re-suggestion.
- [Report scope: per-run vs rolling](issues/06-report-scope-per-run-vs-rolling.md) — per-run only; the durable artifact is `report-input.json` (committed to source control), not `report.html` (a thin server-served shell, not committed); CLI stages `report-input.json`, developer commits alongside implementation; conflict flow offers continue/stash/discard; rolling diagnostics out of scope.
- [Cross-run finding deduplication](issues/07-cross-run-finding-deduplication.md) — CLI-side fold by `suggestion_key` before HTML is written; duplicates collapse to one record (max severity, evidence union, recurrence count); recurrence adds a bounded ranking boost; dismissal penalty from history outweighs recurrence boost.
- [Mobile/small-screen interaction model](issues/08-mobile-small-screen-interaction-model.md) — fully supported at ≥375px via `--self-improve` server (fixed port, stable QR code, per-run server, JS polling for auto-reload); server-side clipboard write; prompt in collapsible read-only section with full-screen edit overlay; `file://` mode shows degraded-state banner. (`--serve` flag in ticket body was superseded by `--self-improve` auto-launch, ticket 09.)
- [CLI callback vs copy-out boundary](issues/08-cli-callback-vs-copy-out-boundary.md) — user-initiated server write-back is permitted; automatic mutations are not. Minimal surface: `POST /api/ship-prompt` (supersedes `POST /clipboard` from ticket 09, per ticket 13) and `POST /api/dismissals` (new). `--dismiss <key>` CLI command retained as `file://`-mode fallback only.
- [Report input contract](issues/01-report-input-contract.md) — dedicated `report-input.json` written
  by the orchestrator; findings are atomic (one category each), severity derived from per-dimension
  specialist scores; HTML fetches data from local C# server API, not inline embedding.
- [Local server delivery model](issues/09-local-server-delivery-model.md) — `--self-improve` auto-launches a fixed-port C# server (default `5173`); single process, no history server; API surface: `GET /api/report`, `GET /ping`, `POST /api/ship-prompt` (supersedes `POST /clipboard`), `GET /shutdown`; server stays alive until idle timeout or shutdown after browser connects; no self-contained HTML fallback.
- [Port conflict handling and Windows firewall](issues/10-port-conflict-and-firewall.md) — port already bound means our server is running; log a warning and continue. Binding mode (loopback vs all-interfaces for mobile) is a skill-level preference, persisted in temp, passed as a flag to the server tool on launch.

- [Decision-log normalization pass](issues/14-decision-log-normalization-pass.md) — superseded wording reconciled across map and tickets; authoritative current state: C# server sole delivery path (`--self-improve` auto-launch), `POST /api/ship-prompt` replaces `POST /clipboard`, data file is `improvement-report-data.json`, `--serve` flag was never adopted.

- [C# tool and HTML report role](issues/13-csharp-tool-and-html-report-role.md) — the C# server is the sole process (server + tool); HTML assembles the readable prompt in-browser (with origin preamble injected from `GET /api/report`); "Copy readable" writes directly from browser; "Copy for LLM" POSTs to `POST /api/ship-prompt` which runs the middleware pipeline, writes to desktop clipboard, and returns the result for optional browser inspection. `POST /clipboard` is superseded.
- [Write-back to improvement-report-data.json](issues/12-write-back-to-report-input-json.md) — `report-input.json` renamed to `improvement-report-data.json`; promoted to a living document accumulating origin context, user decisions, and the shipped prompt; C# server owns all post-generation writes (atomic temp-rename); presence of decisions/shipped_prompt is the CLI readiness signal for staging.
- [Prompt enhancement pipeline](issues/11-prompt-enhancement-pipeline.md) — `/api/ship-prompt` runs a middleware chain: (1) syntactic compression (strip markdown, collapse whitespace — two regex passes, deterministic C#); (2) LLM semantic compression (named future slot, not wired for MVP); (3) persist to `shipped_prompt` + write to desktop clipboard + return in response body. Applies to "Copy for LLM" only.

## Not yet specified

*(None — all fog items graduated and resolved.)*



- Core package contracts, routing policy, and specialist definitions — owned by
  [Map: Blazor orchestration core package](../blazor-orchestration-core/map.md).
- Implementing the report. This map plans; a later effort builds.
- Hosting the report anywhere other than the local machine.
- Cross-run diagnostics and aggregate views — a future layer on top of the committed per-run
  artifacts, not part of this effort.
