# S22-006 — Component Orders performance diagnostics

Date: 2026-10-19. **COMPLETE / OWNER-ACCEPTED — 2026-10-19.**
Starting HEAD: `3de483092cf0a470e80d094ffc1b5176fd993a8b`. Pre-entry
`src/frontend/tsconfig.tsbuildinfo` modification is preserved. No commit/push or live agent-run query.

## Scope and authority

Owner-authorized performance checkpoint, separate from the Component Orders UI redesign.
`AGENTS.md`, the current project status, Stage 10 closeout/source mapping §8 and Stage 22 checklist
govern this work. Historical PERF-001 found PO acquisition dominant and only modest benefit from
500→250 batching. Those observations are not a current baseline. Stage 11's accepted performance
investigation also demonstrates shared-server variability; its results do not measure Component Orders.
The normal shared QAD/MPS baseline and DBA-reviewed plan/index gate remain prerequisites to SQL tuning.

Bounded plan executed: trace existing request/cache/refresh paths; fill client phase gaps without
changing acquisition; profile representative generated data through existing test seams; retain an
optimization only if demonstrated; run scoped regressions, independent security review and sidecar
publication/startup verification; give the owner an ordinary-application timing procedure.

## Current request path and coverage

1. `CustomerWorkspace` mounts `ComponentOrdersPanel` only while selected. `useComponentOrders` awaits
   the existing Tauri base-URL resolution, then calls the typed client's GET with workspace/snapshot.
   Its request token suppresses superseded replies. There is no dedicated request queue, acquisition
   gate, hidden retry or Component Orders refresh endpoint. Concurrent misses can independently acquire;
   this checkpoint does not change that behavior. Browser/HTTP dispatch and UI rendering are outside
   backend phase timings; note visible load duration separately when needed.
2. `ComponentOrderEndpoints` validates the snapshot id and uses the application-local business date.
   The accepted request log links workspace/snapshot to `OperationId`. The final **request prepared**
   log includes response DTO mapping, but excludes subsequent JSON serialization, HTTP transfer and UI.
3. `ComponentOrdersService` reads workspace configuration (including any wait inside that provider),
   checks the loaded/current MPS generation, then looks up `(workspace, snapshot, business date)`.
   No MPS load is initiated. A successful cache hit bypasses BOM/PO/enrichment/grouping, not DTO mapping.
4. Miss: snapshot-resolved parents → current-effective complete BOM per parent, unchanged concurrency
   eight → first-occurrence case-insensitive component dedup in parent order. `BomScope` encloses all
   fan-out, `BomGateWait` separates semaphore wait per parent, `BomParent` measures its source call,
   and `BomDeduplication` measures collection work. BOM source summaries separate connection setup,
   client-observed query/materialization and depth-first traversal, with counts/outcome/last phase.
5. `QadComponentOrderReader`: unchanged normalization and sequential 250-part batches, one connection.
   `PoConnection` includes connection construction/pool acquisition/open **and the existing isolation
   setup command**. `PoAndKssBatch` records batch index/count, scoped input/output counts, query duration,
   normalization duration and total. Conventional PO and independent KSS are CTEs in **one statement**;
   neither has a separately measurable client execution time. Query duration includes network, server
   work and buffered Dapper materialization; it is **not SQL Server execution time**.
6. `Enrichment` encloses exact request-key collection, `EnrichmentSource` and `EnrichmentMapping`.
   The existing Shortages reader opens one connection, reads comment batches followed by supplier
   batches (unchanged 1,000-key limit), retaining exact keys and latest-row SQL precedence.
   `ShortagesConnection`, `CommentsBatch` and `SuppliersBatch` record elapsed/count/index/outcome.
   Batch durations include command preparation, transfer, row reads, dictionary mapping and disposal;
   no claim of isolated server time. Missing source records remain absent, not fabricated flags.
7. `Grouping` uses the unchanged Domain builder. The original post-acquisition snapshot check still
   rejects replacement generations. Only fully enriched successes cache; optional-source failure
   returns useful QAD groups with enrichment unavailable and does not cache. QAD/BOM failure remains
   unavailable, not empty; cancellation still propagates.

