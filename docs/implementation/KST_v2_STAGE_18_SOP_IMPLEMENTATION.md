# Stage 18.5 S&OP — I1 bounded implementation

Status: **COMPLETE / OWNER-ACCEPTED** — closeout `KST_v2_STAGE_18_SOP_CLOSEOUT.md`, 2026-10-12.
Accepted implementation baseline c48266c.
Owner implementation authorization supersedes D1 questions: keep Scheduling Notes, no additional
field removal; current frontend fiscal settings for averages and appended full-horizon period totals;
preserve first26 physical weekly/average subtotal membership; consume fixed existing procedure, no
reconstruction or procedure-definition prerequisite. Six sites; Python inputs/row multiplicity.

## Plan and gates
Authority: AGENTS/enacted security policy, current status/checklist, accepted CMRP/STG closeouts,
D1 packet and owner I1 amendment. Version alpha18/installer0.1.0. No live queries/legacy execution,
database grants/objects/writes, new dependencies, commits/push/fetch or Stage9–11 change.

Sequence: C# DTO/spec → OpenAPI → generated TS → backend → frontend → sidecar republish.
Focused Domain source cells/report presentation; Application fresh ordered acquisition/descriptor
validation; QAD fixed stored-procedure adapter; bounded API acquisition/preparation ownership;
Exports ClosedXML live formulas; Reports/frontend calendar; explicit Rust Sop transport/kind routes.
Reuse concrete preparation/save/open/status/close implementation without framework extraction.

Verification: mocked procedure command/schema/data-reader, repeated/failed/cancelled range reads;
descriptor ownership/coverage/limits; synthetic workbook formulas/grouping/blank/partial/53week
expectations; isolated Excel probe with no links/macros/events; report/API/frontend/native regressions,
typecheck/build/scoped format/lint; matching published hashes. Live procedure permissions/schema,
internal behavior and numerical parity remain unavailable evidence, not synthetic assurance.

## Narrow exceptions authorized by owner
Acquisition may invoke only `dbo.sp_QAD_ktmpswkm` with Domain inferred from site, Site, pl1, pl2,
hours_units0, once per ordered pair. No caller-selected target/database/SQL. Workspace MPS remains
direct SELECT. Permanent mutation remains prohibited. Procedure internals unavailable: no claim of
read-only proof or expanded account authority. Live execution/testing requires separate authorization.

Export-only fiscal descriptor carries frontend-derived week/period/quarter assignments and settings
provenance for rendering; backend validates identity/partition, never implements fiscal semantics.
No fiscal additions to MpsSourceRow/MpsBucket. Acquired rows stay immutable server-side, owner-bound,
bounded/expiring, discarded on departure. Actual calendar settings frozen; unsaved preparation invalid
on settings change. One visible Generate action performs acquire→assign→prepare.

Known pre-existing work preserved: frontend buildinfo, generated Tauri desktop/windows schemas,
untracked root Stage14 plan. Broad formatting/lockedStage10 lint/AFT gaps are exceptions, not passes.

## Implemented contract and limits
QadSopReader owns CommandType.StoredProcedure fixed target/five parameters, integrated connection,
1–60s command/100s complete acquisition timeout, cancellation, strict ordered eight fixed headers
and consecutive invariant Sunday headers (ISO date-only or M/d/yyyy/MM/dd/yyyy; two-digit years,
culture-dependent names/timestamps rejected). RawI omitted without inspecting its value; remaining
labels Monday. Header names including leading-space Falldown retained. Null/text/numeric distinct,
finite numeric values preserved at Python-compatible double boundary; no derived replacements.
Exact returned raw schema/horizon compatibility remains live-unverified; unsupported schema fails
clearly, never coerces/reorders/renames. Limits156weeks/10,000rows/500,000cells/16MiB estimated source,
1024characters per text scalar; exceeding any limit rejects entire report, never truncates horizon.
Ordered ranges concatenate without dedup; require1–5 four ASCII digit pairs, no reversal/order check.

SopService freezes rows; SopAcquisitions holds latest per owner, maximum8,30min expiry and one-use
prepare token; explicit discard and opportunistic pruning, no workspace state. API shares existing
generation semaphore2,8MiB exact-byte preparation store. Page gets week identities only; typed fiscal
descriptor unknown fields rejected, bounded provenance/periods, exact ordered coverage/partition.
No page formulas/cell refs/paths/programs. Server validates transport integrity, frontend current
calendar helpers alone compute fiscal semantics. No FY27 exception default. Settings save notification
plus storage event and save-time fingerprint invalidate unsaved bytes; active acquired token is
cleaned on error/abort/departure/finally, late host results epoch checked. Native Sop route/owner-held
acquisition and kind-owned byte preparation extend accepted concrete save/status/Open/close flow.

Workbook single S&OP sheet: A:H untouched, blank yellowI Scheduling Notes, weeksJ+, current-calendar
paired quarter averages at each boundary, first26physical weekly/average subtotals, appended
FYxx Pxx totals with partial n/Nw labels. Live SUM/ROUND(AVERAGE)/SUBTOTAL9 from final membership;
no Falldown period/subtotal, extra rounding, filter/table/protection/sheet/visible metadata/print redesign.
General numeric details, Aptos Narrow11, gray headers/Total substring rows, outlines, AutoFit and
row1 freeze intent. Period block no unapproved extra tint. Source formula-like text remains text.
Calendar provenance only workbook document Comments. Sort case-insensitive current culture,
blank empty last; no secondary part sort. Native probes established mixed-case merged groups,
whitespace no subtotal, all/trailing blank grand outline1 (OOXML), named details2/groups1.
Culture/locale complex text ordering and precise AutoFit pixels remain owner-comparison limits.

