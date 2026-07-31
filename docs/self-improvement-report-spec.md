# Self-improvement report spec

## Purpose

Produce an interactive HTML report that helps the user review and act on agent/skill improvement suggestions.

## Core requirements

1. The report must be generated after a run when `--self-improve` is enabled.
2. Each suggestion must be scored on a 5-point scale from -2 to +2.
3. Each suggestion must include at least these dimensions:
   - token efficiency impact
   - quality impact
   - maintainability impact
   - clarity impact
4. The report should support a simple interaction model:
   - select a suggestion
   - add its prompt fragment to a sidebar
   - review the combined prompt
   - copy the combined prompt for later use

## Suggested suggestion structure

- `id`
- `title`
- `summary`
- `category` (tool, routing, prompt, verbosity, scope, telemetry)
- `scores`
  - `token_efficiency`
  - `quality`
  - `maintainability`
  - `clarity`
- `expected_impact`
- `recommended_prompt_fragment`
- `evidence`

## Output artifact

- `reports/self-improvement-report.html`
- `reports/self-improvement-prompts.json`
