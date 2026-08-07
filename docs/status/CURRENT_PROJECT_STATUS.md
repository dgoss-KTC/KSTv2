# Current Project Status

Date: 2026-08-07  
Workstation: Windows (`C:\Dev\kst_v2`)  
Current stage: Stage 5B — MPS Dashboard Implementation  
Stage 5A status: **COMPLETE / ACCEPTED — 2026-08-07**  
Stage 5B status: **READY TO BEGIN**

## Current Position

Stages 1–4B are complete. Stage 5A completed the MPS data/source investigation, accepted the production query strategy and data contracts, reconciled project documentation, produced the Stage 5B implementation plan, and received final project-owner acceptance on 2026-08-07.

Stage 5B is now the active project stage. Implementation should follow the accepted Stage 5B plan in controlled checkpoints, beginning with repository preflight/reconciliation before production changes.

## Accepted Stage 5A MPS Decisions

- Workspace scope is site + product-line/range and/or explicit parent parts; customer code and IOS are not authoritative scope inputs.
- Domain is inferred from site at the QAD integration boundary.
- Initial MPS uses a direct parameterized SQL Server 2016-compatible query in `Kst.Integrations.Qad`; it does not call `sp_QAD_ktmpswkm` and does not require a new stored procedure/TVF.
- QAD integration uses the existing architecture: `Microsoft.Data.SqlClient`, Dapper, Windows-integrated authentication, and read-only access.
- MPS source rows are work-order-backed `mrp_det` facts with `mrp_dataset = 'wo_mstr'` and `mrp_type IN ('supply','supplyf','supplyp')`.
- MRP-to-WO identity uses domain + site + part + WO number + WO ID.
- Closed work orders are excluded.
- RMA work orders (`wo_bom_code = 'RMABOM'`) are excluded.
- `rps_mstr` repetitive-schedule rows are intentionally not consumed directly; schedulers should run MRP after schedule changes, with overnight MRP providing eventual reconciliation.
- All qualifying historical unfinished work is retained for due-date Falldown; future coverage supports the maximum 72-week Due/Release horizon.
- Due Date and Release Date are retained in the same source snapshot so view changes are local.
- A/F/R drive execution-state presentation; multiple distinct A/F/R states produce Mixed. P and e are independent Planned / Explicitly Scheduled presentation flags.
- MPS snapshots load automatically with the workspace, replace atomically after successful refresh, retain old good data on refresh failure, and are not persisted across sessions initially.
- Fiscal calendar logic is frontend-only. FY26 starts June 29, 2025; normal years use 4-4-5 × 4; users maintain only 53-week exceptions and choose which period receives the extra week.

## Stage 5A Durable Artifacts

- `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `KST_v2_STAGE_5A_DATABASE_ACCESS_PERFORMANCE_STRATEGY.md`
- `KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `KST_v2_STAGE_5A_FISCAL_CALENDAR_STRATEGY.md`
- `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md`
- `KST_v2_STAGE_5B_VSCODE_IMPLEMENTATION_PROMPT.md`
- `KST_v2_Master_Project_Checklist_STAGE_5_REVISION.md`
- `KST-v2-Master-Project-Checklist.md`
- `KST v2 Revised Phased Implementation Strategy — Stage 5A Reconciled.docx`

## Stage 5B Next Action

1. Start a fresh VS Code/Copilot agent conversation using `KST_v2_STAGE_5B_VSCODE_IMPLEMENTATION_PROMPT.md`.
2. Begin with Stage 5B.0 repository preflight rather than immediately writing MPS production code.
3. Progress through controlled checkpoints: QAD connectivity → source query → normalization → week/status logic → snapshot/refresh → API/OpenAPI → frontend fiscal calendar → real MPS grid → validation/closeout.

## Previous Verified Foundation

Stage 3 Technical Foundation remains PASS. Stage 4 / 4B application shell, workspace configuration, and workspace scope extension are complete and are the accepted foundation for Stage 5.

Reference historical commits:

- `d0e592b` — initial technical foundation
- `6f5644c` — Stage 3 final closeout
- `ce717a1` — Stage 4 initial implementation slice
