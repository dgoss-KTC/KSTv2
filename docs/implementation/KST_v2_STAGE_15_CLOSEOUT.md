# Stage 15 — Finished Goods closeout

**COMPLETE / OWNER-ACCEPTED — 2026-10-08.**

Authority: owner explicit final acceptance after desktop checklist, following 15.1/15.2 acceptance
and authorized 15.3 correction/validation. Accepted rules remain in
`KST_v2_STAGE_15_SOURCE_AND_DECISION_LEDGER.md`; dated amendments and historical evidence preserved.

## Delivered and corrected

Five-column workspace-scoped positive physical inventory, exact USD domain/part MAX valuation,
informational case-insensitive/no-trim RA lot badge, local literal AND filters and compatible snapshot/
Refresh/stale handling. No Hold/readiness/date/demand/export functionality.

Taco failure was evidenced as InventoryScopeInvalid: three case-only site variants passed SQL CI
scope but failed adapter equality. Domain/site guard now OrdinalIgnoreCase; canonical context emitted,
raw location/lot/reference and multiplicity unchanged. Static reason/type/Activity trace logging contains
no raw exception payload, SQL or operational values. Public error contract unchanged.

## Attributed evidence

- **Synthetic/offline/mocked:** final full backend 1,182 passed; additional safe-log test afterward,
  focused Application13 passed (not a single1,183 full run). Focused frontend15 passed. Scoped format,
  AFT diagnostics and diff whitespace checks passed.
- **Agent-observed live-read/runtime:** actual Tauri development child at
  `src/tauri/target/debug/Kst.Api.exe`, alpha.15, matching published/external/executed SHA256
  `EEBC0B71A65DA2E3F9C59FB0D90E2E5DD3AEFA07F3336FE65C61008684DFE858`.
  Taco assignment7a8b90a4-0937-4016-b7f1-f747229a33ac/siteSW/domainKTC, loaded MPS
  3e7396c3-6258-4cc0-baa4-460bd659d039,17 resolved parents,16rows,6RMA.
  Acquisition/cache/POSTRefresh104/5/64ms; cached time/id retained, successful refresh new time/id;
  incompatible-MPS409. Ten targeted metadata fields,17price envelopes and16physical rows reconciled:
  native/text quantity/price and exact reference-preserving quantity/MAX/value multiset mismatches0.
- **Owner-run — 2026-10-08:** visible loading, mixed-case partial filters, cached time/Refresh,
  scroll/density/keyboard and compatible workspace/MPS switching accepted. Owner explicitly accepts
  Stage15 and synthetic attribution of unobserved edge cases. No additional specific UI observations
  are inferred from this acceptance.

## Retained limits and exceptions

Source reconciliation covers Taco at observed time, not untested sites/domains or a unique-key proof.
READ UNCOMMITTED acquisition is non-atomic and may have time skew. Observed quantities were integral;
fractional/missing/negative/all-null/duplicate-exact-tuple/fault cases remain synthetic, accepted as such.
No invented SLA, installer validation or independent security review.
Prior default frontend run had8failures; bounded-worker full run660passed/5skipped. Broad .NET/Rust
format exceptions and two locked Stage10 lint errors remain exceptions, not passes.

Operational rows remain only in operator-restricted local application scratch. Shared reports contain
sanitized findings. Detailed history: `C:\Dev\kst_v2_tmp\Stage15\STAGE_15_3_VALIDATION_REPORT.md`;
historical15.2 completion report retained. Earlier sidecar hash discrepancy and owner rebuild attribution
remain historical evidence; corrected runtime identity was separately verified.

## Preservation and disposition

Version stays0.1.0-alpha.15. Protected frontend build-info, both Tauri schemas and empty root Stage14
plan match original hashes at acceptance recording. Accepted Stage14 and locked9–11 algorithms preserved.
Work remains uncommitted on main at230c60d; no staging/commits/pushes authorized or performed.
**Stage21 export/no-export remains PENDING.** No next-stage implementation authorized by this closeout.
