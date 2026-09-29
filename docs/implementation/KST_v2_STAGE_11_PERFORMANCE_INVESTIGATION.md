# Stage 11 Workspace Shortages — measured performance investigation

## Owner acceptance and final disposition (2026-09-29)

**Stage 11 status: COMPLETE / ACCEPTED / LOCKED.** The project owner completed desktop validation
and accepted Workspace Shortages for report accuracy, display/usability, cold-load performance,
cached performance, and overall readiness. The owner compared the completed report with the full
KSTv1 legacy shortage report. The reports closely matched; minor differences and future fine-tuning
are accepted as non-blocking future-backlog work.

### Owner-observed desktop cold-load measurements

These are real-world owner measurements, not controlled SQL benchmarks. Server load affects elapsed
time and the runs did not control database plans, waits, caches, or concurrent server activity.

| Workspace | KSTv1 runs (seconds) | KSTv2 runs (seconds) | KSTv1 median | KSTv2 median |
|---|---|---|---:|---:|
| Shure / 2140 | 53, 72, 106 | 21, 11, 10 | 72 | 11 |
| Taco / 3230 | 105, 21, 54 | 24, 10, 9 | 54 | 10 |
| MSA/Neutronics / 1391 | 48, 20, 17 | 9, 5, 6 | 20 | 6 |

Across all nine observations, the overall median was **53 seconds in KSTv1 versus 10 seconds in
KSTv2**. The overall average was approximately **55.1 seconds versus 11.7 seconds**, an average
elapsed-time decrease of approximately **79%**. KSTv1 ranged from **17 to 106 seconds**; KSTv2
ranged from **5 to 24 seconds**.

This evidence does not establish a controlled database-performance SLA or a root cause for QADPro2
variation. It does demonstrate that KSTv2 was consistently and materially faster than KSTv1 across
the selected stress, medium, and small workspaces, which is sufficient for the owner’s Stage 11
acceptance.

### Deferred follow-up

Cold acquisition can still vary with QADPro2 server load, but it is no longer a Stage 11 blocker.
The DBA evidence request remains a valid historical and future diagnostic packet and is deferred
unless production use reveals a material concurrency or reliability issue. No database index,
schema, permission, isolation, server-setting, query-hint, or temporary-table acquisition change
was authorized. No further speculative acquisition optimization is required for this closeout.

Row virtualization remains deferred: its prototype lost keyboard focus when a focused row left the
rendered range. Reconsider it only after that focus/accessibility issue is solved. Minor report
fine-tuning is future-backlog work, not unfinished Stage 11 scope.

## Production bounded pass — Stage A/B gate (2026-09-29)

Recorded **before installing Stage C**. Starting uncommitted work is preserved in the
`stage11-before-production-pass` tool checkpoint; no commit or push. The complete projection and
export remain separate from the new immutable `/long-term-shortages/screen` DTO. It carries one
week number/start/Monday-label vector, component metadata and signed opening/safety/KSS/warning,
and confirmed/all mode severity, first-short/risk, maximum episode shortage, first recovery,
raw ending, backend-formatted ending and weekly severity vectors. Read-only collection wrappers
are used at mapping boundaries. Backend-formatted strings avoid matrix decimal-rule reapplication;
raw balances remain available for existing accessibility text and negative-value styling.

`/long-term-shortages/projection-detail` requires workspace, snapshot, component and matching
population/horizon options. It selects cached complete detail through the existing export selection
gate and checks both modes, retained evidence completeness and current snapshot identity. Missing/
replaced snapshot returns 409; missing component/cache/incomplete detail returns 503, with Problem
Details type `urn:kst:shortages:projection-detail-unavailable`. No BOM, MRP, projection build, PO or
comment read occurs. Drawer-only parents/Past/manufacturing lead/manufacturer item come from this
response; PO/comments/master/alternates remain independent. The client ignores late/mismatched
identities and uses the visible report's options while a replacement horizon/population loads.

Stage A/B evidence:
* All 12 retained captures and both modes: every screen field equal; **2,304 filter/sort cases** equal,
  including Show All. Full serialized projections remain byte-identical.
* **24/24 workbook hashes** match retained successful evidence (worksheet/address/value/type/style,
  both modes, all 12 captures), completed before the ledger change. No browser hydration/export read.
* Shure original **18,974,826 → 1,533,731 bytes (91.9% reduction)**. The prototype's 1,246,336 bytes
  did not include backend display strings; the production increase preserves rounding ownership.
  Original smaller captures: 849,156 → 71,704; 952,020 → 78,452 bytes.
* Seven mapping/serialization samples per capture: Shure original warm median mapping **12.63 ms**,
  serialization **8.73 ms**, mapping allocation **3.64 MB**. This is a new after-only run; earlier
  prototype/control measurements remain above/below as historical evidence, not paired speedups.
* Production-path headless Edge Shure replay: transfer **32.1 ms**, parse **17.9 ms**, initial display
  **1,125.5 ms**, 431 rows/14,223 cells; Show All **328.8 ms**, receipt toggle **525.6 ms**.
  Selected captured-detail replay **62.3 ms**; hydration, Escape close/focus and one main request
  passed. This is local captured replay with independent PO/master failures stubbed, not live Tauri.
* Focused backend **53 passed** (32 Domain, 8 Application, 6 QAD, 7 API). Frontend **66 passed**,
  including Edge sticky-header geometry. Final selection-gate/visible-options tightening rechecked
  API **7/7** and panel/geometry **14/14**. Typecheck passed. OpenAPI generated/types synchronized.
* Snapshot race test loads A, populates B, rejects detail A with 409 and verifies unchanged BOM/MRP/PO
  read counts during rejection; absent snapshot/cache/component and incomplete all-mode detail tested.

Retained captures remain the historical 2140/2141/2142 set. New benchmark/owner-validation scope for
this pass is **Shure SMT/2140**, **Taco/3230**, **MSA/Neutronics/1391**. Acquisition redesign remains
blocked on the unchanged DBA request. Virtualization is explicitly deferred (offscreen-focus failure).
At this historical Stage A/B checkpoint, full-suite/sidecar/owner acceptance and Stage C remained
pending. The subsequent Stage C verification and 2026-09-29 owner acceptance are recorded above.

## Production bounded pass — Stage C and final verification (2026-09-29)

Stage C was installed only after the Stage A/B record above. `LongTermShortagesBuilder.BuildBoth`
ports the retained dense prototype, removes the sparse branch, and returns read-only collections.
Application orchestration calls it once for both modes. The previous indexed implementation is
retained as `scripts/stage11-performance/IndexedProjectionReference.cs`, linked only into diagnostic
and test builds. No production sparse path, SQL change, new cache, virtualization or dependency.

### Dense equivalence, operation-order review and measurements

* **168/168** alternating indexed/production measurements (12 captures × seven runs × two variants)
  matched complete retained dual-mode JSON bytes. Each includes all identities, line 2, raw facts,
  planning/release evidence, weekly data and episodes. No comparison normalization or weakening.
* Installed dense output matched **24/24** retained workbook hashes in both modes; together with
  Stage A/B, **48 new production workbook comparisons** passed. Workbook code was unchanged.
* Retained 102-fixture signed/fractional/unknown-safety/locked-oracle suite passed at 13/26/52/72
  weeks against the independent indexed reference. Four new domain theory cases additionally check
  101 fixtures with magnitude/scale-sensitive decimal mixtures and read-only output behavior.
  Locked `74320-27` and `355203-PUR` production tests passed in focused and complete suites.
