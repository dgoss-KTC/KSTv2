You are working inside the KST v2 repository in VS Code.

# Stage 8D.2 — BOM QAD Adapter / Normalization
## Plan Pass — Explore → Plan → Human Review

Stage 8D.1 — Shared Inventory Capability is complete and accepted.

This checkpoint is narrowly scoped to the QAD-side representation of the
current-effective multi-level BOM.

This is a PLANNING-ONLY pass.

Do not modify production code./
Do not modify tests.
Do not modify API/OpenAPI/generated TypeScript.
Do not modify frontend code.
Do not update project documentation.
Do not commit or push.

Stop after producing the implementation plan for human review.

---

# 1. Goal

Plan the smallest safe QAD integration capability that can read and normalize:

- a complete current-effective multi-level BOM
- preserving every legitimate structural occurrence
- preserving actual hierarchy depth
- preserving proven traversal order
- carrying the fields Stage 8 later needs
- without inventory enrichment yet
- without API/application/frontend work yet

The output of this checkpoint is structural BOM data only.

Inventory enrichment occurs later using the shared capability completed in 8D.1.

---

# 2. Repository inspection

Read only the repository files needed for this checkpoint.

At minimum inspect:

- `AGENTS.md`
- backend dependency/architecture instructions
- accepted 8D.1 implementation
- existing QAD readers and query-builder/test conventions
- QAD site→domain resolution
- connection/timeout/cancellation patterns
- SQL Server 2016-compatible patterns already used in the repository
- existing Domain feature/model conventions
- architecture tests constraining placement

If the Stage 8D.0 prompt/plan is present, use it as context but do not repeat the
repository-wide audit.

Do not inspect frontend details unless needed to confirm that no frontend work belongs here.

---

# 3. Authoritative legacy BOM evidence

The project owner supplied and reviewed the legacy stored procedure:

`dbo.sp_QAD_ktbmpsrp`

Its source behavior is authoritative evidence for the BOM traversal semantics.

Relevant parameters include:

- Parent
- Domain
- effective date
- maximum level
- operation range
- sort behavior

Stage 8 does NOT need to call the stored procedure itself.

KST v2 should preferably own the read-only SQL behind `Kst.Integrations.Qad`
if repository conventions permit a safe equivalent.

Do not redesign the traversal merely because another SQL formulation seems cleaner.

SQL Server 2016 compatibility is required.

---

# 4. Accepted structural traversal behavior

## 4.1 Source

Primary structural source:

`qadpro2.dbo.ps_mstr`

Parent-child relationship:

`ps_par` → `ps_comp`

Domain must match the workspace-resolved QAD domain.

Stage 8 has no operation-range UI.

The scheduler needs the complete current-effective BOM, so do not introduce an
operation filter that would hide otherwise effective structural rows.

## 4.2 Effectivity

A relationship is effective when:

