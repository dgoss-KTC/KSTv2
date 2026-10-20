# Stage 15 — Finished Goods: accepted source and decision ledger

## S22-009 pagination removal — owner-accepted / closed 2026-10-20

Owner reports pagination removed and all working well; accepted 2026-10-20. Only the accepted
pagination presentation is superseded: all matching acquired physical rows render in retained order through normal scrolling,
without page controls/slicing/state or reading the old size preference. Filters/facets, exact values,
source/acquisition/safety boundaries and no-export scope remain unchanged. Historical decisions and
acceptance below are preserved. Owner-reported behavior, independently inspected current artifact
identity and guarded-build/provenance limits remain distinct. Combined acceptance/evidence:
`KST_v2_S22_005_009_UNPAGED_TABLES.md`. Stage 22 remains open.

## S22-009 presentation/filtering amendment — 2026-10-19

Owner authorizes replacing separate Product/Location/Lot Contains inputs with shared-editor header
funnels: Values / literal case-insensitive Contains for those columns, exact numeric conditions on
existing On Hand/Value strings. Complete loaded-report AND filtering/cascading facets precede paging;
100/500/1000 replaces fixed 100, with a separate guarded size-only preference. This explicitly amends
the older filter/paging presentation below, not inventory population, row identity/order, valuation,
price absence/error, source reads or acquisition/scope/stale semantics. No export. Implementation and
48 passing offline frontend tests recorded in `KST_v2_S22_009_FINISHED_GOODS_UI.md`. On 2026-10-19
the owner reviewed/accepted styling, all five filters, cascading counts, exact-decimal filtering and
paging; **S22-009 COMPLETE / OWNER-ACCEPTED / CLOSED**. Owner-run visual/interaction evidence is
distinct from retained offline tests; closeout reuses verification and authorizes one local commit,
not a push. Stage 22 remains open. Original Stage 15 acceptance below is preserved.

## Final owner acceptance — 2026-10-08

**Stage15 COMPLETE / OWNER-ACCEPTED.** Owner explicitly confirms visible Finished Goods loading,
mixed-case partial filtering, cached module return retaining acquisition time and Refresh updating it,
scroll/density/keyboard usability, and workspace/MPS switching without incompatible data. Owner accepts
live-unobserved fractional/missing/negative/all-null/duplicate-exact-tuple/fault cases as **synthetic
evidence**, not invented live passes, and explicitly accepts Stage15 overall. These are owner-run
observations; source/API evidence remains separately attributed. Earlier pending statements below are
historical and superseded by this acceptance. Stage21 disposition pending; no Git/next-stage authority.
See `KST_v2_STAGE_15_CLOSEOUT.md` for accepted evidence and retained limits.

2026-10-07: **15.1 and 15.2 OWNER-ACCEPTED by explicit owner statement; 15.3 AUTHORIZED.**
Stage 15 overall acceptance remains pending. Standalone reconciliation requires concrete query approval.

**2026-10-07 correction authorization/result amendment:** owner authorized bounded Taco verification,
safe diagnostics and fix work. InventoryScopeInvalid proved case-sensitive reader site equality rejected
three case-variant rows admitted by source CI collation. Reader domain/site guards now OrdinalIgnoreCase;
canonical context emitted, raw location/lot/reference unchanged. Corrected Tauri child alpha.15 acquired
16 rows/17 loaded parents and successfully refreshed. Ten targeted metadata fields and exact multiset
reconciliation pass for Taco only. Desktop visual/interaction owner evidence remains pending; Stage15
not owner-accepted. Details and runtime hashes in scratch STAGE_15_3_VALIDATION_REPORT.md amendment.

## Authority and provenance

Owner decisions in the 2026-10-07 Stage 15.2 authorization supersede proposals in
`C:\Dev\kst_v2_tmp\Stage15\STAGE_15_1_SOURCE_AND_DECISION_LEDGER.md` and
`STAGE_15_IMPLEMENTATION_PLAN.md`. Those offline discovery artifacts remain provenance.
Stage 14 through 14.7, pricing amendment and follow-ups remain accepted at `230c60d`.
Stages 9–11 and `PartInventorySummary` remain unchanged.