* Source review compared the actual port with `ProjectionAlternatives.cs` and traced each transition
  against the indexed reference: adjusted opening and initial risk/episode; demand-first low;
  mode-specific recovery/deepest reset; final unresolved episode and stable-clear invalidation;
  summary precedence; release-date accumulation independent of due date. Category sums and combined
  weekly receipt sums accumulate separately in original evidence order. Daily grouped additions and
  `ending + weekly demand - weekly receipts`, followed by daily demand/receipt reconstruction,
  remain exact. Independent-reference automated checks corroborate this review. No separate human
  or second-agent review is claimed; owner review of this sensitive arithmetic remains appropriate.

Warm medians use runs 1–6; run 0 excluded. Alternating measurements share process/input, include both
modes and exclude JSON comparison/serialization. Decimal MB. Workstation variability remains.

| Original retained capture | Indexed ms | Production dense ms | Allocated MB indexed → dense |
|---|---:|---:|---:|
| Shure SMT / 2140 | 446.63 | 128.49 | 242.51 → 37.59 |
| SHU Metals / 2141 | 54.83 | 8.58 | 17.40 → 3.33 |
| SHU Molding / 2142 | 56.97 | 7.09 | 17.50 → 3.25 |

Across the four Shure captures, indexed medians **388.83–446.63 ms**, dense **121.25–140.47 ms**.
This is about 71% less warm execution time and 84.5% less allocation on the original Shure capture.
Taco/MSA have new API/browser captures but no captured raw-input paired ledger benchmark in this
pass; their ledger savings must not be inferred from the historical 2141/2142 scopes.

### Isolated published-sidecar live measurement

One owned published-sidecar process, loopback port 0, MPS prerequisite loaded before each shortage
measurement; first screen request then cache hit, cached projection detail, cached legacy control
payload and selected purchasing. No workspace configuration changes. Workbook generation, tests,
builds and browser replay finished or had not started; no known Kst.Api/Stage11Performance/Excel
process was running beforehand. Only the owned sidecar was stopped afterward. Source bodies/logs
remain ignored and workstation-local (`production-runtime-20260929-192144.jsonl`).

| Workspace | Components | Cold headers / total ms | Cached headers / total ms | Compact bytes | Projection detail HTTP 200 ms | Purchasing HTTP 200 ms |
|---|---:|---:|---:|---:|---:|---:|
| Shure SMT / 2140 | 600 | 77,127.05 / 77,168.22 | 14.01 / 34.98 | 1,532,471 | 22.89 | 121.06 |
| Taco / 3230 | 329 | 61,954.52 / 61,960.49 | 8.38 / 14.33 | 803,671 | 2.56 | 237.81 |
| MSA/Neutronics / 1391 | 212 | 24,081.46 / 24,085.19 | 4.54 / 8.48 | 515,309 | 2.09 | 29.06 |

Same cached acquisition legacy screen bytes → compact: Shure **18,969,372 → 1,532,471**, Taco
**10,192,988 → 803,671**, MSA **6,506,599 → 515,309**. The legacy control was requested after the
timed compact requests, through the existing cache, not by re-reading QAD.

Cold means application-cache miss, not SQL-cache cold. No cache flush or server change. Runtime
logs expose Stage 10 purchasing phases but **not individual Stage 11 BOM/fact/normalization phases**;
these are unavailable in this sample. Server concurrency, waits and plans remain unknown. Normal
OS/background activity was not isolated at operating-system level. One sample cannot prove a stable
cold improvement, and no such claim is made. Earlier 15.1/87.2-second Shure samples remain evidence,
with their original confounders. Cold QAD acquisition remains unresolved pending the unchanged DBA
request; no SQL cause is inferred from elapsed time.

### Production-path browser replay of the new live captures

Fresh-page headless Edge, 1440×900, production profiling React build, loopback replay. Browser is
fed the actual compact DTO directly, with no legacy expansion adapter. Snapshot IDs are substituted
only by the diagnostic replay to its fixed synthetic identity. Successful selected projection detail
is replayed from the same captured complete control; PO/master failures are stubbed. These are
browser measurements independent of the live HTTP measurements above, not totals to add together.

| Workspace | Transfer / parse ms | Initial display ms | Show All / mode toggle ms | Detail replay ms |
|---|---:|---:|---:|---:|
| Shure SMT | 25.7 / 14.1 | 914.1 | 291.4 / 306.7 | 35.4 |
| Taco | 29.3 / 6.1 | 586.5 | 357.0 / 242.9 | 46.0 |
| MSA/Neutronics | 21.7 / 3.5 | 332.4 | 232.8 / 198.2 | 28.5 |

All three: one main-screen request; one selected projection-detail request; successful detail
hydration; Escape close and focus restoration. Shure new capture renders 429 rows/14,157 cells.
Original fixed Shure capture's Stage A/B replay was 1,125.5 ms (431 rows), compared with historical
control 1,938–2,052 ms and diagnostic compact 1,337–1,347 ms. These are separate runs, not a paired
same-session speedup claim. Seven frozen columns/sticky headers/scroll/resize/scaling were verified
by the retained Edge geometry test. Native desktop rendering, full successful master/comment/alternate
latency and screen-reader use still need owner validation. Row virtualization remains deferred.

### Final verification and review boundary

* Focused Stage 11 backend after dense installation: **57 passed** (36 Domain, 8 Application,
  6 QAD, 7 API). Complete backend: **891 passed** (180 Domain, 311 Application, 233 QAD, 158 API,
  9 architecture). Subsequent test-only compact→export assertion passed independently (1/1).
* Focused frontend: **68 passed** in the panel (14), shared modal (46), presentation (3), export (3)
  and selected-detail hook race (2) suites. Includes sticky-header Edge geometry. Existing React
  `act(...)` warnings remain in the panel tests; no test failure. Late A response after B selection
  is ignored; mode/Show All do not re-request projection detail.
* Typecheck and changed-file ESLint passed; production frontend build passed with the pre-existing
  mixed static/dynamic Tauri core import warning. Backend build/OpenAPI generation passed; generated
  TypeScript synchronized. Published/copied sidecar **126,962,282 bytes**, then live-tested above.
* AFT reported zero diagnostics but incomplete authoritative C# coverage; compiler/tests are the
  gate. Working-tree whitespace check passed. Captures/binaries remain ignored; no commit/push.

**Disposition:** this bounded implementation is ready for owner desktop correctness/interaction and
warm-path performance review. **Stage 11 is not ready for unconditional performance acceptance:**
cold acquisition remains tens of seconds in the isolated sample, DBA evidence is outstanding, and
desktop acceptance is the owner's responsibility. Independent human arithmetic review has not been
performed by this agent. No business-rule change, stage acceptance, or cold-performance SLA is claimed.

Owner steps: launch rebuilt desktop; Shure first, then Taco and MSA. Verify Show All/search/planner/
KSS/status and all six sorts, both receipt modes, seven frozen columns/sticky headers/bottom scroll
at normal/resized/scaled sizes. Select components and check parents/Past/manufacturer/lead fields,
separate PO/comment/master/alternates states, Escape/focus, and refresh-to-new-snapshot failure behavior.
Export selected-mode filtered rows directly after loading, before opening drawers, and verify workbook
values/evidence against the same snapshot. Record cold and cached timing separately.

### Files changed by this bounded pass

Paths below are relative to the repository; they describe this pass, not the substantial pre-existing
working-tree changes. The complete task diff was reviewed against the pre-edit checkpoint for the
overlapping service, DTO, drawer and screen-hook files, plus the prototype-to-production ledger diff.

* Backend: `src/backend/Kst.Api/Dtos/LongTermShortageDtos.cs`,
  `src/backend/Kst.Api/Endpoints/LongTermShortagesEndpoints.cs`,
  `src/backend/Kst.Application/LongTermShortages/LongTermShortagesService.cs`,
  `src/backend/Kst.Domain/LongTermShortages/LongTermShortagesBuilder.cs`.
