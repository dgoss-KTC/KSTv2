# KST v2 — Stage 10.1 Component Orders Field-Discovery Ledger

**Status:** Read-only discovery baseline — no production code, configuration, database connection, or Stage 9 change
**Scope:** Canonical Component Orders data surface for the active workspace
**Companion:** `STAGE_10_DISCUSSION_FRAMEWORK.md`

## 1. Accepted behavioral contract

- Component Orders is the canonical PO-related data surface.
- It is scoped to components relevant to the active workspace and lists only components with
  qualifying conventional open PO supply.
- Canonical detail grain is component + PO line. Components are grouped in the UI: the collapsed
  row shows the earliest due PO, and an arrow expands additional PO lines.
- Components with a missing due date sort first as yellow exceptions. The remainder sort by earliest
  PO due date; ties use PO number, then Line.
- A PO line qualifies only when `pod_qty_ord - pod_qty_rcvd > 0` and its status is not C or X,
  case-insensitively. `PO Qty` is labeled **Open Quantity**.
- KSS is an indicator on an included conventional PO line. A KSS component with no conventional
  qualifying open PO is outside this initial list.
- Current Comments use the latest active ShortageMaster record for site + component. The table
  shows two wrapped preview lines; a detail box shows the selected record’s complete multiline text.
- Credit Hold and CIA show a checkmark only for truthy source values; false, blank, null, missing,
  and zero values display blank.

## 2. Authority and evidence reviewed

| Evidence | Role in this discovery |
|---|---|
| KST v2 current status / Stage 9 closeout | Stage 9 is locked and is not modified by this work. |
| `docs/implementation/KST_v2_STAGE_9_SOURCE_MAPPING.md` | Accepted QAD evidence for open quantity, C/X exclusion, tracking, and independent effective KSS classification. |
| `QadNextPurchaseOrderReader.cs` | Current v2 parameterized reader shape and existing line-level qualification/order evidence. It supplies Stage 9 context only; it is not the Stage 10 service. |
| KSTv1 `features/shortage_report.py` | Legacy field/source evidence for PO detail, buyer display, lead time, supplier, ShortageMaster, and PreferredSuppliers. Not automatically authoritative. |
| Project-owner decisions in the Stage 10 discussion | Authoritative product behavior for the table, filtering, grouping, dates, and display rules. |

## 3. Field-source ledger

