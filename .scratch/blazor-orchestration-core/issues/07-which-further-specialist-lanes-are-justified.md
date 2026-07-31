# Which further specialist lanes are justified

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 03

## Question

Beyond extractor, author, and form specialist, which lanes justify their own isolated agent?

Candidates named in the plan: Fluent UI, data fetching, shared state. Further candidates exist in the
routing skill: scaffolding, auth, prerendering, JS interop.

Decide:

- The admission test a candidate lane must pass — real independent work, distinct tool needs, or
  meaningful context isolation.
- Which candidates pass today, based on the work actually done in this user's Blazor projects.
- Which stay as skills consulted inline rather than becoming agents.
- Whether Fluent UI is a constraint layer applied across lanes rather than a lane of its own.

## Answer

Admission into its own specialist lane now requires all three:

1. The work shows up repeatedly as an independent task in this user's Blazor sessions.
2. Keeping it in a dedicated context materially reduces drift versus bundling it with author/form
   work (different failure modes, review targets, or acceptance checks).
3. The lane can stay narrow and stable as a reusable contract, not a one-off escalation bucket.

Applying that test to current candidates:

- **Pass now: data fetching**. It recurs frequently, has a clear contract boundary (`HttpClient`,
  loading/error/empty states, service abstractions), and is often review-sensitive enough to justify
  isolation from component-authoring noise.
- **Defer for now: shared state**. It remains common but usually coupled to whatever primary lane is
  active (forms, data, or authoring); keep it as inline guidance unless repeated standalone state
  orchestration tasks appear.
- **Keep inline skill-only: scaffolding, auth, prerendering, JS interop**. These are important
  concerns but are currently better handled as consulted skills on top of a primary lane than as
  always-on dedicated agents.

Fluent UI is a **cross-lane constraint layer**, not a lane: apply `fluentui-blazor` alongside the
selected primary lane specialist whenever Fluent components/providers/theming are in scope.

Net lane set after this decision: extractor, author, form, and data-fetching as the 4th specialist;
other concerns remain skill overlays until workload evidence justifies a new lane.

## Comments

- Closed. Added data-fetching as the only newly justified dedicated lane; kept shared state,
  scaffolding, auth, prerendering, and JS interop as inline skill overlays; treated Fluent UI as a
  cross-lane constraint layer.