* Backend tests: `src/backend/tests/Kst.Api.IntegrationTests/LongTermShortagesEndpointTests.cs`,
  `src/backend/tests/Kst.Domain.Tests/Kst.Domain.Tests.csproj`,
  `src/backend/tests/Kst.Domain.Tests/LongTermShortages/DenseLedgerEquivalenceTests.cs` (new).
* Frontend: `src/frontend/src/api/client.ts`, `src/frontend/src/api/longTermShortagesApi.ts`,
  `src/frontend/src/hooks/useLongTermShortages.ts`,
  `src/frontend/src/hooks/useLongTermShortageProjectionDetail.ts` (new),
  `src/frontend/src/components/LongTermShortagesPanel.tsx`,
  `src/frontend/src/components/ComponentInfoModal.tsx`,
  `src/frontend/src/components/LongTermShortageDetailModal.tsx`,
  `src/frontend/src/longTermShortages/longTermShortagesPresentation.ts`.
* Frontend tests: `src/frontend/src/components/LongTermShortagesPanel.test.tsx`,
  `src/frontend/src/components/ComponentInfoModal.test.tsx`,
  `src/frontend/src/longTermShortages/longTermShortagesPresentation.test.ts`,
  `src/frontend/src/longTermShortages/compactScreenFixture.test-support.ts` (new),
  `src/frontend/src/hooks/useLongTermShortageProjectionDetail.test.tsx` (new).
* Generated: `docs/openapi/Kst.Api.json`, `src/frontend/src/generated/api.ts`,
  build-updated `src/frontend/tsconfig.tsbuildinfo`; ignored published/copied sidecar artifacts.
* Diagnostics under `scripts/stage11-performance/`: `Program.cs`, `ArchitecturePrototypes.cs`,
  `frontend-entry.tsx`, `frontend-profile.mjs`, `runtime-profile.ps1`, `verify-compact.ts`, `README.md`;
  new `IndexedProjectionReference.cs`, `ProductionVerification.cs`, `frontend-production-entry.tsx`,
  `legacyScreenPresentation.ts`, `production-summary.mjs`, `verify-production.ts`, `verify-production.mjs`.
* Documentation: this investigation, `docs/status/CURRENT_PROJECT_STATUS.md`,
  `docs/implementation/stage11_component_mrp_algorithm.md`. DBA evidence request unchanged.

Historical checkpoint date: 2026-09-29. At this checkpoint, bounded improvements were implemented
and verified and owner review was pending. The later 2026-09-29 owner acceptance recorded above
supersedes that pending status. This historical section is not itself the Stage 11 acceptance record.

**Follow-up architecture/prototype pass:** see the final section below. It supersedes the earlier
unbenchmarked-alternative recommendations and adds a materially slower bounded live cold sample.
Prototypes are isolated diagnostic code; no further production optimization was installed.

## Scope, authority, and reproducibility

Authority: `stage11_component_mrp_algorithm.md`, current project status, `AGENTS.md`, and enacted
security policies. The starting tree on branch `stage11-rebaseline-attempt-archive` at `41fc9b2`
contained substantial pre-existing uncommitted implementation and documentation changes. HEAD alone
is **not** the before implementation. This pass preserved that work. No commit or push was made.

GPT-6 Astra was used. The harness offered no reasoning-effort control; High was not independently
verified. No new dependency, agent capability, database object, index, hint, or server option was added.

Diagnostics: `scripts/stage11-performance/`. Captured inputs, complete original projections,
SQL IO/TIME messages, and browser metrics remain workstation-local under its gitignored `captures/`.
Do not commit source captures. The original domain assembly is retained in `bin/api/` for paired
offline benchmarking. The runner links the actual production endpoint mapper and DTO definitions.

All times below are milliseconds unless stated otherwise. First acquisition means an application
cache miss, **not** an empty SQL buffer/plan cache. Repeat acquisition also re-reads QAD; it is not
a cache-hit request. SQL Server caches were never flushed. Live QAD changed during acquisition:
before/after live row counts cannot establish semantic equivalence. Offline comparisons use exactly
the same retained inputs. SQL diagnostics were separate acquisitions from the application-reader
baseline; do not add their elapsed times to that baseline. Browser results are headless Edge at
1440×900 using a production profiling React build and locally replayed payloads, not live Tauri.

## Baseline and production-reader after measurements

Shure SMT / 2140 is the owner's primary Stage 11 validation workspace and representative worst-case
stress workspace: 38 configured parents, large BOMs and complicated assemblies. The owner reports
that it is historically slower than other product lines, including in QAD. SHU Molding / 2142 is
the currently configured eight-parent mid-sized benchmark, not the historical owner-validation
workspace. SHU Metals / 2141 remains the small-workspace regression benchmark.

The owner observed a substantial Shure SMT desktop-load improvement after the first optimization
pass. This is qualitative owner evidence; no timing measurement is assigned to that observation.

| Workspace | Parents | Unique purchased components | Initial captured MRP events |
|---|---:|---:|---:|
| SHU Metals / 2141 | 3 | 27 | 17,666 |
| SHU Molding / 2142 | 8 | 31 | 15,236 |
| Shure SMT / 2140 | 38 | 600 | 68,481 |

| Workspace | Before universe first/repeat | Before source first/repeat | After universe first/repeat | After source first/repeat |
|---|---:|---:|---:|---:|
| 3 parents | 74 / 31 | 8,836 / 1,099 | 222 / 53 | 3,171 / 1,777 |
| 8 parents | 157 / 104 | 2,760 / 819 | 175 / 97 | 2,069 / 1,154 |
| 38 parents | 4,888 / 989 | 211,057 / 115,141 | 1,861 / 1,605 | 6,160 / 9,133 |

Total acquisition: **215.95 / 116.13 seconds → 8.02 / 10.74 seconds** for 38 parents.
Small totals: 8.91 / 1.13 seconds → 3.39 / 1.83 seconds. Eight-parent totals:
2.92 / 0.923 seconds → 2.24 / 1.25 seconds. The small scopes use identical SQL/batch counts before
and after; their varying acquisition timings are environmental, not credited as optimization.

Scope discovery before these totals was 607 ms large, 38 ms small, 50 ms eight-parent in the first
capture run. It normally precedes shortages through the MPS snapshot. BOM acquisition is sequential,
one read per resolved parent. First large BOM reads included several 0.5–0.7-second calls; repeat
calls mostly took 18–49 ms. Every individual BOM duration is retained by the runner.

**Dominant baseline:** source acquisition, overwhelmingly the large fact SQL batch. Projection,
serialization, and browser work are material warm-path costs but cannot explain a multi-minute read.

## SQL measurements and audit

SQL uses the production builders and `QadConnectionFactory`, preserving READ UNCOMMITTED and no
explicit transaction. SqlClient statistics supply received TDS bytes and server round trips;
bytes include protocol/diagnostic messages, not just value bytes. STATISTICS IO/TIME was enabled
only in the diagnostic connection. Per-query records include SQL CPU, elapsed, table logical reads,
row counts, and bytes. No actual plan was obtained. Initial SHOWPLAN RPC attempts yielded empty
files and a misleading available=true diagnostic; these are invalid evidence. A native text
SHOWPLAN request was denied with **SQL error 262**. Neither privileges nor server settings changed.

### Large workspace, source query groups

