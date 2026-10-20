# S22-018/030 — Workspace action-menu keyboard ownership and focus

2026-10-20 — **BOUNDED CORRECTION COMPLETE / OWNER-ACCEPTED / CLOSED.**
Owner: “The behavior has been corrected. Accepted.” Acceptance closeout and one complete local
checkpoint commit explicitly authorized, **not a push**. Entry HEAD
`0010d353b6f5aca4342e9e5ae971ebe4f86b4c12`. Broader S22-018/030 and Stage 22 remain open.

## Scope and delivered behavior

Authority: `AGENTS.md`, current status/master checklist, accepted MPS navigation/export implementation,
the owner's bounded implementation dispatch and external
`C:\dev\kst_v2_tmp\Stage22\STAGE_22_TEAMMATE_SCAN_2026-10-20.md` (§1). Existing keyboard conventions:
`docs/implementation/KST_v2_UI_NAVIGATION_KEYBOARD_ERGONOMICS_PLAN.md`.

- `WorkspaceTabBar.tsx`: an open menu advertises `data-workspace-action-menu`, an accessible name and
  trigger/menu relationship. Entry focuses the first enabled item (ArrowUp: last). Arrows wrap through
  enabled items; Home/End reach the endpoints. Native button Enter/Space activation is retained, not
  duplicated with another action handler. Disabled Move actions cannot dispatch.
- Menu Escape is captured, consumed and closes only the menu; focus returns to its stable owning
  kebab with `preventScroll`. Tab closes without preventing default and anchors normal forward/backward
  progression at that trigger. Pointer dismissal is bounded to the actual menu/owning trigger, not the
  entire tab bar, and never reclaims focus from the clicked destination.
- Move Left/Right restores the moved workspace's connected, keyed trigger before delegating the
  unchanged reorder callback. Edit/Archive/Delete still pass that same stable trigger to existing
  application-owned dialogs/confirmations; menu cleanup does not reclaim dialog focus.
- `MpsWorkspace.tsx`: Escape yields to the open workspace menu and already-consumed events. The DOM
  marker protects MPS-first listener order; `defaultPrevented` protects menu-first order even if menu
  removal commits before the later listener. No navigation/pending-entry state changes on dismissal;
  the next Escape follows the original hierarchy or pending-entry cancellation. Normal asynchronous
  completion remains allowed.
- The exclusion also recognizes modal `alertdialog`, the actual role used by workspace confirmations.
  A focused offline check exposed background MPS navigation while a confirmation is busy and leaves
  Escape unconsumed. Including that role preserves confirmation ownership without changing its
  handlers, busy gate, dirty-work decision, navigation rules or focus restoration.

The shared analytical export menu is an interaction reference only; its code, authority, preparation,
native save and recovery remain unchanged. No global overlay framework, other tablist, pagination,
S22-029 geometry, business/source/locked Stage 9–11, backend/native/API/generated contract, dependency,
capability, database object, webview permission, version or sidecar change. Accepted deliveries,
workbook-helper retirement, existing pagination-removal follow-ups and Workspace Shipments scope/export
disposition remain untouched.

## Offline regression and verification

