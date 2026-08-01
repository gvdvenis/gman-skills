# ReportServer best-practices pass

## Problem Statement

The ReportServer C# tool — the local HTTP server that serves the improvement report
shell, accepts dismissal decisions, and ships compressed prompts — was built
functionally but bypasses several ASP.NET Core Web API best practices. Endpoints
hand-roll JSON deserialization, validation, and error envelopes. DTOs are mutable
classes where sealed records would be clearer. `DateTime` is used where
`DateTimeOffset` is expected. The handlers return untyped `IResult` instead of
`TypedResults`, carry no OpenAPI metadata, and have no `.http` test file. The
publishable AOT binary also carries default framework conveniences (globalization,
event tracing, HTTP activity, server GC) that a strictly local, single-user,
no-auth tool does not need.

## Solution

Apply a focused best-practices pass to ReportServer that improves API semantics and
reduces published binary size, without changing the on-disk report schema or the
external API contract surface. The server keeps its current endpoints, its
file-backed `ReportStore`, and its AOT/single-file publish profile. Error
responses move to RFC 7807 Problem Details, request DTOs become sealed records
with data-annotation validation, handlers switch to `TypedResults` with explicit
`Results<...>` return types, endpoints gain OpenAPI metadata, and the csproj is
tightened with AOT size-reduction properties. A `.http` file is added as living
documentation. No OpenAPI document endpoint and no separate service interface
abstraction are introduced — both are out of scope for this strictly local tool.

## User Stories

1. As a ReportServer developer, I want error responses to follow RFC 7807 Problem
   Details, so that error semantics are standardized and handlers no longer
   hand-roll error JSON.
2. As a ReportServer developer, I want a global exception handler that maps
   domain exceptions to HTTP status codes, so that handlers stay free of
   try/catch boilerplate.
3. As a ReportServer developer, I want request DTOs to be sealed records with
   data-annotation validation, so that invalid input is rejected by the
   framework before the handler runs.
4. As a ReportServer developer, I want minimal-API parameter binding for POST
   bodies, so that deserialization and validation are handled by the framework
   instead of manual `JsonSerializer.DeserializeAsync` calls.
5. As a ReportServer developer, I want handlers to use `TypedResults` with
   explicit `Results<...>` return types, so that response types are explicit and
   compile-time checked.
6. As a ReportServer developer, I want `ReportStore` to throw
   `KeyNotFoundException` when a finding ID is missing, so that the 404 path is
   expressed through the exception handler rather than a sentinel return value.
7. As a ReportServer developer, I want server-produced API response date fields
   to use `DateTimeOffset`, so that UTC offset is preserved and JSON
   serialization is unambiguous.
8. As a ReportServer developer, I want the `Ok` boolean removed from success
   response bodies, so that the HTTP status code is the single source of truth
   for success/failure.
9. As a ReportServer developer, I want endpoints organized into static
   `ReportEndpoints` and `SystemEndpoints` classes, so that `Program.cs` stays
   focused on host setup and lifecycle.
10. As a ReportServer developer, I want OpenAPI metadata (name, summary,
    description, produces) on every endpoint, so that endpoint intent is
    self-documenting in code.
11. As a ReportServer developer, I want the source-generated `ReportJsonContext`
    wired into `HttpJsonOptions`, so that `TypedResults` serializes correctly
    under AOT.
12. As a ReportServer developer, I want a `.http` test file in the project root,
    so that I can quickly exercise every endpoint from VS Code or Rider.
13. As a ReportServer maintainer, I want the published AOT binary to be as small
    as possible, so that the self-contained local tool stays lightweight.
14. As a report shell consumer, I want error responses to use Problem Details, so
    that the JS fetch handlers can read a consistent `problem`/`title` shape on
    non-2xx responses.
15. As a report shell consumer, I want the success response bodies to drop the
    redundant `Ok` field, so that the response shape is cleaner and the HTTP
    status code is authoritative.

## Implementation Decisions

### Scope boundary

