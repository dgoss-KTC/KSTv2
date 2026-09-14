# KST v2 Stage 10.1 — Component Orders Source-Validation Evidence

**Checkpoint:** 10.1 (source-validation pass only)
**Status:** COMPLETE AS VALIDATION PASS — historical Stage 10.1 source evidence; accepted runtime
reconciliation and Stage 10 closeout are recorded in §8 and
`KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md` — 2026-09-14
**Scope:** Documentation-only. No production code, configuration binding, database objects, Stage 9 artifact, or dependency changed. Bounded Shortages validation was completed in external temporary harnesses; see §5.

## 1. Purpose and method

This document records the Stage 10.1 source-validation evidence for the Component Orders data
foundation: the accepted field mappings from `STAGE_10_FIELD_DISCOVERY_LEDGER.md` validated against
live QAD, plus the Shortages-database integration preflight result.

**Method (QAD):** Read-only live-QAD queries (SELECT-only) via a temporary .NET 10 console project
(`Microsoft.Data.SqlClient` 7.0.2 — the same package/version already used by `Kst.Integrations.Qad`,
Windows-integrated auth, `Encrypt=false`, `ApplicationName="KST v2 Stage 10.1 validation"`), matching
the accepted Stage 9.1/9.8 validation pattern. The project was created **outside** the repository at
`C:\Dev\kst_stage10_validation` and deleted after validation. All queries were parameterized,
bounded (TOP-limited or single-row aggregates), and executed against `QADPRO2` on `KNWVM13`.

**Sample set:** No explicit part list was supplied with the 10.1 prompt; samples were derived from
live data under the ledger §7 packet criteria, scoped to site **SW / domain KTC** — all five active
workspaces (Shure SMT, SHU Metals, SHU Molding, Taco, MSA/Neutronics) are SW-scoped per the local
workspace configuration. Derived samples: parts `236669`, `175329-1` (multi-line incl. equal-due-date
and partially received lines), `ICC-00994` (equal-due-date lines, 40 sharing one due date), `100190`
(partially received open line; site-buyer ≠ master-buyer case), `86089-007*AIR AUTH` (open line with
**no** due date and **no** part-master row).

## 2. Repository / architecture preflight (Part A)

### 2.1 Worktree state

Pre-existing uncommitted work was present at start and was **not touched**: modified
`src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs` and
`src/backend/tests/Kst.Integrations.Qad.Tests/Connection/QadConnectionStringFactoryTests.cs` (both show
as modified with **zero content diff** — stat-dirty entries under `core.autocrlf=true`, no actual
change), modified `src/frontend/tsconfig.tsbuildinfo` (real 1-line change), untracked `docs/api/` and
this ledger/mapping pair.

### 2.2 Existing patterns a later implementation must reuse

| Concern | Existing pattern | Location |
|---|---|---|
| Conventional open-PO qualification + ordering | `LOWER(ISNULL(pod_status,'')) NOT IN ('c','x') AND pod_qty_ord - pod_qty_rcvd > 0`; order by due date, PO number, line | `Kst.Integrations.Qad/Shortages/QadNextPurchaseOrderReader.cs` (Stage 9 context reader; TOP(1) shape is not reused — Component Orders needs all qualifying lines) |
| Independent effective KSS relationship | `pod_det` domain/site/part + `(pod_end_eff##1 IS NULL OR >= today)` + `po_mstr.po_sched = 1` + `(po_eff_to IS NULL OR >= today)` | `Kst.Integrations.Qad/Shortages/QadKssScheduleReader.cs` (Stage 9.8-corrected; indicator only) |
| Multi-level BOM expansion | `QadBomReader.ReadAsync(site, parentPart, effectiveDate)` — one parent at a time, current-effective structure, depth-first occurrences preserved | `Kst.Integrations.Qad/Bom/QadBomReader.cs` (Stage 8; lazy per-parent, not itself the Component Orders population service) |
| Workspace component scope | MPS snapshot retains resolved **parent** scope (bucket-level), not a component set — ledger §5. Stage 10 needs the smallest focused snapshot-aware orchestration: parents → BOM explode → distinct components → batch PO read | `Kst.Application` MPS snapshot stores (`SnapshotId`-keyed, in-memory) + `QadMpsSourceReader`/`QadMpsScopeResolver` |
| Site→domain resolution | `QadSiteDomainMap.Resolve(site)` (SW→KTC; KV→KTV; others KTC) — QAD boundary only | `Kst.Integrations.Qad/Mps/QadSiteDomainMap.cs` |
| Connection conventions | Windows-integrated auth, `Encrypt=false`, parameterized Dapper reads, command timeout from options | `QadConnectionFactory` / `QadConnectionStringFactory` / `QadConnectionOptions` |

