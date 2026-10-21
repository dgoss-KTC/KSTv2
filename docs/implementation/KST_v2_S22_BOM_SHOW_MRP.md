# Stage 22 — BOM Show MRP / bounded investigation return

2026-10-21. **COMPLETE / OWNER-ACCEPTED / BOUNDED NAVIGATION FOLLOW-UP CLOSED.**
Owner: **“It looks good and it works. Accepted.”** Owner-reported acceptance is separate from the
offline tests and artifact evidence below; it does not establish every suggested desktop scenario,
the exact executable hash tested, agent desktop/live-source verification or material-risk acceptance.
Entry HEAD `657a727d79a42a60ed83aa936333ebf0b67971bd` matches the supplied accepted local commit.
Only entry change: `src/frontend/tsconfig.tsbuildinfo`; actual entry SHA256
`46E25644B316F22F0A655F759D13AF776DC728386ED5FB0CB3BF4A5D89A4C2B3`.
Saved outside Git before editing; restore these bytes, not HEAD or an earlier checkpoint backup.
Implementation-time no-staging/commit statements below are historical. The owner now authorizes
acceptance closeout and one complete local commit with normal hooks, not a push. No installation,
live database access, submission/import or deployment is authorized.

## Authority, trace and bounded implementation

Owner dispatch authorizes implementation without another discovery/dispatch gate; AGENTS.md,
enacted security policies, current status/checklist, accepted `KST_v2_S22_PART_INFO_MRP.md`, and the
existing shell/Customer/MPS/Reports implementations govern. Inspection confirmed Windows PowerShell
5.1, the disabled BOM action, shell-owned navigation/dirty confirmation, customer unmount on Reports,
Reports-owned selector, lookup-owned specification/inputs/submission and independent MRP acquisition.
Source/offline tests are not agent desktop or live-source verification.

- `ComponentInfoModal` enables BOM-only Show MRP when supplied a selected component/site and the
  shell callback. It remains usable while optional component detail loads: identity comes from the
  applied BOM selection and loaded MPS snapshot site, never a saved Reports preference or detail reply.
- `MpsWorkspace` supplies an origin with assignment/snapshot/site/basis identity, connectivity check,
  close-on-success callback and connected focus/scroll restoration. The shell coalesces pending
  activation, waits for its existing in-app discard decision, then revalidates origin/workspace before
  closing the source and replacing Reports context. Cancel leaves source, drafts and focus unchanged;
  the source modal yields Escape to the stacked shell alertdialog.
- Only this originating customer/MPS tree is retained while Reports is active, hidden and inert.
  Its loaded snapshot, parent/bucket/material/candidate trail, detail tab, mounted filters and grid/
  detail scrollers remain. `active=false` suppresses global MPS Escape and existing active-only export
  surfaces. No all-workspace cache, router, additional window or persisted investigation is introduced.
- Planning proposals move to a focused `PlanningSession` child, still alive across customer module
  switches/snapshot changes but unmounted on the approved Reports detour. Non-MPS module panels also
  unmount. This makes approved discard a real session discard, not a guard clear over hidden drafts;
  saved draft files are neither deleted nor automatically restored. Existing workspace-switch module
  behavior is preserved, not reset by a new assignment key.
- Reports selects Part Info & MRP using a monotonically identified shell request. Its one-use claim
  survives rerenders/lookup remounts/selector visits; departure clears the request. A new visit to the
  same part gets a new token. Lookup waits for successful supported-site specification (including
  Retry settings), applies explicit incoming identity and calls the **normal** submit function once.
  A queued, cancellable application avoids StrictMode effect replay. Normal validation, identity
  checks, cancellation, failure/Retry and subsequent `PartInfoMrp` sequence remain authoritative.
  Each shortcut is a new lookup: **26 weeks / Week / Include Unconfirmed POs on**. No export/save/open
  activation is issued. Delayed/stale data cannot replace the requested identity.
- Contextual **Back to MPS** and normal originating-workspace selection use the same restoration.
  Assignment configuration, mounted identity and snapshot/basis must still match. Replaced or removed/
  archived/deleted origin falls back to current workspace/empty state with explicit feedback; old
  investigation state is not restored. Selecting General/another workspace releases the bounded origin.
