# Scenario: the machine kills the suite

State: all tickets (#152, #153 earlier, #154 this run) are merged and closed. Integrated HEAD is 4e2a9d1.
You started the full suite in a background agent. You just received:

```
<task-notification> Agent "full-suite" stopped: the process was killed because the system is low on memory
(exit code 137). Last output: "Building ShopConfigurator.Client (AsShipped)... " </task-notification>
```

No test result was produced. Three `dotnet` build-server processes (MSBuild node reuse, VBCSCompiler, Razor)
are still running from the earlier integrator builds. The user is not at the keyboard.

Stopping point: the end of your handling of this event (including what happens if it occurs again).
