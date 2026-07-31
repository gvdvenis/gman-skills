# Findings: harness packaging and loader format

Resolved by a `/research` subagent. Verified claims carry a source; inference is marked.

## 1. Skill discovery and format

**VERIFIED.** Copilot CLI discovers skills from:

- Project: `.github/skills/`, `.claude/skills/`, `.agents/skills/`
- Personal: `~/.copilot/skills/`, `~/.agents/skills/`
- Plugin-contributed skill directories

Each skill is a directory containing exactly `SKILL.md`. Reload with `/skills reload`; inspect with
`/skills info NAME`.

Required metadata:

- `name` — 1–64 lowercase alphanumeric/hyphen characters, no leading/trailing/consecutive hyphens,
  must match the parent directory name.
- `description` — 1–1024 characters, stating both what the skill does and when to use it.
- Optional: `license`, `compatibility`, `metadata`, experimental `allowed-tools`.

The body loads only after activation; startup loads names and descriptions only. The specification
recommends under 5,000 instruction tokens and under 500 lines, with reference files loaded on demand.
Descriptions are the primary routing mechanism — imperative, intent-focused, with explicit positive
and near-miss triggers.

`allowed-tools` on a skill **pre-approves** tool use. It is not an execution allowlist comparable to
an agent's `tools` field.