### 2.3 Shortages configuration/options assessment (recorded, not changed)

- `appsettings.json`: `"ShortagesDatabase": { "Server": null, "Database": null }` — unconfigured;
  `DisabledShortagesConnectivityCheck` returns `NotConfigured` without network calls (intentional, per
  accepted security records).
- **Gap:** `ShortagesConnectionOptions` (`Kst.Integrations.Shortages/Options`) has **no username or
  password fields** — it is documented and built for Windows-integrated authentication only. It
  therefore **cannot represent the dedicated SQL-auth app-login account** required by the Stage 10
  Shortages integration gate, and no approved external-secret injection path exists in the repository
  today (configuration sources are `appsettings*.json` + process environment variables; nothing
  Shortages-specific). Representing a SQL-auth credential through any of these would be a
  security-relevant configuration change requiring its own bounded checkpoint under §8/§21 rules.

## 3. QAD source validation (Part B) — result: **QUALIFIED**

All queries read-only, parameterized, bounded; executed 2026-09-09 against `QADPRO2`/`KNWVM13`.
Representative durations: metadata probes <100 ms; sample discovery 77–3,548 ms; KSS cross-site check
2,383 ms; vendor match-rate aggregate 1,051 ms.

### 3.1 Field mapping table

| Display field | Source / join keys | Fallback / precedence | Transform / null behavior (evidence-backed) | Status |
|---|---|---|---|---|
| Comp | `pod_det.pod_part` (nvarchar(30)) within workspace component scope (§2.2) | — | Scope derivation is the Stage 10 orchestration item, not a source question | **CONFIRMED** (scope gate per ledger §5 unchanged) |
| Description | `pt_mstr.pt_desc1`, join domain + part | none observed | **Missing master is real:** 1,447 of 18,041 SW open lines (8%) have no `pt_mstr` row; sample `86089-007*AIR AUTH` has none. Display behavior for missing master = open product decision | **CONFIRMED WITH QUALIFIER** |
| Weeks LT | Site `ptp_det.ptp_pur_lead`, join domain+site+part; master `pt_mstr.pt_pur_lead` fallback, join domain+part | site row present → site value; else master | Both columns are **int** (nullable) — fractional values impossible by type. Zero is common: 86,454/137,057 SW site rows (63%); 126,871/232,269 master-only parts (55%). No NULLs observed in KTC/SW but columns are nullable → display rule for 0 and null needed (`ceil(days/7)` gives 0 weeks for zero). Sample: site≠master values exist (e.g. 63 vs 70) so fallback is meaningful | **CONFIRMED WITH QUALIFIER** (display rules for 0/null = open product decision) |
| PO | `pod_det.pod_nbr` (nvarchar(80), numeric strings e.g. `2076185`) | — | No normalization observed/needed in samples | **CONFIRMED** |
| Line | `pod_det.pod_line` (**int**) | — | Integer 1..n; no leading-zero/formatting issue possible by type | **CONFIRMED** |
| PO Due | `pod_det.pod_due_date` (datetime, nullable) | — | NULL due dates exist on qualifying open lines (sample `86089-007*AIR AUTH`) → yellow-exception-first sort case is real; date-only normalization as in Stage 9 readers (`DateOnly.FromDateTime`) | **CONFIRMED** |
| Open Quantity | `pod_qty_ord - pod_qty_rcvd` (decimal(28,10)) | — | Positive-open qualification re-validated; partially received lines observed (e.g. 1000 ordered / 600 received). UOM display policy open: `pod_det.pod_um` is nullable nvarchar(30); 718/18,041 SW open lines blank; only 62 lines mismatch component `pt_mstr.pt_um` (case-insensitive) | **CONFIRMED** (UOM display = open product decision) |
| Confirmed | `pod_det.pod__log01` (**bit**, nullable) — line-level, per accepted contract §8 of ledger | none; do **not** substitute `po_mstr.po_confirm` (distinct master fact, bit) | Open-line population: 1 → 14,979 rows / 0 → 3,063 rows; **no NULLs observed** in the qualifying open population. Yes/No display maps directly from bit | **CONFIRMED** |
| Supplier (display) | `po_mstr.po_vend` (nvarchar(80)) → `vd_mstr` join on **`vd_addr = po_vend AND vd_domain = <workspace domain>`**; display value `vd_sort` (nvarchar(30), vendor name) | — | See finding F2: the ledger's "display candidate `vd_sort`" is the **display value**, not the key. Match rate 506/506 (100%) for SW open-line vendors in KTC; `(vd_addr, vd_domain)` unique (0 duplicates); 0 blank `vd_sort` displays among 24,528 KTC vendor rows | **CONFIRMED** (join key corrected — F2) |
| Supplier (stable risk key) | `po_mstr.po_vend` → `vd_mstr.vd_addr` + QAD domain → `vd_sort` (`SupplierDisplay`) | Exact ordinal string equality with `dbo.PreferredSuppliers.[Supplier Name]`; no trim, case, numeric, padding, or cross-domain fallback | **Owner-directed 10.3F.5 V1-compatible runtime correction.** The prior `po_vend → [Supplier Nbr]` 18/20 bounded result is preserved as historical evidence but superseded for runtime use because live Component Orders produced no visible risk facts. Modern-ODBC bounded validation was unavailable on the workstation; normal application UAT must confirm this key. QAD domain resolves the vendor display only and is not a Shortages lookup predicate | **OWNER-DIRECTED — LIVE UAT PENDING** |
| Buyer | Code from site `ptp_det.ptp_buyer` (join domain+site+part), else master `pt_mstr.pt_buyer` (domain+part); resolved in `code_mstr` by **`code_fldname` discriminator** + `code_value`, display = `code_user1` | site code nonblank → site; else master; else blank | Discriminator values confirmed live: `ptp_buyer` (2,216 rows/4 domains) and `pt_buyer` (2,161/4) are the active tables; legacy `pt_mstr.pt_buyer` has 1 row. Fallback cases all real on KTC/SW: 103 site rows with blank buyer; 231,231 master-only parts with a buyer; **1,125 parts with no buyer anywhere** (blank display). `code_mstr` has **no site column** (not site-scoped — consistent) but **is domain-partitioned**: same code maps to different users per domain (F1) | **QUALIFIED — owner decision required (F1)** |
| Manufacturer Item | `pod_det.pod_vpart` (nvarchar(80)) | none | Populated in samples (`KING PACKAGING # JRZ01476`, `RENESAS ELECT# R5F2LA66ANFP#30`, …); blank for the no-master sample part. Blank → blank display | **CONFIRMED** |
| KSS (indicator) | Independent effective relationship per Stage 9: same domain/site/part; `(pod_end_eff##1 IS NULL OR >= today)`; `po_sched = 1`; `(po_eff_to IS NULL OR >= today)` — types re-verified: datetime / bit / datetime | applied only to rows already admitted by the conventional rule | **No part in any site/domain currently has both an effective KSS relationship and a qualifying conventional open PO** (cross-site check, 0 rows; context: 755 KSS parts on AR, 643 on SW). Packet item 5 = NOT OBSERVED at validation date; indicator logic itself remains validated by Stage 9 evidence | **CONFIRMED (mapping); sample NOT OBSERVED (F3)** |
| Tracking Info | `pod_det.pod__chr06` (nvarchar(80)) | none | Mostly blank on open lines; free text observed (`Split line#1`; case variants like `DHL`/`dhl` in history). Trim + empty→null matches existing reader convention; no other normalization rule accepted | **CONFIRMED** |
| Credit Hold | `dbo.PreferredSuppliers.[RCHI]`, exact `SupplierDisplay`/`vd_sort` = `[Supplier Name]` lookup | Duplicate precedence: `Date DESC`, then `ID DESC` | **Owner-directed 10.3F.5 V1-compatible runtime correction; live UAT pending.** `RCHI` is non-null `bit`; `true` is truthy while `false` and missing enrichment render blank. The superseded `po_vend → [Supplier Nbr]` bounded evidence remains historical only | **OWNER-DIRECTED — LIVE UAT PENDING** |
| CIA | `dbo.PreferredSuppliers.[CIA]`, exact `SupplierDisplay`/`vd_sort` = `[Supplier Name]` lookup | Duplicate precedence: `Date DESC`, then `ID DESC` | **Owner-directed 10.3F.5 V1-compatible runtime correction; live UAT pending.** `CIA` is non-null `bit`; `true` is truthy while `false` and missing enrichment render blank. The superseded `po_vend → [Supplier Nbr]` bounded evidence remains historical only | **OWNER-DIRECTED — LIVE UAT PENDING** |
| Current Comments | Latest active `dbo.ShortageMaster` record for `Site` + `Component`, with `isRemoved = 0` | `Modification Date DESC`, then `Added Record Date DESC`, then `id DESC` | Stable identity `id` (`int`); comment text `[Current Comments]` (`varchar(max)`, nullable). A compliant active-history sample contained two active and eight removed rows; all ordering fields were present and the first ordered row was active. Multiline active comments were observed | **QUALIFIED** |

