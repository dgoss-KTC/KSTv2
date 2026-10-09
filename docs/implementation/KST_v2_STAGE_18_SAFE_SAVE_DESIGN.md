# Stage 18 Component MRP — native safe-save design proposal

**I3 current amendment:** owner authorizes per-attempt host-confirmed overwrite (D29); historical
new-file-only restrictions below are superseded for Component MRP. See I3 actual implementation
record and native diff/tests. Complete temp+verify+flush and bounded authority/cleanup/outcomes
retained; no universal filesystem/powerloss guarantee. Independent review/owner gates pending.

**Current I2 amendment (2026-10-08):** owner expressly removed continuous preventive selection/
ancestor/reparse binding as implementation prerequisite. Deliberate local-program folder/link
manipulation is a documented limitation. Historical D5.2/I1 preventive obligations below are retained
for provenance, not current gates. [I2 production record](KST_v2_STAGE_18_I2_IMPLEMENTATION.md)
supersedes their unimplemented/stub status: actual host preparation, native no-replace publication,
preferences, artifact/open and reconciliation implemented, pending independent review/owner acceptance.
No universal filesystem, sync absence, loaded-byte or power-loss guarantee claimed. Other retained
requirements remain. Ledger D24 records amendment provenance; IDs/arithmetic unchanged.

Date: 2026-10-08. Current: **18-I1 partial implementation, pending owner verification and acceptance**.
Report/workbook/UI implementation is recorded in [I1 record](KST_v2_STAGE_18_I1_IMPLEMENTATION.md).
Native report save/open is explicitly unavailable before filesystem mutation: §3.2's preventive
path-binding boundary remains unresolved. Existing host code implements only Open-boolean preference
and a fail-closed save outcome. This design is not implemented/admitted by that code.
**Owner accepted amended D2 documentation, D3 evidence review and D4 design review; this is not
implementation acceptance/admission. Initial delivery: NEW FILES ONLY; overwrite DEFERRED.**
D5/D5.1 supplied evidence establishes limited native observations, not accepted safety. Current
candidate is **PROPOSED / UNVERIFIED**: ordinary FileRenameInfo, NULL RootDirectory, fully qualified
host-resolved destination and ReplaceIfExists FALSE, retained source/ancestor protections, no CWD
dependence. Path binding must cover absolute CREATE_NEW through publication before any write.
**Not implemented or admitted.** Initial scope is Component
MRP only. Companion [source/decision ledger](KST_v2_STAGE_18_COMPONENT_MRP_SOURCE_AND_DECISION_LEDGER.md)
records baseline, owner provenance, fixtures and version boundary.

## 1. Required protection and current gap

Owner decision (18-D2): Generate acquires fresh data and freezes context; Save As cancellation or
failure retains the exact prepared workbook for retry without a source reread; input changes
invalidate preparation. **Failed save must preserve an existing destination.** Temporary artifacts
must be operation-owned and cleanup reviewed. This requirement cannot be weakened to direct write.

**Separate owner amendment provenance — 2026-10-08, subsequent documentation-only instruction:**
Reports-form **Open workbook after saving** checkbox, initially unchecked and remembered between
sessions; standard native Save As unchanged. Ledger D15–D18 record this decision independently
of D01–D14. The checkbox is a save preference, not a calculation input: no byte invalidation,
regeneration or QAD read when toggled. Only a host-confirmed committed save may trigger opening;
open failure never downgrades save success. This amendment does not accept the full save design.

**Separate owner-approved D5.2 amendment — 2026-10-08:** ledger D19 records new-file-only scope
from accepted D4/reaffirmed D5.2; D20–D23 record configured Documents on first Save As, actual
successful parent remembered per user across sessions, fallback/explicit chooser and host-only
path state. See §3.4. These requirements do not authorize implementation or accept native guarantees.

Current repository/offline evidence:

| Locator | Observed behavior / limit |
|---|---|
| `src/frontend/src/longTermShortages/saveLongTermShortagesWorkbook.ts:10–24` | Native dialog then direct `writeFile(path, bytes)`; no sibling temp, replacement or rollback. Browser download returned as saved is not proof of disk save |
| `src/frontend/src/shared/saveCsvFile.ts:5–32` | Separate saved/downloaded/cancelled outcomes; direct native write; failure thrown |
| `src/tauri/capabilities/default.json:3–8` | User-selected-file mediation; core, save dialog, write-file only; no rename/remove/temp-directory grant |
| `src/tauri/src/lib.rs:103–130,201` | Existing host commands/handler and host-known backend state; no protected-save command |
| `src/backend/Kst.Exports/PlaceholderExportService.cs:10–55`; `OpenOrdersWorkbook.cs:8–36` | Workbook serialized in memory in Exports; no host destination authority |
| `src/backend/Kst.Infrastructure/Preferences/JsonPreferencesStore.cs:73–81` | JSON temp+move pattern; not evidence of Windows report replacement/failure guarantees |

The accepted Stage 11/13/14 export helpers remain unchanged. Their acceptance is not evidence
that they meet this new Stage 18 protection. Browser download cannot provide the required native
preservation acknowledgement; proposed Component MRP protected save is desktop-only.

## 2. Available platform evidence — documentation and supplied observations

Local source root **R**:
`C:\Users\dgoss\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f`.
These cached source files were read, not installed, downloaded, compiled or executed. Presence
and transitive lock entries do **not** admit a new direct dependency or changed use/features.

