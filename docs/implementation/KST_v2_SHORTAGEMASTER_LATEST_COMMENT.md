# ShortageMaster latest-comment amendment

Date: 2026-10-20
Status: **COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-20.**

## Owner acceptance and bounded closeout — 2026-10-20

Owner: “It works! I accept this implementation. Closeout authorized.” The subsequent closeout
dispatch explicitly authorizes documentation reconciliation and one complete local feature commit,
not a push. This is owner-run/reported application acceptance, not agent-run live QAD/Shortages
verification, itemized completion of every historical checklist scenario, measured live refresh
latency, deployment certification or material-risk acceptance.

Closeout entry HEAD is the Windows checkpoint below. Accepted implementation, tests and generated
contracts are unchanged during closeout; existing offline verification/build/security confirmations
are reused. Retained TRX counters confirm 29 final parser/reader + 40 Application + 27 API + 10
architecture passes; the final 29 replace the initial 28, and the zero-test Domain filter is excluded.
Retained frontend JSON confirms 111 passes, zero failures/skips. No repeated suites, builds, AFT,
independent review or publication solely for closeout. Review the complete exact staged paths/hunks,
whitespace and normal hooks, then compare the committed tree/blobs against the reviewed index.

The pre-existing build-info modification below remains preserved and excluded. Binaries, caches,
bulk logs and full evidence remain outside Git at the retained local evidence root below. The
existing Windows backlog `C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md`
records this accepted comment amendment within S22-007, not closure of its broader UI/navigation
scope or S22-037. No separate numbered comment checkpoint is evidenced or invented. Stage 22 and
unrelated checkpoints remain open. Further development requires a separate owner dispatch.

## Dispatch and checkpoint entry

Original implementation dispatch authorized a read-only comment-enrichment/display amendment, offline verification and affected
sidecar/review-desktop rebuilding. No live database reads, writes, dependencies/capabilities,
subagents, commits, pushes or deployment. Locked Stages 9–11 algorithms remain unchanged.

Actual Windows HEAD: `214a45a204d1dfbc5a0b7e9d0fcf62a9f52de78f` on `main`.
S22-008 closeout is already committed at that HEAD; no overlapping source edits were present.
Only entry modification: `src/frontend/tsconfig.tsbuildinfo`, SHA256
`39F8E34F0169E1D03E3C80A39586ABCBD450B7F1E378FD8EC9270518DB5C80C6`.
It is preserved byte-for-byte after builds. No staging or Git mutation was authorized/performed
during implementation; the subsequent bounded local closeout authorization is recorded above.

## Source gate and decisions

`KST_v2_STAGE_10_SOURCE_MAPPING.md` §5.3 documents `dbo.ShortageMaster` identity, exact Site/Component,
removed flag, `[Current Comments]` (`varchar(max)`, nullable), and the two row-ordering dates.
It does **not** establish a comment-history column. The accepted field-discovery ledger and current
reader likewise contain no history-column identifier. Indexed and direct repository text searches
for `Comment History`, `CommentHistory` and `comment_history` found no matches before this record.
The supplied history example establishes the intended content format, not the physical column name.
"History" in the old §5.3 validation means multiple ShortageMaster rows, not a documented history field.

The initial source gate was correctly paused. **Owner confirmed on 2026-10-20 that `[Comment History]`
is the correct column**, clearing the gate without a live query. SQL type/nullability were not
independently queried; the adapter accepts nullable text through the existing SqlClient facilities.

One shared `ShortageCommentParser` in the Shortages integration produces Domain `BuyerComment`
(effective Text, nullable RecordedAt, nullable Author). It recognizes a leading M/d/yyyy, h:mm,
AM/PM, single-token username header with flexible whitespace. Calendar/time values are validated.
The first following complete valid header ends the body; no sorting, dates-only splitting, historical
object collection or older-entry selection. Outer delimiter whitespace is trimmed; interior line
breaks/indentation/spacing remain. A recognized empty first body stays empty and displays
“No comment text in latest entry.” with its metadata. Missing/unrecognized history retains Current
Comments exactly, with unavailable metadata; nonempty fallback displays “Date unknown”.

