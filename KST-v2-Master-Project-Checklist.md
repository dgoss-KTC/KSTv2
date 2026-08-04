# KST v2 Master Project Checklist

**Current project position:** Stage 3 — Technical Foundation is complete. Stage 4 — Phase 1: Application Shell and Workspace Configuration is implementation-complete pending owner acceptance.

**Stage 3 closeout commit:** `6f5644c` — `chore: complete Stage 3 technical foundation closeout`

> This Markdown edition reconciles the original checklist with the completed C#/.NET 10 walking skeleton. The original checklist contained a few stale Python references and several database/export foundation items that the rolling-wave strategy intentionally defers until the first UI phase that requires them.

## Status legend

- `[x]` Complete or formally accepted at the current rolling-wave depth
- `[ ]` Not started or still required
- `[~]` Deferred to the UI phase where the capability is first required

## Project planning model

KST v2 uses rolling-wave planning organized by UI section. Each implementation phase contains its own UI review, field inventory, source-data mapping, business rules, backend design, cache design, API contract, frontend implementation, exports where applicable, automated tests, legacy comparison, and user acceptance.

Later phases may extend or refactor models and services created during earlier phases. The complete application does not need to be specified field by field before implementation begins.

---

## Stage 1 — Project Charter ✅

- [x] Project charter approved and current-state, product vision, users, scope, exclusions, safety boundaries, architecture, and rollout strategy established.
- [x] C#/.NET 10, ASP.NET Core, React/TypeScript, and Tauri/Rust selected as the supported architecture.
- [x] Release 1 and pilot strategy established.

## Stage 2 — Legacy System and Product Inventory ✅

- [x] Legacy capability census completed.
- [x] Capability migration dispositions established.
- [x] Prototype inventory created at the broad-product level.
- [x] Initial dataset and source-system inventory completed.
- [x] MPS source decision established around `sp_QAD_ktmpswkm` behind an adapter.
- [x] Rolling-wave, UI-section implementation strategy adopted.
- [x] Detailed field lineage intentionally continues inside each UI phase.

## Stage 3 — Technical Foundation ✅

- [x] Final repository layout established.
- [x] React/TypeScript frontend, Tauri 2 shell, and C#/.NET 10 backend solution established.
- [x] Formatting, linting, type-checking, SDK, analyzer, and package policies established.
- [x] ASP.NET Core loopback API with `/health`, `/ready`, system status, Problem Details, JSON conventions, and OpenAPI established.
- [x] OpenAPI-generated TypeScript contracts established.
- [x] Self-contained `win-x64` single-file backend sidecar publication automated.
- [x] Tauri sidecar discovery, dynamic-port handshake, readiness polling, and frontend URL bridge established.
- [x] Development and packaged CORS policies verified for the observed Tauri origins.
- [x] Explicit sidecar ownership, cleanup, timeout handling, crash notification, and orphan prevention established.
- [x] Single-instance behavior established.
- [x] Development application launches, connects, reports failures truthfully, and shuts down cleanly.
- [x] MSI and NSIS packages build successfully.
- [x] Packaged application launches, connects, prevents duplicate instances, and shuts down without orphan processes.
- [x] Backend, frontend, API, architecture, CORS, and lifecycle-related automated checks pass.
- [x] Tracked setup, lifecycle, troubleshooting, packaging, verification, and current-status documentation established.
- [~] Real QAD/shortage database access, Dapper/SqlClient adapters, production cache models, and export libraries are deferred to the first UI phase that requires them.

---


## Stage 4 — Phase 1: Application Shell and Workspace Configuration

Status: IN_PROGRESS (initial implementation slice complete; second slice added edit/archive/restore/delete/reset workspace lifecycle; third slice (this change) adds snapshot/data-source lifecycle expansion, the refresh coordinator and endpoint, local user preferences (theme/accent color/row density), the General workspace tab, workspace tab reordering, and duplicate-workspace validation)

Implemented slice reference: `ce717a1` — `feat: Stage 4 Phase 1 — application shell, workspace tabs, and backend configuration`

Second slice: Edit Workspace, Archive Workspace, Restore Archived Workspace, Permanent Delete, Reset All Workspace Configuration, active-tab fallback behavior, confirmation dialogs, and toast notifications.

Third slice (this change): Snapshot/data-source status lifecycle expanded to NotLoaded/Loading/Current/Stale/Partial/Failed; `RefreshCoordinator` orchestrates a full refresh cycle across QAD/Shortage Database providers and exposes `POST /api/v1/system/refresh`; local user preferences (theme, accent color, row density) persisted via `GET`/`PUT /api/v1/preferences`; new General workspace tab with Appearance, Workspace Management, and Application Status sections; `BottomStatusBar` shown for both workspace and General views; workspace tab reordering via drag-and-drop and Move Left/Move Right menu items backed by `PUT /api/v1/workspaces/order`; duplicate-scope validation rejects new/edited workspaces that match an existing enabled workspace's site/customer/product-line range.

