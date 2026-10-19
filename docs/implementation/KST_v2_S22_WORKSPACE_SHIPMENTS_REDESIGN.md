# S22-010 — Workspace Shipments redesign / listing amendment

Owner-dispatched 2026-10-19; starting HEAD `1edbebd`. **COMPLETE / OWNER-ACCEPTED — 2026-10-19.**
The owner accepted the rebuilt release application and explicitly authorized this closeout and one
local checkpoint commit; no push. Only pre-entry change: `src/frontend/tsconfig.tsbuildinfo`, preserved
unstaged and excluded from this checkpoint. The implementation phase did not authorize committing;
the subsequent acceptance/commit instruction does. No installs, agent-run live source reads or deployment.

## Plan and accepted scope

This explicitly amends Stage 19 Part 2's fixed display-only listing contract: header filters now
refine the **complete frozen acquisition**, filtered On Time counts follow the view, and Workspace
pages support 100/500/1000 (Reports stays 200 maximum). Acquisition criteria, workspace population,
source values/order/calculations and Stage 9–11 remain unchanged. Optional exact Part acquisition
restriction stays visible, separately from header filters. Apply uses edited criteria; Refresh uses
applied criteria. Column filters survive both within the visit; only page size persists.

Authority: owner dispatch; `AGENTS.md`; enacted security policies; `docs/api/WORKSPACE_SHIPMENTS.md`,
`HISTORICAL_SHIPMENTS.md`; accepted Stage 19 closeout and Part 2 evidence; backend architecture.
External `Workspace_Shipments_handoff` README/spec/six mockup sources/styles are design evidence,
not source authority or reusable production code. Owner dispatch resolves their open questions.

Sequence: Domain typed predicates/validation → Application view contracts → Infrastructure atomic
derived index and bounded disk-sort facets under existing store ownership/workers → API DTO/spec
and generation → themed shared anchored overlay + Shipments UI → synthetic regression/resource
evidence → separate-context security review/corrections → sidecar and embedded desktop rebuild.

Existing patterns reused: isolated FileWorkspaceShipmentsStore + FileHistoricalShipmentsStore
leases/cancellation/expiry/two workers/eight slots; generated contract pipeline; OrdersPopover
anchoring/focus/theme portal pattern; exact decimal/local business date frontend helpers.
No new dependency/capability/database object, QAD query change, sorting, export, virtualization,
Open Orders migration or cross-visit filter storage.

Verification targets: independent synthetic rows beyond first page, exact signed decimal/date/blank
predicates, cascading facets and >1000 distinct values, retained selected zero-count values,
literal contains, include/exclude/global-all semantics, atomic cancel/failure, versions/sequences,
wrong owner/release/expiry, Reports isolation/caps, no rereads, bounded high-cardinality disk behavior;
frontend interaction/focus/errors/stale replies; compilation/tests/contracts; source-disabled
published startup/non-acquiring probes. Actual WebView appearance and connected use remain owner-run.

## Delivered / decided

- Dedicated typed full-result view/facet routes reuse owned frozen files and the existing Workspace
  two-worker/eight-operation budget; Reports remains 200 maximum and independently owned. No integration
  SQL/source/calculation change. Server acquisition row count/summary remain immutable; view counts
  independently drive the displayed rate with the existing eligibility and one-decimal formatting.
- Pending offset index publishes atomically with version/filter/count metadata. Failure/cancel keeps
  the last completed view and acquisition; expected versions, monotonic request/cancel sequence and
  client epochs prevent stale publication. Cancellation that loses to completed publication is
  explicitly reconciled/labeled, not represented as an undo. No lifetime extension.
- Facets evaluate every eligible frozen row while excluding their own column predicate, count ordinal
  values, search literal-ignore-case across the complete population, and keep selected/exception values
  including zero counts. Global all is exclude-empty; search bulk explicitly affects **displayed**
  results only. Null/empty are blank; literal caption-looking values and whitespace remain actual values.