### 3.2 Status-value evidence (no `po_stat` predicate introduced)

- SW `pod_status` census: `c` = 794,534; `x` = 136,304; blank = 24,892 — **only** lowercase c/x and
  blank observed on SW (Stage 9 evidence had uppercase `X` elsewhere → the case-insensitive C/X rule
  remains required). No other status values observed.
- `po_mstr.po_stat` was **blank on every sampled open line**, consistent with the Stage 9 deferred
  evidence item. No master-status predicate is introduced, per accepted behavior.

### 3.3 Data-quality observations (evidence, not rules)

- **F4 — Mixed-case key storage:** `pod_site` values for domain KTC include lowercase entries (`ch`,
  `nw`) alongside uppercase (`AR`, `MN`, `MS`, `SW`, `VT`). Existing readers work because the server
  collation is case-insensitive; application-side comparisons/dedup must not assume exact casing.
- **F5 — Missing part master:** 8% of SW open lines reference parts with no `pt_mstr` row (affects
  Description, master buyer fallback, and master lead-time fallback simultaneously).

## 4. Findings requiring owner decision or correction

| ID | Type | Statement | Recommended resolution (not adopted) |
|---|---|---|---|
| F1 | **Contradiction — owner decision required** | Accepted behavior states the buyer code lookup is "not site/domain scoped". Live evidence: `code_mstr` has no site column (site part consistent), but rows are partitioned by `code_domain`, and identical codes map to **different** users per domain (e.g. code 366 → `achavdar` in KTC vs `jharroun` in JZ/KTS/KTV; code 369 → `achavdar`/KTC, `bstrange`/JZ, `tshaw`/KTS+KTV). A lookup without a domain filter is ambiguous for real buyer codes. | Include `code_domain = <workspace QAD domain>` plus the source-field discriminator (`ptp_buyer` / `pt_buyer`) in the lookup; confirm with owner whether "not site/domain scoped" was intended to mean site-only. |
| F2 | Correction of legacy assumption | Vendor display join is **not** on `vd_sort`. In this deployment `vd_mstr.vd_sort` holds vendor *names*; the numeric vendor code from `po_vend` matches `vd_mstr.vd_addr`, disambiguated by `vd_domain`. 100% match rate on SW open lines; key unique per domain. | Adopt `vd_addr + vd_domain → vd_sort` as the display join; keep `po_vend` (+domain) as the stable supplier identifier for risk lookups. |
| F3 | Evidence limit (NOT OBSERVED) | No part in any site/domain has both an effective KSS relationship and a qualifying conventional open PO at validation date, so ledger packet item 5 could not be sampled live. The KSS indicator mapping itself is unchanged Stage 9-accepted evidence. | Re-sample opportunistically during implementation; no rule change implied. |
| F4 | Data-quality observation | Mixed-case `pod_site` storage (and by extension other QAD key fields) under CI collation. | Application-side normalization/case-insensitive comparison in the new readers; record as a convention note at implementation time. |
| F5 | Data-quality observation | 8% of SW open lines have no part-master row. | Owner decision on missing-master display (Description blank vs part-number fallback) before UI checkpoint. |

