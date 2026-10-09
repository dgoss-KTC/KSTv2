# Stage 18-I3.1 — lifecycle and print corrections

Implemented 2026-10-09; pending focused independent re-review and owner laptop verification.
Stage 18 remains unaccepted. Application `0.1.0-alpha.18`, installer `0.1.0` unchanged.

## Corrections to independent I3 findings

- **R1:** host report state latches normal close/exit, prevents new report commands, and holds
  window/backend through chooser, staging, publication/cleanup, automatic open and explicit retry.
  Generation also holds close. Terminal records precede release; close re-enters the existing
  workspace-discard rules. Cancel closing clears the host latch without cancelling publication.
  Runtime exit checks the same workspace guard. Waiting state is visible in the shell.
- **R2:** Reports stays mounted, hidden outside its tab. Pending/unknown/failed-open state and opaque
  IDs survive navigation; hidden completion updates it. Active operations guard tab navigation;
  unknown inactive operations allow departure/return. Unmount no longer sends a rejected host reset.
- **R3:** native IPC outcome adds `operationComplete` independently of `saved`. Commitment events
  expose Saved/opening-in-progress immediately; terminal events and status settle later. Reconciliation
  retains ID until terminal; terminal-event IDs suppress stale status and lost-response downgrades.
  Host events/status never initiate launches. Artifact checked retry uses the same bounded grant logic.
- **R4:** only grid labels occupy repeated column A. Metadata/notes start at B; first horizontal
  break ends at I so merged notes stay on page one. Header/footer carry part/site/period/date range
  on every page; continuation has no empty metadata labels or clipped notes. Single contiguous area,
  Letter landscape, fixed readable scale, five rows and no screen panes retained.

These are implemented corrections, not independent adjudication or owner acceptance. Forced kill,
crash, power loss and deliberate local namespace manipulation limitations remain unchanged.

## Evidence and limits

Local evidence: `C:\dev\kst_v2_tmp\Stage 18\I31_20261009_0845\local-evidence`.
37 scoped backend/architecture tests passed; 11 frontend tests passed; 17 Rust tests passed,
one real-association test ignored. Typecheck, scoped eslint, frontend build, Cargo check and scoped
backend/Rust formatting passed. Sidecar republished; final copy hashes recorded in preservation.

Synthetic native tests pause actual production publication after staged flush, before rename and
after commitment using channels (no sleep), verify close deferral, exact published bytes, terminal
recording and no remaining owned temp. Existing no-consent/collision/injected/locked/read-only and
artifact rejection regressions pass. Controlled artifact launch tests use production verify path,
pause mocked launch success/failure, observe status and close barrier; no real association dispatch.
Production retry grant tests check replay refusal/dedup and Cancel closing. Workspace guard tests pass.
Frontend mocked host events/IPC test response loss, intermediate Saved, hidden completion, terminal
open failure, stale intermediate suppression; retained hidden panel tests both terminal success/failure,
navigation recovery and Retry Open. Normal chooser/exit/window integration is source-inspected,
not executed desktop verification; no isolated startup was asserted. Actual OS close and workspace
Discard/Cancel interactions remain owner checks.

Installed Excel rendered synthetic workbooks read-only with macros disabled to PDFs. Week and wide
Day first/continuation pages inspected: complete context, readable dates/five metrics, no label/notes
fragments; no physical printing. Long-description synthetic workbooks also retained. Owner laptop
Excel/printer settings remain unverified. No live acquisition/numerical parity.

No C# DTO/OpenAPI change; IPC follows existing Rust camelCase outcome convention. No dependency,
lock, capability, shell permission, version or locked business algorithm changes. Full-solution
formatting and locked Stage 10 lint exceptions remain outstanding, not passes. AFT reports incomplete
C# diagnostics and missing rust-analyzer; authoritative compiler checks passed instead.

## Next checkpoint

Focused independent re-review of actual source/diff and these scoped outputs, then owner laptop
checklist: close during chooser/save/open (including Cancel closing and workspace Discard/Cancel),
tab leave/return with unknown and open-failed recovery, observational status without replay,
overwrite Yes/No, receipt inclusion/amber, Day/Week/Month and print continuation. Stage 18 is unaccepted.