### Reading the timings

Existing cumulative `ElapsedMs` progress logs remain useful, but are **offsets from operation start**,
not independent phase durations. New timings use `PhaseElapsedMs`, or specifically named subdurations.
`OperationId` is the BCL Activity trace id, with spans/parent spans on phase events. No external
telemetry listener, dependency or exporter is added. Parent index identifies BOM reads without logging
part/parameter values. Error diagnostics on the changed service path no longer attach raw exception
payloads; only outcome categories are retained.

`Service`, `BomScope`, `PoSource`, `Enrichment` and request preparation are inclusive/nested. Do **not**
sum them. Parallel BOM wait/read/connection durations overlap across parents; their sum can exceed
`BomScope`. Sequential PO batch query+mapping fields are subdurations of that batch; sequential
comment/supplier batches are children of enrichment. Residual overhead includes small untimed
validation/preparation/logging/disposal. `Failed`/`Cancelled` and last phase distinguish incomplete
acquisition; null counts mean not completed, not zero. Snapshot outcomes/cache Hit/Miss are explicit.

No SQL, parameter, batch/concurrency, timeout, retry, cache-policy, UI, API DTO, export or business-rule
changes. Qualifying PO population/raw quantity, case-insensitive status exclusion, independent KSS,
null/date behavior, grouping/order, exact enrichment precedence and all security boundaries remain.

## Synthetic application investigation — not live SQL performance

Existing Application delegate seams supply generated BOM/PO/enrichment records; no network or simulated
SQL delay. Release test run on this laptop: one warmup plus seven measured acquisitions per workload,
fixture construction/expected-output serialization/comparison outside measured time. In-memory logging
capture is included; file logging, real source mapping, DTO/JSON/HTTP and UI are not. Current-thread
allocations are valid here because generated delegates complete synchronously. Workloads are bounded
small/broad stress shapes, **not evidence of actual workspace population frequencies**.

| Parents / BOM occurrences / distinct components / PO lines | Total median ms (range) | BOM scope median ms | Enrichment median ms | Grouping median ms | Median allocated bytes |
|---|---:|---:|---:|---:|---:|
| 12 / 1,200 / 500 / 2,000 | 2.110 (1.690–5.126) | 0.389 | 0.531 | 0.841 | 1,026,208 |
| 120 / 24,000 / 5,000 / 40,000 | 51.241 (39.535–92.254) | 2.941 | 18.843 | 32.232 | 14,263,840 |

Phase medians are calculated independently and need not sum to total median. This is the final
`isolated-final/application-isolated-final.trx` Release run with no concurrent build/format command.
Earlier Debug/Release measurements are retained locally, not used as a before/after control.
The generated fixtures preserve mixed-case duplicate groups, exact-case differing supplier keys,
missing due dates/master text/confirmation/suppliers, precise decimals, multiline comments and KSS.
All complete meaningful group/line outputs match the expected composed values on every acquisition and
cached revisit; existing dedicated tests cover failure, refresh races, ordering and precedence.

**Disposition: instrumentation only.** No avoidable application work with a demonstrated worthwhile
safe improvement was established. These after-only synthetic measurements do not establish production
latency, a speedup, SLA or the present dominant phase. No change was manufactured to claim optimization.

## Owner timing procedure — baseline collection

Use the ordinary refreshed desktop application during the low-load evening window. Keep the same
workspace, site, business date, MPS basis/horizon and comparable parent/component/PO population. Close
other KST windows and avoid simultaneous reports/builds/load tests. Do not cross midnight or alter
workspace membership between samples. Do not clear SQL Server caches or change logging/cache settings.

1. Restart the app once (process-local Component Orders cache clears). Select the chosen workspace
   and let its MPS Dashboard load finish. Record its visible load duration and the existing **MPS source
   batch** count/part count/row count/elapsed lines as a small shared-server timing reference.
