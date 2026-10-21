# Workspace Shortages protected workbook preparation

**COMPLETE / OWNER-ACCEPTED — S22-008, 2026-10-20.** Owner-reported UI/export acceptance;
not a new Stage 11 calculation amendment or full source-parity/fault verification.
Scope, accepted build, reused verification and limits:
`../implementation/KST_v2_S22_008_WORKSPACE_SHORTAGES.md`.

## Contract and meaning

POST `/api/v1/reports/workspace-shortages` accepts `WorkspaceShortagesExportRequestDto`:
workspaceId, mpsSnapshotId, site, application-local businessDate, acquiredAtUtc and selection
(`ExportLongTermShortagesRequestDto`: snapshotId, ordered componentParts, includeManufacturedParts,
includePhantoms, includeUnconfirmed, horizonWeeks and showAll). C# DTOs/endpoints → generated
`docs/openapi/Kst.Api.json` → generated TypeScript remain authoritative; no handwritten business DTO.

This prepares the complete displayed filtered/sorted component selection from an **already loaded
cached projection**, not a new source acquisition or a request to export a pending replacement.
From the owner-authorized 2026-10-21 view amendment, Include KSS is unchecked by default (existing KSS
rows excluded); checked admits both KSS and non-KSS otherwise-matching rows. Show All does not override
this choice. Ordered `componentParts` already carries the resulting selection; no API field, acquisition,
classification or workbook change is needed. KSS context changes retain existing preparation protection.
Snapshot IDs must agree; workspace/site/current snapshot, business date, unique nonempty components,
supported horizon (13/26/52/72), compatible loaded population/options and acquisition timestamp are
validated. Existing cached export validation supplies the rows; only validated rows are reordered to
the requested display order. Cached acquisition/snapshot/date are checked again after serialization.
No QAD or Shortages reread. The existing workbook generator/sheets/quantities/classifications are
unchanged; receipt selection uses the accepted confirmed/all-receipts projection, not new arithmetic.

`X-KST-Report-Session` is required (D-format GUID). Success returns `AnalyticalExportPreparedDto`
(preparationId, fileName, expiresAtUtc). This reused response schema does **not** confer Analytical
native grant lifetimes. Invalid selection/session is 400; missing/replaced/incompatible loaded context
is 409; unsupported workbook/preparation capacity is 422; busy/failure is 503. Failures do not return
successful-empty workbooks or acquire fresh source data as fallback.

GET `/api/v1/reports/workspace-shortages/preparations/{id}/bytes` returns exact XLSX bytes, bound to
the session and `WorkspaceShortages` kind. Invalid session is 400; absent/expired/wrong-session or
wrong-kind preparation is 410. Reuses the existing bounded preparation store (8 MiB per workbook,
eight entries, 30-minute lifetime and two generation slots), not a persistent report archive.

## Selected-component current supplier status (2026-10-20 addition)

GET `/api/v1/workspaces/{assignmentId}/long-term-shortages/purchasing` extends
`LongTermShortagePurchasingDto` with `suppliers` (the existing Part Info section/supplier-row shapes)
and `supplierIdentityUnavailable`. This is current enrichment associated only with the selected
component's qualifying conventional open POs, not historical shortage-snapshot facts or calculation
inputs. Existing snapshot/options/selected-component validation remains mandatory; no MRP/BOM read.

Supplier identifiers are taken unchanged from the existing PO reader and deduplicated ordinally.
Existing Shortages batching and exact returned-name matching apply; no new vendor-code join,
normalization or fallback is authorized. Identifiable supplier rows retain nullable risk flags:
true = Yes, false = No, null after successful acquisition = Unknown. An acquisition error marks the
section Unavailable, retaining known identities with null flags. An empty acquired value uses the
established no-supplier-status wording. `supplierIdentityUnavailable` discloses uncovered PO identities;
even explicit No for all identified suppliers cannot mean None across incomplete coverage.

The existing component-only comment request stays separate from the supplier-only request (one
deduplicated request, not a request per supplier/PO); no suppliers means no supplier request, never
suppression of the comment request. Supplier failure does not erase successful POs/comments or other
independently acquired popup sections. Cancellation and snapshot replacement cannot yield a loaded
obsolete result. These fields do not change standalone Part Info, BOM or export contracts/behavior.

## Native save/recovery boundary

Fixed native kind/route `WorkspaceShortages` / `workspace-shortages` uses existing commands and the
typed shared Export menu, preference, feedback and recovery flow. No renderer-supplied paths,
arbitrary URLs, executable selection or browser-download-success fallback. Native chooser remains
destination authority; cancellation retains exact prepared bytes for Save again within existing bounds.
Semantic context changes invalidate preparation; scroll/detail/unrelated renders do not. Late preparation
cannot initiate save after departure/context change. Already-started preparation can finish within
existing bounds; cancellation is not proof of immediate network abort.

Status reconciliation observes the original save operation, never republishes. Opening uses the bound
artifact and existing retry limits; non-Analytical departure/expiry can remove opening authority.
Ordinary success feedback expires after eight seconds; warnings/uncertain/open-failure recovery follow
the existing shared flow. Open dispatch is not proof Excel loaded the file. No source population,
calendar, threshold, classification, precision, cache/freshness or Stage 11 algorithm change is implied.
