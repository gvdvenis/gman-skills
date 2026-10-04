# Scenario: the full suite fails

State: #154 was merged by its integrator (merge 4e2a9d1) and closed. Integrated HEAD is now 4e2a9d1. No ticket is
pending, implementing or integrating. You started the full suite in the integration worktree and it just finished:

```
Failed!  - Failed: 1, Passed: 270, Skipped: 0, Total: 271
  Failed ShopConfigurator.Server.Tests.PriceExportTests.Export_writes_all_rows [30 s]
  Error Message:
   System.TimeoutException: The write to the test server did not complete within 30 seconds.
   at ShopConfigurator.Server.Tests.TestServerHost.WriteAsync(...)
```

The ticket #154 changed how prices are read from the package (it touched JSON deserialisation and the trimming
settings of the published app). Ticket #154 has the unticked criterion
"- [ ] deferred proof: the full -AsShipped suite passes (completion run)".
#152 and #153 each have an unticked box "- [ ] deferred proof: full -AsShipped suite passes (completion run)".

Stopping point: you have done everything the skill says up to and including getting a fix merged, OR
up to reporting that no fix is needed. Branch on the results you would observe.
