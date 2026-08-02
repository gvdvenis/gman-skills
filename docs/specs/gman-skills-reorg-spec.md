**Label:** ready-for-agent

# gman-skills: ship as a published skills repo

## Problem Statement

A developer uses a Blazor orchestration package to route multi-concern Blazor work through a thin orchestrator with specialist delegation, local telemetry, and an opt-in self-improvement report. The package currently lives in its original plugin-layout form — a single `blazor-orchestrator` skill that bundles orchestration and self-improvement concerns, specialist agents as separate files, and telemetry schemas in a top-level directory. This structure cannot be distributed cleanly via `npx skills`, the self-improvement flow is not separable from orchestration, and first-time setup (dotnet-blazor dependency, report-server binary) is manual and undocumented. The developer needs a published, installable skills repo where the three skills are cleanly separated, setup is automated, and the full install-to-run path works on Windows.

## Solution

Publish a public GitHub repo (`gvdvenis/gman-skills`) containing three skills — `blazor-architect` (orchestration), `self-improve` (report generation + server), and `setup-gman-skills` (dependency + binary bootstrap) — distributed via `npx skills add gvdvenis/gman-skills`. The repo uses a root-level `skills/` directory as the single source of truth (no `agents/`, no `plugins/`), with the report-server C# source under `src/` for dev-only builds and its binary distributed via GitHub Releases. First-time setup is handled by invoking `/setup-gman-skills`, which installs the dotnet-blazor plugin dependency and downloads the report-server binary. The full path — install, setup, invoke `/blazor-architect` with and without `--self-improve` — is verified by a headless smoke test driven by `copilot -p`, which can run locally and in CI.

## User Stories

