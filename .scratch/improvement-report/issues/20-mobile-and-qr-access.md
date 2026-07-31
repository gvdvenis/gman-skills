# 20 — Mobile/small-screen interactions and QR access

**What to build:** The report is fully interactive at screen widths ≥375px. On mobile, the prompt workspace renders in a collapsible read-only section; editing opens a full-screen overlay. When the server is configured to bind to all-interfaces (`0.0.0.0`), a stable QR code for the fixed-port URL is displayed in the terminal at launch so a phone user can navigate directly without retyping. JS polling drives auto-reload so the mobile view stays current when the desktop user makes changes. When the report is opened via `file://`, a degraded-state banner guides the user to use the server path instead.

**Blocked by:** 17 — Report HTML shell, 18 — `POST /api/dismissals` and atomic decisions write-back.

**Status:** done

- [x] Report is fully interactive (add/remove queue, dismiss, copy) at ≥375px viewport width
- [x] Prompt workspace is collapsible on mobile with a full-screen edit overlay
- [x] QR code for the fixed-port URL prints to the terminal when all-interfaces binding is active
- [x] JS polling keeps the mobile view current without a manual refresh
- [x] `file://` usage triggers a visible degraded-state banner with server-path guidance
