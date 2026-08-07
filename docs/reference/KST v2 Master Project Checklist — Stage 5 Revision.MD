# KST v2 Master Project Checklist — Stage 5 Revision

## Stage 5 — MPS Data Foundation and Dashboard Implementation

Stage 5 is divided into two controlled sub-stages:

* **Stage 5A — KST v2 Data Inventory and Data Strategy**
* **Stage 5B — MPS Dashboard Implementation**

Stage 5B must not begin until Stage 5A is complete and accepted.

---

# Stage 5A — KST v2 Data Inventory and Data Strategy

**Status:** NOT STARTED

## Purpose

Establish the authoritative data requirements, source mappings, stored-procedure strategy, query boundaries, refresh behavior, snapshot design, and data-quality assumptions required for KST v2 before production SQL implementation begins.

This stage is documentation and design first.

Do not implement the MPS dashboard or production database queries during Stage 5A except for narrowly scoped investigative SQL used to understand existing sources.

---

## 5A.1 Reconcile Existing Data Assumptions

* [ ] Review the current KST v2 prototype field inventory.
* [ ] Review the Legacy KST System Inventory.
* [ ] Review the Capability Disposition and Migration Map.
* [ ] Review the Revised Phased Implementation Strategy.
* [ ] Review current Stage 5 checklist assumptions.
* [ ] Identify assumptions that are now obsolete.
* [ ] Remove IOS code as a workspace-scope dependency.
* [ ] Reconcile site/customer/product-line workspace filtering rules.
* [ ] Reclassify `sp_QAD_ktmpswkm` as reference/legacy logic rather than the assumed production MPS contract.
* [ ] Record the availability of database-supported KST-specific stored procedures as a new implementation option.
* [ ] Identify other earlier source/query assumptions requiring confirmation.

---

## 5A.2 Define Data Inventory Structure

For each required dataset, document:

* [ ] Capability / UI section
* [ ] Dataset name
* [ ] Business purpose
* [ ] Data grain
* [ ] Primary key or natural key
* [ ] Site context
* [ ] Customer context
* [ ] Product-line context where applicable
* [ ] Required display fields
* [ ] Required calculation fields
* [ ] Required filtering fields
* [ ] Required join fields
* [ ] Required status inputs
* [ ] Required date fields
* [ ] Expected data volume
* [ ] Source system
* [ ] Source database
* [ ] Existing table/view/procedure if known
* [ ] Candidate KST-specific stored procedure
* [ ] Refresh frequency
* [ ] Snapshot/cache behavior
* [ ] Lazy-load suitability
* [ ] Known data-quality risks
* [ ] Open questions
* [ ] Evidence/source for each mapping

---

## 5A.3 MPS Dataset Inventory

Define the complete data requirement for the future MPS dashboard.

At minimum investigate and document:

* [ ] Site
* [ ] Customer
* [ ] Customer number
* [ ] Product line
* [ ] Parent part
* [ ] Part description
* [ ] Schedule date
* [ ] Week bucket
* [ ] Fiscal period
* [ ] Fiscal quarter
* [ ] Fiscal year
* [ ] Scheduled quantity
* [ ] Planned supply quantity
* [ ] Firm/regular supply quantity
* [ ] Falldown
* [ ] Work-order identity
* [ ] Work-order quantity
* [ ] Work-order status
* [ ] Work-order release state
* [ ] Work-order start date
* [ ] Work-order due date
* [ ] Relevant allocation/kitting status inputs
* [ ] Relevant shortage-status inputs
* [ ] Any raw fields required for MPS color classification
* [ ] Any additional fields required for release-date versus due-date modes

Do not assume a field belongs in the procedure simply because it appears in the UI.

Separate:

```text
Display field
Calculation/status input
Filter input
Join key
Diagnostic metadata
```

---

## 5A.4 Work-Order Status and MPS Color Rules

Before building the MPS procedure, define the inputs needed to determine dashboard status.