## 5. Shortages-database preflight and validation (Part C) — result: **QUALIFIED WITH CROSS-SOURCE LIMIT**

### 5.1 Connection and read access

The 2026-09-11 bounded validation used the local, ignored resource exclusively from disposable
external harnesses. The configured SQL-auth transport connected successfully with
`Encrypt=true` and `TrustServerCertificate=true`. The effective SQL login/database user was
verified in the first bounded pass; `dbo.ShortageMaster` and `dbo.PreferredSuppliers` both
accepted `SELECT TOP (0)` read-access probes. No write, DDL, transaction, stored procedure, or
QAD activity was attempted.

`TrustServerCertificate=true` is a current server-certificate trust exception: the preceding
attempt with certificate validation enabled reached the server but failed because its certificate
chain was not trusted. This pass records the configured transport fact; it does not assess or
accept the underlying certificate-trust risk.

### 5.2 Metadata, PreferredSuppliers, and cross-source key evidence

- `dbo.PreferredSuppliers`: `ID` is the non-null `bigint` primary key; `[Supplier Nbr]` is a
  non-null `nvarchar(80)` nonunique indexed lookup key; `[Supplier Name]` is non-null
  `nvarchar(30)`; `RCHI` and `CIA` are non-null `bit`; and `Date` is non-null `datetime`.