Local evidence: many personal skills exist under `C:\Users\g.vd.venis\.agents\skills\`;
`C:\Users\g.vd.venis\.copilot\skills\` is empty.

Sources: <https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills>,
<https://docs.github.com/en/copilot/concepts/agents/about-agent-skills>,
<https://agentskills.io/specification>,
<https://agentskills.io/skill-creation/optimizing-descriptions>

## 2. Custom-agent discovery and schema

**VERIFIED.** Locations:

- Project: `.github/agents/`
- Personal: `~/.copilot/agents/`
- Plugin: manifest-configured `agents/` directories
- File name: `NAME.agent.md`

A personal agent overrides a project agent of the same name. Agents load after a CLI restart.

Supported frontmatter: `description` (required), `name`, `target`, `tools`, `model`,
`disable-model-invocation`, `user-invocable`, `infer` (retired), `mcp-servers`, `metadata`.
The Markdown prompt is limited to 30,000 characters.

Canonical, case-insensitive tool aliases: `execute` (`shell`, `Bash`, `powershell`), `read`, `edit`,
`search`, `agent` (`custom-agent`, `Task`), `web`, `todo`. MCP tools select as `server/tool` or
`server/*`. Unknown names are ignored. Omitting `tools` or using `["*"]` enables everything; `[]`
disables everything; a list enables only that subset — **so the allowlist genuinely restricts a
sub-agent**.

Sources: <https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/create-custom-agents-for-cli>,
<https://docs.github.com/en/copilot/reference/custom-agents-configuration>

## 3. Package and plugin support

**VERIFIED.** Copilot CLI 1.0.77 has a real plugin system. A plugin is a directory whose required
root manifest is **`plugin.json`** — not `manifest.yaml`. It can bundle agents, skills, hooks, MCP
configuration, LSP configuration, commands, and extensions.

Minimum manifest:

```json
{ "name": "blazor-orchestration" }
```

Optional: `$schema`, `description`, `version`, `author`, `homepage`, `repository`, `license`,
`keywords`, `category`, `tags`. Component fields: `agents`, `skills`, `commands`, `hooks`,
`extensions`, `mcpServers`, `lspServers`.

Install a local plugin with `copilot plugin install PATH`. The CLI **caches installed content**, so
local changes require reinstalling.

Local evidence: real installed plugins exist under `C:\Users\g.vd.venis\.copilot\installed-plugins\`,
registered in `C:\Users\g.vd.venis\.copilot\config.json`. An installed Blazor plugin has a standard
manifest at `installed-plugins\dotnet-agent-skills\dotnet-blazor\plugin.json`.

**UNVERIFIED / unsupported.** No documentation identifies `~/.copilot/packages/` or `manifest.yaml`
as loader conventions. The package's current `manifest.yaml` is therefore **not** a Copilot loader
manifest.

Sources: <https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/plugins-creating>,
<https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-plugin-reference>

## 4. Skill arguments and flags

**VERIFIED.** Copilot accepts text following a skill slash command, e.g.
`/Markdown-Checker check README.md`, and a skill may be referenced in a prompt as `/frontend-design`.

**UNVERIFIED for Copilot CLI.** GitHub's documentation does not specify `$ARGUMENTS`, indexed
substitutions, named arguments, or `argument-hint`. Those are **Claude Code extensions**.

Consequence: `/blazor-orchestrator --self-improve` can be treated as invocation text parsed by the
skill's own instructions, but `$ARGUMENTS` substitution must not be assumed.

Sources: <https://docs.github.com/en/copilot/concepts/agents/copilot-cli/comparing-cli-features#skills>,
<https://code.claude.com/docs/en/skills#pass-arguments-to-skills>

## 5. Spawning sub-agents

**VERIFIED.** The main agent can invoke built-in and custom agents as sub-agents. A custom agent is
selected via `/agent`, explicit instruction, description inference, or
`copilot --agent NAME --prompt ...`.

The `agent` tool alias (compatible with `Task`) allows invoking a different custom agent. The
built-in `task` agent is for commands such as tests and builds — it is not synonymous with every
specialist custom agent.

Nested delegation exists in current builds: version 1.0.74 records sub-agent prompts originating from
"another subagent", and later releases expose configurable sub-agent depth and concurrency. To
prevent specialists from delegating, **omit `agent`/`Task` from their `tools` allowlists**.

**Important limitation.** A skill is injected instructions, not a separately permissioned execution
identity. A skill *cannot technically guarantee* that it alone may use the delegation tool.
Specialists can be hard-restricted, but "only the orchestrator spawns agents" remains a prompt/hook
policy unless the orchestrator is itself an agent with a dedicated tool allowlist.

Sources: <https://docs.github.com/en/copilot/concepts/agents/copilot-cli/about-custom-agents>,
<https://raw.githubusercontent.com/github/copilot-cli/main/changelog.md>

## 6. Orchestration best practices

**VERIFIED.** Keep routing metadata short and load detail on demand. Put what/when triggers in
descriptions. Delegate research-heavy, independent, verification, or context-polluting work. Avoid
delegation when startup and context overhead exceed the benefit. Request scoped summaries rather than
raw file dumps. Give specialists narrow tools and focused prompts.

GitHub documents context isolation and parallelism but publishes no fixed per-delegation token
charge; Anthropic's cost observations are design guidance, not Copilot billing guarantees.

Sources: <https://agentskills.io/specification#progressive-disclosure>,
<https://claude.com/blog/subagents-in-claude-code>

## 7. Copilot CLI versus Claude Code

Both implement the open Agent Skills core. Claude Code adds non-portable fields and behaviour:
`$ARGUMENTS`, `arguments`, `argument-hint`, `context: fork`, `agent`, dynamic shell injection, and
Claude-specific directories. Copilot-specific differences: multiple project skill roots, personal
`~/.copilot/skills`, custom agents in `~/.copilot/agents`, canonical Copilot tool aliases, and its own
`plugin.json` loader.

## Implications for the package

1. Replace loader reliance on `manifest.yaml` with a root **`plugin.json`**.
2. Keep `agents/*.agent.md` and `skills/<name>/SKILL.md` as the component layout.
3. Install with `copilot plugin install <package-directory>`.
4. Verify with `/plugin list`, `/agent`, `/skills list`.
5. Reinstall after local changes, because installed content is cached.
6. Grant specialists only required tools; omit `agent` to prohibit nested delegation.
7. Keep the orchestrator as a skill, but treat "only the orchestrator delegates" as an architectural
   policy, not an enforceable ACL.
8. Preserve telemetry schemas as plugin resources referenced by skills, or implement deterministic
   telemetry through hooks.

## Open/unverified

- Whether Copilot CLI 1.0.77 implements Claude-style `$ARGUMENTS` substitution.
- Exact sub-agent depth defaults, and whether they vary by plan.
- Whether `skill`, `web_search`, and `web_fetch` are valid exact agent tool names, or whether the
  canonical `agent`/`web` aliases should be used instead.
- Whether a hook can reliably enforce "delegation only while the orchestrator skill is active".
- `.copilot/packages/` has no verified loader role.