Investigate:

* [ ] Released state
* [ ] Frozen state
* [ ] Allocating state
* [ ] Shortage state
* [ ] Empty/no-WO state
* [ ] Competing status conditions
* [ ] Status precedence
* [ ] Whether status belongs to the work order, schedule bucket, parent part, or aggregate
* [ ] Whether multiple work orders may contribute different statuses to one schedule bucket
* [ ] How multiple WO states roll up
* [ ] Which status decisions belong in SQL
* [ ] Which status decisions belong in C#
* [ ] Which values should be returned raw for later interpretation

Do not encode final color rules in SQL unless there is a clear reason.

Prefer returning authoritative raw status inputs and applying presentation/business classification in the backend.

---

## 5A.5 Existing `sp_QAD_ktmpswkm` Analysis

Obtain and review the SQL definition of the existing stored procedure.

Document:

* [ ] Input parameters
* [ ] Site filtering
* [ ] Customer filtering
* [ ] Product-line filtering
* [ ] Date-range behavior
* [ ] Dynamic week-column logic
* [ ] Quantity calculations
* [ ] Planned supply logic
* [ ] Firm supply logic
* [ ] Falldown logic
* [ ] Temporary tables
* [ ] Source tables/views
* [ ] Joins
* [ ] Assumptions
* [ ] Known limitations
* [ ] Performance characteristics if known
* [ ] Logic worth preserving
* [ ] Logic that should not be carried forward

Treat this procedure as business-rule evidence and reference implementation.

Do not assume KST v2 must call it directly.

---

## 5A.6 KST-Specific Stored Procedure Strategy

Define when KST-specific stored procedures are preferred.

Candidate principles:

* [ ] Stored procedures should be read-only.
* [ ] Procedures should be purpose-built around stable application data boundaries.
* [ ] Procedures should accept explicit site/customer/product-line scope.
* [ ] Procedures should not expose unnecessary whole-site datasets.
* [ ] Procedures should return normalized row-oriented results where practical.
* [ ] Procedures should avoid encoding UI presentation details.
* [ ] Procedures may centralize complex joins and proven database-side calculations.
* [ ] Procedures should return raw values needed by backend business rules.
* [ ] Procedures should have documented result contracts.
* [ ] Procedures should have predictable performance characteristics.
* [ ] Procedures should be versionable/change-controlled where organizational process permits.
* [ ] KST must remain able to adapt database results behind `Kst.Integrations.Qad`.

Define criteria for choosing between:

```text
Existing stored procedure
New KST-specific stored procedure
Parameterized direct SQL
View + parameterized SQL
Backend aggregation after database retrieval
```

---

## 5A.7 Stored Procedure Contract Standards

Define a standard for future KST database procedures.

Document expectations for:

* [ ] Naming convention
* [ ] Parameter naming
* [ ] Site parameter
* [ ] Customer parameter
* [ ] Product Line From
* [ ] Product Line To
* [ ] Date/horizon parameters
* [ ] Null behavior
* [ ] Empty-result behavior
* [ ] Data types
* [ ] Fixed-width identifier handling
* [ ] Date/time types
* [ ] Numeric precision
* [ ] Result-set column naming
* [ ] Stable versus dynamic columns
* [ ] Sort-order guarantees
* [ ] Duplicate-row expectations
* [ ] Error behavior
* [ ] Read-only requirements
* [ ] Performance expectations
* [ ] Index dependency documentation
* [ ] SQL Server 2016 compatibility
* [ ] SQL Server 2019 compatibility considerations

Prefer stable row-based results over dynamically generated API-shaped output unless evidence strongly favors otherwise.

---

## 5A.8 Workspace-to-Database Scope Mapping

Define exactly how workspace configuration becomes database query scope.

Workspace inputs:

```text
Site
Customer Number?
Product Line From?
Product Line To?
```

Document:

* [ ] Site is always required.
* [ ] Customer number is optional.
* [ ] Product-line range is optional.
* [ ] Customer or product line must be present.
* [ ] Customer + product line means intersection.
* [ ] Product-line range is inclusive.
* [ ] Single product line is represented consistently.
* [ ] Customer number remains an 8-character string.
* [ ] Product line remains a 4-character string.
* [ ] Site remains a 2-character string.
* [ ] Leading zeroes are preserved.
* [ ] No IOS-based workspace filtering is used.
* [ ] Query scope remains bounded enough to avoid accidental whole-site retrieval.

---

## 5A.9 Data Grain Map

Explicitly define the grain of each major dataset.

At minimum:

* [ ] MPS schedule row
* [ ] Schedule bucket
* [ ] Work order
* [ ] Work-order material requirement
* [ ] Parent part
* [ ] Component
* [ ] BOM relationship
* [ ] Inventory position
* [ ] Purchase-order line
* [ ] Shortage record
* [ ] Sales-order line
* [ ] Shipment history record
* [ ] Planning adjustment
* [ ] Export/update record

For each, document what uniquely identifies one record.

Avoid joining datasets merely because fields have similar names.

---

## 5A.10 Source-System Map

Create an authoritative source map.

At minimum classify:

### QAD

Potential sources include:

* [ ] MPS/schedule
* [ ] Work orders
* [ ] Part master
* [ ] Product line
* [ ] Customer
* [ ] BOM
* [ ] Inventory
* [ ] Purchase orders
* [ ] Sales orders
* [ ] Shipment history
* [ ] Planner/site planning parameters

### Shortage database

Investigate:

* [ ] Shortage records
* [ ] Buyer comments
* [ ] Material coverage
* [ ] Supplier-related fields
* [ ] Other shortage-specific enrichment

### Local KST data

* [ ] Workspace configuration
* [ ] Preferences
* [ ] Temporary coverage settings
* [ ] Future staged planning changes
* [ ] Other locally owned state

Clearly identify which system is authoritative for every field.

---

## 5A.11 Data Quality and Reliability Register

Create a tracked data-risk register.

Examples:

* [ ] IOS code reliability/maintenance issue
* [ ] Missing customer metadata
* [ ] Inconsistent product-line assignments
* [ ] Work-order status inconsistencies
* [ ] Missing/invalid dates
* [ ] Closed/open WO ambiguity
* [ ] Part-number formatting
* [ ] Duplicate records
* [ ] Stale shortage data
* [ ] Buyer-comment freshness
* [ ] Site-specific differences
* [ ] Null handling
* [ ] Cross-site customer identity issues

For every risk, record:

```text
Risk
Impact
Affected capability
Mitigation
Fallback behavior
Open owner/question
```

---

## 5A.12 Query and Performance Strategy

Define broad query principles before implementation.

* [ ] All production database access is read-only.
* [ ] Queries are always site-bounded where applicable.
* [ ] Workspace filters are applied as early as practical.
* [ ] Avoid retrieving whole-site data when a customer/product-line scope exists.
* [ ] Avoid N+1 database query patterns.
* [ ] Prefer bounded batch retrieval for initial workspace load.
* [ ] Use lazy loading for deep drill-down data where appropriate.
* [ ] Identify expected maximum MPS row count.
* [ ] Identify expected WO count per workspace.
* [ ] Identify expected BOM/component volume.
* [ ] Identify expected shortage volume.
* [ ] Identify data that is inexpensive versus expensive to retrieve.
* [ ] Identify procedures requiring analyst/DBA performance review.
* [ ] Document indexes or database dependencies where relevant.

---

## 5A.13 Snapshot Strategy

Define what belongs in the workspace snapshot.

Candidate initial snapshot:

* [ ] Workspace identity
* [ ] Snapshot ID
* [ ] Snapshot creation timestamp
* [ ] Source status
* [ ] MPS schedule
* [ ] Minimal WO summary required for dashboard status