`RecordedAt` is `DateTimeKind.Unspecified`; APIs serialize `2022-01-31T13:30:00` without an offset.
Despite OpenAPI's general `date-time` annotation, this field is specifically source wall-clock time,
not an RFC3339 instant. The shared frontend formatter uses its components directly, never `Date`,
UTC conversion or inferred timezone. Modification/Added dates remain row-ordering facts only.
Existing comment-text API fields remain; `commentRecordedAt` / `commentAuthor` are additive fields
on ComponentOrderLineDto, LongTermShortagePurchasingDto and PartInfoResponseDto. Part Info acquisition
time stays in its existing comment section and is separately labelled. No full history leaves backend.

## Active consumer trace

Shared acquisition boundary:
`src/backend/Kst.Integrations.Shortages/ComponentOrders/ShortagesComponentOrderEnrichmentReader.cs`.
`ReadAsync` returns structured `ComponentOrderEnrichmentResult.CommentsByComponent` through
`src/backend/Kst.Application/ComponentOrders/IComponentOrderEnrichmentReader.cs`.
It is registered directly for that interface in `Kst.Api/Program.cs`.

| Path | Backend/API propagation | Active presentation |
|---|---|---|
| Component Orders | `ComponentOrdersService.EnrichAsync` → Domain `ComponentOrderLine.CurrentComments` → `ComponentOrderEndpoints.ToDto` → `ComponentOrderLineDto` → generated API aliases in `api/componentOrdersApi.ts` | `components/ComponentOrdersPanel.tsx`: group display-row preview and `ComponentCommentDialog`. Additional PO rows carry the same field but deliberately do not repeat its preview/button. |
| Workspace Shortages / Component Information | `LongTermShortagesService.GetPurchasingAsync` → `LongTermShortagePurchasingResult.CurrentComment` + `CommentAvailable` → `LongTermShortagesEndpoints.Purchasing` → `LongTermShortagePurchasingDto` → generated alias in `api/longTermShortagesApi.ts` | `components/LongTermShortageDetailModal.tsx` exports `ShortageInformationSections`, embedded in the shortages-context `ComponentInfoModal.tsx` drawer. Buyer-comment section is independent of PO count. PO DTO mapping also carries `CurrentComments`, although this table does not display it. |
| Reports Part Info Lookup | `PartInfoService.LookupAsync` independent comment task → `PartInfoReport.Comment` → `PartInfoEndpoints.Map` → `PartInfoResponseDto.Comment` (`PartInfoSectionDto<string>`) → generated alias in `api/partInfoApi.ts` | `reports/PartInfoLookup.tsx`: Buyer comment strip/preview and `BuyerCommentDialog`. `comment.acquiredAtUtc` is a separately labelled acquisition time, not comment time. |

Direct source/property/alias searches found no additional active ShortageMaster-comment consumer.
Ordinary Component Information outside shortages context does not acquire this comment.
Invoice remarks and sales-order comments are unrelated and excluded. No export-column amendment.
Callgraph interface resolution was incomplete; the interface call sites above were verified by direct
source inspection and repository text search rather than treating its empty result as proof.

## Preserved selection, performance and presentation constraints

- `BuildCommentCommand`: parameterized exact Site/Component scope, `sm.isRemoved = 0`, one row per
  component ranked by Modification Date DESC, Added Record Date DESC, id DESC. Selection unchanged.
  The ranking CTE carries the ID; the final ID join retrieves Current Comments/history only from the
  winning rows. No additional query, source population, object or ordering rule is introduced.
- Reader distinct-exact scopes and 1,000-component batches remain; Component Orders requests unique
  components once before mapping shared enrichment onto repeated PO lines. Existing snapshot/date
  cache and optional-source failure behavior are unchanged.