| Evidence locator | What it supports / does not support |
|---|---|
| `src/tauri/Cargo.toml:28–38`, `Cargo.lock:2775,3543,4644–4671` | Existing dialog/fs stack and cached Windows bindings; no direct Windows save binding declared by KST |
| R `tauri-plugin-dialog-2.7.3/src/lib.rs:187,658–659,773–774` | Rust host can invoke existing native dialog builder; use nonblocking callback rather than block UI thread |
| R `tauri-plugin-dialog-2.7.3/src/commands.rs:222–258` | Existing JS save command obtains native selection, grants that selected path to process scopes, returns path. It is not a grant to arbitrary siblings |
| R `rfd-0.16.0/src/backend/win_cid/file_dialog/dialog_ffi.rs:29–56,79–81` | Uses Windows FileSaveDialog and SetOptions; inspection does not prove overwrite prompting for every race/destination |
| R `windows-sys-0.61.2/src/Windows/Win32/Storage/FileSystem/mod.rs:68,148,186,273,367,395,1787–1804,1917` | Declarations for CreateFileW, FlushFileBuffers, GetFileInformationByHandleEx, MoveFileExW, ReplaceFileW, SetFileInformationByHandle, FILE_RENAME_INFO and FileRenameInfoEx |
| Same file:2163–2164,3383 | Replacement/write-through flag declarations. A constant's existence does not prove that a function honors it or supplies crash durability |
| R `windows-sys-0.61.2/src/Windows/Win32/Foundation/mod.rs:4198–4200` | Named replacement error codes `ERROR_UNABLE_TO_MOVE_REPLACEMENT`, `_2`, `ERROR_UNABLE_TO_REMOVE_REPLACED`; no full recovery semantics established by declarations |

**Current mechanism proposed for bounded verification:** complete operation-owned sibling temp,
FlushFileBuffers, single **SetFileInformationByHandle / ordinary FileRenameInfo** on its original
retained handle H. RootDirectory **NULL**; FileName a **fully qualified, host-resolved** destination;
ReplaceIfExists **FALSE**, whole union/padding zero. No FileRenameInfoEx/POSIX flags, CWD-relative
publication, replacement, delete-first, copy-over, cross-volume or alternate-API fallback.

Primary Microsoft **user-mode** documentation independently accessed 2026-10-08 [W1–W3 in §10]:
FILE_RENAME_INFO documents NULL root in the common case, absolute filename, byte-counted name and
an error on existing target when FALSE. SetFileInformationByHandle documents BOOL/error and
OS/driver variation. CreateFileW has pathname creation, CREATE_NEW/no-truncation, sharing and
directory/reparse flags, but no directory-relative creation parameter. These facts support evaluating
this candidate; they do not prove race closure, universal failure atomicity or crash durability.
Do not substitute kernel FILE_RENAME_INFORMATION semantics for this user-mode structure.

Supplied external evidence, preserved rather than rerun:

| Packet / locator | Observed limit |
|---|---|
| Accepted D4 review §§3,9 | New-file design review; prior held-parent-relative recommendation and top-down chain are unverified proposals, not implementation admission. |
| `D5_20261008_a731e62c/Mechanism.cs`, `results.json`, `native-events.log:21–40` | N01 staged/readback/flushed B; held-root relative rename FALSE/error87; N02–N09 unexecuted. No disposition executed; retained temp is not published-file safety evidence. |
| `D51_20261008_b84c109e/Diagnostic.cs:105–138`, `configuration-results.json`, case logs/results | Twelve controls: eight held-root failures87 across both name/buffer rules and added list/traverse rights; four NULL-root successes. M07/M08 absolute controls retain source identity and exact B after all handles close. No ABI defect established; x64 SDK offsets0/8/16/20, fixed24. Neither buffer over-allocation nor narrow rights additions changed outcome. |

D5.1 had isolated CWD=parent even for absolute controls; no changed-CWD test proves independence
yet. Absolute-path documentation supports the proposed no-CWD contract. Controls do **not**
establish collision refusal, path/substitution protection, cleanup safety or integrated readiness.
No need to explain error87 internals before evaluating this separately proposed design; its internal
cause remains unknown. The original held-root form remains unrepaired, not universally impossible.

## 3. Proposed ownership and trust boundary

### 3.1 Prepared workbook

1. Application freezes request identity (site, part, H, mode, as-of, generation token); QAD
   acquires fresh facts; Domain calculates; Exports completes workbook bytes **B** once.
2. Proposed backend process-local prepared record contains opaque ID, immutable bytes, byte count,
   digest, frozen context and owner/session. No destination path in backend request.
3. Native host retrieves only that Component MRP prepared ID from the **host-known active loopback
   sidecar**, not a frontend-supplied URL/port/path. Endpoint must validate session/generation and
   exact report type; response has bounded content type/length. It must not rerun QAD or serialize
   a fresh workbook. Host retains immutable B for save attempts.
4. Host-owned digest/length confirms transfer identity; not proof of business correctness. Use a
   reviewed existing/platform hashing facility; direct crate/features, if required, need admission.
5. Input change cancels/revokes prior generation on backend and host; stale preparation cannot
   start a save. Frontend state alone is not the host authorization gate.
6. Records are memory-only until input change/session end/app exit or explicitly reviewed limits;
   no silent expiry that breaks exact-byte retry. Busy-operation reference keeps bytes alive.
   Byte/memory limit and exhaustion outcome require review; no truncated workbook allowed.