### 4.1 UI behavior review

- [x] Review the shell prototype
- [x] Confirm top application bar behavior
- [x] Confirm customer-tab behavior
- [x] Confirm General-tab behavior
- [x] Confirm theme behavior
- [x] Confirm accent-color behavior
- [x] Confirm row-density behavior
- [x] Confirm bottom action-bar behavior
- [x] Confirm refresh and status presentation
- [x] Confirm local persistence expectations
### 4.2 Field inventory

- [x] Filter workbook to Global Shell and Customer Workspace fields
- [x] Map application-owned fields
- [x] Map customer identifiers
- [x] Map site identifiers
- [~] Map planner display data
- [~] Map lead-time display data
- [~] Map active-part counts
- [~] Map shortage counts
- [x] Identify fields that remain local-only
- [~] Add any missing prototype fields
### 4.3 Customer-and-site configuration

- [x] Define CustomerSiteAssignment
- [x] Define site
- [x] Define customer code and display name
- [x] Define product-line filters
- [~] Define planner filters
- [~] Define active-part filters if needed
- [x] Support one customer at multiple sites
- [x] Support temporary customer coverage
- [x] Define add, edit, enable/disable (archive/restore), and remove (delete/reset) behavior
  - Drag-and-drop reorder (plus Move Left/Move Right menu items) is now implemented.
- [x] Define validation
  - Includes duplicate-scope rejection (same site/customer/product-line range) among enabled workspaces.
- [x] Persist locally
### 4.4 Snapshot status

- [x] Define not-loaded state
- [x] Define loading state
- [x] Define current state
- [x] Define stale state
- [x] Define partially refreshed state
- [x] Define failed state
- [x] Display last successful refresh
- [~] Display source-level warnings
  - Per-source status (NotConfigured/Loading/Current/Stale/Failed/Unavailable) is surfaced; detailed per-source error/warning text is deferred.
- [x] Preserve cached data after failed refresh
### 4.5 Backend and API

- [x] Create workspace configuration service
- [x] Create local settings service
- [x] Create application status service
- [x] Create source-status model
- [x] Create workspace-list endpoint
- [x] Create workspace-update endpoint
  - Also added: archive, restore, delete, reset-all, and reorder endpoints for the full workspace lifecycle.
- [x] Create preferences endpoints
- [x] Create health and status endpoints
  - Also added: `POST /api/v1/system/refresh` orchestrating the refresh coordinator.
### 4.6 Frontend

- [x] Build top application bar
- [x] Build customer tabs
- [x] Build General tab
- [x] Build customer header
- [x] Build theme control
- [x] Build accent control
- [x] Build density control
- [x] Build source-status indicator
- [x] Build refresh action shell
- [x] Build confirmation dialog
- [x] Build toast notifications
### 4.7 Validation

- [x] Test local settings persistence
- [x] Test customer/site distinction
- [x] Test temporary coverage
- [x] Test corrupted local configuration
- [x] Test empty workspace
- [x] Test backend unavailable state
- [x] Test workspace edit, archive, restore, delete, and reset flows
- [x] Test active-tab fallback behavior after archive/delete
- [x] Test failed refresh state
- [ ] Owner acceptance
Phase 1 completion gate: Implementation-complete pending owner acceptance. Real QAD-backed planner/lead-time/active-part/shortage-count field mapping and per-source warning detail text remain intentionally deferred to the rolling-wave phase(s) that first require live QAD/shortage data (Stage 5 onward).



## Stage 5 — Phase 2: MPS Dashboard Grid

### 5.1 UI behavior review

- [ ] Confirm the MPS grid layout
- [ ] Confirm sticky part columns
- [ ] Confirm horizontal scrolling
- [ ] Confirm weekly horizon options
- [ ] Confirm fiscal period and quarter bands
- [ ] Confirm release-date and due-date modes
- [ ] Confirm status colors and symbols
- [ ] Confirm empty bucket behavior
- [ ] Confirm row selection
- [ ] Confirm week-cell selection
- [ ] Confirm full-grid and collapsed-grid transitions
- [ ] Confirm refresh behavior
### 5.2 Field inventory and lineage

- [ ] Complete all MPS grid field mappings
- [ ] Map part number
- [ ] Map description
- [ ] Map customer and site
- [ ] Map planner
- [ ] Map product line
- [ ] Map part status
- [ ] Map runtime standard
- [ ] Map planned supply
- [ ] Map firm or regular supply
- [ ] Map falldown
- [ ] Map week start
- [ ] Map fiscal period
- [ ] Map fiscal quarter
- [ ] Map fiscal year
- [ ] Map status summary inputs
- [ ] Add fields discovered during real-data inspection
### 5.3 Stored-procedure adapter

