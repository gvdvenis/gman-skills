# Agent guidance

## Repo structure

This repo is a `npx skills`-distributed skills package, not a Copilot CLI plugin. The single
source of truth for skill content is the root-level `skills/` directory:

- `skills/blazor-architect/` - orchestration skill (user-invokable).
- `skills/self-improve/` - self-improvement report skill (loaded by blazor-architect, not user-invokable).
- `skills/setup-gman-skills/` - first-time dependency + binary bootstrap skill (user-invokable).
- `src/report-server/` - C# report-server source, built and published to GitHub Releases.
  The binary is never committed; it is downloaded by `setup-gman-skills` to `~/.copilot/gman-skills/bin/`.
- `docs/` - package contracts, agent guidance, and specs.
- `CONTEXT.md` - ubiquitous language for the domain.

There is no `agents/` directory, no `plugin.json`, no `manifest.yaml`, and no plugin install path.
Install with `npx skills add gvdvenis/gman-skills`; first-time setup with `/setup-gman-skills`.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the default five canonical triage labels. See `docs/agents/triage-labels.md`.

### Domain docs

This is a single-context package using root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.