- Workspace Shortages requests only the already-selected cached component; no MRP/BOM reacquisition
  or workspace-wide history acquisition. Part Info requests one component independently of POs;
  its separate supplier-only read has no component scope. No new query exists.
- History is parsed once per returned selected component at the shared adapter boundary, not once per
  PO line/UI. Matching stops at the first subsequent complete valid header. No text truncation.
- Previews/dialogs share `shared/buyerCommentPresentation.ts`; metadata uses existing secondary theme
  typography and stays outside the two-line Component Orders body clamp. Long-comment dialogs and
  existing source-unavailable/missing/focus behavior remain. Expanded PO rows still do not repeat the
  group-level preview. No hooks, cache lifetimes, analytical algorithms or export columns changed.
- Existing comment render sites use React text children (`pre`, `p`, `span`, `div`), not HTML injection.
  No raw comment/history logging was introduced. Existing reader timing records counts/durations,
  not bodies; failures expose categories, not source text. No production content was acquired.
- Security confirmation (no subagent review per dispatch): both backend regexes explicitly use runtime
  `RegexOptions.NonBacktracking`; no backreferences/lookarounds/backtracking-prone execution. Long
  synthetic timings below verify the actual parser. New content is rendered as React text, tested
  with literal HTML-shaped strings; no `innerHTML`/HTML injection or sanitization bypass.

## Verification and limitations