- [ ] Implement sp_QAD_ktmpswkm execution
- [ ] Map customer/site assignment to procedure parameters
- [ ] Support multiple calls for non-contiguous filters
- [ ] Validate fixed columns
- [ ] Detect dynamic week columns
- [ ] Parse week-column dates
- [ ] Parse falldown
- [ ] Preserve SUPPLY and PLANNED
- [ ] Preserve site context
- [ ] Normalize numeric values
- [ ] Deduplicate merged calls
- [ ] Record procedure execution metadata
- [ ] Handle malformed results
### 5.4 Calendar service

- [ ] Define week-start convention
- [ ] Define fiscal calendar source
- [ ] Define fiscal period boundaries
- [ ] Define fiscal quarter boundaries
- [ ] Define fiscal year boundaries
- [ ] Support up to 72 weeks
- [ ] Test year transitions
- [ ] Test fiscal-period transitions
- [ ] Test leap years
- [ ] Test falldown boundary
### 5.5 Schedule models and rules

- [ ] Define PartSchedule
- [ ] Define ScheduleBucket
- [ ] Define supply classifications
- [ ] Preserve procedure source type
- [ ] Define scheduled-quantity display
- [ ] Define work-order summary placeholders
- [ ] Define shortage summary placeholders
- [ ] Define Released status
- [ ] Define Frozen status
- [ ] Define Allocating status
- [ ] Define Shortage status
- [ ] Define Empty status
- [ ] Separate prototype styling rules from confirmed business rules
### 5.6 Cache and API

- [ ] Add MPS data to customer/site snapshot
- [ ] Define initial dashboard endpoint
- [ ] Define horizon parameter
- [ ] Define release/due basis parameter
- [ ] Return normalized week records
- [ ] Return snapshot metadata
- [ ] Return source warnings
- [ ] Avoid 72 dynamic API properties
- [ ] Test snapshot reuse across tab switching
### 5.7 Frontend

- [ ] Implement fiscal bands
- [ ] Implement week headers
- [ ] Implement part rows
- [ ] Implement quantity cells
- [ ] Implement status styling
- [ ] Implement horizon selector
- [ ] Implement release/due selector
- [ ] Implement row selection
- [ ] Implement week selection
- [ ] Implement loading skeleton
- [ ] Implement empty customer state
- [ ] Implement stale-data warning
- [ ] Implement refresh feedback
### 5.8 Validation

- [ ] Compare normalized data with procedure output
- [ ] Compare totals by part
- [ ] Compare planned totals
- [ ] Compare supply totals
- [ ] Compare falldown totals
- [ ] Verify product-line filtering
- [ ] Verify planner filtering
- [ ] Verify site filtering
- [ ] Verify multiple procedure-call merge
- [ ] Validate representative customers
- [ ] Validate 12-week view
- [ ] Validate 72-week view
- [ ] Owner acceptance
Phase 2 completion gate: A scheduler can open a customer/site workspace and use a validated, cached MPS grid for schedule review.


## Stage 6 — Phase 3: Part Information Drill-Down

### 6.1 UI and fields

- [ ] Confirm the Part Info tab
- [ ] Map revision
- [ ] Map planner
- [ ] Map lead time
- [ ] Map UOM
- [ ] Map item class
- [ ] Map description
- [ ] Map component count
- [ ] Map on-hand finished goods
- [ ] Map WIP
- [ ] Map safety stock
- [ ] Map part-level schedule status
### 6.2 Backend

- [ ] Create part-master adapter
- [ ] Create site-planning-parameter adapter
- [ ] Define effective planner fallback
- [ ] Define effective lead-time fallback
- [ ] Define inventory summary
- [ ] Define WIP calculation
- [ ] Define component count
- [ ] Create PartDetail
- [ ] Create part-detail endpoint
- [ ] Cache stable part information where appropriate
### 6.3 Frontend and validation

- [ ] Build Part Info panel
- [ ] Build loading state
- [ ] Build missing-part state
- [ ] Build partial-data warnings
- [ ] Validate against QAD
- [ ] Validate fallback behavior
- [ ] Owner acceptance
Phase 3 completion gate: Selecting an MPS part displays validated part attributes and inventory summaries.


## Stage 7 — Phase 4: Work Orders and Kitting

### 7.1 Field and rule discovery

- [ ] Map work-order number
- [ ] Map ordered quantity
- [ ] Map completed quantity
- [ ] Map open quantity
- [ ] Map status
- [ ] Map start date
- [ ] Map due date
- [ ] Map production line
- [ ] Identify allocation fields
- [ ] Define kitting percentage
- [ ] Map component requirements
- [ ] Map issued quantities
- [ ] Define variance quantity
- [ ] Define variance percentage
- [ ] Confirm severity thresholds
### 7.2 Backend

- [ ] Create work-order adapter
- [ ] Create WO-material adapter
- [ ] Define WorkOrderSummary
- [ ] Define WorkOrderMaterialLine
- [ ] Create work-order service
- [ ] Create kitting service
- [ ] Create variance service
- [ ] Join work orders to schedule buckets
- [ ] Add work-order summaries to cached MPS data or lazy detail
- [ ] Create work-order endpoints
### 7.3 Frontend and validation

