# Review-to-prompt interaction model

Type: prototype
Status: resolved
Blocked by: 01

## Question

How does a reviewer get from a list of findings to a chosen prompt in a few clicks?

Build a rough interactive prototype to react to, then lock the model.

Decide:

- How findings are grouped and ordered — by severity, category, or expected impact.
- The queueing gesture, and whether a queued finding can be edited before it enters the prompt.
- Whether the prompt draft is directly editable, and what happens to edits when the queue changes.
- How severity is shown without turning the page into noise.
- What the empty state looks like when a run produced no findings.

## Answer

**Chosen model: A — Card list + right sidebar (responsive to bottom drawer on small screens).**

The winner preserves the "few clicks" goal while keeping review and prompt assembly visible at the
same time.

### Final interaction model

1. **Grouping and order**
   Findings are grouped by severity with fixed order: High, Medium, Low. Within a severity group,
   findings are ordered by score descending, then stable by suggestion key.
2. **Queueing gesture**
   Each finding card has a one-click `+ Add to prompt` action; queued items can be removed from the
   queue panel.
3. **Edit-before-queue**
   No pre-queue edit step. Queueing stays fast; edits happen at the prompt stage.
4. **Prompt edit behavior**
   Prompt preview is read-only while queueing. Entering `Edit prompt` opens explicit edit mode.
   While edit mode is active, queue changes are blocked. Exiting edit mode saves an override draft.
   `Rebuild from queue` is the explicit action that regenerates and replaces the override.
5. **Severity signal**
   Use compact severity badges and section headers only; no extra color channels beyond badge + header.
6. **Empty state**
   If there are no findings, show "No improvements suggested for this run." with no queue or copy
   controls. If findings exist but queue is empty, show "Add findings to build your prompt."

### Variant disposition

- **A selected** as canonical interaction model.
- **B rejected** for added interaction cost (detail-first step slows bulk triage).
- **C rejected** for delayed feedback (prompt appears late in a modal instead of as a live workspace).

### Prototype location

`.scratch/improvement-report/prototype/review-to-prompt.html` (throwaway; keep as design evidence)

## Comments

Reopened to replace the prior "awaiting human reaction" state with a single locked interaction
model. Closed in the same session with Variant A as the canonical model.