Evidence labels: **owner-decided**, **repository/static**, **synthetic/offline**, **mocked**,
**historical live-read**, **owner-run**, **unverified**. No Stage 15 live-source evidence exists yet.

## Accepted mapping

| Requirement | Source / rule |
|---|---|
| Population | Already-loaded `MpsSnapshot.ResolvedParts.ParentPart`, assignment Site; existing `QadSiteDomainMap` (NW/SW/AR/MN/MS → KTC; KV → KTV). No fresh scope resolution or customer/IOS/PL gate. |
| Product | `QADPRO2.dbo.ld_det.ld_part` |
| Location | `ld_loc`, uppercase presentation only; raw source retained. |
| Lot/reference | `ld_lot`, `ld_ref` retained, including null/blank distinctions. No lot adapter needed. |
| On Hand | `ld_qty_oh > 0`, one output per source row, no inventory aggregation/DISTINCT/deduplication. Zero/negative excluded, not offsets. |
| Price | `MAX(Analysis.dbo.in_price.inp_custprice)` grouped by `inp_part, inp_domain`. All matching sites/dates/customers/feeds/lists/MOQs participate. Preaggregate before composition; no raw-price fan-out. |
| Price absence/errors | Explicit completed zero records → 0; genuine zero/negative valid MAX retained. Mixed null+valid uses MAX; records with only null prices fail. Malformed/incomplete acquisition, scale loss/overflow or batch/query failure fails acquisition. |
| Value | Exact USD unit price × each physical row quantity. Exact underlying decimals; invariant decimal-string transport. Value displayed to two places with existing exact formatter; quantity retains fractions. |
| RMA | Lot starts with `RA`, ordinal case-insensitive, no trimming; null/blank false. Informational badge in Lot; rows remain visible and valued. |
| Filters | Product/Location/Lot case-insensitive literal contains, AND, full loaded snapshot. Empty inputs unrestricted; no trimming/wildcards/regex/source calls. |
| Freshness | First-entry acquisition, compatible process-local reuse, explicit Refresh, original acquisition UTC time. Failed refresh returns compatible last-good with stale warning; initial failure unavailable, never empty/zero. No timer/persistence. |

Five visible columns only: Product, Location, Lot, On Hand, Value. Snapshot-local row IDs preserve
duplicate-looking rows without asserting source-key uniqueness. Deterministic sorting and rendered
pagination operate on the full response. Application rechecks assignment/site/MPS before acquisition,
publication and fallback; cancellation/scope changes cannot publish incompatible/partial results.

## Dated scope amendment — owner-decided 2026-10-07

The older Stage 15 checklist/status/roadmap described selected date, demand coverage, shipping/hold
locations, nettable readiness, lot/status adapters and summary cards. Owner replaces that scope with
current-inventory inspection/filtering. **Hold annotations removed:** order Holds are deferred to a
later Open Orders enhancement, requiring its own authorization; no inventory status/location or
sales-order hold acquisition. No readiness, date/history, due orders/units, allocation, coverage,
editing/drafts/layouts or analytical export. Stage 21 export/no-export disposition **PENDING**.

## Exact evidence locators and limits

- Owner account query: discovery ledger §3 OQ1 (owner-supplied, owner-reported use, offline only).
  Its unused `pt_mstr` join and customer-view IOS/PL join are omitted; they can multiply inventory.
  Sample KTC/SW replaced by parameterized current scope. Missing-price NULL changed to approved zero.
- `docs/data/qadpro2-data-map.md`: `ld_det` / Analysis `in_price` tables: offline documented fields;
  not fresh schema/type/unique-key evidence.
- `src/backend/Kst.Integrations.Qad/Mps/QadSiteDomainMap.cs`, `QadMpsScopeResolver.cs`:
  existing scope/domain patterns; explicit resolved parents do not require MRP activity.
- `docs/api/Shared Inventory Semantics.md`; `Inventory/QadPartInventoryReader.cs`:
  accepted positive-only Site+Part aggregate, unsuitable for retaining detail and unchanged.
- `docs/implementation/KST_v2_STAGE_8D5_COMPONENT_INFO_BACKEND_PLAN.md` §D:
  historical reported Analysis access and `decimal(18,5)` price type, not Stage 15 live validation.
