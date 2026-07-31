# Suggestion history and scoring feedback

Type: grilling
Status: closed
Assignee: @gvdvenis
Blocked by: 02

## Question

How does the system stop re-suggesting things the user already rejected?

Decide:

- Where accepted and dismissed decisions are recorded, and in what format.
- How a suggestion is identified stably across runs, so history can match it.
- Whether a dismissed suggestion is suppressed permanently, suppressed for a period, or merely deprioritized.
- Whether the analyzer's ranking learns from this history, and how that stays deterministic and explainable.
- Who owns writing history back — the report, the CLI, or the user by hand.

## Answer

Suggestion history is owned by the CLI/analyzer layer, not the browser report.

- Decisions are written to a CLI-managed history file as append-only records (accepted/dismissed,
  timestamp, rationale, and source run metadata), so local-first constraints remain intact and the
  report stays read-only.
- A suggestion is matched across runs by a deterministic `suggestion_key` derived from normalized
  intent + target surface + proposed change shape; volatile wording and scores are excluded.
- Dismissals are **deprioritizations, not permanent bans**: they apply a strong score penalty with a
  cooldown window and optional user "never suggest again" override for that key.
- Ranking learns deterministically through explicit additive weights (accept boosts similar keys;
  dismiss reduces them) with bounded impact and visible factors, so ordering remains explainable.
- History writes happen only on explicit user actions handled by the CLI pathway; no manual editing
  and no report-side persistence are required.