Here and in §§4–5, input change means a **report/calculation input** change, not the D15 checkbox.
Snapshot the checkbox into the save attempt at start. Changes while active persist for the next
attempt only; do not mutate current intent, cancel saving or invalidate B. Persistence storage,
malformed-value policy and failure feedback remain proposals (ledger O09).
Folder defaults/remembering likewise are not report inputs. Snapshot host-owned chooser hint and
checkbox intent when each attempt starts (§3.4); site changes still revoke report preparation.

The frontend sends opaque prepared/session/operation IDs and bounded UI input; **never destination,
directory, backup/temp path, arbitrary URL or shell command**. Commands validate main-window caller,
report type, session, ID existence and current generation. Native dialog is host-owned; returns a
path internally to this operation, not reusable unrestricted filesystem authority to the page.
Chosen basename may be returned for truthful feedback; full path need not be logged/exposed.

### 3.2 Destination grant

Proposed native command opens the existing dialog builder parented to the main window with fixed
XLSX filter and sanitized suggested basename. Default cannot be an arbitrary frontend absolute
path. Dialog cancellation authorizes no write. Host stores chosen destination in a single-operation
grant bound to window/session/prepared ID and filesystem validation; changing destination requires
another native selection. No general read/write/rename/delete IPC is exposed.

Host identifies selected parent/volume and ordinary XLSX leaf; reject directories, devices, ADS,
relative/drive-relative paths, unresolved/nonlocal targets, unsupported filesystems and reparse/
symlink/cloud-placeholder routes pending support review. String prefix, extension and Path.GetFullPath
are not identity/authorization. Preserve native-selected location as an operation-bound intent;
acquire/compare directory identity before any report mutation. If the chooser supplies only a path,
the selection-to-first-open identity gap remains: current lookup could already name a substituted
directory. Require a reviewed binding/host confirmation of the validated actual location, or refuse;
do not assert that opening whichever directory currently occupies that string preserves old intent.

**Top-down ancestor protection candidate, unverified:** acquire volume root then each component to
selected parent, retained non-inheritable handles, OPEN_EXISTING, BACKUP_SEMANTICS|
OPEN_REPARSE_POINT, appropriate least rights, no FILE_SHARE_DELETE. D5 used READ_ATTRIBUTES0x80,
share READ|WRITE3; D5.1 added list/traverse only to compare error87, not to approve stronger protection.
Use actual FileAttributeTagInfo attributes/tags and FileIdInfo volume+128-bit identity at every
component, not asserted ordinary-path labels; check directory type and reject any reparse route.
Keep H and protective chain through namespace return and state disarm. No elevation/ACL changes.

**Precise unresolved proof obligations before absolute CREATE_NEW and absolute rename:**

- Opening handles top-down does not establish child-to-held-parent binding while acquiring via
  absolute lookup. A pinned parent does not lock all child entries; each acquisition/identity/actual
  ancestry must be reviewed, including changes before the next child is opened.
- No-delete-sharing protects against compatible delete/rename opens, not all directory mutation.
  Microsoft says attribute/extended-attribute access is not affected by share flags [W3]. Determine
  whether a protected component can be made/changed into a reparse route under permitted rights,
  including existing handles; a one-time ordinary attribute check is insufficient. No confirmed
  exploit is asserted. Narrower shares alone are not a documented solution to attribute mutation.
- Drive-letter/DOS-device mappings, SUBST/mounted aliases and root-volume binding are outside a
  directory handle's name lock. Review stable host-resolved volume mapping, acquisition lookup and
  both absolute API paths. A volume-GUID path may reduce drive-letter dependence [W6,W7], but is
  not automatically admitted: parser/rename/dialog/open compatibility and mapping stability need
  evidence. Never silently convert user device paths into accepted authority.
- Define aliases/8.3/case behavior and actual component boundaries; compare open identities/ancestry,
  not lowercased string equality. Multiple names may identify one object and NTFS case behavior may
  vary. Reparse rejection at the final component alone does not cover earlier components.
- Establish the **same authorized parent** for temp creation and publication, same volume, and
  retained source identity; a final-path query is an observation, not a name-resolution lock.
  Postcreation/prewrite observations strengthen evidence but cannot undo a wrong-directory CREATE_NEW.

Path/alias/mapping/share protection must be established **before creation**, maintained throughout
write/flush and final absolute rename; a post-write-only check cannot be preventive protection.
If any obligation is unsupported, fail before mutation or commission §7's separately reviewed
directory-relative mechanism. Do not claim that held ancestor handles prove race closure. No-replace
collision protection is an independent property; it does not authorize writing into another parent.

Creating a sibling temp and native no-replace publication is a **new host filesystem authority surface**,
even if no broad plugin permission is added. Review this trust expansion under Security Assurance
§§2/6/7, AI Security Review and Dependency Admission. Existing `core:default`/application-command
patterns do not constitute approval. Do not grant `fs:*` or directory-wide frontend rights.

### 3.3 Saved-artifact authorization and opening — bounded proposal

**Accepted behavior (D15–D18):** checked intent requests opening the committed `.xlsx` using the
OS default associated application. Unchecked attempts never auto-open. Preparation, cancellation,
precommit failure or unresolved save commitment never opens anything. Save and open are separate
outcomes. On open failure retain Saved and show **“Workbook saved, but could not be opened”**,
with bounded **Open** retry. Launch accepted means only a successful launch request; it does not
prove Excel/another application started, displayed or loaded this workbook.

