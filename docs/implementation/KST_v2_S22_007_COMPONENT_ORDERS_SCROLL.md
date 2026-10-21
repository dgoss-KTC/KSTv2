# S22-007 — Component Orders local scrolling correction

Date: 2026-10-21. Entry HEAD: `243e307a7977b4474d7025b0c5acc899d8e0de4c`.

**COMPLETE / OWNER-ACCEPTED / BOUNDED SCROLLING CORRECTION CLOSED — 2026-10-21.**
Owner: “I checked it. The scrollbar works. Accepted.” Existing S22-007 owns this concrete layout defect;
S22-018/030 cross-reference the bounded scroll-region/focus conventions only. No duplicate task,
broader Component Orders redesign, performance work, Stage 22 closure or baseline freeze.
This confirmation removes the scrolling hold; the separate Stage 22 milestone decision remains open.

## Finding and bounded implementation

Windows source already used `overflow: auto`. The reproduced defect was not missing overflow:
the non-shrinking panel plus `max-height: calc(100vh - 240px)` sized the table without accounting
for actual shell/workspace chrome, toolbar height or the enrichment notice. The horizontal scrollbar
was below the visible workspace. At 900×600 compact in the synthetic browser baseline, its bottom
was 606.75px without the notice and 650.75px with it; comfortable was another 6px lower. The same
fixture already contained horizontal table overflow. A broad ancestor-width defect was **not**
demonstrated, so no ancestor/shared layout styles changed.

`ComponentOrdersPanel.css` now follows the existing bounded Shortages-panel pattern: a shrinking
column-flex panel with zero minimum width/height and local overflow containment; the two-axis table
scroller takes remaining space. Toolbar/filter controls wrap, toolbar and notice retain their height,
and the viewport-based height estimate is removed. Existing columns, cell content, font sizes,
table layout and sticky headers remain. No pagination, virtualization or hidden columns.

The scroll region uses the existing `ScrollTable` convention (`tabIndex=0`, named `region`) and a
visible inset focus outline; native pointer/arrow scrolling is not intercepted. Comment-dialog cleanup
returns focus to its connected opener after Close/Escape. Existing tests previously did not assert
that return; focused assertions now do. No new dialog design or general keyboard framework.

Acquisition/hooks/cache, ordering/grouping/expansion, all filters including **KSS only**, supplier-state
presentation, comments and accepted simulation exclusion are otherwise untouched. No backend/API/
contracts/native source, database, authentication, dependency, permission or capability change. No
live company-source reads, installations, submissions/imports, process termination, commit or push.

## Focused verification and limits

