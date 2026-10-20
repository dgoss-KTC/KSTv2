# Current Project Status

Date: 2026-10-20
Workstation: Windows (`C:\Dev\kst_v2`)

## ShortageMaster latest-comment amendment — COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-20

Owner: “It works! I accept this implementation. Closeout authorized.” One complete local feature
commit is explicitly authorized, **not a push**. This is owner-run/reported application acceptance,
not agent-run live QAD/Shortages verification, exhaustive checklist completion, measured refresh
latency or risk acceptance. Closeout entry HEAD `214a45a204d1dfbc5a0b7e9d0fcf62a9f52de78f`;
accepted implementation/tests/contracts unchanged. Existing verification is reused with exact staged
paths/hunks/whitespace, normal hooks and post-commit tree/blob checks only. No repeated suites,
builds, AFT, independent review or sidecar publication solely for documentation closeout.

Owner confirmed `[Comment History]` after the initial metadata gate. Shared backend parsing now
selects only the first recognized history block from the same latest-active exact Site/Component
record, with nullable source wall-clock time/author; Current Comments remains the undated fallback.
Empty newest never resurrects older/fallback text. Component Orders preview/dialog, Workspace
Shortages Component Information and Part Info Lookup preview/dialog share metadata presentation.
Existing batching/cache lifetimes, source selection, locked algorithms and exports remain unchanged.

Offline **106 backend / 111 frontend tests pass**; generated contracts, backend/frontend builds,
sidecar publication and offline embedded review release pass. During implementation, no agent live
queries/app launch, new dependencies, subagent review, Git mutation or deployment. Known AFT deadlines, touched-file inherited
lint/format findings and Vite warnings remain explicit. Owner build-info bytes preserved.
Review `src/tauri/target/release/kst-tauri.exe`, alpha.21, **15,306,240 bytes**, SHA256
`F109709118FD00F295B73F6440088A91C8F2570ED698FA1B9E39C63BCACB871A`; all four sidecar copies match.
Parser-only long synthetic timings are recorded separately from **unmeasured** database-transfer
cost. Compact trace/implementation/security confirmations/limits/owner checklist:
`docs/implementation/KST_v2_SHORTAGEMASTER_LATEST_COMMENT.md`. Owner acceptance is recorded above;
live transfer cost and numerical ordinary-refresh timing remain unmeasured, not acceptance blockers.
The existing Windows backlog records the bounded comment amendment under S22-007 without closing
broader UI work/S22-037 or Stage 22. Owner build-info stays unstaged/excluded; full evidence/binaries
remain local outside Git. No push/deployment or new live query is authorized.

## S22-008 — Workspace Shortages touch-ups/export — COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-20

The owner reports reviewing the updated UI, testing export and accepting the presentation and shared
Export/protected Save As/recovery integration. One complete local checkpoint commit explicitly
authorized, **not a push**. Owner UI/export acceptance is not additional native fault-test, live-query,
external-submission, exhaustive checklist or risk-acceptance evidence. Accepted implementation/tests
and generated contracts match retained fingerprints and are unchanged; reuse existing verification
and scoped security review with exact staged-file/diff/whitespace, normal hooks and post-commit
checks only. No repeated suites/builds/review/publication solely for closeout.

Entry HEAD `e655aa4`. LongTermShortagesPanel (not work-order ShortagesPanel) now has grouped
view/projection controls, counts, accurate Week 1/negative-cell legend, severity pills, density/striping,
framed states and seven opaque frozen columns (KSS 58px). Existing projection/hooks/cache/source,
filter/sort/modal/workbook calculations unchanged. Typed cache-only protected export reuses the
accepted shared menu/preference/feedback/recovery, exports complete displayed order with loaded
projection identity, and uses a distinct fixed native kind with existing non-Analytical lifetimes.