Proposed host record after successful commit: opaque `savedArtifactId`, parent save-operation ID,
host-resolved path (never a webview argument), ordinary-XLSX type, parent/volume/file identity,
committed byte count/digest, main-window identity and application-session nonce. Record existence
requires **confirmed committed** state, not merely a frontend “saved” flag. Freeze automatic-open
intent from the attempt, separate from saved-artifact metadata and current checkbox value.

- Saved record/grant survives report input changes and prepared-ID invalidation in the same
  window/application session. It does not depend on a current prepared workbook or live sidecar
  generation. Default proposal retains it until window/session close or host exit, explicit
  revocation, or detected file disappearance/change; memory limits need review without silently
  redirecting retry to another artifact. A subsequent save to the same destination invalidates the
  old artifact identity. No grant persistence or automatic opening after restart.
- Webview Open retry supplies only bound artifact ID + idempotent retry-request ID. Host validates
  caller/window/session, successful save record, retry-eligible open outcome and absence of another
  open attempt. No arbitrary path, URL, program, verb, command line or shell IPC. One user-triggered
  launch attempt per accepted retry; repeated delivery of that retry ID returns its status.
- Before automatic open and retry, revalidate supported parent/file identity and committed bytes.
  If missing, replaced, edited, renamed or no longer safely identifiable, refuse/revoke the grant;
  explain Open unavailable while preserving successful save history. Do not recreate/resave the
  file or acquire fresh source. No implicit consent to a changed artifact at the same path.
- File validation and associated-app opening may use path-based OS interfaces; external mutation
  between validation and application load is an **unverified race**. Holding handles may mitigate
  it but can prevent the associated application from reading. Required guarantees, sharing flags
  and fail-closed support rules need platform/security evidence; do not claim loaded-byte identity.

**Automatic launch deduplication proposal:** host operation transitions under a lock from
`NotRequested` to `Claimed` once after commitment when frozen intent is true, then to
`LaunchRequestAccepted`, `OpenFailed` or `OpenOutcomeUnknown`. Claim before dispatch; at most one
automatic dispatch per save operation. Completion events and read-only outcome polling only
observe this state, never launch. Lost frontend response returns recorded status, not another
dispatch. Uncertain launch cannot be auto-replayed; explicit bounded retry may be offered after
the original attempt is terminal, warning that an earlier launch could already have occurred.
No exactly-once application-load guarantee or replay after host restart is asserted.

Open retry uses the saved on-disk artifact only: no save/replace, workbook serialization, QAD
read or prepared reconstruction. Checkbox toggles do not open a prior artifact retrospectively.
Successful save with cleanup warning remains eligible only after save worker reaches its terminal
state; opening does not change cleanup ownership or delete files. Opening failure cannot trigger
rollback. Proposed outcomes: Saved + NotRequested / LaunchRequestAccepted / OpenFailed /
OpenOutcomeUnknown; a missing/changed artifact gives unavailable retry, never Failed save.

**Mechanism for future review:** host-only Windows **ShellExecuteExW**, fixed `open` verb, the
validated saved local `.xlsx` path as file argument, no frontend program/parameters/URL and no
`cmd`, PowerShell or process-command fallback. This names a platform candidate, not an admitted
interop implementation or verified safe API use. OS file association governs the application;
do not hardcode Excel, elevate or bypass OS prompts.

New offline evidence under local source root R (§2):

| Locator | Evidence limit |
|---|---|
| R `windows-sys-0.61.2/src/Windows/Win32/UI/Shell/mod.rs:756,5674–5751` | ShellExecuteExW/structure declarations exist; no proof of launch/error/race guarantees or binding admission |
| R `tauri-plugin-shell-2.3.5/src/open.rs:105–138` | General path/URL opening; deprecated entry; Rust no-scope branch dispatches detached opening without scope validation. Does not supply saved-artifact authorization or workbook-load acknowledgement |
| `src/tauri/capabilities/default.json:3–8` | No shell-plugin permission to webview; preserve this boundary |

Authoritative Windows association/verb/error/launch-acknowledgement semantics and local-file
handling remain unverified. Review platform interop, new host command trust boundary, process
launch, saved-grant bounds and races under security/admission policies before implementation.
No shell scope broadening, opener dependency, capability or native command is added here.

### 3.4 Configured Documents, remembered parent and persistence — bounded proposal

**Accepted behavior: ledger D20–D23; mechanism below PROPOSED.** First attempt uses Windows current
user's configured Documents as a chooser starting hint. After confirmed commit, remember the actual
committed parent per user across sessions; next attempt recomputes a fresh filename from frozen
report date/context and sequence. Every save still chooses/validates its location. Cancel/failure/
unknown leaves last-successful state unchanged. Open failure never rolls it back. Unavailable last
folder → configured Documents; unavailable Documents → chooser with explicit selection, no autosave,
automatic report-directory creation or silent relocation. An unsupported location selected in the
chooser is refused with an opportunity to select a supported one, not silently replaced.

**Windows resolution proposal [W4,W5]:** host calls SHGetKnownFolderPath(FOLDERID_Documents,
KF_FLAG_DEFAULT=0, hToken=NULL), current user, not Default User/Public/DocumentsLibrary/
AppDataDocuments and not `%USERPROFILE%` concatenation. Do not set DEFAULT_PATH (would bypass
current redirection), CREATE/INIT (would mutate), or DONT_VERIFY (would bypass existence check).
Check HRESULT; initialize returned pointer and free returned allocation with CoTaskMemFree whether
call succeeds or fails; validate bounded Unicode path and accessible directory before using hint.
Default verification is not writable/support/identity proof. Run potentially slow redirected/network
resolution off UI thread with truthful pending/error handling; timeout cannot turn an in-flight native
call into a safe write grant. Documents may be redirected, network-backed or synced; starting there
does not admit it into the fixed-NTFS save envelope. Do not query/create report files to test a hint.