| Query group | Before first/repeat ms | After first/repeat ms | Before/after queries | Before/after received bytes, first | Before/after rows, first |
|---|---:|---:|---:|---:|---:|
| Fact + QOH + confirmation | 246,914 / 80,064 | 6,198 / 8,182 | 2 / 3 | 15,910,532 / 15,909,650 | 68,461 / 68,443 |
| Presentation PO/KSS | 2,619 / 1,066 | 1,953 / 5,362 | 2 / 2 | 49,857 / 49,853 | 600 / 600 |
| BOM structural reads | 2,050 / 2,967 | 6,476 / 3,080 | 38 / 38 | 1,713,650 / 1,713,686 | 9,984 / 9,984 |
| Selected drawer PO | 216 / 16 | 94 / 36 | 1 / 1 | 4,907 / 4,901 | 6 / 6 |

Each SELECT was one measured round trip. Normal large acquisition has 42 SELECTs before and 43
after, plus one isolation-setting command per opened connection (38 BOM connections + one source
connection). This excludes MPS prerequisite acquisition and selected-component drawer queries.
Small acquisition has five SELECTs; eight-parent acquisition has ten; both counts are unchanged.

| Large fact SQL table | Before first logical reads | After first logical reads |
|---|---:|---:|
| mrp_det | 89,333 | 442,698 |
| ld_det | 20,924 | 20,807 |
| loc_mstr | 3,313 | 36,372 |
| pod_det | 13,566 | 13,566 |
| pt_mstr | 4,488 | 4,404 |
| ptp_det | 3,902 | 3,818 |
| code_mstr | 3,973 | 3,889 |

Large fact SQL CPU: 4,297 / 3,281 before versus 2,251 / 2,641 after. Server elapsed:
245,743 / 78,738 before versus 5,149 / 8,006 after. The smaller batches improve observed latency
and CPU but increase MRP logical reads about fivefold. This is a disclosed operational tradeoff,
not proof of an optimal or stable server plan. Presentation reads approximately 60,739 pod_det and
118,240 po_mstr pages, unchanged in the large sample.

Small fact query before: 1,170 / 1,687 ms, 17,658 rows, ~4.06 MB, 228,078 mrp_det reads;
after: 5,909 / 2,843 ms, 17,653 rows, ~4.06 MB, 228,045 mrp_det reads. Eight-parent fact before:
1,829 / 1,000 ms, 15,244 rows, ~3.51 MB, ~141,000 mrp_det reads; after: 2,861 / 1,716 ms,
15,240 rows, ~3.51 MB, 140,938 mrp_det reads. These statements were not changed.
Small presentation before 467 / 247 ms; after 1,237 / 1,089 ms. Eight-parent presentation before
884 / 74 ms; after 976 / 202 ms. Full table-level records are retained locally.

### Measured batch alternatives

Same 600-component scope, unchanged SQL text except parameter-list size:

* 100: 8,350 then 3,967 ms; six fact round trips.
* 250: 4,371 then 3,989 ms; three fact round trips.
* Interleaved 250: 6,318 ms; subsequent 500 exceeded the external 600-second deadline. The final
  250 leg was not reached. No diagnostic process remained after timeout.
* Production reader at 250: 6,160 and 9,133 ms including presentation and normalization.

The 60-second SqlClient command timeout did not limit total streaming wall time; the diagnostic
external deadline stopped the stalled run. No production timeout behavior was changed.

### Statement-by-statement conclusions

* BOM `BuildQuery`: parent/domain/effective-date scoped recursive CTE, approved physical relationship
  closure DISTINCT, part/site enrichment and sibling rank. Repeated across parents and may reacquire
  shared subgraphs. Application filters purchased/nonphantom output after traversal; pushing those
  filters into recursion would incorrectly hide descendants. Batch traversal is a larger design.
* `BuildBatchQuery`: bounded component VALUES, domain/site, due OR release before horizon end.
  QOH is SQL-aggregated signed inventory with ld/loc status precedence and RMA exclusions. Wide
  component metadata repeats for every MRP row. PO confirmation is correlated OUTER APPLY per MRP
  row, not a network N+1. MatchCount protects ambiguity. No WO status filter: stale/open WO demand
  remains visible. Identity dedupe remains solely exact source-row ID.
* Dates: no lower bound is deliberate for past-due inventory consumption/evidence. OR release-date
  admission can retain events whose due date is beyond the horizon; planned release evidence and
  cached facts make blanket due-only filtering unsafe. SUPPLYF/unclassified evidence is retained;
  filtering it would change the full evidence contract. Not removed.
* Functions: inventory status trim/upper/coalesce, lot upper, presentation status lower, open-quantity
  arithmetic, and conversion on both PO-line join operands are present. Catalog evidence shows
  `mrp_line` nvarchar(80), `pod_line` int; replacing textual comparison with numeric conversion is not
  established equivalent. Main part/site/domain columns are nvarchar, matching Dapper string
  parameters; no demonstrated ANSI/Unicode mismatch. No speculative predicate rewrite applied.
* `BuildPresentationQuery`: one bounded row per component, separate from MRP to prevent multiplication;
  TOP(1) confirmed conventional PO plus independent KSS set. Repeats some PO work, but KSS remains a
  screen filter and presentation remains workbook evidence. Deferring all of it is not a drop-in change.
* Drawer: `GetPurchasingAsync` validates cached snapshot/component, then one selected-component
  Stage 10 PO read and one component-scoped Shortages enrichment request. It does not re-expand BOM
  or reacquire MRP. Main screen does not issue per-component PO/comment requests. Component master
  detail is a separate existing reader; Approved Alternates activate on expansion.
* Drawer master/planning SQL is domain/part + selected-site LEFT JOIN; cost SQL is domain/site/part,
  Standard simulation, TOP(1) latest date; QCTC is domain/site/part + qtbom_det, TOP(1) latest date
  in Analysis. These are three sequential commands on one connection. Shared drawer inventory is
  its own scoped aggregate query with the established Stage 6 positive-only semantics; it must not
  be substituted for Stage 11 signed opening inventory. No missing scope predicate was identified.
* Comment SQL is a bounded component VALUES set, exact site and active-record filter, ROW_NUMBER
  by modification date, added date, id. No blank-comment filtering is added. The empty supplier
  list used by the shortage drawer means its supplier-risk query does not execute. These detail
  paths were inspected but not separately live-profiled in this pass.
* No evidence of duplicate-result explosion established; no business-value dedupe introduced.
  Join order, cardinality estimates, parameter sniffing, memory grants, and wait types are **unknown**
  without plans/wait evidence. Low CPU versus high elapsed is not enough to name the wait.

## Projection complexity and measured bounded change

Let P = parents, B = total expanded BOM occurrences, C = unique displayed components, E = total
MRP events, W = weeks, D ≈ 7W, L = PO lines. Universe construction is O(B) plus repeated BOM SQL;
parent-reference storage is O(B) worst case. Projection is per unique component, not per parent.
Normalization groups events and sorts by evidence key: O(E + sum E_c log E_c). PO-line detail is
separate, O(L) normalization with SQL sorting. Two independent Build calls each track both balances
but reconstruct selected-mode episodes/severity independently.

Before: daily filtering scans each component's entire evidence D times; weekly summary/low
reconstruction scans it again. O(D·E + W·E + C·D + C log C), with substantial transient collections.
Change: build an ordered `ToLookup` by due date once; daily lookup no longer scans all evidence.
Weekly scans remain, so asymptotic cost is still O(W·E + C·D + C log C), not a new event-stream design.
Addition/summation order, source identities, decimal precision, and weekly reconstruction are preserved.

Paired same-process alternating old/new assembly benchmark, six warm samples after initial pass:

| Scope | Before median ms | After median ms | Approx allocated bytes before/after |
|---|---:|---:|---:|
| 38 parents | 1,006 | 803 | 339.25 MB / 242.52 MB |
| 3 parents | 120 | 74 | 20.07 MB / 17.40 MB |
| 8 parents | 120 | 77 | 21.33 MB / 17.49 MB |

