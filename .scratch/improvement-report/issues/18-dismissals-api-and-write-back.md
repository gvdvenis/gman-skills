# 18 — `POST /api/dismissals` and atomic decisions write-back

**What to build:** Calling `POST /api/dismissals` from the report shell records a user dismissal for one or more findings. The C# server writes the updated `decisions` map into `improvement-report-data.json` atomically using a temp-rename. Dismissal records are idempotent at the record level. Written decisions integrate with the CLI's cross-run history linkage so that dismissed `suggestion_key` values are deprioritized in future runs. The UI reflects the dismissed state immediately after the call returns.

**Blocked by:** 16 — Fixed-port C# server startup and lifecycle.

**Status:** done

- [x] `POST /api/dismissals` accepts a finding id and optional dismissed_reason
- [x] Server writes `decisions` map to `improvement-report-data.json` atomically (temp-rename)
- [x] Write is idempotent — re-dismissing an already-dismissed finding is a no-op
- [x] Written decisions link to the CLI's cross-run history store so dismissed keys are deprioritized
- [x] UI reflects dismissed state after the response returns
- [x] `decided_at` timestamp is set on the decision record