- `AnalyticalExcelExport` opts **Component MRP only** into retaining already prepared bytes on context
  departure. Recovery borrows the existing application-lifetime **single preparation slot**, independent
  of the new lookup, not another byte cache. Only explicit Save again can retry the original adapter/
  preparation/kind. Explicit discard, successful save or a new explicit export releases/supersedes it.
  Pending/uncertain operations remain blocked by existing global save state; reconciliation is
  observation-only and terminal replies beat late failures. Host expiry, save/open/close authority,
  original-kind Open again recovery and other reports' fiscal/input invalidation remain unchanged.
  An unfinished preparation is still cancelled on departure, never converted into a late automatic save.

Part Info/MRP is an **independent current acquisition**, not an extension of the originating MPS
snapshot. The contextual strip states this; existing acquisition/as-of labels remain. No occurrence
quantity, snapshot facts or analytical formula enters the report contract.

## Owner-agreed analytical dispositions / later destinations

These resolve the existing S22-033 review items; they do not assert new missing calculations.

| Item | Current disposition | Explicit later destination / trigger |
|---|---|---|
| Extended Requirement | Retain existing WO remaining requirements and MRP gross requirements in their existing contexts. No new BOM extended/build-quantity number. | Separate **BOM build-quantity capability proposal**, outside this Stage 22 navigation task. Trigger: owner requests a concrete build-quantity workflow and approves quantity/date/site/occurrence semantics and source fixtures before calculation work. It is not a release/accepted-page gate. |
| Incoming Supply | Use existing qualifying open POs and MRP scheduled receipts; do not sum them into a new combined field. | A separate supply-aggregation proposal only if the owner requests an operational decision that existing PO/MRP views cannot answer; require explicit overlap/time/status rules and independent fixtures first. Otherwise retained/no implementation backlog obligation. |
| Coverage / Material Status | Retain contextual WO material status, shortage severity and MRP balances. No universal coverage percentage. | Separate domain/rule amendment only for a specified decision with an owner-approved denominator, allocation/horizon/usability rules and fixtures. Any Stage 9–11 change requires explicit unlocking/authorization; this is not permission to change those algorithms. |
| S22-037 | Accepted Part Info page remains accepted; this new navigation/return follow-up is now owner-accepted and closed. No generic additional acceptance gate. | No concrete unresolved Part Info concern identified at this closeout. A subsequently observed, reproducible performance/export fault belongs in a separately scoped follow-up with measured evidence; that conditional trigger is not an outstanding acceptance task. |

Shortage-popup deep links, global navigation redesign, new analytics, backend optimization, deployment
and Stage 22 closure are out of scope. Existing accepted popup, Part Info/MRP and simulation-filter
deliveries remain accepted; the new navigation is now accepted as recorded above.

## Focused verification / retained evidence

External evidence root: `C:\Users\dgoss\AppData\Local\Temp\opencode\bom-show-mrp\`.
New tests use independently specified identities/state transitions, not an echo of implementation
tokens: delayed/failed settings, preference mismatch, StrictMode/rerender, duplicate clicks, repeated
same-part visits, stale Reports kind/inputs, all defaults after previous controls, cancellation and
real proposal discard, parent/bucket/candidate/tab/filter/scroll/focus retention, Reports Escape,
late replies/departure and invalidated snapshot/workspace/removed origin. Reports integration tests
retain exact-byte and uncertain-save/original-kind recovery without new export on arrival.

Final offline frontend run: **415 passed / zero failed / three inherited skips in 21 files**,
bounded to two workers and an explicit 15-second test deadline (`frontend-final.json`). The three
unchanged Stage 7D.7/7D.8 material/card skips in `MpsWorkspace.test.tsx` remain skipped, not passing
evidence. Covers affected shell/workspace/MPS/navigation/BOM/modal/lookup/MRP, Reports workbook/
Historical regressions and shared export/recovery. New shortcut/recovery cases are included, not
added again to this total. Changed-file ESLint (15 TS/TSX files, zero-warning gate), TypeScript and
frontend build pass; Vite mixed static/dynamic core import and >500 kB chunk warnings remain.
Initial AFT scope had authoritative zero errors/warnings in three production files but incomplete
complexity; later inspections reached filesystem deadlines, so no complete final clean AFT snapshot
is claimed. Actual compilers/tests are the gates.

The first guarded release attempt stopped **before Cargo** for running installed KST processes
(`kst-tauri` PID 59576 and `Kst.Api` PID 20436 at subsequent inspection). Earlier process inspection
had no rows; no launch timing is inferred. Owner normal shutdown is requested; no process termination,
guard bypass or old-host-as-current claim was made at that stop.
`build-embedded.ps1` retains the unchanged process/exclusive-lock/destination access guard, accepted
simulation-corrected four-sidecar/config fingerprints and offline embedded Cargo command. No sidecar
publication is included. Owner then confirmed **“Closed — retry build”**; the same guard and offline
embedded build passed. Cargo exit zero, no process termination, guard bypass, app launch or deployment.
All **522 tracked backend/native/contract files**, four sidecars and both companion configs match
pre-build fingerprints after the build; no Cargo manifest/generated-schema drift or sidecar republication.
`PREBUILD_ARTIFACTS.json` identifies the **existing, not newly rebuilt** repository host at this stop:
`src/tauri/target/release/kst-tauri.exe`, 15,351,296 bytes, package alpha.21 / PE 0.1.0, SHA256
`5046649535DAA8D579D9E0577A5641E7CF05D6C89FE7CB88EE4BE1F36F67F53C`.
It is not presented as the new navigation review executable, or assumed to be the exact earlier
accepted host from its equal size/version. No new executable identity is claimed before the build.

### Initial embedded review executable — superseded by styling correction below

**`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**

