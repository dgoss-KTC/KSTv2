# KST v2 — Stage 7 Work Orders and Kitting Implementation Plan

**Status:** COMPLETE — 7D.0 through 7D.12 implemented and verified; 7D.13 (this document) in progress; 7D.14 owner acceptance pending
**Stage:** 7 — Phase 4: Work Orders and Kitting
**Authority:** `docs/reference/KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist.md`; `docs/implementation/KST v2 — Stage 7 Work Orders and Kitting — VS Code-Copilot Implementation Prompt.md` (original business-rule prompt, sections 1–21)

This document is a retrospective record of what was actually implemented, checkpoint by checkpoint, superseding the original prospective plan now that implementation is complete. See `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` for the accepted rules as currently implemented (including the 7D.11 candidate-rule revision) and `STAGE_7_REAL_DATA_VALIDATION.md` for live-QAD findings.

## Working method

Implemented in controlled checkpoints (7D.0–7D.14) with automated verification and an owner stop-point after each one, per the mandated process. No checkpoint was skipped.

## Layering (reused, not redesigned)

`Kst.Domain.WorkOrders` (pure business rules) → `Kst.Application.WorkOrders` (`WorkOrderDrilldownService`, one orchestration service for all three use cases, not split into separate Kitting/Variance services) → `Kst.Integrations.Qad.WorkOrders` (SQL readers, bridged into Application via `Delegate*` adapters constructed only in `Kst.Api/Program.cs`, preserving the existing rule that `Kst.Application` never references `Kst.Integrations.*`) → `Kst.Api` (`WorkOrderEndpoints`, thin DTO mapping only).

## Checkpoint summary

| Checkpoint | Scope | Outcome |
|---|---|---|
| 7D.0 | Preflight — read prompt/checklist, inspect Stage 5/6 patterns to reuse | Done; no code changes |
| 7D.1 | Domain models: `WorkOrderStatus`, `WorkOrderIssueStatus`, `WorkOrderIssueStatusClassifier`, `WorkOrderMaterialLine`, `KittingSummary`, `WorkOrderSummary` | Done; 30 new Domain tests |
| 7D.2 | QAD readers: `QadWorkOrderSummaryReader` (by-WOID and candidate queries), `QadWorkOrderMaterialReader`, `WorkOrderDrilldownPolicy` (shared depth/limit constants) | Done |
| 7D.3–7D.4 | Application orchestration (`WorkOrderDrilldownService`, cache stores, delegate adapters, DI wiring) | Done |
| 7D.5 | API endpoints/DTOs (`WorkOrderEndpoints`, `WorkOrderDtos`), Problem Details conventions | Done |
| 7D.6 | Frontend Part Info tab bar extended with Work Orders/Shortages/Future Shortages/Components tabs (disabled until a bucket is selected) | Done |
| 7D.7 | `WorkOrderCard` component, top-level bucket → Work Orders wiring | Done |
| 7D.8 | `WorkOrderMaterialGrid` (Kitting detail, search/filter, sort, exception styling) | Done |
| 7D.9 | Manufactured-subassembly candidate drill-down UI (`WorkOrderCandidatePanel`, recursive `WorkOrderCard` reuse, one-branch-per-level state) | Done |
| 7D.10 | Automated verification/gap audit across the full Stage 7 surface | Done |
| 7D.11 | Live-QAD validation — see below | Done |
| 7D.12 | Full regression verification (backend/frontend/Tauri/manual desktop) | Done |
| 7D.13 | Documentation and checklist reconciliation (this document and its siblings) | In progress |
| 7D.14 | Stage 7 completion gate / owner acceptance | Not yet — owner call |

## 7D.11 highlights (live-QAD validation)

- Live Tauri app walkthrough against real QADPRO2 found the implementation matched the accepted contract in every area (top-level schedule context, WO card values, material detail, search usability, refresh behavior).
- One real scope addition from live feedback: added `SalesOrder` (`wo_so_job`) to the card, plus a card-width fix so a single-WO bucket no longer stretched full width.
- A genuine Dapper bug was found and fixed during this checkpoint: a new SQL-projected column (`SalesOrder`) was added in the middle of the card SELECT list while the C# record's matching constructor parameter was last, breaking Dapper's positional deserialization for every work-order-summary read. Fixed by reordering the SQL to match the record's parameter order; recorded as a durable lesson in repo memory.
- The "only 1 candidate WO" observation was investigated against ground-truth QADPRO2 data and found to be correct behavior under the original due-date-bounded rule — not a bug. The project owner then decided to remove that boundary anyway (see `STAGE_7_REAL_DATA_VALIDATION.md` §2 and the contract §7 note) and requested horizontal card stacking; both were implemented, tested, and confirmed live in the same checkpoint.

## 7D.12 highlights (full regression verification)

Backend 468/468 tests, `dotnet format`/build clean; frontend 167/167 tests, lint/typecheck/build clean; `cargo check` clean; sidecar rebuilt and copied; live manual desktop regression (workspace load, MPS load, Part Info, Due/Release toggle, horizon change, Falldown, Work Orders drill-down, single-instance enforcement, clean shutdown) all verified directly against the running Tauri app with real QAD data. Full detail in `STAGE_7_REAL_DATA_VALIDATION.md`.

## Verified totals at Stage 7 completion

- Backend: **468/468** tests passing (`Kst.Domain.Tests`, `Kst.Integrations.Qad.Tests`, `Kst.Application.Tests`, `Kst.ArchitectureTests`, `Kst.Api.IntegrationTests`).
- Frontend: **167/167** tests passing, lint/typecheck/build clean.
- `cargo check` clean; sidecar rebuilt via `scripts/build-sidecar.ps1`.

## Deliberate scope exclusions (unchanged from the original prompt)

Full Component/BOM Detail, BOM explosion, Component MRP, immediate/future shortages, inventory coverage, PO coverage/drill-down, buyer notes, shared-component analysis, finished goods, planning workbook, and sales-order investigation remain out of scope — Stage 8+.