- [ ] Build work-order cards
- [ ] Build selected-week filtering
- [ ] Build all-open-WO view
- [ ] Build kitting expansion
- [ ] Build variance sorting
- [ ] Build no-WO state
- [ ] Compare against WO Variance report
- [ ] Validate partial issue
- [ ] Validate over-issue
- [ ] Validate completed work orders
- [ ] Owner acceptance
Phase 4 completion gate: A scheduler can trace an MPS bucket to its work orders and component issue status.


## Stage 8 — Phase 5: Component and BOM Detail

### 8.1 Field and rule discovery

- [ ] Map component part
- [ ] Map component description
- [ ] Map quantity per
- [ ] Define extended requirement
- [ ] Map component on hand
- [ ] Map incoming supply
- [ ] Define coverage percentage
- [ ] Define material status
- [ ] Confirm BOM revision and effective-date behavior
- [ ] Confirm multi-level BOM expectations
- [ ] Confirm phantom and substitute behavior
### 8.2 Backend

- [ ] Create BOM adapter
- [ ] Create BOM-explosion service
- [ ] Define ComponentRequirement
- [ ] Define required quantity grain
- [ ] Add inventory availability service
- [ ] Add component supply summary
- [ ] Create component endpoint
- [ ] Decide when pre-exploded BOM storage is justified
- [ ] Add BOM tests
### 8.3 Frontend and validation

- [ ] Build Components tab
- [ ] Build component selection
- [ ] Build coverage display
- [ ] Build no-components state
- [ ] Compare against Component MRP
- [ ] Validate multi-level quantities
- [ ] Validate duplicate components
- [ ] Owner acceptance
Phase 5 completion gate: A scheduler can inspect the material structure and coverage behind a scheduled parent part.


## Stage 9 — Phase 6: Immediate Shortages

### 9.1 Rule definition

- [ ] Confirm immediate window length
- [ ] Define required quantity
- [ ] Define available quantity
- [ ] Define nettable inventory statuses
- [ ] Define shortage quantity
- [ ] Define On Hand status
- [ ] Define Due This Week status
- [ ] Define Short status
- [ ] Define work-order association
- [ ] Define receipt timing assumptions
- [ ] Define inventory allocation assumptions
- [ ] Define treatment of shared inventory
### 9.2 Backend

- [ ] Define ImmediateShortage
- [ ] Create immediate-requirement service
- [ ] Create inventory-netting service
- [ ] Create immediate-PO-coverage service
- [ ] Create shortage-classification service
- [ ] Add shortage counts to MPS buckets
- [ ] Create immediate-shortage endpoint
- [ ] Add stale-source warnings
### 9.3 Frontend and validation

- [ ] Build Shortages tab
- [ ] Sort components by severity
- [ ] Build status indicators
- [ ] Build component selection
- [ ] Build no-immediate-WO state
- [ ] Compare with existing Shortage Report
- [ ] Validate receipt boundary
- [ ] Validate insufficient incoming supply
- [ ] Validate fully covered requirements
- [ ] Owner acceptance
Phase 6 completion gate: A scheduler can identify immediate component shortages affecting near-term work orders.


## Stage 10 — Phase 7: Purchase-Order Drill-Down

### 10.1 Field discovery

- [ ] Map PO number
- [ ] Map vendor
- [ ] Map ordered quantity
- [ ] Map open quantity
- [ ] Map due date
- [ ] Map confirmed or scheduled status
- [ ] Map buyer
- [ ] Define PO coverage
- [ ] Map shortage comment
- [ ] Map supplier credit-hold flag
- [ ] Map CIA flag
- [ ] Confirm multiple-PO ordering
### 10.2 Buyer-note decision

- [ ] Confirm authoritative note source
- [ ] Determine whether KST may update ShortageMaster
- [ ] If writes are prohibited, define read-only behavior
- [ ] Evaluate local-only note storage
- [ ] Evaluate export-based note updates
- [ ] Define conflict and refresh behavior
- [ ] Document final persistence decision
### 10.3 Backend and frontend

- [ ] Create PO adapter
- [ ] Create vendor adapter
- [ ] Create supplier-risk adapter
- [ ] Create note adapter
- [ ] Define ComponentPurchaseOrder
- [ ] Create PO-coverage service
- [ ] Create PO-detail endpoint
- [ ] Build Component PO Drill card
- [ ] Build previous/next PO navigation
- [ ] Build buyer-note interaction
- [ ] Build no-open-PO state
- [ ] Validate with current shortage output
- [ ] Owner acceptance
Phase 7 completion gate: A scheduler can trace a component shortage to its open purchase orders, vendor, coverage, and current buyer information.


## Stage 11 — Phase 8: Future Shortages and Component MRP

### 11.1 Rule discovery