2. Select **Component Orders**; wait for completion. Record visible wait and the request's accepted →
   prepared `OperationId`, snapshot, outcome and group count. Verify `CacheLookup=Miss`. This is an
   **application cache miss**, not a cold SQL Server cache. Do not treat failed/partial enrichment as a
   complete comparable sample.
3. Go to Dashboard and back to Component Orders without Refresh. Verify `CacheLookup=Hit`; record its
   request preparation duration. A tab remount makes another GET; no source phases should run.
4. On Dashboard, click its existing **Refresh** once and wait for success/new snapshot; record MPS
   reference timing/counts. Return to Component Orders: verify new snapshot and `Miss`; collect the
   same phase fields. This is the supported **explicit-refresh path**, not a force-refresh control on
   Component Orders. Retry reissues GET; it does not bypass a valid cache. Changing MPS display basis
   or horizon is local reprojection and is not a cache-clearing substitute.
5. Repeat the refresh → miss → cached revisit cycle only twice more, serially and paced during the
   window (three refresh-driven miss and hit samples, with the initial miss kept separately). Stop
   rather than repeatedly forcing full acquisition if the server becomes slow or failures appear.
   Report median/range per cache state, not one combined average.

Keep full logs on the laptop in `%LOCALAPPDATA%\KST\logs\kst-YYYYMMDD.log`. Only a compact comparison
is needed: timestamp/workspace/site/business date/snapshot; cache state/outcome; visible wait and
`PreparationElapsedMs`; `Service`, `Workspace`, `BomScope`/largest `BomGateWait`, `PoSource`/
`PoConnection`, `Enrichment`, `Grouping`, `ResponsePreparation`; PO batch count/input/output counts and
query/mapping durations; comment/supplier batch indices/counts/durations; MPS reference batch timings.
Use `OperationId` for new timing rows; existing cumulative progress/MPS logs can be matched by the
single serial run's timestamps and workspace. Do not paste raw records, comments, connection strings,
SQL/parameter contents or exception payloads. Unexpected row-count differences invalidate a paired
comparison. Without a trustworthy comparable pre-change run, label all results **baseline collection**.

## Remaining decision evidence

If PO query acquisition still dominates while mapping/enrichment are small, the smallest next evidence
is one representative slow batch's **DBA-reviewed actual plan plus relevant existing-index information
and execution/read/wait context**, matched to the scope/count/timing (parameter values retained locally
with the DBA, not pasted into logs/chat). That distinguishes conventional joins from independent KSS,
plan/cardinality/index issues and shared-server delay, and informs a **subsequent scope decision** on
SQL/batching/index work. No broad evidence archive or tuning is requested now. A large connection,
BOM, enrichment or DTO phase instead narrows the next investigation to that layer. Continue using
locked accepted behavior until an explicit amendment where needed.

## Verification and local evidence

