# Stage 18.6 Open Order Report contract

COMPLETE / OWNER-ACCEPTED — 2026-10-12. Historical checklist section18.3 corresponds to owner
checkpoint18.6. Acceptance and bounded verification limits: `../implementation/KST_v2_STAGE_18_OOR_CLOSEOUT.md`.
Independent Reports acquisition, never workspace/MPS population.

POST `/api/v1/reports/open-orders` accepts `OpenOrderReportRequestDto`: nullable customerName,
customerNumber, salespersonId, productLineFrom/To, ios, dueDateFrom/To, site and required optionalFields.
Date bounds are Python-compatible input text, not an ISO-only date contract. Blank site selects all
six accepted pairs; explicit site selects its pair. Require a non-date filter (explicit site qualifies).
Paired four-digit product lines (Unicode decimal digits, leading zeros retained, no ascending check);
paired valid ascending dates; allowlisted unique optional headers. Mandatory columns always precede
alphabetically ordered selected optionals. Inputs are trimmed; dates use the application-local year.

`X-KST-Report-Session` is required (D-format GUID). Every Generate acquires fresh data and returns
`OpenOrderReportPreparedDto`: outcome prepared/noData, preparationId/fileName/expiry nullable for
noData, asOfDate/acquiredAtUtc. Failures400/503 never become noData or stale fallback.
GET `/api/v1/reports/open-orders/preparations/{id}/bytes` serves owner/kind-bound immutable XLSX;
400 invalid session,410 unavailable/expired/wrong ownership. Reuse existing 8MiB/30minute preparation,
eight-owner and two-generation limits. No source-row, SQL, path, URL, program or formula authority.

Native fixed report kind OpenOrderReport uses the accepted save/open/status/departure/close foundation.
Optional-column preference only is remembered; other filters start blank, All supported sites default.
No-data has no workbook. Source/schema/capacity failures reject the whole report. Owner UI/output and
export/saving observations are accepted; exhaustive live parity is not asserted.
Authoritative DTO→OpenAPI→generated TypeScript; never hand-edit generated types.
