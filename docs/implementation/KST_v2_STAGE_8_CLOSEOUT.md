# KST v2 — Stage 8 Component/BOM Detail Closeout

**Status:** COMPLETE / ACCEPTED
**Owner acceptance date:** 2026-08-21
**Next stage:** Post-Stage-8 handoff (see below) → Stage 9

## Completion statement

A scheduler can select a parent part, inspect and search its current effective multi-level BOM,
select any relevant purchased or manufactured component, and inspect validated site-specific
inventory, planning, cost, and approved-alternate information without navigating to QAD.

Stage 8 is informational. It does not implement material-requirement/netting functionality.

## Delivered checkpoints

8D.1 Shared Stage 6 Inventory capability
8D.2 BOM QAD adapter and normalization
8D.3 BOM application service and API
8D.4 BOM frontend (parent-contextual tab, search/filter)
8D.5 Component Info backend
8D.6 Component Information modal
8D.7 Approved Alternates (AVL)
8D.8 Integrated verification and closeout (this document)

All seven prior checkpoints are committed on `main` (`bf89c60` → `87bc6b1`) and were owner-accepted
before this pass began.

## Final visible capabilities

### BOM

- Parent-contextual BOM tab showing the current effective multi-level BOM (`ps_mstr`).
- Structural hierarchy/order, actual `Level` values, and duplicate/repeated occurrences preserved.
- Phantoms displayed and exploded through.
- P/M display uses the accepted selected-site `ptp_det.ptp_pm_code` otherwise `pt_mstr.pt_pm_code`
  fallback rule (P/M only — not generalized to other planning fields).
- Visible rows limited to effective P/M rows without flattening descendants.
- Component Item substring search, local P/M filter, local Phantom Yes/No filter; filters combine
  (AND semantics) and never trigger new BOM requests.
- Net QOH / Non-Net QOH use the shared Stage 6/8D.1 inventory semantics.

### Component Information

- Selecting a BOM row opens a blocking modal that does not navigate away from the BOM, prevents
  interaction with the underlying Scheduler Console, closes via X or Escape (not backdrop click),
  restores focus to the originating BOM row, and preserves BOM search/filter/scroll/context.
- Visible real data: Component Part, Description; Net QOH, Non-Net QOH; Standard Cost, QCTC
  (each exactly four decimal places, null distinct from numeric zero); Time Fence, Safety Time,
  Safety Stock, Buyer/Planner; Purchase LT, Inspect LT, Cumulative LT, Min Order, Order Multiple;
  Part Status, IOS.

### Approved Alternates

- User-facing terminology is "Approved Alternates"; backend/source implementation keeps its
  technical names (`ApprovedVendor`, `ApprovedVendorService`, `vp_mstr`) unchanged.
- Starts collapsed for each newly opened component; does not load until expanded; loads
  independently of Component Detail; successful result is retained for the current
  modal/component lifetime; collapse/re-expand does not unnecessarily refetch; failure is
  localized to the Approved Alternates region; zero rows is a successful empty state.
- Displays Supplier, Vendor Name, Supplier Item, MFG Part; preserves source ordering (`vp_vend`)
  and multiplicity (no dedup/DISTINCT).

## Authoritative source decisions preserved

- **BOM:** current effective BOM from `ps_mstr`; occurrence identity/order is structural.
- **Effective P/M:** selected-site `ptp_det.ptp_pm_code` otherwise `pt_mstr.pt_pm_code` (P/M only).
- **Inventory:** shared Stage 6/8D.1 rule — positive inventory only, RMA excluded, Net/Non-Net
  determined by location-status nettable/non-nettable flag, no qualifying inventory = zero.
- **Site planning:** `ptp_det` keyed by domain + part + selected site; never `pt_site`; missing
  selected-site planning yields null planning values with no master fallback.
- **Standard Cost:** `sct_det`, domain + site + part, `sct_sim = 'Standard'`, latest
  `sct_cst_date` (not latest across all simulations).
- **QCTC:** `Analysis.dbo.in_price`, domain + site + part, `inp_source = 'qtbom_det'`, latest
  `inp_start_date` (not `idh_hist`/`pid_det`).
- **Approved Alternates / AVL:** `pt_mstr` INNER JOIN `vp_mstr` INNER JOIN `ad_mstr`, grain
  domain + component part, primary ordering `vp_vend`, no DISTINCT/deduplication.

## Verification

### Backend (final, this pass)

`dotnet format Kst.slnx --verify-no-changes`: clean. `dotnet build Kst.slnx`: succeeded, no
warnings/errors. `dotnet test Kst.slnx`: **656/656 passing**, 0 failed, 0 skipped.

| Project | Tests |
|---|---|
| Kst.Domain.Tests | 118 |
| Kst.Application.Tests | 242 |
| Kst.Integrations.Qad.Tests | 173 |
| Kst.ArchitectureTests | 9 |
| Kst.Api.IntegrationTests | 114 |
| **Total** | **656** |

### Frontend (final, this pass)

`npm run typecheck`: clean. `npm run lint` (`--max-warnings 0`): clean. `npm test`:
**260/260 passing** across 14 test files. `npm run build`: succeeded.

### Contract

`npm run generate:types` regenerated `src/frontend/src/generated/api.ts` from
`docs/openapi/Kst.Api.json` with **zero diff** — no hand-edited generated-contract drift.
Confirmed routes present:

