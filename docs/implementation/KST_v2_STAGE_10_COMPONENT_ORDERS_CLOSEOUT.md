# KST v2 Stage 10 - Component Orders Closeout

**Status:** COMPLETE / ACCEPTED / LOCKED - 2026-09-14
**Authority:** Project-owner authorization and acceptance recorded 2026-09-14; accepted current
project state is reconciled in `docs/status/CURRENT_PROJECT_STATUS.md` and
`KST-v2-Master-Project-Checklist.md`.

## Delivered Scope

Stage 10 delivers Component Orders as an informational open-PO supply surface for components in the
active workspace's resolved MPS parents and current-effective multi-level BOM. It does not alter
Stage 9, which remains COMPLETE / ACCEPTED / LOCKED.

- A qualifying conventional PO line has positive raw open quantity
  (`pod_qty_ord - pod_qty_rcvd`) and a `pod_status` other than C/X, case-insensitively. No UOM
  conversion or `po_mstr.po_stat` predicate is used.
- Components are grouped by their earliest due qualifying PO line. Expanded rows preserve due-date,
  PO, then line order. Missing due dates are yellow exceptions; dates on or before the applicable
  Friday cutoff are red/late.
- Delivered QAD context includes description/master-data fallback, weeks lead time, PO/line/due/open
  quantity, line confirmation, supplier, workspace-domain buyer, manufacturer item, tracking, and
  the independent effective KSS indicator.

## Read-Only Enrichment And Packaging

Shortages enrichment is read-only. It retrieves the latest active exact site/component Current
Comments record plus Credit Hold and CIA. Current Comments retain multiline content in an accessible

Supplier risk uses the owner-directed, KSTv1-compatible exact supplier-display-name lookup to
`PreferredSuppliers.[Supplier Name]`, with `Date DESC, ID DESC` duplicate precedence. The earlier
`po_vend`/Supplier-Nbr evidence remains historical/superseded, not a runtime fallback.

The packaged application uses the untracked `src/tauri/resources/secrets.json` only through the
release-only Tauri configuration. No real secret is versioned. The tracked example schema and
operator setup guide are safe repository artifacts.

## Evidence

- Backend solution build: 0 warnings / 0 errors.
- Focused automated evidence: Component Orders application 17/17, QAD 11/11, Shortages 27/27, API
  integration 10/10, and architecture 9/9.
- Frontend evidence: Component Orders panel 11/11, presentation 15/15, and production frontend
  build.
- Owner live UAT succeeded: Credit Hold, CIA, and Current Comments worked in the running
  application.

## Explicit Deferrals

Component Orders is informational only. It does not claim PO supply covers, reserves against, or
will clear a Stage 9 shortage. Projection, coverage, netting, and clear-date logic belong to Stage
11 - Future Shortages and Component MRP.

Current Comments are read-only. No ShortageMaster write, local note persistence, or export-note
update was authorized or delivered. A separate PO drill card, previous/next navigation,
write-capable notes, and no-open-PO detail are not part of the accepted informational scope.

`PERF-001` remains deferred. The initial cache-miss profile showed QAD PO reads as the dominant
phase, and a reader-local 500-to-250 batch experiment was only modestly faster. No further tuning
is authorized until a normal shared QAD/MPS baseline and DBA-reviewed plan/index evidence are
available.
