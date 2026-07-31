# Prompt enhancement pipeline

Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 09, 03

## Question

Ticket 09 deferred an optional prompt-enhancement pipeline: before writing the assembled prompt to
the desktop clipboard via `POST /clipboard`, the server could optionally pass the prompt through an
LLM or a deterministic transformation step.

Decide:

- Whether prompt enhancement is in scope for this effort at all, or permanently deferred.
- If in scope: which transformation paths are permitted (deterministic C# rewriting, local LLM call,
  external LLM call via a configured key, or routing through the copilot CLI agent)?
- Whether enhancement is opt-in (user clicks an "Enhance before copy" toggle or button) or
  automatic.
- How enhancement interacts with the dual-output model (ticket 03): does it apply to the readable
  copy, the compressed copy, or both?
- If an external model call is involved, how this squares with the local-first and privacy-safe
  constraint from the map notes.
- What happens when enhancement fails or is unavailable — does the server fall back to the raw
  assembled prompt, or does the copy fail?
- Whether the enhancement step belongs inside the C# server, in a separate process, or in the CLI
  layer.

## Answer

**The pipeline is in scope for MVP as a middleware chain inside the C# server, with one active
deterministic step. Model-assisted enhancement is a named future slot, explicitly out of scope
for MVP.**

### Pipeline architecture

`POST /api/ship-prompt` (see ticket 13) runs the assembled readable prompt through a middleware
chain in the C# server. The chain is designed for extension — each step is an independent
middleware that receives the prompt string and returns a transformed string.

### MVP chain (in order)

| Step | Type | MVP status |
|---|---|---|
| Syntactic compression | Deterministic C# | **Active** |
| LLM semantic compression | Model-assisted | Named slot — not wired |
| Persist + deliver | Side effect | **Active** |

### Syntactic compression (active for MVP)

The browser posts the readable prompt already containing the `origin` preamble (injected
client-side from `improvement-report-data.json`). The server applies two regex passes:

1. Strip markdown formatting characters (`#`, `**`, `_`, `- `, fenced code block markers, etc.)
2. Collapse excess whitespace and blank lines

No semantic understanding. No LLM. Keeps the shipped prompt lean without restructuring meaning.

### LLM semantic compression (future slot)

Merging near-duplicate directives, deduplication across findings, and semantic rewriting require
a model. This step is a named, unimplemented slot in the chain — it can be bolted on without
restructuring the boundary. Out of scope for this effort.

### Persist + deliver (active for MVP)

After the chain runs:
1. Write the transformed prompt to `shipped_prompt.transformed` in `improvement-report-data.json`
   (atomic temp-rename, C# server).
2. Write to desktop clipboard.
3. Return the transformed prompt in the HTTP response body so the browser can surface an optional
   inspection panel.

### Fallback

If a pipeline step fails, the server logs the error and passes the input string through unchanged.
The clipboard write and persist steps still execute. The HTTP response includes an `"warnings"`
field listing any failed steps. The user always gets a usable prompt.

### Scope note

`POST /clipboard` from ticket 09 is superseded by `POST /api/ship-prompt` (ticket 13). The
"Copy readable" button writes directly to the browser clipboard — no server pipeline involved.
The pipeline applies only to the "Copy for LLM" path.

## Depends on

- [Local server delivery model](09-local-server-delivery-model.md) — `POST /api/ship-prompt` is the call site
- [Dual output: readable markdown and compressed LLM prompt](03-dual-output-readable-and-compressed.md) — pipeline applies to the compressed/LLM copy only
- [C# tool and HTML report role](13-csharp-tool-and-html-report-role.md) — pipeline lives inside the C# server

## Question

Ticket 09 deferred an optional prompt-enhancement pipeline: before writing the assembled prompt to
the desktop clipboard via `POST /clipboard`, the server could optionally pass the prompt through an
LLM or a deterministic transformation step.

Decide:

- Whether prompt enhancement is in scope for this effort at all, or permanently deferred.
- If in scope: which transformation paths are permitted (deterministic C# rewriting, local LLM call,
  external LLM call via a configured key, or routing through the copilot CLI agent)?
- Whether enhancement is opt-in (user clicks an "Enhance before copy" toggle or button) or
  automatic.
- How enhancement interacts with the dual-output model (ticket 03): does it apply to the readable
  copy, the compressed copy, or both?
- If an external model call is involved, how this squares with the local-first and privacy-safe
  constraint from the map notes.
- What happens when enhancement fails or is unavailable — does the server fall back to the raw
  assembled prompt, or does the copy fail?
- Whether the enhancement step belongs inside the C# server, in a separate process, or in the CLI
  layer.

## Depends on

- [Local server delivery model](09-local-server-delivery-model.md) — `POST /clipboard` is the call site for enhancement
- [Dual output: readable markdown and compressed LLM prompt](03-dual-output-readable-and-compressed.md) — compression is currently deterministic; enhancement is a potential upgrade path
