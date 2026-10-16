# Stage 22 — MPS/BOM export pilot

Date: 2026-10-15

Status: **IMPLEMENTED / OFFLINE-VERIFIED — PARTIAL OWNER ACCEPTANCE; REMAINING CHECKS PENDING**

This is one owner-authorized Stage 22 checkpoint, not overall Stage 22 completion or acceptance of
the separate pending Part Info Lookup or drill-down checkpoints. Application remains
`0.1.0-alpha.21` / installer `0.1.0`. No commit, push, live QAD/database read, dependency install,
QXtend submission, new command/capability, or operational-file change is part of this pilot.

## Authority and protected baseline

Authority: `AGENTS.md`, enacted security policies, current status/checklist, the accepted Stage 21
`KST_v2_STAGE_21_EXPORT_INVENTORY_AND_EVIDENCE.md` and current addenda, accepted Stage 18 safe-save
foundation/closeout and I3/I33/I34 implementation evidence, and the explicitly dispatched pilot scope.
The owner-supplied `C:\Dev\kst_v2_tmp\Stage22\MPS Drill-Down\` boards/spec are design references,
not new business rules or admitted assets. Execution was separately authorized before implementation.
The owner subsequently explicitly requested rerunning tests and rebuilding the sidecar after closing
the running app; that does not authorize agent-run live acquisition or automatic association tests.

Starting HEAD: `1831fb7`. Pre-existing uncommitted Part Info, navigation, sticky-header, detail-scroll,
documentation and generated-contract work was preserved. The actual pilot-entry working tree, not
HEAD, is the focused-diff baseline. The first checkpoint's export exclusion remains historical;
this later, separately authorized checkpoint delivers the pilot. Locked Stage 9–11 algorithms,
source queries/populations, workbook generator, C# DTOs/OpenAPI/TypeScript contracts and operational
CSV workflows are unchanged by this pilot.

## Delivered behavior

- MPS export moves from the toolbar into the grid caption. The caption states the **loaded** basis
  and horizon and, during drill-down, the displayed subset. Export explicitly includes **all N
  parts**, independent of selected parent. Basis, Horizon, Refresh and snapshot information remain
  in the toolbar. The accepted header stack, frozen columns and bounded detail scroll are retained.
- Selected-parent BOM export stays on its own title bar. Scope states filtered/loaded row counts,
  selected parent and whether current filters apply. Empty filtered results remain legitimate
  headers/metadata exports. Filter conjunction, occurrences, gaps, repeats and original order remain.
- Shared `AnalyticalExcelExport` uses a secondary themed button, inline download/chevron glyphs,
  existing typography/theme/density/focus tokens and no icon/font dependency. The format and
  **Open Excel after saving** choice are in a keyboard-operated menu. Menu and persistent feedback
  are portalled outside the corrected sticky/overflow containers; menu Escape does not unwind MPS.
- One format activation prepares and automatically continues to existing host-owned Save As. The
  UI still separates those phases. Each effect setup acquires a fresh analytical client; cleanup
  releases that exact client. Semantic context, availability, client lifetime and the freshly built
  request are rechecked before continuation. Synchronous local/shared guards prevent rapid duplicates.
- Cancel preparation ends that visit and invalidates its generation; it is not rollback of a save.
  Save As cancellation or definite precommit failure retains the **same preparation** for Save again
  and deliberate Discard, without regeneration/source rereads. Context departure or a deliberate
  superseding preparation still invalidates unsaved work; the host retains one prepared slot.
- Preparation/failure reasons and recovery actions persist independently of four-second notifications.
  Saved/open-failed stays **Saved**. Open again uses only the existing host artifact ID, never a path.
  Opening acknowledgement does not prove Excel loaded the file. Check save status observes the
  existing operation ID; pending, unknown and intermediate Saved remain guarded against write replay.
- Application-session operation history and terminal latches survive navigation and later exports.
  Late original invoke/status replies cannot overwrite settled outcomes or successful opening retry.
  Analytical preferences share a revision-guarded, serialized session value; activation captures
  its own intent and a deliberate retry reads the latest choice.

## Owner review and success-message dismissal correction — 2026-10-15

The owner accepts the improved export control's **appearance and normal save workflow**. This is
owner-reported acceptance of those aspects only, not every native/recovery scenario, the separate
drill-down/Part Info checkpoints, or overall Stage 22 completion.

Owner-authorized frontend-only amendment: ordinary final Saved feedback dismisses after **8 seconds**,
starting only after the operation is terminal and any requested opening has settled successfully.
The existing acknowledgement meaning is unchanged; it is not proof that Excel loaded the workbook.
Known ordinary save/open acknowledgements expire; preference warnings, unknown messages and other
unresolved failures remain persistent. Cancelled/failed retained-workbook actions, Saved/Open again
and pending/uncertain Check save status remain persistent. Manual dismissal remains available where
previously appropriate. Keyboard focus inside the card defers automatic dismissal until focus leaves.

Timers are owned by each feedback revision and cleaned up on replacement/unmount; operation and
revision guards prevent obsolete dismissal. Dismissal is now an explicit **presentation flag**, not
removal of the operation record: result, preparation, client, saved-artifact identity, native grants,
terminal latch and duplicate guards are not cleared or changed by hiding feedback.

Focused execution: **53 passed, 0 failed**, five existing/new export test files, including **17 new
fake-clock tests** for the timeout boundary, opening settlement, persistent recovery/warnings, newer
messages, revision guards, keyboard focus, manual dismissal, unmount cleanup and retained outcomes.
Frontend type checking and changed-file lint pass. Scoped AFT remains partial, not a clean assurance
claim. Evidence: `feedback-dismissal-tests.json` and `feedback-dismissal.diff` under the existing
ExportPilot evidence root. No unrelated full suites, native/backend builds, sidecar publication,
dependency/capability changes, live source queries, commits or pushes were performed for this amendment.
The actual desktop timing/focus behavior of this amendment remains owner-unverified. It introduces
no native authority or save/recovery-semantic change requiring a new independent security review.

## Calendar/context correction

Allocating an identical object was **not** an invalidation defect: the previous key already used
serialized values. The pilot retains meaningful snapshot/workspace/site/basis/horizon/fiscal/stale/error
inputs and excludes selection, trail, scrolling and unrelated rendering. The request is independently
revalidated even if a date changes before the midnight/focus/visibility UI invalidation runs.

Accepted MPS weeks are Sunday–Saturday with a Monday label, **including the following Monday on
Sunday**. Fiscal labels now explicitly use authoritative loaded bucket dates. Independently specified
tests use Saturday 2026-10-17 (labels 10-12/10-19), Sunday 10-18 (10-19/10-26) and changed fiscal
anchors. A previous-week loaded view on Sunday still requires reload. ISO transport dates remain
separate from existing M/d/yyyy metadata display formatting. Current business-date, snapshot and BOM
effective-date compatibility are not bypassed to avoid legitimate refresh requirements.

## Narrow existing-host correction and retained limits

`src/tauri/src/component_mrp.rs` retains **only Analytical** saved-artifact grants across idle
departure or eventual busy-worker release. Departure still advances the generation epoch and drops
preparation. Kind-bound analytical grants may use the existing Open command after departure; all
other report kinds retain their accepted departure behavior. Main-window, request-ID, close/busy,
file identity, exact-content and retry checks remain host-owned. No new command, arbitrary path,
filesystem support, shell permission or browser fallback was introduced.

Existing limits remain: one native preparation, eight saved-artifact slots and 64 operations per
host session, and at most three explicit opening retries per artifact. Retained grants consume the
existing capacity even after feedback dismissal; dismissal does not revoke a native grant. Capacity
exhaustion remains fail-closed and may require an application restart, which loses session recovery.
No restart-persistent grants, rollback, crash/power-loss guarantee or stronger association guarantee is
claimed. **Show in folder is omitted**: no admitted implementation exists in this pilot.

## Verification and independent security review

Evidence root: `C:\Dev\kst_v2_tmp\Stage22\ExportPilot\`.

- Final execution summaries and commands: `verification-summary.json`; frontend assertions in
  `frontend-final.json`, workbook fixtures in `analytical-workbook*.trx`, native logs/check exits,
  and frontend type/lint/build logs. Final counts/status are recorded there.
- Final serial protected frontend suite: **269 passed, 3 inherited skips, 0 failed**, 16 files.
  Of these, **34** current-surface/menu-Escape/lifecycle/failure cases include the four theme/density
  DOM inheritance scenarios (not an additive total). Typecheck, changed-scope ESLint and production
  frontend build pass.
  Existing workbook integration tests: **13 passed**. Native: **24 passed, 2 existing ignores**;
  `cargo check --locked --offline` and the current desktop-host `cargo build --locked --offline`
  pass after the owner closed the app. The serial rerun kept the
  normal test timeout and changed no assertions to hide the earlier parallel timeouts.
- Owner-requested sidecar rebuild/publish completed through `scripts/build-sidecar.ps1` with
  `--no-restore` appended to its existing dotnet invocation (installed cache only). Published and
  copied executables match: **127,855,236 bytes**, ProductVersion **0.1.0-alpha.21**, SHA-256
  `7E7195F4451A04D57E6FB7A953A8F860026BD52B40F0C4EBE245FF6A934F05F8`.
  This rebuild includes the current preserved working tree, including separately pending Part Info;
  it is not acceptance of that work or a new pilot backend behavior. No desktop app or live source
  acquisition was launched. Matching artifact identity does not establish a running desktop process.
- Mocked/simulated DOM evidence covers menu keyboard/focus/portal placement, all-parent and BOM filter
  scopes, duplicate activation, preference intent, failed/cancelled/obsolete preparation, Strict Mode,
  exact retry, failed opening, retained multi-operation recovery and observation-only reconciliation.
  Protected navigation/header/scroll, Component Information, Reports and pending lookup tests are
  included. This is not visual, native-modality or live-data proof.
- Existing synthetic API tests exercise **actual XLSX bytes/cells** for MPS Due/Release/all-parent
  populations and BOM filters/repeats/gaps/empty/stale/literal/precision/formatting. Backend/source and
  workbook-generator production code are unchanged; no source reader is invoked for preparation.
- Native tests exercise actual protected publication/collision/cleanup/identity/content checks with
  synthetic files, plus state-level analytical grant lifetime, kind binding and duplicate retry.
  The two existing interactive chooser/association tests remain ignored, not passing native UX proof.
- Separate-context, security-specific independent review: `INDEPENDENT_SECURITY_REVIEW.md`, based on
  retained `pilot.diff`. **No confirmed new material security defect; no blocking correction.**
  The reviewer source-inspected the controls and attributed supplied execution evidence; the reviewer
  did not rerun tests, launch the desktop or accept risk. Final test-only additions/documentation are
  in `pilot-final.diff`. A bounded follow-up checks the menu/feedback portal target: the application
  shell, outside its scrolling content, so light/dark, accent, font and density tokens are inherited
  rather than falling back to the body's dark/compact defaults. The independent follow-up leaves
  the disposition unchanged; save/open authority is unchanged.
- Partial/unavailable AFT results are not clean assurance. Known locked Stage 10 full-lint errors,
  the inherited native input-size-guard rustfmt exception, retained build warnings and existing Pkcs
  advisories remain exceptions/open items, not passes or risk acceptance. Full dotnet formatting was
  not rerun. A parallel attempt's navigation timeouts and locked-debug-copy build failure are retained
  separately from final reruns; no thresholds, permissions or guards were weakened to make them pass.

## Owner desktop checklist — not executed by the agent

Use the rebuilt current Tauri host with the matching published sidecar; a .NET sidecar rebuild alone
does not refresh an already-running native host. No new browser harness was created because no
applicable existing offline MPS setup was available.

1. Check light/dark × compact/comfortable at normal and narrow widths. Verify themed captions,
   scope, focus, menu placement and unclipped persistent feedback; no loose checkbox/default button.
2. Select a parent; MPS still says all N parts. Open the menu with keyboard, close with Escape and
   confirm the parent/trail remains. Check quarter/period/week sticking and selected-row/detail-scroll
   preservation while drilling into work orders and Immediate Material Analysis.
3. In MPS and filtered BOM, choose Excel once; verify exactly one Save As. Cancel, wait longer than
   four seconds, then Save again; confirm no regeneration. Exercise deliberate Discard and preference.
4. Change relevant context/leave during preparation: no late Save As. Ordinary MPS drilling/scrolling
   should retain a cancelled full-population workbook. Check current-week/BOM reload reasons rather
   than changing the real workstation clock merely for this test.
5. Where safely reproducible, reconcile a lost reply using Check save status after navigation: no
   second write. Exercise a controlled opening failure: remains Saved; Open again survives navigation
   and refuses changed/missing artifacts. Do not treat association acknowledgement as viewer success.

**Done:** bounded implementation, offline verification, sidecar rebuild and scoped independent review.

**Decided:** MPS/BOM-only pilot; existing host authority and finite session recovery; no Show in folder.

**Open:** owner visual/native checks and acceptance; inherited exceptions/advisories remain.

**Next checkpoint:** owner reviews this pilot before any further report migration; no commit/push.