Offline **98 frontend / 85 backend passed; native 16 passed / 2 intentional interactive ignores**.
Windows Edge matrix layout helper, types/scoped lint/format/builds, sidecar publication and embedded
release pass. Independent scoped security review found no confirmed material blocker at reviewed
depth. AFT deadlines, pre-existing broad native formatting and Vite warnings remain explicit.
Review release `src/tauri/target/release/kst-tauri.exe`, alpha.21, **15,306,240 bytes**, SHA256
`AC511C449AF8EA12BD399BA11C52594D0C864B185071287563486071B0EAE6DC`.
Final assets embedded; all four sidecar copies match; pre-entry build-info bytes preserved.
No agent live query/app launch, new dependencies/capabilities/commands, submission, push or deployment.
Pre-existing build-info bytes remain unstaged/excluded; evidence/binaries remain outside Git. Compact record,
decisions/limits/checklist: `docs/implementation/KST_v2_S22_008_WORKSPACE_SHORTAGES.md`;
full local evidence `C:\dev\kst_v2_tmp\Stage22\WorkspaceShortages\evidence\`.
Accepted API semantics: `docs/api/WORKSPACE_SHORTAGES.md`. Existing Windows backlog S22-008 closed;
shared workstreams, overall Stage 22, broader performance and unrelated deferred items remain open.
Closeout evidence is local under `evidence/commit-closeout/`. Further development requires separate dispatch.

## Reports redesign — COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-19

The owner confirmed visual, functional and native-save testing, accepted all six Reports views and
the scoped export/column/draft/paging work, and authorized one complete local commit, **not a push**.
This is owner-reported evidence, separate from agent checks; not new automated/native-suite,
agent live-query, exhaustive-fault, deployment or risk-acceptance evidence. Accepted sources/tests
are unchanged; closeout reuses existing verification with exact staged-path/diff/whitespace, normal
hook and post-commit checks only. No repeated suites, builds, AFT, security review or publication.

Single owner-dispatched checkpoint closes S22-011–015 for Reports and records accepted Reports/
shared-control portions of S22-003/018 and Part Info title/query/identity within S22-037 only.
No duplicate backlog tasks or closure of the broader S22-003/018/037 workstreams.
Grouped Reports shell/forms, shared typed report-kind-preserving export/recovery, Historical
draft/applied ownership and bounded 100/500/1000 paging are implemented. Header filters/sorting,
source/workbook algorithms, Part Info data region and locked Stages 9–11 remain unchanged.

Final offline verification: **187 frontend passed / 2 inherited skips; 132 backend passed;
16 native export tests passed / 2 intentional interactive ignores**. Types, scoped lint/C# format,
frontend/backend/native checks, sidecar publication and final embedded release build pass.
Independent separate-context security review + final delta found no confirmed new material blocker
at reviewed depth. AFT filesystem deadlines, an unmodified native format expression and existing
Vite warnings remain explicit. No agent live query/app launch, install, submission, push or deployment.
Local closeout staging/commit is authorized above. Pre-entry build-info bytes preserved/excluded;
generated contracts regenerated without content drift. No broader grant/dependency/capability change
or risk acceptance.

Accepted review host: `src/tauri/target/release/kst-tauri.exe`, **15,305,728 bytes**, SHA256
`81A8D79E4A6111E92CD7E0510DEBD80537CC627939824BB1F07AAA03F8B8953D`, alpha.21;
final assets embedded and all rebuilt sidecar copies match. Full identity, decisions, scopes, evidence,
known limits and compact owner checklist: `docs/implementation/KST_v2_S22_REPORTS_REDESIGN.md`.
Local full evidence: `C:\dev\kst_v2_tmp\Stage22\ReportsRedesign\evidence\`.
Windows backlog reconciles existing IDs without unrelated closure. Full closeout evidence remains
local under `evidence/commit-closeout/`. Next development requires separate dispatch; Historical
header filters/sorting, Part Info data region and overall Stage 22 are not marked complete.
Preceding accepted checkpoints below retain their original evidence.

## S22-009 — Finished Goods UI/header filters — COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-19

The owner reviewed the application and explicitly accepted styling, all five header filters,
full-report cascading counts, exact-decimal filtering and 100/500/1000 paging. This is owner-run
visual/interaction evidence, not additional agent desktop/live-query/native-suite/exhaustive-fault,
installer, deployment or risk-acceptance evidence. One complete local closeout commit is explicitly
authorized, **not a push**. Accepted implementation/tests match retained evidence and are unchanged;
existing verification is reused, with exact staged-path/diff/whitespace, normal hook and post-commit
checks only. No repeated suites/builds/AFT/security review or sidecar publish for closeout.

Windows entry HEAD `f83c2b5`. Accepted report frame/density/themes, five accessible header funnels,
unchanged shared editor/popover via local adapter, full-loaded-report AND predicates/cascading facets
and exact source-decimal conditions before rounding. Guarded Finished Goods-only 100/500/1000
preference (default 100), first-row-preserving resizing and valid reset/clamped/empty paging.
Acquisition/cache/stale/identity/obsolete protection, physical rows/order, USD valuation and warnings
unchanged; no backend/source/contract/shared-component/dependency/export/native authority change.

Offline **48 frontend tests passed / zero skips**, typecheck, changed-file lint, frontend build and
offline embedded release build pass. Initial AFT fresh/clean; final requests deadline-failed. Known
broad lint/format/Vite exceptions retained. Sidecars unchanged, no republish; owner build-info bytes
preserved unstaged/excluded. No agent live queries, app launch, installs, deployment or push.
Review release `src/tauri/target/release/kst-tauri.exe`: **15,302,656 bytes**, SHA256
`10B2A4458677DB70E8917BC23685549A00C9B3B88065CDA506DCCD0E8EE1270D` (final asset names embedded).
Scope/security screening/evidence/owner checklist: `docs/implementation/KST_v2_S22_009_FINISHED_GOODS_UI.md`;
full evidence `C:\dev\kst_v2_tmp\Stage22\FinishedGoods\evidence\` and `commit-closeout\`.
Local Windows S22-009 backlog entry is closed; evidence/binaries remain outside Git. S22-009 is closed;
Stage 22 remains open. Preceding accepted checkpoints below remain unchanged.

## S22-005 reopened header filtering + pagination — COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-19

The owner completed the final desktop checklist, confirmed everything works, explicitly accepted both
header filters/shared editor/exact values and pagination, and authorized one complete local commit,
**not a push**. This is owner-run/reported acceptance, not new agent desktop/native-test/live-query,
external QXtend, installer, deployment or risk-acceptance evidence. Original S22-005 acceptance and
the subsequent reopening are preserved below; accepted S22-010 commit `e3585f1` remains intact.
Authorized addition supersedes fixed 100: both modes offer 100/500/1000 (default 100), one guarded
Open Orders-only global preference. First-row-preserving resizing, selected-size Find row/hidden
counts, valid empty/clamped pages and unchanged all-population prepared exports are verified.
Final pagination scope: **168 frontend passed / 2 inherited skips**, types/lint/frontend/release build
pass; existing 96 backend/native/header-filter evidence reused, no sidecar publish or repeated security
review. Existing C# format/AFT exceptions remain; pagination production diagnostics fresh, final test
diagnostics partial. Current host **15,302,656 bytes**, SHA256
`B599F27E13005836148A47108B9A5E51781A84F11F601C356ECE2D37AA1D53C2`, alpha.21 at
`src/tauri/target/release/kst-tauri.exe`; verified final assets embedded. Supersedes the preceding
header-filter host identity below. Sidecars/owner build-info bytes unchanged. Header-filter verification
is **232 frontend passes / 2 inherited skips, 96 backend passes**, actual synthetic XLSX checks and
independent review. The **168 / 2 pagination** scope overlaps; do not add frontend counts. Closeout
reuses these checks without rerunning suites/builds/review or republishing the sidecar. Exact staged
diff/path/whitespace and post-commit checks honor normal hooks. Only accepted work/closeout is included;
original `tsconfig.tsbuildinfo` remains byte-identical, unstaged and excluded. No installs, live queries,
submissions, deployment or push. Evidence: prior root plus `pagination/`; checklist/closure and the local
Windows backlog update are recorded in S22-005 implementation record. **Stage 22 remains open**.

### Preceding header-filter implementation evidence (build identity superseded above)

Entry HEAD `e3585f141dd67137a757c36de25133a970c8cbc0`; original accepted S22-005 UI, lifecycle,
pointer and numeric-focus corrections remain. Header funnels/shared S22-010 editor replace chips;
Filters retains hidden/filter-only access and Product Line text Range. Full-loaded-report predicates
and cascading facets combine with seven-field quick search before unchanged sorting/100-row pages.
Plan filters use retrieved values, not proposals; all-workspace generation/drafts/epochs and analytical
save/recovery/eight-second feedback remain. A small DTO-only invariant numeric-text extension avoids
rounded numeric predicates; generated contracts synchronized. No source/query/calculation/locked-stage,
dependency/capability/native authority changes or new source reads.

Final offline: **232 frontend passed / 2 inherited skips**, **96 backend passed**; actual 205-/103-row
synthetic XLSX inspected, types/scoped lint/build pass. Independent separate-context review plus final
delta finds no confirmed new material blocker at reviewed depth. Scoped formatting fails and broader
AFT remains partial/deadline-limited; known exceptions are not passes. Full evidence/owner checklist:
`docs/implementation/KST_v2_S22_005_OPEN_ORDERS_UI.md` and
`C:\dev\kst_v2_tmp\Stage22\OpenOrdersHeaderFilters\evidence\`.

**Refreshed review executable:** `src/tauri/target/release/kst-tauri.exe` (alpha.21), 15,302,144 bytes,
SHA256 `1AC4BFB57FD5BA8FF62AD07DE4C13E46F22F8D3DE1392F02DF165137C5A6CEE7`.
Final frontend assets verified embedded. Published/binaries/release/**debug** sidecars now match:
127,524,006 bytes, SHA256 `DD92C835760B90D88EB5516E522B0EA15DDD53B663D326A4A09B4ABBCD209FD7`.
The prior S22-010 stale-debug-sidecar exception below is historical and resolved by this copy;
debug still uses the configured dev server, so review release. Source-disabled four-route probes pass;
no agent desktop UI/live source/native Save As/installer/deployment claim. At that handoff owner
acceptance was pending and the index was empty; the combined follow-up is now accepted above.
Owner build metadata remains preserved byte-for-byte and excluded. Stage 22 remains open.

## S22-010 — Workspace Shipments redesign/filtering amendment — COMPLETE / OWNER-ACCEPTED — 2026-10-19

The owner reviewed and accepted the rebuilt release application on 2026-10-19 and explicitly
authorized S22-010 closeout and one complete local checkpoint commit, not a push. This owner-run
acceptance is separate from retained offline tests and does not imply itemized checklist completion,
additional connected-source parity or exhaustive fault verification.

Starting HEAD `1edbebd`. Explicit owner-dispatched and now accepted amendment to
accepted Stage 19 Part 2 listing: full frozen-result typed header filters, cascading searchable bounded
facets, filtered On Time summary and 100/500/1000 Workspace pages. Original acquisition population,
queries, ordering/calculations, Reports 200 cap, operation ownership/expiry/workers and locked Stages
9–11 are unchanged. Exact Part/date acquisition scope stays distinct/visible; visit-local filters
survive Apply/Refresh, page-size preference alone may persist. No export/Open Orders migration.

Final offline scope: **82 backend passes**, **139 frontend passes / 3 inherited skips**; generated
contracts, type/build/scoped lint/format and release desktop/sidecar builds pass. Separate-context
independent security review plus final delta reports no confirmed new material blocker at reviewed
depth. High-cardinality synthetic evidence, source-disabled four-route startup probes and retained
exceptions/limits are in `docs/implementation/KST_v2_S22_WORKSPACE_SHIPMENTS_REDESIGN.md` and
`C:\Dev\kst_v2_tmp\Stage22\WorkspaceShipments\evidence\`. AFT TypeScript diagnostics remained partial;
known broad format/lint exceptions are not passing checks.

**Accepted review target:** `src/tauri/target/release/kst-tauri.exe` (rebuilt embedded frontend, alpha.21).
Publish/binaries/release sidecars match: 127,519,910 bytes, SHA256
`6CE2AAB8468E3809032130C2D640F9B2133980CDEC43C336E8BE311438F4BFFE`.
The running debug app locked its old sidecar, so debug check/copy failed with access denied and
**debug was not refreshed**; the app was left untouched. This remains an old-debug/new-release
distinction, not a closeout rebuild requirement. Owner desktop acceptance applies to release; no
agent-executed desktop UI/live query, installer or deployment is claimed. Retained resource limits
and unverified fault modes remain documented limitations. The complete accepted implementation,
shared controls, tests, contracts and closeout are authorized for the local commit; the pre-entry
modified `tsconfig.tsbuildinfo` remains preserved unstaged and excluded. The closeout changes only
documentation and reuses verification, with exact staged diff/path/whitespace and post-commit checks;
no full-suite/build/review rerun, dependency/capability changes, debug refresh or push.

**At the S22-010 closeout, S22-005 was reopened for a subsequent filtering update.** Its original
accepted implementation remained the baseline; the subsequent combined follow-up closure is recorded
above, not part of S22-010 acceptance. Stage 22 overall remains open.

## S22-006 — Component Orders performance diagnostics — COMPLETE / OWNER-ACCEPTED — 2026-10-19

The owner reports checking the timings, conducting a desktop review and explicitly accepting the
update on 2026-10-19. This is owner-run acceptance evidence, separate from the offline verification
below. No numerical live timings, sample counts or controlled speedup are inferred from that report.

2026-10-19, starting HEAD `3de4830`. Bounded instrumentation-only checkpoint adds correlated,
client-observed workspace/cache/BOM wait/read/dedup, PO connection/query-batch/mapping,
Shortages connection/comment/supplier/enrichment, grouping and DTO preparation timings. Conventional
PO and independent KSS remain one unchanged SQL statement; no isolated server execution time is
claimed. Existing cache/refresh, SQL, 250/1,000 batching, concurrency eight, locked algorithms,
API contracts, UI/export and security boundaries are unchanged. No demonstrated application
optimization was installed and no production speedup is claimed.

Final focused offline tests **120 passed** (8 Domain, 24 Application, 68 QAD/BOM/Shortages, 10 API,
10 architecture); isolated Release Application rerun **24 passed**. Generated 2,000-/40,000-line
profiles show application-only medians **2.110 / 51.241 ms**, not live-source measurements.
Changed-file format and scoped diagnostics pass. Independent security review's conditional timing-sink
failure finding was corrected, fault-injection tested and independently marked resolved at its
source/offline boundary; no confirmed new material blocker remains at reviewed depth.

Sidecar publication, offline native check/debug/release builds and final source-disabled copied-sidecar
startup/loopback probes pass. Four sidecar copies match (127,450,278 bytes; final hash in record).
No agent-run live reads, dependency installs, staging, commits, pushes or shared deployment.
Pre-entry `src/frontend/tsconfig.tsbuildinfo` change is preserved. Inherited format/lint exceptions
are not passes; inherited Fluent Assertions licensing notice remains a separate owner entitlement
item. S22-006 is complete and owner-accepted; Stage 22 remains open. This documentation-only
acceptance update reuses existing verification and does not authorize a commit, push or further tuning.

Subsequent owner instruction explicitly authorizes one local S22-006 checkpoint commit covering the
accepted implementation/tests and closeout documentation. Pre-entry frontend build metadata is excluded
and remains unstaged; local evidence/binaries are not included. Existing verification is reused; no
push, new implementation, live queries or shared deployment is authorized by this commit-only step.

Request trace, timing interpretation, verification limits and compact ordinary-app miss/hit/explicit
MPS-refresh procedure: `docs/implementation/KST_v2_S22_006_COMPONENT_ORDERS_PERFORMANCE.md`.
Full local evidence: `C:\Dev\kst_v2_tmp\Stage22\S22-006\`. PERF-001 still requires representative
shared QAD/MPS measurements and DBA-reviewed plan/index evidence before any SQL/batch/index tuning.

## S22-005 — Customer Open Orders redesign — COMPLETE / OWNER-ACCEPTED — 2026-10-19

The owner confirmed completion of `OWNER_REVIEW_CHECKLIST.md` (all 24 items checked) and explicitly
accepted all S22-005 session changes on 2026-10-19. Scope: Report/Plan presentation, seven-field
literal search AND existing filters, workspace/mode-local columns-only named views, protected view
switching, planning dock and actual shared analytical export/Save As/recovery/eight-second feedback
through a typed cached-report adapter. Operational fresh validation and per-file CSV saving remain
separate. No source algorithms, native capabilities or dependencies changed.

Accepted corrections: monotonic operational context epoch prevents Plan→Report→Plan revival;
Windows header/list pointer dragging preserves native file-drop configuration and keyboard ordering;
numeric focus removes quantity padding and starts Price at four visual places without changing
exact source/proposal values or triggering focus-only draft saves.

Chronology: initial 2026-10-16 frontend evidence was 206 passes / 2 inherited skips; the 2026-10-19
pointer correction had 221 / 2; latest numeric-focus scope is **223 passed / 2 inherited skips**.
Original backend/architecture: **82 passed**. Native: **24 passed / 2 intentional interactive ignores**.
Backend/native suites were not rerun for frontend-only corrections or this documentation/commit
closeout. Actual synthetic XLSX checked; generated contracts synchronized; changed-file lint/type/
build and local sidecar/embedded release builds pass in retained evidence, not newly rerun here.
Full lint's two unchanged locked Stage 10 errors, touched-file formatting failure and partial AFT
diagnostics remain explicit exceptions, not passes. Independent security-specific review has no
confirmed material blocker remaining; its low lifecycle finding was reproduced and resolved.

Closeout full staged whitespace check reports one blank line at EOF in the unchanged accepted
`src/frontend/src/openOrders/useColumnDrag.ts:83` (exit 2); it is not a pass. No source formatting
cleanup is included and no Git hook is bypassed or weakened.

See `docs/implementation/KST_v2_S22_005_OPEN_ORDERS_UI.md` and
`C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\evidence\` (owner checklist, review, logs, synthetic workbook).
Latest frontend/release identities are in `number-format-fix\`; earlier artifact/pending-review
records remain historical. Owner checklist completion is owner-run evidence, not agent-executed
desktop testing, proof of external QXtend acceptance/submission or installer/release certification.

The owner separately authorized closeout documentation and one complete local S22-005 commit;
accepted implementation is unchanged and existing verification is reused. No push, new development,
live agent-run source queries, installs or shared deployment is authorized/performed by this closeout.
Large evidence and build artifacts stay outside Git. **Stage 22 remains open.** Prior checkpoint
authorization below is historical and does not broaden this bounded commit.

## Accumulated-work checkpoint for owner testing (2026-10-16)

The owner authorizes one local checkpoint commit of all accumulated repository work after S22-034
commit `86fd0cb6db6366f63c8dcc6130bc4de2760b64e8`; no push is authorized. This records the current
implementation, not acceptance of pending features or Stage 22 completion. Included areas are Reports
Part Info and its backend/API/generated contracts/tests, MPS navigation/header/detail scrolling,
MPS/BOM export and eight-second feedback, supporting native grant handling, and retained Stage 19
discovery/legacy source references. The already-tracked `src/frontend/tsconfig.tsbuildinfo` is included
unchanged from checkpoint entry under an explicit owner-approved one-time build-metadata exception;
ignore rules are unchanged and ignored build/evidence/private artifacts are not included.

The prior closeout records owner acceptance of MPS/export and the eight-second correction at its
reported scope. It does not establish an exhaustive desktop/recovery matrix. Part Info, the broader
drill-down checkpoint and detail-scroll confirmation remain pending as documented below. Earlier
implementation-time pending/no-commit statements and artifact identities are historical; S22-034's
accepted remediation and latest publication evidence supersede earlier Pkcs advisory/publication state.
Known formatting/lint exceptions remain exceptions, not passes or newly accepted risk.

Checkpoint whitespace inspection also reports 377 trailing-whitespace findings confined to the
newly tracked historical reference `docs/reference/ktmg1160.p`. Its existing source formatting is
preserved rather than cleaned up in this commit-only task. The staged whitespace check for all other
paths passes; the complete staged check is not claimed as passing.

Existing offline verification and independent-review evidence below is reused, not rerun for this
checkpoint. Commit creation is not proof that the application works. The next application tests and
remaining visual/native/source validation are owner-run; no new acceptance decision is made here.

## S22-034 — Pkcs remediation complete / owner-accepted / closed (2026-10-16)

**COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-16.** The owner reports everything is working fine and
explicitly accepts S22-034; this is owner-reported desktop acceptance, not detailed authentication or
live-query evidence. Implementation/offline/payload verification and independent review are retained.
Original Pkcs 9.0.21 attempt corrected the NuGet graph but lost to affected framework Pkcs 10.0.12.
The owner authorized exact **10.0.13** evaluation; normal-path admission and the same single central
transitive pin now select patched Pkcs **10.0.13** in both the graph and actual published bundle.
API → QAD/Shortages → SqlClient **6.1.1** remains the dependency chain. SDK **10.0.401**, net10.0,
runtime packs **10.0.12**, SNI **6.0.2**, all other dependencies and security boundaries are unchanged.

Restore/build/publication pass without dependency warnings; **1,371 offline backend tests pass**.
All three NuGet transitive audits are clean. Direct bundle inspection recovers one Pkcs payload,
product **10.0.13**, matching the admitted package's Windows DLL SHA256; embedded dependency ownership and
admitted Syft inventory corroborate it. Both reported advisories are outside this effective version.
Independent publication-level review resolves **S22-034-ISR-001** for the actual local publication and
four matching copies, with **no confirmed blocking finding remaining**. This is remediation, not risk
acceptance or general framework/ecosystem vulnerability clearance.

Established sidecar publish and offline Tauri debug/release builds refresh all four copies:
**127,425,702 bytes**, SHA256 `CC76D047E5A403EE7C194CC204F16606195D15C827D25B1A2CAC9AC7147F038E`.
Native SNI payload is byte-identical to entry; self-contained win-x64/single-file/native extraction
settings remain. Source-disabled copied-sidecar handshake/identity, loopback-only listener,
health/readiness/static spec probe passes; no source reads or live authentication check occurred.
Full admission, graph/payload identities, review and logs:
`docs/security/S22_034_PKCS_DEPENDENCY_REMEDIATION.md`, `C:\Dev\kst_v2_tmp\Stage22\S22-034\`.
The owner authorizes a focused remediation/closeout commit; no push, shared deployment or installer
distribution is authorized. Existing verification is reused because the reviewed implementation and
artifact identities are unchanged. Format/lint exceptions and eventual fleet/installer notice
reconciliation remain explicit limits. **Closing S22-034 does not close Stage 22.**

All pre-existing work is preserved, including pending Part Info. The current owner dispatch confirms
acceptance of the export pilot and eight-second correction; this does not accept Part Info, the
whole drill-down checkpoint or imply previously unexecuted checks were performed. S22-034's separate
owner acceptance is recorded above.

## Stage 22 — MPS/BOM export pilot (2026-10-15)

**IMPLEMENTED / OFFLINE-VERIFIED — OWNER DESKTOP ACCEPTANCE PENDING.** Separately owner-authorized after the first
drill-down checkpoint: themed caption menus, explicit all-parent MPS/filtered selected-parent BOM
scope, prepare → automatic protected Save As, exact-workbook cancellation retry, persistent session
status/Open again and observation-only uncertain-save recovery. Only analytical saved-artifact grants
gain departure retention; existing host authority, finite limits and other report behavior remain.
No workbook, source, business-rule, API contract, dependency/capability or operational CSV changes.

Implementation, scoped independent security review, final verification evidence and owner checks:
`docs/implementation/KST_v2_STAGE_22_MPS_BOM_EXPORT_PILOT.md` and
`C:\Dev\kst_v2_tmp\Stage22\ExportPilot\`. The owner additionally authorized tests/sidecar rebuild
after closing the app. Native/visual desktop verification remains owner-run; Stage 22, pending Part
Info and first drill-down checkpoint acceptance are not implied. Version remains `0.1.0-alpha.21`.
Final scoped evidence: 269 frontend passes / 3 inherited skips, 13 workbook tests, 24 native passes /
2 existing ignores; frontend type/build/scoped lint and native check/build pass. Sidecar publication
and copied binary identity are verified. Independent security review plus the theme-portal follow-up
reports no confirmed new material defect; retained exceptions/advisories are not risk-accepted.

Owner review and bounded dismissal amendment (2026-10-15): **appearance and normal save workflow
accepted by the owner**; other pilot/recovery, drill-down and Part Info acceptance is not implied.
Ordinary final successful-save feedback now hides after eight seconds only once requested opening
has settled; keyboard focus defers hiding. Recovery actions and unresolved warnings/errors remain
persistent, and dismissal retains recorded outcomes and native grants. Focused frontend verification:
53 export tests pass (17 new fake-clock cases), typecheck and changed-file lint pass. No native/backend
build or sidecar publication was performed for this amendment. New desktop timing/focus checks remain
pending; see the pilot record's owner-review amendment and `feedback-dismissal.diff`/test JSON.

## Stage 22 — first MPS drill-down redesign checkpoint (2026-10-15)

**IMPLEMENTED / OFFLINE-VERIFIED — OWNER DESKTOP ACCEPTANCE PENDING.** Stage 22 is now in progress,
not complete or accepted. Verified starting HEAD was accepted Stage 21 `1831fb7`; all pre-existing
uncommitted Reports Part Info Lookup work remains pending its separate owner acceptance.

Frontend-only: one Back/Escape/breadcrumb hierarchy, delayed nonempty-only bucket entry with
bounded loading/error/empty feedback and obsolete-response protection, replacement candidate levels
through the accepted depth 3, whole-card keyboard material toggles, All/Needs attention controls,
plain-language availability labels, Shortages-tab removal with embedded analysis/details preserved,
parent-scoped Part Info/BOM, modal precedence/focus and valid return-state/scroll preservation.
Existing API populations, locked calculations, export/save code, backend/native boundaries and
application version (`0.1.0-alpha.21`) are unchanged. No live queries, installs, commits or pushes.

Implementation, verification limits, security-trigger screening and owner desktop checklist:
`docs/implementation/KST_v2_STAGE_22_MPS_DRILLDOWN_CHECKPOINT.md`.
Offline evidence: `C:\Dev\kst_v2_tmp\Stage22\frontend-final.json` and associated scoped diff/logs.
Actual theme/density/narrow-window and Windows/webview interaction acceptance remains owner-run;
no applicable existing MPS browser fixture was available and no new harness was introduced.
The separately authorized MPS/BOM export pilot is now recorded above; this first checkpoint's
frontend-only/export-unchanged description remains historical to its own delivered scope.

Owner-review correction (2026-10-15): the complete three-row MPS header is now pinned as one group,
rather than letting quarter/period/week cells overlap at `top: 0`. Frozen parent/Falldown columns
remain. Owner confirmed that header scrolling works; that confirmation does not accept the whole
checkpoint. Follow-up detail-scroll correction keeps the selected MPS row/trail/tabs outside bounded
content viewports below the tabs. Focused tests (179 passes, 3 inherited skips), build/type compilation
and scoped lint pass. Actual owner confirmation of the new detail scroll boundary remains pending;
see the Stage 22 checkpoint document.

## Reports Part Info Lookup — implemented / pending owner acceptance (2026-10-14)

Owner explicitly dispatched this bounded checkpoint before Stage 22. Reports now exposes an exact
part + supported-site page with Enter submission, compact facts and full-width table tabs, inventory/lot rows,
Approved Alternates, supplier-specific Credit Hold/CIA, independent buyer comments and
conventional open POs. No workspace, MPS, BOM expansion or shortage projection is required. No export,
native save/open, shortage snapshot or Past region is introduced. Version remains `0.1.0-alpha.21`.
At this lookup checkpoint Stage 22 was unstarted; the later bounded MPS checkpoint is recorded above.
Implementation is not acceptance or release readiness.

Owner visual-review correction (2026-10-14): Part Info Lookup typography reduced to a 13px base
with proportionally sized headings, controls and secondary text. Inventory/lot quantities trim
insignificant trailing decimal zeros using exact-decimal text (whole numbers have no decimal point;
meaningful fractional digits and missing-versus-zero remain intact). Visual re-review is pending.

Owner-approved layout replacement (2026-10-14): translated the local `kst_part_lookup.html` reference
into KST theme/density-aware presentation: six-field fact bar, More part details disclosure for all
remaining approved fields, persistent supplier-risk and buyer-comment strips, and Open POs (default),
Inventory lots and Approved alternates full-width tabs. Supplier summary names are exact source
identifiers; None requires explicit No for every covered supplier, with Unknown/Unavailable retained.
The full supplier flags remain in a disclosure; AVL retains accepted row relationships without inferred
risk joins. PO buyer/manufacturer values remain per row; inventory statuses are not merged or reclassified.
Buyer comments open a keyboard-contained dialog with Escape/backdrop/Close dismissal and focus return.
Tabs support arrows/Home/End and selected-panel semantics. No prototype CDN/icon/font dependencies or
clipboard action were introduced. Existing identity/abort logic and independent inventory refresh remain.

Layout-only verification: **32 tests passed** (9 lookup + 23 Reports), changed-scope ESLint, TypeScript
and production frontend build passed; AFT scoped diagnostics reported no errors/warnings. Build retained
existing chunk/dynamic-import warnings. Owner tsconfig build-info restored byte-for-byte. Evidence:
`C:\Dev\kst_v2_tmp\PartInfoLookup\frontend-tabbed-tests.json`. Backend/sidecar are unchanged by this
presentation revision; prior published-sidecar evidence still applies. Actual dark/light, density and
narrow-window visual review remains owner-run/pending; use all three tabs, More part details, supplier
details and the buyer-comment dialog when rechecking. Stage 22 and owner acceptance remain pending.

Implementation/source decisions:
- New `Kst.Application.PartInfo.PartInfoService` composes existing scoped master/cost/planning,
  inventory summary, locations, AVL, PO and enrichment readers. Existing workspace endpoints and
  freshness restrictions are unchanged. New API-owned DTOs/spec flow through OpenAPI and generated
  TypeScript; decimal quantities/costs are strings here to preserve precision and null-versus-zero.
- Supported sites SW/NW/AR/MN/MS/KV use existing Reports validation; domain is derived only inside
  QAD through `QadSiteDomainMap`. Part input is nonblank, bounded to 256 characters, rejects control
  characters, and is trimmed for exact parameterized lookup. No arbitrary domain/database is accepted.
- `QadPartInfoContextReader` reads only the retained informational subset of accepted Stage 11 SQL:
  manufacturer item = `pod_vpart` from first positive-open, confirmed, nonclosed/noncancelled line,
  ordered due date/PO/line; effective P/M = nonblank selected-site `ptp_pm_code` then master
  `pt_pm_code`; manufacturing lead = selected-site `ptp_mfg_lead`, displayed only for M. No full
  projection/MRP acquisition or locked-algorithm amendment. This manufacturer selection is separate
  from the accepted conventional-open-PO population and is not inferred from that list.
- Risk identity is the existing `QadComponentOrderReader.SupplierIdentifier`: normalized `vd_sort`,
  fallback vendor code, matched to Shortages PreferredSuppliers exact source name. It is **not** a
  newly assumed vendor-code join. Only suppliers evidenced by this part's qualifying POs are requested;
  ordinal grouping and result matching preserve different exact keys, Yes/No/Unknown. No match is not
  No; failed enrichment is explicitly unavailable. Comments use their independent part/site request,
  including when no POs exist. AVL does not broaden risk coverage.
- Optional sections acquire independently and never turn failures into successful empty lists.
  Master/planning/costs retain the existing composite reader as one prerequisite; their source failure
  fails the top-level lookup. Missing master is distinct from acquisition failure. No server cache or
  stale fallback is introduced. `inventory-locations` supports independent refresh without erasing other
  sections; failed refresh removes old locations. Full refresh reloads all sections. Frontend epochs,
  aborts, response identity checks and report departure prevent obsolete/incompatible publication.

Verification and evidence (offline unless explicitly stated):
- Full backend suite: **1,363 passed** (358 Domain, 412 Application, 331 QAD, 10 architecture,
  252 API). After final independent-refresh/cancellation additions: focused **29 passed** (7 Application,
  10 API, 2 QAD, 10 architecture); zero-test Domain filter is not counted. Synthetic fixtures specify
  true/false/unknown exact supplier results, absent values/zero, precision, missing master, invalid
  input, no workspace/MPS, empty collections, independent failures and cancellation.
- Final focused frontend: **84 passed / four files**, including lookup, Reports, shared modal and
  inventory panel regressions. Input/Enter, disclosure, missing/error/empty states, independent refresh,
  late replies, changed identity, cancellation and departure are exercised. Production frontend build,
  typecheck, changed-scope ESLint and new-file scoped `dotnet format --verify-no-changes` pass.
- Full solution format **FAILS** on inherited unmodified whitespace/end-of-line issues. Full lint
  **FAILS** on the two locked Stage 10 setState-in-effect errors. `cargo fmt --check` **FAILS** on
  pre-existing Rust formatting. These are exceptions, not passes. A new lookup lint warning and a test
  clock compile failure were corrected, then affected checks passed. AFT diagnostics remained PARTIAL;
  successful compiler/build/test evidence is the verification authority. No Rust code changed.
- Separate-context independent security review and final-refresh follow-up found **no confirmed new
  security defect** at reviewed depth. Existing Pkcs 9.0.4 moderate/high advisories remain unresolved,
  not risk-accepted. No dependency/capability, DB object/write, connection/credential/CORS/CSP change.
- `scripts/build-sidecar.ps1` successfully published/copied the final 127,855,236-byte sidecar;
  published/copy SHA256 `392C7D7550DD69473B95FB899760348ACDEBF2AAD3AC6031A0A3946E9B4E4CF8`.
  Copied-sidecar startup verified handshake/process identity, 127.0.0.1-only listener, health/readiness
  and the static lookup spec with QAD disabled. Only those three non-acquisition routes were called.
  No actual desktop UI startup/lookup, live QAD/Shortages read, visual acceptance or parity is claimed.
- Local evidence: `C:\Dev\kst_v2_tmp\PartInfoLookup\` — `backend\` and `backend-final\` TRX,
  `frontend-tests.json`, `independent-security-review.md` (with final addendum), format/lint logs,
  `sidecar-identity.json`, `sidecar-startup.json/.log`, startup verifier and focused diff/new-file evidence.
  `build-backend.log` records the superseded compile failure, not final build success.
- Pre-existing owner `tsconfig.tsbuildinfo` bytes were restored after frontend build (SHA256
  `9218A9C78BC13B1885ADC9197E269C9A717D608DD874015FD3BD5326E841732D`); untracked Stage 19
  discovery and `ktmg1160.p` preserved. Nothing staged, committed or pushed.

Owner-run acceptance checklist — **not executed**; live reads require owner authorization:
1. Restart the desktop using the refreshed sidecar. With no workspace/MPS loaded, open Reports →
   Part Info Lookup. Look up an owner-selected real part/site using Enter; confirm identity/description.
2. Compare retained inventory/cost/planning/manufacturer/AVL/PO values against accepted source evidence.
   Confirm four-decimal costs, missing versus zero, long identifiers, multiline comments and exact quantities.
3. Check a part with multiple PO suppliers/different flags and unmatched supplier; verify exact name
   identities and Unknown. Check no-PO part still retrieves a buyer comment and explains no risk coverage.
4. Change site/part rapidly while loading; refresh/retry inventory independently; switch reports and
   return. Confirm no old identity or obsolete rows reappear. Exercise real unavailable/empty/missing states
   where safely available; do not change databases to create them.
5. Check light/dark, compact/comfortable, narrow window stacking, Tab/focus, Enter, alternate disclosure
   and keyboard table scrolling. Confirm no shortage/Past fields or export/save/open controls. Spot-check
   existing Reports, BOM/shortage popups and Component Orders. Record acceptance or specific corrections.

## Accepted stage baseline

Stage 21 — **COMPLETE / OWNER-ACCEPTED — 2026-10-14.**
MPS and selected-parent BOM XLSX actions use loaded data and the accepted host-owned Reports
save boundary. Bounded inventory, unresolved dispositions, verification limits and owner acceptance are recorded in
`docs/implementation/KST_v2_STAGE_21_EXPORT_INVENTORY_AND_EVIDENCE.md`. Finished Goods explicitly needs
no export; copying from the report screen is sufficient. No subsequent stage or release readiness is implied. Current application
version is `0.1.0-alpha.21`; numeric installer remains `0.1.0`. Sidecar publication exposed existing Pkcs
9.0.4 moderate/high NuGet advisories; security disposition remains unresolved and is not risk-accepted
by Stage 21 acceptance. Owner removed Planning analytical Excel export on 2026-10-14 for a complete
redesign in future iterative improvements, **not Stage 22**; the
accepted Planning Mass Updater CSV remains available. BOM Level is left-aligned and whole quantities
display without trailing decimal points.
Owner reports checking the reports and working through fixes with the coding agent, and explicitly
accepts Stage 21. This report is separate from synthetic/mocked automation; no exhaustive desktop
interaction matrix, live parity or numerical coverage is inferred. Export-button placement/presentation
is an acknowledged usability limitation deferred to later optimization. Unresolved inventory decisions,
the Stage 11/13 browser-save feedback discrepancy, verification exceptions and inherited host/save limits
remain open as separately tracked items. This documentation-only closeout authorizes no later work.

**Stage 19 Historical Shipments — COMPLETE / OWNER-ACCEPTED — 2026-10-13.** Part 1
Historical Shipments in Reports and Part 2 Workspace Shipments are both accepted. See
`docs/implementation/KST_v2_STAGE_19_CLOSEOUT.md` for owner evidence, scoped offline verification,
the final sidecar identity and retained limitations. Detailed implementation evidence remains in the
Part 1/Part 2 foundation records; their initial phase-status text is historical and superseded by this
closeout.
Workspace Shipments resolves current assignment membership as discovered product-line parents UNION
explicitly configured parents, with mandatory workspace site/domain scope and no MPS snapshot dependency.
It intersects optional exact Part Number and inclusive Ship Date bounds. Its full-result On Time Rate
counts each physical detail with On Time 1 over eligible 1-or-0 rows; unavailable values are excluded.
See `docs/api/HISTORICAL_SHIPMENTS.md` and `docs/api/WORKSPACE_SHIPMENTS.md`.
Stage 18 acceptance and limitations stand; Stage 19 closeout made no live database queries.
Current: **Stage 18.4 Shipments-To-Go COMPLETE / OWNER-ACCEPTED — closeout 2026-10-12**.
Python authority, KS/KTS summary removal, multisite defaults and report-departure discard/recovery
are recorded in `docs/implementation/KST_v2_STAGE_18_STG_IMPLEMENTATION.md`.
Owner ran STG and reported identical legacy output; comparison completed. Separate-context F1 review
resolved the finding; owner confirmed acceptance and local-only commit authorization. See
`docs/implementation/KST_v2_STAGE_18_STG_CLOSEOUT.md` for evidence/limits.
Stage 18.5 S&OP COMPLETE / OWNER-ACCEPTED — closeout 2026-10-12. Owner reports generated workbook
good and accepted S&OP overall; reviewed/accepted the corrected Product lines form. Closeout and
evidence limits are in `docs/implementation/KST_v2_STAGE_18_SOP_CLOSEOUT.md`.
Open Order Report Stage 18.6 is **COMPLETE / OWNER-ACCEPTED — closeout 2026-10-12**. Sol's implementation
is retained with Sonnet's accepted presentation adaptation. Owner accepted UI/output and observed export/
saving in tested cases. Final focused review established no bounded blocking defect; shared SQL guard
fixture gap resolved (self-reviewed correction; retained 7 passing tests, exit 0). See
`docs/implementation/KST_v2_STAGE_18_OOR_CLOSEOUT.md` for separately recorded offline evidence, owner
observations and unperformed exhaustive live parity/printing/full desktop checks. Historical checklist
18.3 corresponds to owner designation 18.6. Shared 18.1 definitions are reconciled against actual uses.
Owner confirmed the S&OP legacy-workbook comparison was already performed; no repeat comparison was
required. The statement records no invented date, inputs, counts or numerical scope. Stage 18 is
COMPLETE / OWNER-ACCEPTED as of 2026-10-13; retained verification limits remain in the overall closeout.
Owner: “I ran the app and generated a few reports. Saving works well now.” Generation/saving
observations do not imply every desktop, printing or numerical check was performed. See
`docs/implementation/KST_v2_STAGE_18_COMPONENT_MRP_CLOSEOUT.md` for acceptance and evidence limits.
Direct-part report/workbook/UI and host-owned Save As, remembered
folder, optional opening and status reconciliation implemented. Owner removed continuous preventive
namespace binding prerequisite; deliberate local-program folder/link manipulation documented limitation.
I3 adds five-row customer export, default included unconfirmed POs/amber, Day/Week/calendarMonth
and host-confirmed overwrite. See `docs/implementation/KST_v2_STAGE_18_I3_IMPLEMENTATION.md`.
I3 review R1–R3 lifecycle and R4 print corrections are recorded in
`docs/implementation/KST_v2_STAGE_18_I31_IMPLEMENTATION.md`; combined review resolved original
R1/R2/R4 at its evidenced depth and supported I3.3's chooser.
See `docs/implementation/KST_v2_STAGE_18_I32_IMPLEMENTATION.md` for D31–D33 and the installed
chooser's historical path-only gap. `docs/implementation/KST_v2_STAGE_18_I33_IMPLEMENTATION.md`
records the host native candidate-bound correction and isolated Windows interaction evidence.
Combined I3.1–I3.3 review resolved original R1/R2/R4 at evidenced depth and supported I3.3;
F1 stale successful replies blocked readiness. Correction/evidence are in
`docs/implementation/KST_v2_STAGE_18_I34_IMPLEMENTATION.md`; independent F1 review adjudicated
Resolved; Component MRP is owner-accepted and Stage 18 overall is closed with the retained limits in
the overall closeout.
Application was `0.1.0-alpha.19` at Stage 19 closeout; numeric installer `0.1.0` is unchanged. Stage 19 completion is not release readiness and does not imply exhaustive live parity, printing or full desktop verification. See
`docs/implementation/KST_v2_STAGE_18_I2F_FORMATTING.md` for the owner-authorized compact workbook
presentation history/synthetic previews; current acceptance/evidence limits are in the closeout.
`docs/implementation/KST_v2_STAGE_18_I2_IMPLEMENTATION.md` for scope, synthetic evidence and limitations.
Current implementation position: **Stages 1–11 and 13 COMPLETE / ACCEPTED; Stages 9–11 LOCKED; Stage 12 RETIRED.**
The revised Release 1 roadmap is owner-approved. **Stage 13 — Customer Open Orders** has an
owner-accepted bounded plan; checkpoints 13.1–13.5 are implemented and owner-accepted (13.5
commit `5ea47da`). **Checkpoint 13.6 and Stage 13 are COMPLETE / OWNER-ACCEPTED — 2026-10-01**;
bounded live results, owner-reported external outcomes and accepted verification exceptions are
documented in `docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md`. **Stage 14 — Planning Workbook:
Checkpoint 14.1 (decision documentation and synthetic fixtures) COMPLETE / OWNER-ACCEPTED —
2026-10-02** (`docs/implementation/KST_v2_STAGE_14_SOURCE_AND_DECISION_LEDGER.md`). **Checkpoint
14.2 (pure Domain calculation core and fixture-driven tests) COMPLETE / OWNER-ACCEPTED —
2026-10-02**; the application version is `0.1.0-alpha.14`. **Checkpoint 14.3 (read-only
adapters, workbook composition and workbook API) COMPLETE / OWNER-ACCEPTED — 2026-10-02**.
**Checkpoint 14.4 (read-only Planning Workbook view and fiscal-quarter summaries) IMPLEMENTED /
OWNER-ACCEPTED with zero-value display amendment — 2026-10-02**. **Checkpoint 14.5 (Planning
adjustment staging and saved drafts) — OWNER-ACCEPTED with Master plan redesign and presentation
amendments — 2026-10-06** (owner review; at that checkpoint's close, automated evidence was offline/mocked
and later live-source/desktop confirmations remained pending); **Checkpoint 14.6 (Mass Updater CSV export) — OWNER-ACCEPTED —
2026-10-07** (owner review completed after the 14.6 frontend corrections and in-app export testing;
offline/mocked automation evidence remains separate from owner testing; desktop/sidecar integrated
verification and closeout are 14.7 scope). **Checkpoint 14.7 (integrated verification, sidecar
refresh and closeout) — OWNER-ACCEPTED — 2026-10-07; Stage 14 overall — COMPLETE / OWNER-ACCEPTED —
2026-10-07.** Owner evidence: sidecar rebuilt and desktop behavior verified; the owner confirmed the
desktop checklist covering close-warning, case-insensitive RA exclusion and repaired Planning/Open
Orders confirmations. This owner evidence is separate from automated verification in the Stage 14 ledger.
UI Navigation & Keyboard Ergonomics A: **COMPLETE / ACCEPTED — 2026-08-21**
Active cross-cutting effort: **R0 — Repository / Documentation Reconciliation — COMPLETE /
ACCEPTED — 2026-08-21** (see `R0 — Repository / Documentation Reconciliation Status` below and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`)
Current cross-cutting effort: **S0 — Security Foundation Integration — COMPLETE / ACCEPTED — 2026-08-31** (S0.1 — Security
Policy Injection — **COMPLETE / ACCEPTED — 2026-08-21**; S0.2 — Security Baseline Discovery —
**COMPLETE / ACCEPTED — 2026-08-24**; S0.3 — Existing-Tool Security Checks — **COMPLETE /
ACCEPTED — 2026-08-24**; see `SECURITY.md` and `docs/security/`)
Current: **S0.4 — Security Finding Disposition & Bounded Remediation — COMPLETE / ACCEPTED —
2026-08-25** (S0.4A — QAD SQL Transport Correction — **COMPLETE / ACCEPTED — 2026-08-25**; S0.4B —
Tauri Shell Capability — **COMPLETE / ACCEPTED — 2026-08-25**; S0.4C — npm Development-Tooling
Advisories — **COMPLETE / ACCEPTED — 2026-08-25**; see
docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md,
docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md,
docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md and "Remaining S0 Work" below)
Current: **S0.6 — Security Tool Admission — COMPLETE / ACCEPTED — 2026-08-27** (Capability Review 1 — Rust
Dependency Advisory Capability, gap `S0.3-G001` — **COMPLETE / ACCEPTED — 2026-08-26**:
cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED; S0.3-G001 — Covered /
Resolved — see
docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md; Capability Review 2 — Dedicated Secret
Scanning, gap `S0.3-G007` — **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks
v8.30.0 installed, release-integrity and synthetic-canary verified, scanned current KST content
(4 findings) and full Git history (8 findings), all rule `private-key`, confirmed documentation
false positives; `S0.3-G007` — Covered / Resolved) — research at
docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md, owner decision and implementation
evidence at docs/security/S0_6_SECRET_SCANNING_ADMISSION.md (Gitleaks v8.30.1, TruffleHog
v3.97.1, detect-secrets v1.5.0 DEFERRED); Capability Review 3 — Software Bill of Materials
(SBOM), gap `S0.3-G008` — **COMPLETE / ACCEPTED — 2026-08-27** —
Anchore Syft v1.51.1 installed and release-integrity verified, run against KST build/repository
evidence (SPDX 2.3, 1,027 packages; CycloneDX 1.6, 1,026 components) and a complementary
packaged-artifact view (published `Kst.Api` sidecar, 37 NuGet packages recovered directly from
the single-file executable) — research at docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md (neutral
research packet, not a recommendation or admission decision) and
docs/security/S0_6_SBOM_ADMISSION.md (owner decision and implementation evidence; Anchore Syft
v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED; Microsoft sbom-tool v4.1.5 and the CycloneDX
ecosystem-native approach DEFERRED; six informational findings `S0.6-F014`–`S0.6-F019`, none
blocking; complete Tauri Windows installer/application bundle Unable to Verify / future
packaged-release verification boundary, not Accepted Risk); `S0.3-G008` — Covered / Resolved;
Capability Review 4 — Dedicated Static Application Security Testing (SAST), gap `S0.3-G006` —
**COMPLETE / ACCEPTED — 2026-08-27** — owner reviewed the neutral research packet
comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 and admitted
DevSkim CLI v1.0.90, which was installed, self-verified, synthetically validated, and
run against the KST source tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed
2026-08-27 and reclassified to Informational / Framework-Local Origin / Confirmed DevSkim False
Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule
Limitation; neither Accepted Risk); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED (not
rejected) pending organizational licensing/entitlement review — see
docs/security/S0_6_SAST_ADMISSION_RESEARCH.md and
docs/security/S0_6_SAST_ADMISSION.md; `S0.3-G006` — Covered / Resolved; Microsoft DevSkim CLI
v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED. With `S0.3-G001`, `S0.3-G006`, `S0.3-G007`,
and `S0.3-G008` all Covered / Resolved, **S0.6 — Security Tool Admission is COMPLETE / ACCEPTED —
2026-08-27**; S0.7 —
Runtime & Infrastructure Verification
(**COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification working pass —
**COMPLETE / ACCEPTED — 2026-08-28**: 2026-08-27 evidence pass VALID /
ACCEPTED AS EVIDENCE by owner review, and 2026-08-28 `S0.5-F001` loopback-binding remediation
implemented + release-runtime re-verified — `S0.3-G009` Covered / Resolved; S0.7B — Database /
Infrastructure
Permission Verification — **COMPLETE / ACCEPTED — 2026-08-28**
(`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see
docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md +
docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md); S0.8 — Independent Assurance & S0
Closeout (**COMPLETE / ACCEPTED — 2026-08-31** — project-owner acceptance recorded; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md and docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md)
Cross-cutting governance foundation: **Third-Party Software & Licensing Governance — ENACTED /
ACCEPTED — 2026-08-27** — see
docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md (governs license/commercial-use
admission for third-party dependencies, developer tooling, and services; integrated into
docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md as a supplement to, not a replacement for,
existing security admission requirements); now a governing prerequisite for future third-party
dependency/tool admission. S0.6 is now COMPLETE / ACCEPTED — 2026-08-27; S0.7 is now COMPLETE /
ACCEPTED — 2026-08-28 (S0.7A — Local Release Runtime Verification — COMPLETE / ACCEPTED —
2026-08-28: 2026-08-27 evidence valid/accepted as evidence; 2026-08-28 S0.5-F001 remediation
implemented and re-verified, S0.3-G009 Covered / Resolved; S0.7B — database/infrastructure
permission verification — COMPLETE / ACCEPTED — 2026-08-28, S0.3-G010 Covered / Resolved,
S0.7-F002 RETIRED);
S0.8 is now COMPLETE / ACCEPTED — 2026-08-31. S0 is now COMPLETE / ACCEPTED — 2026-08-31.
Stage 9: **COMPLETE / ACCEPTED / LOCKED — 2026-09-08**
Stage 10: **COMPLETE / ACCEPTED / LOCKED — 2026-09-14**
Stage 11 Workspace Shortages: **COMPLETE / ACCEPTED / LOCKED — 2026-09-29**. The owner completed desktop validation and accepted report accuracy, display/usability, cold-load performance, cached performance, and overall readiness. The accepted algorithm is `docs/implementation/stage11_component_mrp_algorithm.md`; it uses a daily ledger from the application-local `asOfDate`, Sunday–Saturday presentation with Monday labels, separate confirmed-only and include-unconfirmed modes, and snapshot-scoped cached projection/export behavior. Workspace Shortages integrates the shared Component Information drawer, PO and buyer-comment presentation, compact initial screen, frozen columns, sticky headers, horizontal scrolling, filtering, sorting, severity display, and workbook export. Stage 9 and Stage 10 remain locked and unmodified.
Pre-Stage-10 customer-workspace UI foundation: **COMPLETE / ACCEPTED** (inter-stage UI update;
not a Stage 10 checkpoint; see `docs/implementation/KST_v2_PRE_STAGE_10_UI_FOUNDATION_CLOSEOUT.md`)
Stage 7 status: **COMPLETE / ACCEPTED - 2026-08-13; reopened, amended, and closed by Stage 7R (Four-Week Work Order Planning Window) - 2026-09-01** - see [Stage 7R Amendment](#stage-7r-amendment-four-week-work-order-planning-window) below
Stage 6 status: **COMPLETE / ACCEPTED — 2026-08-11 — commit `863a638`**
Application version: **`0.1.0-alpha.21`** — the alpha prerelease number identifies the current
numbered project stage (see [Versioning Foundation](#versioning-foundation) below)

## Current Position

**Stage 8 follow-up — Inventory / Lot Locations: COMPLETE / OWNER-ACCEPTED — 2026-10-08,
including the quantity-display amendment.** Owner accepted desktop checklist items 1–5 and
subsequently confirmed the amended display works and looks great.
Owner-authorized brought-forward scope from deferred
Stage 22, before Stage 18, fills the existing Component Information region in both callers with all
selected-component/workspace-site/domain physical rows, exact decimal text, independent Refresh,
localized errors and compatible stale acquisition metadata. Summary readers and locked algorithms
are unchanged. Version stays `0.1.0-alpha.15`; Stage 18 is unstarted. Finished Goods export will be
addressed in Stage 21. Whole quantities display without decimals; fractions round to three places,
with exact source values retained. Evidence, verification exceptions, protected hashes and owner
desktop checklist: `docs/implementation/KST_v2_STAGE_8_INVENTORY_LOCATIONS_FOLLOWUP.md`.

Stages 1–11 are complete and owner-accepted at their documented depth. Stages 9, 10, and 11 are
locked. Stage 11 Workspace Shortages was accepted on 2026-09-29 with its accuracy, display,
usability, export, cold-load performance, and cached performance validated. Its accepted calendar is
Sunday–Saturday with Monday as the visible label. Individual Component MRP is not unfinished Stage
11 scope; it is retained in Stage 18 as a standalone Excel report generator.

Stage 13.6 (2026-10-01): integrated checks and a republished matching desktop sidecar are recorded
in `docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md`. On an owner-approved single live pass for
MSA/Neutronics, the already-loaded six-parent workspace returned a fresh four-row report in one
refresh (103 ms HTTP; one batch, 87 ms) and one owner-selected date-only targeted validation
succeeded (one line, HTTP 200, 114 ms, date family prepared in memory only). Direct legacy parity
was not attempted: its unmodified query cannot restrict to the exact parent list. Cache-hit UI
timing and broader-workspace performance remain unverified. A standalone default-timeout frontend
rerun passed 431 tests with five skipped; earlier Stage 13 timeouts did not recur. Synthetic
golden-byte results and these live reads do **not** themselves prove external QXtend acceptance.
The owner separately reports that the external QXtend process **accepted date, quantity and price
file types**; no environment, timestamp, row count or additional validation evidence is asserted.
KST did not submit or import files. The owner accepts these evidence limits and requests no further
exact-scope legacy comparison or larger-workspace timing. Full lint still **failed** on two unchanged
locked Stage 10 files (changed-scope lint passed); `cargo fmt --check` still **failed** on broader
pre-existing Rust formatting. The owner accepts these as documented Stage 13 closeout exceptions,
not passing checks. The owner explicitly accepted Checkpoint 13.6 and Stage 13 completion on
2026-10-01 with those limits. Version remains `0.1.0-alpha.13`.

Stage 14.1 (2026-10-02): the owner supplied the Planning Workbook business baseline and six
decisions (RA% sales orders excluded inside the workbook; MRP `FORECAST` rows displayed at their own
dates and display-only; projection seed Net QOH + Falldown MPS − Falldown SO with Falldown flows
once in the current fiscal quarter; signed adjustments kept on baseline change with no in-session
acknowledgment; whole-number adjustments; no fresh QAD reread before export). They are recorded in
the Stage 14 decision ledger with synthetic fixtures. The owner accepted Checkpoint 14.1 on
2026-10-02 together with the proposed 14.1 choices, amended so that forecast quarter sums are not
required (weekly and Falldown forecast display remain). No live source check was run at 14.1; later
owner confirmations and the offline `master_plan.py` reference comparison are recorded in ledger §11.

Stage 14.2 (2026-10-02): the owner decided that non-RA sales-order lines without a Due Date are kept
as separate Undated quantity/SO $ totals outside Falldown, weekly/quarter totals and projections,
and directed the version advance to `0.1.0-alpha.14`. Checkpoint 14.2 adds the pure
`Kst.Domain.PlanningWorkbook` calculation, validation, draft-comparison and Mass Updater row
derivation core with fixture-driven Domain tests. It adds no QAD adapter, application service,
endpoint, persistence, CSV serializer or UI, and no backend fiscal-calendar logic. The owner accepted
Checkpoint 14.2 on 2026-10-02, including the D06 and D04/D05 test corrections and the
valid-zero-baseline test (backend suite 1,009/1,009). Verification limits stand: the evidence is
synthetic-fixture only (live source evidence E1–E5 unverified); full-solution `dotnet format`
still fails on 62 untouched files (the scoped check on new files passes); frontend checks,
`cargo check` and the sidecar rebuild were not run, so no packaged `alpha.14` evidence exists.
Details are in the Stage 14 ledger §13.

Stage 14.3 (2026-10-02): complete and owner-accepted on 2026-10-02. It adds:

- batched read-only `QadPartPriceReader` (the accepted Part Info price rule) and `QadForecastReader`
  (MRP `FORECAST` rows);
- `PlanningWorkbookService`, composing the accepted Domain builder from the loaded MPS snapshot,
  Stage 13 Open Orders, Net QOH, prices and forecast;
- an in-memory per-workspace snapshot store with stale last-good behavior;
- `GET`/`POST refresh` `/api/v1/workspaces/{id}/planning-workbook` endpoints, regenerated OpenAPI
  and TypeScript contracts, and `docs/api/PLANNING_WORKBOOK.md`.

Refreshing the workbook also refreshes the shared Stage 13 Open Orders report. The owner accepted
the checkpoint including four post-review corrections (a missing price row is a failed read; the
workspace is re-read after the acquisition gate and before publication or stale fallback; both
query parameters are required in OpenAPI; corrected cache documentation) and the documented cache,
stale-data and shared Open Orders refresh behavior (backend suite 1,053/1,053). Verification limits
at that checkpoint: offline only, with no live QAD call, sidecar, Rust or desktop check. The owner
later confirmed E1–E5 items in the Stage 14 ledger §11; these are owner confirmations rather than
agent-run live queries. Full-solution `dotnet format` still fails on 62 untouched files. Details are in the
Stage 14 ledger §14.

Stage 14.4 (2026-10-02): **owner-accepted on 2026-10-02 with the zero-value display amendment** (exact numeric zeros in grid cells display blank). It adds:

- the read-only Planning Workbook view on the workspace Planning tab (replacing the placeholder),
  with retained part order, Falldown and weekly columns, a fiscal-quarter summary column after each
  quarter's last visible week (partial quarters marked), workspace weekly/quarterly flow totals,
  stale/unavailable/empty/MPS-context states, late-response protection and a Refresh Planning
  control that calls only the workbook refresh endpoint (it also refreshes the shared Stage 13
  Open Orders report);
- frontend-owned fiscal-quarter summaries using the accepted fiscal calendar and 53-week
  exceptions, recomputed locally when fiscal settings change, with exact decimal arithmetic in a
  shared `src/frontend/src/shared/exactDecimal.ts` (Stage 13 imports preserved by re-export);
- owner decisions recorded 2026-10-02 (ledger §10 Q4, Q-N2, Q-N6): 12-week default/minimum and an
  MPS-derived maximum `max(12, lastMpsWeekIndex + 2)` (targeted backend `PlanningHorizonRange`,
  `minimumSelectableHorizonWeeks`/`maximumHorizonWeeks` response fields and 400 range validation;
  no fixed 72-week cap; Dashboard horizon unchanged); blank workspace MPS $ totals when any part is
  unpriced; no Undated display.

Automated verification is offline only (mocked API responses; backend 1,066/1,066). After the
2026-10-02 review corrections (guarded Refresh during loading, business-week rollover revalidation
by GET, fiscal settings verified through the real settings workflow, green SO / yellow MPS row
colours), frontend tests were 477 passed and 5 skipped. After the zero-value display amendment
(exact numeric zeros in grid cells display blank; display only), they were 479 passed and 5
skipped. Separately, the owner rebuilt and reviewed the app in dev mode before accepting. Live E1–E5
and price-tie/currency questions remain unverified. The agent ran no sidecar, Rust or connected
desktop check. Details are in the Stage 14 ledger §15.

Stage 14.5 (2026-10-02): authorized after full 14.4 acceptance; **OWNER-ACCEPTED 2026-10-06 (with
the Master plan redesign and presentation amendments)**. Text below records the implementation
evidence at each step. It adds per-part weekly signed whole-number Adjustment inputs with read-only Proposed MPS,
Adjusted MPS $ and Adj On Hand rows (exact decimals; quarter flows and ending balances; workspace
Adjustment/Proposed MPS/Adjusted MPS $ totals, blank adjusted dollars when any part is unpriced);
validation messages with removal, undo, confirmed Clear All and change counts; proposals kept across
module switches, horizon changes, refresh, week rollover and MPS snapshot replacement (signed
adjustments re-applied, newly invalid entries flagged); optional per-assignment saved drafts
(`%LOCALAPPDATA%\KST\config\planning-workbook-draft-{assignmentId}.json`, atomic, corrupt files
retained) with presence/load-reconcile/save/delete endpoints and review-before-restore; workspace
delete/reset remove the draft, archive keeps it; and unsaved-change protection generalized from
Open Orders to Planning. Review corrections (2026-10-02) invalidate obsolete draft reviews, isolate
asynchronous results per workspace session and bound Undo to one cell-editing session. Offline
verification: backend 1,084/1,084; frontend 541 passed, 5 skipped on clean full runs (intermittent
failures only in unchanged Stage 11/13 tests are recorded in the ledger). No owner review, live QAD,
sidecar, Rust or connected desktop check yet. No CSV export (14.6). Details are in the Stage 14
ledger §16.

Stage 14.5 Master plan UI redesign (2026-10-06): **OWNER-ACCEPTED 2026-10-06**. The Planning
presentation was replaced with the owner's Master plan design (`docs/reference/MP_UI_update/`):
report mode by default (Sales orders, MPS, Projected on hand) and a local planning mode (Adjust row;
proposed MPS and adjusted inventory merged into the MPS and Projected on hand rows; Changes panel);
a compact grid with one sticky header row, quarter totals after each quarter's last visible week,
pinned model column, collapsible model blocks with health strips, struck-through loaded values on
changed cells and whole-workspace totals pinned at the bottom; a View panel (models, rows, totals;
preferences per assignment, filter chips not persisted); a Changes panel and a modal saved-draft
comparison; an inert Import forecast stub; export disabled until 14.6. Owner decisions: totals cover
the whole workspace with the two dollar totals pinned by default; Planning follows the global density
preference; entries outside the visible horizon stay listed and are not by themselves
export-blocking. A remaining draft-safety gap (an earlier visit's save/delete completing after a
restore) was closed. Frontend only; no backend, contract or calculation-rule change; IBM Plex assets
from the prototype were not bundled (unverifiable provenance/license). Offline verification: frontend
559 passed, 5 skipped on three of four full runs (the other failed only the unchanged Stage 11 Edge
scaling test); typecheck, scoped lint, build, version check and
`git diff --check` passed; backend last 1,084/1,084 (not repeated). The 14.6 export column order
(redesign: part, due date, change quantity vs the confirmed Part, adjustment, date) must be reconciled
before 14.6. Review corrections (2026-10-06) recover obsolete saved-draft presence checks after
A → B → A (single recheck per draft change, Retry on failure, no loop) and return keyboard focus
after the saved-draft review to the Review control or the mode button; frontend 570 passed, 5 skipped
on two consecutive full runs. The owner tested the Planning sheet and asked for four presentation
amendments (row tint with a per-workspace View toggle, Projected-on-hand balance line, hover crosshair,
content-sized value columns); they are implemented in the frontend only and reviewed by the owner
on the amended screen (frontend 577 passed, 5 skipped on three consecutive runs; mocked browser checks
found no clipped values and contrast ≥ 4.5:1; 570 → 577 passed is a net +7, 9 tests added — see
ledger §17). The owner accepted the checkpoint including these amendments on 2026-10-06:
**Checkpoint 14.5 — OWNER-ACCEPTED with Master plan redesign and presentation amendments —
2026-10-06.** At that checkpoint closeout, later decisions and desktop checks were still pending;
they are recorded in ledger §11 and §18–§21.

Stage 14.6 (2026-10-06): **OWNER-ACCEPTED — 2026-10-07**. Owner decisions recorded in the
Stage 14 ledger (§7, §10, §18): the Mass Updater CSV columns are **Part, Qty, Date** (no header); **Qty is the
signed adjustment** to the existing MPS quantity; after a *successfully saved* export the user is asked
*Clear the staged adjustments included in this export?* (Clear / Keep; declining or dismissing keeps; cancelled
or failed saves never clear or prompt); saved drafts stay unchanged. It adds `MpsMassUpdateService` (validates
every staged adjustment — including weeks hidden by the horizon, View or filters — against the complete loaded
MPS snapshot for the current workspace, site and snapshot; whole-file rejection; **no QAD access**, MPS
refresh, database write, submission or import), `Kst.Exports.MpsMassUpdateCsv` (golden-byte format: UTF-8 no BOM,
CRLF after every row, `M/d/yyyy`, no leading `+`, no formula prefix), the
`POST /planning-workbook/mass-updater-export` endpoint (regenerated OpenAPI/TypeScript contracts,
`docs/api/PLANNING_WORKBOOK.md`), and the enabled *Review and export* flow (preview parsed from the actual
file bytes, explicit Save As through the existing Tauri capabilities — none added — saved / cancelled / failed
outcomes, prepared-export invalidation on session, adjustment, MPS snapshot, site or business-week change, and
the Clear / Keep prompt, offered only after a confirmed native file write; a browser-fallback download is reported honestly as a
started download, keeps every adjustment and offers no Clear / Keep). Export success means the file was saved, not that QAD
applied it. Review corrections (2026-10-06): Clear removes only untouched exported entries (an entry edited back to the
exported value or removed and re-entered is kept), and the export dialog keeps keyboard focus on a usable control through
every step. Offline verification: backend 1,135/1,135 (unchanged by the review corrections, not rerun); frontend
642 passed, 5 skipped on two consecutive full runs (a third run had one failure in the unrelated, unchanged
Long-Term Shortages Edge-helper test; an earlier run had a load-related timeout in the unchanged Stage 13 Save As test);
typecheck, scoped lint, production build, version
check and `git diff --check` passed; full lint still shows only the two known Stage 10 errors. **Not verified at 14.6 closeout:**
desktop Tauri Save As, the packaged sidecar (not rebuilt), the external QAD Mass Updater trial (E5), live-source
E1–E5. Subsequent owner confirmations and final desktop acceptance are recorded in the Stage 14 ledger §11 and
§19–§21. Version remains `0.1.0-alpha.14`. Details are in §18.

Stage 14.7 (2026-10-07): **OWNER-ACCEPTED — 2026-10-07**. Integrated verification was run on
the current checkout without starting 14.8: backend restore/build/tests; OpenAPI regeneration +
TypeScript contract regeneration/sync; frontend typecheck/tests/lint/build; Rust `cargo check`,
`cargo test --locked --offline` and `cargo build`; sidecar publish via `scripts/build-sidecar.ps1`.
The sidecar identity was verified by matching SHA256 for `publish/backend-sidecar/Kst.Api.exe` and
`src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`
(`30D9151A564F74ACDCC2486FA7C6135AA32C58A6044FC1E1E8D28A0784CD74AB`). Runtime probes against a
local backend instance confirmed `/health` and `/api/v1/system/status` report
`0.1.0-alpha.14`; a Mass Updater export route probe returned sanitized 404 `workspace-missing`,
confirming endpoint reachability and error contract behavior. Frontend full-suite run 1 had three
timeouts in unchanged files (`OpenOrdersPanel` Save As workflow test, `PlanningWorkbookAdjustments`
fixture-merge test, `PlanningWorkbookPanel` unpriced/zero-price test); isolated reruns of those
files then passed and a subsequent full run passed. Full lint still reports only the two known
locked Stage 10 errors; full-solution `dotnet format --verify-no-changes` still reports broad
pre-existing formatting/EOL failures, and scoped format including `OpenOrdersQxtendCsv.cs` confirms
its pre-existing LF/EOL mismatch. Gitleaks/DevSkim were unavailable on PATH and were not installed.
At this checkpoint's initial closeout, native desktop behavior and external Mass Updater acceptance
were awaiting owner checks; the owner subsequently completed and accepted those checks on 2026-10-07.
Owner and automated evidence are recorded separately in Stage 14 ledger §19–§21. Details are in
Stage 14 ledger §19 and
`C:\Dev\kst_v2_tmp\Stage14\STAGE_14_7_CHECKPOINT_COMPLETION_REPORT.md`.

The owner approved the revised Release 1 roadmap on 2026-09-29:

- Stage 12 Multi-Part Shortage Analysis is **RETIRED / SUPERSEDED** by accepted Workspace Shortages.
- Stage 13 is workspace-scoped Customer Open Orders with Report Mode, Planning Mode, saved layouts,
  optional saved drafts, and QXtend date/quantity/price files. Cross-customer Open Orders belongs to
  Stage 18 rather than an individual workspace.
- Stage 14 is Planning Workbook, reordered behind Open Orders.
- Stage 15 remains Finished Goods.
- Stage 16 is **ABSORBED** into Stage 18; its cross-customer outcome is not part of workspace Stage 13.
- Stage 17 General WO Variance is **RETIRED / NO LONGER DESIRED**.
- Stage 18 contains the required standalone Excel generators: Component MRP, Open Order Report,
  Shipments-To-Go, and S&OP.
- Stage 19 Historical Shipments is **COMPLETE / OWNER-ACCEPTED — 2026-10-13**; see
  `docs/implementation/KST_v2_STAGE_19_CLOSEOUT.md`.
- Stage 20 Legacy Simulation is **RETIRED / NO LONGER DESIRED**.
- Stage 21 is **COMPLETE / OWNER-ACCEPTED — 2026-10-14** for corrected MPS/BOM analytical exports and
  the bounded inventory/conformance record, with explicit unresolved dispositions and evidence limits
  retained. Finished Goods needs no export; Planning analytical redesign is future iterative work
  outside Stage 22. The accepted Stage 11 export and Planning Mass Updater CSV remain delivered.
- Stage 22 is an interactive, product-wide Refinement & Optimization stage. It receives measured
  performance work, usability defects, refinements, and deferred Release 1 work that does not fit a
  feature stage. The known nonfunctional horizontal scrollbar in the main MPS matrix is a required
  Stage 22 item.
- Quality and Hardening moves to Stage 23; Release 1 Readiness to Stage 24; Pilot to Stage 25;
  Incremental Multi-Site Rollout to Stage 26; and Post-Release Roadmap to Stage 27.

The stage-disposition matrix, deferred-work registry, removed-capability record, and documentation
reconciliation ledger are in `docs/status/RELEASE_1_SCOPE_REVIEW_2026-09-29.md`.

Stage 22 does not silently reopen locked algorithms. A change to accepted Stage 9–11 business
behavior requires an explicit owner-approved amendment. New standalone capabilities also require an
explicit roadmap disposition rather than entering Stage 22 as unbounded refinement.

This roadmap disposition did not itself authorize an implementation stage. Stage 13 checkpoints
13.1–13.6 subsequently received separate owner authorization and acceptance; Stage 13 is complete
and accepted as of 2026-10-01. Future stages require their own authorization and acceptance.

### Accepted Stage 11 closeout and historical foundation status

Stage 11 closeout (2026-09-29): automated verification and owner desktop validation passed. The
owner’s real-world cold-load measurements compared KSTv1/KSTv2 medians of Shure **72/11 seconds**,
Taco **54/10 seconds**, and MSA/Neutronics **20/6 seconds**. Across nine observations, the overall
median was **53/10 seconds** and average approximately **55.1/11.7 seconds**, a roughly **79%**
average elapsed-time decrease. Server load affected the uncontrolled measurements; they demonstrate
consistent material improvement rather than a SQL benchmark or SLA. The retained production-pass
evidence records 891 backend tests, 68 focused frontend tests, OpenAPI/type synchronization,
typecheck, changed-file lint, production build, sidecar rebuild, 2,304 screen equivalence cases,
and retained projection/workbook comparisons.

**Deferred future backlog:** Individual Component MRP is not an unfinished Stage 11 requirement. It
is assigned to the Stage 18 standalone report generators and remains unstarted. The DBA
evidence request is retained but deferred unless production use reveals a material concurrency or
reliability issue. Virtualization is deferred until its keyboard-focus/accessibility failure is
resolved. Minor report fine-tuning is non-blocking future backlog. No next implementation stage is
authorized by the current project documentation.

**Final S0 closeout:** S0 — **COMPLETE / ACCEPTED — 2026-08-31**; S0.8 — **COMPLETE / ACCEPTED — 2026-08-31**; S0.7 — **COMPLETE / ACCEPTED — 2026-08-28**. Stage 9 — **COMPLETE / ACCEPTED / LOCKED — 2026-09-08**.

Stages 1 through 8 are complete and accepted, and UI Navigation & Keyboard Ergonomics A is complete
and accepted. Stage 9 is **COMPLETE / ACCEPTED / LOCKED** following owner acceptance at 9.11.
R0 — Repository / Documentation Reconciliation is complete and accepted. The current cross-cutting effort is **S0 — Security
Foundation Integration**; S0.1 — Security Policy Injection and S0.2 — Security Baseline Discovery
are complete and owner-accepted, and S0.3 — Existing-Tool Security Checks is complete and
owner-accepted (see `SECURITY.md`, `docs/security/`, and the S0 section of
`KST-v2-Master-Project-Checklist.md`). The remaining S0 work is approved as checkpoints
S0.4–S0.8 (see "Remaining S0 Work" below and
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md). S0.4 — Security Finding
Disposition & Bounded Remediation is **COMPLETE / ACCEPTED — 2026-08-25**: S0.4A — QAD SQL
Transport Correction is **COMPLETE / ACCEPTED — 2026-08-25** (accepted remediation evidence:
`docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`; resolves `S0.2-F003` at the
application-configuration level); S0.4B — Tauri Shell Capability is **COMPLETE / ACCEPTED —
2026-08-25** (resolves `S0.2-F001`; accepted remediation evidence:
`docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`); S0.4C — npm
Development-Tooling Advisories is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.3-F001`;
accepted remediation evidence:
`docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`). All three S0.4 sub-checkpoints are
owner-accepted. S0.5 — Security Regression & Architecture Checks is **COMPLETE / ACCEPTED —
2026-08-26** (accepted implementation evidence:
docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md). S0.6 — Security Tool
Admission is **COMPLETE / ACCEPTED — 2026-08-27**: Capability Review 1 (Rust Dependency Advisory Capability, gap
`S0.3-G001`) is **COMPLETE / ACCEPTED — 2026-08-26** — cargo-audit 0.22.2 ADMITTED /
ACCEPTED (installed, verified, owner-accepted) per
docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md (S0.3-G001 — Covered / Resolved;
cargo-deny 0.20.2 DEFERRED); Capability Review 2 — Dedicated Secret Scanning (gap
`S0.3-G007`) is **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks v8.30.0 installed,
release-integrity and synthetic-canary verified, scanned current KST content (4 findings) and full
Git history (8 findings), all rule `private-key`, confirmed documentation false positives;
`S0.3-G007` — Covered / Resolved) — see docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md
(neutral research packet, not a recommendation or admission decision) and
docs/security/S0_6_SECRET_SCANNING_ADMISSION.md (owner decision and implementation evidence;
Gitleaks v8.30.0 — ADMITTED / IMPLEMENTED / ACCEPTED; Gitleaks v8.30.1, TruffleHog v3.97.1,
detect-secrets v1.5.0 DEFERRED); Capability Review 3 —
Software Bill of Materials (SBOM), gap `S0.3-G008`, is **COMPLETE / ACCEPTED
— 2026-08-27** — Anchore Syft v1.51.1 installed (a pre-existing binary was
independently verified byte-identical to a freshly verified official release), release-integrity
verified, and run against KST build/repository evidence (SPDX 2.3, 1,027 packages; CycloneDX 1.6
via the explicit `@1.6` selector, 1,026 components) and a complementary packaged-artifact view
(the published self-contained single-file `Kst.Api` sidecar, from which 37 NuGet packages were
recovered directly) — see
docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md (neutral research packet, not a recommendation or
admission decision) and docs/security/S0_6_SBOM_ADMISSION.md (owner decision and implementation
evidence; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED; Microsoft sbom-tool v4.1.5
and the CycloneDX ecosystem-native approach — cyclonedx-dotnet
6.2.0, cyclonedx-npm 6.0.1, cargo-cyclonedx 0.5.9 — remain DEFERRED; six informational findings
`S0.6-F014` through `S0.6-F019` recorded, none blocking; complete Tauri Windows
installer/application bundle Unable to Verify / future packaged-release verification boundary, not
Accepted Risk); `S0.3-G008` is Covered /
Resolved; Capability Review 4 — Dedicated Static Application Security Testing (SAST), gap
`S0.3-G006`, is **COMPLETE / ACCEPTED — 2026-08-27** — the owner reviewed the
neutral research packet comparing
Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 (see
docs/security/S0_6_SAST_ADMISSION_RESEARCH.md; not a recommendation or admission decision) and
admitted Microsoft DevSkim CLI v1.0.90, which was installed, self-verified,
synthetically validated (C#, JavaScript/TypeScript, Rust, SQL), and run against the KST source
tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed 2026-08-27 and reclassified to
Informational / Framework-Local Origin / Confirmed DevSkim False Positive for
plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule Limitation,
neither Accepted
Risk) (see docs/security/S0_6_SAST_ADMISSION.md); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4
DEFERRED (not rejected) pending organizational licensing/entitlement review; `S0.3-G006` is
Covered / Resolved; Microsoft DevSkim CLI v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED.
With `S0.3-G001`, `S0.3-G006`, `S0.3-G007`, and `S0.3-G008` all Covered / Resolved, **S0.6 —
Security Tool Admission is COMPLETE / ACCEPTED — 2026-08-27**. The
cross-cutting **Third-Party
Software & Licensing Governance** foundation is **ENACTED / ACCEPTED — 2026-08-27** — see
docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md, integrated into
docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md as a licensing/commercial-use admission gate
supplementing (not replacing) existing security admission requirements; this is now a governing
prerequisite for future third-party dependency/tool admission. Stage 9 begins only after S0 is formally
closed and accepted.

