# Stage 22 — Shortage popup / shared Part Info presentation

Date: 2026-10-21. **POPUP ALIGNMENT / CREDIT HOLD-CIA / INCLUDE KSS — COMPLETE /
OWNER-ACCEPTED / BOUNDED FOLLOW-UPS CLOSED.**
The owner previously accepted the redesigned popup and Credit Hold/CIA addition and now confirms
Include KSS: “Works great! Accepted.” Final acceptance is dated 2026-10-21; no further acceptance gate.
This is owner-reported behavior, separate from artifact inspection and offline tests—not a claim that
every suggested desktop check occurred, the owner tested the exact hash below, agent desktop testing,
Part Info/MRP acceptance or Stage 22 closure. Earlier pending/no-commit statements and executable
identities below are historical checkpoint evidence, superseded only by this bounded closeout and the
final acceptance section at the end. One complete local commit with normal hooks is authorized; no push.

**Preceding owner-requested addition:** Credit Hold / CIA above Current Buyer Comment, described in
"Supplier-status amendment" below. This explicitly supersedes only the original design's exclusion of
supplier status for the shortages popup. Earlier presentation-only scope/artifact statements below
describe the preserved alignment checkpoint, not the cumulative supplier amendment or its new release.
Entry HEAD: `b5e5617665505bd477ff54bd6d1bd0991e92d60e`. Only entry modification was
`src/frontend/tsconfig.tsbuildinfo`; actual bytes backed up outside Git and restored after build,
SHA256 `F10640732C264545F2629CE2F89F857EDA4DD827D0B29CB6594E55E07E9335B8`.
Build-info remains unstaged/excluded. No staging, commit or push is authorized or performed.

## Authority and bounded delivery

Owner dispatch and supplied `C:\Dev\kst_v2_tmp\Shortage_popup_part_info_alignment_design_spec.md`
authorize this presentation-only follow-up, not the companion Part Info/MRP implementation.
Current status, enacted instructions/security policy, accepted Stage 11 algorithms and existing
implementation/contracts remain authoritative. The two production modal callers were confirmed:
Workspace Shortages (`LongTermShortagesPanel`) and BOM (`MpsWorkspace`).

- `shared/partInfoParts.tsx/.css`: display-prop PartIdentity, FactsStrip, InfoCard, Fields,
  ScrollTable and keyboard/roving InfoTabs extracted once. No business DTOs or lookup data flow moved.
  The lookup retains its original classes, IDs, form/Enter/refresh/abort/identity behavior, prices,
  quantities, supplier strip and buyer-comment dialog. Its original 11 tests are unchanged; a separate
  literal pre-extraction markup/ID regression covers the shared regions and simultaneous lookup/popup.
- Shortages-only modal hierarchy: fixed header outside the scrolling body; identity/site/status/KSS/
  severity, Shortage Snapshot with six independently valued fields and Demand Parents, standard facts,
  closed More part details, inline buyer-comment expansion and full-width table tabs. Dialog width
  1100px; shared responsive facts/disclosure rules and bounded table scrollers retained.
- Existing popup hooks, acquisition/refresh timing, independent inventory and retry callbacks remain.
  All tab panels stay mounted. Alternates activate on first reveal only, with presentation activation
  keyed to workspace/site/selected part; tab switching does not reacquire. Hidden panels, inactive
  roving tabs and collapsed disclosure content are excluded from the modal focus loop.
- Blocking dialog name, initial Close focus, capture-phase Escape, non-dismissing backdrop, scroll
  lock, caller focus return and BOM markup/labels/Show MRP behavior remain. Existing BOM disclosure
  activation behavior is deliberately not changed to the shortages tab model.
- Exact quantity/null handling, four-decimal popup price formatting, PO qualification/order/as-of-date
  detection, original metadata and localized loading/empty/error/stale/unavailable copy remain.
  The three projection-dependent snapshot cells now explicitly show `…` / `Unavailable` as specified.