1. As a developer, I want to install the skills from a public GitHub repo, so that I don't need a local path or plugin marketplace.
2. As a developer, I want `blazor-architect` and `self-improve` to be separate skills, so that orchestration and self-improvement concerns are independently maintained.
3. As a developer, I want `self-improve` to be non-user-invokable, so that it is only loaded by `blazor-architect` when `--self-improve` is active.
4. As a developer, I want `blazor-architect` to announce a run ID at the start of every invocation, so that I can correlate outputs across a run.
5. As a developer, I want `blazor-architect` to parse `--skip-code-review` and `--self-improve` from natural language, so that I don't need to remember exact flag syntax.
6. As a developer, I want `blazor-architect` to apply the route classifier inline-vs-delegate decision before touching files, so that narrow work stays inline and broad work is delegated.
7. As a developer, I want `blazor-architect` to fold the five specialist agent definitions into delegation instructions inside its SKILL.md, so that specialist scope, tool constraints, and report format live in one place.
8. As a developer, I want `blazor-architect` to document its relationship with dotnet-blazor plugin skills, so that it is clear it delegates to those skills rather than replacing them.
9. As a developer, I want `blazor-architect` to reference the routing classifier and review-loop contract from bundled reference files, so that the SKILL.md body stays under 500 lines.
10. As a developer, I want `blazor-architect` to hook into `self-improve` with one paragraph when `--self-improve` is active, so that the full report-generation algorithm is not duplicated in the orchestration skill.
11. As a developer, I want `self-improve` to contain the complete report-generation algorithm, so that suggestion-key derivation, cross-run dedup, ranking, and history weighting are in one skill.
12. As a developer, I want `self-improve` to contain the report-server auto-launch instructions, so that port binding, lifecycle, and exit codes are documented.
13. As a developer, I want `self-improve` to contain the CLI staging readiness signal and conflict flow, so that git staging of the report data file is deterministic.
14. As a developer, I want `self-improve` to reference all telemetry schemas from bundled reference files, so that schema files are loaded on demand.
15. As a developer, I want a `/setup-gman-skills` skill, so that first-time setup is a single command.
16. As a developer, I want `/setup-gman-skills` to check whether the dotnet-blazor plugin is installed, so that the dependency is confirmed or installed.
17. As a developer, I want `/setup-gman-skills` to install the dotnet-blazor plugin via the dotnet/skills marketplace when missing, so that the dependency is resolved automatically.
18. As a developer, I want `/setup-gman-skills` to check whether the report-server binary exists, so that it skips download when already present.
19. As a developer, I want `/setup-gman-skills` to download the report-server binary from GitHub Releases when missing, so that the self-improvement server is available without a local build.
20. As a developer, I want the download to detect OS and architecture, so that the correct binary is fetched on Windows, Linux, and macOS.
21. As a developer, I want the download script to install the binary to `~/.copilot/gman-skills/bin/`, so that the path is predictable across skills.
22. As a developer, I want `/setup-gman-skills` to print a summary of what was installed and what was already present, so that I can confirm setup state.
23. As a developer, I want idempotent check-deps scripts, so that setup and future session-start hooks can preflight without blocking.
24. As a developer, I want the download script to handle a missing GitHub Release gracefully, so that setup fails with instructions rather than a raw error.
25. As a developer, I want the repo to have a README, so that visitors understand what the skills are and how to install them.
26. As a developer, I want the README to show the `npx skills add` install command, so that installation is copy-pasteable.
27. As a developer, I want the README to document first-time setup as `/setup-gman-skills`, so that the dependency + binary bootstrap step is discoverable.
28. As a developer, I want the README to document the dev workflow, so that contributors know to edit in `skills/`, build in `src/`, and publish releases with `gh release create`.
29. As a developer, I want a skills overview table in the README, so that the name, description, and user-invokable status of each skill is visible at a glance.
30. As a developer, I want AGENTS.md to reflect the new repo structure, so that agent sessions in the repo use correct paths.
31. As a developer, I want `.scratch/` to be gitignored, so that local issue tracking and wayfinder artifacts are not committed.
32. As a developer, I want the repo published to `gvdvenis/gman-skills` on GitHub, so that it is installable via `npx skills add gvdvenis/gman-skills`.
33. As a developer, I want the report-server binary published to a GitHub Release, so that the download script can fetch it without a local build.
34. As a developer, I want to verify the full install-to-run path works, so that I can ship with confidence.
35. As a developer, I want to verify that `/blazor-architect` with `--self-improve` generates `improvement-report-data.json`, so that the report flow fires end-to-end.
36. As a developer, I want to verify that the report-server auto-launches on port 5173 under `--self-improve`, so that the browser can reach the report.
37. As a developer, I want to verify that without `--self-improve` the report flow does not trigger, so that normal runs stay clean.
38. As a developer, I want the smoke test to run headless via `copilot -p`, so that it can execute in CI as well as locally.
39. As a developer, I want the smoke test to assert the run ID format in the first output line, so that the run-announcement contract is verified.
40. As a developer, I want the smoke test to assert the report-server responds on `/api/report`, so that the server lifecycle is verified.
41. As a developer, I want the smoke test to assert the `--self-improve` flag toggle, so that the opt-in behavior is verified.
42. As a developer, I want `npx skills add` to list all three skills, so that the install is confirmed to expose the full set.
43. As a developer, I want `npx skills update` to work, so that skill updates flow without reinstalling the repo.
44. As a developer, I want the repo to have no leftover files from the old plugin layout, so that the structure is clean and unambiguous.
45. As a developer, I want `docs/interactive-flow.md` removed if it is stale, so that documentation does not contradict the specs.

## Implementation Decisions

### Repo structure

- Single monorepo, no plugin path. Distributed via `npx skills add gvdvenis/gman-skills`. No `plugin.json`, no `agents/` directory, no `manifest.yaml`.
- Root-level `skills/` is the single source of truth for skill content.
- `src/report-server/` holds the C# report-server source for dev-only builds. The binary is not committed; it is published to GitHub Releases and downloaded by setup scripts.
- `docs/` holds package contracts, agent guidance, and specs. Agent-consumed references (routing classifier, review-loop contract) live inside `skills/blazor-architect/references/`. Telemetry schemas and generation docs live inside `skills/self-improve/references/`.
- `.scratch/` is gitignored. It holds local issue tracking and wayfinder maps.

### Skill split: blazor-architect