## S0 — Security Foundation Integration

**Status:** COMPLETE / ACCEPTED — 2026-08-31

### S0.1 — Security Policy Injection

**Status:** COMPLETE / ACCEPTED — 2026-08-21

This checkpoint converted the Security Foundation working draft
(`docs/reference/security/KST v2 Security Foundation — Initial Policy and Enactment Draft.md`,
retained as historical design provenance) into a durable, repository-integrated, enacted security
policy framework:

- `SECURITY.md` — repository security entry point.
- `docs/security/SECURITY_ASSURANCE_POLICY.md` — primary normative platform-neutral policy.
- `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md` — coding-environment/agent expectations.
- `docs/security/DEPENDENCY_ADMISSION.md` — dependency and development-tool admission rules.
- `docs/security/AI_SECURITY_REVIEW.md` — independent AI security-review model.
- `docs/security/APPLICATION_SECURITY_PROFILE.md` — KST-specific declared security properties.
- `AGENTS.md` §8 — concise mandatory agent security behavior, pointing to the policy above.

This checkpoint was documentation/policy work only: no security baseline discovery (S0.2), no
existing-tool security checks (S0.3), and no security remediation were performed. No application
code, tests, or dependency manifests changed. `docs/security/SECURITY_BASELINE.md` did not exist as
of this checkpoint — it was produced by the subsequent S0.2 checkpoint (see below).

