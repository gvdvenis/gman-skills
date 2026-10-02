# Agent guidance

## Repo structure

This repo is a plugin for Claude Code and Copilot CLI, and also installable with `npx skills`.
The single source of truth for skill content is the root-level `skills/` directory; both plugin
manifests and `npx skills` read it:

- `skills/blazor-architect/` - orchestration skill (user-invocable).
- `skills/self-improve/` - self-improvement report skill (loaded by blazor-architect, not user-invocable).
- `skills/setup-gman-skills/` - first-time dependency + binary bootstrap skill (user-invocable).
- `skills/implement-spec-tickets/` - implements every ticket of one spec with parallel worktree agents (user-invocable).
- `skills/use-plain-language/` - plain-language rules, loaded at every session start by the plugin hook (not invocable).
- `src/report-server/` - C# report-server source, built and published to GitHub Releases.
  The binary is never committed; it is downloaded by `setup-gman-skills` to `~/.copilot/gman-skills/bin/`.
- `.claude-plugin/` - Claude Code plugin and marketplace manifests; hook in `hooks/claude-hooks.json`.
- `.github/plugin/` - Copilot CLI plugin and marketplace manifests; hook in `hooks/copilot-hooks.json`.
  The two tools use different hook formats, so each manifest names its own hook file.
- `docs/` - package contracts, agent guidance, and specs.
- `CONTEXT.md` - ubiquitous language for the domain.

Install as a plugin (see README); first-time setup with `/setup-gman-skills`. An `npx skills`
install gets the skills but not the session-start hook.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the default five canonical triage labels. See `docs/agents/triage-labels.md`.

### Domain docs

This is a single-context package using root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.