Evaluate whether to include:

* [ ] Part details
* [ ] Full work-order detail
* [ ] BOM
* [ ] Components
* [ ] Inventory
* [ ] Shortages
* [ ] Purchase orders

Classify each as:

```text
Initial workspace load
Lazy-loaded and cached
Always live/on-demand
Not yet decided
```

---

## 5A.14 Refresh Strategy

Define refresh semantics.

Document:

* [ ] What Refresh means for one workspace
* [ ] Which sources refresh together
* [ ] Whether refresh is full or incremental
* [ ] Whether stale snapshot remains visible during refresh
* [ ] What happens when one source succeeds and another fails
* [ ] Partial-refresh behavior
* [ ] Last refresh attempt
* [ ] Last successful refresh
* [ ] Per-source timestamps
* [ ] Snapshot replacement rules
* [ ] Cancellation behavior
* [ ] Concurrent refresh behavior
* [ ] Tab switching during refresh
* [ ] Whether inactive workspaces refresh automatically
* [ ] Whether startup restores old snapshot data or configuration only

Do not implement automatic background refresh until requirements support it.

---

## 5A.15 SQL versus C# Responsibility

For every derived value, determine ownership.

### SQL is appropriate for

* Efficient filtering
* Joins
* Set-based aggregation
* Source-system-specific retrieval
* Proven database calculations where appropriate

### C# is preferred for

* Application business rules
* Status classification
* Cross-source composition
* Validation
* Snapshot orchestration
* UI-independent calculations
* Rules likely to evolve independently of database structure

Create an explicit decision record for ambiguous calculations.

---

## 5A.16 Security and Database Access Strategy

Confirm:

* [ ] QAD access is read-only.
* [ ] Shortage database access is read-only unless explicitly re-evaluated.
* [ ] Windows-integrated authentication requirements.
* [ ] Connection-string configuration mechanism.
* [ ] Credential handling for any non-Windows-auth source.
* [ ] No credentials committed to repository.
* [ ] Database timeout defaults.
* [ ] Command timeout policy.
* [ ] Cancellation-token behavior.
* [ ] Connection pooling assumptions.
* [ ] Logging avoids sensitive connection information.
* [ ] Production errors do not expose SQL text unnecessarily to UI.

---

## 5A.17 Data Contract Design

Define the first normalized backend contracts before implementation.

At minimum:

* [ ] MPS source-row contract
* [ ] Normalized schedule record
* [ ] Schedule bucket
* [ ] Work-order status summary
* [ ] Source-query metadata
* [ ] Snapshot metadata
* [ ] API dashboard response candidate

Do not make database result records equal to API DTOs.

Maintain:

```text
Database procedure/result
        ↓
QAD integration record
        ↓
Application/domain normalization
        ↓
Snapshot model
        ↓
API DTO
        ↓
Frontend
```

---

## 5A.18 Data Validation Plan

Before Stage 5B, identify representative validation cases.

Include:

* [x] At least one customer with one product line
* [ ] Customer with multiple product lines
* [ ] Product-line-only workspace
* [ ] Customer + product-line intersection
* [ ] Multiple sites
* [ ] Part with no work orders
* [ ] Part with one WO
* [ ] Part with multiple WOs
* [ ] Mixed WO status condition
* [ ] Planned-only schedule
* [ ] Firm/released schedule
* [ ] Falldown example
* [ ] Empty result
* [ ] Large workspace
* [ ] Known edge cases from current KST

Capture expected results before implementation where possible.

---

## 5A.19 Required Stage 5A Deliverables

Produce durable repository artifacts for:

* [ ] KST v2 Data Inventory
* [ ] UI-to-Data Requirement Map
* [ ] Data Grain Map
* [ ] Source-System Map
* [ ] Stored Procedure / Query Strategy
* [ ] KST Stored Procedure Standards
* [ ] Snapshot and Refresh Strategy
* [ ] Data Quality / Reliability Register
* [ ] SQL-versus-C# Responsibility Map
* [ ] MPS Data Contract
* [ ] Stage 5B Implementation Plan
* [ ] Updated Master Project Checklist
* [ ] Updated Revised Phased Implementation Strategy if necessary