- A fresh bounded sample set observed duplicate supplier numbers, numeric and nonnumeric supplier
  key forms, and both true/false forms for each flag. For a duplicate supplier number, its two
  records had non-null dates. The deterministic proposed source ordering is `Date DESC, ID DESC`.
- **10.3E cross-source validation:** one compact QAD `TOP (20)` candidate query selected distinct,
  nonblank vendor identities from the accepted KTC/SW conventional-open-PO context. One parameterized
  Shortages query matched those in-memory candidates to `[Supplier Nbr]`, returning at most one latest
  row per candidate ordered by `Date DESC, ID DESC` and duplicate-presence evidence. Of 20 sampled
  QAD identities, 18 matched and 2 were missing. Every matched key was ordinally exact; no formatting
  edge case was observed. No matched key had duplicate Shortages rows in this bounded cross-source
  sample; the already-qualified Shortages-only duplicate rule remains `Date DESC, ID DESC`.
- QAD domain is source context only: all candidates were from one QAD domain, while
  `PreferredSuppliers` has no domain discriminator. It is not a Shortages lookup predicate and no
  cross-domain fallback exists. The two unmatched sample identities are valid no-enrichment results:
  Credit Hold and CIA render blank and the PO line remains included.

### 5.2.1 Owner-directed runtime supplier-risk correction (10.3F.5)