- Chip/severity styling is shared through focused stylesheets and existing tokens, not imported from
  entire Reports/Shortages stylesheets. Grid tooltip behavior/severity rules are unchanged.

### Status source/context evidence

Grid `QadStatus` selects `pm.pt_status` from `pt_mstr`, joined on domain + scoped component part
(`QadLongTermShortageSourceReader.BuildBatchQuery`, lines 63, 78). Detail `PartStatusCode` selects
`pt.pt_status`, filtered on domain + component part
(`QadComponentSourceReader.BuildMasterPlanningQuery`, lines 108, 128–129). Both modal hooks are
selected through the same workspace assignment and component. This supports the grid code as a
**loading-only** fallback. Loaded detail status, including null, takes precedence; absent/failed detail
uses a neutral dash. Snapshot versus detail acquisition times remain different; equal current values
are not guaranteed or claimed. No live lookup was used to establish source equivalence.

## Verification and limits

Focused final run: **202 passed / zero failed / zero skipped, 11 files**:
ComponentInfoModal (61), PartInfoLookup (11), PartInfoPresentation (1), InventoryLocationsPanel (6),
LongTermShortagesPanel (27), MpsWorkspace.navigation (39), BomPanel (37), ComponentOrdersPanel (13),
buyerCommentPresentation (2), useLongTermShortageProjectionDetail (2), longTermShortagesPresentation (3).
Independent expected snapshot values/UOMs, null/price formatting, PO ordering/as-of date, alternate
order/duplicates/nulls/lazy lifecycle, tab keyboard/focus, comment text/expansion/Escape, localized
failures/retries, hidden inventory timing, unchanged BOM and navigation/export behavior are covered.
The existing Windows Edge Workspace Shortages grid fixture also passed; it is not new popup visual
acceptance and is not evidence for the separate S22-029 owner confirmation.

Changed-file ESLint (eight TS/TSX files, `--max-warnings 0`), `npm.cmd run typecheck` and
`npm.cmd run build` pass. Tests use offline/synthetic fixtures, not live database queries. React act
fixture warnings remain visible; Vite retains the existing static/dynamic Tauri import and >500kB
chunk warnings. Initial scoped AFT diagnostics had zero errors/warnings but partial complexity;
several post-edit requests failed filesystem deadlines. A final narrow request returned authoritative
zero errors/warnings for ComponentInfoModal, LongTermShortageDetailModal and partInfoParts, still with
partial complexity; other post-edit AFT coverage is not claimed. TypeScript/lint/build are the executed
gates. Broader inherited .NET/Rust format and locked Stage 10
lint exceptions remain exceptions, not passes or new risk acceptance; no broad cleanup/suite rerun.
Other suites' inherited skips were not removed or claimed executed by this zero-skip focused run.

Routine self-screen against `SECURITY.md`, Security Assurance Policy triggers/control preservation
and AI Security Review finds no changed security/trust boundary: comments remain React text,
existing requests/exports/native authority unchanged. No independent security review is claimed or
required solely for presentation extraction. No backend/API/generated contracts/native source,
dependencies/capabilities, live access, installations, database objects/changes, source algorithms,
QXtend/Mass Updater submissions, sidecar republication or deployment are included.

## Evidence and review release

