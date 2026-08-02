# gman-skills

Personal Copilot CLI skills for Blazor orchestration and self-improvement, distributed via `npx skills`.

## Language

**Blazor-architect**:
The orchestration skill that routes multi-concern Blazor work through a thin orchestrator with specialist delegation.
_Avoid_: blazor-orchestrator, orchestrator

**Self-improve**:
The skill that generates improvement report data from specialist self-diagnosis, auto-launches the report-server, and handles CLI staging. Loaded by blazor-architect when `--self-improve` is active.
_Avoid_: self-improvement, improvement-analyzer

**Report-server**:
A local C# server that serves the self-improvement report UI and handles dismissals and prompt shipping.
_Avoid_: report-UI, improvement-report-server

**Setup-gman-skills**:
The skill that installs the dotnet-blazor plugin dependency and downloads the report-server binary.
_Avoid_: setup-skill, installer

**Route classifier**:
The fixed decision table that maps task categories to inline or delegate execution and selects the primary lane.
_Avoid_: routing-table, router

**Specialist lane**:
A bounded scope of Blazor work delegated by blazor-architect via the `task` tool.
_Avoid_: agent, specialist-agent

**Run ID**:
A `run-YYYYMMDD-HHMM` identifier generated at the start of every blazor-architect invocation.
_Avoid_: session-id, trace-id

**Suggestion key**:
A stable normalized string (`<category>:<target_surface>:<normalized_intent>`) identifying the same weakness across runs for cross-run dedup and history weighting.
_Avoid_: finding-id, dedup-key

**dotnet-blazor**:
An external Copilot CLI plugin providing Blazor component skills that blazor-architect delegates to.
_Avoid_: dotnet-skills, blazor-plugin
