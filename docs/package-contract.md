# Package contract

## Purpose

The package routes Blazor UI work through one thin orchestrator and a small set of isolated
specialists. It optimizes token use before speed, keeps responsibility boundaries explicit, and
keeps telemetry local by default.

## Installation and layout

Install the package as a Copilot CLI plugin:

```sh
copilot plugin install <path-to-blazor-orchestration-package>
```

After installation, verify the components loaded:

```sh
copilot /plugin list        # confirm blazor-orchestration is listed
copilot /agent              # confirm blazor-orchestrator and specialists appear
copilot /skills list        # confirm blazor-orchestrator skill is available
```

After any local change, reinstall to pick up the updated content (the CLI caches installed plugins).

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

- generates a `run_id` (format: `run-YYYYMMDD-HHMM`) at the start of every invocation and announces
  it in the first output line, regardless of telemetry settings;
- parses `--skip-code-review` and `--self-improve` from natural language — `$ARGUMENTS` substitution
  is not relied upon;
- applies the deterministic route classifier in `docs/routing-classifier.md` to select exactly one
  primary lane and decide inline vs delegate before touching any file;
- keeps narrow, low-coordination work inline (single lane, ≤ 2 files, no broad repo discovery);
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
~/.blazor-architect/runs/<run_id>/
  events.jsonl
  reports/<agent-id>.json
  analysis.json
  reports/self-improvement-report.html
```

Artifacts are stored in the user-level Copilot directory, not the target repository. The HTML
report is optional and is created only with `--self-improve`. Repository content, credentials, and
prompts are not sent to third-party services by this package.

## Contract ownership

| Decision | Owner |
| --- | --- |
| Lane selection and delegation | Orchestrator |
| Blazor implementation details | Specialist |
| Shared Blazor constraints | Skill |
| Event and report shape | Telemetry contracts |
| Recommendation ranking | Improvement analyzer |
| Human approval of improvements | User |
