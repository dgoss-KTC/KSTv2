# Stage 22 — Part Info & MRP

2026-10-21. **COMPLETE / OWNER-ACCEPTED / BOUNDED FOLLOW-UP CLOSED.**
Entry HEAD `f3896685829677276d9ae4cb0df991583efc5d5c`; no later commits/unrelated source changes at entry.
Only entry modification: `src/frontend/tsconfig.tsbuildinfo`, actual SHA256
`4AA277F5551D7AFEED9D776DD3B4230F414AA98B0AB0B0BE73A629992FDD6E29`.
Its bytes were independently saved outside Git and restored after the final frontend build; the hash
matches byte-for-byte. It remains unstaged/excluded. HEAD/the previous checkpoint's hash were not used.

## Authority / single implementation checkpoint

Owner dispatch and `C:\Dev\kst_v2_tmp\Part_Info_and_MRP_design_spec.md`, subject to the dispatch's
binding corrections; AGENTS.md, enacted security policies, current status/master checklist, accepted
Reports redesign, shared popup closeout and Stage 18 Component MRP closeout/source ledger.
Accepted popup, Credit Hold/CIA and Include KSS are preserved, not redone or reopened.

Bounded sequence: inspect current contracts/readers/period/workbook/native/export tests; extend C#
DTOs/build OpenAPI/generate TypeScript; retain both accepted projection modes and add screen-only
preparation; integrate shared presentation/title export and bounded native fields/routes; verify
independent numbers/lifecycles/recovery offline; publish changed sidecar and guarded embedded host;
record evidence and stop for owner review. No separate discovery checkpoint, speculative cache
optimization, locked Stage 9–11 algorithm change, dependency/capability or database object.

## Delivered behavior / source confirmation

- Default Reports `PartInfo` is **Part Info & MRP**. Five entries, three Workbooks then two On screen;
  standalone Component MRP selector removed, no redirect/deep link/BOM Show MRP changes.
- Shared PartIdentity/FactsStrip/Fields/InfoCard/ScrollTable/InfoTabs and SupplierStrip are reused.
  Five-row MRP grid follows warning/comment strips and precedes the unchanged three detail panels.
  Frozen metric column, keyboard-scrollable periods, density tokens, negative-only QOH wash,
  unconfirmed receipt markers, server labels/bounds, existing exact UOM rounding. All horizons/periods.
- Current accepted `QadComponentMrpReader` composes the existing raw Stage 11 schedule SQL, validates
  direct-part metadata/PO confirmation, reads QCTC and fails rather than substituting incomplete data.
  `ComponentMrpService` already calls `LongTermShortagesBuilder.BuildBoth`; formerly it discarded the
  unselected row. This change retains both, and `Select` chooses the matching row before period
  regrouping/workbook generation. No reader SQL, netting, source population, workbook or locked builder
  change. Source confirmation was source/test inspection only, not a live query.
- Typed screen API/retention semantics and bounds: `docs/api/COMPONENT_MRP.md`. Contract-first build
  generated OpenAPI before TypeScript/consumer implementation; no handwritten business DTOs.
- Independent MRP state, applied identity, coalescing/abort/newest-response guards and scope-bound
  protected export. Full Refresh preserves settings; new Lookup resets; inventory refresh stays
  independent. Existing section failures/retries, comments, supplier states, IDs and keyboard behavior
  remain. Site preference is validated using acquired lookup spec, not a second hard-coded list.
- Owner expiry decision: no export-only acquisition/warning fallback. Refresh MRP must replace the
  screen before new preparation. `prepareDisabledReason` blocks only new preparation and is excluded
  from shared ExportVisit's remount key/save guard, preserving already prepared exact-byte retries.
  Explicit context change still invalidates preparation; unknown-save and saved/open recovery retain
  original ComponentMrp kind/host authority. Other reports' algorithms/behavior are unchanged.

## Verification and evidence

