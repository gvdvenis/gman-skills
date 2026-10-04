# Dry-run instructions (read first)

You are being tested on how well you follow a skill. The skill is in the folder given to you as SKILL_DIR (normally this skill folder):
read SKILL_DIR/SKILL.md and every file in SKILL_DIR/references/ before answering.

This is a DRY RUN. Do not run git, glab, dotnet, or any other command against a real repository, and do not
spawn agents. Only read the skill files. Then write, to the output file you were given, the exact sequence of
actions you would take from the state described below until you reach the stopping point named in the
scenario. For each action write one of:

- `TOOL <name>: <arguments>` (Bash commands written out in full; Agent calls with their full prompt text)
- `SAY: <exact text of your message to the user / end-of-turn text>` (write `SAY: <empty>` if you end the turn with no text)
- `TRACKER: <ticket> <exact comment text, or checkbox change, or state change>`
- `FILE: <path> <what you write>`
- `WAIT: <what you wait for and how>`

Where a result is needed to decide the next step, state the assumption you branch on
(e.g. "IF rerun passes both times: ... ELSE: ..."). Be concrete, follow the skill exactly, and do not add
steps the skill does not ask for. Keep it under 120 lines.

# Shared state of the run

- Repository: D:\Source\Repos\shopdesigner (Windows, .NET 10, git 2.47). Tracker: GitLab, project
  acme/shop-configurator, used through `glab` as docs/agents/issue-tracker.md describes.
  Your GitLab username: dev-user. Remote: origin.
- Spec: #151 "Price lookup from the pricing package". Child tickets: #152 and #153 (both closed in an EARLIER
  run on 2026-09-30), #154 (this run).
- Parent branch: main. Integration worktree: D:\Source\Repos\shopdesigner\.worktrees\spec-151.
  The repository root D:\Source\Repos\shopdesigner was switched to a detached HEAD by step 1.
- Starting HEAD of this run: b0ccf14. The repository's full suite is `pwsh ./build.ps1 -AsShipped -Test`
  (about 25 minutes, 271 tests). The session has no scratchpad folder.