The policy documents above are enacted, owner-accepted Tier 1 authority as of 2026-08-21.

### S0.2 — Security Baseline Discovery

**Status:** COMPLETE / ACCEPTED — 2026-08-24

This checkpoint produced an observational (not normative) security baseline:
`docs/security/SECURITY_BASELINE.md`, covering repository dependencies (NuGet/npm/Cargo), SDK/build
tooling, the development/AI-agent environment, and the application's networking, CORS/CSP/Tauri
capability, subprocess, filesystem, credential, database, external-destination, logging, and
packaging surfaces, observed against commit `4b4ba3f`.

Three observations were initially recorded (`S0.2-F001` Tauri shell-capability scope,
`S0.2-F002` database-level read-only enforcement, `S0.2-F003` QAD `TrustServerCertificate` default).
Following a 2026-08-24 correction incorporating project-owner/IT-provided operational authority on
QAD authentication, transport, and authorization:

- `S0.2-F001` (Tauri shell-capability scope) remains `Potential / Investigation Required`.
- `S0.2-F002` (database-level read-only enforcement) is **retired** — QAD's Windows Integrated
  Authentication plus a prohibition on SQL-authenticated access corroborates read-only/least-
  privilege access at the authentication-policy level.
- `S0.2-F003` (QAD transport configuration) is **reclassified to `Confirmed`** — the
  repository-observed `TrustServerCertificate=true` configuration does not accurately express the
  IT-confirmed required transport configuration (`Encrypt=false`, because the current QAD SQL
  infrastructure does not support encrypted client connections). No severity is assigned and the
  underlying unencrypted-transport constraint is explicitly **not** marked `Accepted Risk` —
  formal IT/security risk acceptance remains unresolved. Recommended remediation
  (`Encrypt=false`, remove `TrustServerCertificate=true`) is deferred to an explicitly authorized
  future remediation checkpoint.

