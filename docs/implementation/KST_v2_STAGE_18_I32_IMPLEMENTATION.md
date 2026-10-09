# Stage 18-I3.2 — compact header delivered; single-prompt change blocked

2026-10-09. Stage18 unaccepted; I3.1 focused independent adjudication pending.
Owner decisions D31–D33 recorded in ledger. Application alpha.18 / installer0.1.0 unchanged.

## Delivered

Export-only removal of Inspection Lead, Cumulative Lead and Time Fence. Remaining metadata uses
five/four/four grouped rows; grid begins row10 (previous11), notes shortened vertically accordingly.
No source acquisition/DTO/domain/API changes. Five metric numeric values, UOM rounding, amber and
negative-only Projected QOH red rule retained. No Safety Stock highlighting introduced.

## Single confirmation — exact interface limitation

Source-inspected installed Cargo registry `tauri-plugin-dialog-2.7.3/src/desktop.rs:202–211` converts
`rfd::FileHandle` to path only after async save completes. Installed `rfd-0.16.0` Windows
`file_dialog/dialog_ffi.rs:301–309` creates FileSaveDialog and leaves options at defaults;
`dialog_future.rs:11–25` calls Show, retrieves result only on success, maps failure/cancel to None.
No host overwrite/selection-state hooks are exposed by that public path-only interface. Production
`component_mrp.rs` checks presence in the completion callback, then requests attempt-local hostYesNo.
Default Windows save overwrite prompting is documented under FILEOPENDIALOGOPTIONS; the owner
observed it, but Yes/No/Cancel/change-filename were not exercised by the agent in an isolated chooser.

Successful dialog completion can support consent under controlled behavior; a separate consent receipt
is not inherently required. Missing here is **which completion was an existing-file approved flow**
versus **new-file selection with a file appearing before the post-Show callback check**. Replacing
the app confirmation with returnedPath+exists would grant that latter collision replacement authority.
Therefore duplicate confirmation was **not removed**; D32 remains blocked, not a passed acceptance item.
This is a concrete interface limitation, not a newly asserted vulnerability or universal path-binding gate.

Smallest proposed remedy: narrow host Windows chooser integration retaining FOS_OVERWRITEPROMPT and
recording selection/overwrite state for each candidate, bound to successful final Show/GetResult.
Reset discarded candidates on No/namechange; a pre-prompt OnOverwrite callback alone is **not** Yes.
Final new-file selections remain no-replace. This needs separately bounded implementation/interaction
verification; no registry patch, dependency/capability or new chooser mechanism added in this checkpoint.

Primary references accessed2026-10-09:
https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-_fileopendialogoptions
https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifiledialogevents-onoverwrite

## Verification

Local `C:\dev\kst_v2_tmp\Stage 18\I32_20261009_1030\local-evidence` contains logs/workbooks/previews.
37 scoped backend/architecture,17Rust,11mockedfrontend passed; association test ignored. Added actual
pre/post I3.1 fixture comparisons by metric/bucket for Day/Week/Month and both modes, numeric types,
stored values and fills unchanged. Header absence/useful fields and conditional format checks pass.
First comparison erroneously included blank postgrid columns in Month; corrected test boundary to
heading's last cell, then passed. Not a calculation or preservation regression.
Scoped format, TypeScript, Cargo check and sidecar publish passed. Sidecar copies match by SHA256.
Excel synthetic files explicitly opened read-only/macrosdisabled, Week/wideDay first/continuation
PDFs inspected, complete context/no fragments. Final SafetyStock regrouping rerendered locally.
No app startup/live queries/real association dispatch. Broad formatting/lockedStage10lint and historical
AFT gaps remain exceptions; no new broad scans. Native/frontend/lifecycle sources baseline-identical.

## Owner checklist / next

After focused independent review of current I3.1+I3.2: inspect compact header and both print pages;
existing synthetic destination WindowsYes/appYes, WindowsNo then changefilename, chooserCancel,
appNo preservation; filename numbering and new-name late collision. Singleprompt remains unmet.
Exercise I3.1 close/cancel/workspaceDiscard, navigation unknown and saved-openfailed recovery.
No comprehensive numerical/live acceptance inferred from earlier owner observations.
