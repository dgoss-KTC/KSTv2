# Stage 19 Part 1 — Historical Shipments backend foundation

Historical implementation plan. Part 1 and Stage 19 overall were subsequently COMPLETE / OWNER-ACCEPTED
on 2026-10-13; see `KST_v2_STAGE_19_CLOSEOUT.md`. Requirements and verification details below remain
the original plan/evidence, not a claim that each planned check was performed.

Authority: AGENTS.md, enacted security policy, accepted Stage 18 closeouts, the owner's
Stage 19 Part 1 authorization, `docs/reference/STAGE_19_HISTORICAL_SHIPMENTS_DISCOVERY.md`
and the supplied catalog evidence under `C:\Dev\kst_v2_tmp\Stage 19`.
The owner's explicit invoice-only, all-channel, unbounded-calendar-date and physical-detail
requirements supersede the legacy discovery's candidate filters and mixed population.

## Contract and sequence

1. C# DTOs and named HTTP contract, OpenAPI build and generated TypeScript types.
2. Domain column/filter rules; Application reader/service and operation-store interface;
   QAD-only parameterized streaming reader; Infrastructure file-backed immutable results.
3. Streaming XLSX in Kst.Exports and host-owned file-backed transport through the existing
   protected native destination chooser/publication/open workflow.
4. Typed frontend transport helpers and offline verification; UI handoff to Sonnet, then
   final integrated verification in this same checkpoint after UI implementation.

## Acceptance design

- Required single supported site, domain from QadSiteDomainMap; historical customer ranges,
  inclusive item/product-line/salesperson ranges, mutually exclusive direct corporate group.
- Header association remains the source-supported domain/invoice association. Zero/multiple
  matches fail the entire acquisition explicitly. No invented sales-order tie-breaker.
- Membership uses EXISTS through address/business relation/group. Global relations need not
  match a domain; restricted relations use the supplied domains bridge. Invoice scope stays fixed.
- Current optional enrichment uses only unambiguous records; absent/ambiguous values are null.
- Client-known operation IDs are registered before acquisition. Release invalidates publication,
  cancels pending acquisition/preparation and removes owned local files after outstanding leases.
  Timer-driven expiry and process disposal clean files; no history-wide frontend payload.
- Rows and disk offsets are streamed to operation-local files, pages bounded to 200 rows.
  Export reads the frozen rows and uses the same effective selected columns as display.
- XLSX is streamed ZIP/XML with literal inline strings. Identifiers remain text; numbers beyond
  Excel's reliable 15 significant digits remain exact decimal text. No formulas/totals/conversion.
  Worksheet row and cell limits fail the whole preparation explicitly; no partial success.
- Native streaming transport replaces history-wide byte buffering only for this report and
  preserves chooser-bound overwrite, no-replace, identity/content checking, retry/status and
  active save/open ownership. Existing report limits and behavior remain intact.

## Verification

Offline synthetic expectations cover filters/date bounds, SQL scope/membership/no multiplying
joins, missing historical masters, signed distinct physical details, header failures, unavailable
enrichment, precise literal workbook content and sizes beyond Stage 18 limits. Lifecycle tests
cover cancellation/release/late publication/expiry/failure cleanup. Independently inspect ZIP/XML
and open the produced workbook through the existing installed workbook reader. Run scoped .NET,
architecture/API tests, contract generation, TS/lint and Rust checks. No live queries or new
dependencies/capabilities. Known broad formatting, Stage 10 lint and AFT exceptions are reported
as exceptions. Sidecar republish and full UI integration follow the generated-client UI handoff.

## Preserved work and limits

Initial tree: modified generated Tauri desktop/windows schemas; untracked Stage 19 discovery
Markdown and supplied legacy source. Preserve these. Stage 18 accepted limitations stand;
Stages 9–11 remain locked. No retention/performance or live-source correctness claim. No owner
acceptance, database writes, installations, commits, pushes or fetches.