No remediation, tool installation, vulnerability scanning, or SBOM generation was performed.
`SECURITY.md` was updated to link the accepted baseline.

This checkpoint was documentation/observation work only: no application code, tests, dependency
manifests, or security controls changed.

**Next:** S0.3 — Existing-Tool Security Checks (see below).

### S0.3 — Existing-Tool Security Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-24

This checkpoint executed security-relevant checks using only the repository's existing
toolchain: existing tests, project compilers/analyzers/linters, ecosystem-native
package-manager advisory functionality of the already-installed toolchain (the only authorized
external advisory queries), and read-only Git/native checks. No tool was installed or
activated, no remediation was performed, no dependency manifest or lockfile changed, no
configuration or security control changed, no database connection was made, no SQL was
executed, and no application/sidecar was launched. Evidence:
`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (accepted verification/check
evidence; not normative policy).

Key results:

- Backend: analyzer-enabled build 0 warnings/0 errors; **656/656 tests passing**, including
  `DependencyRuleTests` (6/6), `VersionConsistencyTests` (3/3), and `CorsPolicyTests` (2/2 —
  partial origin coverage; 3 of 5 configured origins and the no-`AllowAnyOrigin`/
  no-credentials properties are not test-verified).
- Frontend: lint clean, typecheck clean, **281/281 tests passing** (general quality; no
  security-relevant assertions identified).
- Rust: `cargo clippy --locked --offline` — 2 style-only warnings (`needless_return`), no
  security diagnostics; **0 tests exist** in the Tauri crate.
- NuGet advisory check (native, transitive included, no restore): **no known advisories**
  reported for the evaluated (last-restored) dependency graph.
- npm advisory check (native): **3 advisories**, all in development-only packages
  (`openapi-typescript`, transitive `undici`, transitive `nanoid`) — recorded as
  **S0.3-F001 (Confirmed)**; unremediated by design of this checkpoint; disposition
  (reachability analysis + dependency-admission decision) remains for a later explicitly
  authorized checkpoint. npm-reported severities are not KST risk severities.
- Cargo advisory: **no authorized/available Rust dependency advisory scanner exists**
  (`cargo-audit`/`cargo-deny` absent) — recorded as a gap.
- S0.2 finding re-verification: **S0.2-F001** remains `Potential / Investigation Required`
  (no existing least-privilege verification identified); **S0.2-F002** remains retired;
  **S0.2-F003** re-verified **still present** at the check-execution commit (read-only
  configuration check only; not remediated; not `Accepted Risk`).
- Ten existing-tool coverage gaps recorded (**S0.3-G001** through **S0.3-G010**), including
  Rust advisories, dedicated secret scanning, SAST, SBOM, runtime listener verification, CSP
  verification, Tauri least-privilege verification, and database-grant verification. Candidate
  later needs are listed as capability categories only — no scanner/SAST/SBOM/CI selection is
  made.

`SECURITY_BASELINE.md` was **not modified**; the accepted S0.2 baseline remains the snapshot.

**Remaining S0 work:** approved as checkpoints S0.4–S0.8 in the remaining-S0 work plan (see
"Remaining S0 Work" below).

### S0.4 — Security Finding Disposition & Bounded Remediation

**Status:** COMPLETE / ACCEPTED — 2026-08-25

S0.4 addressed the open findings established by accepted S0.2/S0.3 evidence, organized as three
bounded sub-checkpoints (full scope/boundaries in
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md). All three sub-checkpoints
(S0.4A, S0.4B, S0.4C) are owner-accepted; S0.4 is complete.

- **S0.4A — QAD SQL Transport Correction: COMPLETE / ACCEPTED — 2026-08-25** (implemented and
  verified 2026-08-24; owner-accepted 2026-08-25). Corrects the confirmed `S0.2-F003`
  configuration mismatch — the effective QAD connection now establishes `Encrypt=false` (explicit)
  with `TrustServerCertificate` not set to `true`, preserving Windows Integrated Authentication,
  read-only / least-privilege access, and the internal-network restriction. **Resolves
  `S0.2-F003` at the KST application-configuration level.** No `keytronicshortage`, Tauri-capability,
  or npm-dependency change. Accepted remediation evidence (**not normative policy**):
  `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`. Regression coverage:
  `QadConnectionStringFactoryTests`; full backend suite 660/660 passing at implementation. The
  underlying unencrypted-transport constraint remains a **separate residual infrastructure issue —
  not** `Accepted Risk`; formal IT/security risk acceptance and runtime/infrastructure
  verification remain unresolved (S0.7/S0.8).
- **S0.4B — Tauri Shell Capability: COMPLETE / ACCEPTED — 2026-08-25** (implemented and
  verified 2026-08-25, including owner-guided live sidecar lifecycle verification; owner-accepted
  2026-08-25). **Resolves `S0.2-F001`:** the webview capability now grants exactly
  `core:default` (the unused `shell:allow-execute` / `shell:allow-open` grants are removed; the
  `Kst.Api` sidecar and the PID-scoped powershell/taskkill shutdown handling run from the Rust
  host, outside the webview IPC surface). Regression coverage: `capability_guard` tests in
  `src/tauri/src/lib.rs` (`cargo test` in `src/tauri`); mutation check confirmed
  shell-permission reintroduction is caught. Accepted remediation evidence (**not normative
  policy**): `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`. New finding:
  `S0.4B-F001` (Informational — unused `@tauri-apps/plugin-shell` frontend dependency; removal
  requires separate authorization).
- **S0.4C — npm Development-Tooling Advisories: COMPLETE / ACCEPTED — 2026-08-25** (implemented
  and owner-accepted 2026-08-25). **Resolves `S0.3-F001`:** controlled `openapi-typescript`
  6.7.6 → 7.13.0 major update plus a targeted `nanoid` 3.3.16 → 3.3.18 transitive re-resolution;
  `undici` removed from the graph; `npm audit` total 0; OpenAPI generation and the full frontend
  typecheck/lint/test/build verified (281/281 tests). Accepted remediation evidence (**not
  normative policy**): `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`.

### S0.5 — Security Regression & Architecture Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-26 (implemented 2026-08-25; owner-accepted
2026-08-26; starting commit `f784c835a507b9fd4a67c95c46ffef9ab308304c`)

S0.5 added durable, repository-native regression protection for the accepted security
properties identified in the accepted S0.3 evidence: loopback-only backend binding
(`LoopbackBindingTests` — behavioral OS-level bind check), Tauri CSP `connect-src` loopback
restriction (`csp_guard` tests in `src/tauri/src/lib.rs`), the accepted frontend CORS origin
set plus `AllowAnyOrigin`/credentials absence (extended `CorsPolicyTests`), and
application-emitted read-only QAD SQL (`QadReadOnlySqlTests`). S0.3-G004 (Tauri
least-privilege) was confirmed already covered by the accepted S0.4B `capability_guard` tests;
no new S0.4B-scope code was added. No production behavior change, no new dependency, no tool
installation, no runtime/infrastructure verification (routed to S0.7), and no S0.6 tool
admission. Informational findings: `S0.5-F001` (operator `ASPNETCORE_URLS` override is
outside repository regression protection — carried to S0.7), `S0.5-F002` (read-only SQL check
is lexical/structural; server-side grants remain S0.7). Final accepted gap dispositions:
S0.3-G002 — Covered by S0.5 repository regression protection; S0.3-G003 — Covered by S0.5
repository regression protection; S0.3-G004 — Covered by accepted S0.4B regression
protection; S0.3-G005 — Covered / Resolved (application-emitted QAD SQL is
repository-regression protected as read-only by S0.5 `QadReadOnlySqlTests`, and effective
production QAD permissions independently enforce read-only per S0.7B/G010; the application-level
guard remains lexical/structural per S0.5-F002); CORS secondary observation — Covered by S0.5
regression protection. Evidence
(**not normative policy**):
docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md.

### Remaining S0 Work — Approved Roadmap (S0.4–S0.8)

**Status:** Approved Planning Baseline — 2026-08-24 (active planning / Tier 4; not normative
policy)

The project owner approved the following structure for the remaining S0 work, derived from the
accepted S0.2/S0.3 evidence:

| Checkpoint | Name | Status |
|---|---|---|
| S0.4 | Security Finding Disposition & Bounded Remediation | **COMPLETE / ACCEPTED — 2026-08-25** (S0.4A COMPLETE / ACCEPTED; S0.4B COMPLETE / ACCEPTED; S0.4C COMPLETE / ACCEPTED) |
| S0.5 | Security Regression & Architecture Checks | **COMPLETE / ACCEPTED — 2026-08-26** (implemented 2026-08-25 — see docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md) |
| S0.6 | Security Tool Admission | **COMPLETE / ACCEPTED — 2026-08-27** — Capability Review 1 (Rust dependency advisory, `S0.3-G001`) **COMPLETE / ACCEPTED — 2026-08-26** — cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED — see docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md (S0.3-G001 — Covered / Resolved); Capability Review 2 (dedicated secret scanning, `S0.3-G007`) **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks v8.30.0 ADMITTED / IMPLEMENTED / ACCEPTED) — see docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md and docs/security/S0_6_SECRET_SCANNING_ADMISSION.md; Gitleaks v8.30.1/TruffleHog v3.97.1/detect-secrets v1.5.0 DEFERRED; `S0.3-G007` — Covered / Resolved; Capability Review 3 (SBOM, `S0.3-G008`) **COMPLETE / ACCEPTED — 2026-08-27** — Anchore Syft v1.51.1 ADMITTED / IMPLEMENTED / ACCEPTED — see docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md and docs/security/S0_6_SBOM_ADMISSION.md; Microsoft sbom-tool v4.1.5 and the CycloneDX ecosystem-native approach DEFERRED; six informational findings `S0.6-F014`–`S0.6-F019`, none blocking; complete Tauri Windows installer/application bundle Unable to Verify / future packaged-release verification boundary, not Accepted Risk; `S0.3-G008` — Covered / Resolved; Capability Review 4 (dedicated SAST, `S0.3-G006`) **COMPLETE / ACCEPTED — 2026-08-27** — neutral research packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 — see docs/security/S0_6_SAST_ADMISSION_RESEARCH.md (not a recommendation or admission decision) and docs/security/S0_6_SAST_ADMISSION.md (owner decision, full implementation evidence, and 2026-08-27 project-owner acceptance); Microsoft DevSkim CLI v1.0.90 ADMITTED / INSTALLED / VERIFIED / ACCEPTED (50 findings across 3 bundled rules from the KST source-tree scan; `S0.6-F020` — Informational / Framework-Local Origin / Confirmed DevSkim False Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule Limitation; neither Accepted Risk); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED (not rejected); `S0.3-G006` — Covered / Resolved. All four S0.6-assigned gaps (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are Covered / Resolved. |
| S0.7 | Runtime & Infrastructure Verification | **COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification (working pass) — **COMPLETE / ACCEPTED — 2026-08-28** (2026-08-27 evidence pass VALID / ACCEPTED AS EVIDENCE by owner review: release-built runtime evidence — loopback-only `127.0.0.1` sidecar listener on OS-assigned port, no wildcard/LAN listener, clean sidecar lifecycle with no orphans, runtime CORS matching the accepted five-origin allowlist, release-build CSP/capability artifact evidence; safe `ASPNETCORE_URLS` loopback precedence test confirmed the operator environment override alters the effective listener. 2026-08-28: `S0.5-F001` loopback-binding remediation implemented — the sidecar now unconditionally sets an explicit `http://127.0.0.1:<port>` `UseUrls` endpoint (verified on the shipped self-contained .NET 10 release runtime to ignore an inherited `ASPNETCORE_URLS` value after the fix), so inherited hosting configuration no longer takes authority over the listener — with failure-safe behavioral regression tests (no test can create a wildcard listener even in its failing state; original wildcard real-process test replaced before acceptance — see evidence §26.3; incl. demonstrated pre-fix failure), 672/672 backend suite, and post-fix release-runtime re-verification (env value no longer controls listener selection) — **`S0.5-F001` — REMEDIATED AND VERIFIED BY S0.7**; **S0.3-G009 — Covered / Resolved** (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational / Package-Identity Coexistence Issue — Deferred for packaging/deployment decision; S0.7B — database/infrastructure permission verification incl. `S0.3-G010` — **COMPLETE / ACCEPTED — 2026-08-28** (`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md + docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md |
| S0.8 | Independent Assurance & S0 Closeout | **COMPLETE / ACCEPTED — 2026-08-31** (project-owner acceptance recorded; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md and docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md) |

