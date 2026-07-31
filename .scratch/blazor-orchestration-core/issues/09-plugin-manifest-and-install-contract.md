# Plugin manifest and install contract

Type: grilling
Status: resolved
Assignee: @gvdvenis

## Question

Now that `plugin.json` is confirmed as the real loader manifest, what exactly does this plugin declare?

Decide:

- The plugin `name`, and whether it collides with the already-installed `dotnet-blazor` plugin.
- Which optional metadata is worth carrying: `version`, `description`, `author`, `category`, `tags`.
- Which component fields are declared — `agents`, `skills`, and whether `hooks`, `commands`, or
  `mcpServers` earn a place.
- What becomes of the existing `manifest.yaml`: delete it, or keep it as human documentation with an
  explicit note that it is not a loader contract.
- The documented install and reinstall procedure, given that installed content is cached.
- How the package coexists with the user-level `blazor-component-architect` skill and agent that
  already exist outside the plugin.

## Context

See [Harness packaging and loader format](01-harness-packaging-and-loader-format.md) and
[01-findings.md](01-findings.md).

## Answer

Use a first-class Copilot plugin manifest and lock this package to a non-colliding plugin identity:

1. **Plugin name**: `blazor-orchestration-core` (distinct from installed `dotnet-blazor`).
2. **Metadata to carry**: include `version`, `description`, `author`, and `tags`; leave `category`
   optional unless distribution/discovery needs it.
3. **Declared components**: declare `agents` and `skills`; add `hooks` for deterministic telemetry
   and policy-drift detection. Do not declare `commands` or `mcpServers` in the core package now.
4. **`manifest.yaml` status**: keep it only as human-facing package documentation, with an explicit
   note that `plugin.json` is the loader contract.
5. **Install lifecycle**: install with `copilot plugin install <package-path>`; after any local
   plugin change, reinstall because installed plugin content is cached.
6. **Coexistence with user-level Blazor assets**: avoid duplicate names for bundled agents/skills.
   Keep plugin-scoped names unique and treat existing user-level `blazor-component-architect`
   tooling as external optional guidance rather than overridden package components.