- [ ] Define projection horizon
- [ ] Define lead-time horizon
- [ ] Define projected balance
- [ ] Define planned-order handling
- [ ] Define covering PO
- [ ] Define projected clear week
- [ ] Define coverage gap
- [ ] Define future-shortage quantity
- [ ] Confirm behavior when no WO exists
- [ ] Confirm forecast treatment
### 11.2 Backend

- [ ] Define ProjectedShortage
- [ ] Create time-phased Component MRP service
- [ ] Create projected-balance service
- [ ] Create future-shortage service
- [ ] Reuse component and PO services
- [ ] Create future-shortage endpoint
- [ ] Create Component MRP endpoint
- [ ] Define Component MRP export dataset
### 11.3 Frontend, export, and validation

- [ ] Build Future Shortages tab
- [ ] Build projection descriptions
- [ ] Build no-future-shortage state
- [ ] Build Component MRP export options
- [ ] Support selected parent parts
- [ ] Support selected components
- [ ] Support date horizon
- [ ] Support selectable columns
- [ ] Compare with existing Component MRP
- [ ] Owner acceptance
Phase 8 completion gate: A scheduler can see future material exposure and export a scoped Component MRP report.


## Stage 12 — Phase 9: Multi-Part Shortage Analysis

### 12.1 Selection behavior

- [ ] Confirm Multi mode
- [ ] Confirm row checkbox behavior
- [ ] Confirm one-part WO-centric view
- [ ] Confirm multi-part component-centric view
- [ ] Confirm affected-parent display
- [ ] Confirm selection clearing
- [ ] Confirm export scope
### 12.2 Rules and backend

- [ ] Define shared-component aggregation
- [ ] Define inventory netting across selected parents
- [ ] Prevent duplicate inventory multiplication
- [ ] Define work-order-specific shortage grain
- [ ] Define component-centric shortage grain
- [ ] Define affected-parent relationships
- [ ] Create selection-analysis endpoint
- [ ] Create shortage export request
- [ ] Create configurable shortage export dataset
### 12.3 Frontend and validation

- [ ] Build Multi selection
- [ ] Build WO-centric table
- [ ] Build part-centric table
- [ ] Build shared-component pills
- [ ] Build export dialog
- [ ] Support selected columns
- [ ] Support selected parts and WOs
- [ ] Validate shared inventory
- [ ] Compare exported results with current Shortage Report
- [ ] Owner acceptance
Phase 9 completion gate: A scheduler can analyze and export shortages for one or several selected MPS parent parts.


## Stage 13 — Phase 10: Planning Workbook

### 13.1 Field and rule discovery

- [ ] Map sales-order quantities
- [ ] Map forecast quantities
- [ ] Map MPS quantities
- [ ] Map unit price
- [ ] Map unit cost
- [ ] Define SO value
- [ ] Define MPS value
- [ ] Define demand selection
- [ ] Define estimated on-hand
- [ ] Define adjusted on-hand
- [ ] Define adjustment grain
- [ ] Define frozen-fence restrictions
- [ ] Define validation rules
- [ ] Define export mappings
### 13.2 Backend

- [ ] Define PlanningBucket
- [ ] Define ProposedMpsAdjustment
- [ ] Create planning-data service
- [ ] Create inventory-projection service
- [ ] Create price and cost adapters
- [ ] Create adjustment staging service
- [ ] Create validation service
- [ ] Create planning endpoints
- [ ] Create MPS mass-update exporter
### 13.3 Frontend and validation

- [ ] Build Planning Workbook grid
- [ ] Build grouped part blocks
- [ ] Build editable adjustment row
- [ ] Highlight staged changes
- [ ] Build clear confirmation
- [ ] Build export behavior
- [ ] Display last export
- [ ] Test negative inventory
- [ ] Test frozen periods
- [ ] Test invalid adjustments
- [ ] Validate exported mass update
- [ ] Owner acceptance
Phase 10 completion gate: A scheduler can review supply and demand, stage MPS adjustments, validate them, and produce a QAD-compatible update file.


## Stage 14 — Phase 11: Customer Open Orders

### 14.1 Field inventory and rules

- [ ] Map sales-order number
- [ ] Map customer PO
- [ ] Map line
- [ ] Map item and revision
- [ ] Map ship date
- [ ] Map perform date
- [ ] Map required date
- [ ] Map dock date
- [ ] Map on hand
- [ ] Map extended price
- [ ] Map ship-to
- [ ] Map order status
- [ ] Confirm editable date fields
- [ ] Define date validation
- [ ] Define QXtend mapping
### 14.2 Backend

- [ ] Create sales-order adapter
- [ ] Define OpenOrderLine
- [ ] Define ProposedOrderChange
- [ ] Create customer Open Orders service
- [ ] Create order-change validation
- [ ] Create Open Orders endpoints
- [ ] Create QXtend-compatible exporter
### 14.3 Frontend and validation

