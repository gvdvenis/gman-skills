# gman-skills

Personal Copilot CLI skills for Blazor orchestration and self-improvement.

`gman-skills` packages a thin Blazor-focused orchestration layer, an opt-in self-improvement
report generator, and a first-time setup skill. It is distributed as a public GitHub repo
installable with `npx skills`.

## Install

```sh
npx skills add gvdvenis/gman-skills
```

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

Idempotent preflight scripts (`check-deps.ps1` / `check-deps.sh`) warn on missing components
without blocking.

## Skills overview

| Skill | Description | User-invokable |
|---|---|---|
| `blazor-architect` | Route a full Blazor work request across the appropriate specialist lane(s). Triggers on full-request, multi-concern phrasing ("implement this feature", "review and refactor this page"). Delegates to dotnet-blazor plugin skills as specialist resources. | Yes |
| `self-improve` | Loaded by `blazor-architect` when `--self-improve` is active. Handles improvement report generation (algorithm, dedup, ranking), report-server auto-launch on port 5173, and CLI staging readiness. | No |
| `setup-gman-skills` | First-time setup: installs the dotnet-blazor plugin dependency and downloads the report-server binary from GitHub Releases. Run once after `npx skills add`. | Yes |

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