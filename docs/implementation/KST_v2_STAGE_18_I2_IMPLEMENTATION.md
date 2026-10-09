# Stage18-I2 — implemented, pending independent review and owner verification/acceptance

2026-10-08. Fresh Windows main/05e1109 baseline includes all uncommitted I1 work; no fetch.
alpha.18 / installer0.1.0 unchanged. No Stage18 completion/acceptance claimed.

## Owner-approved scope amendment

Continuous preventive selection/ancestor/reparse binding is no longer an implementation prerequisite.
Deliberate directory/link manipulation by another local program is a documented limitation, per the
owner's explicit I2 authorization. Retained boundaries are host selection/authority, no replacement,
exact prepared bytes, owned cleanup, truthful state, resource bounds and confirmed-save-only opening.
This is not AI risk acceptance or a universal filesystem/power-loss guarantee.

## Production flow

- Host `component_mrp_begin` advances generation and revokes one current preparation. Native Generate
  calls only its ready managed sidecar URL (no frontend URL), with a process-local random transport
  session. Backend typed response contains ID/context/expiry, NOT workbook bytes. Separate same-session
  byte endpoint returns retained bytes without QAD/calculation/workbook regeneration. Wrong ownership,
  replaced ID or expiration returns410. Backend stores cloned immutable bytes, maximum8 entries,
  8MiB each,30minutes, latest preparation per transport session; maximum2 simultaneous generations.
- Host admits one generation and one current preparation,8MiB bounded transfer, strict content type,
  exact Content-Length, no redirect,120-second transport timeout. Host receives only inputs and opaque
  IDs from main injected window. Calculation input/unmount invalidates host epoch; late response rejects.
- One save/open active.64 operation records and8 saved artifacts maximum until host exits; capacity
  error is explicit, no eviction of uncertain operations. Preparation30minutes; bytes retained by
  Arc during active attempt and artifact lifetime (session only); no restart artifact restoration.
- Host Save As uses existing native chooser, fixed XLSX filter/safe name. Suggested sequence is advisory,
  increases after successful saves, not reservation. Every collision requires another explicit chooser
  selection. No direct-write/export-helper/browser fallback or overwrite branch.
- Initial hint is configured Documents via SHGetKnownFolderPath flags0/currentuser; remembered actual
  successful parent if available, then Documents, otherwise native explicit selection. Internal fixed
  per-user JSON includes checkbox/parent; session updates precede persistence. Failure/cancel/unknown
  does not change parent. Persist failure reports separately without downgrading Saved. Load bounded4KiB.
- Initial supported mutation route: ordinary absolute local fixed NTFS, path at most240chars, no
  detected reparse ancestor, valid nonreserved `.xlsx` leaf. Current checks do not freeze namespace,
  detect synchronization or prove a folder is unsynced. Documents default does not admit its route.
- Production `component_mrp_native::publish`: exclusive random CREATE_NEW sibling, read/write/DELETE
  access/share0, no DELETE_ON_CLOSE, retained original H; write_all, exact length/byte comparison,
  sync_all then ordinary FileRenameInfo NULL-root/full-path/FALSE. Aligned x64 zero buffer; name byte
  count excludes terminator. FALSE immediately captures error and original-handle precommit disposition;
  cleanup failure is explicit, no name sweep. Success returns identity and closes without cleanup.
  Worker lost/panic completion → unknown: no host cleanup, replay or opening. No crash durability claim.
- Operation ID is registered pending before chooser. Repeated ID/status returns stored value, never
  publishes/dispatches again. Confirmed commitment is recorded before preference/open work. Frontend
  lost response offers Check save status; pending/unknown cannot replay. Terminal result reconciles.
- Artifact grants store internal path, volume/file identity and exact immutable content, no frontend
  native path. Auto open only captured checked intent after commitment; one automatic dispatch per
  save call, not status. Retry bound3, per-request dedup, serialized worker, rechecks identity AND bytes.
  Missing/changed artifact fails. Fixed `open` verb, no parameters/program/URL, ShellExecuteExW STA/
  NOASYNC/NO_UI. Open failure retains Saved and exact accepted message. Successful request proves only
  dispatch acknowledgment; validation-to-open/load race remains documented. No automatic restart open.

## Evidence and limits

External I2 handoff contains complete before/after source/diffs, actual scoped outputs and synthetic
files. Actual production publish function executed against local synthetic NTFS files: exact B,
existing A/B collision refusal, prepublication injected failure and owned cleanup, successful retry,
published survival and changed/missing-artifact rejection. Injection is not actual disk-full/power loss.
Preference codec/persistence exercised across reload and failure; UI reconciliation uses mocks; no
native chooser desktop test or full command transport test is implied. One authorized synthetic
workbook was saved through production publish and dispatch accepted through production ShellExecuteExW.
Viewer/loading confirmation is NOT established. No connected Generate/live sources/desktop startup.

No new dependency or capability; direct Windows platform externs use installed linker/platform libs.
Independent fresh-context actual-source review remains gate, not implementation-agent selfreview.
Known broad .NET/Rust formatting, lockedStage10lint and AFTgaps remain exceptions. Scoped results do
not clear them. No operational writes, QXtend, commits/pushes/fetch. Locked calculations unchanged.

## Owner verification after independent review

Open Reports; inspect defaults/order; use approved offline/mock source or separately authorize live
Generate. Verify native chooser Documents/last successful parent, Cancel retry, existing-name collision
and confirmed new selection. Toggle checkbox during attempt; inspect later intent. Verify saved/Open
failure/retry/status UI, restart hint and persistence warning. Sidecar republished before desktop use.
Do not label accepted until owner expressly accepts. Release/installer and live business validation
remain outside I2 evidence.
