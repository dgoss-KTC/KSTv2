# Stage 22 Reports redesign checkpoint

Owner-dispatched 2026-10-19. Entry HEAD `c76c3ffdbeca506298a11ae79ab2a3b21ac49697`.
**COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-19.**

The owner confirmed visual, functional and native-save testing, accepted this checkpoint and
explicitly authorized one complete local checkpoint commit, **not a push**. Acceptance covers all
six Reports views, typed report-specific adapters, shared save/recovery presentation, grouped columns,
Historical draft/applied behavior and 100/500/1000 paging. Part Info acceptance is limited to title/
query/identity presentation. This is owner-reported testing, separate from the agent-executed checks
below; it adds no new automated-suite, agent live-query, exhaustive-fault, deployment or risk-acceptance
claim. Historical header filters/sorting, Part Info's data region and overall Stage 22 remain open /
outside this checkpoint. Accepted source/tests are unchanged at closeout.

## Scope and plan

Reconciles S22-011 (Component MRP), 012 (Shipments-To-Go), 013 (S&OP), 014 (standalone
Open Order Report), 015 (Historical Shipments), shared export/conventions S22-003/018 and
only the title/query/identity portion of S22-037 (Part Info). No duplicate backlog IDs.

Authority: AGENTS.md, enacted SECURITY.md/security policies, current status/checklist,
accepted Stage 18/19/21 and Stage 22 export/Open Orders implementation evidence, current
Historical Shipments API contract. External design specification and all nine boards in
`C:\dev\kst_v2_tmp\Stage22\KST_Reports_Redesign\` are presentation references only.
Owner dispatch supersedes design assumptions: unchanged queries can refresh, paging is
100/500/1000 across layers, current shared anchored popover/control-on-accent are reused.

Sequence within this single checkpoint:
1. Generalize shared export/recovery with a typed preparation/save port and report adapters;
   preserve kind, host-event outcomes, exact retry, preference serialization and close protection.
2. Replace Reports shell/forms and grouped column selection; limit Part Info edits to its header.
3. Separate Historical drafts/applied operation ownership; bounded paging and guarded preference.
4. Add scoped offline behavioral/native/backend tests, regenerate contracts where affected,
   verify actual synthetic workbook contents and run compiler/lint/build checks.
5. Separate-context security review of implemented diff/evidence; correct scoped blockers.
6. Republish sidecar, rebuild embedded release host; record identity and owner checklist.

No source population/query/algorithm/calculation or locked Stage 9–11 changes. No header
filters/sorting, new dependencies/capabilities/DB objects, live agent source reads or submissions.
Existing non-Analytical departure/artifact expiry/retry rules stay host-owned. Historical
acquisition lifetime belongs to the report, not its export component or draft query.

Pre-existing modified `src/frontend/tsconfig.tsbuildinfo` is preserved separately, SHA256
`2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`.
Local evidence root: `C:\dev\kst_v2_tmp\Stage22\ReportsRedesign\evidence\`.

## Implemented decisions

- Top grouped report-selector buttons and framed title/body/footer. Workbook forms use current
  tokens/density, secondary Export, accent acquisition actions, segmented enumerations and the
  existing shared anchored popover (not a copy). Part Info changes only title/query/identity chrome;
  facts, sections, tabs/dialogs and acquisition behavior remain unchanged.
- `ExcelExportPort<Request>`/`ExcelSavePort` describe presentation requirements, not source DTOs or
  native grants. Typed WorkbookReportExport retains actual ComponentMrp/ShipmentsToGo/Sop/
  OpenOrderReport kind for save/status/open. HistoricalReportExport borrows the applied client.
  Existing Analytical consumer is unchanged in authority. All use one shared open preference.
- Format activation prepares and automatically saves; Enter submits the same guarded action.
  Four generators say they acquire fresh facts. Exact cancellation retry, Discard, observation-only
  unknown-status reconciliation and Open again remain. Report warnings persist alongside feedback;
  ordinary success retains eight-second/focus-aware dismissal. NoData has successful-empty feedback
  and a footer until input change. S&OP retains fingerprint checks and both calendar listeners.
- Shared application-lifetime feedback now receives `component-mrp-operation` events. Terminal
  outcomes cannot be replaced by late invokes/events/status replies. Shared status drives the
  Reports shell busy callback; native close/barrier enforcement remains authoritative.
- Workbook adapter owns its generation and AbortController. Cleanup and API cancellation are now
  epoch-bound `sop_discard_generation`, not an unscoped discard that could invalidate a later visit.
  No native command/capability/grant expansion. Unresolved saves remain observational after departure;
  only Analytical saved grants retain their existing departure privilege.
- Historical client ownership stays with the report, independent of export remount/retry/discard and
  draft edits. Export cleanup clears its borrowed presentation reference, not the acquisition or
  displayed pages. Native/frozen workbook bytes remain bounded by operation replacement/departure/
  expiry, as before; UI discard is not a new early file-deletion authority.
- Separate draft/applied filters and columns; Any customer initially, inactive mode text retained
  but excluded from requests. All summaries/export scope describe the applied result. Valid unchanged
  Display can refresh. Local inclusive calendar ranges only. Existing catalog IDs/order, defaults,
  physical rows/provenance/unavailable values and monetary caveats retained; unknown picker IDs fall
  into Other. Only existing ID preferences plus guarded Historical page-size preference persist.
- Spec/store/API/native/frontend accept up to 1,000 rows, with 100/500/1000 UI choices. First-row
  preserving resize reads one bounded page and retains workbook identity. Explicit JSON ceiling is
  20 MiB (five times the previous 200-row/4 MiB ceiling), not unlimited history transport. Source
  population and streaming frozen-workbook generation are unchanged. No DTO shape changes; backend
  spec generation and generated TypeScript regeneration produced no content differences.

## Verification / findings / review build

Implementation, scoped verification, separate-context security review and final release build are
complete. At implementation handoff, owner review was pending; owner acceptance is now recorded
above, not as agent-run live-query or desktop interaction evidence. Initial tests exposed
renamed-control and epoch-invalidation assertions, then Windows Vitest dynamic import mock resolution
on repeated adapter use; report adapters now use the already-admitted core invoke import consistently.
No new dependency/capability was used. Retained failed intermediate evidence is not a passing result.

Final retained evidence under the local evidence root above:

| Gate | Result / depth |
|---|---|
| `frontend-final.json` | **187 passed / 0 failed / 2 inherited skips**, 18 files. Reports + every production adapter, Historical UI/transport/preferences, Part Info, shared export/feedback/preference races, MPS/BOM and Open Orders/CustomerWorkspace regressions. Skips are existing opt-in synthetic screenshot/view writers, not failed behavior tests. |
| `backend/reports-api.trx` | **43 passed**: Historical/Workspace Shipments and report API regressions. Added 2,501-row frozen-population fixture verifies 100/500/1000 pages, tail/invalid bounds, exact repeated full workbook bytes, one source read, signed quantities, identifiers and nonformula text. Actual XLSX retained as `historical-2501-frozen.xlsx`. |
| `backend/reports-domain.trx` | **70 passed**: report/domain and synthetic workbook contents (Component MRP, STG, S&OP, Open Order, Analytical). Synthetic Component MRP workbook also retained. |
| `backend/reports-application.trx` | **9 passed**, report orchestration. |
| `backend/architecture.trx` | **10 passed**, architecture/security invariants. Backend total **132** across these distinct projects. |
| Native `cargo test --offline --lib component_mrp` | **16 passed / 2 intentional ignores** (interactive Windows chooser and association dispatch; not agent-run). Production synthetic file publication/replacement, held ownership/close, bounded retry/content verification, Historical file lifetime, kind/departure isolation and new paging-bound test included. |
| Compiler / build | `dotnet build Kst.slnx --no-restore`, contract regeneration, `npm run typecheck`, final `npm run build`, `cargo check --offline` and embedded release build all pass. Changed sidecar republished using `scripts/build-sidecar.ps1`. |
| Lint / formatting | Zero-warning changed frontend ESLint and scoped C# `dotnet format --verify-no-changes --no-restore --include ...` pass. Native `rustfmt --check` retains only pre-existing unmodified Analytical input-capacity expression formatting; new hunks are formatted. No unrelated formatting cleanup. `git diff --check` passes. |
| AFT | Scoped diagnostic requests failed with filesystem `read_deadline_exceeded`; **no final fresh/clean AFT claim**. Actual compilers/tests/builds are gates. |
| Retained warnings | Existing Vite mixed static/dynamic core import and >500 kB bundle warnings. Broader locked-stage lint/whole-solution formatting exceptions were not reopened or misrepresented as newly passing. |

Independent security review: full `independent-security-review.md` and narrow-final-delta addendum
remain local. **No confirmed new material defect/blocker at reviewed source/offline-test depth.**
Reviewed actual tracked/untracked production changes, kinds/grants/lifetimes/departure, exact retry,
unknown/terminal/late outcomes, preferences/close and bounded Historical paging. Independently ran
88 tests, then overlapping 26 adapter/status and 11 final transport tests; do not add these to the
implementer's totals. No risk acceptance, native chooser interaction or exhaustive-fault claim.
The review-time build-info hash difference was concurrent build output, reconciled by restoring the
entry backup and verifying the original hash after the final frontend build (see `ARTIFACTS.json`).

During implementation there was no new dependency/tool/capability, DB query/object/change, QXtend
submission, agent app launch, installer, deployment, staging, commit or push. The subsequent local
commit is explicitly authorized above, not a push. All source changes are this checkpoint except the
explicitly preserved pre-entry build-info modification; no generated manifest/schema/contract drift.

## Acceptance closeout / commit boundary

Closeout reuses the existing verification without repeating suites, builds, AFT or independent review.
Exact entry tracked diff and all 11 new files match the retained accepted implementation snapshots;
the accepted executable hash and pre-entry build-info hash also match. Only acceptance/closure
documentation and the existing Windows backlog records are changed during closeout. Review the
complete explicit staged paths/hunks, including new files, run staged whitespace checks, honor normal
Git hooks and verify committed contents and remaining index/worktree state. Full closeout evidence is
local under `evidence/commit-closeout/`; evidence, logs, caches, secrets and published binaries are not
commit candidates. `src/frontend/tsconfig.tsbuildinfo` remains unchanged, unstaged and excluded.

Existing S22-011–015 are closed for this accepted Reports checkpoint; S22-015 still explicitly defers
header filters/sorting. S22-003/018 record the completed Reports/shared-control portion only, not
closure of their broader workstreams. S22-037 records only accepted Part Info title/query/identity;
its data region and other screen refinements remain outside this closeout. No duplicate IDs or
unrelated closures. Stage 22 remains open. No implementation or authority change during closeout.

## Accepted review build identity

Use **`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**, not a running old/debug/dev-server
instance. Application version remains **0.1.0-alpha.21**. Host **15,305,728 bytes**, SHA256:

