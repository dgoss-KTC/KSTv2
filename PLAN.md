# Stage 8D.2 Plan — BOM QAD Adapter / Normalization

> **STATUS: APPROVED WITH AMENDMENTS — implementing.** Owner approved the plan with four
> amendments (below); all other plan requirements remain in force.
>
> Scope: the smallest safe Domain + QAD capability that reads and normalizes the complete
> current-effective multi-level BOM for a parent — preserving every structural occurrence,
> actual levels, and proven depth-first traversal order — with no inventory enrichment and no
> Application/API/frontend work (those belong to 8D.3+).
>
> **Approved amendments:**
> 1. **Occurrence identity** — `BomOccurrence.OccurrenceKey` identifies the *expanded
>    structural occurrence*, not the physical relationship OID. Generated deterministically
>    during the C# DFS from the relationship-OID path (root OID / ancestor OID / child OID …).
>    Stable; different for the same physical relationship reached through different paths;
>    never used for ordering; opaque to consumers. Test: shared physical descendant via two
>    paths → both emitted, different keys.
> 2. **SQL owns sibling collation/order** — after the recursive closure is reduced to unique
>    physical relationships, SQL Server assigns a numeric sibling rank via an *outer*
>    `ROW_NUMBER() OVER (PARTITION BY parent ORDER BY ComponentPart, Reference, OidPsMstr)`.
>    Database collation stays in SQL; C# owns expansion, DFS, Level, path-based OccurrenceKey,
>    cycle guard, normalization, and trusts `SiblingOrder` for sibling order.
> 3. **DISTINCT approved for the closure only** — collapses duplicate path copies of the same
>    physical `ps_mstr` relationship (identity includes `oid_ps_mstr`); distinct physical
>    relationships stay separate; the C# expansion recreates every legitimate expanded
>    occurrence; no DISTINCT on the final `BomOccurrence` result. Explanatory code comment
>    required.
> 4. **Recursion failure** — `OPTION (MAXRECURSION 100)` approved; do not couple application
>    behavior or tests to a specific SQL Server numeric error code. Exceeded protection /
>    query failure → propagate failure; never a silently truncated BOM.
>
> **Approved §H decisions:** closure DISTINCT — approved; SQL closure + C# DFS — approved with
> Amendment 2; MAXRECURSION 100 — approved; unknown-parent deferral to 8D.3 — approved.
>
> **Live schema confirmed read-only (2026-07, KNWVM13/QADPRO2, SQL Server 2016 SP2):**
> `oid_ps_mstr` decimal(28,10) (fractional OIDs, e.g. `201306300024529805.0009000000`);
> `ps_qty_per`/`ps_scrp_pct` decimal(28,10) nullable; `ps_start`/`ps_end` datetime nullable;
> `ps_par`/`ps_comp`/`ps_ref` nvarchar(60); `pt_desc1`/`pt_desc2` nvarchar(160);
> `pt_phantom` bit nullable; `pt_pm_code`/`ptp_pm_code` nvarchar(60) — unset values commonly
> EMPTY STRING, not NULL. Raw-row types below reflect the confirmed schema.

---

## Context

Stage 8D.1 (shared `Site + Part` inventory capability) is complete and accepted
(commit `bf89c60`). Stage 8D.2 establishes the structural BOM concept on top of it: a QAD
reader that reproduces the proven `dbo.sp_QAD_ktbmpsrp` traversal semantics (current-effective,
multi-level, occurrence-preserving, depth-first) as KST-owned read-only SQL behind
`Kst.Integrations.Qad`, plus a Domain structural model (`BomOccurrence`) that the 8D.3
Application service will filter to P/M and enrich with the 8D.1 inventory capability.

Authoritative evidence: owner-reviewed legacy SP `dbo.sp_QAD_ktbmpsrp` semantics as distilled
in the Stage 8D.2 prompt (the SP source is not in the repository). `qadpro2.dbo.ps_mstr` is the
primary structural source (`ps_par` → `ps_comp`); `oid_ps_mstr` is the relationship identity.

---

## A. Repository Fit

**QAD reader pattern to follow** (all verified in the current tree):

