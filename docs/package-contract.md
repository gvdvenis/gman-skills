# Package contract

## Purpose

The package routes Blazor UI work through one thin orchestrator and a small set of isolated
specialists. It optimizes token use before speed, keeps responsibility boundaries explicit, and
keeps telemetry local by default.

## Installation and layout

Install by copying the complete `blazor-orchestration-package` folder into the agent/skill package
root supported by the harness. `manifest.yaml` is the package index. The harness-specific loader
target is intentionally not hard-coded until that integration decision is settled.

The package owns these directories:

| Directory | Owner | Purpose |
| --- | --- | --- |
| `agents/` | Orchestrator package | Executable orchestrator and narrow specialists |
| `skills/` | Shared guidance | Reusable routing and implementation constraints |
| `telemetry/` | Orchestrator | Event and report contracts |
| `reports/` | Orchestrator/analyzer | Optional run reports |
| `docs/` | Package maintainers | Contracts and operational guidance |

## Execution contract

The orchestrator:

- parses the task and `--skip-code-review` / `--self-improve` flags;
- selects exactly one primary lane before execution;
- keeps narrow, low-coordination work inline;
- delegates when isolation or parallelism has a net quality/token benefit;
- aggregates structured specialist reports;
- runs code review by default, with a stop rule for diminishing returns;
- runs self-improvement only when explicitly enabled.

Specialists:

- accept a bounded task for one lane;
- use only their declared tools;
- do not silently absorb adjacent concerns;
- return a structured report using `telemetry/feedback-report-template.json`;
- include self-diagnosis only when self-improvement is enabled.

## Artifacts

Each run uses a unique `<run_id>` and writes locally to:

```text
.copilot/blazor-orchestration/runs/<run_id>/
  events.jsonl
  reports/<agent-id>.json
  analysis.json
  reports/self-improvement-report.html
```

The HTML report is optional and is created only with `--self-improve`. Repository content,
credentials, and prompts are not sent to third-party services by this package.

## Contract ownership

| Decision | Owner |
| --- | --- |
| Lane selection and delegation | Orchestrator |
| Blazor implementation details | Specialist |
| Shared Blazor constraints | Skill |
| Event and report shape | Telemetry contracts |
| Recommendation ranking | Improvement analyzer |
| Human approval of improvements | User |