**Cross-cutting governance foundation:** Third-Party Software & Licensing Governance is **ENACTED /
ACCEPTED — 2026-08-27** — see docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md
(integrated into docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md); a governing prerequisite for
future third-party dependency/tool admission, not itself an S0 checkpoint and not an admission
decision for any specific component.

Full scope, boundaries, and the finding/gap-to-checkpoint mapping are in
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md (approved active planning; not
normative policy). The roadmap is historical planning evidence; S0.4–S0.8 are now complete and
accepted. Stage 9 — Immediate Work-Order Shortages is **COMPLETE / ACCEPTED / LOCKED — 2026-09-08**.

## Stage 8 — Component and BOM Detail

**Status:** COMPLETE / ACCEPTED — 2026-08-21

Stage 8 is an **informational Component/BOM investigation capability**. It does not implement
material-requirement netting, shortage classification, or PO coverage.

Accepted behavior includes the current effective multi-level BOM (`ps_mstr`) with structural
hierarchy/order and repeated occurrences preserved, phantoms shown and exploded, BOM Component Item
and Description search plus P/M and Phantom filters (the Description filter was added by the
2026-08-31 accepted UX amendment recorded in the Stage 8 closeout), the shared Stage 6 Net /
Non-Net inventory semantics, a blocking Component
Information modal with selected-site planning fields, Standard Cost (`sct_sim = 'Standard'`, latest
`sct_cst_date`), QCTC (`inp_source = 'qtbom_det'`, latest `inp_start_date`), and Approved Alternates
as the user-facing term (technical `ApprovedVendor` / `vp_mstr` naming is retained).