- `GET /api/v1/workspaces/{assignmentId}/parts/{parentPart}/bom`
- `GET /api/v1/workspaces/{assignmentId}/components/{componentPart}`
- `GET /api/v1/workspaces/{assignmentId}/components/{componentPart}/approved-vendors`

The technical Approved Vendors route name was preserved (not renamed for the "Approved
Alternates" UI terminology).

### Architecture

9 `Kst.ArchitectureTests` pass: `Kst.Domain` has no infrastructure/ASP.NET Core/`Kst.Api`
dependency; `Kst.Application` has no ASP.NET Core/SQL client dependency;
`Kst.Integrations.Qad`/`Kst.Integrations.Shortages` have no `Kst.Api` dependency. Code
inspection confirmed a single shared `IPartInventoryReader`/`QadPartInventoryReader` composed by
both `BomService` and `ComponentDetailService` (no duplicated inventory logic), and that
`ComponentDetailService` has no coupling to Approved Vendors.

### Live read-only smoke validation (real QAD, `KNWVM13`/`QADPRO2`, workspace "Shure SMT")

- **BOM** (`95B57948`): 200 OK, 216 lines, levels 1–3+, duplicate occurrences preserved (e.g.
  `145FF49R9`, `145HF1000` each appear twice), phantoms present and exploded through, inventory
  populated on lines.
- **Component Detail, full data** (`KEY-RES-0390`): 200 OK — Standard Cost `0.0012320000`,
  QCTC `0.00152`, full planning/lead-time/order fields populated.
- **Component Detail, partial data** (`155610`): 200 OK — zero inventory (distinct from null),
  Standard Cost present, QCTC null, all selected-site planning fields null (obsolete part, no
  site planning row) — matches the accepted null/zero-distinction rule.
- **Approved Alternates** (`339696`): 200 OK, 4 rows, Supplier-ordered, all four required fields
  present.

### Sidecar / desktop

Sidecar rebuilt via `scripts/build-sidecar.ps1` after this pass to ensure the packaged backend
includes the 8D.7 Approved Alternates endpoint before owner manual validation. `cargo check`
clean.

### Owner manual validation

**PASS.** The owner completed the full guided manual checklist (BOM workflow, Component
Information, Approved Alternates, modal behavior) against the real desktop app and reported
PASS with no required corrections.

## Deferred capabilities (intentional, NOT unfinished Stage 8 defects)

The following are deliberately out of scope for Stage 8 and are reserved for future stages:

- Show MRP (Component Information modal contains a disabled/future control).
- Inventory / Lot Locations (Component Information modal reserves a future region; intended to
  let schedulers see where component inventory physically resides without opening QAD; source
  rules and API design remain future work).
- Extended Requirement
- Incoming Supply
- Coverage % / Material Status
- Component MRP / component supply netting
- Future Shortages / PO coverage
- Manufactured-subassembly drilldown (not required — the full multi-level BOM already exposes
  the hierarchy)

## Known limitations

- Live validation exercised one representative workspace ("Shure SMT") and a small number of
  representative parts; the remaining four configured dev workspaces were not individually
  re-exercised in this pass (no evidence of a workspace-specific defect; deferred as
  environment-breadth, not a functional gap).
- No live cross-domain (KV/KTV) validation has ever been possible in this development
  environment (pre-existing limitation, not introduced by Stage 8); that mapping remains
  unit-test-only coverage.

## Scope preserved

This closeout performed only integrated verification, the guided manual validation pass, and
this closeout artifact. No new business capability was implemented. The planned broader
Project Documentation Reconciliation and Repository Memory Cleanup was explicitly deferred, as
was any Stage 9 work. Pre-existing untracked security-policy document work
(`docs/reference/security/`) was left untouched.

## Post-Stage-8 handoff

1. UI Navigation & Keyboard Ergonomics — a broader owner-requested review of navigation/back
   behavior, closing behavior, keyboard shortcuts, and consistency across the application.
2. Project Documentation Reconciliation and Repository Memory Cleanup.
3. Stage 9.

None of the above were started in this pass.

## Final decision

**Stage 8 completion gate: PASS. Stage 8 is COMPLETE / ACCEPTED.**

## Post-acceptance amendments

### 2026-08-31 — BOM Description filter (accepted UX amendment)

After the original Stage 8 acceptance, the project owner accepted a small BOM UX amendment: a
separate, frontend-local **Description** text filter in the BOM view, alongside the existing
filters. The BOM view now supports:

- Component Item substring filter (part-number-only; unchanged by this amendment);
- Description substring filter — case-insensitive substring against the displayed component
  description; leading/trailing filter whitespace trimmed; empty or whitespace-only imposes no
  restriction; null/blank descriptions never match a non-empty query and cause no error;
- P/M filter;
- Phantom filter;
- all four filters combine using AND semantics;
- filtering remains entirely frontend-local and never triggers an additional BOM request; source
  order, repeated occurrences, actual Level values, and occurrence identity are preserved.

Frontend-only change (`src/frontend/src/components/BomPanel.tsx` and its tests); no backend, QAD
SQL, OpenAPI, generated-contract, or dependency change. Manually verified and accepted by the
project owner on 2026-08-31. This amendment is recorded as accepted after the original 2026-08-21
acceptance; it does not modify the acceptance record above. Stage 8 remains COMPLETE / ACCEPTED.
