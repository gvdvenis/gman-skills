# Report input contract

Type: grilling
Status: resolved

## Question

What data does the report consume, and in what shape?

Decide:

- Whether the report reads run artifacts directly, or whether the analyzer emits a dedicated
  report-input file that the report is the sole consumer of.
- The finding record: id, title, summary, category, scores, expected impact, prompt fragment, evidence.
- How evidence points back to a specific event or specialist report without pasting repository content.
- Whether data is embedded in the HTML file or loaded alongside it, given local file-origin restrictions.

## Answer

The report reads from a dedicated **`report-input.json`** file, written by the orchestrator into the
run directory at `~/.copilot/blazor-orchestration/runs/<run_id>/`. It is the report's sole consumer
and its durable source of truth. The HTML report is volatile presentation — it fetches this file via
the local C# server API (see delivery decision below); no inline data embedding.

### Top-level structure

```json
{
  "schema_version": "1.0",
  "run_id": "2026-07-31T16:06:00Z-abc123",
  "generated_at": "<ISO timestamp>",
  "findings": [ ... ]
}
```

`run_id` at the root means individual findings do not need to carry it — all findings in the file
belong to that run.

### Finding record

```json
{
  "id": "<run-scoped unique slug>",
  "specialist": "blazor-component-architect",
  "title": "Short label for the weakness",
  "summary": "1–2 sentence description of the problem.",
  "category": "validation",
  "severity": "high",
  "expected_impact": "One sentence: what fixing this would change.",
  "prompt_fragment": "In blazor-component-architect sessions: before finalising output, always run `dotnet build` to confirm the component compiles.",
  "evidence": {
    "specialist": "blazor-component-architect",
    "issue_index": 2
  }
}
```

**`category`** is a fixed enum — one value per finding. Multi-category `self_diagnosis` entries are
split by the orchestrator into separate atomic findings. The five values:

| Value | Covers |
|---|---|
| `tool_use` | Wrong tool chosen, tool skipped, tool misused |
| `planning` | Scope misjudged, steps out of order, task decomposition problems |
| `output_quality` | Code correctness, completeness, adherence to conventions |
| `validation` | Tests not run, build not checked, evidence not collected |
| `communication` | Summary unclear, next_action vague, handoff instructions missing |

**`severity`** (`low | medium | high`) is derived by the orchestrator from per-dimension scores
(`tokens`, `quality`, `clarity`, `maintainability`) supplied by the specialist in
`self_diagnosis.scores`. This requires a schema amendment to the specialist report-back (see
follow-on below).

**`prompt_fragment`** is orchestrator-written. Specialists draft domain-specific suggestions in
`self_diagnosis.improvements`; the orchestrator refines these, resolves overlapping concerns across
specialists, and writes the final ready-to-use instruction string for each finding.

**`evidence`** is a pointer into the specialist's `reports/<agent-id>.json` in the same run
directory — no content is copied. `issue_index` is the zero-based index into
`self_diagnosis.issues[]`.

### Data delivery

The HTML report fetches `report-input.json` via `GET /api/report` on the local C# server running at
`127.0.0.1`. Self-contained inline embedding is dropped; the local server is the primary delivery
path. This supersedes the single-file constraint from the local-first delivery constraints decision.

### Follow-on items flagged

1. **Specialist schema amendment** (core map): add optional `self_diagnosis.scores` object
   (`tokens`, `quality`, `clarity`, `maintainability` — each `low|medium|high`) to the specialist
   report-back schema locked in [Specialist report-back schema](../../blazor-orchestration-core/issues/04-specialist-report-back-schema.md).
2. **Local server delivery model** (this map): the local C# server is now the primary
   delivery mechanism; the self-contained HTML constraint from ticket 04 is superseded. See
   [Local server delivery model](09-local-server-delivery-model.md).