- Exact-decimal coefficient/scale comparison supports source extensions beyond System.Decimal without
  rounding; requests have explicit precision/length/list/byte bounds. Inclusive Perform Date filters
  and On Time choices use actual metadata; Ship Date has no header filter. See the amended API contract.
- External facet sorting has <=4096 strings / nominal 1 MiB chunks (one bounded source value may exceed
  that threshold), 16-reader merges, sixteen online-compaction levels, <=240 persistent run paths and
  conservative 242 scratch files including in-flight work. Top values/retained dictionaries are bounded.
  Scratch bytes remain linear in the acquired column; pending/current derived indexes are <=8 bytes
  per acquisition row each. There is no new whole-history disk quota/truncation or aggregate HTTP
  parser-memory admission. Do not mistake worker bounds for a global HTTP-memory limit.
- Frontend retains one page only; 100/500/1000 sizes are spec-gated, preference-validated and use one
  backend request. Resizing retains the first visible row within the new page; filters reset page one.
  Draft acquisition inputs remain separate from the visible applied retrieval scope/exact Part.
  Header filters survive reacquisition only within the visit. No Shipments export/Open Orders migration.
- Real table, bounded independent scrolling, sticky headers, themed density/striping/negative/On Time
  colors, caption/footer/states and About information replace the old stacked card. New small shared
  `components/shared/AnchoredPopover` follows the accepted OrdersPopover approach without migrating
  Open Orders. Portal remains under `.shell` outside grid clipping, clamps/flips, contains keyboard
  focus, handles Escape/outside dismissal and returns focus. Async facet search is debounced, aborted,
  query-keyed and identity/version/sequence-checked. Initial portal relocation focus loss was found by
  the keyboard fixture and corrected before final verification.
- Narrow confirmed primary-button contrast correction: existing dark foreground on light blue was
  3.66:1; `--control-on-accent` gives light-blue white text (5.17:1) and retains dark text for other
  current accent combinations (4.76–7.79:1). Used only by shared MPS primary control and new controls;
  no global text/token palette, remote-font, mockup globals or unrelated theme migration.

## Verification (offline, not business/source parity)

