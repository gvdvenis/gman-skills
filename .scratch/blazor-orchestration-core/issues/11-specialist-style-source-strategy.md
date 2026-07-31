# Specialist style-source strategy

Type: grilling
Status: resolved
Assignee: @gvdvenis

## Question

Should specialist behavior and output style come from one shared house-style skill, or should each
specialist read project conventions directly?

Decide with explicit implications for:

- Trigger reliability and dispatch correctness (false positives/over-triggering versus under-triggering).
- Ongoing maintenance cost and drift risk as conventions evolve.
- Consistency of specialist output and how quickly a new specialist can be added safely.

## Answer

Adopt a **single shared house-style skill** as the canonical source for project conventions, and
have each specialist reference it rather than re-reading conventions independently.

Trigger implications:

1. Keep specialist trigger text focused on *domain intent* ("data fetching issue", "form validation
   flow") instead of convention details; this reduces cross-trigger overlap and accidental routing.
2. Put convention interpretation in the shared style layer so trigger behavior does not change every
   time convention wording changes.
3. Require specialists to apply house-style checks post-dispatch, not to encode style heuristics in
   their own trigger criteria.

Maintenance implications:

1. Conventions change in one place, then immediately apply across all specialists, which minimizes
   drift and contradictory guidance.
2. New specialists onboard faster: they inherit style behavior by linking the shared skill instead
   of cloning convention logic.
3. Version the shared style skill contract; breaking style-rule changes are announced once and
   consumed uniformly, instead of requiring synchronized edits in every specialist.
