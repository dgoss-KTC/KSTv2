# KST v2 -- Stage 9.9 Full Regression

**Status:** COMPLETE / ACCEPTED — retained full-regression evidence.

**Date:** 2026-09-08

## Scope and Boundaries

This is Stage 9.9 regression evidence only. No production behavior, Stage 9 business rule, API contract design, Stage 9.7 scenario inventory, or Stage 9.8 live-QAD source validation was changed or reopened. Stage 9.10 and Stage 9.11 were not started.

The preflight baseline was `main` at `3e2805d` (`feat: close Stage 7R work order planning window`), tracking `origin/main`. The worktree was already dirty with the uncommitted Stage 9 implementation, audit, live-QAD validation, generated-contract, data-map, and unrelated documentation changes. Those changes were preserved. No merge conflicts were present.

## Automated Verification

| Area | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build Kst.slnx --no-restore` | PASS -- 0 warnings, 0 errors. |
| Kst.Domain.Tests | `dotnet test Kst.slnx --no-restore` | PASS -- 194 passed. |
| Kst.Integrations.Qad.Tests | `dotnet test Kst.slnx --no-restore` | PASS -- 136 passed, including `QadReadOnlySqlTests` read-only SQL/security guards. |
| Kst.Application.Tests | `dotnet test Kst.slnx --no-restore` | PASS -- 286 passed. |
| Kst.ArchitectureTests | `dotnet test Kst.slnx --no-restore` | PASS -- 9 passed, including dependency and version-consistency architecture guards. |
| Kst.Api.IntegrationTests | `dotnet test Kst.slnx --no-restore` | PASS -- 141 passed, including loopback-binding coverage. |
| Backend total | `dotnet test Kst.slnx --no-restore` | PASS -- 766 passed; no failed or skipped tests reported. |
| Backend format | `dotnet format Kst.slnx --verify-no-changes --no-restore` | PASS. |
| Frontend lint | `npm run lint` | PASS. |
| Frontend typecheck | `npm run typecheck` | PASS. |
| Frontend tests | `npm test` | PASS -- 17 test files passed; 313 tests passed; 3 skipped. |
| Frontend build | `npm run build` | PASS. |
| Contract regeneration | `npm run generate:types` | PASS -- regenerated `src/frontend/src/generated/api.ts` from `docs/openapi/Kst.Api.json`; no unexplained drift. The existing Stage 9 OpenAPI and generated TypeScript additions remain aligned. |
| Tauri compilation | `cargo check` in `src/tauri` | PASS. |
| Sidecar rebuild | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-sidecar.ps1` | PASS -- published `Kst.Api.exe` and copied it to `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`; application settings were copied beside it. |
| Sidecar bundle declaration | `src/tauri/tauri.conf.json` | PASS -- still resolves external binary `binaries/Kst.Api`. |
| Generated/binary worktree check | `git status --short` and `git check-ignore` | PASS -- published sidecar, copied sidecar, and copied settings are ignored; no unexpected generated/binary repository change appeared. |
| Diff validation | `git diff --check` | PASS. Git emitted only existing LF-to-CRLF normalization warnings; it reported no whitespace errors. |

### Frontend Skipped-Test Disposition

The three skipped tests are unchanged from the accepted Stage 9.7 audit, all in `src/frontend/src/components/MpsWorkspace.test.tsx`:

| Skipped group | Disposition |
| --- | --- |
| Legacy standalone material-grid expansion/collapse behavior | Obsolete retired `/work-orders/{woid}/material` workflow; superseded by the integrated Stage 9 Show/Hide immediate-material flow. |
| Legacy no-applicable-material empty wording | Obsolete endpoint wording; covered by the current `ShortagesPanel` empty-components behavior. |
| Stage 7D.8 kitting material-grid group | Unrelated legacy Stage 7 coverage; direct `WorkOrderMaterialGrid` coverage remains passing and it is not Stage 9 evidence. |

## Manual Desktop Regression Matrix