- [ ] Build customer order grid
- [ ] Build editable date cells
- [ ] Highlight staged changes
- [ ] Build clear confirmation
- [ ] Build export behavior
- [ ] Display change count
- [ ] Validate representative orders
- [ ] Validate output file with QXtend requirements
- [ ] Owner acceptance
Phase 11 completion gate: A scheduler can inspect customer orders and generate validated date-change files without direct database writes.


## Stage 15 — Phase 12: Finished Goods

### 15.1 Field and rule discovery

- [ ] Define as-of date
- [ ] Map due orders
- [ ] Map due units
- [ ] Map finished-goods on hand
- [ ] Map location
- [ ] Map lot
- [ ] Define shipping locations
- [ ] Define hold locations
- [ ] Define RMA classification
- [ ] Define nettable status
- [ ] Define inventory value
- [ ] Define demand coverage
### 15.2 Backend and frontend

- [ ] Create finished-goods adapter
- [ ] Create lot adapter
- [ ] Create inventory-status adapter
- [ ] Define FinishedGoodsPosition
- [ ] Create coverage service
- [ ] Create Finished Goods endpoint
- [ ] Build summary cards
- [ ] Build location and lot grid
- [ ] Build date selector
- [ ] Build export if retained
- [ ] Validate nettable inventory
- [ ] Validate RMA exclusion
- [ ] Owner acceptance
Phase 12 completion gate: A scheduler can determine whether available finished goods cover immediate customer demand.


## Stage 16 — Phase 13: General Open Orders

### 16.1 Search design

- [ ] Confirm all filters
- [ ] Confirm required versus optional filters
- [ ] Confirm default site behavior
- [ ] Confirm result limits
- [ ] Confirm sorting behavior
- [ ] Confirm selectable columns
- [ ] Confirm column order
- [ ] Confirm saved layouts
- [ ] Confirm export behavior
### 16.2 Backend and frontend

- [ ] Define OpenOrderSearchRequest
- [ ] Define OpenOrderSearchRow
- [ ] Create cross-customer search service
- [ ] Add filter validation
- [ ] Add pagination or safe result limits
- [ ] Create search endpoint
- [ ] Create configurable export
- [ ] Build filter bar
- [ ] Build column builder
- [ ] Build sortable grid
- [ ] Build saved layouts
- [ ] Validate large result sets
- [ ] Owner acceptance
Phase 13 completion gate: A scheduler can perform flexible cross-customer Open Orders searches and exports.


## Stage 17 — Phase 14: General WO Variance

### 17.1 Rules and backend

- [ ] Confirm IOS-code filter
- [ ] Confirm included WO statuses
- [ ] Confirm component inclusion
- [ ] Confirm variance thresholds
- [ ] Confirm negative-variance treatment
- [ ] Define WorkOrderVarianceRow
- [ ] Create cross-customer variance service
- [ ] Create search endpoint
- [ ] Decide whether export remains required
### 17.2 Frontend and validation

- [ ] Build IOS selector
- [ ] Build sortable variance grid
- [ ] Build severity highlighting
- [ ] Build empty state
- [ ] Build export if retained
- [ ] Compare with current WO Variance report
- [ ] Owner acceptance
Phase 14 completion gate: A scheduler can independently investigate work-order material variance by IOS or equivalent scope.


## Stage 18 — Phase 15: Standalone Excel Reports

### 18.1 Shared report infrastructure

- [ ] Define report request pattern
- [ ] Define output-directory behavior
- [ ] Define filename conventions
- [ ] Define overwrite behavior
- [ ] Define workbook metadata
- [ ] Define progress reporting
- [ ] Define cancellation behavior
- [ ] Define error cleanup
- [ ] Define workbook validation tests
### 18.2 Shipments-To-Go

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Map every output field
- [ ] Extract business rules
- [ ] Reuse shared order and shipment services
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate with stakeholders
- [ ] Owner acceptance
### 18.3 S&OP

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Determine use of MPS procedure data
- [ ] Extract aggregation rules
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate monthly period behavior
- [ ] Owner acceptance
Phase 15 completion gate: Required Shipments-To-Go and S&OP workbooks can be generated and validated from KST v2.


## Stage 19 — Phase 16: Historical Shipments

### 19.1 Requirements

- [ ] Confirm user questions
- [ ] Confirm retention horizon
- [ ] Confirm customer and site filters
- [ ] Confirm part filters
- [ ] Confirm date-range behavior
- [ ] Confirm order and PO fields
- [ ] Confirm shipment quantity
- [ ] Confirm revenue calculation
- [ ] Confirm returns and reversals
- [ ] Confirm corrections
- [ ] Decide export requirements
### 19.2 Backend and frontend

- [ ] Investigate tr_hist
- [ ] Identify authoritative shipment transactions
- [ ] Define ShipmentHistoryRow
- [ ] Create transaction normalization
- [ ] Create reversal handling
- [ ] Create shipment-history endpoint
- [ ] Build search and results UI
- [ ] Build drill-downs if needed
- [ ] Build export if approved
- [ ] Validate historic totals
- [ ] Owner acceptance
Phase 16 completion gate: A scheduler can review reliable historical shipment activity for a selected site, customer, part, and date range.