Existing host dialog source `tauri-plugin-dialog-2.7.3/src/lib.rs:452–456` has Rust `set_directory`;
set_file_name/set_parent and nonblocking save callback are existing facilities. Availability is not
admission/verification of this usage. Documents/LocalAppData resolution may require new Windows
interop direct crate/features or platform declarations: review exact approach, authority, ABI and
changed dependency/licensing use before implementation. No package/path/store plugin is added.

**Concrete recommended storage:** host-owned fixed filename
`%LOCALAPPDATA%\KST\config\component-mrp-save-preferences.v1.json`, resolving current-user
FOLDERID_LocalAppData internally, then literal KST/config/name. This follows existing
`LocalAppDataPaths.cs:11–24`; do not substitute Tauri identifier-specific roaming app-data or let
the page/environment argument choose a storage root. Reuse existing Rust std::fs and direct
serde/serde_json (`Cargo.toml:32–33`) rather than add JS store/path APIs. Proposed strict schema:
`schemaVersion` integer exactly 1, `lastSuccessfulParent` null or host-recorded absolute directory
string, `openAfterSave` strict boolean; reject duplicates, unknown fields, wrong types/versions,
embedded NUL, malformed/over-bound data. Missing document defaults null/false. Missing/invalid
boolean never enables opening. Validate last parent on every attempt; stored path is untrusted hint,
never authority. No artifacts, grants, B, source values or secrets persist here.

Choose and review finite JSON byte/path/depth limits before production; bounded reading checks length
before allocation and validates streamed count. D4 numerical suggestions are not accepted limits or
business rules. Restrict schema to these fields, no arbitrary KST file-write/read/delete command.
Only host-confirmed commit sets lastSuccessfulParent from committed grant; page may submit a strict
boolean checkbox update through an actual-caller/window/session-checked narrow command, never path.
Prefer a fixed typed preference view to frontend (boolean/persistence status, no directory path).

**Access/ownership proposal:** resolve/validate per-user root and fixed KST/config chain before
read/write; reject reparse/redirected unexpected storage routes and nonordinary preference file,
verify actual directory/file ownership/access with current user, ordinary inherited per-user ACLs,
no broader grants/elevation/attribute changes. Do not blindly follow a malicious fixed-file symlink
or temp name; operation-owned CREATE_NEW temp and held identity/chain, restrictive sharing and
bounded handle-based access need concrete review under §3.2. Create only fixed app-config directories
when validated/user-owned; this permission is distinct from creating report directories. Do not
assume generic std::fs calls supply race-safe object binding; if storage cannot be safely established,
keep session preferences and report cannot remember. No overwrite of another owned preference file.

**Load/write/error/session policy proposal:** malformed/unknown-version files retained unchanged on
load (no automatic corruption rename/sweep); ignore unsafe hint and default false where boolean is
unusable, issue bounded nonblocking reset/unavailable notice without private paths. Explicit later
preference update may replace only the validated fixed preference object under reviewed storage
authority. Serialize updates under host mutex; assign monotonic session revision at logical update,
one writer, merge current checkbox and last successful parent so delayed completion cannot roll back
newer fields. Save operation is already one-flight; deduplicate confirmed commit update by operation
ID. Read once on startup; don't let cross-process/disk changes alter an active attempt. Existing
single-instance convention is evidence of intent, not a filesystem lock: if another process/storage
writer conflicts, refuse/defer persistence and preserve session behavior, no overwrite-by-stale-snapshot.

Update in-memory actual parent immediately after confirmed commitment, before optional opening;
persist asynchronously with honest status. Persistence failure leaves Saved + session hint/checkbox
and a notice **“Workbook saved, but save preferences could not be remembered”** (proposal). Preference
errors before saving say preferences cannot be remembered, not Failed save. Lost completion can
query saved record/read-only status; it must not reapply an older folder revision or launch again.
Unknown commit stores no last-successful update. No restart open grants or automatic opening.

Preference temp+flush+std::fs rename/replace strategy needs its own Windows fixed-file concurrency/
failure review; no delete-first fallback, no claim of atomicity from standard-library rename and no
use of preference persistence as proof of report atomicity. Session behavior is the failure fallback.
The existing backend `JsonPreferencesStore.cs:32–81` is comparison evidence: it would send a native
path to sidecar/API and couple host chooser state to backend preferences/contracts, unnecessarily.
Frontend localStorage for this path violates D23's boundary. Existing localStorage site/checkbox
proposals O06/O09 remain historical alternatives; current recommendation puts checkbox with host
save preferences, site separate. All storage/command/native authority still requires review even
though std::fs/serde_json introduces no new dependency by itself.

## 4. Proposed operation phases and outcomes

One active save operation per main window, additionally serialized by destination identity;
duplicate operation IDs return current outcome rather than start another write. Busy/new Save As
requests are rejected or joined explicitly. Each attempt uses fresh host-generated IDs/temp name.

