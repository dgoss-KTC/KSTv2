# Stage 15 — Finished Goods implementation plan

**Final disposition: COMPLETE / OWNER-ACCEPTED — 2026-10-08.** Accepted closeout:
`KST_v2_STAGE_15_CLOSEOUT.md`. Dated authorization and implementation history below retained.

2026-10-07: **15.1 OWNER-ACCEPTED; 15.2 implementation/documentation/alpha.15 AUTHORIZED.**
Accepted rules: `KST_v2_STAGE_15_SOURCE_AND_DECISION_LEDGER.md`. Historical discovery proposals
in `C:\Dev\kst_v2_tmp\Stage15` are superseded by the accepted rules, not implementation evidence.

## Bounded checkpoints

1. **15.1 discovery:** accepted 2026-10-07, with final Stage 15.2 decisions (no Hold, USD,
   mixed-null MAX, positive detail rows and accepted local filter/cache behavior).
2. **15.2 offline implementation:** DTO/spec → OpenAPI/generated TS → backend → frontend →
   version/verification → sidecar republish without launch. IMPLEMENTED / OFFLINE-VERIFIED;
   OWNER-ACCEPTED by explicit owner statement 2026-10-07; offline evidence remains separate.
3. **15.3 validation/acceptance:** separate authorization for desktop and any bounded live-source reads.
    AUTHORIZED 2026-10-07 for Taco normal desktop use; standalone reconciliation requires concrete
    query-pack approval initially; later explicit authorization includes bounded loaded-Taco source checks
    and fixes. Case-sensitive adapter guard corrected, scoped source/API verification passes; owner desktop
    checks and explicit final acceptance recorded 2026-10-08. No Git authority inferred.

## Placement and reuse

- Domain: detail source facts, exact Value and informational RA lot calculation. Snapshot-local identity.
- Application: reader/Delegate, scope-gated acquisition, outcomes and snapshot/store contracts.
- QAD: focused detail/MAX reader using existing connection/options/domain/batching/cancellation.
  No pt/customer/status joins, raw-price fan-out or inventory deduplication.
- Infrastructure: process-local cache. API: thin typed GET/Refresh endpoints, sanitized Problem Details
  and composition-root delegates (concrete QAD depends on Domain, never Application).
- Frontend: generated API client, five-column panel replaces only FG placeholder, exact formatter,
  global density/table styles, three local filters, deterministic sorting/pagination and late-response guards.
- Shared summary, locked 9–11 algorithms and accepted Stage 14 remain intact. No dependencies,
  capabilities, database objects, persistence, exports or Hold enhancement.

## Verification and version

Synthetic/mock tests in Domain, QAD query/normalization, Application scope/stale/cancellation,
API serialization/error routes and frontend filters/acquisition/navigation. No company connection.
Documented cached build/test/generation commands; scoped formatting/lint then regressions, typecheck,
frontend build, architecture/security/version tests and cached Rust checks. Known broad format and
two locked Stage 10 lint exceptions remain exceptions, not passes; no unrelated repair.

Authorized alpha.15: `KstActiveStage=15`, suffix `alpha.15`, prefix `0.1.0`; established version script
and own-package lock metadata only. Tauri installer stays `0.1.0`. Republish using
`scripts/build-sidecar.ps1`, verify binary hashes/metadata only, no health call or desktop launch.
Capture/preserve existing owner build-info/schema/root-plan bytes; report generated changes separately.

Completion evidence goes in the ledger and
`C:\Dev\kst_v2_tmp\Stage15\STAGE_15_2_CHECKPOINT_COMPLETION_REPORT.md`; 15.2 subsequently owner-accepted
2026-10-07. Stage 15 overall acceptance remains pending; current progress in the 15.3 validation report.
Stage 21 export/no-export pending. Completion gate: **A scheduler can inspect and filter current
Finished Goods inventory by product, location and lot.**
