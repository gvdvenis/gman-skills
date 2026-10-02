# Human steps

Some preconditions only the user can meet: a token, an allowlist entry, a workload install.

Name the exact thing needed and where it comes from, checked against the corrections. Give the
command for the user to run in a terminal of their own. The token then stays out of the
conversation, and the session's safety checks block agents from reading one anyway. The command
reads the token from its variable when it runs (`%VAR%` in a `nuget.config`, `$env:VAR` in a
script), so no file on disk ever holds it.

Two facts save round trips:

- A user-level environment variable reaches only processes started after it was set. Claude Code
  and an IDE terminal that were already open do not see it. A script the user runs, which reads
  the variable from the user scope, works at once.
- A package restore the user runs once fills the machine's package cache. Agents then build from
  the cache with no credential.

Park only the tickets that need the step; the rest of the frontier keeps going. Before the turn
ends waiting on the user, post the hand-off note on the spec ticket (see "Keep the orchestrator
context small" in SKILL.md), since a restart loses this context.