```text
(ps_start IS NULL OR ps_start <= effectiveDate)
AND
(ps_end   IS NULL OR ps_end   >= effectiveDate)

The Stage 8 frontend will not supply an arbitrary effective date.

Later application orchestration will provide the current business date.

The adapter should therefore accept an explicit effective date as an input so
the query remains deterministic and testable.

Do not call system time directly inside the QAD query if the repository already
uses an application clock abstraction.

The application clock is wired later; this adapter simply accepts the date.

4.3 Complete recursion

Traverse every effective structural row.

Do not stop recursion merely because a row:

is not P or M
is Phantom
will eventually be hidden from the scheduler-visible output

This is crucial.

Example:

Parent
  └─ Drawing/nonphysical line
       └─ Manufactured child

The nonphysical intermediate occurrence may eventually be hidden from the grid,
but its manufactured descendant must still be found and must retain its actual level.

P/M presentation filtering occurs after structural traversal.

4.4 Occurrence preservation

Every BOM relationship occurrence is significant.

The same component may legitimately appear:

more than once under the same parent
beneath different subassemblies
at different levels

Do NOT:

consolidate repeated component numbers
use SELECT DISTINCT defensively on BOM output
aggregate BOM occurrences by component

Legacy relationship identity:

oid_ps_mstr

Preserve this internally as the structural occurrence identity.

A future API may expose an opaque occurrenceKey; do not design API DTOs in 8D.2.

4.5 Levels

Preserve the actual structural level produced by the traversal.

Do not cosmetically renumber visible descendants later.

If a hidden Level 2 structural row has a visible Level 3 descendant, the descendant
remains Level 3.

4.6 Traversal order

The legacy procedure performs depth-first structural traversal.

For the Stage 8/default sort behavior (@sortref = 0), sibling ordering from the
reviewed legacy procedure is effectively:

Component Item
Reference
oid_ps_mstr as the final deterministic relationship order

Preserve the depth-first result order using this sibling ordering.

Do not assume OID alone defines sibling order.

Do not use a short fixed-width concatenated OID path such as varchar(40) that
can truncate on a multi-level BOM.

During this Plan pass, recommend a SQL Server 2016-compatible ordering technique
that safely preserves the complete depth-first order.

The ordering technique must be deterministic and testable.

5. Structural/master enrichment required in 8D.2

The structural reader needs enough component master/site information to normalize
the future BOM occurrence.

Required facts:

Component Item

ps_mstr.ps_comp

Description

Use component pt_mstr:

pt_desc1
pt_desc2

Combine null-safely.

One NULL description segment must not erase the other.

The exact whitespace/formatting convention should follow existing repository
normalization patterns where possible.

Qty Per

ps_mstr.ps_qty_per

This is relationship-level Qty Per.

Do NOT multiply Qty Per through hierarchy.

Do NOT calculate Extended Requirement.

Scrap

ps_mstr.ps_scrp_pct

Relationship-level value.

Do not turn this into a requirement calculation.

Phantom

Component master:

pt_mstr.pt_phantom

Do not flatten phantom structure.

A Phantom occurrence remains a structural row and recursion continues beneath it.

6. Accepted P/M classification

Scheduler-visible BOM rows later will be limited to effective P/M values:

P
M

But 8D.2 must preserve the complete traversal before that filtering.

Primary site-specific P/M source:

ptp_det.ptp_pm_code

Join using:

domain
component part
selected workspace site

Do NOT use pt_mstr.pt_site to establish the pt_mstr → ptp_det relationship.

Fallback:

if selected-site ptp_pm_code is non-null/nonblank:
    use ptp_pm_code
else:
    use pt_mstr.pt_pm_code

Important:

The fallback applies ONLY to P/M classification.

Do not establish a general pt_mstr fallback rule for other planning fields.

Treat whitespace-only ptp_pm_code as unavailable, not as an authoritative value.

Known non-P/M codes include values such as:

2
3
4
C
D
N
S

Their business meanings do not need to be solved in this checkpoint.

Do not filter them out during recursion.

The normalized structural occurrence should carry the effective P/M classification
so the later Application service can select visible P/M rows.

7. Intended structural model

Do not blur structural BOM grain with inventory grain.

The accepted conceptual separation is:

BomOccurrence
    structural relationship facts


PartInventorySummary
    Site + Part inventory facts


later Application composition
        ↓


BomLine
    API/UI presentation row

8D.2 should create only the structural concept.

A reasonable conceptual Domain shape is:

BomOccurrence
- Occurrence identity
- Level
- Component Part
- Effective P/M Code
- Phantom
- Description
- Quantity Per
- Scrap
- Structural Sort/Order if needed

The exact C# names should follow repository conventions.

Do not put:

Net QOH
Non-Net QOH
RMA QOH
Extended Requirement
Incoming Supply
Coverage
Material Status
Short Quantity
Projected QOH

on BomOccurrence.

The Domain model should not expose QAD table-shaped names such as oid_ps_mstr.

If the QAD reader needs an internal raw row containing QAD-specific names, keep
that raw row inside Kst.Integrations.Qad.

8. Recommended query direction to assess

Stage 8D.0 suggested that a KST-owned recursive CTE may be the safest direct SQL
implementation.

Treat that as a candidate, not an already-approved SQL implementation.

Assess whether a recursive CTE can safely reproduce:

effective-date filtering
complete traversal
depth-first ordering
sibling ordering:
Component → Ref → OID
occurrence identity
original levels
full recursion through hidden/non-P/M rows
SQL Server 2016 compatibility

If yes, recommend the exact query strategy.

If repository or SQL evidence makes a temp-table/iterative technique safer or more
faithful, explain why.

Do not implement either technique in this Plan pass.

Do not call the legacy stored procedure merely to avoid reproducing known behavior
unless repository evidence strongly favors that approach.

9. Recursion safety

The legacy procedure includes a maximum-level concept.

Stage 8 needs normal multi-level product BOMs, not infinite recursion.

Inspect existing repository conventions and recommend:

a sensible recursion-depth protection mechanism
how a cycle/pathological BOM should fail or be surfaced
whether SQL MAXRECURSION, an explicit level limit, or another SQL Server
2016-compatible guard best fits the project

Do not invent normal business behavior that silently truncates a valid BOM.

A protective failure is preferable to silently returning an incomplete BOM.

Report genuine ambiguity if QAD data has known cycle behavior.

10. Proposed QAD reader boundary

Conceptually, the later consumer will need something like:

ReadBomAsync(
    site,
    parentPart,
    effectiveDate,
    cancellationToken)
        → IReadOnlyList<BomOccurrence>

Domain should continue to be resolved from Site at the QAD integration boundary.

Do not make future frontend/API callers supply QAD Domain.

The exact method/class name should follow current repository conventions.

This checkpoint should NOT add the Application interface/delegate bridge yet
unless a real Application consumer is being implemented here.

The first Application BOM consumer belongs in 8D.3.

Follow the same reasoning used in accepted 8D.1:
establish the Domain + QAD capability first; expose the Application bridge when
the Application use case actually exists.

11. Empty/error semantics

The QAD reader must distinguish:

Valid parent with no effective BOM relationships:

successful empty structural collection

Database/query failure:

exception/failure
never fake an empty BOM

Unknown/nonexistent parent:

assess whether the structural reader itself can or should distinguish this from
"valid parent with no BOM"

Do not invent a second pt_mstr existence query unless repository/Application
responsibility makes it necessary.

If parent existence belongs in 8D.3 orchestration instead, recommend that.

Cancellation:

propagate truthfully according to existing reader conventions
12. Testing requirements

The implementation plan must include focused automated coverage for at least:

Effective-date predicate:
open start
open end
start <= effective date
end >= effective date
Multi-level traversal.
Depth-first ordering.
Sibling ordering:
Component
Ref
OID final ordering
Duplicate component occurrences are preserved.
Same component at multiple levels remains separate.
Phantom occurrence is retained and descendants remain traversable.
Non-P/M intermediate occurrence does not block P/M descendant traversal.
Original structural level is preserved through hidden intermediate rows.
Selected-site ptp_pm_code overrides global pt_pm_code.
NULL site P/M falls back to global.
Blank/whitespace site P/M falls back to global.
Neither source P nor M remains a valid structural occurrence, even though a
later Application service may hide it.
Description combination is null-safe.
Qty Per remains relationship-level.
Scrap remains relationship-level.
No DISTINCT/aggregation collapses BOM occurrences.
Empty BOM returns empty collection.
Query failure/cancellation remains truthful.
Cycle/recursion-limit protection behaves as designed.

Prefer pure query-builder/normalization tests consistent with existing QAD integration
test conventions.

Do not require a live QAD database for the automated test suite.

13. Live-data validation plan

After implementation, plan a small read-only validation against known QAD parents.

We want examples including, where available:

a simple one-level BOM
a known multi-level BOM
duplicate component occurrences
a phantom
a non-P/M structural intermediate with visible P/M descendants
a current effectivity boundary

Compare the new KST-owned traversal with the proven legacy procedure/QAD result.

Validate:

row count
component sequence
Level
Qty Per
Scrap
Phantom
effective P/M
duplicate occurrence preservation

Do not alter QAD data to construct cases.

14. Out of scope

Do not implement or plan in detail:

shared inventory usage
BOM inventory enrichment
Stage 8 Application service
Stage 8 API endpoint
OpenAPI/generated TS
BOM frontend
search UI
Component Info
Standard Cost
QCTC
AVL
Shortages
Component MRP
PO coverage
documentation reconciliation

8D.2 is structural BOM integration only.

15. Required output

Return a concise plan with these sections:

A. Repository Fit

Identify the existing QAD reader/model/test patterns 8D.2 should follow.

Confirm where the new capability should live.

B. Recommended Traversal Strategy

Recommend the safest SQL Server 2016-compatible approach.

Explain specifically how it preserves:

recursion
effectivity
occurrence identity
Level
depth-first ordering
sibling Component → Ref → OID ordering

Do not write production SQL yet.

C. Normalized Structural Model

Recommend the Domain structural model and QAD raw-row shape.

Keep inventory out of it.

D. P/M and Master Enrichment

Describe the exact join/fallback/normalization behavior for:

pt_mstr
selected-site ptp_det
effective P/M
Phantom
Description
E. Recursion / Failure Safety

Recommend cycle/depth protection and empty/error semantics.

F. Exact Implementation File Plan

List:

files to add
files to modify
tests to add/modify

Keep the file set bounded.

Do not add Application/API/frontend files.

G. Verification Plan

List:

focused automated tests
full relevant backend regression commands
small read-only live-QAD comparison
H. Risks / Owner Decisions

List only genuine unresolved issues discovered from repository/source evidence.

Do not manufacture design debates.

If no owner decision is required, say so.

I. Stop Confirmation

Explicitly state:

no production files changed
no tests changed
no commits created
ready for human approval before implementation
16. Planning standard

Do not reopen Stage 8D.0 or 8D.1.

Do not write a broad repository survey.

The purpose of this Plan pass is one question:

What is the smallest safe Domain + QAD implementation that faithfully reproduces
the proven current-effective multi-level BOM traversal and normalization, while
preserving every structural occurrence and its actual order/level for later
Stage 8 composition?

Produce that plan now and stop for human review.