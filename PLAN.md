# Focused plan

## 1. Lock the architecture

- Keep one orchestrator and 2-4 specialist agents.
- Use a single-lane rule: each specialist owns one task family only.
- Keep the orchestrator thin and routing-oriented.

## 2. Define the package contract

- Standardize agent input/output:
  - task summary
  - lane
  - tool allowlist
  - report format
  - completion criteria
- Make the orchestrator responsible for handoff and aggregation.

## 3. Add telemetry

Capture structured events for:
- task dispatch
- lane selection
- tool usage
- handoff
- completion
- feedback submission
- improvement suggestions

## 4. Add self-diagnosis

Each agent should end with a short diagnostic block:
- what went well
- what was unclear
- missing or missing-useful tools
- token inefficiencies
- ambiguity or scope drift
- follow-up suggestions

## 5. Add improvement analysis

At the end of a run, the orchestrator should:
- aggregate reports from all agents
- rank issues by severity and frequency
- propose concrete improvements to prompts, tool allowlists, or routing rules

## 6. Package and iterate

- Ship as a folder-based package first.
- Add an installer or import script later.
- Iterate based on actual telemetry rather than intuition.
