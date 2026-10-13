# Stage 19 Part 2 — early UI handoff to Sonnet

Historical UI handoff dated 2026-10-13. UI, parent-scope correction, On Time Rate, integration and
sidecar republish were completed; Part 2 and Stage 19 overall were subsequently COMPLETE /
OWNER-ACCEPTED on 2026-10-13. See `KST_v2_STAGE_19_CLOSEOUT.md`; remaining evidence limits are
explicitly retained there.

## Client and scope

`src/frontend/src/api/workspaceShipmentsApi.ts` exports generated-schema aliases and:

- `initialWorkspaceShipmentsFilters(today?)`: fresh local-calendar today-minus-89 through today,
  blank Part Number; do not persist.
- `getWorkspaceShipmentsSpec(assignmentId, signal?)`: authoritative fixed fifteen columns in order.
- `new WorkspaceShipmentsClient(assignmentId)` per mounted assignment visit.
- `acquire(request, signal?)`: register/acquire a new immutable result; cancels/releases prior result.
- `page(offset=0, limit=100, signal?)`: bounded page; max 200; never append every history page.
- `status(signal?)`: operation metadata. `release()`: terminal visit departure/abort/release.

Input request has only optional exact `partNumber`, `shipDateFrom`, `shipDateTo`. Empty date input
maps to null; each independent; both blank = all available history, undated Ship Dates included last.
Scope is resolved server-side from assignment ID. Actual `WorkspaceAssignmentDto`/persisted model has site and configured product-line/parent values, but **no customer or Corp Group fields**. The server uses the existing `QadMpsScopeResolver` on every acquisition (product-line-discovered parents UNION explicit parents); it requires no MPS snapshot or Finished Goods row. Do not infer scope from displayName or another panel.
Transport uses established loopback HTTP on desktop/browser, its own visit session and separate disk
store; do not call `component_mrp_begin`, report discard or native report transport commands.

## Mount, filters and replies

In `src/frontend/src/components/CustomerWorkspace.tsx`, add **Workspace Shipments** immediately after
Finished Goods. Render only for the active module. **Key the panel by `workspace.assignmentId`**:
ApplicationShell reuses CustomerWorkspace across assignments, so merely conditional rendering is
insufficient when switching directly between workspaces on the same active module.

On mount: initialize edited and applied filters; load spec and acquire immediately. Apply commits
edited filters and acquires; Refresh reacquires applied filters without committing un-applied edits.
Track loading/errors locally. Replacement clears old visible rows/metadata and resets page offset.
Maintain UI generation/request tokens (including page/spec requests) so late replies cannot mutate
new state. Abort and call release on unmount/workspace change; discard all rows and result state.
A departed client is never reused. For React StrictMode, create a client inside the visit effect so
effect replay receives a fresh client rather than reusing a terminal released instance.

## Display

Use returned spec column labels/kinds/provenance and operation column IDs; no handwritten catalog.
Fixed columns: no picker, export, totals or selection workflow. Use physicalDetailId as row key
within the operation; invoice/order/line are not unique. Reuse established exact numeric/date display
helpers and accessible Part 1 On Time presentation (1 green check / True, 0 red X / False, null —).
Prices are retro prices; signed quantities/nulls/identifiers must survive. Expose complete row count,
acquisition time, expiry, bounded page controls, localized error/retry and enrichment warning as appropriate.

## Implementation paths (historical phase sequence)

- Contract: `docs/api/WORKSPACE_SHIPMENTS.md`; C# `Dtos/WorkspaceShipmentsDtos.cs`,
  `Endpoints/WorkspaceShipmentsEndpoints.cs`; generated OpenAPI/`src/generated/api.ts` synchronized.
- Scope/service: `Kst.Application/HistoricalShipments/WorkspaceShipmentsService.cs`, reusing
  `Kst.Application.Mps.IMpsScopeResolver` through a workspace-listing-specific adapter.
- Filter/query: `Kst.Domain/HistoricalShipments/WorkspaceShipmentsFilter.cs`,
  `Kst.Integrations.Qad/HistoricalShipments/QadHistoricalShipmentsReader.cs` (`BuildWorkspaceQuery`);
  complete parent membership is parameterized as an XML value table, not as a truncated list.
- Disk ownership: `Kst.Infrastructure/HistoricalShipments/FileWorkspaceShipmentsStore.cs` wraps the
  accepted disk store through a separate instance/root. Part 1 catalog/parser/store are reused.
- Evidence: `docs/implementation/KST_v2_STAGE_19_PART_2_FOUNDATION_EVIDENCE.md`.

The subsequent scoped regressions, separate-context review/corrections, final sidecar republish and
owner acceptance are recorded in `KST_v2_STAGE_19_CLOSEOUT.md` and the Part 2 foundation evidence.