Full local evidence (outside Git):
`C:\Users\dgoss\AppData\Local\Temp\opencode\s22-workspace-menu\`.

| Evidence | Result |
|---|---|
| `regression-before.json` | Before production changes, **both menu/selected-parent Escape cases fail**: expected `MPS grid → PARENT`, received no trail. Original capture/bubble collision reproduced offline, not on desktop. |
| `confirmation-before.json` | Before adding `alertdialog` exclusion, busy-confirmation marker regression fails with the same unintended background navigation. This is a DOM ownership surrogate, not a native/dialog-operation test. |
| `frontend-final.json`, `frontend-final.log` | **187 passed / zero failed / three inherited skips, seven files**. Tests use mocked APIs/host and jsdom; no live sources or native save. |
| `lint.log` | Installed ESLint on the four changed/new frontend files, `--max-warnings 0`: **PASS**. |
| `typecheck.log` | Repository `npm run typecheck`: **PASS**. |
| `frontend-build.log` | Repository `npm run build` (`tsc -b && vite build`): **PASS**. Existing mixed static/dynamic Tauri core import and >500 kB chunk warnings retained. |
| `desktop-release-build.log`, `ARTIFACTS.json` | Offline embedded release **PASS** after owner closure; final asset names embedded/release newer than assets, unchanged four sidecars and preserved entry build-info verified. No native/backend source changes or sidecar republish. |
| Scoped AFT | Initial three-file diagnostic coverage: zero errors/warnings, but overall **PARTIAL** (one complexity parse gap). Subsequent/final four-file requests failed `read_deadline_exceeded`; no fresh final AFT pass claimed. |

Final test command: `npm test --` followed by these paths, `--maxWorkers=2`, default/JSON reporters
and the external JSON output path (Windows invocation uses `npm.cmd` through `cmd.exe`):
`src/components/WorkspaceTabBar.test.tsx`, `MpsWorkspace.navigation.test.tsx`, `MpsWorkspace.test.tsx`,
`WorkspaceLifecycle.test.tsx`, `AnalyticalExcelExport.test.tsx`, `CustomerWorkspace.test.tsx`, and
`PlanningUnsavedChanges.test.tsx` (all seven in `src/components/`).

New tests independently state expected trails, focus destinations, order, invocation counts and pending
outcomes. They check both actual document-capture registration orders; parent, bucket, root and
candidate material; selected/root pending completion and subsequent cancellation/late reply;
inactive Dashboard/root; retained filters, expanded WO and synthetic horizontal/vertical scroll.
Menu tests cover disabled actions, entry, arrows/Home/End, Enter/Space once, Escape, Tab/ShiftTab,
reorder, pointer destinations, dialog-return triggers and modal/export ownership marker surrogates.
Existing real React dialog/Material Detail/component modal and shared analytical export suites run
alongside them; marker fixtures do not establish combined native overlay behavior.

Tab harness limitation: user-event's default uses the original keydown target even after focus moves;
once the menu item unmounts, it loses focus/throws. Tests explicitly assert **uncancelled** Tab dismissal
and live trigger focus, then simulate normal Tab progression from that connected trigger separately.
Actual WebView progression is not established by this automated test or separately itemized by the
owner's bounded acceptance; that limitation does not reopen the accepted correction.
An initial two-suite run had those two harness failures (53 other passes); expectations were not
relaxed to accept lost focus. Final seven-suite run passes. Existing Planning unsaved/native-close
fixtures emit React act/unawaited-act warnings; these are not suppressed. Three inherited Stage 7
material/kitting skips remain unexecuted, not passes. Initial PowerShell `npm.ps1` argument forwarding
failed before Vitest; corrected Windows npm invocation produced the recorded pre-fix failures.

Known broad lint/format exceptions remain open and were not rerun or reclassified. No backend/native
suite, independent security review, install, live query, app launch, submission/import or deployment
was performed. Bounded self-screen against `SECURITY.md`, Security Assurance Policy §§5–6 and AI
Security Review triggers finds no changed trust/security boundary; additional independent review is
not justified solely by these event/focus changes. This is not independent assurance or risk acceptance.

## Preservation and review release

Only pre-entry modification: `src/frontend/tsconfig.tsbuildinfo`. Actual bytes copied outside Git to
`entry.tsconfig.tsbuildinfo`, then restored after builds, **not replaced with HEAD**. Entry SHA256:
`2B79FB70977593B1C5D3699EF3C198E6CF92AA27B01EF062B085FE1307497195`.
It stays unstaged/excluded. Published/binaries/release/debug sidecars are unchanged at 127,543,462
bytes each, SHA256 `16CE3AC6FB9A05CA8CAF7D8E3696431251194983EBAB322D3D02E2BD5CC9EA6B`;
no sidecar publication is needed or performed. `sidecars-entry.json` retains the entry identities.

KST reopened from the installed location after an earlier closed-process check. The exclusive
pre-build guard stopped before Cargo; the owner then confirmed normal closure. A fresh process check
found no KST/Cargo/rustc process, and `cargo build --release --features tauri/custom-protocol --locked
--offline` passed in 1m 24s. No process was killed; no concurrent release build or older executable
was used for verification. Native tracked files/generated schemas did not drift.

**Current review executable:** `C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`, alpha.21,
**15,307,264 bytes**, last-write UTC `2026-10-20T20:50:27.4299667Z`, SHA256:
`EBBB26D4FA531C8A4A1EF214943176BDA6DBC583D48CAA486C16AA9852B1FA0C`.
External `verify-artifacts.ps1` / `ARTIFACTS.json` verify final embedded asset names
`index-PHMZuC-F.js`, `event-BCTmptBe.js`, `index-SuUpcr74.css`, release/asset timestamps, four unchanged
sidecars, source/test fingerprints and exact entry build-info preservation. Agent did not launch it.
Complete repository diff including both new files (excluding entry build-info): `focused.diff`;
external backlog changes: only the existing S22-018/030 rows in the Windows inventory.

## Historical owner-review guidance

The implementation handoff requested use of the identified `src/tauri/target/release/kst-tauri.exe`
above, not the installed or older/debug executable. This checklist is retained as guidance, not proof
that each suggested check was performed. No agent desktop reproduction is claimed.
Use an already available/owner-authorized MPS snapshot; this checkpoint authorizes no live acquisition.

1. Select an MPS parent, bucket or material investigation; note its trail/filter/scroll. Open workspace
   actions, press Escape: menu only closes, focus returns to that kebab, investigation stays unchanged.
   Press Escape again: normal one-level MPS Back. Repeat once during bucket loading if practical;
   the first Escape must not cancel it (ordinary completion is allowed).
2. Focus the kebab: Enter/Space/ArrowDown enters at Edit, ArrowUp at Delete. Check arrows/Home/End,
   skipping disabled moves. Tab/ShiftTab closes and progresses normally; outside click keeps its focus.
3. Move a middle workspace left/right: exactly one reorder and focus remains on its moved kebab.
   Open Edit, Archive and Delete; cancel/Escape returns through existing dialog focus/confirmations.
   Confirm Material Detail and analytical export Escape still close their own surface before MPS Back.

## Owner acceptance and bounded closeout — 2026-10-20

The owner confirms: “The behavior has been corrected. Accepted.” The owner explicitly authorizes
acceptance closeout and one complete local commit of this bounded correction. This is owner-reported
correction/acceptance, separate from mocked/jsdom verification; it does not establish every suggested
keyboard/Tab/pending-entry check, live-query parity, native-save/installer behavior, independent
security assurance or risk acceptance. Only this bounded correction is closed, not broader
S22-018/030 or Stage 22. Pagination-removal follow-ups and all accepted deliveries remain unchanged.

Closeout compares the complete focused diff (including both new files), four source/test fingerprints,
final assets, accepted executable, four sidecars and actual entry build-info with retained evidence:
all match. Production code and tests are unchanged. Reuse the 187 passing tests / three inherited
skips, changed-file lint, typecheck, frontend/offline release builds and artifact checks above. AFT
partial/deadline results, synthetic Tab/scroll/overlay limits, React fixture/Vite warnings and broader
format/lint exceptions remain explicit. No repeated suites/builds, sidecar publication or additional
independent review is needed solely for this closeout.

Commit scope: `WorkspaceTabBar.tsx`, new `WorkspaceTabBar.test.tsx`, `MpsWorkspace.tsx`,
`MpsWorkspace.navigation.test.tsx`, this record, current status and master checklist. Existing external
Windows backlog updates only S22-018/030. Inspect exact staged paths/full diff/new-file contents and
whitespace, honor normal hooks, then verify committed paths/tree/blobs, empty index and remaining
working tree. Closeout evidence stays outside Git under `commit-closeout/` in the evidence root.
Actual entry build-info remains byte-identical, unstaged and excluded; logs/binaries are not committed.
No push, install, live access, operational submission/import, deployment or further development is
authorized. Further work requires a separately dispatched checkpoint.