| Display field | Proposed source / transform | Status | Validation still required |
|---|---|---|---|
| Comp | `pod_det.pod_part` | Established QAD field | Confirm active-workspace component-scope derivation. |
| Description | `pt_mstr.pt_desc1`, joined by domain + part | **Validated 10.1:** field confirmed; missing master is a real data case (8% of SW open lines, incl. sample `86089-007*AIR AUTH`) | Missing-master display behavior = product decision (mapping doc F5). |
| Weeks LT | `COALESCE(ptp_det.ptp_pur_lead, pt_mstr.pt_pur_lead)` days; display `ceil(days / 7)` | **Validated 10.1:** both columns int (nullable) — fractional impossible by type; zero common (63% SW site rows, 55% master-only parts); no NULLs observed in KTC/SW; site≠master values confirm fallback is meaningful | Display rules for zero/null = product decision; purchasing-vs-cumulative choice unchanged. |
| PO | `pod_det.pod_nbr` | Established QAD field | None beyond line identity test. |
| Line | `pod_det.pod_line` | **Validated 10.1:** int — no leading-zero/formatting issue possible by type | None. |
| PO Due | `pod_det.pod_due_date` | **Validated 10.1:** datetime, nullable; NULL due dates exist on qualifying open lines (yellow-exception case real) | Date-only normalization follows Stage 9 reader convention. |
| Open Quantity | `pod_qty_ord - pod_qty_rcvd` | **Validated 10.1:** re-confirmed incl. partially received lines; `pod_um` blank on 718/18,041 SW open lines, UOM mismatch rare (62) | Displayed UOM policy = product decision. |
| Confirmed | `pod_det.pod__log01` | **Validated 10.1 (accepted rule confirmed):** bit, nullable; SW open population 1→14,979 / 0→3,063, no NULLs observed; Yes/No maps directly from bit | None remaining on QAD side. Do not substitute the PO-master `po_confirm` fact. |
| Supplier | PO vendor identity `po_mstr.po_vend`; display via **`vd_mstr.vd_addr = po_vend AND vd_domain = <workspace domain>`** → `vd_sort` (name) | **Owner-directed 10.3F.5 V1-compatible runtime correction:** the displayed `vd_sort` is the exact Shortages risk key to `[Supplier Name]`. The prior 18/20 `po_vend → [Supplier Nbr]` bounded result is historical evidence only and superseded for runtime use because live Component Orders showed no risk facts. Modern-ODBC validation was unavailable on this workstation; live UAT is pending. Shortages has no domain discriminator, so QAD domain resolves the vendor display only. | No trim, case, numeric conversion, padding, cross-domain fallback, or Supplier Nbr fallback is accepted. Live UAT confirmation is required; no population-wide conclusion is claimed. |
| Buyer | Site `ptp_det.ptp_buyer`, master `pt_mstr.pt_buyer` fallback; `code_mstr` lookup to `code_user1` with `code_fldname` discriminator (`ptp_buyer` / `pt_buyer`) | **QUALIFIED 10.1 — owner decision required (F1):** discriminator and fallback cases confirmed live (no-buyer case real: 1,125 KTC/SW parts), but `code_mstr` is domain-partitioned and the same code maps to different users per domain — contradicts "not site/domain scoped" on the domain dimension | Owner confirmation of domain-scoped lookup before implementation. |
| Manufacturer Item | `pod_det.pod_vpart` | **Validated 10.1:** nvarchar(80); populated in samples; blank observed (no-master sample part) → blank display | None. |
| KSS | Independent effective `pod_det` + `po_mstr` relationship: same domain/site/part; effective `pod_end_eff##1`; `po_sched = 1`; effective `po_eff_to` | **Validated 10.1:** Stage 9 mapping re-confirmed (types datetime/bit/datetime); indicator-only treatment unchanged | Live sample of KSS + conventional open PO NOT OBSERVED in any site/domain at validation date (F3) — evidence limit, not a rule change. |
| Tracking Info | `pod_det.pod__chr06` | **Validated 10.1:** mostly blank on open lines; free text observed (`Split line#1`, case variants in history) | Trim + empty→null per existing reader convention; no other normalization accepted. |
| Credit Hold | `dbo.PreferredSuppliers.[RCHI]`, selected by exact `SupplierDisplay`/`vd_sort` = `[Supplier Name]`; duplicate order `Date DESC`, `ID DESC` | **Owner-directed 10.3F.5 V1-compatible runtime correction; live UAT pending.** No match remains blank no-enrichment, never an error or PO-line exclusion. `RCHI` is a non-null `bit`; true is truthy and false blank. The prior Supplier Nbr result is historical only. | QAD domain resolves the vendor display only; no Shortages domain predicate, trim, case/numeric conversion, padding, Supplier Nbr fallback, or population-wide conclusion is accepted. |
| CIA | `dbo.PreferredSuppliers.[CIA]`, selected by exact `SupplierDisplay`/`vd_sort` = `[Supplier Name]`; duplicate order `Date DESC`, `ID DESC` | **Owner-directed 10.3F.5 V1-compatible runtime correction; live UAT pending.** No match remains blank no-enrichment, never an error or PO-line exclusion. `CIA` is a non-null `bit`; true is truthy and false blank. The prior Supplier Nbr result is historical only. | QAD domain resolves the vendor display only; no Shortages domain predicate, trim, case/numeric conversion, padding, Supplier Nbr fallback, or population-wide conclusion is accepted. |
| Current Comments | Latest active `dbo.ShortageMaster` record by exact `Site` + `Component`, `isRemoved = 0`; text is `[Current Comments]` | **Qualified 10.3D.2:** `id` is identity; active candidate history had 2 active/8 removed rows; deterministic selection is `Modification Date DESC`, then `Added Record Date DESC`, then `id DESC`; multiline active comments observed. | No case-folding or alternate identifier transformation is established. Existing QAD Component Orders optional-display-text normalization does not transform component identity; a later reader must preserve that distinction. |

