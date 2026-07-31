# Local-first delivery constraints

Type: research
Status: resolved

Findings: [04-findings.md](04-findings.md)

## Question

What actually works for a single-file, local HTML report opened from the filesystem?

Investigate:

- `file://` origin restrictions on fetch, modules, and clipboard access in current browsers.
- Whether clipboard writes require a user gesture and a secure context, and what the fallback is.
- Whether any local persistence is available and reliable for a `file://` document.
- Practical size limits for embedding run data inline.
- Whether serving the report from a short-lived local server is meaningfully better, and its cost.

## Answer

> **⚠ Superseded by [Local server delivery model](09-local-server-delivery-model.md) (ticket 09).**
> The self-contained `file://` single-file model decided here was overturned: the local C# server
> is now the sole delivery path. `file://` mode is the degraded-banner fallback only. The
> constraints documented below remain accurate as context for why the server model was adopted.

Ship a **fully self-contained single HTML file** with inline data and scripts, no network calls,
in-memory queue state, and feature-detected clipboard copying triggered by a real click.

Key constraints discovered:

- `file://` documents get an opaque origin, so sibling `fetch`, module imports, JSON modules, and
  file-backed Workers all fail. Inline scripts and inline data work.
- Clipboard writes need a direct click handler, with a `execCommand("copy")` textarea fallback.
- `localStorage`, `sessionStorage`, and IndexedDB are unreliable or undefined for file origins, so
  **no browser persistence may be depended on**.
- Deterministic local compression is realistic via templating, deduplication, and concise directives.
  Quality-preserving semantic compression is not an established technique.

A `127.0.0.1` server remains an escape hatch only for unusually large or modular reports.

Full findings: [04-findings.md](04-findings.md)