| Phase | Actions | Destination state / outcome |
|---|---|---|
| Prepared | Immutable B and frozen identity validated | Prepared, not saved |
| Choosing | Host native dialog; no write | Cancelled if dismissed; B retained |
| Validating chosen new name / path binding | Ordinary XLSX leaf; supported actual parent/grant/ancestry bound before any mutation (§3.2). Existing name offers Choose another / Next sequence / Cancel; changed destination explicitly confirmed | No overwrite authority even if native dialog prompts. Cancel retains B; default folder is only hint |
| Staging | Only after preventive path-binding gate: absolute CREATE_NEW exclusive unpredictable sibling `.kst-component-mrp-{operation}.tmp`; retain original H and parent/ancestor identities; no DELETE_ON_CLOSE; write B completely | Existing destination untouched; known precommit failure/cancel cleans original owned H only; B retained |
| Completing | Verify actual byte count and exact-byte/digest check via owned handle; FlushFileBuffers; handle closing/sharing protocol reviewed for rename | Failure before commit leaves target untouched. No Saved outcome |
| Ready to commit | Retained preventive path/mapping/ancestry protections and owned H; current generation, chosen destination and cancellation; lock operation state | Last-second metadata/existence checks alone insufficient. New target appearance stays collision, no replacement |
| Committing | Noncancellable ordinary FileRenameInfo on H, RootDirectory NULL, fully qualified host-resolved final path, ReplaceIfExists FALSE; same bound parent/volume; no CWD dependency | Only success establishes confirmed namespace commitment. Collision must preserve competing bytes/name; path protection unverified |
| Committed | Record terminal outcome BEFORE optional housekeeping; return operation ID and actual filename | Saved means B successfully committed to selected destination; not crash-durable or QAD-applied |
| Cleanup / remember folder | Permanently disarm deletion on success/unknown; release handles. Only known precommit terminal state may dispose owned H. Confirmed commit updates actual-parent session hint/persistence §3.4 | Saved remains Saved on cleanup/preference warning. Unknown never updates folder or deletes/replays |
| Optional opening | Once committed/terminal save is confirmed, mint/revalidate saved grant and perform the single claimed automatic launch if attempt intent was checked (§3.3) | Separate open outcome; open failure leaves Saved, with bounded Open retry |

**Do not label a successful commit followed by a lost response/postcommit check as failed save.**
Maintain process-local operation outcome and query it by ID; UI can show confirming outcome.
Unknown outcome (host crash/transport interruption) must be stated as unknown, not a false promise
that the destination is unchanged. Before retry, reconcile operation outcome; never blindly
repeat publication. Restart recovery of unknown outcomes is separately unresolved.

Success meanings are different:

- A full temporary write proves only staged bytes, not published output.
- A successful flush is not by itself an atomic namespace swap or power-loss guarantee.
- A successful rename is the proposed commit acknowledgement, not equivalent to direct write,
  transaction, application-level validation or crash durability.
- Postcommit user/external changes do not retroactively invalidate the recorded commit.

## 5. Cancellation, concurrency and race boundaries

Proposed phase progress: Acquiring → Calculating → Preparing → Prepared → Choose destination →
Preparing safe save → Saving → Saved/Cancelled/Failed. No fabricated percentage; UI labels and
whether a modal can be dismissed are review decisions.

- During acquisition: cancellation propagates; partial data cannot prepare B; obsolete completion
  ignored by application/host generation token.
- Before commit: Cancel revokes attempt, joins worker completion and removes only its temp. Native
  dialog cannot always be programmatically dismissed; if Cancel arrives while open, ignore later
  selection and close the attempt without saving.
- Commit boundary: under the host state lock check cancellation/current generation once, then enter
  noncancellable Committing. UI disables report-input changes/Cancel briefly; host does not pretend rollback.
  Checkbox changes, if enabled, still affect only subsequent attempts (§3.1).
  Native close holds the process until terminal commit/cleanup outcome, without changing existing
  accepted proposal-discard behavior. A separately reviewed integration is needed.
- An input-change request before boundary invalidates; after boundary it waits until terminal
  outcome, then clears prepared state. No late response may publish an old preparation as current.
- Backend/host restarts cannot reuse IDs or grants; original B retry lifetime is process/session
  limited. Prepared bytes/operation/artifact grant restart persistence is not accepted scope;
  remembered save-folder and checkbox preferences are separately accepted in D15/D21.
- Concurrent external changes: maintain §3.2's reviewed root/ancestor/parent path binding before
  CREATE_NEW through absolute publication; no-delete-sharing alone is not complete mutation control.
  Parent pins do not prevent independent child entries appearing. Rechecking metadata is not a
  compare-and-swap; unsupported protection stops before mutation, not after writing elsewhere.
- An absent target is committed without replacement; a competing file's appearance causes failure,
  never automatic escalation to replace. Existing targets are refused; changed destination needs
  fresh confirmed choice. Consent is not transferable to another file or parent.

## 6. Failure preservation and cleanup matrix

| Case | Required/proposed behavior | Evidence status |
|---|---|---|
| Destination locked by Excel/another process | Abort without forcing POSIX replacement; B retained; retry after user resolves lock | Unverified Windows checks required |
| Permission denial / read-only / unsupported parent | Fail before commit; never change attributes/ACLs or elevate | Proposed; synthetic + owner-run checks |
| Temp collision | CREATE_NEW fails; never truncate existing artifact; bounded fresh-name retry | Proposed |
| Partial write/disk full/flush/verification failure | Target untouched; cleanup own temp; report Failed and retain B | Proposed, failure injection needed |
| New-name collision / failed publication | No replacement/delete/copy fallback; competing object remains at original path with original bytes; exact B retained | **Unverified current new-file gate**; D5.1 has no collision evidence |
| Cleanup failure before commit | Failed/Cancelled plus cleanup warning; B retained; owned temp identity tracked, no broad sweep | Proposed; permission/handle failure checks |
| Cleanup failure after commit | Saved with cleanup warning; never rollback/delete committed destination | Proposed |
| Parent/ancestor/mapping changes during staging | Prevent wrong-directory creation/write/publication or fail before mutation; appearance of leaf handled by FALSE | Proposed; race closure still needs evidence |
| Response lost after commit | Query operation record; show outcome unknown until resolved | Proposed; crash recovery not guaranteed |
| Process/power failure | Possible abandoned owned temp or uncertain committed outcome; no durability claim | Unverified; not represented as ordinary precommit failure |

