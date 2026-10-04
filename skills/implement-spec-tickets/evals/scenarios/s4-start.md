# Scenario: starting the run

The user typed: `/implement-spec-tickets #151`. You are at the very start (nothing done yet).
Facts you will find when you look (assume these are the command results):
- Checked-out branch in the root: main. `git status` is clean. `git rev-parse HEAD` = b0ccf14.
- `git log origin/main..HEAD --oneline` = `b0ccf14 chore: bump test timeout` (the user's own, unpushed).
- No milestone or project memory names an effort branch.
- The spec #151 says the package exposes a `missingPrices` list, and its acceptance criterion reads
  "- [ ] the export lists every product in `missingPrices` in a warning row".
- The spec also says the package comes from the "acme-internal" NuGet feed on Azure Artifacts.
- Graph discovery will return: map #152 -> #154, #153 -> #154; #152 and #153 closed; #154 open, unclaimed.
  Corrections: (1) "#153 comment 2026-09-29: the package moved from Azure Artifacts to the GitLab package
  registry of group acme"; (2) "#152 body: the 2.0 package dropped `missingPrices`; products without
  a price now come back with `price: null`".

Stopping point: the moment the worker for #154 has been dispatched. Include the full prompt you give the
graph-discovery agent and the worker.