| Concern | Established pattern | Reference |
|---|---|---|
| Reader shape | `sealed class Qad*Reader` in a per-feature folder (`Inventory/`, `PartDetail/`, `Mps/`, `WorkOrders/`); ctor takes `QadConnectionOptions` + `ILogger<T>` | `Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs` |
| Public API | `Task<IReadOnlyList<T>> ReadAsync(site, …, CancellationToken)`; `QadSiteDomainMap.Resolve(site)` inside the reader — callers never supply QAD Domain | `QadPartInventoryReader.ReadSummariesAsync`, `QadMpsSourceReader.ReadAsync` |
| Query construction | `public static (string Sql, DynamicParameters Parameters) Build*Query(…)` — pure, no connection, independently testable | `QadPartInventoryReader.BuildBatchQuery`, `QadPartDetailReader.BuildPartMasterQuery` |
| Connection/timeout/cancellation | `QadConnectionFactory.OpenAsync` (immediate `READ UNCOMMITTED`), `CommandDefinition(sql, parameters, commandTimeout: _options.CommandTimeoutSeconds, cancellationToken)` | `QadConnectionFactory.cs` |
| Failure/cancellation semantics | Exceptions propagate truthfully; never converted to empty/zero; `_options.IsConfigured` guard; `Stopwatch` + `LogInformation` row-count/elapsed log line | all existing readers |
| Raw rows | `Qad*-shaped` record in the same file, QAD column names allowed, "does not travel past this integration boundary" | `QadPartInventoryRawRow`, `QadPartMasterRawRow` |
| Normalization | `public static Normalize(…)` mapping raw → Domain; code-like fields get defensive switch mapping (`NormalizeSupplyType`, `NormalizeWorkOrderState`); C#-side lookup-key normalization precedent (`NormalizePartNumbers`) | `QadMpsSourceReader`, `QadPartInventoryReader` |
| Site ptp_det join | `LEFT JOIN qadpro2.dbo.ptp_det` on `ptp_domain` + `ptp_part` + `ptp_site = @Site`, explicitly **NOT** `pt_site`; LEFT JOIN keeps master rows when site row missing | `QadPartDetailReader.BuildPartMasterQuery` (+ its tests assert `pt_site` absent) |
| Domain model | `sealed record` in a per-feature `Kst.Domain/<Feature>/` folder; XML doc states grain and what it is NOT; no QAD table-shaped names | `Kst.Domain/Inventory/PartInventorySummary.cs` |
| Tests | xunit + FluentAssertions available; convention is **pure query-builder/normalization tests** — assert SQL text/parameter shape, never a live DB | `Kst.Integrations.Qad.Tests/Inventory/QadPartInventoryReaderTests.cs` |

**Placement confirmed:** no BOM model, query, or reader exists anywhere in the backend yet
(searched `Bom` across `src/backend` — only doc-comment mentions). New capability lives in:
- `Kst.Domain/Bom/` (structural record only — no logic),
- `Kst.Integrations.Qad/Bom/` (reader + raw row).

**Architectural tests** (`Kst.ArchitectureTests/DependencyRuleTests.cs`) are satisfied by this
placement: Domain gains no infrastructure references; no Application/SQL code is added.

**8D.1 reasoning applied:** no Application interface/delegate bridge and no `Program.cs` DI
wiring in 8D.2 — the first Application BOM consumer is 8D.3 (same deferral accepted for
8D.1's `IPartInventoryReader`/`DelegatePartInventoryReader`).

---

## B. Recommended Traversal Strategy

**Recommendation: a KST-owned recursive CTE over `ps_mstr` (SQL) for the effective structural
closure, plus a deterministic depth-first ordering + level assignment in pure C#** —
not the legacy SP call, not a temp-table/cursor loop, and no window functions inside the CTE.

### B.1 Why a recursive CTE (and why not the alternatives)

- **Not the legacy SP call.** Repository precedent rejects calling legacy procedures
  ("Do not implement `sp_QAD_ktmpswkm` or create a new database procedure" — Stage 5), the
  8D.2 prompt prefers KST-owned SQL, and KST-owned SQL keeps the query inspectable,
  parameterized, and inside the integration boundary.
- **Not temp tables/cursors.** No existing QAD reader uses temp tables; every reader is a
  single set-based parameterized statement via Dapper `CommandDefinition`. A temp-table
  iterative loop would be a new, heavier pattern with no evidence it is safer here.
- **Recursive CTE fits**: single set-based statement, fully parameterized, SQL Server 2016
  native (recursive CTEs since 2005), composes with the existing `Build*Query`/Dapper
  convention, and reproduces the legacy semantics directly:

| Legacy semantic | CTE reproduction |
|---|---|
| Complete traversal (no early stop on non-P/M/phantom/hidden rows) | Recursion condition is **only** domain + effective-date predicates on `ps_mstr`; no P/M, phantom, or operation (`ps_op`) filter anywhere |
| Effectivity | `(ps_start IS NULL OR ps_start <= @EffectiveDate) AND (ps_end IS NULL OR ps_end >= @EffectiveDate)` — applied in **both** the anchor and the recursive member; `@EffectiveDate` is an explicit parameter (app clock wired later in 8D.3; no `GETDATE()` in the query) |
| Recursion parent→child | `INNER JOIN` of `ps_mstr` child rows on `frontier.ps_comp = child.ps_par` (single recursive reference, right side of INNER JOIN — the documented SQL Server form) |
| Occurrence identity | `oid_ps_mstr` carried on every row, never used to deduplicate output |
| Original levels | Assigned in C# as DFS depth (see B.3) — no cosmetic renumbering |
| No operation range | The SP's operation-range parameter is deliberately **not** reproduced (Stage 8 has no operation UI) |

### B.2 Responsibility split — SQL closure + sibling rank, C# traversal (approved, Amendment 2)

The CTE returns the **flat set of effective relationship rows reachable from the parent**
(the structural closure, with per-path copies). The **outer (non-recursive) SELECT** reduces
that closure to unique physical relationships (the approved closure DISTINCT) and assigns a
**numeric sibling rank** per parent. A pure C# method then performs the deterministic
depth-first pre-order walk, assigns levels, and builds path-based OccurrenceKeys.

```
SQL:  reachable effective structural relationships,
      physical relationship identity,
      Component → Reference → OID sibling rank (database collation),
      master/site enrichment
C#:   parent/child expansion, depth-first traversal, structural Level,
      path-based OccurrenceKey, cycle guard, normalization
```

Why the sibling rank lives in an *outer* `ROW_NUMBER()` rather than inside the recursion:

1. **SQL Server recursive-CTE restrictions** block the standard in-recursion "sibling rank
   path" technique (single recursive reference; no usable per-parent ranking in the
   recursive member). A window function in the **outer** query over the reduced closure is
   unrestricted and is exactly the approved Amendment-2 formulation.