## 4. Reuse and non-reuse decisions

### Reuse

- Stage 9’s accepted conventional PO qualification: positive calculated open quantity and
  case-insensitive C/X exclusion.
- Stage 9’s independent effective KSS classification, but only as an indicator for rows already
  admitted by the conventional PO rule.
- Current QAD reader conventions: parameterized SQL, site-to-domain resolution, read-only access,
  compact projection, and deterministic ordering.
- Existing `Kst.Integrations.Qad` and `Kst.Integrations.Shortages` architectural boundaries.

### Do not reuse blindly

- The Stage 9 next-PO reader retrieves `TOP (1)` and only context fields. Component Orders needs
  all qualifying lines plus component, vendor, buyer, risk, and comments data.
- Stage 9’s `po_confirm` is a PO-master fact and must not replace the accepted Stage 10 line-level
  Confirmed field, `pod__log01`.
- KSTv1’s KSS-only query (`pod_sched` alone) is weaker than the accepted v2 KSS mapping.
- KSTv1’s historical comment selection lacks a documented newest-record ordering. Stage 10 must
  use the actual ShortageMaster latest-record field, not a lexical text fallback.
- KSTv1’s product-group and short-horizon filters are excluded; active-workspace scope and the
  accepted initial filter set govern this capability.

## 5. Active-workspace scope gate

“All components relative to the active workspace” is a product decision, but it still needs an
authoritative technical source. Before implementation, decide and validate whether the component
population is produced by:

1. **Accepted:** explode the active workspace MPS snapshot’s resolved parent parts through the
   current-effective multi-level BOM, then take distinct components.

Repository inspection confirms that the MPS snapshot retains the resolved parent scope, not a
component set. The current BOM reader can retrieve one parent’s complete current-effective
multi-level structure; it is lazy and parent-at-a-time, so it is not itself the Component Orders
population service. Stage 10 should create the smallest focused snapshot-aware component-scope
orchestration needed for this first real bulk use, rather than treating an unscoped site-wide PO
query as workspace scope or prematurely extracting a speculative general abstraction.

The resulting distinct component set is the input to the batch conventional-open-PO read. It must
not be replaced by an unscoped site-wide PO query, because that would display parts unrelated to
the active workspace.

## 6. Shortages-system integration gate

The repository has an intentional `Kst.Integrations.Shortages` boundary, but it is currently
unconfigured/disabled. Security records state that activation requires a dedicated account,
external-secret handling, read-only scoped permission verification, and connection/transport
verification. Stage 10 must complete that bounded integration/security preflight before it reads
ShortageMaster or PreferredSuppliers at runtime.

This gate does not prevent QAD-only source validation or Component Orders planning. It does prevent
declaring Credit Hold, CIA, or Current Comments implemented until the separate source is safely
connected and validated.

Static repository/legacy review found no additional authoritative ShortageMaster or
PreferredSuppliers schema record beyond the KSTv1 queries. The user confirms that a dedicated
app login exists, but no credential, connection, or runtime activation was inspected or attempted
in this discovery pass.

### Required read-only integration preflight

1. Establish the approved configuration and secret-injection path for the dedicated account; do not
   add credentials to source, appsettings, test data, logs, or planning artifacts.
2. Verify connection encryption/transport and the account's effective identity.
3. Verify only `CONNECT` and `SELECT` on the exact required tables/columns; no write, DDL, or broad
   role grant is accepted as part of this work.
4. Read the table schemas via bounded metadata inspection to identify:
   - ShortageMaster identity key, site, component, `isRemoved`, Current Comments, and the
     authoritative create/update/latest ordering column;
   - PreferredSuppliers identity/supplier key, supplier display field, RCHI, and CIA.
5. Run keyed sample reads for the Stage 10 validation packet and record returned values, data types,
   duplicates, null behavior, and duration.
6. Add parameterized readers only after the above source map and permission evidence are accepted;
   preserve the existing `Kst.Integrations.Shortages` boundary.

### Required OpenCode handoff placeholder

The Stage 10 OpenCode implementation prompt must include this conspicuous, local-only fill-in
section before any Shortages integration task. It is an operator input area, not repository
configuration and not a place to commit or paste a real secret into source control.