- Package **0.1.0-alpha.21**; PE FileVersion/ProductVersion **0.1.0**.
- **15,352,320 bytes**, last-write UTC **2026-10-21T20:30:20.2232857Z**.
- SHA256 **`C5220A29CBBD8E78D5056A648DCF82A7B91203831B9B3D79C3B687C36A76845B`**.
- `ARTIFACTS.json.pre-button-style`, `unchanged-layers-entry.json.pre-button-style` and `cargo-embedded.log.pre-button-style`
  retain identity/guard/command/fingerprint evidence. Final asset names `index-C-9Qe0qp.js`,
  `event-9oYus05Y.js`, `index-KyFdgNjA.css` are embedded; host is newer than all assets.
- All four simulation-corrected sidecars remain **127,568,038 bytes**, SHA256
  `D9EA68E203A1A59F5DEF18867286EEF3971A631A5423A1361147B7950014BD95`; configs unchanged.
- Actual entry build-info remains **9,707 bytes**, entry SHA256 above, unstaged/excluded.
  Debug/installed hosts were not refreshed or represented as this release. No agent desktop, live
  source, native chooser/Excel interaction or installed-deployment claim.

Reuse accepted unchanged backend/native/contract/algorithm evidence rather than rerunning those
suites or republishing the sidecar. New shared recovery work is frontend orchestration only: same fixed native commands,
tokens, output authority and host lifetimes; no trust/privilege boundary changes. Routine implementing-
agent checks plus compilers/tests apply; no concrete material reason for independent security review
has been established at this scoped offline depth. This is not independent review or risk acceptance.

Known limits remain explicit: inherited skips, broad locked Stage 10 lint/backend format exceptions,
React fixture/Vite warnings and AFT filesystem deadlines/partial analysis are not clean passes.
The initial changed-file lint caught synchronous effect application (corrected to cancellable queued
application), initial test fixtures/types were corrected, and the first broader suite found two S&OP
retry-invalidations affected by overbroad retention (corrected by Component-MRP-only opt-in) plus an
inherited Open Order Report test selecting the retired “Component MRP” selector (updated to accepted
“Part Info & MRP”, without changing its recovery assertions). Failed executions remain retained.

`focused.diff` contains the **complete 23-path repository diff, including all four new files**,
excluding entry build-info; `focused-manifest.json` records path/new-file flags, bytes and SHA256.
The patch reverse-checks against the working tree without applying changes; tracked/new-file
whitespace checks pass. Index remains empty. `capture-diff.ps1` makes the evidence reproducible.
Existing external backlog rows **S22-033 and S22-037 only** are amended; full pre-edit backup
`backlog.entry.md` and separate `backlog-update.diff` preserve that evidence outside Git.

## Owner review correction — Show MRP enabled styling

Owner reports Show MRP looks greyed out and shows a stop cursor but does work when clicked. This is
not acceptance of all navigation behavior. Confirmed source cause: `.component-info-modal__mrp-btn`
still unconditionally set `var(--tx3)`, opacity `0.6` and `cursor: not-allowed` from the original disabled
placeholder. `ComponentInfoModal.css` now uses normal `--tx2`, opacity `1` and pointer cursor, with
enabled-only hover and keyboard focus feedback. Faded/stop-cursor styling is confined to `:disabled`.
No navigation, lookup, guard, contract or other popup behavior changed.

