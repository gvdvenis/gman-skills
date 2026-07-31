# Mobile/small-screen interaction model

Type: grilling
Status: closed
Assigned: wayfinder

## Answer

**Mobile is fully supported at ≥375px** with a single responsive layout covering both narrow sidebar
(desktop browser snapped to phone width) and genuine second-device use.

> **⚠ Flag name superseded by [Local server delivery model](09-local-server-delivery-model.md)
> (ticket 09).** `--serve` referred to here was not adopted. `--self-improve` auto-launches the
> server — no separate `--serve` flag. All behaviour described below is otherwise current.

**`--serve` mode** starts a per-run HTTP server on a fixed port. The terminal prints a QR code once;
the phone scans it and bookmarks the URL — no re-scanning across runs. The URL is stable because the
port is fixed.

**Auto-reload via polling**: the page JS polls a lightweight `/ping` endpoint. When the server goes
down between runs the poll detects the gap; when the server comes back up with a new report the page
reloads automatically. SSE is not viable across per-run restarts; polling is the mechanism.

**Interaction model on mobile**:
- Queue/dequeue findings: full tap interaction.
- Prompt section: collapsible, read-only by default. A tool button opens a full-screen overlay for
  editing — deliberate action, fully usable on mobile keyboard.
- "Copy to LLM": posts to the server API; the server writes directly to the **desktop machine's
  clipboard**. The user switches to their Copilot session and pastes.

**`file://` mode**: non-intrusive dismissable banner — "Run with `--serve` for second-device access,
server clipboard, and write-back." Fully usable for inspection; less effective as an integrated tool.

**New fog surfaced**:
- `--serve` port selection and conflict handling, process lifecycle, Windows firewall prompt.
- Server-side clipboard API endpoint design (`POST /clipboard`).
- Optional prompt-enhancement pipeline server-side before clipboard write.
- Write-back to input_json for decision traceability.

## Question

What is the minimum mobile UX commitment for the self-improvement report, and what interactions
are intentionally desktop-only?

Decide:

- Whether the report targets mobile at all, or is desktop-only by design.
- If mobile is supported: what is "supported" — readable layout, or fully interactive?
- Which interactions (queue management, prompt editing, copy buttons) must work on small screens,
  and which may degrade or be absent.
- Whether a mobile-specific layout is required, or a single responsive layout suffices.
- The minimum viewport width the report is designed for.
