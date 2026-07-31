# Blazor orchestration package

This package defines a thin, Blazor-focused orchestration layer. It combines:

- a thin orchestrator
- narrow specialist agents
- shared skills for Blazor-specific guidance
- telemetry and feedback capture
- an opt-in, recommendation-only improvement analyzer

## Package contract

The authoritative layout, runtime flags, artifact paths, and ownership rules are in
[`docs/package-contract.md`](docs/package-contract.md). `manifest.yaml` is the machine-readable
package index and must stay aligned with that document.

The package is installed as a folder copied into the harness's agent/skill package root. The exact
harness loader target remains an integration decision; this package does not assume a hosted service
or browser-based model call.

## Runtime ownership

1. The orchestrator parses the task and flags, selects one primary lane, and decides inline versus delegated execution.
2. A specialist owns only its declared lane and returns a structured report.
3. The orchestrator aggregates reports and runs code review unless `--skip-code-review` is set.
4. Self-improvement runs only with `--self-improve`; it emits recommendations and never mutates prompts or skills automatically.

## Deferred scope

Interactive report design, browser/mobile presentation, prompt compression, and provider integration
remain deferred until the core contracts and telemetry schema are stable.