**Current disposition:** owner-guided validation has not started because the established `cargo tauri dev` workflow cannot launch in this local environment. Each family remains pending owner observation; no PASS, PASS WITH QUALIFIER, NOT OBSERVED, or FAIL is claimed below.

| Family | Owner action and expected result | Owner-observed result | Disposition |
| --- | --- | --- | --- |
| 1. Workspace / startup | Start the desktop app, load existing workspace configuration, select a workspace, and confirm no unexpected startup error. | Application started normally; existing workspace configuration loaded; workspace selection worked; no unexpected error observed. | PASS |
| 2. MPS | Load MPS; confirm layout/grid intact and no Stage 9 shortage marker in the MPS grid. | MPS loaded; grid/layout remained intact; no Stage 9 shortage marker appeared in the MPS grid. | PASS |
| 3. Part Info | Open Part Info; confirm expected master/site data and normal close/navigation. | Representative parent Part Info appeared correctly; normal navigation was confirmed. | PASS |
| 4. Work Orders | Open Work Orders through scheduler flow; confirm card strip, Stage 7R population, and top-level/subassembly navigation. | Work Order cards remained visible in the horizontal card strip; accepted Stage 7R population was visible; top-level navigation worked. | PASS |
| 5. Show/Hide material lines | Open selected WO integrated material panel; confirm card strip remains above, one active analysis, correct toggle, and collapse in Work Orders. | Show material lines opened the integrated immediate-material panel beneath the card strip; Hide collapsed it. Single-active-analysis behavior and explicit toggle-label transition were not separately observed. | PASS WITH QUALIFIER |
| 6. Material rows | Confirm representative rows show Component, Description, BOM Qty, Issued Qty, Variance Qty, Issued %, and Short; confirm accepted Actual versus Projected semantics. | All required columns were verified working. Projected rows displayed the accepted N/A/null behavior. | PASS |
| 7. Purchased shortages | Where naturally available, confirm purchased Short distinction, plausible quantity, card indicator, and no MPS-grid marker. | A purchased shortage was verified with visual distinction and plausible Short quantity; no MPS-grid shortage marker appeared. The card-indicator-only applicability was not separately observed. | PASS WITH QUALIFIER |
| 8. Manufactured components | Confirm visible friendly status, N/A Short, no debug output/card shortage effect, and candidate-WO drill-down. | Manufactured components appeared in the list with a drill-down arrow beside the part number. Friendly status, N/A Short, lack of diagnostic/card-shortage contribution, and completed candidate-WO navigation were not separately observed. | PASS WITH QUALIFIER |
| 9. Unknown / Data Issue | If naturally available, confirm visual distinction, N/A Short, and no fabricated PO/NO PO. Otherwise record NOT OBSERVED. | Owner reported PASS. | PASS |
| 10. PO context | For a naturally available conventional shortage, confirm applicable PO details or non-KSS NO PO and that PO does not reduce shortage. | Owner reported PASS. | PASS |
| 11. KSS context | Where supplied examples `126615` or `105808` naturally occur, confirm KSS behavior and any exceptional real PO context; otherwise record NOT OBSERVED. | Owner reported PASS. | PASS |
| 12. Shortages tab | Open selected analysis; confirm Work Orders to Shortages and back retains selected analysis/context. | Owner reported PASS. | PASS |
| 13. Due / Release basis | Exercise both bases; confirm data-supported population change and no stale cross-basis analysis. | Owner reported PASS. | PASS |
| 14. Four-week horizon | Confirm Falldown plus Weeks 0-3 and same Stage 7R window for manufactured candidates. | Owner reported PASS. | PASS |
| 15. Falldown | Where present, open a Falldown WO and confirm material analysis operates normally; otherwise record NOT OBSERVED. | Owner reported PASS. | PASS |
| 16. Component drill-down | From a manufactured component, open candidate WOs and nested part-specific WOs; confirm no fabricated pegging. | Owner reported PASS. | PASS |
| 17. Escape hierarchy | From deep applicable state, verify one-level unwind order and focus restoration to Show/Hide when the immediate-material panel closes. | Initial FAIL: with Material Detail open in a manufactured part's second-level Immediate Material Analysis, Escape exited to the prior analysis level. After bounded M001 correction and automated regression, owner performed the targeted retest and confirmed Escape now works as specified: Material Detail closes first and subsequent Escape presses unwind one level at a time. | PASS |
| 18. Clean shutdown | Close app, confirm no unexpected sidecar/backend process remains, and restart successfully if needed. | Owner reported PASS. | PASS |