All verification is scoped/offline; full evidence local under
`C:\Users\dgoss\AppData\Local\Temp\opencode\shortagemaster-comment\` (test JSON/TRX, initial compiler
log, final identity JSON, entry build-info copy). Full command output is also retained in local AFT
task logs; no raw production content is present. No live reads, agent desktop launch or deployment.

| Gate | Result |
|---|---|
| Backend | **106 distinct tests pass**: 29 parser/Shortages reader (final rerun replaces initial 28), 40 Application, 27 API, 10 architecture. Domain filter matched zero tests and is not counted. |
| Frontend | **111 pass / zero skips/failures** across formatter, Component Orders, Component Information, Workspace Shortages, Part Info Lookup and CustomerWorkspace. |
| Cases | Exact supplied example; first not maximum timestamp; duplicate headers/empty newest; multiline/spacing/prose dates; missing/malformed/invalid-calendar fallback; midnight/noon; unspecified serialized clock; same preview/dialog metadata; HTML-shaped plain text; missing/unavailable states and existing interactions. |
| Reuse/batching | 2,000 repeated PO lines share one component request and one enrichment acquisition across two cached loads. Existing exact dedup/batch helpers produce 1,000/1,000/501 parameterized scopes for 2,501 unique components repeated twice, without a DB connection. Selected-component/no-PO and independent Part Info paths pass. |
| Contracts/builds | Backend solution build clean (zero warnings/errors); C# → OpenAPI → generated TypeScript synchronized; frontend typecheck/build, sidecar publish and offline embedded release Cargo build pass. Final asset filenames verified embedded; four sidecar copies match. |
| Lint/format | New backend parser/model/tests scoped format passes after required CRLF normalization. Other changed frontend lint passes; full changed scope fails solely at unchanged ComponentOrdersPanel setState-in-effect (now line 41). Wider touched C# format fails on working-copy EOL and pre-existing Stage 11 whitespace. Not represented as passes; no unrelated cleanup/rule suppression. `git diff --check` passes. |
| Diagnostics/limits | Two scoped AFT requests hit filesystem deadlines; no fresh diagnostic claim. Existing Vite mixed-import and >500kB chunk warnings persist. Existing broad verification exceptions are not newly accepted or cleared. |

Synthetic parser timing (warm runtime, Debug build, one local sample; not a benchmark guarantee):

| Shape | Characters | Repeats | Total ms | Approx. ms/parse | Allocated bytes, all repetitions |
|---|---:|---:|---:|---:|---:|
| First boundary early; 100,000 older blocks | 4,900,029 | 1,000 | 28.977 | 0.029 | 3,008,176 |
| One-million-character newest body + older history | 5,900,056 | 10 | 65.871 | 6.587 | 20,030,736 |
| Long whitespace + malformed leading history | 1,000,012 | 10 | 45.494 | 4.549 | 656 |

Existing application-only synthetic profiles also ran with shared structured comment values: after
one warmup/seven samples, 500 components × four PO lines had **6.484 ms** median total / **1.771 ms**
enrichment (total range 4.898–16.769); 5,000 components × eight PO lines had **167.275 ms** total /
**52.089 ms** enrichment (range 110.076–349.417). These cover orchestration/mapping/grouping with
repeated component use, **not parser or database work**; concurrent local verification adds noise.
They establish neither a before/after speedup nor live refresh latency. The 2,000-line cache/count
assertions above separately verify no per-PO query multiplication.

Only effective body is copied; span trimming avoids an extra long-body allocation. History transfer
is **not measured**: no live size distribution/server plan/network test was authorized. Early-boundary
parser cost is independent of the unvisited older tail, but transferring full selected histories can
still dominate for large real payloads. No evidence of actual material regression exists in this
offline pass; no speculative truncation/chunking was introduced. Owner acceptance supplies no numerical
ordinary-app refresh timing; if transfer proves material, first evaluate selected-ID, batched incremental retrieval
through the complete first-entry boundary, never a fixed newest-comment truncation or per-PO read.

Initial failed invocations are retained, not concealed as passes: solution is under `src/backend`
(root-path attempt failed); first compiler run exposed one nullable section mismatch and a legacy
synthetic dictionary fixture, corrected before final build/tests. Initial format checks included new
LF files; new-file CRLF normalization then passed. No hooks or lint rules were weakened.

## Review build

`src/tauri/target/release/kst-tauri.exe`, alpha.21, **15,306,240 bytes**, SHA256
`F109709118FD00F295B73F6440088A91C8F2570ED698FA1B9E39C63BCACB871A`.
Rebuilt using `cargo build --release --features tauri/custom-protocol --locked --offline`.
Final assets `index-DED2Bre4.js`, `event-CHyzlnRf.js`, `index-SuUpcr74.css` verified embedded.
Published/binaries/release/debug sidecars: **127,543,462 bytes**, SHA256
`16CE3AC6FB9A05CA8CAF7D8E3696431251194983EBAB322D3D02E2BD5CC9EA6B`.
Publication uses established `scripts/build-sidecar.ps1`; no dependencies changed.
Entry host hash differed from historical S22-008 status, recorded before rebuild; that is not an
amendment to accepted historical evidence. Final identity above supersedes the local entry artifact
for this amendment only. These build checks establish no installer, live acquisition, UI acceptance
or release readiness; subsequent owner application acceptance is recorded separately above.

## Historical owner checklist / next checkpoint

The implementation handoff checklist below is retained without inventing item-by-item execution.
Overall owner acceptance on 2026-10-20 closes this amendment; unchecked scenarios are not blockers
reopened by closeout and are not claimed as additional executed evidence.

- [x] Owner confirmed exact history column; shared parser/typed propagation/presentation implemented.
- [x] Scoped synthetic verification, security confirmations, generated contracts and review rebuild.
- [ ] Restart using the release host above. Owner-selected same Site/Component: compare Component
      Orders preview/dialog, Workspace Shortages component drawer and Reports Part Info preview/dialog.
      Only first history block appears; source date/time/author agree without timezone shifting.
- [ ] Where safely available, verify recognized empty newest (no older/fallback resurrection), undated
      fallback “Date unknown”, missing comment and unavailable source. Do not write source data.
- [ ] Check long/multiline text, narrow/light/dark/density presentation, keyboard dialog access,
      Escape/Close/focus behavior, separate acquisition label and ordinary refresh/cache performance.
- [x] Owner accepted the implementation on 2026-10-20 and authorized bounded closeout/local commit.
      Stage 22 overall remains open; no push authorized.