Report-specific preference addition `kst.sop.inputs.v1`: site/group/ordered up-to-five inputs;
defaultsSW/Planner/one blank pair, corruption→defaults. Existing folder/Open preference remains shared.
No new dependency/capability/database object/permission/version change. No procedure body/read-only
proof asserted; later real-source schema/permissions/numerical comparison explicitly owner-gated.

## Verification evidence (I1)
Evidence outside compact packet: `C:\dev\kst_v2_tmp\Stage 18\SOP_I1_20261012_094728_evidence`.
Mocked/source/synthetic checks distinguished from live/owner: backend scoped regression gates,
frontend Reports/calendar/API, Rust native regression; isolated Excel S&OP grouping and16formula/edit/
filter/outline/type observations. Required final gate results/sidecar identities recorded in the
compact handoff. No live query, connected legacy/main desktop or actual association dispatch.
Initial read-only builder test exposed legacy STG representative-site 'T' invalid; guard's representative
sites now SW (production unchanged). Initial API host mock was synchronous; corrected async invoke.
Initial native runner used wrong synthetic-root variable; rerun KST_MRP_TEST_ROOT succeeds. These
failures are corrected evidence, not suppressed. Broad format/lockedStage10 lint/AFT gaps retained.

Final implementation gates: backend **85 passed** (Domain37, Application8, QAD25, API5, Architecture10), separate
timestamped TRX retained; frontend **32 passed** (Reports23, SopInputs/lifecycle3, SopAPI2, fiscal3,
existing Planning Settings1). Rust20 passed/2deliberately ignored (interactive chooser/real dispatch).
Build/OpenAPI/generated TS/typecheck/frontend production build/scoped ESLint/backend format and
modified-module rustfmt passed. Scoped C#/TS AFT fresh0 diagnostics with Tier2 gaps; Rust analyzer
unavailable; later inspections interrupted, not passes. Full lint/broad formatting not rerun or claimed.
Excel native grouping probe3cases plus16/16 formula/edit observations passed, synthetic only,
macros/events/link updates disabled, zero connections/read-only disposable opened copy, no saving.
Published with scripts/build-sidecar.ps1; both copies SHA256
`44C35D4EED1D1903065049F603CADC81E77CF1A8B19CBE9C2088A3ED6808AFA7`.
Original pre-existing buildinfo restored from retained hash-identical backup, schemas/Stage14 plan
unchanged. No integrated desktop/live comparison/actual association dispatch/physical printing.
At I1 checkpoint the next step was focused review, then separately authorized owner/live verification;
see the superseding acceptance/closeout record below.

## Focused review, correction, UI follow-up, and owner acceptance
SOP_R1 separate-context review (2026-10-12) found no blocking defect and recorded Low confirmed
SOP-R1-F1: DELETE acquisition runtime204 versus OpenAPI/generated200. Corrected narrowly by declaring
Produces(204) and existing400, regenerating OpenAPI/TypeScript, and extending mocked SopEndpointTests
to assert204 plus discarded-token rejection. Focused API test1 passed; backend build0 warnings/errors;
frontend typecheck/scoped format passed; published/Tauri sidecar SHA256 matched
F5DC0309202C31908155404A2A1023D2BDA931B307081999848A24FDA97798CC. Existing running app required
restart; no installed-package/update claim. Review details/limits: packet
C:\dev\kst_v2_tmp\Stage 18\SOP_R1_20261012_103150\REVIEW.md; correction handoff
C:\dev\kst_v2_tmp\Stage 18\SOP_F1_20261012_104940\transfer\HANDOFF.md.

Owner reviewed generated S&OP workbook and reported it good, and explicitly accepted S&OP overall and
authorized closeout (2026-10-12). Owner separately reviewed/accepted the corrected Product lines input
form. This UI-only follow-up separates Site/Group By, arranges each remembered ordered range under a
Product lines section, retains 1–5 bounds/validation/request and lifecycle, adds compact four-digit
fields and narrow S&OP stacking. It does not alter backend/native/contracts, formulas or workbook.
Focused UI packet `C:\dev\kst_v2_tmp\Stage 18\SOP_UI_20261012_110744` records 33 frontend tests,
typecheck/build/scoped TS lint. Interactive visual check was not performed in the agent environment;
owner has reviewed the corrected form. The owner acceptance supplied here covers S&OP overall.

Acceptance is not exhaustive parity: procedure definition/internal read-only behavior, live schema,
permissions, numerical Python parity, physical printing, and a complete desktop/save/close matrix
remain unverified. The owner-generated workbook and corrected form are observed owner evidence, not
proof of all these cases. Synthetic Excel/mocked tests remain synthetic. Broad formatting, locked
Stage10 lint, AFT/Rust/Tier2 limitations remain exceptions, not passes; they remain limits after Stage18 acceptance.
Owner designation18.6 corresponds to historical master-checklist Open Order section18.3; no unrelated
numbering changed. The 2026-10-13 overall Stage18 disposition and owner-reported comparison completion
are recorded in `KST_v2_STAGE_18_CLOSEOUT.md`; no comparison date or result detail is inferred here.