- **In scope:** DTO refactoring, Problem Details error handling, `TypedResults`
  migration, `DateTimeOffset` for server-produced response fields, endpoint
  organization and metadata, csproj AOT size reduction, `.http` file, test
  updates.
- **Out of scope:** OpenAPI document endpoint (`AddOpenApi`/`MapOpenApi`),
  separate `IReportService` interface over `ReportStore`, migrating persisted
  document model date fields to `DateTimeOffset`, tightening the CLI
  orchestrator's date-format adherence.

### Error handling

- Add `builder.Services.AddProblemDetails()` and
  `builder.Services.AddExceptionHandler<ApiExceptionHandler>()`.
- Add `app.UseExceptionHandler()` in the pipeline.
- Do **not** add `UseStatusCodePages()` — `ProblemDetails` covers all error paths
  and the extra middleware is unnecessary for an API-only server.
- `ApiExceptionHandler` (in `Middleware/`) implements `IExceptionHandler`,
  maps `KeyNotFoundException` → 404, `ArgumentException` → 400, and returns
  `false` for all other exceptions (letting the default 500 handler produce a
  generic Problem Details response). The handler logs a warning before
  returning `true`, because returning `true` suppresses the exception
  diagnostics middleware.
- The handler writes a `ProblemDetails` response with `Status`, `Title`,
  `Detail` (safe user-facing message, never `exception.Message`), and
  `Instance` (request path).
- Remove the `ApiErrorResponse` type entirely. Remove all per-handler
  `try/catch (JsonException)` blocks and manual `if (request is null ...)`
  validation checks.

### DTO refactoring

- `DismissalRequest` → `sealed record` with `init` properties:
  `[Required, JsonPropertyName("id")] string Id`, `string? DismissedReason`.
- `ShipPromptRequest` → `sealed record` with `init` properties:
  `[Required, JsonPropertyName("prompt")] string Prompt`,
  `[JsonPropertyName("queued_ids")] List<string> QueuedIds`.
- Remove `ApiErrorResponse`.
- Remove `Ok` from `ApiStatusResponse`, `DismissalResponse`,
  `ShipPromptResponse`.
- `DismissalResponse` → `sealed record DismissalResponse(string Id,
  DateTimeOffset DecidedAt)`.
- `ShipPromptResponse` → `sealed record ShipPromptResponse(string Transformed,
  List<string> Warnings)`.
- `ApiStatusResponse` → `sealed record ApiStatusResponse(string Status)`.
- Add `<summary>` XML doc comments to all response and request records.
- **Keep persisted document models** (`ReportDocument`, `ReportOrigin`,
  `ReportFinding`, `ReportDecision`, `ShippedPrompt`,
  `DismissalHistoryEntry`) as mutable classes — they are mutated in place by
  `ReportStore` and round-tripped via `ReportJsonContext`.

### Validation

- Register `builder.Services.AddValidation()` (.NET 10 minimal-API
  data-annotation validation).
- Switch both POST handlers to parameter binding: the framework deserializes
  the body and validates `[Required]`/`[MaxLength]` annotations before the
  handler runs, returning a 400 validation Problem Details on failure.
- Malformed JSON bodies are handled by the framework's built-in deserialization
  error path (also a 400 Problem Details).

### ReportStore refactoring

- `ReportStore.DismissAsync` throws `KeyNotFoundException` when the finding ID
  is not present in `report.Findings`, instead of returning a sentinel
  `DismissalResult.NotFound`.
- `DismissalResult` simplifies from `(bool IsDuplicate, string? DecidedAt)` to
  `(string DecidedAt)`. The `IsDuplicate` flag is removed.
- The idempotent duplicate-dismissal path (finding already has a `dismissed`
  decision) stays a **success** path — it updates dismissal history and returns
  the existing `DecidedAt` without throwing.
- `ReportStore.ShipPromptAsync` is unchanged in behavior.
- `ReportStore` remains registered as a singleton, directly (no interface).

### DateTime / DateTimeOffset