- `QadConnectionFactory.cs`: READ UNCOMMITTED; consistent with reporting NOLOCK convention,
  including Analysis acquisition. Application snapshot is not an atomic QAD point-in-time guarantee;
  dirty/nonrepeatable observations and batch time skew remain possible. No isolation change.
- `PlanningWorkbookService.cs` context checks, `OpenOrdersService.cs` last-good lifecycle;
  frontend `openOrders/report.ts` literal filtering and `shared/exactDecimal.ts` formatting patterns.

## Fixtures and remaining evidence

Adapt discovery F01–F14: remove Hold/status cases; mixed-null+valid succeeds; all-null fails; USD
confirmed. Cover scope/site/domain, raw location/lot/reference/duplicate rows, positive-only reads,
MAX fan-out/ties/negative/zero/missing, exact `1.23456 × 3.00001 = 3.7036923456` (display `3.70`),
empty results, complete-batch failure, cancellation, stale/scope races and literal AND filters.
Automated tests are synthetic/offline/mocked, not source validation.

Pending separately authorized 15.3 evidence: current source precision/nullability/ranges, collation,
physical grain/reference uniqueness, permissions and actual full-workspace query behavior/performance;
owner desktop interaction confirmation. No live query/probe allowed in 15.2. Stage 21 must later decide
export/no-export; if export approved, decide format/audience/columns/filtered full-snapshot rows,
exact values/currency, acquisition/stale metadata, limits, Save As/error/cancellation semantics.

## 15.2 implementation evidence — synthetic/offline and mocked only

Implemented detail and explicit grouped price result sets in `QadFinishedGoodsReader`; raw numeric
SQL values converted to bounded plain text and parsed with representability checks. No inventory
join/deduplication or status acquisition. Domain BigInteger coefficient/scale checks prevent decimal
parse/product rounding. Application gates complete acquisition and compatible last-good publication;
API transports exact strings; frontend uses keyed scope sessions and complete-response local filtering.
DTO/spec and initial generation preceded backend/frontend implementation; final contracts regenerated.

Verification: backend 1,178 passed (Domain 306, Application 395, QAD 268, Architecture 10, API 199).
Focused new backend initially 34 passed; final Application guard run 12 passed (+2 test-only cases,
36 new backend cases verified in total; full suite not repeated for additions).
Frontend focused initially 13 passed, final 15 passed (+2 test-only cases); full bounded-worker run: 660 passed,
5 skipped / 43 files. Default full run had 8 failures (timeouts/wait failure in existing regressions);
failed files then passed with two workers, followed by full two-worker pass. No timeout changes.
Typecheck, changed-file lint, scoped new-file format and production frontend build passed.
Full lint fails only the two locked Stage 10 errors. Broad dotnet/Rust formatting failures remain
known reported exceptions, not rerun/passes. AFT scope diagnostics zero errors/warnings; some
reachability categories incomplete, not a substitute for build/tests.

Alpha.15 version script and architecture checks pass; cached Rust check/build and 9 host tests pass.
Sidecar republished using established script with local-only NuGet restore source; binaries match
SHA256 `C57EB558CC8B423DEC0926C16730FAA900F99204DF5132CC9B0F896D9AD82E0D`,
ProductVersion `0.1.0-alpha.15`, FileVersion `0.1.0.0`. No runtime health/desktop/source check.
Published files are build evidence, not integrated acceptance. Completion report in Stage15 scratch
records preservation, command adjustments and proposed unexecuted 15.3 checklist. 15.2 not accepted.

## 2026-10-07 acceptance and 15.3 progress amendment

The owner explicitly accepted 15.2 and authorized 15.3; supersedes historical pending-acceptance text
above without rewriting its offline evidence. Current preflight found matching current sidecar copies
with SHA256 `C746321059AFB1796DF339B143BBE75B027721F70F930F2A2991C521D42223DE`, different
from the 15.2 preparation report. ProductVersion remains alpha.15; replacement attribution pending.
Runtime launch/validation stopped at this gate, no rebuild or code fix. Taco saved assignment/site mapped
to KTC; loaded MPS/resolved scope not yet captured. Offline reconciliation proposal prepared, not approved
or executed. Progress: `C:\Dev\kst_v2_tmp\Stage15\STAGE_15_3_VALIDATION_REPORT.md`.