Evidence root: `C:\Users\dgoss\AppData\Local\Temp\opencode\shortage-popup-alignment\`.
`initial-tests.json` retains the first 77-test pass; `final-tests.json` the final 202-test pass.
`entry.tsbuildinfo`, `sidecars-entry.json`, guarded `build-release.ps1` and the complete
`focused.diff` (including relevant new repository files, excluding entry build-info) remain outside Git.
The first guarded release attempt stopped **before Cargo** for running installed KST. The owner then
confirmed normal closure. Windows PowerShell's stderr-as-error logging interrupted the first Cargo
invocation; normal Cargo progress is not a build failure. The external script's logging preference
was corrected only around Cargo, retaining the process check, exclusive lock, destination probes and
nonzero-exit failure gate. A fresh guarded retry passed (`--release --features tauri/custom-protocol
--locked --offline`, 2m 24s). No process was terminated or guard weakened. `cargo-release.log` retains
PowerShell's NativeCommandError formatting of normal progress plus Cargo's successful Finished line;
the checked native exit code was zero.

**Preceding alignment review executable:** `C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`,
repository package **0.1.0-alpha.21**, PE FileVersion/ProductVersion **0.1.0**, **15,307,776 bytes**,
last-write UTC `2026-10-20T23:28:14.3501984Z`, SHA256:
`9249BD7CAA8F3D8A1228450E91B9855B9CA28F7765BF64674D617DBB399F27E7`.
`verify-artifacts.ps1` / `ARTIFACTS.json` verify current asset names embedded and release newer than
assets (`index-D0VSQCqb.js`, `event-CRVUXHW_.js`, `index-EFiTdbT8.css`), all four sidecars unchanged
(127,543,462 bytes each; SHA256 `16CE3AC6FB9A05CA8CAF7D8E3696431251194983EBAB322D3D02E2BD5CC9EA6B`),
source/test fingerprints and actual entry build-info preservation. No sidecar publication, native
source/generated-schema drift, app launch or agent desktop test occurred. Review this executable,
not an installed or older/debug copy.

## Backlog reconciliation and owner visual checklist

Use existing S22-008 for this bounded shortage-popup follow-up, S22-018 for shared presentation
extraction and S22-037 for the Part Info data-region/subsequent MRP update. S22-033 remains the existing
deferred Component Information/MRP disposition. No duplicate backlog IDs or wholesale reopening of
historical acceptance: accepted Reports/S22-008 deliveries stay accepted, this new popup remains
pending visual acceptance, and broader workstreams/Stage 22 remain open.

**S22-029 documentation-only reconciliation:** the owner supplied confirmation in this dispatch that
main MPS horizontal scrolling is resolved. Record it as **OWNER-CONFIRMED RESOLVED — 2026-10-20**,
not new geometry testing, exhaustive supported-window validation or acceptance of the new popup.
The separate historical detail-scroll review is not silently accepted by this statement.

Owner review, using only already available/owner-authorized data:

1. Compact and comfortable density, light/dark: open a shortage and check sticky identity/Close,
   snapshot first, standard facts/disclosure, comment and all three tabs. Check healthy and KSS chips.
2. Narrow window: chips wrap, facts reduce to two columns, details stack; no clipped values or parents.
3. Long/multiline comment: Show full comment / Show less expand inline, preserve text/metadata;
   Escape closes only Component Information and restores the triggering part button.
4. Scroll PO, inventory and alternate tables horizontally/vertically; header/Close stay accessible.
   Arrow/Home/End tabs, Tab/ShiftTab wrap and closed details never visit hidden controls.
5. Where available without new acquisition authorization, review localized unavailable/stale states and
   retries; offline tests cover these states but do not establish live source failures. Revisit tabs
   without duplicate alternate acquisition; select a different component and reveal it independently.
6. Spot-check standalone Part Info (including its own comment dialog/supplier strip) and BOM modal:
   existing labels, disabled Show MRP, close/focus and navigation/export behavior stay unchanged.

Next checkpoint: owner popup visual review, then separately authorized Part Info/MRP work reusing
these display pieces. Implementation completion is not owner acceptance or Stage 22 closure.

## Supplier-status amendment — 2026-10-20

**IMPLEMENTED / OFFLINE-VERIFIED / CURRENT EMBEDDED RELEASE PREPARED; OWNER ACCEPTANCE PENDING.**
Owner likes the alignment and authorizes this addition before final acceptance; this is not acceptance
of the complete popup, Part Info/MRP implementation or Stage 22. Original uncommitted alignment is
preserved. Supplier-checkpoint entry files/backlog and actual build-info bytes are backed up under
`shortage-popup-alignment\supplier-status\`; build-info entry SHA256 is again `F1064073…` (full hash
above), restored byte-for-byte after the frontend build, excluded and unstaged.

### Confirmed current Windows path / smallest extension

- `PartInfoService.LookupAsync` acquires comments independently, groups PO `SupplierIdentifier` with
  `StringComparer.Ordinal`, sends one supplier-only enrichment request and matches returned keys
  ordinally even if the result dictionary uses an insensitive comparer. `ShortagesComponentOrderEnrichmentReader`
  retains parameterized 1000-identifier batches and latest `[Supplier Name]` facts ranked by Date/ID.
- Current PO reader already supplies the accepted exact supplier risk identity/display. Stage 10's
  owner-directed exact displayed-name → `[Supplier Name]` rule (`STAGE_10_FIELD_DISCOVERY_LEDGER.md`
  rows Supplier/Credit Hold/CIA, 10.3F.5) supersedes the old vendor-number idea. No new join, trim,
  case-folding, numeric conversion, fuzzy match or Approved Alternates supplier scope is introduced.
- Before this amendment, `LongTermShortagesService.GetPurchasingAsync` requested only
  `(site, [selected component], [])`; its PO DTO's nullable risk cells did not establish supplier
  coverage or acquisition availability. The existing API also omitted supplier identifiers.
- C# DTO/spec were extended first, backend built to generate OpenAPI, then TypeScript regenerated
  before implementation. `LongTermShortagePurchasingDto.Suppliers` reuses existing Part Info
  section/supplier DTO shapes; `SupplierIdentityUnavailable` discloses qualifying POs without a key.
  Application result reuses the same existing section/supplier records. See
  `docs/api/WORKSPACE_SHORTAGES.md` for field/availability semantics. No handwritten API DTO.
- Selected-component conventional PO qualification/order/source reader is unchanged. Only those lines
  supply identities; ordinal dedup retains first display and case-distinct identities. One separate
  supplier-only request uses existing batching (including >1000 identities), not a request per row
  or supplier. No supplier request for empty coverage; component-only comment acquisition still runs.
- Independent comment/supplier capture preserves successful POs/comments on either optional-source
  failure. Missing/unmatched risks stay nullable; failure retains identifiable supplier rows and a
  localized error with no acquisition timestamp. Current statuses have their own acquired timestamp,
  not the shortage snapshot's. Cancellation propagates; snapshot replacement is checked after both
  enrichment phases; existing client selection/cache/retry ownership and obsolete-response rejection
  remain unchanged. No full Part Info lookup, MRP/BOM acquisition or shortage calculation is added.

### Presentation / preserved consumers

`shared/SupplierStrip.tsx/.css` extracts the existing Part Info strip as display props only. Lookup
markup/classes, summary/details behavior, acquisition, IDs and original eleven tests are unchanged;
literal empty-strip markup is also checked. Popup passes current supplier rows, loading/error,
missing-identity disclosure and the existing purchasing retry callback. The strip is immediately
before Current Buyer Comment and includes individual identifier/display/Yes/No/Unknown rows in the
existing expandable table. In this popup, failed acquisition's null flags read **Unavailable** rather
than Unknown; the lookup's established failure-detail display is deliberately retained.

**None** is allowed only for nonempty, complete explicit-No coverage. Unknown/unmatched suppliers,
missing identities and failures cannot yield None; empty acquired coverage uses **No supplier status
is available from open POs.** Current supplier scope is explicitly described in the strip. No component
risk classification, snapshot-fact claim, alternate-supplier expansion or business-rule change.
BOM never renders the new strip. Modal ownership, tabs, inline comments, quantities, exports and locked
Stage 9–11 algorithms remain unchanged. Part Info/MRP remains a subsequent, separately scoped task.

### Amendment verification / evidence

New execution, without adding overlapping previous runs: **116 frontend passes / zero failures/skips,
six files** (ComponentInfoModal 67, PartInfoLookup 11, PartInfoPresentation 1,
InventoryLocationsPanel 6, LongTermShortagesPanel 27, useLongTermShortagePurchasing 4).
The original alignment run's other **96 cases in six unaffected files** are reused evidence, not
rerun or added to the new run's executed total. Backend: **46 application, 33 API, 24 reader and
10 architecture passes**, zero failures/skips. TRX files and `frontend-tests.json` are under
`C:\Users\dgoss\AppData\Local\Temp\opencode\shortage-popup-alignment\supplier-status\`.

Independent expected results cover repeated suppliers, mixed flags, unmatched and case-distinct keys,
missing identities, no-PO successful comments, either optional source failing without erasing the
other, one >1000-supplier request, replacement snapshot/cancellation during enrichment, late responses
after component/workspace/snapshot changes and retry. Six changed-file ESLint checks, TypeScript,
frontend build and backend/API builds pass. Generated TypeScript reproduces from built OpenAPI.
Initial AFT TS diagnostics were zero, C# analyzer nonresponsive/complexity partial; post-edit requests
hit filesystem deadlines. No post-edit AFT completeness is claimed; compiler/tests are executed gates.
Existing React act fixture, Vite import/chunk warnings and broader inherited .NET/Rust format/locked
Stage 10 lint exceptions remain explicit—not passes, fixes or risk acceptance. No broad cleanup.

Routine self-screen: existing read-only parameterized reader and credentials/network boundaries
retained, untrusted supplier/comment strings remain React text, logs contain failure type only.
No dependency/capability/permission/SQL/database-object/trust-boundary change or independent security
review is claimed. No live reads, installation/deployment, submissions/imports, commit or push.

`supplier-status/build-review.ps1` passed the unchanged KST/process check, exclusive shared build lock
and destination probes before publication/build. It follows `scripts/build-sidecar.ps1` publication
settings with `--no-restore` to stay offline (no directory-wide cleanup), copies the published sidecar
and companion configs to the triple/release/debug locations, then builds with
`cargo build --release --features tauri/custom-protocol --locked --offline`. Total 218 seconds;
Cargo finished in 3m 13s. Native exit codes checked zero; Windows PowerShell still formats Cargo's
normal progress as NativeCommandError in the UTF-16 log, not a compiler failure. KST was closed;
no process termination, guard bypass, installation, application launch or desktop test occurred.

**Preceding supplier-status review executable:** `C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`, package
**0.1.0-alpha.21**, PE FileVersion/ProductVersion **0.1.0**, **15,307,264 bytes**, last-write UTC
`2026-10-21T00:06:16.0009774Z`, SHA256:
`6FE7117250E9C0EF6123D71467D9E983047BA815E7C2B4309AF9FF5E75F1C392`.
`supplier-status/ARTIFACTS.json` / `verify-artifacts.ps1` confirm the newer host embeds all current
asset names (`index-CN5Y1Blw.js`, `event-BX9IKleu.js`, `index-BF-W0QaU.css`), current source/test
fingerprints and exact entry build-info bytes. All **four republished sidecars** are **127,547,558
bytes**, SHA256 `28A3F109426308B83771AF400E0FD45E12B38F1FC55DA8F04008BF390271F298`;
both companion configs agree across all four locations. These are repository review copies, not an
installed deployment; the debug host was not rebuilt or presented as current. No native source/schema
drift; index remains empty. Original `9249BD7C…` host/evidence above remains historical alignment evidence.

`supplier-status/cumulative-popup.diff` includes all 27 changed/new repository paths from `b5e5617`,
including this addition and the preserved alignment. `supplier-addition.diff` isolates 19 paths against
the actual amendment-entry file snapshots (or absent entry files), not against a guessed alignment.
Both exclude build-info and pass read-only reverse-apply checks. `backlog-update.diff` is the actual
amendment-entry external backlog delta; `backlog-cumulative.diff` also includes the earlier alignment
amendment against its explicitly reconstructed original baseline. Existing root evidence is retained.

### Additional owner visual check

Using the current identified release and only already available/owner-authorized data: verify the strip
directly above the comment, expand multiple suppliers and compare names/individual flags, check
None versus Unknown/Unavailable/no-supplier wording where available, narrow-window/density/theme
wrapping, details-table scrolling and keyboard/Escape focus. Confirm the comment, PO tabs and independent
inventory still work. Offline cases cover failures; do not acquire new live data solely for this check
without owner authorization. Popup acceptance and existing S22-008/018/037 follow-up remain pending;
accepted history/S22-029 resolution are unchanged. Next checkpoint is final owner popup review, not MRP.

## Include KSS amendment / owner acceptance — 2026-10-21

### Decision, implementation and preservation

The owner visually accepts the redesigned shortage popup and Credit Hold/CIA addition as recorded
above. Separately, this dispatch explicitly supersedes Workspace Shortages' KSS-only mode and default
visibility: **Include KSS** starts unchecked, excluding existing `isKss` rows; checked admits both KSS
and non-KSS rows that otherwise match. It is not a new classification, source or projection option.
Component Orders' independent KSS-only filter is unchanged.

`LongTermShortagesFilters.includeKss` replaces `kssOnly`; the predicate is `includeKss || !row.isKss`.
Component/search, Buyer/Planner, Status, shortage/Show All selection and all sorts still intersect;
Show All never overrides KSS exclusion. Counts, normal empty states and ordered export component keys
come from the same resulting rows. Export context includes the renamed filter even when row identities
are unchanged, preserving retained-preparation invalidation and obsolete-generation rejection. Existing
selected-component ownership, modal lifecycle and focus return are unchanged; no persistence is added.

Caller/preference inspection found the panel's visit-local state and presentation tests, plus three
diagnostic performance-harness consumers importing the filter/defaults. Those diagnostic consumers now
use inclusion semantics too; their comments distinguish this amendment from historical verification.
No capture files or historical pass counts are rewritten and those capture runners were not executed.
No application acquisition/hook, cache, projection, allocation, classification, backend/API/contract,
native source, dependency/capability or database-object change is made in this amendment. Supplier
backend/contracts already present at entry are preserved, not reimplemented or republished.

Entry HEAD remains `b5e5617665505bd477ff54bd6d1bd0991e92d60e`. All cumulative entry changes were
snapshotted before editing under `shortage-popup-alignment\include-kss\entry\`, with the actual
build-info bytes copied independently outside Git, SHA256
`F10640732C264545F2629CE2F89F857EDA4DD827D0B29CB6594E55E07E9335B8`.
Those exact bytes were restored after frontend build, not replaced with HEAD/older assumed bytes;
build-info remains unstaged/excluded. No staging, commit or push is authorized/performed.

### New focused verification and inherited limits

**67 frontend passes / zero failures / zero skips, five files**, in two disjoint runs:
LongTermShortagesPanel 31, longTermShortagesPresentation 4, AnalyticalExcelExport 23,
analyticalExportApi 1; reportExportAdapters 8. Independent literal component identities cover a mixed
population, initial exclusion, both toggle directions, other simultaneous filters, sort order, matching
counts/export keys, Show All independence, all-KSS empty results/disabled export, unchanged localStorage,
fresh-mount default, no toggle-time reacquisition/detail hydration, focus return, cancelled-preparation
invalidation (including unchanged identities) and no save from a late preparation after KSS context change.
The existing synthetic Edge grid geometry case passed within the panel suite, not desktop acceptance.

Four changed frontend files and three diagnostic callers pass ESLint (`--max-warnings 0`), frontend
`npm.cmd run typecheck` and `npm.cmd run build` pass. The first diagnostic lint invocation from the
frontend directory reported three outside-base-path warnings/exit 1; the corrected repository-root
invocation used the same existing config and passed, without suppressions or rule changes. The
diagnostic-caller standalone typecheck initially omitted Vite's environment types and failed on
`ImportMeta.env`; the corrected check with the existing `node,vite/client` types passes, without
executing capture runners or altering configuration. Initial AFT
diagnostics were authoritative/zero for two production files but complexity partial; post-edit requests
hit filesystem deadlines, so complete post-edit AFT coverage is not claimed. Executed compiler/lint/tests
are the gates. React act fixture and Vite static/dynamic import/>500kB warnings remain visible. Inherited
broader locked Stage 10 lint/.NET/Rust formatting exceptions and other suites' skips remain explicit,
not fixed, passed, removed or risk-accepted by this zero-skip focused run.

Unaffected popup/supplier/backend evidence in the preceding sections is reused, not rerun or added to
the 67 newly executed tests. Routine self-screen preserves existing text rendering, protected export
and host authority. No trust/security boundary change or independent security review is claimed or
needed for this bounded predicate/control change. No live access, acquisition during verification,
installations, QXtend/Mass Updater submissions/imports, database changes, deployment or process killing.

### Evidence and next review

Evidence root: `C:\Users\dgoss\AppData\Local\Temp\opencode\shortage-popup-alignment\include-kss\`.
`frontend-tests.json` / `export-adapter-tests.json`, `entry-status.txt`, `entry-tracked.diff`, entry
snapshots/build-info, guarded `build-release.ps1` / Cargo log, isolated `include-kss.diff`, complete
`cumulative-popup-shortage.diff`, backlog entry/diff and final artifact/preservation evidence are local,
not committed. The isolated diff is against actual task entry (including prior untracked file content),
not HEAD; cumulative includes earlier popup/supplier work and new files but excludes build-info.
`DIFFS.json` records the **11-path isolated / 32-path cumulative** diffs, successful read-only reverse-
apply checks, tracked/new-file whitespace checks, matching source fingerprints and empty index.

Use existing S22-008/018/037 entries only. Popup/supplier visual acceptance is preserved; **Include KSS
remains pending owner visual confirmation**, Part Info/MRP and Stage 22 remain open. On the identified
review release, using already available/owner-authorized data: confirm unchecked excludes KSS, checking
adds matching KSS without removing non-KSS, unchecking removes only KSS; combine search/Buyer/Status,
Show All and sort, compare counts/export scope and recheck popup/supplier strip/focus. The suggested
checks are not represented as already performed. No new live reads are authorized by this checklist.
Next checkpoint: owner visual confirmation of Include KSS, not commit, deployment or Stage 22 closure.

### Current guarded embedded review release

The unchanged process/shutdown check, exclusive shared build lock and destination probes passed before
`cargo build --release --features tauri/custom-protocol --locked --offline`. Native exit code zero;
no guard bypass, process termination, sidecar publication or application launch/agent desktop test.
`build-release.ps1` / `cargo-release.log` retain command provenance. Windows PowerShell formats normal
Cargo progress as NativeCommandError in the UTF-16 log; the checked exit code, not that formatting,
determines success. Frontend build is the one recorded above; Cargo embeds its already-built assets.

**Current review executable:** `C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`, repository package
**0.1.0-alpha.21**, PE FileVersion/ProductVersion **0.1.0**, **15,307,264 bytes**, last-write UTC
`2026-10-21T14:57:19.9464699Z`, SHA256:
`ABF27981C0B23A34ECD5C557FB5EA69E1D969EC3A35432C8F19E2CD140B37338`.
`include-kss/verify-artifacts.ps1` / `ARTIFACTS.json` verify all current asset names embedded and host
newer than assets (`index-v22SSOpu.js`, `event-C9axWYol.js`, `index-BF-W0QaU.css`). All four supplier-status
sidecars are unchanged at **127,547,558 bytes**, SHA256
`28A3F109426308B83771AF400E0FD45E12B38F1FC55DA8F04008BF390271F298`; both companion configs match the
preceding supplier publication at all four locations. Twenty-two unaffected task-entry files remain
byte-identical, including popup/supplier implementation, backend, contracts and build-info. Native source/
schemas are unchanged; index empty. This is a repository review host, not an installed deployment or
proof of which binary the owner previously accepted; previous hosts remain historical evidence.

## Final owner acceptance / bounded closeout — 2026-10-21

**COMPLETE / OWNER-ACCEPTED / CLOSED** for the delivered shortage-popup alignment, selected-PO
supplier-status addition and Include KSS follow-up only. Owner confirmation: “Works great! Accepted.”
Preserve original S22-008/Reports acceptance, S22-018/030's accepted correction and S22-029's documented
owner-confirmed resolution. Existing backlog S22-008/018/037 records only these delivered portions;
broader shared UI/performance work, S22-033/037 Part Info/MRP and Stage 22 remain open.

Closeout from `b5e5617665505bd477ff54bd6d1bd0991e92d60e` reuses the separate verification runs above:
202 alignment frontend passes; 116 supplier frontend passes and 46 application / 33 API / 24 reader /
10 architecture passes; 67 Include KSS frontend passes. These overlap across checkpoints and are not
added into a cumulative test total. Existing lint/type/build/publication/release evidence is reused;
no suites, builds, sidecar publication or independent review repeated solely for closeout. All 28
retained source/test fingerprints and the current release hash match at closeout entry. Existing
AFT coverage limits, React/Vite warnings, inherited lint/format exceptions and other suites' skips
remain explicit, not new passing checks or risk acceptance.

One local commit includes the complete 32-path implementation/tests/contracts/documentation, including
new shared files and both OpenAPI/generated TypeScript. Closeout checks the complete cumulative diff,
new files, exact staged paths, whitespace and committed contents/remaining tree, honoring normal hooks.
Actual entry `src/frontend/tsconfig.tsbuildinfo` SHA256 `F1064073…` (full hash above) is byte-preserved,
unstaged and excluded; build outputs/evidence/logs/binaries remain outside the commit. Preservation
copies and closeout evidence are under `include-kss/commit-closeout/` in the external evidence root.
No push, MRP integration, deployment, installation, live database access, database changes or
QXtend/Mass Updater submissions/imports. Architecture, loopback-only sidecar, Windows-integrated
authentication, read-only sources and locked Stage 9–11 algorithms are preserved.

### Separate Part Info/MRP session handoff

- `src/frontend/src/shared/partInfoParts.tsx` exports **PartIdentity, FactsStrip, Fields, InfoCard,
  ScrollTable, InfoTabs**; companion `partInfoParts.css` supplies display styles/responsive rules.
- `src/frontend/src/shared/SupplierStrip.tsx` exports **SupplierStrip**, with `SupplierStrip.css`.
  Display-only props; consumers retain acquisition/population ownership. Current consumers are
  `reports/PartInfoLookup.tsx` and shortages' `components/ComponentInfoModal.tsx`.
- `shared/shortageSeverity.css` supplies focused severity styles. Shortage-specific **ShortageSnapshot,
  ShortageBuyerComment, ShortageOpenOrders** remain in `components/LongTermShortageDetailModal.tsx`;
  they are not a shared MRP acquisition/calculation layer. Lookup BuyerCommentDialog remains local.
- This record and `docs/api/WORKSPACE_SHORTAGES.md` describe the delivered popup/supplier semantics;
  `docs/implementation/KST_v2_S22_REPORTS_REDESIGN.md` retains earlier Reports acceptance. Whole Part
  Info data-region/MRP integration is not delivered or accepted here; resolve separately under existing
  S22-033/037 scope. Suggested historical desktop checks are not retroactively claimed performed.

Next checkpoint: the separate owner-scoped Part Info/MRP session; no MRP implementation in this closeout.