Every paired result matched the original serialized projection exactly. Earlier independent runs
were faster overall (large ~0.65–0.75 s baseline) and are not substituted into this paired comparison.
Process working-set samples ranged roughly 120–670 MB while retaining/serializing captures; these
are not isolated production peak-heap measurements. Total allocated bytes are the stronger comparison.

Additional captured-raw replay: 68,446 rows / 600 components, grouping 11–26 ms; combined production
`ToInput` inventory/MRP/PO normalization 78–239 ms (last three 78–84 ms). Repartitioning those same
component groups into 250-component batches yielded identical normalized input bytes in six runs.
Reflection and pre-timing JSON materialization are diagnostic mechanics; normalization stage totals
are not independently split by inventory versus MRP versus PO. SQL's integer confirmation aggregate
was converted to bool in the replay exactly as Dapper does before calling the production method.

## API and frontend measurements

API opt-in `includeEvidence=false` skips raw-evidence DTO construction and returns empty evidence
arrays with `evidenceIncluded=false`. Default remains true for existing evidence consumers. Both
complete projections and evidence remain cached for exports; display flags do not affect acquisition.
No shortage, episode, weekly, PO presentation, decimal, or acquisition timestamp field was changed.

| Captured scope | Before bytes | Screen bytes | Reduction |
|---|---:|---:|---:|
| 38 parents | 57,450,783 | 18,974,826 | 67.0% |
| 3 parents | 10,763,373 | 849,156 | 92.1% |
| 8 parents | 9,501,465 | 952,020 | 90.0% |

Large raw evidence contributed 38,479,583 serialized bytes. Original large mapping warm samples
~151–229 ms and serialization ~225–522 ms; reduced mapping ~9–66 ms and serialization ~70–82 ms
on the repeat capture after warmup (first reduced run varied 154–318 ms). Small/eight reduced mapping
~0.2 ms and serialization ~2–3 ms, versus ~25–43 ms original serialization. These are separate
microbenchmark runs, not a measured endpoint end-to-end latency subtraction.

| Edge replay | Before transfer / parse | After transfer / parse | Before initial display | After initial display |
|---|---:|---:|---:|---:|
| 38 parents | 904 / 297 | 470 / 99 | 2,915 | 2,196 |
| 3 parents | 232 / 66 | 49 / 5 | 570 | 418 |
| 8 parents | 124 / 50 | 51 / 4 | 429 | 319 |

Transfer includes fetching/text decoding from the local replay server. Initial display includes
transfer, parse, React commit, layout and frame scheduling. Layout-only forced-read observations were
usually ~0–0.6 ms after the browser had already laid out; one small run forced 166 ms. They are not
complete layout-trace durations. Filter construction on a first locale-sensitive call was 29–46 ms.

Large initial React commit: 240 ms before, 223 ms after (no claim of meaningful initial-render gain).
Rows are now memoized with stable selection callbacks, and filtered rows are memoized. Drawer error
path React work: ~199 ms before, ~12 ms after; updates still occur but unchanged rows bail out.
Show All React work: 150 → 84 ms. Main fetch count remained one across Show All and drawer opening.
Three initial commits (mount/loading/data) occurred. Large grid still mounts all 431 default rows /
14,223 cells; Show All mounts 600 rows. Offscreen virtualization is deferred, not claimed complete.
Sticky-column geometry/scroll/resize tests passed in Edge. Drawer timings above use stub failures and
include a deliberate 100-ms wait; they are not successful live drawer latency measurements.

### Published sidecar live HTTP follow-up

After rebuilding, `runtime-profile.ps1` launched the published executable on an OS-assigned loopback
port, preloaded MPS, then measured a first screen GET and identical cache-hit GET. It did not edit
workspace configuration and stopped its owned process afterward. HTTP bytes are read completely
before stopping the timer; browser parsing/rendering is excluded.

| Scope | Cold headers / total ms | Cache-hit headers / total ms | Screen bytes | Selected purchasing HTTP 200 ms |
|---|---:|---:|---:|---:|
| 38 parents | 14,821 / 15,138 | 12 / 243 | 18,969,718 | 208 |
| 3 parents | 3,495 / 3,864 | 3 / 12 | 849,064 | 30 |
| 8 parents | 2,814 / 2,825 | 3 / 25 | 951,913 | 29 |

These are after-only live Kestrel timings, not paired before/after endpoint measurements. Purchasing
200 confirms the endpoint completed; optional comment availability was not independently recorded,
and component-master/Approved Alternates endpoints were not included. Live rows differ from the
offline capture because QAD continues updating. Full desktop click-to-paint latency remains unmeasured.

## Alternatives and follow-up design packet

| Design | Complexity / expected effect | Correctness / contract / freshness | Evidence and scope |
|---|---|---|---|
| Dense daily aggregates, dual-mode ledger | O(E + C·D), O(C·D) aggregate storage; one classification and weekly accumulation | Preserve source order/decimal sums, demand-first lows, mode-specific episodes and rounding; no API change required; same acquisition key | Current paired lookup saves ~20% large projection and ~97 MB allocation; full dual-pass redesign unbenchmarked. Domain-focused medium change. |
| Sorted sparse event stream, dual-mode state + weekly boundaries | O(sum E_c log E_c + E + C·W), O(E + C·W); avoids empty-day work | Correctly carry opening/risks through empty days, split release/due dates, maintain earliest/deepest/stable-clear; no contract change necessary, same freshness | No prototype benchmark yet; justified candidate for XHigh evaluation, not selected implementation. Higher correctness risk than dense days. |
| Split component metadata/QOH from narrow MRP/confirmation result sets | Reduce repeated metadata bytes and join width; batching/selectivity determines SQL cost | One connection READ UNCOMMITTED, completeness checks per section, exact row identity and ambiguous confirmation handling; internal acquisition contract changes | ~15.9 MB received versus ~68k rows; current 250 batches fast but MRP reads ~5×. Requires DBA plans/waits plus captured raw replay. |
| Summary screen DTO + cached detail endpoint | O(C·W) screen, evidence only on demand; smaller than current 19 MB | Preserve full export/cache, add snapshot-scoped detail contract; prevent accidental reacquisition or stale fallback | Raw evidence deferral measured 67–92% byte reduction; full weekly/mode flattening not benchmarked. Cross-layer design. |

Set-based multi-parent BOM versus repeated reads: viable but requires preserving closure identity,
phantom traversal, per-parent ancestry and effective-date rules. Current BOM cost is far below the
original fact-query stall. Same-connection temporary component tables are design-only: no temp table
or insert was executed under this read-only pass. Any such approach needs policy/DBA review and must
not be treated as implicitly authorized. Compatible reads may share result sets but should not be
joined merely to reduce round trips; MRP × PO multiplication would violate source identity semantics.

No new cache was introduced. Existing backend keys workspace/snapshot/local date/schedule version/
population/horizon, and shorter horizons reuse retained facts; frontend local filters avoid acquisition.
Existing unbounded dictionaries and multiple horizon projections warrant a separate bounded eviction
design if measured retention grows. Do not add TTL/fallback or reuse across snapshots for speed.

### Index recommendations — separately deferred

No index DDL is recommended for execution yet. Request DBA review of existing indexes and actual
plans/waits for mrp_det domain/site/part with due/release access, and pod_det domain/site/part/nbr/line
confirmation access. Evidence: 140k–443k MRP logical reads and large elapsed/CPU divergence; correlated
confirmation line conversion; presentation ~179k combined PO/header reads. Potential benefit is
selective seeks/less scanning and narrower access. It cannot be quantified without current index
definitions/plans. Risks: storage/write amplification on the Pro2 replication target, maintenance,
plan changes, and vendor support. Do not create duplicate or wide covering indexes from this report.