- Frontmatter: `name: blazor-architect`, `user-invokable: true`. Description triggers on full-request, multi-concern Blazor phrasing and is distinct from `blazor-component-architect` (user-level, external).
- Body contains: run ID generation + announcement, flag parsing, route classifier reference, inline-vs-delegate execution, specialist delegation instructions, aggregation + report validation, review-loop reference, run summary format, and a one-paragraph `--self-improve` hook that instructs loading the `self-improve` skill.
- The five deleted specialist `.agent.md` files are folded into a delegation-instructions section. Each lane preserves its scope, tool constraints, in-scope/out-of-scope boundary, report format, and completion criteria. Delegation uses the `task` tool with references to dotnet-blazor plugin skills (e.g. "use `dotnet-blazor:author-component` as guidance").
- Collision boundary with `blazor-component-architect` is documented: blazor-architect is the routing and aggregation surface; blazor-component-architect is single-lane authoring guidance that may be invoked as a specialist resource.

### Skill split: self-improve

- Frontmatter: `name: self-improve`, `user-invokable: false`. Description states it is loaded by blazor-architect when `--self-improve` is active and should not be invoked directly.
- Body contains: the complete self-improvement report generation algorithm (suggestion-key derivation, cross-run dedup fold rules, ranking formula, dismissal and history-weight application, ordered generation algorithm), report-server auto-launch instructions (port, bind address, lifecycle, exit codes), CLI staging readiness signal, and conflict flow.
- All schemas and the generation doc are referenced from `references/`, not inlined.

### Setup skill: setup-gman-skills

- Frontmatter: `name: setup-gman-skills`, `user-invokable: true`. Run once after `npx skills add`.
- Workflow: check dotnet-blazor plugin via `copilot plugin list`; if missing, `copilot plugin marketplace add dotnet/skills` then `copilot plugin install dotnet-blazor@dotnet-agent-skills`; check report-server binary at `~/.copilot/gman-skills/bin/`; if missing, run the platform-appropriate download script; print summary.
- Download scripts (`setup-report-server.ps1` / `.sh`): detect OS + architecture, construct the GitHub Releases download URL, download to temp, extract to `~/.copilot/gman-skills/bin/`, make executable on Unix, verify the binary runs, print installed path. Handle missing release with a graceful error + instructions.
- Check-deps scripts (`check-deps.ps1` / `.sh`): idempotent preflight — warn on missing dotnet-blazor or binary, exit 0 (never block).
- Download scripts are shared: copied to both `skills/setup-gman-skills/scripts/` and `skills/self-improve/scripts/`.

### GitHub repo + release

- `git init` (already done), update `.gitignore` (already covers `.scratch/`, `src/` build artifacts), update AGENTS.md to reflect new structure, write README.md with install + setup + dev-workflow + skills-overview sections.
- `gh repo create gvdvenis/gman-skills --public --source=. --push`.
- Build report-server: `dotnet publish -c Release -r win-x64`, zip the output, `gh release create v0.1.0 report-server-win-x64.zip`.
- The zip's internal layout must place the binary at root, matching what the download script expects.

### Smoke test

- Driven by `copilot -p` (headless, non-interactive). Can run locally and in GitHub CI.
- Uses `--no-ask-user --allow-all-tools` to avoid permission prompts.
- Run in a temp Blazor project (created via `dotnet new blazor` or a fixture project).
- Assertions are on observable contract outputs, not internal reasoning:
  1. Run ID format (`run-YYYYMMDD-HHMM`) appears in the first output line.
  2. With `--self-improve`: `improvement-report-data.json` exists at `~/.copilot/blazor-orchestration/runs/<run_id>/`.
  3. With `--self-improve`: report-server responds on `http://127.0.0.1:5173/api/report`.
  4. Without `--self-improve`: no `improvement-report-data.json` is generated, no server launches.
- The smoke test is non-deterministic in LLM output but deterministic in contract side-effects. It verifies the pipeline fires, not that the generated code is correct.
- 2-3 test cases: one with `--self-improve`, one without, one verifying `npx skills add --list` shows all three skills.

## Testing Decisions

### What makes a good test here

A good test asserts external, observable contract behavior — not the internal reasoning of the LLM or the file shape of markdown. The skills are markdown consumed by an agent; their "test" is whether the observable side-effects of invocation match the documented contract (run ID format, file generation, server lifecycle, flag toggle). The report-server is C# code; its tests assert API contract behavior and persisted state.