Full local evidence: `C:\Users\dgoss\AppData\Local\Temp\opencode\part-info-mrp\`.
Final artifact identity is recorded below. Intermediate failed commands are
not passing evidence: initial native run omitted the required `KST_MRP_TEST_ROOT` (corrected without
changing tests); simultaneous .NET builds briefly contended for compiler output (serial retry);
initial type/lint/scoped format findings were corrected in checkpoint files. Initial root-level
format invocation used the wrong solution location; documented `src/backend` invocation is used.
Existing AFT requests have filesystem deadline failures; no complete clean diagnostic snapshot is
claimed. Actual compilers/tests remain the gates. Inherited broad format/locked Stage 10 lint,
React/Vite warnings and intentional interactive/skipped tests remain explicit, not suppressed.

Final newly executed offline gates (do not add overlapping intermediate/retry counts):

| Gate | Result / retained evidence |
|---|---|
| Frontend | **252 passed / zero failed / zero skipped,19 files**, final `frontend-final.json`, bounded two workers / explicit15-second runner deadline for heavy synthetic UI cases. Includes final regression: Retry lookup after a failed full Refresh preserves the selected MRP controls; a new Lookup still resets them. Prior five-second concurrent/bounded-two-worker runs retain250 passes/one deadline-shaped failure in the unchanged heavy Open Orders popup/facet case (5.47s, registration stack `STACK_TRACE_ERROR`, no assertion mismatch). That case passed isolated at4.595s with the default deadline and4.329s with the explicit15-second bound. Failed reports remain `frontend-parallel-timeout.json` / `frontend-two-worker-timeout.json`; no test-source/skip change or relabeling those commands as passing. Other suites' inherited screenshot skips remain unmodified, not claimed executed. |
| API | **34 passed**, `reports-api-final.trx`: screen/numeric/ownership/bounds/expiry/preparation and Part Info/other report endpoint regression scope. Six actual synthetic screen workbooks retained under `workbooks/`; all period/mode assertions use independent literal expectations. |
| Application | **10 passed**, `application.trx`: Component MRP and Part Info orchestration. |
| Domain / exports | **88 passed**, `domain-exports.trx`: existing Component MRP/locked Long-Term Shortages and report/workbook regressions. |
| QAD mapping | **5 passed**, `qad-mrp-offline.trx`, synthetic reader mapping only; no live reads. |
| Architecture | **10 passed**, `architecture.trx`; initial shared-output compiler contention resolved by serial retry. **147 backend passes total** across these five disjoint projects. |
| Native | **17 passed / two intentional interactive ignores**, `cargo test --locked --offline --lib component_mrp` with required bounded `KST_MRP_TEST_ROOT`; production synthetic publication/replacement/close/retry/kind/departure and new fixed request/token/route validation. No native chooser or real Excel association interaction claimed. |
| Contracts/compiler | Backend/API build, OpenAPI regeneration, `npm.cmd run generate:types`, `npm.cmd run typecheck`, `cargo check --locked --offline` and test compilation pass. No package restoration/installation. |
| Lint / format / frontend build | Zero-warning ESLint across 13 changed TS/TSX files, scoped C# `dotnet format` verify and changed native-file `rustfmt --check` pass. New scoped formatting findings were corrected without broader cleanup. `npm.cmd run build` passes; existing Vite mixed core import and >500kB chunk warnings remain. |

Source tests and generated contracts are included in the authorized local commit; evidence, logs,
synthetic files, build outputs and exact entry build-info backup remain outside Git. Offline evidence
does not establish owner acceptance; the explicit owner decision is recorded separately below.

First guarded publication/build attempt stopped **before sidecar publication** because KST was running
(`kst-tauri` PID71940, `Kst.Api` PID60984 at that inspection). The earlier process command showed no
visible rows, but the guard is authoritative; no inference about launch timing is made.
No processes were terminated and no shutdown/exclusive-lock/destination guard was
weakened. Owner then confirmed normal shutdown (“Closed — retry”). An immediate retry still saw
shutdown processes; a subsequent process inspection and the unchanged guard allowed publication/build.
The successful foreground retry checked native exit codes zero; Cargo Finished release in3m31s.
PowerShell's UTF-16 log formats normal Cargo progress as NativeCommandError; the native exit code,
not that formatting, is the success gate. No termination/bypass/application launch or desktop test.

## Current embedded review release / final diff

Use **`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**, package **0.1.0-alpha.21**,
PE FileVersion/ProductVersion **0.1.0**, **15,351,296 bytes**, last-write UTC
`2026-10-21T16:21:58.2866127Z`, SHA256:

`FF9A8BD784F0F44C8165A01B781EA399E46B3C380224F6644A63BB1CF382948A`

`build-review.ps1`, `sidecar-publish.log`, `cargo-release.log`, `build-embedded-final.ps1`,
`cargo-embedded-final.log`, `verify-artifacts.ps1` and
`ARTIFACTS.json` retain guarded offline command provenance/artifact checks. Published/binaries/release/
debug sidecars were refreshed: all four **127,568,038 bytes**, SHA256
`EC8C666F8A955E9864B647F6D9A58D299FD65C864F096D411120EF776538BB15`;
both appsettings companion files agree across all locations. Publication follows established
`scripts/build-sidecar.ps1` self-contained win-x64/single-file/native extraction settings with
`--no-restore` and no directory-wide cleanup. No authentication/loopback/source-access boundary change.
The final frontend-only Refresh/retry correction required a second embedded host build: the unchanged
shutdown/process/exclusive-lock/destination guards ran again, and all backend/native source,
four published sidecar and companion-config fingerprints were checked against the successful first
publication before reusing it. No redundant backend publication or source changes were hidden.
`ARTIFACTS-first-candidate.json` retains the preceding candidate; `ARTIFACTS.json` is current.
The debug host was not rebuilt/presented as current; use the embedded release above, not its dev server.

Verified final embedded asset names: `index-CYrZPbgb.js`, `event-CctcAGfd.js`,
`index-CcdWqlOh.css`; release is newer than assets. Actual entry build-info9522 bytes/hash above is
byte-identical, unstaged/excluded. No Cargo manifest/generated-schema drift. The implementation-time
index was empty; the complete intended change is authorized for the local closeout commit below.
No installed deployment, agent desktop/live-source parity, native chooser/real Excel interaction or
exact owner-tested executable identity is claimed by these artifact checks.

`focused.diff` includes all **30 changed/new repository paths**, including all **10 new files** and
both generated contracts, excluding entry build-info. `focused-manifest.json`/`DIFF.json` retain hashes,
reverse-apply-check and tracked/new-file whitespace checks; the index was empty at that checkpoint. External Windows
backlog `C:\Dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md` updates existing
S22-011/033/035/037 dispositions only, with S22-018 cross-reference; no duplicate task or broad closure.
`backlog-update.diff` is the separate four-row external amendment, reconstructed from the exact
pre-edit rows retrieved during this checkpoint (not a claimed full-file entry snapshot), and
reverse-checks against the current external file without modifying it.

Synthetic numeric tests assert raw screen DTOs and actual workbook cells against independent literal
expectations in all three periods/both receipt modes: Past Due/overdue receipts not seeding QOH,
week/month boundaries, net-zero unconfirmed contributions, negative balances, planned due/release
distinct from receipts, full precision before KG/EA UOM rounding. Controllable-clock/ownership tests
exercise expiry, current business date, unknown/foreign/mismatched tokens, limits and no prepare reads.
Frontend/native tests cover constructed token forwarding, serialization/cancellation, rapid controls,
late replies, section isolation, validated preferences and exact-byte/uncertain-save/report switching.

Routine scoped security self-screen: main-window/managed loopback/session and fixed-route authority
are preserved; new fields are bounded/validated, token ownership is server checked, output is React
text, no source details/secrets are returned on error. Independent review is not claimed: no concrete
material issue requiring a separate review was established at this bounded source/offline-test depth.
This is not risk acceptance, exhaustive fault certification or live-source parity.