The repository remains the durable project memory.

---

## 5A.20 Stage 5A Completion Gate

Stage 5A is complete only when:

* [ ] MPS data requirements are fully inventoried at the current implementation depth.
* [ ] Existing `sp_QAD_ktmpswkm` logic has been reviewed.
* [ ] The production MPS retrieval strategy has been selected.
* [ ] Required WO/status inputs are identified.
* [ ] Stored-procedure strategy is documented.
* [ ] Workspace filters map cleanly to database scope.
* [ ] Dataset grains are documented.
* [ ] Source ownership is documented.
* [ ] Known data-quality risks are documented.
* [ ] Snapshot strategy is defined.
* [ ] Refresh strategy is defined.
* [ ] SQL/C# responsibility boundaries are defined.
* [ ] Representative validation cases are selected.
* [ ] Stage 5B implementation scope can be written without guessing about the data contract.
* [ ] Project owner accepts the Stage 5A data strategy.

**Stage 5B must not begin until this gate passes.**

---

# Stage 5B — MPS Dashboard Implementation

**Status:** BLOCKED BY STAGE 5A

## Purpose

Implement the MPS dashboard vertical slice using the data contract and source strategy approved during Stage 5A.

---

## 5B.1 Database Integration

* [ ] Implement the approved MPS data-access mechanism.
* [ ] Add required `Microsoft.Data.SqlClient` support.
* [ ] Add Dapper if selected by the Stage 5A strategy.
* [ ] Implement Windows-integrated QAD connectivity.
* [ ] Add configuration and diagnostics.
* [ ] Implement the approved KST MPS stored-procedure adapter or alternative.
* [ ] Use parameterized database access.
* [ ] Apply workspace site/customer/product-line scope.
* [ ] Support cancellation.
* [ ] Apply command timeout policy.
* [ ] Log execution metadata without leaking sensitive information.

Do not automatically implement `sp_QAD_ktmpswkm`.

Use the source contract approved in Stage 5A.

---

## 5B.2 MPS Normalization

* [ ] Map source records into integration records.
* [ ] Normalize schedule dates.
* [ ] Normalize quantities.
* [ ] Normalize product-line identifiers.
* [ ] Normalize work-order identifiers.
* [ ] Normalize work-order status inputs.
* [ ] Preserve site context.
* [ ] Deduplicate results where required.
* [ ] Detect malformed/incomplete source records.
* [ ] Return source warnings.

---

## 5B.3 Calendar and Bucketing

* [ ] Define week-start convention.
* [ ] Implement weekly buckets.
* [ ] Implement fiscal period mapping.
* [ ] Implement fiscal quarter mapping.
* [ ] Implement fiscal year mapping.
* [ ] Support configured horizon.
* [ ] Test year transitions.
* [ ] Test fiscal transitions.
* [ ] Test leap-year behavior.

---

## 5B.4 MPS Status Classification

Using the raw inputs approved in Stage 5A:

* [ ] Implement Released classification.
* [ ] Implement Frozen classification.
* [ ] Implement Allocating classification.
* [ ] Implement Shortage classification.
* [ ] Implement Empty classification.
* [ ] Implement status precedence.
* [ ] Implement multi-WO aggregation rules.
* [ ] Keep semantic color choice in presentation layer where appropriate.
* [ ] Add unit tests for every status rule.

---

## 5B.5 Snapshot Integration

* [ ] Add MPS data to workspace snapshot.
* [ ] Populate snapshot ID.
* [ ] Populate source metadata.
* [ ] Populate creation timestamp.
* [ ] Preserve old snapshot during refresh where approved.
* [ ] Replace snapshot atomically after successful load.
* [ ] Implement partial/failure behavior per Stage 5A strategy.
* [ ] Avoid unnecessary reload during tab switching.

