# Stage 19 Part 1 — UI handoff to Claude Sonnet 5.5

Historical UI handoff for the implementation phase (2026-10-13). Part 1 and Stage 19 overall are
subsequently COMPLETE / OWNER-ACCEPTED — 2026-10-13. The accepted closeout and final verification
limits are in `KST_v2_STAGE_19_CLOSEOUT.md`; this handoff is retained as historical implementation
guidance, not current pending work. Preserve accepted Stage 18 behavior and limitations.

## Entry points

- `src/frontend/src/api/historicalShipmentsApi.ts`: generated-schema aliases only;
  `getHistoricalShipmentsSpec(signal)`, `HistoricalShipmentsClient`.
- `docs/api/HISTORICAL_SHIPMENTS.md`: named routes, HTTP validation and lifecycle.
- `src/frontend/src/generated/api.ts`, `docs/openapi/Kst.Api.json`: synchronized generated contracts.
- `src/frontend/src/components/ReportsPanel.tsx`: existing report navigation, save outcomes,
  status reconciliation, application busy reporting. UI is not yet modified.

Create one client per Historical Shipments visit. Call `acquire(request, signal)`, then
`page(offset, limit, signal)` (max 200). `status` returns operation metadata; `prepare` uses
the frozen result and columns without QAD reread. Never concatenate all pages in frontend memory.
Call `release()` on departure/unmount; release is epoch-bound on desktop. A departed client is
never reused. Keep result/view generation tokens so older page replies cannot replace a newer page.

Desktop `prepare` also transfers the workbook into the host's private file-backed cache.
Use `save(prepared, openAfterSave, saveOperationId)` with a new caller-known UUID per save attempt.
Use `saveStatus(saveOperationId)` after an interrupted/unknown reply; never replay an unresolved
save. `open(savedArtifactId, requestId)` is bounded and deduplicated. Reuse existing open preference
commands through the current ReportSavePort. Report kind is `HistoricalShipments`.
Browser acquisition/paging works; protected saving deliberately requires desktop.

Keep application save/open busy protections through terminal outcome, including optional open.
Report departure releases report state but a native worker retains its immutable local file until
completion. Status is retained after departure. Cancelled Save As keeps the preparation for retry.
The accepted local fixed NTFS/ordinary path constraints and native candidate-bound overwrite consent
remain. No new Tauri capabilities, shell permissions or arbitrary caller URLs/paths.

## Inputs and columns

### Owner default amendment (2026-10-13)

Backend amendment is now implemented. Consume `/spec.columns` in its returned order and filter on
`defaultSelected`; do not copy a frontend catalog. The 17 defaults follow the owner SELECT projection,
with the subsequent owner amendment making Due Date, Required Date and Currency optional.
Currency is no longer added automatically, even with prices selected. New IDs: `requiredDate`, `retroUnitPrice`, `retroExtendedPrice`,
`onTime`, `detailSalesOrder` (optional). Existing IDs are preserved; notably `originalCommit` is now
Perform Date, `unitPrice`/`extendedPrice` explicitly mean original invoice prices, not the retro defaults.
`salesOrder` is header-owned; the optional detail order has a distinct ID/label.

Existing migration code replaces a stored selection matching the old ten-column defaults with these
catalog defaults; recognized custom selections stay usable. Restore default columns reads the same
catalog. Display uses operation columns, and export uses that identical frozen projection.
On Time renders green ✓ for 1, red ✕ for 0 and — for null; exported values stay numeric/null.
Owner picker amendment: catalog defaults are always included in Reports and have no checkboxes.
Only optional columns are selectable; restoring preferences adds all defaults while retaining recognized
optional choices. Restore default columns clears optional additions. The picker lists the always-included labels.
Verify UI with the live `/spec` **in offline mocks**, including default labels/order and Currency after acquisition.
No DTO shape changes or new frontend business types are required. Semantics: `docs/api/HISTORICAL_SHIPMENTS.md`.

- Site mandatory: SW, NW, AR, MN, MS, KV. Filters start blank; do not persist any filters.
- Corp Group direct spelled-out value, mutually exclusive with either Customer range bound.
- Customer, Item, Product Line and Salesperson inclusive ranges; each blank bound unrestricted.
  Customer is historical invoice-header customer; never validate against current master endpoints.
- Invoice dates blank initially. Both blank = all available invoice history, one bound open-ended,
  entered bounds inclusive calendar dates. No maximum span.
- All invoiced activity included; no RMA/memo/ADJ exclusion controls or uninvoiced consignment.
- Read column IDs/labels/provenance/kind/defaults from `/spec`. Persist only valid selected column IDs
  under a report-specific versioned key. No duplicated handwritten catalog or business DTO.
- Effective `operation.columns` is authoritative for table and XLSX. Currency is appended whenever
  Recorded Unit Price or Quantity × Recorded Unit Price is selected. Keep column ordering.
- Nullable cell strings: null = unavailable, exact decimal text for quantities/monetary values,
  ISO calendar-date strings for dates. Preserve leading zeros and signed values. Do not claim net
  shipments/accounting revenue, convert currencies, normalize quantities or add unsupported totals.
- PhysicalDetailId is row key only within the acquired operation. Equal invoice/order/line values
  are separate rows. Show acquired timestamp, expiry, complete row count and unavailable enrichment
  notice. Header failure is whole acquisition failure; no partial table masquerading as complete.

## Implementation paths

| Layer | Path |
|---|---|
| Domain filters/catalog | `src/backend/Kst.Domain/HistoricalShipments/HistoricalShipmentsRules.cs` |
| Application orchestration/contracts | `src/backend/Kst.Application/HistoricalShipments/HistoricalShipmentsService.cs` |
| QAD streaming SQL/parse | `src/backend/Kst.Integrations.Qad/HistoricalShipments/QadHistoricalShipmentsReader.cs` |
| Disk-backed operation store | `src/backend/Kst.Infrastructure/HistoricalShipments/FileHistoricalShipmentsStore.cs` |
| Streaming XLSX | `src/backend/Kst.Exports/HistoricalShipmentsWorkbook.cs` |
| C# API contracts/routes | `src/backend/Kst.Api/Dtos/HistoricalShipmentsDtos.cs`, `Endpoints/HistoricalShipmentsEndpoints.cs` |
| Host transport/publication | `src/tauri/src/component_mrp.rs`, `component_mrp_native.rs`, `lib.rs` |

## Final integration after UI

Run scoped UI tests/typecheck/lint/build, required backend/Rust regressions, regenerate contracts
if changed, republish sidecar using `scripts/build-sidecar.ps1`, and verify matching artifact identity.
Guided desktop checks: mandatory-site and blank/open-ended dates; column persistence but fresh filters;
page navigation without history-wide memory; export same frozen columns; cancellation/retry; overwrite;
open success/failure/status recovery; leave during acquisition/prepare/save; return without old results;
expired preparation; existing four Stage 18 reports unaffected. No live database query is authorized.
Use synthetic/mocked fixtures for integration and keep owner observations separate from automation.