- `DismissalResponse.DecidedAt` changes from `string` to `DateTimeOffset`,
  parsed from the store's ISO-8601 string. If parsing fails (corrupt
  server-written value), the `FormatException` surfaces as a 500 via the
  default handler — correct, since it should never happen for server-owned
  values.
- Persisted document model date fields (`generated_at`, `decided_at`,
  `shipped_at`, `dismissed_at`) **stay `string`** — they are shared with the
  CLI orchestrator, which is an LLM-adjacent producer. Tightening the
  orchestrator's date-format adherence is out of scope and would require a
  producer-side skill change.

### Endpoint organization

- Extract domain endpoints into `ReportEndpoints` static class with
  `MapReportEndpoints(this WebApplication app)`:
  - `GET /api/report` — returns raw JSON pass-through via `Results.Content`
    (not `TypedResults.Ok`, to avoid double-serialization of the string body).
    The `Results` factory is the skill-sanctioned fallback for this special
    case.
  - `POST /api/dismissals` — parameter-bound `DismissalRequest`, returns
    `TypedResults.Ok` with explicit `Results<Ok<DismissalResponse>,
    NotFound, BadRequest>` return type. Throws `KeyNotFoundException` for
    missing finding ID (caught by the global handler → 404).
  - `POST /api/ship-prompt` — parameter-bound `ShipPromptRequest`, returns
    `TypedResults.Ok` with explicit `Results<Ok<ShipPromptResponse>,
    BadRequest>` return type.
- Extract infrastructure endpoints into `SystemEndpoints` static class with
  `MapSystemEndpoints(this WebApplication app)`:
  - `GET /ping` — `TypedResults.Ok(new ApiStatusResponse("ok"))`.
  - `/shutdown` — existing shutdown logic, moved into the static class.
- `Program.cs` calls `app.MapReportEndpoints()` and
  `app.MapSystemEndpoints()` after `app.Build()`.

### OpenAPI metadata

- All 5 endpoints get `.WithName(...)`, `.WithSummary(...)`,
  `.WithDescription(...)`.
- Domain endpoints additionally get `.Produces<T>(200)` and
  `.ProducesProblem(404)` / `.ProducesProblem(400)` as appropriate.
- Metadata is in-memory `EndpointMetadata` data — no AOT binary size impact.
  No `AddOpenApi()` or `MapOpenApi()` is added.

### JSON serialization options

- Add
  `builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolver = ReportJsonContext.Default)`
  so that `TypedResults.Ok<T>` serializes via the source-generated context
  under AOT.
- Do **not** add `JsonStringEnumConverter` — no enum properties exist in any DTO
  or response.
- Do **not** add strict-JSON options (`NumberHandling.Strict`,
  `PropertyNameCaseInsensitive`, `AllowDuplicateProperties`) — the project
  has an existing client and these could break it.
- `ReportJsonContext` source-gen options (`SnakeCaseLower`, `WriteIndented`,
  `WhenWritingNull`) stay as-is.
- Remove `ApiErrorResponse` from `ReportJsonContext`'s
  `[JsonSerializable]` list. Update `DismissalResponse` and
  `ShipPromptResponse` entries to match new shapes.

### JS client updates

- `wwwroot/index.html` fetch error handlers currently check fetch
  `Response.ok` (HTTP-level) for success — this is unaffected by removing the
  body `Ok` field.
- Update non-2xx error message extraction to read `problem`/`title` from the
  Problem Details response body instead of the removed `{ ok, error }` shape.

### csproj AOT size reduction

Add to `ReportServer.csproj` `PropertyGroup`:

- `InvariantGlobalization` = `true` — removes culture-specific resource
  DLLs and ICU globalization stack. Safe: the app uses
  `DateTime.UtcNow.ToString("O")` (culture-invariant), JSON is
  culture-invariant, HTML is English-only.
- `EventSourceSupport` = `false` — removes ETW/EventSource infrastructure.
  Safe: logging is cleared and console-only.
