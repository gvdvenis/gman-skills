# Implementation notes

## Phase 1

- Create run-level metadata and a structured feedback collector.
- Implement the orchestrator decision gate for inline vs delegated execution.
- Add a simple report generator that outputs an HTML file.

## Phase 2

- Add interactive suggestion selection.
- Add a prompt sidebar that accumulates chosen prompt fragments.
- Add a copy-to-clipboard action for the assembled prompt.

## Phase 3

- Add optional persistence so the report and selected prompt fragments can be reloaded later.
- Add scoring history so the system can learn from which suggestions were accepted.