Evidence directory: `C:\Dev\kst_v2_tmp\Stage22\S22-006\` (full logs/TRX remain local).
Initial focused offline solution run: **118 passed** — Domain 8, Application 22, QAD/BOM/Shortages 68,
API 10, architecture 10. Final focused run after the timing-sink correction: **120 passed** — Domain 8,
Application 24, QAD/BOM/Shortages 68, API 10, architecture 10. Final isolated Release Application rerun:
**24 passed**, including seven-sample profiles and complete-output/cache comparisons. These are
separate runs, not additive unique-test counts. No wall-clock regression assertions.
Scoped AFT diagnostic coverage reported zero errors/warnings; earlier complexity refresh was partial.
Compiler/test results are authoritative. Sidecar/security/final checks are recorded in the final
verification addendum below; owner live timings and actual desktop review remain unexecuted here.

### Final verification addendum

- Independent security-specific separate-context review found potential low-severity
  `S22-006-ISR-001`: a throwing timing sink could mask cancellation or interrupt gate/connection
  ownership. New timing emissions are failure-isolated; two passing fault-injection tests check
  12-parent gate completion, original failure/cancellation object identity and synthetic disposable
  resource handoff. Independent re-review marks **Resolved at the source/offline-evidence boundary**;
  no confirmed new material blocker remains. Synthetic handoff is not a real QAD connection test.
  See local `independent-security-review.md`; no production security clearance/risk acceptance implied.
- Changed-file `dotnet format Kst.slnx --no-restore --verify-no-changes --include ...` passes
  (`format-final.log`). Final scoped AFT: authoritative diagnostics for 13/13 files, zero errors/warnings,
  five nonblocking using hints; other health categories intentionally outside the diagnostic scope.
  Full-solution inherited formatting, locked Stage 10 frontend lint and Rust formatting exceptions
  remain exceptions; full format/lint and unchanged frontend suites were not rerun or claimed passing.
- API build/OpenAPI generation during tests/publication leaves the specification unchanged. DTOs and
  generated TypeScript are unchanged, so no contract regeneration/change is required.
- Established `scripts/build-sidecar.ps1` publication/copy passes; restore reports all projects already
  up-to-date, no dependency added/installed. Final `cargo check --offline`, `cargo build --offline` and
  `cargo build --release --offline` pass, refreshing debug/release sidecar copies without installs.
  PowerShell 5.1 labels native stderr progress as `NativeCommandError` in retained Cargo logs; actual
  Cargo exits are zero and all report Finished. These are not failed builds or suppressed errors.
- All four final sidecars (publish, Tauri binaries, target/debug, target/release) match:
  **127,450,278 bytes**, SHA256
  `704DC4E6891C9952DCE14DEA7B9434D809A49819C6DD757DEF784D370091CC8B`.
  `sidecar-identities-final.json` records paths/hashes. Application version stays `0.1.0-alpha.21`.
- Reused source-disabled startup verification passes on the final copied sidecar: matching
  handshake/process identity, only `127.0.0.1` listener, health/readiness/static Part Info spec.
  Only those three non-acquisition routes were called, with QAD disabled and a nonexistent secret
  path. No actual desktop UI, live authentication/read, installer or shared deployment is claimed.
- `git diff --check` passes for the tracked working diff. Pre-entry frontend build metadata is
  preserved; no native schema/manifest, dependency, SQL, UI/export or generated-contract content drift.
  No staging, commit or push. Local initial publication evidence is historical; final logs/identities
  supersede it for the delivered binary.
- The inherited Fluent Assertions commercial-license notice appeared in architecture-test output
  (also previously recorded by Stage 9). Owner entitlement remains a separate governance item; this
  task neither changes dependencies/use model nor establishes entitlement or accepts terms/risk.

### Owner acceptance — 2026-10-19

The owner reports: “I have checked the timings and conducted a desktop review. I accept the update.”
**S22-006 is COMPLETE / OWNER-ACCEPTED.** This owner-run evidence is separate from the agent's
offline tests, synthetic profiles and source-disabled startup checks above. No numerical live timings,
sample counts, workspace identities, controlled before/after comparison or exhaustive interaction
coverage were supplied; none is inferred. The retained timing procedure remains available for future
baseline collection. No production speedup or SQL-tuning authorization is implied.

Acceptance closes this instrumentation-only checkpoint, not Stage 22 or the broader PERF-001 tuning
gate. Representative shared QAD/MPS measurements and DBA-reviewed plan/index evidence remain required
before any subsequent SQL/batch/index tuning decision. Existing verification limits and the separate
licensing entitlement item remain unchanged. This documentation-only acceptance update reuses the
existing verification; no new tests/builds/live queries, staging, commit or push are performed.

### Local checkpoint commit authorization — 2026-10-19

After acceptance, the owner explicitly authorized the checkpoint commit. Scope is the accepted
S22-006 implementation, synthetic/regression tests, implementation record, current status and master
checklist. The pre-entry `src/frontend/tsconfig.tsbuildinfo` change is excluded and preserved unstaged;
large local evidence and published binaries remain outside Git. Earlier no-staging/no-commit statements
describe the implementation and acceptance phases, not this subsequent authorization. Existing
verification is reused because production/test code has not changed since final verification.
No push, new implementation, live query, dependency installation or shared deployment is authorized.