2. **Database collation stays in SQL** (Amendment 2): `ORDER BY ps_comp, ps_ref, oid_ps_mstr`
   inside the `ROW_NUMBER()` uses the QAD database collation — a C# comparer could not be
   guaranteed to reproduce it. C# consumes the numeric `SiblingOrder` as-is.
3. **Testability**: the C# DFS (expansion, level, key, cycle guard) is directly unit-testable
   with synthetic trees; the SQL shape (DISTINCT identity, ROW_NUMBER partition/order,
   predicates, joins, MAXRECURSION) is text-assertable per repo convention.
4. **Faithfulness**: the C# walk reproduces the legacy per-visit semantics — *when a part is
   visited, its children are all effective `ps_mstr` rows for that part, recursed beneath
   each child in sibling-rank order*.

**Sibling order (per parent):** SQL `ROW_NUMBER() OVER (PARTITION BY ps_par ORDER BY
ps_comp, ps_ref, oid_ps_mstr)` — Component → Reference → OID, in database collation. (OID
alone does **not** define sibling order.)

**Level:** C# DFS depth, 1-based. A descendant beneath a hidden (non-P/M) intermediate keeps
its actual level (e.g., Level 3 under a hidden Level 2). No renumbering.

### B.3 The one DISTINCT — and why it is not "defensive deduplication"

In a frontier-join recursive CTE, a **shared** physical relationship row (same
`oid_ps_mstr`) is emitted once per path that reaches its parent (path multiplicity), and
duplicate component rows multiply that further. The final `SELECT DISTINCT` over the full
row **including `oid_ps_mstr`** therefore collapses only *identical physical relationship
rows* — it can never merge two distinct occurrences (distinct rows have distinct OIDs).

The prohibited "defensive DISTINCT on BOM output" does **not** occur: the reader's returned
occurrence list is built by the C# expansion, which deliberately re-lists a shared
relationship under every parent-occurrence that reaches it (duplicate components, diamonds,
same component at multiple levels — all preserved). This is the single place `DISTINCT`
appears in the query, it is semantically load-bearing (identity-preserving closure), and it
is flagged for explicit owner confirmation in §H.

**Query shape (approved, per Amendments 2–3):**

