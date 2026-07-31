# Harness packaging and loader format

Type: research
Status: resolved

Findings: [01-findings.md](01-findings.md)

## Question

How does the Copilot CLI harness actually discover and load skills, agents, and packages, and what
does that mean for this package's installed layout?

Specifically:

- Where must a skill live to be invocable (`.agents/skills/<name>/SKILL.md`, user vs project level),
  and where must a custom agent live (`.copilot/agents/<name>.agent.md`)?
- Is a folder "package" with a `manifest.yaml` a real loader concept, or is the manifest documentation
  only, with installation meaning "copy each component to its own conventional location"?
- What frontmatter fields are honoured for skills and agents, including tool allowlists?
- How does a skill pass arguments and flags, and how does a skill spawn a sub-agent?
- What are the documented best practices for a skill that orchestrates sub-agents?

## Answer

Copilot CLI has a **real plugin system**. A plugin is a directory with a root **`plugin.json`**,
installed via `copilot plugin install PATH`, bundling `agents/`, `skills/`, commands, hooks, MCP and
LSP configuration. `.copilot/packages/` and `manifest.yaml` have **no loader role** — the current
manifest is documentation, not an installable contract.

Component layout is confirmed: skills as `skills/<name>/SKILL.md` (name must match its directory),
agents as `agents/<name>.agent.md`. An agent's `tools` allowlist genuinely restricts it; omitting
`agent`/`Task` prevents a specialist from delegating further.

Two constraints that change the design:

1. **`$ARGUMENTS` is not a verified Copilot contract** — it is a Claude Code extension. Flags must be
   parsed from invocation text by the skill's own instructions.
2. **A skill cannot enforce "only I delegate"** — a skill is injected instructions, not a permissioned
   identity. Specialists can be hard-restricted, but orchestrator exclusivity is policy, not an ACL.

Installed content is cached, so local changes require reinstalling.

Full findings: [01-findings.md](01-findings.md)
