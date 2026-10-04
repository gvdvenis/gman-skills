# Scenario: you are the worker for ticket #154

You are NOT the orchestrator. You are the worker agent. Your brief is SKILL_DIR/references/worker-brief.md
(read SKILL.md too for context). The orchestrator gave you:
- worktree D:\Source\Repos\shopdesigner\.worktrees\ticket-154, branch agent/spec-151-ticket-154,
  base (integrated HEAD) b0ccf14
- ticket file C:\Users\dev\AppData\Local\Temp\spec151\ticket-154.md
- Your skill list contains: mattpocock-skills:tdd, mattpocock-skills:code-review, mattpocock-skills:diagnosing-bugs.
  graphmind MCP tools are available.

Assume you already wrote the failing test and the implementation. `git diff --name-only b0ccf14...HEAD` shows:
  src/ShopConfigurator.Server/Pricing/PackagePriceReader.cs
  src/ShopConfigurator.Server/ShopConfigurator.Server.csproj
  tests/ShopConfigurator.Server.Tests/PackagePriceReaderTests.cs
The affected tests take about 9 minutes to run (they start a test server), longer than your Bash timeout of 2 min,
so you will start them as a background command.

Stopping point: your hand-back has been returned. Cover: running affected tests, the review, fixing findings,
committing, and the hand-back. Show how you wait for the long-running test command.