`81A8D79E4A6111E92CD7E0510DEBD80537CC627939824BB1F07AAA03F8B8953D`

Final `index-DDLd_c2G.js`, `index-B5rMum05.css`, `event-CJmhe6tm.js` verified embedded.
Published, Tauri source-binary and release-adjacent sidecar copies all match: **127,524,006 bytes**,
SHA256 `3001985F467375664382CB9042A5F51ADF0F7C656199002DC566E1EFFFF8196F`.
Full paths, timestamps, hashes and asset checks: local `ARTIFACTS.json`. Owner build-info remains
byte-identical to the pre-entry 8,496-byte backup/hash above, unstaged and excluded from task scope.

## Historical owner-review handoff checklist

The checklist below is retained as the implementation handoff, not a new pending approval gate or
agent-executed checklist. The owner's visual/functional/native-save confirmation and acceptance are
recorded above at the reported depth, without claiming separately observed results for every fault
scenario. Avoid invoking an external import or treating a report workbook as operational changes.

1. **Component MRP:** check SW/26/Week/Unconfirmed defaults and remembered inputs; Enter and Export
   both disclose fresh acquisition and open one protected Save As. Cancel, Save again, Discard;
   verify warning/failure feedback. Export another site/period and inspect workbook as usual.
2. **Shipments-To-Go:** check site All/None and Include Next Week; workbook and successful-empty
   outcome/footer. Change inputs to clear the empty footer.