**Astra XHigh pass is justified**, focused on acquisition design and a smaller immutable screen
contract, with dense versus sparse projection designs evaluated on captures. It should not assume
the projection algorithm caused the original delay. Obtain DBA plan/wait evidence first for SQL
architecture decisions; no stable-plan or parameter-sniffing conclusion is established here.

## Correctness and verification

* All six original captures: byte-for-byte complete dual-mode projection equivalence, including
  raw evidence, source line2/row IDs, episodes, weekly values, rounding inputs and classifications.
* Original three scopes, both modes: every used workbook cell's value/type/address/style hashed
  identically before/after. ZIP bytes intentionally not compared because package metadata can vary.
* Locked 74320-27 and 355203-PUR regression tests pass, including actual September 24 cutoff,
  opening -63,146 / -1,206, demand-first low -123,723 and recovered-but-Critical classification.
  These named fixtures are the historical oracles; they are not assumed present in today's scope.
* New API test compares every non-evidence row field, verifies cached export retains source row ID,
  and confirms one source read. Batch test checks exact partition/parameter coverage of 600 parts.
  `verify-payload.mjs` also deep-compared every retained API field for all three complete captured
  responses; only explicit evidence omission and its flag differ.
* Full backend: **886 passed** (176 Domain, 311 Application, 233 QAD, 157 API, 9 architecture).
  Focused Stage 11 prior to final batch-test addition: 51 passed. Full suite includes the added test.
* Focused frontend: **15 passed**, including real Edge sticky-header geometry and selected-component
  purchasing/filter/receipt-mode/horizon/export regressions.
* Typecheck passed; changed-file ESLint passed via direct installed CLI after an npx wrapper timeout.
* Production frontend build passed. Existing mixed static/dynamic Tauri core import warning remains.
* Backend build passed; OpenAPI and generated TypeScript synchronized. Sidecar rebuilt successfully
  through `scripts/build-sidecar.ps1` (126,933,610-byte published/copied executable).
* AFT reported no errors but incomplete C# diagnostic coverage; compiler/tests are the verification gate.
* Performance-only diffs were inspected against pre-edit checkpoints. Whitespace check passed with
  Windows CRLF interpretation; disabling autocrlf initially produced CR-at-EOL false positives.
  Git status confirms diagnostic captures/builds are ignored and only runner source is untracked.

### Measurement gaps and owner review

The investigation did not independently isolate inventory versus MRP versus PO normalization within
the combined production reader, isolate successful live comment/master-detail drawer latency,
capture paired before/after Kestrel totals, capture complete browser layout traces, or isolate
production peak memory. Connection-setting round trips are derived from the verified factory, not
included in SELECT-only statistics. Full live Tauri end-to-end totals are not asserted by adding
separately measured phases. Actual/estimated plans and server waits remain unavailable. These gaps
must remain explicit; the report establishes bounded improvements, not completion of all profiling.

Owner checks: launch rebuilt desktop; load Shure SMT first, then the mid-sized and small scopes;
verify Show All/receipt mode/search/sort stay local, drawer selection and Escape focus work, horizontal
scroll/sticky headers hold, and exported current snapshot matches displayed selected-mode balances.
Review the 250-batch latency/read-cost tradeoff with DBA/IT only if future production evidence
warrants investigation. Stage 11 manual acceptance is complete; the DBA packet is retained and deferred.

## Architecture and prototype follow-up — 2026-09-29

### Scope and evidence discipline

This pass preserved the substantial starting working tree, including the first optimization pass.
Added diagnostic C#/TypeScript sources under `scripts/stage11-performance/`, corrected workspace
identity/status text, and prepared `KST_v2_STAGE_11_DBA_EVIDENCE_REQUEST.md`. No commit/push, new
dependency, production SQL change, production projection change, or application DTO change.
The prototypes are not imported by the application. Cached exports retain complete evidence.

All **12** retained input captures were used (original/repeat and after/after-repeat for each of
2140/2141/2142). There are 252 alternating projection measurements (three variants × seven runs ×
12 captures) and 168 paired DTO measurements (two shapes × seven runs × 12 captures). Each complete
projection was compared as serialized bytes to its retained baseline. Both receipt modes are included
in each measurement. Run 0 is first invocation for that capture, not process-cold: other captures and
production reference generation may already have warmed the process. Runs 1–6 provide warm medians.
No server cache flush. Decimal MB means 1,000,000 bytes. Workstation timings are samples, not SLAs.

### Acquisition architecture and SQL tradeoffs

**Measured fact:** smaller batches reduced observed CPU and elapsed while increasing page reads in
the first pass. **Inference:** total logical reads alone is not a sufficient latency predictor for
these executions. **Unknown:** the mechanism. Plans, row estimates, waits, memory grants/spills,
compilation/reuse and competing replication workload were not available. No claim about join order,
parameter sniffing or stable plans follows. The new cold endpoint sample below further prevents
calling the 250-component ceiling optimal or consistently fast.

The captured raw-row split prototype stored component metadata/signed QOH once and narrow event
fields separately. It reconstructed **68,446 rows byte-value-equivalently** (JSON structural equality,
preserving array order and each source field); all repeated metadata agreed for the 600 components.
Serialized raw diagnostic JSON fell **44,587,569 → 18,699,567 bytes (58.1%)** with 600 metadata rows
plus 68,446 narrow rows. These are JSON shape bytes, **not** measured TDS bytes, and splitting was
offline: no SQL reads/CPU/elapsed savings are asserted. No business-value or source-ID dedupe was
introduced by the split. Confirmation remains per event in this first prototype.

Here Bf = ceil(C/250), Bp = ceil(C/configured presentation batch size); for the retained large scope
Bf=3 and Bp=2. Counts exclude MPS prerequisites and the factory's isolation-setting command.