Local full evidence: `C:\dev\kst_v2_tmp\Stage22\WorkspaceShipments\evidence\`.

- Final scoped backend: **82 passed, zero failed/skipped** — Domain 17, QAD 28, architecture 6,
  API/store 31. `backend-final\*.trx`; zero-test Application filter is not counted as coverage.
  Earlier 79-pass run is historical, not additive. Tests include beyond-first-page matching and
  independently stated counts/boundaries, decimal precision/sign/zero, dates, literal `%`/`_`, blanks,
  AND/include/exclude, cascading/retained facets, no rereads, last-good view after cancellation/malformed
  tail/pending-write obstruction, old versions/sequences, owner isolation/release/expiry, saturation
  and Reports independence/caps, declared and unknown-length streamed request overflow, and cancellation
  after first online merge begins. The write obstruction is not an actual disk-full or access-denied
  cleanup test; failure specifically at final index replacement is also unverified. The accepted
  broader cleanup/abandoned-root limitations remain.
- Representative streamed synthetic run: **100,004 physical rows / 90,002 distinct values**; full
  search finds values beyond the display cap, global all/exclusion includes hidden values and two
  blank rows, and scratch is removed. Final recorded elapsed **5,490 ms**, working set before facets
  **144,904,192**, after **182,747,136** bytes; final owned result/index/view disk **17,299,575** bytes.
  Observational whole-test process readings, not peak memory, live timing, production throughput or a
  capacity guarantee. Independent expected formulas avoid a full-history expected-value dictionary.
- Final frontend six-file run: **139 passed / 3 inherited skips**, zero failures. `frontend-final.json`
  covers Shipments panel, header filters, transport, CustomerWorkspace, MPS and Reports Historical
  Shipments. Tests include portal/theme/focus/Escape/outside behavior, real filter draft modes, cap/global
  all labeling, literal-caption distinctions, numeric/date quick ranges, failed/late facets, lost filter
  replies, cancellation, no-match headers, scope retention, page resize and late-page rejection.
- Backend build/OpenAPI and `npm run generate:types` pass. Generated spec/types are synchronized;
  `npm run typecheck`, production frontend build, changed-scope ESLint, changed-scope C# format and
  tracked `git diff --check` pass. Initial new-test type/query-option mistakes and portal fixture failure
  were corrected; initial npm test argument forwarding failed, then the existing Vitest runner was
  invoked directly through Node without installing a tool. Initial failures are not final passes.
- AFT: individual Domain/sorter checks reported zero diagnostics; broader/TypeScript attempts were
  interrupted/partial (no authoritative TypeScript diagnostic report within budget). Not claimed clean;
  compiler, typecheck/build and tests are the gates. Known inherited full-solution whitespace/format,
  locked Stage 10 full-lint errors and Rust-format exceptions are **not** passing checks or risk-accepted
  by this checkpoint; those unchanged broad checks were not rerun. Frontend retains existing Vite
  mixed static/dynamic Tauri-import and >500 kB chunk warnings. Existing Fluent Assertions entitlement
  remains a separate owner governance item; no dependency/use-model change.
- Independent separate-context security review plus post-compaction/query/focus delta review reports
  **no confirmed new material blocker** at reviewed source/offline depth. See local
  `independent-security-review.md`; no risk acceptance, live-source/security certification or exhaustive
  disk-full/cleanup-denial/aggregate HTTP saturation assurance is implied.

## Desktop review build / artifact identity

Application remains **0.1.0-alpha.21** (installer numeric 0.1.0). Implementation-time publication included
no installer, shared deployment, agent-run live query, staging, commit or push. Established
`scripts/build-sidecar.ps1`, frontend build,
`cargo build --release --offline` and `cargo check --release --offline` pass.

**Owner launch target:** `C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe` — 15,134,720 bytes,
SHA256 `9C276A31814E6DEE25120C46F53953CE010DAD9D2C51C354EB1547D69B65BDEF`.
It embeds the rebuilt frontend (`index-oQMU9afj.js`, `index-B1eqg-k6.css`).

Published, `src/tauri/binaries` and `target/release` sidecars match — **127,519,910 bytes**, SHA256
`6CE2AAB8468E3809032130C2D640F9B2133980CDEC43C336E8BE311438F4BFFE`.
`artifact-identities.json` records full paths/hashes, including frontend identities.

**Explicit exception:** initial `cargo check --offline` (debug) failed with Windows access denied while
Tauri's existing build helper tried to replace `target/debug/Kst.Api.exe`. The owner's running debug
`kst-tauri` and sidecar were identified and left untouched, not killed. **Debug remains the old
127,450,278-byte S22-006 sidecar**, SHA256
`704DC4E6891C9952DCE14DEA7B9434D809A49819C6DD757DEF784D370091CC8B`.
Do not review through the running debug app or assume its sidecar was refreshed. Close that app
normally before launching the release target; single-instance forwarding otherwise reaches the old
window. Refreshing debug artifacts is deferred only until that lock is released.

Source-disabled startup/non-acquiring probes pass on the new copied sidecar: handshake/process
identity, `127.0.0.1`-only listener, health/readiness, fifteen-column Workspace spec with 100/500/1000,
and unchanged Reports 200 cap. Only those four non-acquiring routes were called with absent secret
file/disabled source settings. `verify-sidecar.ps1`, `sidecar-startup.json/.log`, `sidecar-publish.log`,
`cargo-release-final.log` retain evidence. No agent-executed desktop UI launch, live authentication or
source parity is claimed. Owner desktop acceptance is recorded separately below. Original build metadata
restored byte-for-byte:
`2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`.

## Owner acceptance and local commit closeout — 2026-10-19

The owner reports reviewing and accepting the rebuilt **release** application on 2026-10-19 and
explicitly authorizes closure and the complete local S22-010 checkpoint commit. **S22-010 is COMPLETE /
OWNER-ACCEPTED.** This is owner-run evidence, not agent-executed testing or proof that each item in the
earlier checklist below was individually demonstrated. No additional connected-source parity, exhaustive
fault coverage, production capacity, installer or deployment verification is inferred. The accepted
Stage 19 Part 2 acquisition/business baseline remains; its listing/filter/paging amendment is now accepted.

The commit scope is the complete reviewed Domain/Application/Infrastructure/API implementation,
shared anchored controls and narrow primary-button contrast correction, Shipments presentation/client,
tests, OpenAPI/generated TypeScript and this closeout plus API/status/master-checklist updates.
The index was empty at closeout entry and HEAD remained `1edbebdb84cbf1883251e3979d37aec577ab1de5`.
Exact paths/hunks are reviewed and staged explicitly; local evidence, logs, secrets, caches and published
executables stay outside Git. The unrelated modified `src/frontend/tsconfig.tsbuildinfo` remains unstaged
with the identity above. No source implementation change is made during this documentation/commit step.

Existing verification and independent review are reused unchanged: **82 backend passed**, **139 frontend
passed / 3 inherited MPS skips**, retained build/type/scoped lint/format/publication evidence and
no-confirmed-material-blocker security review. Raw retained TRX/frontend counters were rechecked, not
rerun. Closeout checks review the complete staged diff/path inventory and whitespace, honor applicable
Git hooks without bypass, and verify the resulting commit and remaining index/working tree. No new
tests/builds/security review, debug refresh, dependency installation, live queries, deployment or push.

No confirmed new security blocker is open at reviewed depth; documented resource limits, unverified
disk-full/final-index-replacement/cleanup-denial/aggregate HTTP saturation modes, partial AFT diagnostics and inherited broad
lint/format exceptions remain limitations, not risk acceptance. Published/binaries/release sidecars
match as recorded; **debug remains old**, not part of the accepted rebuilt release identity.
S22-005 remains reopened **for a subsequent filtering update only**; its original accepted implementation
is preserved, not migrated here. **Stage 22 remains open.**

### Historical owner-review checklist (issued before acceptance; not agent-executed)

1. Close the existing debug app normally, then run the **release path above**. Open Workspace →
   Shipments. Check light/dark, current accents, compact/comfortable and narrow-window wrapping;
   only the table scrolls, headers/footer remain reachable, and there is no export.
2. Check ninety-day default, optional exact Part, open/inclusive dates and inverted-date error. Edit
   inputs without Apply: retrieved dates/Part must stay accurate. Apply uses edits; Refresh uses the
   applied scope. Header filters survive both; departure/revisit resets filters/results.
3. Try Values/Contains, literal `%`/`_`, blank versus caption-looking actual values, signed quantities,
   zero/exact decimals, Perform Date ranges and On Time choices. Check filtered row count/rate/tag,
   unchanged retrieved total, cascading counts and clear/no-match states. If high-cardinality data is
   available, search beyond the displayed cap; global all includes hidden values, search bulk is labeled
   displayed-only. Check source values against owner-selected evidence without changing databases.
4. Exercise 100 → 500 → 1000 and First/Previous/Next/Last; former first row stays within the resized
   page. Check filter reset to page one, page error/retry and rapid refresh/workspace departure; no old
   rows or summaries should revive. Try cancelling a long filter; previous complete view stays usable,
   or a just-completed result is explicitly identified. Cancel retrieval separately and retry.
5. Tab/Shift-Tab, Enter, Escape and outside-dismiss header/About popovers: no clipping, focus remains
   visible and returns to trigger. Record appearance/interaction acceptance or concrete corrections.
   Reports Historical Shipments should retain its existing behavior/cap. Do not infer acceptance of
   future Open Orders filtering, installers or deployment.
