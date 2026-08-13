# Stage 7 — Work Orders and Kitting: Real-Data Validation

Date: 2026-08-13
Nature of checkpoints: 7D.11 (Live-QAD Validation) and 7D.12 (Full Regression Verification). Verification and one accepted business-rule/UI revision only — no QAD data was created or modified, no database indexes or objects were added.

## 1. Environment

Real QADPRO2 database, server `KNWVM13`, Windows-integrated auth, `READ UNCOMMITTED` isolation (existing repo convention). Validation performed against the real running Tauri desktop app (not a standalone API instance) so the sidecar's actual resolved connection/content-root behavior was exercised, not assumed. Five real dev workspaces available: "Shure SMT", "SHU Metals", "SHU Molding", "Taco", "MSA/Neutronics" (all site `SW` / domain `KTC`).

## 2. Candidate subassembly rule — ground truth and revision

Component `H06-01-6001-33-1` (MSA/Neutronics workspace) has exactly 2 real work orders in the eligible A/F/R status set:

| WOID | Status | Due Date |
|---|---|---|
| 31095079 | A | 2026-08-21 |
| 33185277 | R | 2026-07-31 |

A third `P`-status WO and dozens of `C` (closed) WOs exist for the same component and are correctly excluded by the status filter.

The two real parent WOs consuming this subassembly:

| Parent WOID | Part | Due Date |
|---|---|---|
| 32495142 | HM6-02-6001-67-0 | 2026-08-10 |
| 33098166 | HM6-02-6001-67-0 | 2026-08-17 |

Under the original discovery-phase rule (`candidate Due Date <= immediate parent WO Due Date`), WO 31095079 (due 08-21) is correctly excluded for either parent (both due before 08-21); only WO 33185277 (due 07-31) qualifies. This was confirmed independently via the live backend log: `Candidate work order read for component H06-01-6001-33-1 in site SW returned 1 rows` — matching ground truth exactly, and reconfirmed a second time against parent WO 33098166 specifically via the live app's own on-screen due dates.

**Decision:** although the boundary was confirmed working exactly as designed, the project owner decided during 7D.11 to remove it so schedulers see every eligible A/F/R candidate for a component regardless of the parent's own Due Date. This was implemented full-stack (SQL, reader/service/DI signatures, endpoint text, checklist documentation) — see `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` §7. The `wo_due_date DESC` ordering and the `ParentDueDateUnavailable` precondition (the parent WO must still have *a* Due Date to attempt a candidate lookup) were both retained.

## 3. Live UI feedback and fixes (7D.11)

| Finding | Resolution |
|---|---|
| Work Order card values | Confirmed correct against real data |
| Single-WO bucket card stretched full width | Fixed (`align-self`/max-width, later superseded by the horizontal-stacking layout) |
| Missing Sales Order on card | Added `SalesOrder` (`wo_so_job`), labeled "SO", opposite the Status badge |
| Material detail | Confirmed correct against real data |
| Material search usability | Exceeded expectations per project owner |
| Refresh behavior | Confirmed: a successful refresh collapses Stage 7 drill-down back to Part Info |
| Cards should stack horizontally, one per WO | Implemented (`flex-direction: row; flex-wrap: wrap`); see §4 for the regression this caused and its fix |

A genuine Dapper positional-deserialization bug was found and fixed during this checkpoint (new SQL column added out of position relative to the C# record's constructor parameter order, breaking every work-order-summary read with a generic "Database currently unavailable" UI message). Root-caused via the sidecar's own Serilog file log, not guesswork. Durable lesson recorded in `/memories/repo/troubleshooting.md` (not duplicated here; that file is the canonical location for cross-session gotchas).

## 4. Horizontal card-layout regression and fix

Implementing horizontal card stacking by giving `.work-order-card` a fixed `width: 320px` directly squeezed the (still-nested) material grid to the same 320px width, and — because the candidate panel is itself nested inside a parent card's material `<td colSpan>` — made candidate cards appear to still stack vertically (no room to lay out side-by-side). Fixed with a conditional `--expanded` modifier class: cards stay compact (320px, side-by-side) when collapsed, and only the specific card whose material lines are shown grows to full row width. Verified live via Vite HMR against the already-running app; project owner confirmed both the material-grid width and the horizontal stacking were correct.

## 5. 7D.12 full regression verification

| Layer | Result |
|---|---|
| Backend format/build | Clean |
| Backend tests | 468/468 (`Kst.Domain.Tests`, `Kst.Integrations.Qad.Tests`, `Kst.Application.Tests`, `Kst.ArchitectureTests`, `Kst.Api.IntegrationTests`) |
| Frontend lint/typecheck/build | Clean |
| Frontend tests | 167/167 |
| `cargo check` | Clean |
| Sidecar | Republished via `scripts/build-sidecar.ps1`, copied to `src/tauri/binaries/` |
| Normal `tauri dev` launch | Clean; real MPS query succeeded immediately (38 parts / 630 rows, Shure SMT) |

Manual desktop regression, verified live against the actual running app (screenshot + synthetic-click technique, not just automated checks):

- All 5 real workspaces load.
- Real MPS loads.
- Part Info drill-down works (verified against part `95B57948`: MOQ/price tier 300 @ $25.58, matching prior Stage 6 ground truth).
- Bucket-cell selection correctly enables the Work Orders tab (previously appeared "unresponsive" to a synthetic click before a bucket was selected — confirmed to be the tab's intentional disabled state, not a coordinate/DPI bug).
- Due/Release toggle re-queries and correctly reflows data (no new QAD query — same `snapshotId`, matching the existing local-reprojection design).
- Horizon change (24 → 12 weeks) correctly reflows grid columns.
- Falldown column renders real values.
- Single-instance enforcement: launching a second `kst-tauri.exe` while the first is running exits on its own without spawning a second `Kst.Api.exe`.
- Clean shutdown: terminating the `tauri dev` process leaves zero orphan `kst-tauri`/`Kst.Api` processes.

## 6. Outstanding / not re-exercised live

Rare scenarios not naturally present in the currently available real data (e.g. a manufactured component with more than 10 real A/F/R candidates, exercising `isTruncated`; a component at maximum Level-3 drill depth with real multi-level manufactured subassemblies) remain covered by deterministic automated tests only, consistent with the same pattern already accepted for Stage 5B/6 real-data validation.