### Seams

**Seam A — Skill invocation smoke test (new):** Run `blazor-architect` headless via `copilot -p` in a temp Blazor project. Assert the observable contract: run ID format, `improvement-report-data.json` generation under `--self-improve`, report-server responding on port 5173, and the opt-in toggle (no report artifacts without the flag). This is the highest seam — it tests the full install-to-run path. It is non-deterministic in LLM output but deterministic in side-effect contracts. 2-3 test cases.

**Seam B — Report-server unit tests (existing):** The 5 existing xUnit/MSTest tests cover `ServerOptions` parsing, `PromptCompressor` behavior, `ReportStore` dismiss/ship persistence, and missing-finding errors. No new tests needed for this effort; the build + test run is the validation gate for the binary that gets published to the release.

**No seam for repo publishing:** `gh repo create` and README rendering are verified by `gh repo view` and visual inspection. No automated seam — the `npx skills add --list` assertion in the smoke test confirms the repo is installable.

### Prior art

- Report-server tests: `ReportServer.Tests/ReportServerTests.cs` — fixture-based contract validation with temp directories and deterministic JSON inspection. This is the established pattern for C# code in this repo.
- Skill eval pattern: the skill-creator's `run_eval.py` uses `copilot -p` with `--output-format stream-json` to detect skill triggering. The smoke test reuses the `copilot -p` headless pattern but asserts side-effects rather than trigger behavior.
- The existing specs (`docs/specs/blazor-orchestration-core-spec.md`, `docs/specs/self-improvement-report-spec.md`) both specify "contract-first validation with deterministic payload inspection" as their testing approach — this spec follows the same philosophy.

## Out of Scope

- Linux/macOS CI and multi-platform binary publishing. The destination is a working Windows install.
- Skill eval harness (the dotnet/skills `tests/` + `eng/run-skill-evals.sh` pattern). Valuable for future rigor but not needed for personal-use validation.
- Marketplace listing (`marketplace.json` for `copilot plugin marketplace`). Only relevant if the plugin path is added.
- Plugin path (`plugin.json` + `agents/` + `hooks/`). Can be added later for convenience without restructuring.
- Changesets / automated versioning. Relevant for the CI/release phase, not this effort.
- Custom installer CLI (a .NET CLI wrapping `npx skills` with `--with-dependencies`). Parked until manual setup friction becomes real.
- Issue tracker migration to GitHub Issues. The local markdown tracker works; migration is a future effort once the GitHub remote exists.
- `/self-improve` standalone mode (analyzing past run artifacts independently, decoupled from blazor-architect). Natural graduation from the current gated integration.
- Automatic prompt application to agents/skills from self-improvement output. The self-improve flow produces recommendations and a shippable prompt; it never mutates skills automatically.

## Further Notes

- The folder reorganization (ticket 01) is already complete: 43 tracked files, `dotnet build` and `dotnet test` pass. The remaining work is the skill split (ticket 02), setup skill creation (ticket 03), GitHub repo + release publish (ticket 04), and end-to-end verification (ticket 05, now realized as the smoke test seam).
- `docs/interactive-flow.md` was kept during reorganization but is largely superseded by `docs/specs/blazor-orchestration-core-spec.md`. It should be deleted during this effort if no longer referenced.
- The `blazor-architect` SKILL.md currently still has the old `blazor-orchestrator` name and frontmatter. The split requires rewriting the frontmatter and body, not just renaming the file.
- The `self-improve` SKILL.md does not exist yet. It must be created from the self-improvement sections currently in the `blazor-orchestrator` SKILL.md body plus the `references/self-improve-generation.md` doc.
- The `setup-gman-skills` SKILL.md and all scripts do not exist yet.
- The report-server binary must be built and published to a GitHub Release before the smoke test can verify the `--self-improve` server-launch path. The download script must handle the case where the release does not exist yet.
- The smoke test's `copilot -p` invocation requires the skills to be installed (via `npx skills add`) in the environment running the test. In CI, this means the install step runs before the smoke test step.
- All decisions in this spec were locked during the wayfinder charting session. The closed ticket bodies in `.scratch/gman-skills-reorg/issues/` contain the detailed execution checklists that informed these decisions.