`ComponentInfoModal.test.tsx` adds enabled callback, incomplete-identity disabled cases and a direct
CSS selector-wiring regression (not a claim jsdom proves native visual rendering). Amendment checks:
**79 passed / zero failed / zero skipped in two files**, `button-style-tests.json` (71 modal + eight
shell/MPS shortcut integration cases); changed-test lint, TypeScript and frontend build pass. Prior
415-pass/three-skip broader evidence above is reused, not rerun or summed with these overlapping cases.
Existing React act/Vite warnings and AFT filesystem-deadline limits remain. Actual entry build-info
bytes were restored/verified again. Earlier complete 21-path patch/manifest retained with
`.pre-button-style` suffix; cumulative diff now includes these two newly modified existing files.

The same guarded offline embedded build passes with no running KST process detected and no process
termination or bypass. **Current review executable** is still
`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`, alpha.21 / PE 0.1.0,
**15,352,320 bytes**, last-write UTC **2026-10-21T21:28:07.0894654Z**, SHA256
**`43816C58C2F2A1E474F4E71D47F45984AE16C510EB3920DD3FED005292CEC7BC`**.
Current `ARTIFACTS.json` / `cargo-embedded.log` verify final asset names `index-B9kKWdbu.js`,
`event-l9ZnVu-w.js`, `index-JEepiujf.css` embedded/newer host, all 522 unchanged-layer files and four
sidecars/configs unchanged. No backend/native source, sidecar republication, installation, live access,
commit or push during implementation. The intended desktop sequence was: enabled Show MRP should
look clickable/show a pointer, acquire the selected identity once, and return to the same investigation.
The later owner acceptance above closes navigation/style review without asserting itemized execution.

## Retained suggested desktop review — new navigation only, not a remaining gate

1. In the embedded review release, select an MPS parent/bucket, inspect a WO/candidate if desired,
   then BOM; set a filter and scroll the grid/detail. Open a BOM component.
2. Show MRP: if unsaved Planning work exists, Cancel first and confirm modal/drafts/focus remain;
   then approve discard. Confirm Part Info & MRP has the selected component and **snapshot site**,
   one lookup/result, **26 / Week / Include on**, existing acquisition/as-of labels and no automatic
   workbook/save/open. A differing remembered Reports site must not win.
3. Back to MPS: modal is closed; parent/bucket/candidate, selected tab/filter and grid/detail scroll
   are retained, with focus on the BOM row or a connected investigation target. Repeat using the
   normal workspace tab. Repeat the same part; confirm one new lookup. Reports Escape must not
   navigate the hidden investigation. Direct manual Reports entry must remain normal.

Owner acceptance of this navigation is recorded above. No agent live database/desktop/native chooser,
Excel-association, installer or deployment verification is claimed.

## Acceptance closeout / local commit — 2026-10-21

Close only the delivered BOM Show MRP and bounded investigation-return follow-up under existing
S22-033/S22-037. Preserve the analytical dispositions and explicit later destinations/triggers above;
no new calculation, implementation or unspecified Part Info reacceptance is required. Stage 22 remains
open. Shortage-popup deep links/global redesign are outside this delivery, not newly authorized work.

The dispatch's 21-path count and `C5220A29…` hash describe the pre-styling checkpoint. Current retained
evidence identifies **23 intended paths including four new files** after the styling correction, and
the superseding **`43816C58…`** executable above. All 23 entry fingerprints and the current release
match the final external manifest/artifact record. Both historical identities are retained; neither
is asserted to be the exact owner-tested binary. Reuse the 415-pass/three-inherited-skip evidence and
overlapping 79-pass styling amendment, lint/types/build/guarded release and unchanged-layer evidence;
do not add overlapping counts or rerun suites/builds/publication solely for committing.

Review the complete focused diff/all four new files, exact staged paths and contents, whitespace,
committed tree and remaining status; use normal hooks without bypass. Actual checkpoint-entry
build-info stays byte-identical (`46E25644…`), unstaged and excluded. Runtime evidence/manifests/logs/
binaries remain outside Git under the existing evidence root. External backlog reconciliation changes
only existing S22-033/S22-037; no duplicate task or broader closure. No push, live access, new dependency/
capability, database change or QXtend/Mass Updater submission/import is included. Further development
requires separate owner dispatch, not another acceptance gate for delivered functionality.
