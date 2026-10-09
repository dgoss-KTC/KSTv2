# Stage 18-I3.3 — single Windows overwrite prompt implemented

2026-10-09. Pending focused independent review; Stage18 unaccepted. I3.1 adjudication still pending.
Application alpha.18 / installer0.1.0 unchanged. No backend change or sidecar republication.

## Integration

Host-only `component_mrp_chooser.rs` calls Windows FileSaveDialog on a dedicated COM STA worker,
with borrowed main-window HWND and synchronous modal Show. Explicit FOS_OVERWRITEPROMPT,
FOS_FORCEFILESYSTEM and DONTADDTORECENT added to retained default options. Existing suggested
filename/sequence and remembered folder supplied unchanged. No frontend path/replacement authority.
Minimal local platform COM ABI follows installed rfd0.16 Windows vtable ordering and installed SDK
declarations; no package fork/registry edits/direct dependency/feature/capability changes.

Event sink is reference-counted, advised for synchronous Show, unadvised then released before COM
teardown. OnOverwrite returns FDEOR_DEFAULT, **never ACCEPT**. Records candidate path/identity;
this event alone cannot authorize saving. OnFileOk records matching final candidate and consumes
event state; replacement requires matching overwrite path+file identity and final successful
Show/GetResult matching the accepted path. Failure to inspect identity degrades to no-replace.
Folder/selection/type/share changes reset state; new overwrite events reset prior state. Cancellation
returns None and discards entire attempt. No path.exists postcompletion authority decision.
There is no callback specifically for WindowsNo; it cannot alone produce OnFileOk/Show success.
Actual same-name resubmission generated a fresh overwrite event; changed final name cannot match
previous candidate. Identity check additionally prevents same-name deleted/recreated candidate from
using stale evidence. Approved postselection namespace race limits remain; no continuous binding claim.

`component_mrp_save` replaces plugin chooser+appYesNo with this typed Selection(path,replace), then
uses the unchanged protected publication. Cancellation settles normally retaining preparation.
Chooser errors record failed/no publication and release close barrier. Report busy covers chooser;
close state/dedup/status/preference/artifacts/open outcome logic retained. Only Windows prompt remains.

## Evidence

`C:\dev\kst_v2_tmp\Stage 18\I33_20261009_1200\local-evidence` retains logs/synthetic sentinel files.
19Rust tests passed,2interactive tests ignored in regression run (one realassociation, one chooser);
11frontend mocked lifecycle tests and typecheck passed; Cargo check/scopedrustfmt passed.
Candidate tests demonstrate preprompt alone is insufficient, change/reset/new attempt clears consent,
and accepted newfile with controlled late collision invokes actual production no-replace, preserves
sentinel and cleans owned temp. Existing replacement/read-only/locked/precommit/cleanup/artifact and
channel-paused lifecycle regressions pass. Workbook/backend/frontend production hashes unchanged.

**Actual isolated Windows interaction**, no app/sidecar/live acquisition/publication/association:
test invokes production chooser with owner0 and synthetic folder; WScript.SendKeys drove prompt:
- Yes: overwrite→file-ok replace=true; successful existing candidate.
- No then same filename submission/Yes: overwrite→overwrite→file-ok replace=true.
- Cancel before: no overwrite/file-ok; None.
- No then Cancel: overwrite only; None.
- No then folder+new filename: overwrite→folderchange→file-ok replace=false.
- No then changed filename: overwrite→file-ok replace=false (earlier local trace; binding also tested).
Sentinel files are not published by chooser tests. First automation could not activate title while
the native test later completed; that initial trace alone does not attribute user/automation Yes.
Subsequent matrix explicitly found test process HWND and recorded controlled sequence/results.
Interaction automation uses pauses only for input dispatch; lifecycle correctness tests use channels.
These native observations establish this workstation's callback ordering, not all Windows versions.

Owner-unverified: real main HWND modality, integrated close/Cancel-closing/navigation while chooser
active, filename hints/sequence in desktop and end-to-end chooser→replacement. Retained host source
and synthetic lifecycle tests cover coordination; standalone chooser is not full desktop proof.
No physical print or live parity. AFT missingrustanalyzer remains gap; broadformat/lockedStage10lint
exceptions remain notpasses. No independent security adjudication or risk acceptance claimed.

## Primary sources / bounded review

Microsoft accessed2026-10-09:
https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifiledialogevents-onfileok
https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifiledialogevents-onoverwrite
https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-fde_overwrite_response
Installed `rfd-0.16.0/src/backend/win_cid/file_dialog/com.rs` and dialog_ffi/future corroborate ABI,
STA/Show/GetResult and existing defaults; local platform functions used without dependency expansion.

Next: one fresh-context review of I3.1 lifecycle plus I3.2/I3.3, then owner synthetic checklist:
Yes singleprompt, No/change name, No/same name resubmit, folder change, Cancel before/afterNo,
close/cancelclosing/navigation/reconciliation/openfailed retry and latecollision refusal. Stage18unaccepted.