- `HttpActivitySupport` = `false` — removes HTTP activity/diagnostics
  hooks. Safe: no OpenTelemetry, no distributed tracing.
- `ServerGarbageCollection` = `false` — workstation GC. Safe: single-user
  local tool.
- `ConcurrentGarbageCollection` = `false` — simpler GC. Safe: no
  high-throughput concurrent workload.

Do **not** add `HttpLogging` or `DeveloperExceptionPage` middleware.

### .http file

- Create `ReportServer.http` in the project root with `@baseUrl =
  http://localhost:5173` (matches default port from `ServerOptions`).
- Include one request per endpoint: `GET /ping`, `GET /api/report`,
  `POST /api/dismissals` (realistic body), `POST /api/ship-prompt` (realistic
  body), `GET /shutdown`.
- Include error-path examples: dismiss a non-existent finding ID (expect 404),
  ship with missing prompt (expect 400).
- Port comment noting it must match `--port` / `launchSettings.json`.

## Testing Decisions

### What makes a good test here

Test external behavior at the service/store layer. The WebAPI layer
(endpoints, Problem Details, TypedResults) is thin wiring over `ReportStore`
and is exercised manually via the `.http` file. Adding a
`WebApplicationFactory`-based integration test seam would pull in the full
ASP.NET Core host for a local AOT tool — heavier than the value it provides.

### Modules tested

- `ReportStore` — dismissal idempotency, missing-finding exception,
  ship-prompt persistence. This is the only module whose behavior changes.
- `PromptCompressor` — unchanged, existing test stays.
- `ServerOptions` — unchanged, existing tests stay.

### Prior art

`ReportServer.Tests/ReportServerTests.cs` (MSTest) already tests
`ServerOptions.ParseAsync`, `PromptCompressor.Compress`, and `ReportStore`
directly. This is the right level and the same seam.

### Test changes

- `ReportStore_DismissAndShipPrompt_PersistsDecisions`:
  - Remove `IsDuplicate` assertions (flag is removed).
  - Assert that dismissing a non-existent finding ID throws
    `KeyNotFoundException`.
  - Assert that re-dismissing an already-dismissed finding returns the same
    `DecidedAt` (idempotent success).
  - `DismissalRequest`/`ShipPromptRequest` construction stays compatible —
  records with `init` properties accept the same `{ Id = ...,
  DismissedReason = ... }` object-initializer syntax.

## Out of Scope

- **OpenAPI document endpoint** (`AddOpenApi` + `MapOpenApi`) — no external
  API consumer; AOT binary size cost.
- **Separate `IReportService` interface** over `ReportStore` — the store is
  already the right seam; an interface adds indirection without a concrete
  testing need.
- **Persisted document model date migration to `DateTimeOffset`** — shared
  with the CLI orchestrator (LLM-adjacent producer); changing the on-disk
  JSON wire shape risks parse failures on existing report files.
- **CLI orchestrator date-format tightening** — producer-side skill change,
  separate effort.
- **`WebApplicationFactory` integration tests** — too heavy for a local AOT
  tool; the `.http` file covers manual endpoint verification.
- **Strict JSON serialization options** (`NumberHandling.Strict`,
  `PropertyNameCaseInsensitive`, `AllowDuplicateProperties`) — existing
  client, risk of breakage.
- **`UseStatusCodePages()`** — redundant with Problem Details.

## Further Notes

- The `QRCode.Print` call (mobile URL) is unaffected by all changes.
- The `ElBruno.QRCodeGenerator.CLI` package reference stays.
- The port-probe (`IsPortBound`) and idle-timeout background task stay in
  `Program.cs` — they are host lifecycle concerns, not endpoint logic.
- The `ShutdownAsync` helper moves into `SystemEndpoints` but its behavior
  (write JSON, complete response, cancel shutdown token) is unchanged.
- The `browserConnected` / `lastActivity` middleware stays inline in
  `Program.cs` — it is cross-cutting host behavior, not an endpoint.