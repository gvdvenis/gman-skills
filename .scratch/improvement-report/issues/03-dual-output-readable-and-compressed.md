# Dual output: readable markdown and compressed LLM prompt

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 02

## Question

What exactly distinguishes the readable copy from the compressed LLM copy?

Decide:

- The structure of the readable markdown output — headings, rationale, evidence links.
- What the compressed form drops, and what it must never drop.
- Whether compression is deterministic and local, or model-assisted, given that browser-to-external-model
  calls are ruled out.
- If model-assisted, whether compression runs CLI-side before the report is written, so the browser
  artifact stays offline.
- How both outputs are copied, and how the user knows which one they copied.

## Answer

Readable and compressed carry identical semantic content — the readable copy **is** the prompt, rendered with proper markdown (headings, numbered items, no escape characters). No human-only decoration.

The compressed form is a semantically optimised rewrite of the same content: terse directives, LLM-friendly wording, and — when multiple improvements are queued — restructured and deduplicated into one coherent prompt with skill invocations injected where relevant.

Compression is **deterministic for now** (CLI-side templating). Model-assisted or C# tool-driven compression is fog, to be revisited when the dedicated C# feedback-report tool is scoped.

Both copy buttons — "Copy prompt (readable)" and "Copy prompt (compressed)" — appear once the queue is non-empty, with a per-button "Copied!" confirmation. Neither is hidden or secondary.