| Option | SQL round trips / returned rows and bytes | Reads, CPU, elapsed evidence | Replica/concurrency and authorization | Contract, completeness and correctness |
|---|---|---|---|---|
| Current 250 VALUES batches | Bf+Bp=5 fact/presentation SELECTs, plus 38 BOM SELECTs. Prior sample ~68,443 fact rows / 15.91 MB TDS, 600 presentation rows / ~50 KB | Prior fact reads 442,698 MRP; CPU 2,251/2,641 ms; elapsed 5,149/8,006 ms. New endpoint total is not a fact-query measurement | No extra authorization for existing reads; fivefold MRP read cost can matter under concurrency. No concurrency benchmark | Existing complete source sections, signed QOH, identity/confirmation guards and complete export cache; retain as measured interim choice |
| Split metadata/QOH + narrow MRP + separate presentation | Separate commands: Bm+Bf+Bp, e.g. 7 with metadata at 500. Batched multiple result sets could reduce command trips to 5 but need section validation. Rows C+E+C; offline shape bytes above | Live logical reads/CPU/elapsed **unmeasured**; smaller transfer does not prove fewer reads | Short same connection, READ UNCOMMITTED; independent sections widen observation interval. DBA plan/load evidence before production selection | Internal adapter contract only; exact scope coverage for metadata/presentation, explicit event-section completion, empty successful events allowed, missing sections fail closed. Preserve due OR release, no lower bound, SUPPLYF evidence and row identities |
| Confirmation lookup separated from narrow events | Potential extra bounded commands per distinct PO-line key; key-result rows plus E events, not E×PO rows; bytes unmeasured | Unknown; reduction in repeated confirmation work is a hypothesis | Must bound parameter count/round trips; same consistency; DBA comparison needed | Preserve textual conversion semantics and MatchCount ambiguity. Missing/ambiguous required confirmation fails closed. Do not assume conventional-open PO population includes all receipt-confirmation keys |
| Alternative parameter transport | VALUES current Bf; JSON/XML one parameter per batch may reduce request bytes, not inherently trips or result bytes. Existing TVP possible only if supported | Unmeasured; compatibility, estimates and plan effects unknown | Confirm server version/compatibility. New TVP type requires schema authorization; JSON/XML parsing adds server work. No live prototype | Exact component scope and duplicate parameter semantics must survive encoding; parameterized, no interpolation of raw SQL values |
| Same-connection temporary component table | Setup/load/read/cleanup commands; count depends on bulk/parameter transport; result rows same as split | All metrics unmeasured; tempdb cost unknown | **Design only**. Needs explicit owner/security-policy and DBA authorization for temporary CREATE/INSERT, cancellation cleanup and replica constraints | Session-bound lifecycle; never reuse across requests/snapshots; loss of session or incomplete load fails closed |
| Reduce duplicated PO work | Combined command with independent result sets could reduce trips; potential shared key acquisition must include both calculation and presentation populations | Prior presentation ~60,739 pod_det +118,240 po_mstr reads. Savings unmeasured | Broader PO scans or materialization might offset savings; DBA plans first | Receipt confirmation and earliest conventional confirmed PO/KSS have different predicates. Never join independent one-to-many sets, never use Stage 10 open-only PO lines as all confirmation evidence |
| Snapshot-scoped lazy detail | Initial screen emits no detail rows. Existing selected purchasing path: one component PO read plus optional comment read; cached projection detail requires zero QAD trips | Offline screen/detail and live purchasing measurements below; no complete successful live drawer total | Low bounded selection load; no cross-snapshot fallback/new unbounded cache | Reuse GetCachedForExportAsync selection gate for projection detail. Cache miss/changed snapshot returns unavailable/conflict, never BOM/projection reacquisition. Keep full evidence for exports |
| Set-based multi-parent BOM | Candidate one command per bounded parent batch versus 38; output still per-parent closure occurrences; bytes unknown | Prior BOM timing variable, below original fact stall; new implementation unmeasured | Larger recursive query can increase replica cost. Lower priority, needs plan evidence | Preserve effective dates, phantom traversal, per-parent identity/ancestry, purchased selection after traversal. No inferred graph dedupe |

The DBA packet requests existing index definitions, comparable 250/larger plans and parameters,
query waits, grants/spills, Pro2 write-load constraints, temp-table acceptability and vendor support
for any proposed index. No unauthorized mutation, hint or consistency change was attempted.

### Immutable screen contract prototype

`ScreenResponse` retains snapshot ID, refresh date, acquisition timestamp, consistency, stale/warning
metadata and one shared week-start vector. Each `ScreenComponent` contains component/UOM/status/
description/planner/buyer-code, signed opening, safety stock, warning and KSS once. Two `ScreenMode`
values carry severity, first-short/first-risk, maximum episode shortage and first episode recovery
(the exact existing sort keys), parallel weekly ending and severity arrays. C# records hold read-only
collections. No frontend business projection or decimal re-evaluation is needed to switch modes.

Raw evidence, full episodes, parent lists, past details, planning/descriptive detail and PO context
leave the initial screen. Weekly lowest/planning/receipt evidence remains in the complete cached
projection; the present grid renders ending balance/severity only. If weekly detail is restored to
the screen, extend the screen contract deliberately rather than silently filling missing fields.
Selected projection detail should be mapped through the **existing snapshot cache selection path**
used by exports/purchasing; extend a component-specific endpoint with snapshot detail, independently
of optional PO/comment availability. The current purchasing endpoint does not already return that
projection detail, so this requires an explicit API change in a future pass. Component master and
Approved Alternates remain the established selected-component endpoints. Export never depends on
the reduced DTO, browser hydration or fresh QAD reads.

| Original capture | Current bytes → compact bytes | Current → compact warm mapping ms | Current → compact warm serialization ms | Mapping allocations |
|---|---:|---:|---:|---:|
| Shure SMT 2140 | 18,974,826 → 1,246,336 (93.4% reduction) | 26.46 → 8.48 | 94.27 → 10.27 | 11.06 → 2.03 MB |
| SHU Metals 2141 | 849,156 → 58,184 (93.1%) | 0.298 → 0.182 | 4.18 → 0.492 | 0.499 → 0.092 MB |
| SHU Molding 2142 | 952,020 → 63,031 (93.4%) | 0.363 → 0.155 | 3.60 → 0.478 | 0.570 → 0.105 MB |

First invocation mapping/serialization for these captures: large current 27.06/99.38 versus compact
3.49/14.48 ms; small 0.267/3.77 versus 0.219/0.425; mid-sized 0.258/3.02 versus 0.130/0.344.
Across the four large captures, warm mapping medians were 25.79–36.78 versus 8.48–18.02 ms;
serialization 82.25–263.40 versus 10.27–17.21 ms. Do not hide the first capture's slower serializer.
All 12 captures passed every screen-field comparison and **2,304** filter/sort result comparisons,
covering both modes, six sorts, Show All, KSS, status, component and planner filtering. The browser
adapter is diagnostic compatibility code; its selected-detail hydration is not a production immutable
client implementation. This is the reason to land the real screen contract as a bounded cross-layer
change with dedicated endpoint/missing-cache tests, not ship the diagnostic adapter.

### Dense versus sparse projection

Both prototypes compute confirmed/all ledgers in one pass, classify/copy evidence once, preserve
original-order daily category sums, and independently accumulate weekly sums in evidence order.
Weekly lows still reconstruct from ending + weekly demand − weekly receipts using the exact original
arithmetic grouping. Tracking a forward minimum instead could change decimal summation behavior.
Release-date evidence is accumulated independently of due-date inclusion. Every source fact remains
in output. Episode state, demand-first lows, earliest/deepest/recovery/stable-clear and evaluation
rounding remain mode-specific. No source filtering/dedupe is performed by the algorithms.

Dense allocates a daily totals array and visits every ledger day: O(E+C·D), plus final row sort.
Sparse accumulates per-date totals, then sorts event-day indices with asOf, week ends and next-day
observations. The next-day observation preserves existing behavior even with negative receipts;
empty spans are otherwise skipped in the main ledger. Weekly-low reconstruction visits at most
seven days per week: O(E + sum Kc log Kc + C·W), Kc ≤ event dates plus boundaries, with a constant
seven-day weekly loop. Thus it is a sparse aggregated-date stream, not a claim of zero empty-day
operations anywhere. Evidence remains in original order. Dense is the simpler maintenance candidate.

| Original capture | Indexed warm ms / allocated MB | Dense warm ms / allocated MB | Sparse warm ms / allocated MB |
|---|---:|---:|---:|
| Shure SMT | 604.89 / 242.51 | 196.68 / 37.55 | 124.47 / 27.32 |
| SHU Metals | 78.09 / 17.40 | 10.12 / 3.33 | 13.85 / 3.22 |
| SHU Molding | 54.75 / 17.49 | 10.62 / 3.25 | 5.34 / 2.93 |

First invocation indexed/dense/sparse: large 621.97/190.91/105.61 ms; small 69.48/5.57/10.41;
mid-sized 61.66/7.90/4.44. Across all four large captures, warm medians indexed 555.11–643.42,
dense 132.72–196.68, sparse 124.47–167.84 ms. Sparse did not win every capture; allocation reduction
is the clearer consistent signal. These paired current-control values supersede comparisons to the
earlier 803-ms run for this experiment; no speedup is computed across different benchmark sessions.

