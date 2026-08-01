# 01 — Apply best-practices pass

**Type:** task
**Status:** done

## Summary

Apply a focused ASP.NET Core Web API best-practices pass to ReportServer:
Problem Details error handling, sealed-record DTOs, `TypedResults` with explicit
`Results<...>` returns, `DateTimeOffset` for server-produced response fields,
endpoint organization and metadata, csproj AOT size reduction, `.http` file, and
test updates. See `spec.md` for full decisions.

## Checklist

- [x] Tighten `ReportServer.csproj` with AOT size-reduction properties
      (`InvariantGlobalization`, `EventSourceSupport=false`,
      `HttpActivitySupport=false`, `ServerGarbageCollection=false`,
      `ConcurrentGarbageCollection=false`).
- [x] Refactor DTOs to sealed records (`DismissalRequest`,
      `ShipPromptRequest`); remove `ApiErrorResponse`; remove `Ok` from
      success responses; `DismissalResponse.DecidedAt` → `DateTimeOffset`;
      add XML doc comments.
- [x] Refactor `ReportStore.DismissAsync` to throw `KeyNotFoundException` for
      missing finding ID; simplify `DismissalResult` to `(string DecidedAt)`.
- [x] Create `Middleware/ApiExceptionHandler.cs` implementing
      `IExceptionHandler` (`KeyNotFoundException`→404,
      `ArgumentException`→400, rest→default 500). Register
      `AddProblemDetails` + `AddExceptionHandler` + `UseExceptionHandler`.
- [x] Wire `Program.cs`: `AddValidation()`,
      `ConfigureHttpJsonOptions` with `ReportJsonContext.Default`
      `TypeInfoResolver`, `AddProblemDetails`. Remove inline try/catch and
      manual validation. Call `MapReportEndpoints()` and
      `MapSystemEndpoints()`.
- [x] Create `ReportEndpoints` static class (`MapReportEndpoints`) for
      `/api/report`, `/api/dismissals`, `/api/ship-prompt` with `TypedResults`
      + explicit `Results<...>` + OpenAPI metadata. `/api/report` keeps
      `Results.Content` pass-through.
- [x] Create `SystemEndpoints` static class (`MapSystemEndpoints`) for
      `/ping` and `/shutdown`.
- [x] Update `ReportJsonContext`: remove `ApiErrorResponse`, update
      `DismissalResponse`/`ShipPromptResponse` shapes.
- [x] Update `wwwroot/index.html` JS error handling to read
      `problem`/`title` from Problem Details on non-2xx.
- [x] Create `ReportServer.http` with all endpoints + error-path examples.
- [x] Update `ReportServer.Tests/ReportServerTests.cs`: remove
      `IsDuplicate` assertions, assert `KeyNotFoundException` for missing ID,
      assert idempotent re-dismissal returns same `DecidedAt`.
- [x] `dotnet build` — zero warnings, zero errors.
- [x] `dotnet test` — all tests green.

## Blocked by

Nothing.