## Stage 20 — Phase 17: Legacy Simulation

### 20.1 Compatibility inventory

- [ ] Document current input format
- [ ] Document current calculation process
- [ ] Document current output
- [ ] Identify external file dependencies
- [ ] Identify PO data requirements
- [ ] Identify configuration requirements
- [ ] Identify known limitations
### 20.2 Migration

- [ ] Move existing logic behind the v2 backend
- [ ] Preserve existing inputs
- [ ] Preserve existing outputs
- [ ] Add regression fixtures
- [ ] Build minimal v2 UI integration
- [ ] Add errors and progress reporting
- [ ] Validate against KST v1
- [ ] Owner acceptance
### 20.3 Deferred redesign

- [ ] Record advanced simulation as future scope
- [ ] Create future-requirements placeholder
- [ ] Avoid designing the advanced simulation engine during Release 1
- [ ] Avoid allowing legacy architecture to constrain future simulation design
Phase 17 completion gate: Existing Simulation functionality is available without expanding Release 1 scope.


## Stage 21 — Cross-Cutting Export Completion

Some export work occurs inside feature phases, but this stage verifies the export system as a whole.

- [ ] MPS configurable Excel export
- [ ] Component MRP configurable Excel export
- [ ] Shortage configurable Excel export
- [ ] Open Orders export
- [ ] Finished Goods export if retained
- [ ] WO Variance export if retained
- [ ] MPS mass-update CSV
- [ ] Sales-order mass-update CSV
- [ ] Shipments-To-Go workbook
- [ ] S&OP workbook
- [ ] Historical Shipments export if approved
- [ ] Consistent filenames
- [ ] Consistent destination handling
- [ ] Consistent error handling
- [ ] Selected-column support
- [ ] Selected-part support
- [ ] Selected-date support
- [ ] Workbook formatting standards
- [ ] Export audit metadata
- [ ] Golden-master validation

## Stage 22 — Cross-Cutting Quality and Hardening

### 22.1 Data integrity

- [ ] Verify domain filtering
- [ ] Verify site filtering
- [ ] Verify customer assignments
- [ ] Verify product-line filtering
- [ ] Verify planner filtering
- [ ] Verify date boundaries
- [ ] Verify numeric precision
- [ ] Verify null handling
- [ ] Verify duplicate handling
- [ ] Verify stale-data handling
- [ ] Verify partial-refresh handling
### 22.2 Performance

- [ ] Measure startup time
- [ ] Measure initial customer load
- [ ] Measure refresh time
- [ ] Measure drill-down time
- [ ] Measure 72-week MPS rendering
- [ ] Measure large Open Orders search
- [ ] Measure shortage analysis
- [ ] Measure export generation
- [ ] Add indexes or query changes where allowed
- [ ] Add in-memory caching where measured
- [ ] Reconsider persistent cache only if justified
- [ ] Reconsider pre-exploded BOM only if justified
### 22.3 Reliability

- [ ] Test QAD unavailable
- [ ] Test shortage DB unavailable
- [ ] Test Analysis DB unavailable
- [ ] Test one source failing during refresh
- [ ] Test backend crash recovery
- [ ] Test corrupted local settings
- [ ] Test interrupted export
- [ ] Test invalid destination
- [ ] Test low disk space
- [ ] Test application update compatibility
### 22.4 Security

- [ ] Verify read-only QAD access
- [ ] Verify read-only shortage access unless an exception is approved
- [ ] Prevent credentials in logs
- [ ] Protect local configuration
- [ ] Bind API only to the local machine
- [ ] Validate all file paths
- [ ] Sanitize filenames
- [ ] Validate all user-entered filters
- [ ] Validate staged update values
- [ ] Ensure no direct company-database writes exist
### 22.5 Accessibility and usability

- [ ] Keyboard navigation
- [ ] Visible focus state
- [ ] Color-independent status indicators
- [ ] Light and dark mode readability
- [ ] Compact and comfortable density
- [ ] Scaling on common Windows resolutions
- [ ] Horizontal-grid usability
- [ ] Loading feedback
- [ ] Clear empty states
- [ ] Clear stale-data warnings
- [ ] Clear export confirmation
- [ ] User testing with schedulers
### 22.6 Documentation

- [ ] Architecture overview
- [ ] Repository guide
- [ ] Developer setup
- [ ] Database-source catalog
- [ ] Business-rule catalog
- [ ] API documentation
- [ ] Cache and refresh documentation
- [ ] Export documentation
- [ ] Deployment guide
- [ ] Troubleshooting guide
- [ ] Scheduler user guide
- [ ] QAD-upgrade migration guide
- [ ] Architecture decision records

