# Finished Goods — Stage 15.2

**Implemented / offline-verified; 15.2 OWNER-ACCEPTED — 2026-10-07.**
15.3 validation authorized; standalone query approval and Stage 15 overall acceptance pending.
Rules/evidence: `docs/implementation/KST_v2_STAGE_15_SOURCE_AND_DECISION_LEDGER.md`.
Mechanical contract: generated `docs/openapi/Kst.Api.json`.

- GET `/api/v1/workspaces/{assignmentId}/finished-goods?mpsSnapshotId={guid}`
- POST `/api/v1/workspaces/{assignmentId}/finished-goods/refresh?mpsSnapshotId={guid}`

Required nonempty current MPS ID; scope is its already-resolved parts at the assignment site,
domain resolved within QAD. No MPS/Open Orders acquisition or additional account gates.
Each positive physical inventory row is retained, including identical-looking rows and references;
row ID is snapshot-local, not an asserted durable database key. Raw location/lot/reference retained,
Location uppercase display. Informational RA-prefix lot badge never excludes inventory.

Prices are separate preaggregated domain/part MAX envelopes with record/non-null counts, no
site/date/feed/customer/list/MOQ restrictions. No records → explicit zero; mixed null/valid → MAX;
all-null/incomplete/malformed/failed read → acquisition failure. Zero and negative MAX remain valid.
All numeric transport is invariant exact decimal text, USD; two-place Value display only.
Unrepresentable operands/products fail rather than silently rounding or overflowing.

Each complete success gets a new FG snapshot ID and UTC acquisition time. GET reuses only compatible
process-local data. Refresh failure keeps compatible last-good ID/time with `isStale` and warning;
initial unavailable is 503, never successful empty. Empty scope requires no source call; successful
no-positive-inventory response contains zero rows, no fabricated per-part zero rows.
Assignment/site/MPS rechecked before acquisition/publication/fallback. Cancelled work publishes nothing.

HTTP: 400 invalid MPS ID, 404 unknown/deleted assignment, 409 missing/changed MPS/site scope,
503 unavailable without compatible data; 200 complete/empty/stale response. Errors contain no raw SQL,
connection details or business rows. No date selector or current-time guarantee: READ UNCOMMITTED and
multi-batch acquisition are reporting conventions, not a transactionally atomic inventory instant.

Frontend filters Product/Location/Lot by case-insensitive literal contains, AND across full response;
no trimming, wildcard/regex, source reread or source truncation. Rendering pages 100 rows, deterministic
Product/Location/Lot/Reference/raw-location/row-ID order. New session per assignment/MPS/site ignores
late A→B→A responses. No Hold, readiness, demand, editing or export. Stage 21 disposition pending.
Live source precision/grain/collation/performance and desktop interaction remain unverified 15.3 work.