Cleanup ownership: store temp name, directory identity and file identity/original handle **H** when
exclusive creation succeeds. Explicitly **NO FILE_FLAG_DELETE_ON_CLOSE** [W3,W8]: rename does not
remove that deletion hazard. Under the same commit/cancel lock, only a terminal **known precommit**
Failed/Cancelled state with verified owned H may issue FileDispositionInfo on **that original H**
then close; no pathname reopen/delete. Committing cannot enter cleanup. Confirmed successful or
unknown publication permanently disarms deletion before handle release, including RAII/error guards.
Close-only on unknown; preserve uncertain artifacts. D5.1 close survival without disposition does
not test these guards. Never delete destination
on a failure path, scan all `.tmp` names, remove another operation's artifacts, or trust a filename
prefix as ownership. If identity cannot be verified, leave artifact and surface cleanup failure.
Retry cleanup only for this recorded object. Cross-restart temp cleanup needs a separately reviewed
ownership/recovery scheme; no executable cleanup service or journal is authorized here.

## 7. Initial destination support proposal and limits

Initial candidate support: ordinary files in a local, fixed **NTFS** volume/directory, with the
owned temp in the same directory and confirmed no reparse/cloud redirection in the supported path.
This is a proposed support envelope, not acceptance or proven sufficiency.

| Destination | Proposed disposition / evidence needed |
|---|---|
| Ordinary local fixed NTFS | Candidate only after precise creation/publication path binding, collision, owned cleanup, permission/fault/race evidence; D5.1 success controls insufficient |
| UNC/SMB, mapped network drives | Unsupported initially; redirector/server rename, disconnect and durability semantics require separate evidence |
| Cloud/synced folders, placeholders, reparse points | Unsupported initially; local cache/provider behavior not covered by plain NTFS claim |
| Removable FAT/exFAT/ReFS/other filesystems | Unsupported initially; separate mechanism/support decision and evidence |
| Devices, ADS, directories, symlinks/junction-routed targets | Reject; no general filesystem API |

Do not silently fall back to direct writing. User may choose another supported location. Configured
Documents/remembered parent can be redirected/synced; a chooser hint is not destination admission.
Absence of reparse/cloud tags is not proof of no sync provider; ordinary local files may be synced.
Eventual detection/user-assurance/support policy remains unresolved, separate from fixed synthetic
NTFS verification. No existing-file metadata merging is initial scope because replacement is deferred.

**Main alternative for initial new-file path binding:** separately review a documented user-mode
directory-relative mechanism covering **both creation and publication** against the authorized
held parent, including initial parent acquisition/ancestry/alias/reparse binding. D5's tested held-root
FileRenameInfo form failed and D5.1 established no repair. This is a design investigation, not a named
new API admission, execution instruction, kernel/NT substitution or runtime fallback. Relative
publication alone cannot fix absolute temp creation. If current candidate cannot close obligations,
stop it and obtain a separately reviewed design/authorization.

**Historical overwrite alternative, retained/deferred:** ReplaceFileW backup/recovery and metadata
merging may merit future overwrite review. Microsoft documents partial failures including loss of
original pathname (1176/1177); backup survival is not unchanged destination and recovery can fail.
MoveFileExW flags are no universal transaction proof. No ReplaceFileW/overwrite experiment or
recovery implementation is required for initial delivery. Existing destinations are never sacrificed.

## 8. Review/admission and later validation

Required decisions before implementation:

1. Approve/revise host-owned ID/dialog/destination-grant contract and sidecar byte transfer.
2. Establish exact NULL-root/fully qualified creation/publication path protection **before creation**,
   supported mapping/alias/share/access/reparse behavior, collision preservation and owned cleanup.
   Document support and unknown/crash limits; no absolute power-loss guarantee established.
3. Review new native commands/filesystem authority, window-close coordination, bounds and cleanup.
   Independent security review is warranted by trust/filesystem changes; routine agent self-review
   is not equivalent. No new capability, dependency or feature is admitted by D2/D5.2 documentation.
4. Select Win32 interop approach: existing/platform binding or separately admitted direct crate and
   features. Transitive `windows-sys` availability is not permission to add it directly. Licensing
   and commercial-use review applies if dependency use changes.
5. Approve filename/new-name collision confirmation/progress/cancel UX, memory limits, supported locations and
   error/cleanup messaging. Retry must preserve exact B.
6. Review §3.3 associated-application mechanism, saved-artifact grant identity/lifetime/race limits,
   launch deduplication and separate open outcomes. Checkbox persistence accepted; storage and
   native mechanism/security/dependency admission remain undecided. No existing shell plugin
   permission or host no-scope path is accepted as sufficient authorization.
7. Review §3.4 host fixed-name preference storage, strict schema/bounds/current-user path validation,
   serialized updates/failure behavior and configured Documents interop. Behavior D20–D23 accepted;
   new authority/commands/direct features are not admitted. Production numerical limits undecided.