```text
SHORTAGES DATABASE CONNECTION — FILL LOCALLY BEFORE RUNNING

Server:   <ShortageMaster SQL Server host>
Database: <ShortageMaster database name>
Username: <dedicated app-login username>
Password / secret reference: <supply through the approved external-secret path; never commit>
```

The prompt must direct OpenCode to use the existing approved secret/configuration mechanism,
avoid logging connection strings or credentials, and stop if the required read-only access cannot
be verified.

## 7. Validation packet — read-only only

Prepare a small, owner-approved sample set and record results, not credentials, in the Stage 10
source-mapping evidence:

1. One conventional open line for every required table field.
2. A component with two or more open lines, including equal due-date lines.
3. A C line, an X line, a blank/open line, and a partially received line.
4. A line with no due date, if one exists.
5. A conventional open PO for an effective KSS component.
6. Buyer cases with site buyer, master fallback buyer, and no buyer code.
7. Supplier display/key cases, including one without a PreferredSuppliers match.
8. Truthy, false, blank/null, and absent Credit Hold/CIA values.
9. A component with a long multiline latest active Current Comments record and, if available,
   multiple active/historical ShortageMaster records to prove newest-record selection.

Each result must capture source identity, field values, expected display, query duration, and any
data-quality qualification. No broad scans and no production writes.

## 8. Accepted confirmation mapping

Component Orders `Confirmed` is the **line-level** confirmation fact:
`pod_det.pod__log01`. It is displayed as Yes/No. `po_mstr.po_confirm` describes an entire PO and
remains a distinct fact used by locked Stage 9 context; it is not a substitute for this column.

## 9. Stage 10 source-validation outcome (2026-09-11)

Full evidence: `KST_v2_STAGE_10_SOURCE_MAPPING.md`.

- **QAD mapping — QUALIFIED:** all QAD-backed fields validated against live `QADPRO2`/SW+KTC samples; two corrections (F2 vendor join key = `vd_mstr.vd_addr` + domain, display = `vd_sort`) and one owner-decision item (F1 buyer-lookup domain scoping contradicts "not site/domain scoped" on the domain dimension). NOT OBSERVED evidence limit: F3 (no KSS part with a conventional open PO in any site at validation date).
- **Shortages contracts — QUALIFIED, supplier-risk runtime correction pending UAT:** bounded external SQL-auth validation confirmed read access,
  schema, flag forms, supplier duplicates, active Current Comments history, deterministic latest
  ordering, and multiline active comments. The configured connection used `Encrypt=true` plus
  `TrustServerCertificate=true`; the latter is a certificate-trust exception, not a risk decision.
  The 10.3E bounded cross-source `po_vend → [Supplier Nbr]` result (18 of 20 KTC/SW candidates)
  remains historical evidence only. By owner direction in 10.3F.5, runtime risk enrichment now
  uses V1-compatible `SupplierDisplay`/`vd_sort → [Supplier Name]`; its normal live application
  UAT confirmation is pending because modern-ODBC validation was unavailable. QAD domain resolves
  the vendor display and is not a Shortages predicate. Neither result is a population-wide claim.
- Accepted owner decisions in this ledger were not rewritten; only validation statuses changed.

## 10. Accepted implementation reconciliation — 2026-09-14

The Stage 10.1 evidence above is retained as historical discovery/validation evidence. Stage 10 is
now **COMPLETE / ACCEPTED / LOCKED**; the closeout authority is
`KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md`.

- Owner live UAT succeeded for Credit Hold, CIA, and Current Comments. The owner-directed exact
  supplier-display-name risk lookup is the accepted runtime rule; the earlier Supplier-Nbr evidence
  is historical/superseded and has no runtime fallback.
- Buyer resolution is workspace-domain scoped with the accepted field discriminator and site-first,
  master-second buyer-code fallback.
- Current Comments remain read-only. No ShortageMaster write, local note persistence, or
  export-note update was authorized or delivered.
- Component Orders is informational only. PO coverage, projection, netting, and clear-date logic
  are intentionally deferred to Stage 11.
