# 21 — Docs and contract fixture normalization

**What to build:** All documentation, test fixtures, and inline comments that reference superseded names or flags are updated to reflect the canonical final contract from the spec. Implementation will have used the canonical names throughout, so this ticket is a verification and cleanup pass — not a rename refactor. After this ticket, no reference to `report-input.json`, `POST /clipboard`, or `--serve` remains in any normative document or test fixture.

**Blocked by:** 19 — `POST /api/ship-prompt` middleware chain, 20 — Mobile/small-screen interactions and QR access.

**Status:** done

- [x] No reference to `report-input.json` remains in docs or test fixtures (canonical: `improvement-report-data.json`)
- [x] No reference to `POST /clipboard` remains (canonical: `POST /api/ship-prompt`)
- [x] No reference to `--serve` flag remains (canonical: `--self-improve` auto-launch)
- [x] Contract fixtures in tests match schema version `1.1` and the normalized endpoint surface
- [x] Spec cross-references in map and decision files point to the correct final names