```sql
WITH BomStructure AS
(
    -- Anchor: effective level-1 relationships of the parent
    SELECT ps.oid_ps_mstr, ps.ps_par, ps.ps_comp, ps.ps_ref,
           ps.ps_qty_per, ps.ps_scrp_pct
    FROM qadpro2.dbo.ps_mstr AS ps
    WHERE ps.ps_domain = @Domain
      AND ps.ps_par = @ParentPart
      AND (ps.ps_start IS NULL OR ps.ps_start <= @EffectiveDate)
      AND (ps.ps_end   IS NULL OR ps.ps_end   >= @EffectiveDate)

    UNION ALL

    -- Recursion: effective children of each frontier component.
    -- No P/M, phantom, or operation filter — complete traversal.
    SELECT ch.oid_ps_mstr, ch.ps_par, ch.ps_comp, ch.ps_ref,
           ch.ps_qty_per, ch.ps_scrp_pct
    FROM qadpro2.dbo.ps_mstr AS ch
    INNER JOIN BomStructure AS frontier
        ON frontier.ps_comp = ch.ps_par
    WHERE ch.ps_domain = @Domain
      AND (ch.ps_start IS NULL OR ch.ps_start <= @EffectiveDate)
      AND (ch.ps_end   IS NULL OR ch.ps_end   >= @EffectiveDate)
)
SELECT
    u.oid_ps_mstr    AS OidPsMstr,
    u.ps_par         AS ParentPart,
    u.ps_comp        AS ComponentPart,
    u.ps_ref         AS Reference,
    u.ps_qty_per     AS QuantityPer,
    u.ps_scrp_pct    AS ScrapPercentage,
    pt.pt_desc1      AS Description1,
    pt.pt_desc2      AS Description2,
    pt.pt_phantom    AS Phantom,
    ptp.ptp_pm_code  AS SitePmCode,
    pt.pt_pm_code    AS GlobalPmCode,
    ROW_NUMBER() OVER (
        PARTITION BY u.ps_par
        ORDER BY u.ps_comp, u.ps_ref, u.oid_ps_mstr
    ) AS SiblingOrder
FROM (
    -- APPROVED 8D.2 CLOSURE DISTINCT (Amendment 3): collapses only duplicate PATH COPIES
    -- of the same physical ps_mstr relationship (identity includes oid_ps_mstr). NOT
    -- business-level BOM deduplication — the C# structural expansion recreates every
    -- legitimate expanded occurrence; the final BomOccurrence result is never DISTINCTed.
    SELECT DISTINCT
        b.oid_ps_mstr, b.ps_par, b.ps_comp, b.ps_ref,
        b.ps_qty_per, b.ps_scrp_pct
    FROM BomStructure AS b
) AS u
LEFT JOIN qadpro2.dbo.pt_mstr AS pt
    ON pt.pt_domain = @Domain AND pt.pt_part = u.ps_comp
LEFT JOIN qadpro2.dbo.ptp_det AS ptp
    ON ptp.ptp_domain = @Domain
    AND ptp.ptp_part  = u.ps_comp
    AND ptp.ptp_site  = @Site
OPTION (MAXRECURSION 100);
```

Parameters: `@Domain` (from `QadSiteDomainMap`), `@ParentPart`, `@EffectiveDate`
(`DateOnly` → midnight `DateTime`, same convention as the price query), `@Site`.
All joins carry `@Domain`; `pt_site` is never used. No final `ORDER BY` — the C# DFS
consumes `SiblingOrder`.

---

## C. Normalized Structural Model

### C.1 Domain — `Kst.Domain/Bom/BomOccurrence.cs` (new)

```csharp
namespace Kst.Domain.Bom;

// sealed record — structural BOM occurrence (relationship grain, NOT inventory grain).
public sealed record BomOccurrence(
    string  OccurrenceKey,      // opaque EXPANDED-occurrence identity (Amendment 1): deterministic path of
                                // relationship OIDs from the root ("oidA/oidB/..."); different per structural path,
                                // never used for ordering; consumers must not parse it
    int     Level,              // actual structural level (1-based, preserved through hidden rows)
    string  ComponentPart,      // ps_comp
    string? PmCode,             // effective P/M classification (site ptp_det, fallback pt_mstr); any code; unfiltered
    bool    IsPhantom,          // pt_mstr.pt_phantom
    string? Description,        // null-safe pt_desc1 + pt_desc2 combination
    decimal? QuantityPer,       // ps_qty_per — relationship-level; never multiplied through hierarchy
    decimal? ScrapPercentage);  // ps_scrp_pct — relationship-level; no requirement calculation
```

Deliberately **absent** (prompt §7): Net/Non-Net/RMA QOH, Extended Requirement, Incoming
Supply, Coverage, Material Status, Short Quantity, Projected QOH, parent-part, reference,
sort key, and any QAD table-shaped name. Grain statement (in the XML doc, per
`PartInventorySummary` convention): one row per structural occurrence in traversal order;
inventory belongs to `PartInventorySummary` (Site + Part) and is composed later in 8D.3.
No logic in the record → no `Kst.Domain.Tests` additions needed (same as 8D.1).

### C.2 QAD raw row — in `QadBomReader.cs` (integration-only, does not cross the boundary)

