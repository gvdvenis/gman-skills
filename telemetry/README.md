# Telemetry design

Telemetry should be lightweight and structured.

## Core event categories

- `dispatch`: the orchestrator assigns a task to an agent
- `tool_use`: a tool was used by an agent
- `tool_missed`: a likely better tool or workflow was available but not used
- `handoff`: the agent or orchestrator passed work to another lane
- `completion`: the lane completed
- `feedback`: the agent reports an issue or improvement opportunity
- `analysis`: the orchestrator summarizes the run and suggests changes

## Report shape

Each agent should emit a concise report with:
- lane
- outcome
- files touched
- tools used
- issues observed
- self-diagnosis
- suggested improvements
