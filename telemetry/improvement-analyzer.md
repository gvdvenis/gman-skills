# Improvement analyzer contract

The analyzer consumes `events.jsonl` and specialist reports, then emits deterministic,
evidence-linked recommendations in `analysis.json`. It must:

- group findings by category;
- score token efficiency, quality, maintainability, clarity, and operational risk from `-2` to `+2`;
- rank by expected impact;
- include an evidence reference for every recommendation;
- emit prompt fragments without changing agents or skills.

Recommendations require explicit human approval before they are applied. The analyzer is not
enabled unless the run includes `--self-improve`.
