# Scenario: notifications while waiting, then a merge

State: the worker for #154 is running (agent id a813d3a8). Nothing else is in flight. You (the orchestrator) receive,
one after another, in three separate turns:

Turn 1: `<task-notification> Agent a813d3a8: background work still running (command "dotnet test ..." started 4 min ago) </task-notification>`
Turn 2: `<task-notification> Agent a813d3a8: background work still running </task-notification>`
Turn 3: the worker's hand-back:
```
state: ready-to-integrate
commit: 7c1e0aa
changed: src/ShopConfigurator.Server/Pricing/PackagePriceReader.cs, tests/.../PackagePriceReaderTests.cs
tests: dotnet test --filter "FullyQualifiedName~PackagePriceReader" - 14/0
review: done
criteria: ...
```
Turn 4: the integrator's report: "merged 4e2a9d1 (parents b0ccf14, 7c1e0aa); tests 14/0; #154 closed with summary".

Stopping point: the end of turn 4 (you do not need to start Completion). Write each turn's actions separately,
including the exact end-of-turn text, and any file you keep updated.