---

## 5B.6 MPS API

* [ ] Define customer/workspace dashboard endpoint.
* [ ] Return normalized row-oriented schedule data.
* [ ] Return week/fiscal metadata.
* [ ] Return status classifications.
* [ ] Return snapshot metadata.
* [ ] Return source warnings.
* [ ] Support horizon options.
* [ ] Support release-date/due-date mode if required by Stage 5A contract.
* [ ] Update OpenAPI.
* [ ] Regenerate TypeScript contracts.

---

## 5B.7 Frontend MPS Grid

* [ ] Implement MPS grid shell.
* [ ] Implement sticky parent-part column.
* [ ] Implement horizontal scrolling.
* [ ] Implement week headers.
* [ ] Implement fiscal period bands.
* [ ] Implement fiscal quarter bands.
* [ ] Implement schedule quantities.
* [ ] Implement status styling.
* [ ] Implement horizon selector.
* [ ] Implement release/due mode if approved.
* [ ] Implement row selection.
* [ ] Implement week-cell selection.
* [ ] Implement loading state.
* [ ] Implement empty state.
* [ ] Implement stale-data warning.
* [ ] Implement failed-refresh behavior.
* [ ] Implement refresh feedback.

---

## 5B.8 Data Validation

* [ ] Compare KST v2 results to direct database results.
* [ ] Compare against existing KST output where applicable.
* [ ] Validate representative customers.
* [ ] Validate representative sites.
* [ ] Validate product-line-only scope.
* [ ] Validate customer-only scope.
* [ ] Validate customer + product-line scope.
* [ ] Validate schedule totals.
* [ ] Validate WO associations.
* [ ] Validate WO status classifications.
* [ ] Validate falldown.
* [ ] Validate planned supply.
* [ ] Validate firm supply.
* [ ] Validate empty results.
* [ ] Validate large-result performance.
* [ ] Record discrepancies and resolutions.

---

## 5B.9 Automated Verification

* [ ] Database adapter tests.
* [ ] Normalization tests.
* [ ] Status-rule tests.
* [ ] Calendar tests.
* [ ] Snapshot tests.
* [ ] API integration tests.
* [ ] Frontend component tests.
* [ ] Refresh-state tests.
* [ ] Error-state tests.
* [ ] Architecture-boundary tests.
* [ ] Full backend build/test.
* [ ] Full frontend lint/typecheck/test/build.
* [ ] Rust/Tauri verification.
* [ ] Sidecar rebuild.
* [ ] Live Tauri manual verification.

---

## 5B.10 Documentation

* [ ] Document final database contract.
* [ ] Document stored procedure used.
* [ ] Document parameters.
* [ ] Document result columns.
* [ ] Document normalization rules.
* [ ] Document status rules.
* [ ] Document snapshot behavior.
* [ ] Document refresh behavior.
* [ ] Document known source limitations.
* [ ] Update project status.
* [ ] Update Master Project Checklist.
* [ ] Update API documentation.
* [ ] Update data inventory with confirmed mappings.

---

## 5B.11 Stage 5B Completion Gate

Stage 5B is complete only when:

* [ ] A configured workspace loads real MPS data from the approved source.
* [ ] Site/customer/product-line filtering is validated.
* [ ] Schedule quantities are validated.
* [ ] Work-order associations are validated.
* [ ] MPS status classification is validated.
* [ ] Refresh and snapshot behavior are validated.
* [ ] The UI displays the real MPS grid.
* [ ] No fake production data remains.
* [ ] Error/stale/empty states work.
* [ ] Automated verification passes.
* [ ] Representative data matches source evidence.
* [ ] Owner acceptance passes.

**Completion gate:** A scheduler can open a configured workspace and use a validated, cached, real-data MPS grid for schedule review.