3. **S&OP:** check ranges, remove/add bounds and Group By; fiscal calendar edit invalidates a cancelled
   workbook before retry. Confirm workbook labels/calculations remain familiar.
4. **Open Order Report:** use a qualifying filter and flexible dates; grouped Columns search, optional
   All/None and mandatory locks. Inspect workbook and no-data/invalid-input outcomes.
5. **Historical Shipments:** Display >1,000 rows with owner-authorized criteria; edit draft query and
   optional columns and confirm applied summary/table/export remain old until Display. Switch customer
   modes, try local date quick ranges, Display unchanged criteria to refresh. Resize 100→500→1000
   keeping the prior first row visible; verify no fresh acquisition and full applied workbook, not page.
   Check saved page size/columns versus unpersisted query filters; locked defaults remain locked.
6. **Part Info Lookup:** Enter/Lookup, Refresh, identity, existing section retry, table tabs, buyer
   comment dialog and inventory refresh unchanged; only header/query area visually redesigned.
7. **Common desktop/layout recovery:** light/dark and compact/comfortable, narrow/long text; column/
   info/menu focus, arrow keys/Tab/Escape/outside click and scroll positioning. Shared open preference
   across MPS/BOM/Reports. Save success hides after eight seconds; cancellation/opening failure/warnings
   and unknown outcomes remain recoverable. If an uncertain result is naturally observed, Check status
   must not replay. Close while save/open is pending must defer safely. Leaving non-Analytical reports
   must not grant new artifact-opening authority; an expired/departed host grant may correctly refuse.