```csharp
public sealed record QadBomStructuralRawRow(
    decimal  OidPsMstr,       // relationship identity — live-confirmed decimal(28,10)
    string   ParentPart,      // ps_par — needed by the C# DFS parent linkage
    string   ComponentPart,   // ps_comp
    string?  Reference,       // ps_ref — carried for traceability; sibling order comes from SiblingOrder
    decimal? QuantityPer,     // ps_qty_per decimal(28,10), nullable (live-confirmed)
    decimal? ScrapPercentage, // ps_scrp_pct decimal(28,10), nullable (live-confirmed)
    string?  Description1,    // pt_mstr.pt_desc1 (raw segments; combined in C#)
    string?  Description2,    // pt_mstr.pt_desc2
    string?  SitePmCode,      // ptp_det.ptp_pm_code for the selected site (raw; fallback in C#)
    string?  GlobalPmCode,    // pt_mstr.pt_pm_code (raw; commonly empty string when unset)
    bool?    Phantom,         // pt_mstr.pt_phantom bit, nullable (live-confirmed)
    long     SiblingOrder);   // SQL ROW_NUMBER per parent: Component → Reference → OID, DB collation;
                              // long because the driver reports ROW_NUMBER() as Int64 (live-confirmed)
```

**Live smoke-check finding (2026-07, before first successful read):** Dapper materializes the
raw record **positionally** — constructor parameter order must mirror the SELECT column order
exactly (same failure class as the documented Stage 7 positional-deserialization bug). The raw
row order above matches the query's column order; the smoke check ran the real `ReadAsync` path
read-only against KNWVM13 and confirmed the query executes on SQL Server 2016 and maps cleanly.

**Live schema confirmed read-only (2026-07)** against KNWVM13/QADPRO2 (SQL Server 2016 SP2):
`oid_ps_mstr` is `decimal(28,10)` (fractional OIDs), `ps_qty_per`/`ps_scrp_pct`
`decimal(28,10)` nullable, `ps_par`/`ps_comp`/`ps_ref` `nvarchar(60)`, `ps_start`/`ps_end`
`datetime` nullable, `pt_desc1`/`pt_desc2` `nvarchar(160)`, `pt_phantom` `bit` nullable,
`pt_pm_code`/`ptp_pm_code` `nvarchar(60)` (unset = empty string, not NULL).

`QuantityPer`/`ScrapPercentage` are nullable (faithful to possible NULL; no invented
zero-fill — unlike inventory aggregates, a relationship value has no zero identity).
`Phantom` nullable for the LEFT-JOIN-missing-master case. `OidPsMstr` is non-nullable
(physical relationship identity; a NULL OID is a genuine data error that should surface,
not be normalized).

---

## D. P/M and Master Enrichment

SQL performs the two LEFT JOINs (shape in B.3): `pt_mstr` on **domain + component**,
`ptp_det` on **`ptp_domain` + `ptp_part` + `ptp_site = @Site`** — explicitly **not**
`pt_mstr.pt_site` (accepted Stage 6 rule; existing test asserts `pt_site` absence).
LEFT JOIN (not INNER) so a missing master row never drops a structural occurrence; the row
survives with master-sourced facts null.

All per-row resolution happens in pure, unit-testable C# statics on `QadBomReader`
(normalization precedent, like `NormalizePartNumbers`):