## Defects

No automated production regression was discovered.

### Stage 9.9-M001 -- Escape closes too many investigation levels

- **Failing workflow:** Stage 9.9 manual family 17, Escape hierarchy.
- **Expected behavior:** When Material Detail is open, the first Escape closes only Material Detail. A later Escape closes the manufactured candidate branch, then the selected WO immediate-material panel; each Escape unwinds exactly one level.
- **Observed behavior:** With Material Detail open from a manufactured component's second-level Immediate Material Analysis, Escape exits to the prior analysis level rather than closing only Material Detail.
- **Implicated area:** Frontend nested immediate-material/candidate-detail Escape handling, including `src/frontend/src/components/MpsWorkspace.tsx`, `src/frontend/src/components/ShortagesPanel.tsx`, and the Material Detail surface integration.
- **Root cause:** Material Detail state in `WorkOrderCandidatePanel` was local to the nested candidate panel, while the ancestor `MpsWorkspace` capture-phase Escape handler only knew about top-level Material Detail state. It therefore popped the nested selected-WO Escape-stack entry before the nested modal's same-document handler could close Material Detail.
- **Correction:** `WorkOrderCandidatePanel` now registers an open nested Material Detail through the existing `useEscapeLevel` stack after its selected nested-WO level. The first Escape consequently pops only the detail registration, clears only local detail state, and restores focus to the initiating row. The selected nested WO analysis and candidate panel remain mounted. The next separate Escape pops the existing selected nested-WO level.
- **Automated evidence:** Added the integrated-path regression `Escape closes nested Material Detail before unwinding its second-level immediate material analysis` in `MpsWorkspace.test.tsx`. It opens Work Orders, top-level analysis, manufactured candidate panel, nested analysis, and nested Material Detail. It verifies first Escape closes only Material Detail while both analysis levels remain, and second Escape closes only the nested analysis while the containing analysis and candidate panel remain.
- **Verification:** Targeted M001 regression PASS; `MpsWorkspace.test.tsx` PASS (77 tests, 3 accepted skips); `ShortagesPanel.test.tsx` PASS (12 tests); `ComponentInfoModal.test.tsx` PASS (37 tests); full frontend suite PASS (17 files, 314 passed, 3 accepted skips); typecheck, lint, build, and `git diff --check` PASS.
- **Disposition:** Bounded frontend correction implemented and owner-retested PASS. No backend/API/business-rule behavior changed.

## Qualifiers

- The initially attempted Cargo `tauri` subcommand is absent. Per owner clarification, it is not the established KST launcher and is not a Stage 9.9 blocker. The approved `npx @tauri-apps/cli dev` workflow launched successfully from `src/tauri`.
- No tooling was installed and no Tauri architecture was changed.
- The known Stage 9.8 `po_mstr.po_stat` deferral remains an owner/QAD-expert evidence question and is not a Stage 9.9 regression failure.
- The backend test output displayed the Fluent Assertions licensing notice. Tests passed; this is not a test or application failure. Any licensing governance disposition remains outside this regression checkpoint.

## Completion Gate

Automated, contract, Tauri compilation, sidecar, and diff checks are green. Manual families 1-18 passed, including owner confirmation of the targeted Family 17 M001 retest. No unresolved FAIL or NOT OBSERVED disposition remains. Stage 9.9 satisfies its regression completion gate, pending project-owner review.

No commit or push was performed.