## Stage 23 — Release 1 Readiness

### 23.1 Functional readiness

- [ ] All required interactive phases complete
- [ ] Required exports complete
- [ ] Simulation compatibility complete
- [ ] Historical Shipments disposition confirmed
- [ ] Customer/site configuration complete
- [ ] Staged update workflows complete
- [ ] No direct database writes
- [ ] All critical business rules approved
### 23.2 Validation readiness

- [ ] Golden-master comparisons complete
- [ ] Representative customer tests complete
- [ ] Representative site tests complete
- [ ] Scheduler walkthroughs complete
- [ ] Known intentional differences documented
- [ ] Open critical defects resolved
- [ ] Performance targets accepted
- [ ] Error behavior accepted
### 23.3 Packaging and deployment

- [ ] Build signed or approved Windows installer
- [ ] Package .NET sidecar
- [ ] Package runtime dependencies
- [ ] Configure installation directories
- [ ] Configure local settings migration
- [ ] Configure logging directories
- [ ] Configure update strategy
- [ ] Test clean installation
- [ ] Test upgrade installation
- [ ] Test uninstall
- [ ] Create deployment instructions
### 23.4 Operational readiness

- [ ] Identify pilot users
- [ ] Identify support contacts
- [ ] Define defect-reporting process
- [ ] Define fallback to KST v1
- [ ] Define issue severity
- [ ] Define data-validation process
- [ ] Define training
- [ ] Define feedback collection
- [ ] Define release notes
- [ ] Approve pilot launch

## Stage 24 — Pilot

### 24.1 Initial-site pilot

- [ ] Deploy at primary site
- [ ] Keep KST v1 available
- [ ] Monitor data discrepancies
- [ ] Monitor refresh reliability
- [ ] Monitor export compatibility
- [ ] Monitor performance
- [ ] Capture scheduler feedback
- [ ] Capture missed fields and workflows
- [ ] Correct critical business rules
- [ ] Refine UI
- [ ] Refine diagnostics
### 24.2 Pilot exit criteria

- [ ] Core workflows used successfully
- [ ] Required reports accepted
- [ ] Mass-update files accepted
- [ ] No unresolved critical data errors
- [ ] No unresolved direct-write risk
- [ ] Refresh reliability accepted
- [ ] Performance accepted
- [ ] User acceptance received
- [ ] Support process functioning
- [ ] Project owner approves broader rollout

## Stage 25 — Incremental Multi-Site Rollout

- [ ] Select next site
- [ ] Gather site-specific configuration
- [ ] Validate customer assignments
- [ ] Validate planner mappings
- [ ] Validate product-line mappings
- [ ] Validate database access
- [ ] Validate reports
- [ ] Validate local operating practices
- [ ] Train users
- [ ] Deploy
- [ ] Monitor
- [ ] Repeat for each site
- [ ] Retire KST v1 only after approved transition

## Stage 26 — Post-Release Roadmap

### 26.1 QAD upgrade preparation

- [ ] Monitor upgrade timeline
- [ ] Obtain test-schema access
- [ ] Compare QAD table changes
- [ ] Update adapters
- [ ] Preserve domain models
- [ ] Preserve API contracts
- [ ] Run migration fixtures
- [ ] Validate exports
- [ ] Deploy compatibility update
### 26.2 Advanced Simulation

- [ ] Gather scheduler requirements
- [ ] Define simulation questions
- [ ] Define scenario inputs
- [ ] Define authoritative source data
- [ ] Define constraints
- [ ] Define calculation model
- [ ] Define validation model
- [ ] Define comparison views
- [ ] Define saved scenarios
- [ ] Create separate charter and implementation plan
### 26.3 Potential future enhancements

- [ ] More historical analytics
- [ ] Additional configurable exports
- [ ] Additional cross-customer views
- [ ] Improved coverage and risk modeling
- [ ] Alternate-part analysis
- [ ] Expanded supplier-risk integration
- [ ] Additional local-first capabilities
- [ ] Site-requested enhancements

## Current Project Position

### Completed

- [ ] Stage 1 — Project Charter
- [ ] Stage 2 — Broad legacy, UI, and dataset inventory
- [ ] MPS source strategy
- [ ] Rolling-wave phased-planning decision
- [ ] Prototype field-inventory workbook
### Current focus

- [ ] Finish only the fields needed for Phase 1 and Phase 2
- [ ] Establish the technical foundation
- [ ] Implement Application Shell and Workspace Configuration
- [ ] Implement the MPS Dashboard Grid
### Planning rule going forward

Before beginning each later phase:

- [ ] Review that section of the prototype.
- [ ] Filter the field inventory to that phase.
- [ ] Map the fields currently known.
- [ ] Add missing fields discovered during review.
- [ ] Confirm business rules.
- [ ] Define the smallest sufficient backend contract.
- [ ] Implement and validate the complete vertical slice.
- [ ] Update the shared models only when the implemented phase proves that an extension is needed.