Local evidence: `C:\Users\dgoss\AppData\Local\Temp\opencode\component-orders-scroll\`.

| Gate | Result |
|---|---|
| Affected frontend tests | **35 passed / zero failed/skipped**, three files: ComponentOrdersPanel (14), componentOrdersPresentation (15), CustomerWorkspace (6). `frontend-tests.json`; actual installed Vitest, two workers, explicit 15-second bound. |
| Real browser | **12 cases pass** using the existing headless Chrome/CDP harness pattern and installed esbuild/React: actual production panel/styles, synthetic acquisition, shell/workspace CSS with 90px synthetic chrome. 1200×800 and supported minimum 900×600, both densities, enrichment available/unavailable; four additional 650px-workspace stress cases at 900×600 actually wrap the toolbar (60/66px). `layout-check.mjs`, `baseline-geometry.json`, `final-geometry.json`, `browser-layout.log`, PNGs. |
| Geometry/interactions | Local horizontal/vertical overflow, visible bottom scrollbar, no document/shell horizontal overflow; native ArrowRight and pointer wheel, Current Comments right edge/header-cell alignment, sticky headers after vertical scroll, expansion/KSS-only and comment close/focus return. At 900×600 the final scrollbar bottom is 571px, including wrapped controls/notice; scroller remains 231px or taller in the stressed cases. |
| Types/build | `npm run typecheck` and `npm run build` **pass**. Vite mixed static/dynamic core import and >500kB chunk warnings retained. |
| Changed-file lint | Standard ESLint **fails only** at the pre-existing `ComponentOrdersPanel.tsx:41` `react-hooks/set-state-in-effect` call, unchanged by this patch. Scoped rerun disabling only that known inherited rule **passes** for both changed TSX files. This is an explicit exception, not a clean standard-lint claim. |
| Diagnostics | Final scoped AFT **FRESH**, authoritative two-file coverage, zero errors/warnings. Initial one-file snapshot had incomplete complexity despite authoritative zero diagnostics; no full-repository analysis claim. |
| Release | Established process/exclusive-lock/artifact-write guard and `cargo build --release --features tauri/custom-protocol --locked --offline` **pass**, no running KST detected. Guard unchanged except evidence destination; no bypass or sidecar publication. `build-embedded.ps1`, `cargo-embedded.log`, `ARTIFACTS.json`. |

The browser fixture is not the full application, live data or native WebView2 desktop. It establishes
real scroll geometry/interaction, unlike jsdom/CSS-text assertions, but does not establish Windows
scaling, all chrome/banner combinations or owner acceptance. No sibling styles changed; existing
CustomerWorkspace tests pass. Inherited locked hook lint, broader .NET/Rust formatting exceptions,
other suites' historical skips and prior React/Vite/AFT warnings are not relabeled passing or waived.
The initial harness-only ESM-path and JSX resolution failures were corrected before geometry capture;
neither was a passing check or a production failure.

## Executable identity / preservation

`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`

* Package **0.1.0-alpha.21**; PE FileVersion/ProductVersion **0.1.0**.
* **15,352,832 bytes**, last-write UTC **2026-10-21T23:03:14.6625201Z**.
* SHA256 **`7DF75E5A77C6FFD36398C33593B3A1D5FF2196B0B695B1F4023C973B57C78A8F`**.
* Final hashed frontend asset names embedded and host newer than assets; **522 unchanged-layer file
  fingerprints**, four existing sidecars and both companion configurations match. No republication,
  installer/deployment or agent desktop launch. Exact assets/config identities: `ARTIFACTS.json`.
* Four pre-existing documentation edits preserved, including the untouched performance record.
  Actual checkpoint-entry build-info bytes backed up/restored, not reset to HEAD: SHA256
  **`46E25644B316F22F0A655F759D13AF776DC728386ED5FB0CB3BF4A5D89A4C2B3`**.
  `src/frontend/tsconfig.tsbuildinfo` remains unstaged/excluded; no staging is authorized.
* Focused diff/evidence includes this new record, the three Component Orders files and bounded
  status/checklist/scope-registry additions. Earlier in-progress dispositions remain separate.

## Owner acceptance and bounded closeout — 2026-10-21

The owner confirms: “I checked it. The scrollbar works. Accepted.” Record only that reported check
and acceptance, not completion of every suggested size/density/filter/focus scenario below or proof
of the exact executable tested. No additional agent desktop check or live-source evidence is claimed.
Only the concrete scrolling correction closes; broader redesign/performance deferrals and all accepted
deliveries stand. Stage 22 remains open, not baseline-frozen; Stage 23 is not started.

One local commit with normal hooks is authorized: the three Component Orders source/test files, this
record and only the bounded scrolling acceptance additions in current status, master checklist and scope
registry. Pre-existing disposition/completion-review changes in overlapping files remain unstaged using
focused index contents; the performance record remains separate. Existing verification above is reused,
including the standard lint failure and separate known-rule-only pass, warnings/skips/diagnostic limits.
Closeout checks accepted source fingerprints, focused/staged diffs and whitespace, committed paths/tree/
blobs and post-commit status. Evidence/backups remain outside Git under `commit-closeout/`; actual entry
build-info stays byte-identical, unstaged/excluded. No repeated tests/builds/launches/live reads,
installations, database changes, submissions, push, version change or deployment.

## Historical suggested owner desktop checks — not an itemized completion claim

1. Open the review executable after normal KST shutdown; use an already loaded Component Orders
   workspace with enough rows to scroll vertically. No import/submission is part of this check.
2. At 1200×800 and 900×600, in compact and comfortable density, move the horizontal scrollbar fully
   right to **Current Comments**, then back. Tab to **Scrollable Component Orders** and use Left/Right.
3. Scroll vertically; confirm sticky headers/cell alignment and the bottom scrollbar remain reachable.
   Check wrapped controls when available width/text scaling causes wrapping; filters must not clip.
4. Expand/collapse a group, exercise the existing component/KSS-only/risk filters, open a comment and
   close with Escape and Close; confirm focus returns to its preview. Check unavailable-enrichment
   notice when naturally available; do not force live-source failures.
5. Confirm the bounded correction. Broader deferred Component Orders PO-detail/navigation redesign,
   Stage 27 performance proposals and remaining Stage 22 milestone dispositions remain separate.