1. **Effective P/M** — `ResolveEffectivePmCode(string? sitePm, string? globalPm)`:
   - trim `sitePm`; if non-blank → return it (any code — `P`, `M`, or known non-P/M codes
     `2/3/4/C/D/N/S` pass through unclassified; P/M *visibility filtering* is 8D.3's job);
   - else trim `globalPm`; if non-blank → return it;
   - else `null`.
   - Whitespace-only `ptp_pm_code` = unavailable (falls back), never an authoritative value.
   - The fallback applies **only** to P/M — no general `pt_mstr` fallback rule for other
     planning fields (none are selected in 8D.2 at all).
2. **Description** — `CombineDescription(string? d1, string? d2)`: trim each segment, drop
   null/whitespace-only segments, join remaining segments with a single space; none remain
   → `null` (missing description is null, matching `PartDetailSourceFacts.Description`).
   One NULL segment never erases the other. Single-space join follows the repo's trim-based
   normalization convention (no existing desc1+desc2 precedent exists in the repo; single
   space is the minimal neutral choice).
3. **Phantom** — `row.Phantom ?? false`: a missing `pt_mstr` row is not evidence of
   phantom; the structural row is preserved with `IsPhantom = false`. (Live-confirmed:
   `pt_phantom` is `bit` — direct `bool?` mapping, no code switch needed.)
4. **Qty Per / Scrap** — passthrough per relationship row; never multiplied, never
   extended; no Extended Requirement or requirement math of any kind.
5. **OccurrenceKey (Amendment 1)** — generated during the C# DFS as the deterministic path
   of relationship OIDs from the root: level-1 key = `FormatOid(oid)`; deeper keys =
   `parentKey + "/" + FormatOid(oid)`; `FormatOid` = invariant-culture decimal string
   (OIDs are `decimal(28,10)`; "/" cannot appear in a decimal string, so paths are
   unambiguous). The key identifies the **expanded structural occurrence**: the same
   physical relationship (same OID) reached through two different structural paths yields
   two occurrences with different keys. It is stable/deterministic, never used to determine
   traversal order (order is SQL `SiblingOrder`), opaque to consumers, and the QAD-specific
   `oid_ps_mstr` naming stays inside the QAD integration layer. A future 8D.3 API
   `occurrenceKey` maps straight from it (no API DTOs designed in 8D.2).

**Phantom and non-P/M rows are structural rows**: the traversal has no phantom or P/M logic
at all — recursion continues beneath both, and the normalized occurrence simply carries
`IsPhantom`/`PmCode` facts for 8D.3 to consume.

---

## E. Recursion / Failure Safety

- **Depth protection — `OPTION (MAXRECURSION 100)`, explicit in the query (Amendment 4).**
  - This is a *protective* ceiling, not a business level limit: when exceeded, the statement
    **fails** — a truthful protective failure, never silent truncation (an explicit
    `WHERE Level < N` cap would silently return an incomplete BOM and is rejected).
  - Application behavior and tests are **not coupled to any specific SQL Server numeric
    error code**: the required behavior is simply “exceeded recursion protection / query
    failure → propagate failure → never return a silently truncated BOM.”
  - The legacy SP's caller-supplied "maximum level" is not a Stage 8 business concept;
    normal product BOMs are far shallower than 100.
  - Cycles in `ps_mstr` are data errors (QAD has no known legitimate cycle behavior — no
    repository evidence of any); a cycle makes the recursion non-terminating and is caught
    by `MAXRECURSION` → exception.
- **C# DFS cycle guard (defense in depth, unit-testable).** Real cycles fail in SQL first,
  but the pure C# walker is made safe against *any* row set: it tracks the parts on the
  current ancestor path (case-insensitive); a child part already on the path throws a
  descriptive `InvalidOperationException`. Legitimate **diamonds** (same part beneath two
  different parents) are not cycles and are fully preserved — each occurrence is emitted
  at its own level.
- **Empty vs error vs unknown parent.**
  - Parent with no effective relationships → anchor produces no rows → **successful empty
    `IReadOnlyList<BomOccurrence>`**.
  - Query/DB failure or cancellation → exception propagates truthfully (existing reader
    convention); **never** a faked empty BOM.
  - Unknown/nonexistent parent → also empty from the structural query. The
    "unknown parent" distinction is **not** resolved in 8D.2 with a second `pt_mstr`
    existence query: the BOM reader's job is structure, and the established
    part-existence semantics already live in `QadPartDetailReader` (returns `null` when no
    `pt_mstr` row). Recommended: 8D.3 orchestration performs the unknown-parent 404
    semantics (reusing existing PartDetail/part-master behavior) when the API layer
    actually needs it.
- **Input validation:** null `parentPart` → `ArgumentNullException`; blank →
  `ArgumentException` (repo convention, `NormalizePartNumbers`). Unconfigured QAD →
  `InvalidOperationException`. Unknown site → `QadSiteDomainMap.Resolve` throws
  (existing behavior).

---

## F. Exact Implementation File Plan

Bounded file set — **3 files added, 0 modified**. No Application, API, OpenAPI, generated
TS, frontend, DI/`Program.cs`, or documentation changes.

| # | File (add) | Contents |
|---|---|---|
| 1 | `src/backend/Kst.Domain/Bom/BomOccurrence.cs` | §C.1 record + grain/boundary XML doc |
| 2 | `src/backend/Kst.Integrations.Qad/Bom/QadBomReader.cs` | `sealed class QadBomReader`: `Task<IReadOnlyList<BomOccurrence>> ReadAsync(string site, string parentPart, DateOnly effectiveDate, CancellationToken ct = default)`; `public static (string Sql, DynamicParameters Parameters) BuildQuery(string domain, string site, string parentPart, DateOnly effectiveDate)`; `public static IReadOnlyList<BomOccurrence> TraverseDepthFirst(string rootParent, IReadOnlyList<QadBomStructuralRawRow> rows)` (DFS + levels + SQL-SiblingOrder consumption + path OccurrenceKey + cycle guard); `public static string? ResolveEffectivePmCode(…)`; `public static string? CombineDescription(…)`; `public static BomOccurrence Normalize(QadBomStructuralRawRow raw, int level, string occurrenceKey)`; `sealed record QadBomStructuralRawRow` (§C.2) |
| 3 | `src/backend/tests/Kst.Integrations.Qad.Tests/Bom/QadBomReaderTests.cs` | xunit; pure query-builder/normalization/traversal tests (no DB) |

`ReadAsync` body follows the existing reader skeleton exactly: `IsConfigured` guard →
`QadSiteDomainMap.Resolve` → validate parent → `QadConnectionFactory.OpenAsync` →
`CommandDefinition(BuildQuery(…), CommandTimeoutSeconds, ct)` → `QueryAsync<
QadBomStructuralRawRow>` → `TraverseDepthFirst(parent, rows)` → stopwatch/log line →
return.

### F.1 Test list (maps to prompt §12)

**SQL shape (`BuildQuery`)** — text/parameter assertions per repo convention:
1. Parameters `@Domain`, `@ParentPart`, `@EffectiveDate` (midnight `DateTime`), `@Site`.
2. Effective-date predicate present in **both** anchor and recursive member (open start /
   open end / `start <=` / `end >=` forms asserted verbatim).
3. Recursion joins `frontier.ps_comp = child.ps_par` and `ch.ps_domain = @Domain`.
4. **No `ps_op`** anywhere (no operation filter).
5. Closure `SELECT DISTINCT` **includes `oid_ps_mstr`** (identity-preserving path-copy
   collapse, Amendment 3); no `GROUP BY`; no aggregation; no other DISTINCT in the query.
6. `LEFT JOIN qadpro2.dbo.pt_mstr` on domain + part; `pt_site` absent from the whole SQL.
7. `LEFT JOIN qadpro2.dbo.ptp_det` on `ptp_domain` + `ptp_part` + `ptp_site = @Site`.
8. `OPTION (MAXRECURSION 100)` present.
9. Sibling rank (Amendment 2): `ROW_NUMBER() OVER` with `PARTITION BY` on the parent and
   `ORDER BY` Component → Reference → OID; `SiblingOrder` is a selected column.
10. Injection test: raw parent value never concatenated into SQL text (parameterized).
11. `READ UNCOMMITTED` is connection-level (factory) — no `SET` in the query (matches
    existing readers).

**Normalization (pure C#):**
12. `ResolveEffectivePmCode`: site wins when non-blank (incl. non-P/M code `C` passthrough);
    NULL site → global; whitespace-only site → global; both null → null; trims. (Live data
    confirmed: unset codes are often empty string, not NULL — blank handling is load-bearing.)
13. `CombineDescription`: both → single-space join; either NULL/blank → the other; both
    null/blank → null; trims segments.
14. Phantom: raw `true`/`false`/`null` → `true`/`false`/`false`.
15. `Normalize`: `OccurrenceKey` passthrough from the DFS path; `QuantityPer`/
    `ScrapPercentage` passthrough (relationship-level, unmodified); P/M + description +
    phantom composed per 12–14.

**Traversal (`TraverseDepthFirst`, synthetic raw-row trees with SQL-style `SiblingOrder`):**
16. Multi-level chain A→B→C→D: full traversal, levels 1–4.
17. Depth-first pre-order: child subtree completes before next sibling; parent before
    descendants.
18. Sibling order follows the SQL-assigned `SiblingOrder` rank exactly — including a
    fixture where `SiblingOrder` deliberately differs from any C# string comparison, proving
    C# does not re-derive collation (Amendment 2) and that the key is not used for ordering.
19. Duplicate component under the same parent: both occurrences preserved; shared
    relationship re-listed beneath each (descendants duplicated per occurrence).
20. **Shared physical descendant via two different paths (Amendment 1)**: same relationship
    OID under two different parents (A→B→D, A→C→D) → both D occurrences emitted, at their
    own levels, with **different** `OccurrenceKey` values; keys deterministic across calls.
21. Same component at multiple levels: separate occurrences, correct levels, no collapse,
    no exception (diamond is not a cycle).
22. Phantom intermediate: retained (`IsPhantom` true) and descendants traversed.
23. Non-P/M intermediate (`PmCode = "N"`): retained; P/M descendants found at correct level.
24. Level preserved through hidden intermediate (descendant remains Level 3; no renumber).
25. Empty row list → empty result.
26. Root part matching is case-insensitive (`"abc"` matches `ParentPart "ABC"`).
27. Cyclic rows (A→B, B→A) → descriptive exception, no infinite loop.
28. A relationship row whose `ParentPart` is unreachable from the root is not emitted
    (closure is root-scoped).

---

## G. Verification Plan

1. **Focused automated tests** — the §F.1 suite (no live QAD required; consistent with
   existing QAD integration test conventions). Query failure/cancellation truthfulness has
   no live-DB-free test surface (same as 8D.1): it is guaranteed structurally by the
   unchanged reader skeleton — `CommandDefinition` propagates cancellation, exceptions are
   never caught/converted to an empty list anywhere in `ReadAsync`, and the live
   validation step (G.3) exercises real query execution.
2. **Backend regression** (repository-documented commands, `docs/development/BUILD_AND_TEST.md`):
   ```powershell
   cd src/backend
   dotnet restore Kst.slnx
   dotnet format Kst.slnx --verify-no-changes
   dotnet build Kst.slnx --nologo
   dotnet test Kst.slnx --nologo
   ```
   No frontend/OpenAPI/sidecar/Tauri steps: 8D.2 changes no contracts, no API surface, no
   host-affecting code (new internal files only).
3. **Live read-only validation (post-implementation, owner-run, QAD untouched):**
   - **Live schema confirmation — DONE (2026-07)** before implementation, as owner-directed:
     read-only catalog + sample queries against KNWVM13/QADPRO2 (SQL Server 2016 SP2).
     Confirmed: `oid_ps_mstr` decimal(28,10); `ps_qty_per`/`ps_scrp_pct` decimal(28,10)
     nullable; `ps_start`/`ps_end` datetime nullable; `pt_phantom` bit nullable;
     `pt_pm_code`/`ptp_pm_code` nvarchar(60), empty string when unset. Raw row finalized
     accordingly (see header + §C.2). Throwaway harness deleted after use; nothing committed.
   - Comparison mechanism: a small **throwaway, non-committed** console harness outside the
     solution that calls `QadBomReader.ReadAsync` (QAD is reachable via existing
     Windows-integrated-auth config) — since 8D.2 has no API/UI yet, the app itself cannot
     exercise it. The owner runs `dbo.sp_QAD_ktbmpsrp` (Parent/Domain/effective date/max
     level/`@sortref = 0`) for the same parents as ground truth.
   - Case classes to select among known `SW`/`KTC` parents (per prompt §13): simple
     one-level BOM; known multi-level BOM; duplicate component occurrences; a phantom; a
     non-P/M structural intermediate with visible P/M descendants; a current effectivity
     boundary (row with `ps_start`/`ps_end` exactly at the effective date).
   - Compare: row count, component sequence, Level, Qty Per, Scrap, Phantom, effective P/M,
     duplicate-occurrence preservation. No QAD data altered to construct cases; absent case
     classes stay covered by deterministic automated tests (Stage 6/7 precedent).

---

## H. Risks / Owner Decisions — ALL RESOLVED (owner-approved with amendments)

1. **Closure `SELECT DISTINCT` (B.3)** — **APPROVED** (Amendment 3), closure only,
   identity includes `oid_ps_mstr`, explanatory code comment required, no DISTINCT on the
   final `BomOccurrence` result.
2. **Ordering ownership split (SQL closure + sibling rank / C# DFS)** — **APPROVED with
   Amendment 2**: SQL assigns `SiblingOrder` via outer `ROW_NUMBER()` (database collation
   stays in SQL); C# owns expansion/DFS/Level/OccurrenceKey/cycle guard/normalization and
   trusts `SiblingOrder`.
3. **`MAXRECURSION 100` protective ceiling** — **APPROVED** (Amendment 4); no coupling to
   any specific SQL Server numeric error code; failure propagates, never silent truncation.
4. **Unknown-parent semantics deferred to 8D.3** — **APPROVED**.

Remaining verification item (not a decision): NULL-reference sibling ordering within the
SQL `ROW_NUMBER()` (nulls-first per standard collation ASC) — to be confirmed by the live
sequence comparison in G.3.

No QAD cycle behavior is known from repository evidence; cycles are handled as protective
failures (E). No open owner decisions remain.

---

## I. Stop Confirmation

- **No production files changed** in this planning pass.
- **No tests changed or added.**
- **No API/OpenAPI/generated TypeScript, frontend, or documentation changes.**
- **No commits created or pushed** (working tree: only the untracked 8D.2 prompt file
  pre-exists).
- **Ready for human review/approval before implementation.** Implementation starts only
  after explicit owner approval of this plan (including §H items 1–3).