All 252 complete dual-mode projections matched retained bytes. **48 workbook comparisons**
(12 captures × two alternatives × two modes) matched every used cell's worksheet/address/value/
type/style hash. Initial workbook run reached its 15-minute external deadline after 29 checks;
the remaining 19 were resumed without changing the algorithm/export implementation and passed.
The locked 74320-27 and 355203-PUR fixtures plus 100 deterministic synthetic signed/fractional/
unknown-safety/multi-mode cases matched production at 13/26/52/72 weeks. These synthetic tests are
equivalence stress data, not invented business expectations. Production named regression tests pass.

**Selection:** retain indexed-daily in production for this architecture pass. Dense is recommended
for the next bounded implementation because most allocation savings come without sparse skipping's
additional observation/focus on boundary cases. Sparse remains a measured optional refinement.
Neither prototype has replaced the production builder and therefore neither has passed the complete
backend suite as its installed implementation. Before replacement: wire the chosen builder into
application dual-mode orchestration, run the entire suite against it, retain exact decimal grouping,
repeat capture/workbook comparisons and independently review state transitions. Sub-second projection
savings cannot resolve the new multi-second/minute cold variability by themselves.

### Frontend evidence and virtualization disposition

Same headless Edge profiling build and captured payload, now with an explicitly viewport-bounded
panel for both variants. Large runs were control→compact, then compact→control. First display is
fresh-page replay, not live desktop and not warm backend. Both modes remain local.

| Scope/variant | Transfer / parse ms | Initial display ms | Selected cached-detail replay ms |
|---|---:|---:|---:|
| SMT current, two runs | 510.1/91.7; 349.8/87.3 | 2,052; 1,938 | not applicable |
| SMT compact, two runs | 64.9/12.0; 35.6/13.3 | 1,337; 1,347 | 8.3; 8.4 |
| Metals current → compact | 35.5/3.5 → 65.3/0.7 | 306 → 339 | 18.1 |
| Molding current → compact | 38.4/4.0 → 31.3/0.7 | 313 → 269 | 10.1 |
| SMT compact + virtual prototype | 108.1/15.6 | 494 | 133.4 |

Compact expansion added ~9.9–11.8 ms large and 1.5 ms small/mid-sized; included in initial display.
Small-scope timing noise means no consistent small-workspace display improvement is claimed.
Detail replay is an in-memory captured component lookup served locally, not a live cache benchmark;
it does not rebuild a BOM or projection. PO/master failures remain stubbed in these browser runs.

The unchanged grid still mounts 431 default rows/14,223 cells. Virtual prototype mounted 37 actual
rows plus one spacer (1,222 cells including spacer), Show All ~53 ms versus ~421–531 ms compact,
and receipt-mode update ~68 ms versus ~399–444 ms compact. Seven frozen-column/header alignment,
horizontal overflow and resize/1.25×/1.5× geometry checks passed. Normal selected-row Escape close
and focus restoration passed. **Offscreen focus did not survive scrolling a focused virtual row
out of the mounted range.** The diagnostic fixed-height assumptions, keyboard traversal through
unmounted rows, accessible row counts/indexes, tooltip-focus pinning and full bottom-scroll behavior
need further design/manual verification. Do not ship virtualization. The initial missing-React-import
prototype failed to load, was corrected, and the recorded successful run above followed.

Existing memoized rows/stable selection callback already isolate unchanged row renders during drawer
updates (~8–10 ms combined React work in these replays). A further drawer-container split is lower
priority. Compact payload alone materially improves initial load, but does not remove the ~400-ms
large Show All/receipt-toggle work. Frontend work remains worthwhile after the compact contract lands;
measure virtual focus pinning/roving navigation before implementation approval. Filter/sort semantics
were exhaustively compared offline; full keyboard navigation and screen-reader equivalence were not
established by the geometry prototype.

### Bounded live sidecar recheck — cold variability remains

Rebuilt published sidecar; existing runtime harness opened an owned loopback process, loaded MPS,
then requested shortages cold and cached, followed by one selected purchasing request. Unique logs
preserve earlier evidence. No workspace configuration changed. This measured the **current production
implementation**, not the prototypes. Local workbook verification overlapped part of the run;
that is a confounder, and the acquisition phases/server waits were not isolated in this recheck.

| Workspace | Cold headers / complete HTTP ms | Cache-hit headers / complete HTTP ms | Bytes | Purchasing HTTP 200 ms |
|---|---:|---:|---:|---:|
| Shure SMT | 86,811 / 87,234 | 17 / 341 | 18,969,402 | 323 |
| SHU Metals | 21,010 / 21,408 | 3 / 15 | 849,046 | 57 |
| SHU Molding | 22,996 / 23,008 | 3 / 12 | 951,928 | 48 |

Earlier SMT 15,138-ms cold/243-ms cached is retained as a real earlier sample, **not a repeatable
guarantee**. The recheck shows cold variability across all three scopes. It does not establish a
250-batch regression, server wait cause or SQL plan cause. No synthetic full-desktop total is formed
by adding these HTTP samples to browser replay. Complete successful live drawer (master, optional
comment availability and alternates) and live Tauri click-to-paint remain unmeasured. The owner’s
qualitative substantial SMT improvement remains valid evidence, separately from timing samples.

### Verification and remaining gates

* Complete retained projection bytes: 252/252 matched; 48/48 workbook hashes matched.
* Locked named cases and 102-fixture prototype equivalence at all four horizons passed.
* Focused backend: 52 passed. Complete backend: **886 passed** (176 Domain, 311 Application,
  233 QAD, 157 API, 9 architecture); this verifies the current production implementation.
* Focused frontend: 60 passed initially, one Edge launch timeout; isolated unchanged sticky-header
  geometry test then passed in 3.8 seconds. Existing React `act(...)` warnings were emitted.
  The npm wrapper rejected `-t`; the installed Vitest CLI ran the isolated check successfully.
* Production typecheck/build passed. Standalone prototype TS/TSX typecheck and changed-file lint
  passed after replacing explicit `any` annotations. No dependency or generated-contract change
  is needed for an offline DTO. Production build retains its existing mixed Tauri import warning.
* Sidecar rebuilt/copied successfully: 126,933,610 bytes. Builds regenerated current OpenAPI;
  the application shape is unchanged in this pass. Generated TypeScript synchronization checked.
* AFT has incomplete C# coverage; one inspect was interrupted and another failed during dead-code
  aggregation. Compiler/tests, not incomplete AFT output, are the correctness gate.
* Manual Shure SMT desktop validation was not performed by the agent; the accessible test surface
  was headless Edge plus published-sidecar HTTP. Owner acceptance remains pending.
* Final task diffs were reviewed against pre-edit checkpoints; working-tree whitespace check passed
  with CRLF-aware interpretation. Captures and diagnostic binaries remain ignored. No owned
  diagnostic/sidecar process remained. All substantial pre-existing uncommitted work remains.

**Recommendation:** Stage 11 is ready for continued owner correctness/interaction validation using
Shure SMT first, but **consistent cold-load performance is not established as acceptable**. Another
bounded implementation pass is warranted: (1) production compact screen DTO + snapshot-only component
detail with cache-miss/race tests and generated contracts, (2) dense dual-mode ledger integration with
complete-suite/evidence gates, (3) virtual-row keyboard/focus/accessibility prototype before any
shipment. In parallel, provide the concrete DBA packet and measure acquisition separately under an
agreed replica load window. Do not choose a new acquisition design or index without that evidence.
Owner should validate Show All, both modes, search/sorts, selected drawer/first-short/Past values,
Escape/focus, sticky columns, bottom horizontal scrolling/resizing/scaling and selected-mode export
against the current snapshot. No acceptance or timing SLA is implied by this recommendation.