## Retained desktop checklist / evidence boundary

The following suggested checks are retained for traceability, not a new acceptance gate or a claim
that every item was performed. The explicit owner acceptance below supersedes pending-acceptance
wording. No agent live read or desktop interaction is claimed.

1. Lookup exact Part/Site; Enter, typo/missing part, comments/supplier details and all three tabs.
2. Change horizon/Day/Week/Month/Include while MRP loads; newest choice wins, facts remain usable.
   Full Refresh preserves controls; another Lookup resets; inventory refresh leaves MRP unchanged.
3. Scroll wide Day/72-week MRP horizontally and the page vertically; frozen labels, all five rows,
   light/dark and compact/comfortable/narrow widths. Grid and PO table remain available together.
4. Export the shown acquisition; compare Past Due, boundaries, quantities/rounding, negative QOH,
   planned orders and Include/exclude receipts with the workbook. Cancel Save As then Save again;
   check Component MRP label, open preference and unchanged other Reports.
5. After 30-minute acquisition expiry (or date rollover), new Export requires Refresh MRP and cannot
   silently reread. Previously prepared Save again still uses exact bytes subject to its own host
   lifetime. Refresh must visibly replace the projection before enabling a new export. Offline tests
    cover expiry/recovery; itemized owner interaction is not established.

Existing S22-011/037 carry only this bounded follow-up; S22-018 shared export adjustment cross-reference,
S22-033 modal/deep-link disposition still open. No duplicate tasks/broader closure/Stage 22 completion.
Implementation-time no-staging/commit statements are historical. The owner separately authorizes
one complete local closeout commit with normal hooks, not a push. No installation, deployment,
new dependencies/capabilities, live DB reads/changes or QXtend/Mass Updater submissions/imports.

## Owner acceptance / local commit closeout — 2026-10-21

Owner: **“The new MRP screen looks great. Accepted.”** This explicitly accepts the delivered Part Info
& MRP follow-up and supersedes earlier pending-acceptance wording; no further acceptance request is
required. Owner-reported acceptance is separate from offline verification and artifact inspection. It
does not establish that the owner tested the exact executable hash above, completed every suggested
desktop/workbook/native-save check, or accepted material security risk.

Closeout reuses the existing 252 frontend / 147 backend / 17 native passes and two intentional native
interactive ignores, with the worker/time bounds, earlier fixture failures and other limits retained
above. All 30 retained path fingerprints (including ten new files and both generated contracts) match
at closeout entry. No production/test/contract changes or repeat suites/builds solely to commit.
Inspect full/staged paths and contents, new files, generated contracts, whitespace, committed tree and
remaining status using normal hooks. Actual entry `tsconfig.tsbuildinfo` bytes remain untouched,
unstaged and excluded; local evidence, synthetic workbooks, logs and binaries remain outside Git.

Existing S22-011/037 and S22-035 close only the delivered combined-page/MRP-export portions;
S22-018 remains a shared-export cross-reference. Broader workstreams, S22-033 modal/deep links and
Stage 22 remain open. The ShortageMaster comment correction is deferred and unchanged.
Next checkpoint: separately scoped simulation-filter correction, not another acceptance gate.

## Subsequent BOM shortcut follow-up — 2026-10-21

The accepted combined page above remains accepted. The owner separately authorized BOM Show MRP,
one-use automatic lookup and bounded MPS investigation return; implementation/new-navigation review,
analytical dispositions/later triggers and evidence are recorded in
`docs/implementation/KST_v2_S22_BOM_SHOW_MRP.md`. This does not reopen page acceptance, change source
or report contracts, or close Stage 22. The owner subsequently confirmed “It looks good and it works.
Accepted.” on 2026-10-21; this bounded navigation/return follow-up is closed. Owner-reported acceptance
is separate from offline/artifact evidence, not itemized scenario or exact-hash confirmation. No
concrete unresolved Part Info concern is identified here and no generic page reacceptance gate remains;
analytical later destinations/triggers are retained in the BOM record.