David Goss authorized the V1-compatible runtime key `po_vend → vd_addr + QAD domain → vd_sort`
(`SupplierDisplay`) → `PreferredSuppliers.[Supplier Name]`. The `po_vend → [Supplier Nbr]`
result above remains historical bounded evidence only and is superseded for runtime use. The local
workstation has neither Microsoft ODBC Driver 17 nor 18, so the prescribed modern-ODBC bounded
validation could not run. This is not independently source-qualified: the next normal Component
Orders application UAT must confirm visible RCHI/CIA enrichment using the corrected key.

### 5.3 Active Current Comments evidence (10.3D.2)

The final compliant ShortageMaster pass executed exactly three parameterized, compact `TOP (20)`
value-bearing queries against `dbo.ShortageMaster` and no other table:

1. Active candidate discovery restricted to `isRemoved = 0` with a present comment and returned
   only identity/key/order fields plus a non-content line-break indicator.
2. Parameterized full history for that one in-memory site/component candidate, ordered by
   `Modification Date DESC`, `Added Record Date DESC`, `id DESC`.
3. Active multiline-comment shape query with identity/order fields and a non-content indicator.

Aggregate findings: the selected candidate was active; its history returned 10 rows, of which two
were active and eight removed. All history rows had both ordering dates, and the first row under
the stated ordering was active. The history contained comment content, and the third query returned
20 active multiline-comment rows, all with the line-break indicator set. No selected key, comment,
or row payload was retained in the evidence.

`dbo.ShortageMaster` metadata: `id` is a non-null `int` primary key; `isRemoved` is a non-null
`bit`; `Component` is non-null `varchar(50)`; `Site` is non-null `varchar(2)`; `[Current Comments]`
is nullable `varchar(max)`; and `[Added Record Date]`/`[Modification Date]` are nullable
`datetime`. The identity and deterministic ordering provide a stable selection rule even where
records share timestamp values.

The match is exact equality on the site and component values passed to the Shortages reader. No
case-folding or alternate key transformation is established by this source evidence. Existing
QAD Component Orders normalization trims optional display text and maps empty optional text to
`null`; it does not transform component identity before source lookup. A later Shortages reader
must preserve this distinction and must not invent case-insensitive or cross-system normalization.

### 5.4 Superseded 10.1 preflight record

The following was the 10.1 preflight result and is retained as historical context. It is superseded
for connection/schema/sample facts by §§5.1–5.3:

1. **No credentials supplied.** The local-only fill-in section of the 10.1 prompt arrived with
   placeholders; no server/database/username/secret-reference was provided through any channel in
   this session.
2. **No approved secure path exists yet.** `ShortagesConnectionOptions` has no SQL-auth credential
   fields (Windows-integrated only, §2.3); the repository has no external-secret mechanism for a
   Shortages account; adding one is a security-relevant configuration change requiring its own
   bounded checkpoint (AGENTS.md §8, ledger §6 gate).
3. **Integration intentionally disabled.** `ShortagesDatabase` appsettings section is null and the
   connectivity check is the accepted `NotConfigured` placeholder — consistent with the current
   project state and security records.

Consequently the following ledger §6 preflight steps were **not** performed: transport/encryption
verification, effective-identity verification, CONNECT/SELECT-only grant verification on
`ShortageMaster`/`PreferredSuppliers`, bounded metadata inspection (identity keys, site/component
columns, `isRemoved`, Current Comments text column, authoritative newest-record ordering column;
supplier key/display/RCHI/CIA types and truthy forms), and keyed sample reads. No buyer-comment or
other Shortages content appears in this document because no Shortages data was read.

**Prerequisites still required before any Shortages-side implementation:**

1. Owner/IT supplies the dedicated app-login through an approved external-secret path (never into
   repository configuration, source, tests, logs, or planning artifacts).
2. A bounded security/configuration checkpoint extends `Kst.Integrations.Shortages` options +
   connection factory for SQL-auth with that secret path, and verifies transport/encryption per the
   ShortageMaster server's capabilities (do not assume QAD's `Encrypt=false` legacy constraint).
