# Issue tracker: Local Markdown

- One spec per directory: `.scratch/<spec-slug>/spec.md`
- Tickets: `.scratch/<spec-slug>/issues/<NN>-<slug>.md`, numbered from `01`
- State is a `Status:` line near the top of each ticket (`open` or `closed`)
- Comments append under a `## Comments` heading

## Wayfinding operations

- **Blocking**: a `## Blocked by` section listing ticket file names. A ticket is unblocked when
  every file it lists has `Status: closed`.
- **Claim**: set `Status: claimed` and save before any work.
- **Release a claim**: set `Status: open` again.
- **Close**: append the summary under `## Comments`, then set `Status: closed`.
