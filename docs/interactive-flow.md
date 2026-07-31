# Interactive flow design

## Goals

- Let the orchestrator decide whether a task should run inline or be delegated to specialist agents.
- Keep token usage as the primary optimization constraint.
- Make self-improvement opt-in via `--self-improve`.
- Make code review opt-out via `--skip-code-review`.
- Produce an interactive HTML report with suggestion cards and a prompt-build sidebar.

## Execution model

### Inputs

- task description
- optional flags:
  - `--skip-code-review`
  - `--self-improve`

### Decision step

1. Decide whether the task should be handled inline in the orchestrator thread or delegated.
2. Prefer inline only when the task is narrow and the added coordination overhead would be larger than the benefit.
3. Prefer delegation when the task spans multiple lanes or would benefit from specialist isolation.

### Delegation rules

- Delegate only when the task clearly spans multiple lanes or needs isolation.
- Prefer one specialist if possible.
- Avoid fan-out when it would create more context overhead than value.
- If the task is truly broad, split into 2-4 specialists, but keep the orchestrator thin.

### Reporting contract

Each specialist returns:
- summary of work completed
- files changed
- validation performed
- token estimate
- self-diagnosis, if `--self-improve` is enabled

### Review gate

- If `--skip-code-review` is not set, run an adversarial review pass.
- If findings exist, fix them and repeat until the review cycle reaches diminishing returns.

### Self-improvement gate

- If `--self-improve` is set, aggregate all specialist feedback and produce an HTML report.
- The report should contain ranked suggestions with score cards.
- Each suggestion should include a button/action to add a prompt fragment to a sidebar.
- A final button should copy the assembled prompt to the clipboard.