3. Read-only grant verification: exactly CONNECT + SELECT on the two required tables/columns; no
   write/DDL/broad-role grants accepted.
4. Bounded metadata inspection + keyed sample reads per ledger §6 steps 4–5 (latest-active-record
   determinism, key normalization/case-sensitivity, duplicate active records, RCHI/CIA data types and
   truthy/false/null forms, exact supplier join key vs `po_vend` format).

## 6. Unresolved items and proposed next actions

1. **F1 buyer-lookup domain scoping** — owner decision required before the Buyer mapping can be
   marked CONFIRMED (recommended: domain-scoped lookup with `code_fldname` discriminator).
2. **Bounded source-coverage limit** — exact QAD `po_vend` to Shortages `[Supplier Nbr]` equality
   is qualified for the 20-identity KTC/SW sample (18 matched, 2 missing), but this is not a
   population-wide match-rate claim. The missing-enrichment behavior is established; broader source
   coverage remains routine implementation validation rather than a new join rule.
3. **Open product decisions** (display rules, not source questions): missing-master Description
   behavior (F5); Weeks LT display for zero/null lead time; Open Quantity UOM display policy when
   `pod_um` is blank or differs from component UOM.
4. **Missing companion document:** the ledger references `STAGE_10_DISCUSSION_FRAMEWORK.md`, which is
   not present in the repository. Validation proceeded on the owner decisions recorded in the 10.1
   prompt and ledger §1/§8; the framework document should be added to the repository for durability.
5. **Data-map follow-up (implementation checkpoint, out of scope here):** `code_mstr` and `vd_mstr`
   are not yet represented in `docs/data/qadpro2-data-map.{md,json,yaml}`. This task did not modify
   the data map; adding both tables with the validated field facts above is a natural part of the
   first implementation checkpoint.

## 7. Change confirmation

- **No production code changed** (nothing under `src/` modified by this pass).
- **No configuration, dependency, or build artifact changed.**
- **No database state changed**: all database activity was SELECT-only; no writes, DDL, grants, or
  operational actions were attempted. The 10.3D.2 pass did not access QAD.
- Temporary validation harnesses were created outside the repository and deleted after use.
- Documentation changes: this file and `STAGE_10_FIELD_DISCOVERY_LEDGER.md` record the bounded
  Shortages evidence. No source/configuration/API/UI artifact changed.

## 8. Accepted runtime reconciliation — 2026-09-14

This addendum preserves the preceding 10.1 validation record, including its then-pending items. It
records the accepted implemented Stage 10 runtime behavior without rewriting that historical
evidence.

- Supplier risk uses the owner-directed KSTv1-compatible exact runtime key:
  `po_vend` → `vd_mstr.vd_addr` plus workspace QAD domain → `vd_sort` (`SupplierDisplay`) →
  `PreferredSuppliers.[Supplier Name]`. Duplicate precedence is `Date DESC, ID DESC`. Owner live
  UAT successfully confirmed Credit Hold and CIA in the running application. The earlier bounded
  `po_vend → [Supplier Nbr]` evidence remains historical/superseded, not a runtime fallback.
- Buyer display uses the site `ptp_buyer` then master `pt_buyer` fallback and a workspace-domain
  `code_mstr` lookup using the field discriminator (`ptp_buyer` / `pt_buyer`) and `code_user1`
  display value. This resolves the F1 domain ambiguity for the implemented runtime rule.
- Current Comments are read-only latest-active exact site/component enrichment; owner live UAT
  successfully confirmed Current Comments in the running application. No write, local persistence,
  or export-note behavior was delivered.
- Component Orders remains an informational open-PO surface. It does not calculate or assert PO
  supply coverage, reservations, netting, shortage clearance, projection, or a clear date.
- `PERF-001` is deferred: do not tune beyond the recorded 500-to-250 reader-local experiment until
  a normal shared QAD/MPS baseline and DBA-reviewed plan/index evidence are available.
