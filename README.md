# gman-skills

Personal Claude Code and Copilot CLI skills for Blazor orchestration, spec-ticket implementation
and plain-language writing.

`gman-skills` packages a thin Blazor-focused orchestration layer, an opt-in self-improvement
report generator, and a first-time setup skill. It is distributed as a public GitHub repo
that is its own plugin marketplace.

## Install

Claude Code:

```sh
claude plugin marketplace add gvdvenis/gman-skills
claude plugin install gman-skills@gman-skills
```

Copilot CLI:

```sh
copilot plugin marketplace add gvdvenis/gman-skills
copilot plugin install gman-skills@gman-skills
```

The plugin includes a session-start hook that loads the `use-plain-language` rules in every
session. In Claude Code the skills are prefixed with the plugin name, e.g.
`/gman-skills:setup-gman-skills`.

Other agents: `npx skills add gvdvenis/gman-skills` installs the skills without the hook.

## First-time setup

After installing, run the setup skill once to bootstrap external dependencies:

```
/setup-gman-skills
```

Setup does two things:

1. **dotnet-blazor plugin** - checks whether the `dotnet-blazor` Copilot CLI plugin is installed
   (via `copilot plugin list`). If missing, it adds the `dotnet/skills` marketplace and installs
   `dotnet-blazor@dotnet-agent-skills`. This plugin provides the single-lane Blazor component
   skills (author-component, collect-user-input, fetch-and-send-data, etc.) that `blazor-architect`
   delegates to.
2. **report-server binary** - checks whether the report-server binary exists at
   `~/.copilot/gman-skills/bin/`. If missing, it queries the GitHub Releases API for
   `gvdvenis/gman-skills`, downloads the platform-appropriate `report-server-{os}-{arch}.zip`,
   and extracts it to `~/.copilot/gman-skills/bin/`. The binary is the local C# server that
   `self-improve` auto-launches to serve the improvement report UI.

## Requirements

The `vsreview` skill is a Firstmate helper and needs:

- [Firstmate](https://github.com/kunchenguid/firstmate). The skill reads its task records. It finds the Firstmate home from `$FM_HOME`, else the current folder or a parent, else `~/firstmate`. Without Firstmate it says so and stops.
- VS Code with the `code` command on PATH. On WSL, if `code` is missing but VS Code is installed on Windows, the skill tells you which folder to add to PATH; if VS Code is not installed, install it on Windows first.

The other skills do not need either.

## Skills overview

| Skill | Description | User-invocable |
|---|---|---|
| `blazor-architect` | Route a full Blazor work request across the appropriate specialist lane(s). User-invoked only: `/blazor-architect <task> [--skip-code-review] [--self-improve]`. Delegates to dotnet-blazor plugin skills as specialist resources. | Yes |
| `self-improve` | Loaded by `blazor-architect` when `--self-improve` is active. Handles improvement report generation (algorithm, dedup, ranking), report-server auto-launch on port 5173, and CLI staging readiness. | No |
| `setup-gman-skills` | First-time setup: installs the dotnet-blazor plugin dependency and downloads the report-server binary from GitHub Releases. Run once after installing the plugin or `npx skills add`. | Yes |
| `implement-spec-tickets` | Implement every ticket of one spec: discovers its dependency graph, dispatches parallel worktree agents, and coordinates merge and closure. Invoke as `/implement-spec-tickets <SPEC-TICKET-ID>`, or with a spec folder on a local markdown tracker. | Yes |
| `vsreview` | Firstmate helper skill: opens a finished crewmate task in VS Code (a ship's local copy on its branch, or a scout's report) and says what to review and where to start. `/vsreview [task-id]`; a part of an id works (`/vsreview mainthread`), and an unclear id lists the candidate tasks with their backlog titles. Requires Firstmate and VS Code (see Requirements). | Yes |
| `use-plain-language` | Plain-language rules for everything written to the user, with extra rules for question rounds, proposed names and hand-backs. The plugin's session-start hook loads it in every session (Claude Code and Copilot CLI). | No (loaded by the hook) |

## Dev workflow

### Edit skills

Skill content lives in `skills/<skill-name>/SKILL.md` plus bundled reference files under
`skills/<skill-name>/references/`. This is the single source of truth - there is no `agents/`
directory, no `plugin.json`, no `manifest.yaml`. Edit the markdown directly.

### Build the report-server

The report-server C# source lives under `src/report-server/`. Build it locally for development:

```sh
cd src/report-server
dotnet build
dotnet test      # runs the xUnit contract tests
```

### Publish a release

The report-server binary is distributed via GitHub Releases and downloaded by the setup scripts -
it is never committed to the repo. To publish a new binary:

```sh
cd src/report-server
dotnet publish -c Release -r win-x64
# zip the publish output so the binary is at the archive root
gh release create vX.Y.Z report-server-win-x64.zip
```

The zip's internal layout must place the binary at the root, matching what the download script
expects. Update `setup-gman-skills` if you change the asset naming convention.

## Language

See [`CONTEXT.md`](CONTEXT.md) for the full ubiquitous-language glossary (blazor-architect,
self-improve, report-server, setup-gman-skills, route classifier, specialist lane, run ID,
suggestion key, dotnet-blazor).

## License

Personal project.