Proposed later **synthetic/mocked** checks (not executed): unknown/replayed/wrong-window IDs;
arbitrary path/URL rejection; byte-count/digest mismatch; session/input invalidation; concurrent
requests; late dialog/completion; cancellation at each precommit phase; postcommit response loss;
write/flush/rename/cleanup injected errors; ownership substitution; no target delete/copy fallback;
retry exactly B with zero new source reads; saved+cleanup warning stays saved.

Opening lifecycle fixtures **S08–S15 in the ledger** are also mocked/proposed, not executed:
unchecked/checked save, checked cancel/failure/unresolved outcome without opening, saved+open
failure with exact message, Open retry independent of prepared invalidation with zero saves/QAD
reads, lost-response polling/repeated completion without duplicate automatic launch, preference
changes preserving bytes and per-attempt intent, missing/changed file and wrong-session/replayed
requests. Later scoped Windows checks should distinguish launch-request acknowledgement from
owner-observed workbook loading, exercise absent association/open errors and artifact changes,
and verify no restart automatic launch. No application is launched in this amendment.

**RESUME N01–N09, proposed only, no execution authorization:** preserve original D5/D5.1 folders;
new unique bounded fixed-NTFS synthetic folder, independently reviewed revised source/protocol and
purpose-bound existing platform tools/compiler artifacts required before any execution. Maintain
same no-replace-only scope. Detailed resume matrix in external D5.2 packet §7:

- N01: exact NULL-root fully qualified buffer, no CWD dependence, preventive mapping/chain protection,
  B/identity/old-temp absence and published survival after all closes; repeat across reviewed name form.
- N02/N03: true existing-name/case/alias collision and competitor immediately before commit; unchanged
  competitor bytes AND path, no automatic sequence publication; confirmed changed destination uses B.
- N04/N05: locked/denied target refusal, temp CREATE_NEW collision, exact original-H owned cleanup,
  sentinel/other-operation preservation, cleanup failure leaves warning/artifact.
- N06: controlled pauses during chain acquisition, before CREATE_NEW, after creation/before first
  write, during staging and before absolute commit; parent AND higher-ancestor mutation/substitution,
  mapping/alias and reparse mutation where separately permitted. No post-write-only validation. Wrong
  directory creation/write is a stop even if later detected. Unexecuted variants remain gates.
- N07/N08/N09: injected partial write/flush/cancel, commit/cleanup serialization and permanently
  disarmed successful/unknown state; lost-response status read-only, no replay/deletion/false failure.

No native test is run here. Later S16–S23 folder/schema/session fixtures and S08–S15 opening fixtures
need separate mocked/integrated and owner-desktop evidence. Opening/App launch remains separately
authorized; preserve D4 STA/NOASYNC, fixed open/NULL parameters, no duplicate dispatch and truthful
path-validation-to-load race limits. Broad .NET/Rust format failures, two locked Stage 10 lint errors,
prior frontend timeouts and AFT gaps remain exceptions, not passes.

Initial scope is Component MRP. Potential reuse is the reviewed native operation/outcome contract;
compare the next real report before extraction. Do not replace accepted Stage 11/13/14 helpers,
design other reports or mark Stage 21 complete.

## 9. Handoff boundary

Only the two named repository Markdown documents are amended; unique external D5.2 packet,
baseline copies, revised exports and offline diff/preservation evidence accompany them. No implementation, tests, filesystem experiments,
live reads, builds, diagnostics, generation, installs, capabilities, locks or version files change.
Current version stays **0.1.0-alpha.15**, installer0.1.0. Alpha.18 is proposed only at a separately
authorized write/implementation checkpoint. Future API work follows C# DTO/OpenAPI → regenerated
TypeScript → backend → frontend → sidecar republish before desktop testing.

Next bounded checkpoint: owner disposition of fresh D5.2 design assessment, then separate source/
execution authorization for the revised bounded protocol only if prerequisites are met. Current
new-file creation/publication path binding, collision preservation and cleanup remain unverified
integration gates. Overwrite is deferred, not initial scope. Agent completion is not owner acceptance;
documentation/review acceptance does not authorize implementation or accept risk.

## 10. D5.2 current primary-source register

Independently accessed **2026-10-08**. User-mode semantics, not kernel rules. Other D2 cached-source
locators remain historical declaration evidence; D4 opening/source review retained as supplied evidence.

| ID | Microsoft URL / relevant documented fact |
|---|---|
| W1 | https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info — NULL/common root, absolute name, FALSE collision, byte length. Generated syntax is not ABI authority; supplied SDK evidence has conditional union/legacy branch, not both. |
| W2 | https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle — FileRenameInfo class, appropriate rights, BOOL/error, OS/driver variation. |
| W3 | https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew — pathname CREATE_NEW, sharing exceptions for attribute access, no-delete share, noninheritance, directory/reparse/delete-on-close flags. |
| W4 | https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath — current user NULL token, HRESULT and CoTaskMemFree on either result. |
| W5 | https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ne-shlobj_core-known_folder_flag ; https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid — flags0/current redirected folder vs DEFAULT_PATH; Documents/LocalAppData are per-user IDs. |
| W6 | https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-volume — mutable drive assignments, volume GUID aliases and mounted folders. |
| W7 | https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew — DOS/GUID/NT/opened/normalized paths; fully resolved path is observational, not a lock. |
| W8 | https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info — DeleteFile does not affect DELETE_ON_CLOSE. |
