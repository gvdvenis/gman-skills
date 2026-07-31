# C# tool and HTML report role

Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 09, 03, 01

## Question

The map notes flag an unresolved question: how will a dedicated C# feedback-report tool handle
compression, API interactions, and integrated prompt assembly — and what does that mean for
the HTML report's role once the tool exists?

Ticket 03 deferred model-assisted or C# tool-driven compression as fog. Ticket 09 established
the local C# server as the sole delivery path, but leaves open how much logic lives in the server
vs in the HTML report vs in a dedicated offline tool.

Decide:

- Whether a dedicated C# tool (separate from the local server) is in scope for this effort, or
  whether the server process *is* that tool.
- What responsibilities belong to the C# layer vs the HTML/JS front-end:
  - Prompt assembly (concatenating queued `prompt_fragment` values into a coherent prompt)?
  - Compression and rewriting for the compressed output?
  - Structured API interactions (dismissal write, clipboard write)?
  - Evidence resolution (loading linked specialist report JSON)?
- If the C# tool is the same process as the server: does prompt assembly move server-side (returning
  an assembled string from `POST /clipboard`) or does the HTML assemble in-browser before posting?
- Whether the HTML report is a thin shell (data display + user gestures only, all logic in C#) or
  a richer client (assembles and transforms prompts in JS before calling the server)?
- How the answer changes if model-assisted compression is added later (ticket 11): does that require
  a server-side pipeline, or can it be bolted on without restructuring the boundary?

## Answer

**One C# server process is both the delivery server and the feedback-report tool.**

### Process identity and location

The ticket 09 server is the sole C# process — no separate offline binary. It lives with the
`self-improve-report` skill, which may reside in the user folder or in a repo. The server receives
`report-input.json` at launch and holds it as its authoritative context for the lifetime of the run.

### Responsibility split: HTML vs C# server

| Concern | Owner |
|---|---|
| Displaying findings, queuing cards | HTML/JS |
| Assembling the readable prompt from queued `prompt_fragment` values | HTML/JS (in-browser) |
| "Copy readable" clipboard write | Browser clipboard API (no server) |
| "Copy for LLM" — transform + deliver to desktop clipboard | C# server via `POST /api/ship-prompt` |
| Evidence resolution, dismissal recording | C# server |

### `POST /api/ship-prompt`

Replaces `POST /clipboard` from ticket 09. The browser POSTs the assembled readable prompt;
the server:

1. Enriches the prompt with `report-input.json` context — repo identity, skill IDs, file paths,
   and any other metadata relevant to where the prompt will be applied.
2. Runs the enriched prompt through a **middleware pipeline** (see below).
3. Writes the result to the desktop clipboard.
4. Returns the transformed prompt in the response body so the browser can surface an optional
   inspection panel.

### Pipeline design

The pipeline is a **middleware chain inside the C# server**. For MVP, it contains deterministic
steps only: context injection (from `report-input.json`) and compression (terse rewriting of the
readable prompt). The chain is designed for extension — model-assisted transformation is a named
future slot and is explicitly out of scope for this effort.

### What this supersedes

`POST /clipboard` from ticket 09 is replaced by `POST /api/ship-prompt`, which does everything
`/clipboard` did plus context enrichment, pipeline transformation, and response return.


- [Local server delivery model](09-local-server-delivery-model.md) — C# server is established; this ticket decides the logic boundary
- [Dual output: readable markdown and compressed LLM prompt](03-dual-output-readable-and-compressed.md) — compression ownership is the central question
- [Report input contract](01-report-input-contract.md) — prompt_fragment assembly is orchestrator-written; this ticket decides where runtime assembly of the queued prompt lives