The following are **intentionally deferred future capability, not unfinished Stage 8 acceptance
requirements**: Show MRP, Inventory / Lot Locations, Extended Requirement, Incoming Supply,
Coverage / Material Status, component MRP / component supply netting, and Future Shortages / PO
coverage.

Full delivered capability, source decisions, and verification evidence (backend 656/656 tests,
frontend 260/260 tests, architecture tests, live QAD validation, owner acceptance) are recorded in
`docs/implementation/KST_v2_STAGE_8_CLOSEOUT.md` — see that document rather than this summary for
detailed acceptance evidence.

## Stage 9 — Immediate Work-Order Shortages

**Status:** **COMPLETE / ACCEPTED / LOCKED — 2026-09-08**. Stages 9.7 automated audit, 9.8 live-QAD
validation, 9.9 full regression, 9.10 documentation reconciliation, and 9.11 owner acceptance are
complete. Current closeout authority:
`docs/implementation/KST_v2_STAGE_9_CLOSEOUT.md`.

Stage 9 provides a **Work Order + Component** immediate-material analysis over the accepted Stage
7R planning window. Actual or Projected requirements preserve master-P/M reliability: manufactured
components remain visible and drillable as `NotApplicable`, while only nonblank master non-`M`
components participate in shortage arithmetic. Physical usable inventory is evaluated through the
accepted hybrid allocation model: authoritative usable `lad_det` hard reservations, residual
committed R-to-A allocation, and a non-additive advisory pool for uncommitted WOs. PO/KSS context
is informational only; KSS is an independent effective supplier-schedule relationship. The normal
workflow is Work Orders → Show/Hide Material Lines → one integrated selected-WO analysis, with no
MPS-grid shortage marker.

The Stage 9 plan preserves planning history. The audit, live-QAD validation, regression record,
source mapping, and closeout provide the current accepted evidence and disposition.

## Stage 10 — Purchase-Order Drill-Down / Component Orders

**Status:** **COMPLETE / ACCEPTED / LOCKED — 2026-09-14**. Current closeout authority:
`docs/implementation/KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md`.

Stage 10 provides the active workspace's **informational-only** Component Orders surface. Its
population is the current-effective multi-level BOM components of the workspace MPS snapshot's
resolved parents, restricted to conventional QAD PO lines whose raw ordered-minus-received quantity
is positive and whose `pod_status` is not C/X case-insensitively. It groups components by their
earliest due PO and preserves due-date, PO, then line ordering when expanded. Missing due dates are
yellow exceptions; dates on or before the applicable Friday cutoff are red/late.

QAD-delivered context includes master-data fallback, weeks lead time, PO/line/due/open quantity,
line confirmation, supplier, workspace-domain buyer, manufacturer item, tracking, and an
independent effective KSS indicator. Read-only Shortages enrichment supplies latest-active exact
site/component Current Comments plus Credit Hold and CIA. An enrichment failure leaves QAD PO rows
visible and marks enrichment unavailable; it does not manufacture blank enriched data. Supplier
risk uses the accepted exact supplier-display-name lookup with `Date DESC, ID DESC` precedence.

Component Orders does **not** assert PO coverage, reservation, shortage clearance, projection,
netting, or a clear date. Those calculations are intentionally deferred to Stage 11. Current
Comments are read-only: no ShortageMaster write, local note persistence, or export-note update was
authorized or delivered. Stage 9 remains complete, accepted, locked, and unmodified.

`PERF-001` remains deferred. A cache-miss profile identified QAD PO reads as the dominant phase and
a reader-local 500-to-250 batch experiment was only modestly faster. Do not tune further until a
normal shared QAD/MPS baseline and DBA-reviewed plan/index evidence are available.

## UI Navigation & Keyboard Ergonomics A

**Status:** COMPLETE / ACCEPTED — owner manual validation PASS, 2026-08-21

The accepted interaction convention is **hierarchical Escape unwind** — not browser-history
navigation and not modal-only dismissal:

1. Topmost blocking modal/dialog → close/cancel only that surface.
2. Nested detail in the current investigation → collapse exactly one level.
3. Main MPS detail/drill-down → return to the Part Matrix.
4. Part Matrix/root → do nothing.

Examples: Component Information → BOM; BOM → Part Matrix; nested material/candidate detail → prior
material level; Show Material Lines → Work Order view; Work Order view → Part Matrix; Part Info →
Part Matrix.

Ergonomics B (arrow-key tab navigation, shared focus-trap extraction, menu roving focus, and
additional shortcut hints) remains deferred/planning-only. Full detail, investigation, and the
accepted outcome are recorded in
`docs/implementation/KST_v2_UI_NAVIGATION_KEYBOARD_ERGONOMICS_PLAN.md`.