This checkpoint is closed by explicit owner acceptance, not agent completion alone. Any further
development, deferred capability or broader cleanup requires a separately dispatched task. The
authorized local commit does not authorize pushing, deployment or completion of overall Stage 22.

## Separately authorized Part Info & MRP follow-up — 2026-10-21

The accepted redesign above remains historical/accepted. A new owner dispatch combines only the
Component MRP and Part Info entries, makes Part Info & MRP default and adds server-derived screen
periods/retained-acquisition export. Workbook kind/save-card authority stays ComponentMrp; other reports
and accepted popup/supplier/KSS work remain unchanged. Owner explicitly supersedes export-only
reacquire-and-warn: Refresh MRP must publish a new screen before new export; acquisition expiry alone
must preserve prepared-byte recovery. Implementation/offline verification, scoped source/security
confirmation, artifact/acceptance limits and owner checklist are maintained in
`KST_v2_S22_PART_INFO_MRP.md`, contract semantics in `docs/api/COMPONENT_MRP.md`.
This follow-up is **COMPLETE / OWNER-ACCEPTED / BOUNDED FOLLOW-UP CLOSED — 2026-10-21**.
Owner: “The new MRP screen looks great. Accepted.” Acceptance is separate from offline/artifact
evidence, not proof of the exact executable tested or every suggested desktop check. One complete
local commit with normal hooks is authorized, not a push. Broader workstreams and Stage 22 stay open.
