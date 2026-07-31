# Findings: local-first delivery constraints

Resolved by a `/research` subagent. Verified claims carry a source; inference is marked.

## 1. `file://` origin restrictions

Modern browsers assign files loaded from disk an **opaque origin**. Consequences:

- `fetch()` and XHR against a sibling local file **fail**.
- External and dynamic ES module imports, including JSON modules, **fail**.
- File-backed Web Workers **fail**.
- Inline classic `<script>` and inline data blocks **work**.

Both Firefox and Chromium follow this model.
Source: <https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS/Errors/CORSRequestNotHttp>

## 2. Clipboard access

`file://` is generally treated as potentially trustworthy, but clipboard policy varies by browser.

- Call `navigator.clipboard.writeText()` **directly from a click handler**.
- Firefox requires transient activation; Chromium accepts activation or permission.
- Fallback chain: selected `textarea` plus `document.execCommand("copy")` (deprecated and
  non-standard, but still widely functional), then manual copy as a last resort.

Sources: <https://w3c.github.io/webappsec-secure-contexts/#is-origin-trustworthy>,
<https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API>,
<https://developer.mozilla.org/en-US/docs/Web/API/Document/execCommand>

## 3. Local persistence

`localStorage` behaviour for file-origin documents is **explicitly undefined**, commonly isolated per
file, and may throw. `sessionStorage` may likewise throw. IndexedDB on opaque origins is not
portable. **Do not depend on any browser persistence.**
Source: <https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage>

## 4. Inline data size

No standardized byte limit exists. Parsing is synchronous and memory-dependent, so a universal
threshold cannot be claimed. Benchmark against representative reports rather than assuming a number.

## 5. Single file versus loopback server

A loopback HTTP server enables normal modules, `fetch`, Workers, reliable origin-scoped storage,
correct MIME types, and security headers. It costs process lifecycle management, port binding,
Windows firewall prompts, and exposure of a local service on a potentially shared machine.

## 6. Deterministic local compression

Realistic and non-model: templating, canonical deduplication, concise directive phrasing, normalized
bullets, and optional reference IDs. Quality-preserving "semantic compression" is **not** an
established technique — retain necessary context and evaluate outputs rather than trusting a
compression ratio.

## Implications for the report design

- Ship a **fully self-contained single HTML file**: inline data, inline scripts, no network calls.
  This satisfies the privacy constraint by construction.
- Keep queue and draft state **in memory only**; treat the report as disposable per run.
- Implement copying as a **feature-detected chain** triggered by a real click.
- Deliver the compressed output through **deterministic local transformation**, not a model call
  from the browser. If model-assisted compression is ever wanted, run it CLI-side before the file is
  written.
- Offer a `127.0.0.1` server only as an escape hatch for unusually large or modular reports.

## Open/unverified

- The practical inline-payload size ceiling for this specific report — needs measurement.
- Exact current Chromium and Firefox clipboard permission behaviour for file origins may shift; the
  fallback chain is the durable answer.