## Stage 7 Accepted Behavior

- Selecting an MPS parent focuses the grid and opens Part Info; Work Orders/Shortages/Future Shortages/Components tabs remain disabled until a bucket is selected.
- Selecting an eligible MPS week cell or Falldown automatically opens Work Orders. Top-level drill-down is limited to Falldown + the first 6 forward MPS weeks.
- Only `Allocating`/`Frozen`/`Released` work orders receive a Stage 7 card; `RMABOM` work orders are excluded from candidate results.
- WOID (`wo_mstr.wo_lot`) is the scheduler-facing identity; Work Order Number is not unique and is never shown.
- Kitting % is line-based (fully-issued line count / applicable line count), never quantity-weighted; zero applicable lines yields null/N-A, never 0%.
- Manufactured-subassembly candidates use a truthful "Work Orders for `<Component Part>`" navigation model — QAD has no reliable parent↔subassembly WO relationship, and none is fabricated. Candidates are all eligible A/F/R work orders for the component (the original due-date-boundary rule was live-validated as correct, then removed by project-owner decision — see `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`).
- Maximum drill depth is 3 levels; lazy-load/cache is keyed by workspace + MPS snapshot generation and is invalidated outright (not stale-fallback) on a new successful MPS refresh.

## Stage 7 Verification

- Backend: **468/468 tests passing**, `dotnet format`/build clean.
- Frontend: **167/167 tests passing**, lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean; sidecar rebuilt after every backend change.
- Live QAD validation (7D.11) across the MSA/Neutronics workspace and component `H06-01-6001-33-1`, matching direct ground-truth SQL exactly.
- Full regression verification (7D.12): all of the above re-confirmed after the due-date-boundary removal and horizontal card-layout change, plus live manual desktop regression (workspace load, MPS load, Part Info, Due/Release toggle, horizon change, Falldown, Work Orders drill-down, single-instance enforcement, clean shutdown) verified directly against the running Tauri app with real QAD data.

## Stage 7 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`
- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md`
- `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`
- `docs/reference/KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist.md` (the authoritative checkpoint-by-checkpoint record)
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`

## Stage 7R Amendment (Four-Week Work Order Planning Window)

Stage 7 was **reopened and amended** as Stage 7R, then manually validated and **closed / accepted on 2026-09-01**. The original Stage 7 acceptance (2026-08-13) is preserved as history; this amendment supersedes the specific provisions below. Full detail: `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` section 14.

- **Parent-level Work Orders visibility:** selecting a parent without a bucket now exposes Part Info + BOM + Work Orders. The Work Orders tab shows the complete parent-level planning population (Due-Date-based Falldown + Weeks 0-3 under the active weekly basis). Shortages remains rendered but deferred (disabled).
- **Four-week forward planning horizon:** the Work Order drill-down eligibility horizon is Falldown + the first four forward MPS business weeks (Weeks 0-3) - `WORK_ORDER_DRILLDOWN_HORIZON_WEEKS = 4` (frontend) and `WorkOrderPlanningWindow.ForwardWeekCount = 4` (backend). Weeks 4+ do not present the drill-down. The broader MPS display horizon is unchanged.
- **Population source:** the top-level planning population is sourced directly from `wo_mstr` (parent-scoped), not from MPS-retained Work Order references. The MPS snapshot remains the authority for selected workspace/parent/site, active snapshot generation, the Due/Release weekly-bucket basis, and bucket selection/display.
- **Non-closed status-driven visibility is no longer limited to A/F/R:** any non-closed `wo_status` produces a planning-window card. Unknown non-closed codes are preserved as their raw value and rendered safely (no invented label, no silent drop). This applies equally to top-level parents and manufactured subassemblies reached through the supported Work Order drill-down.
- **`RMABOM` exclusion retained;** Closed Work Orders remain excluded at the query boundary.
- **Falldown remains always Due-Date based:** Falldown membership is always `wo_due_date < current business-week start`, regardless of the active Due/Release basis. Switching basis never changes Falldown membership.
- **Forward weekly Work Order buckets follow the selected Due/Release basis:** Weeks 0-3 membership uses `wo_due_date` in Due mode and `wo_rel_date` in Release mode. The planning population is the union of Due-Date-based Falldown and active-basis Weeks 0-3, deduplicated by WOID.
- **API:** a single planning-window capability serves both top-level views - `GET /api/v1/workspaces/{assignmentId}/work-orders/planning-window` (parent-level, or bucket-filtered via `bucketKind`/`weekLabel`). The previous `GET .../work-orders/bucket` endpoint is retired. The candidates endpoint remains the manufactured-material authorization path for nested navigation and delegates to the same complete planning-window population.
- **Material (bounded treatment):** actual `wod_det` material records are displayed truthfully; an empty `wod_det` yields a truthful empty/unavailable state (not an error, no fabricated requirements). Kitting reports N/A (never a false 0%) when no applicable material exists. BOM-derived projected requirements for unreleased WOs are **not** implemented and are explicitly deferred to Stage 9 design.
- **Manufactured subassemblies:** Work Orders reached through the supported material drill-down use the same complete four-week planning-window population as top-level parents. The material line remains the navigation authorization; no parent/child Work Order relationship is inferred. The nested path no longer uses A/F/R-only candidate visibility or a ten-result cap.
- **Preserved unchanged:** WOID identity; card fields; SO display; actual material calculations; issued/required quantity behavior; manufactured-component navigation; lazy loading; snapshot-aware cache invalidation; stale/refresh behavior; Escape/navigation; read-only QAD access; architectural layering; max drill depth (3); Stage 8 BOM behavior.
- **Verification:** backend build + full test suite + `dotnet format` clean; frontend lint/typecheck/test/build clean; OpenAPI + generated TypeScript regenerated; `cargo check` clean; sidecar rebuilt; project-owner manual validation passed.
## Stage 6 Accepted Behavior

- Selecting an MPS parent focuses the grid to that parent and opens Part Info directly beneath it.
- Clicking the selected parent again or `Back to full grid` restores the full MPS grid.
- Focused mode renders only the selected row and no longer retains excessive blank full-grid height.
- Part Info is part-scoped, not week-scoped.
- Due/Release, horizon, fiscal, density, and presentation changes do not reload PartDetail.
- PartDetail is lazy-loaded and cached by workspace/parent/current MPS snapshot identity.
- Stale-last-good behavior is preserved across relevant refresh/query failure scenarios.
- QAD Part Status is shown as code + backend-owned description.
- Qty On Hand and Qty Non-Net use positive-only, non-RMA nettable/non-nettable inventory rules.
- Pricing uses the most recent `pi_start <= today` and supports one or more MOQ/price tiers.
- Blank/null informational QAD values are acceptable and do not create false error states.

## Stage 6 Verification

- Backend: **316/316 tests passing**.
- Frontend: **119/119 tests passing** after final UI refinements.
- Backend format/build clean.
- Frontend lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean.
- Sidecar rebuilt after backend changes.
- Live read-only QAD validation completed across five available development workspaces and 71 representative parent parts.
- Direct read-only SQL comparisons matched representative PartDetail inventory and pricing results.
- Final Tauri owner-review verified focused-grid spacing, selection-toggle close behavior, Back-to-full-grid behavior, keyboard interaction, and clean shutdown without orphan processes.
- Rare live scenarios not naturally present (multi-tier price breaks, RMA exclusion, no-current-price, additional site/domain) remain covered by deterministic automated tests.

## Stage 6 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`
- `docs/implementation/KST_v2_STAGE_6_CLOSEOUT.md`
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`
- updated backend-boundary and API-contract workflow documentation

## Administrative Follow-Up

Stage 6 final commit hash: `863a638` (`feat: complete Stage 6 part information drill-down`).

## Versioning Foundation

Between Stage 6 and Stage 7, a lightweight application versioning foundation was
established (inter-stage housekeeping, not a Stage 7 activity):

- Product identity `KST v2` remains distinct from the semantic application version.
- Current application version: **`0.1.0-alpha.14`** ([SemVer 2.0.0](https://semver.org/)). During
  numbered alpha development, Stage `N` uses `0.1.0-alpha.N`; Stage 14 is the current stage.
- Historical note: the initial foundation used `0.1.0-alpha.1` and later
  `0.1.0-alpha.2` before the owner adopted stage-aligned alpha numbering on 2026-09-29.
- Authoritative source: `src/backend/Directory.Build.props` (`VersionPrefix`/`VersionSuffix`),
  propagated to the backend (assembly `InformationalVersion`, system status/health endpoints,
  frontend top bar), `src/tauri/Cargo.toml`, `src/frontend/package.json` (full version), and
  `src/tauri/tauri.conf.json` (numeric-only, for MSI/WiX installer compatibility - see
  `docs/development/VERSIONING.md`).
- New tests: 2 backend integration tests (`SystemStatusEndpointTests`), 3
  `Kst.ArchitectureTests` (`VersionConsistencyTests`) guarding future drift. Full backend suite:
  329/329 passing.
- New repeatable sync/check script: `scripts/check-version.ps1` (`-Fix` to auto-correct drift).
- `KstActiveStage` in `src/backend/Directory.Build.props` and the architecture drift guard enforce
  the stage-to-alpha relationship.
- Full documentation: `docs/development/VERSIONING.md`.
- Packaged Windows build (NSIS + MSI) verified successfully with the new version metadata.
- This work does not begin, renumber, or otherwise affect Stage 7.

## R0 — Repository / Documentation Reconciliation Status

- R0.1 — Read-only repository documentation inventory: **COMPLETE / ACCEPTED**
- R0.2 — Authority and contradiction map: **COMPLETE / ACCEPTED**
- R0.3 — Core project-state reconciliation: **COMPLETE / ACCEPTED**
- R0.4 — Stage-history reconciliation: **COMPLETE / ACCEPTED**
- R0.5 — Architecture and development documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.6 — Data/source documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.7 — Documentation navigation and authority: **COMPLETE / ACCEPTED**
- R0.8 — Reconciliation verification / closeout: **COMPLETE / ACCEPTED**

R0 overall: **COMPLETE / ACCEPTED — 2026-08-21.** Full detail:
`KST-v2-Master-Project-Checklist.md` (`## R0 — Repository / Documentation Reconciliation`) and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`.

## Next Action

Stage 19 is **COMPLETE / OWNER-ACCEPTED — 2026-10-13**. Its closeout records acceptance, verification
scope and remaining evidence limits. This closeout authorizes no subsequent stage; the owner must
separately establish the next active stage before new implementation work begins.

**Stage 13 is COMPLETE / OWNER-ACCEPTED — 2026-10-01.** Stage 14 is COMPLETE / OWNER-ACCEPTED —
2026-10-07: Checkpoints 14.1–14.7 accepted (details and owner evidence in the Stage 14 ledger §11 and
§19–§21). Historical checkpoint detail follows.

**Stage 14 owner-desktop findings (2026-10-07).** Initial owner testing passed review/export, Save As,
Keep/Clear and saved-draft persistence but found missing warnings on workspace switch and native close. The
desktop plugin's asynchronous `window.confirm` and the Rust close lifecycle were fixed with an in-app prompt and
close guard (ledger §19.1). The owner later confirmed the desktop checklist and accepted Checkpoint 14.7 and
Stage 14 overall on 2026-10-07. Follow-up fixes added in-app confirmation for Planning draft restore/delete and
Open Orders Save Draft off, plus case-insensitive RA exclusion; the owner confirmed and accepted these on
2026-10-07 (ledger §21). The owner accepts the observed Undo behavior (Undo does not restore discarded
adjustments); it was not changed or re-tested. Automated evidence remains separately recorded in the ledger.

**Stage 14 pricing amendment — OWNER-ACCEPTED — 2026-10-07.** A successful price lookup with no eligible list price uses $0 for Planning MPS $, Adjusted MPS $ and weekly/Falldown/quarter/workspace totals; acquisition failures remain errors. The owner rebuilt the sidecar and verified desktop behavior. This owner evidence is separate from automated test evidence; no agent live-QAD query was performed. The earlier unpriced/blank-total behavior remains only as historical evidence. See ledger §20.

**Stage 14 owner-confirmed evidence register (2026-10-07; ledger §11).** Forecast quantities are positive and use the `FORECAST` selection/reference in `master_plan.py`; v2's detail predicate and `Math.Abs` display transform match that offline source. Successful absence of an eligible list price uses $0; acquisition failures remain errors. Sales-order due dates are required and RA prefixes may vary in case. Sunday-date investigation is unnecessary. The owner confirms the external Mass Updater works with KST output. Existing price-tie behavior is retained, MOQ price switching is deferred, and all relevant prices are USD. These are owner confirmations/reference comparison, not agent-run live-QAD queries.

Stage 15 — Finished Goods: **COMPLETE / OWNER-ACCEPTED — 2026-10-08.**
**15.1 OWNER-ACCEPTED — 2026-10-07; 15.2 offline implementation,
canonical reconciliation and alpha.15 transition AUTHORIZED — 2026-10-07; 15.2 IMPLEMENTED /
OFFLINE-VERIFIED and OWNER-ACCEPTED by explicit owner statement 2026-10-07.**
Owner scope amendment replaces older selected-date/demand/coverage/readiness/summary-card scope with
a workspace-resolved positive current-inventory table: Product, Location, Lot, On Hand, Value;
informational RA lot badge, exact USD domain/part MAX-price valuation and three local literal filters.
Hold annotations removed; order Holds deferred to a later Open Orders enhancement. Refresh uses a
compatible process-local snapshot with acquisition time and explicit stale/error handling.
Stage 21 disposition — 2026-10-14: **no Finished Goods export required; screen copying is sufficient**.
**15.3 AUTHORIZED for Taco normal desktop validation;
bounded Taco QADPRO2/Analysis verification and correction subsequently explicitly AUTHORIZED.**
Case-sensitive adapter site guard corrected after InventoryScopeInvalid evidence: three case-variant rows.
Matching corrected alpha.15 Tauri child loads/refreshed Taco17-parent/16-row scope; ten-field metadata and
exact source multiset comparison pass. Owner confirms visible loading, mixed-case partial filters,
cached timestamp/Refresh, scroll/density/keyboard and compatible workspace/MPS switching on 2026-10-08;
accepts unobserved edge cases as synthetic evidence and explicitly accepts Stage15. Historical artifact
discrepancy and Taco-only/time-bound limits retained; no installer validation or subsequent-stage authority.
See `docs/implementation/KST_v2_STAGE_15_SOURCE_AND_DECISION_LEDGER.md` and implementation plan.
Accepted closeout: `docs/implementation/KST_v2_STAGE_15_CLOSEOUT.md`.

Cross-customer Open Orders remains Stage 18.

Preserve the three untracked owner QXtend templates and the pre-existing local/generated work.
The one-pass read approval has been used; transactional QAD validation queries, QXtend
submission/import, external Mass Updater trials and any post-14.7 Stage 14 work require separate
owner authorization. The Stage 13.6 closeout commit/push is separately authorized as part of its
owner acceptance.
