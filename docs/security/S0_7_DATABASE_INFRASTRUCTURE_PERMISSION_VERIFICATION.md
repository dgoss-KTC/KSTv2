# S0.7B — Database / Infrastructure Permission Verification

**Status:** LOCAL EVIDENCE COMPLETE / AWAITING IT/DBA EVIDENCE — 2026-08-28

**S0.3-G010:** Partially Verified / Awaiting Authoritative Infrastructure Evidence (IT/DBA)
**S0.7-F002:** QAD Read Scope Exceeds KST Application Need / Least-Privilege Gap / Needs Human Review (new; no severity assigned; NOT Accepted Risk; no remediation authorized in this pass)
**Companion to:** `docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` (S0.7A evidence)
**Canonical gap addressed:** `S0.3-G010` — Database-grant verification (server-side QAD login/group grants)

> S0.7B is a **bounded evidence pass** within the existing canonical S0.7 checkpoint. It is a
> working execution label, not a new permanent roadmap stage. This pass establishes, with
> read-only metadata evidence, what the **current runtime connection actually demonstrates** and
> what the **current database principal can observe about its own permissions**, and it separates
> those from what requires **database-administrator / IT-controlled evidence**. It does **not**
> prove read-only access by attempting a write, does **not** remediate any finding, and does
> **not** begin S0.8 or Stage 9.

---

## 1. Authority and Scope

**Governing authority (read before acting):**

- `AGENTS.md` (Tier 1 — enacted repository rules; §4 architecture boundaries, §7 database safety,
  §9 SQL/source-data rules, §18 uncertainty policy).
- `SECURITY.md` and `docs/security/SECURITY_ASSURANCE_POLICY.md` (Tier 1 — enacted security policy).
- `docs/security/SECURITY_BASELINE.md` §13.1 (QAD: Windows Integrated, read-only + least-privilege
  required, SQL auth prohibited), §13.3 (keytronicshortage: Operator/IT-Provided, 2026-08-24),
  §14 (read-only SQL source search — zero write-verb matches).
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11 — canonical `S0.3-G010` definition.
- `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md` §4.1 (keytronicshortage "separate,
  unchanged"; legacy QAD transport `Encrypt=false`).
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §9 (S0.7 scope; G010 assignment).
- `docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` (S0.7A evidence; §24 remaining S0.7B work).

**Canonical G010 requirement (S0.3 §11, line 379):**

> **S0.3-G010 — Database-grant verification:** actual QAD login/group grants are server-side; no
> repository tool can verify them and no live connection was authorized in S0.3. Operator/IT
> authority establishes the required posture; independent grant inspection remains outstanding.

G010's canonical scope is **QAD server-side login/group grants**. It is assigned to S0.7. This pass
addresses the portion of G010 that the **application principal** can observe (effective permissions)
and identifies the portion that requires **independent IT/DBA grant inspection** (the grant path).

**Scope of S0.7B (this pass):**

- Enumerate every database KST currently accesses (evidence matrix).
- Establish the application **permission-need** benchmark from actual code.
- Using the existing safe Windows-Integrated connection path, run **read-only, metadata-only**
  queries scoped to the current principal to establish QAD identity, authentication, transport,
  role membership, and effective permissions.
- Assess QAD **read-only** and **least-privilege** as two independent properties.
- Record the keytronicshortage surface state (current repository evidence).
- Produce a precise IT/DBA evidence request (read-only query packet) for the grant path.

**Out of scope (not performed):**

- No write testing (no INSERT/UPDATE/DELETE/MERGE/TRUNCATE/CREATE/ALTER/DROP/GRANT/DENY/REVOKE;
  no write-in-transaction-rollback; no stored-proc invocation to probe write capability).
- No remediation of any finding (including `S0.7-F001`).
- No S0.8 work; no Stage 9 work.
- No KST v2 installation; no KST v1 inspection; no installer execution.
- No change to `Encrypt`/`TrustServerCertificate`; no TLS migration.
- No new tool installation; no dependency/lockfile/source/config/credential change.

---

## 2. Starting Repository State

| Item | Value |
|---|---|
| Branch | `main` |
| HEAD | `eef676aff42a4602d1bf8dc657d8e1304ef29801` |
| origin/main | `eef676aff42a4602d1bf8dc657d8e1304ef29801` |
| HEAD == origin/main | Yes (matches expected baseline) |
| Working tree | Clean |
| Staged | Nothing |
| Last commit subject | `security: enforce loopback-only backend binding` |

Preflight (`git branch --show-current`, `git rev-parse HEAD`, `git rev-parse origin/main`,
`git log -8 --oneline`, `git status --short`, `git diff --name-status`, `git diff --cached`)
confirmed the expected clean baseline before any evidence collection. No pull/merge/rebase/reset/
stash/clean/discard/force was performed.

---

## 3. Database Surface Inventory

KST currently declares **two** database configuration surfaces. Only one is actually connected.

| # | Surface | Config section | Technology | Auth mode | Connection-string source | Credentials present? | Credentials external to repo? | Connected at runtime? | Production / system-of-record? |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **QAD** | `QadDatabase` (`appsettings.json`) | SQL Server (Microsoft.Data.SqlClient 6.1.1) | **Windows Integrated** (`IntegratedSecurity=true`) | `QadConnectionStringFactory` (built from `QadConnectionOptions`) | **No** (no User ID / Password) | n/a (none exist) | **Yes** | **Yes** — QAD is the authoritative operational system of record |
| 2 | **keytronicshortage** (Shortages) | `ShortagesDatabase` (`appsettings.json`) | SQL Server (declared boundary) | Declared boundary only (options class carries no credential fields) | **None** — no connection factory exists | **No** | n/a | **No** — integration is unconfigured/disabled | Not currently accessed |

**QAD surface detail (from `src/backend/Kst.Api/appsettings.json` and the QAD integration):**

- Server: `KNWVM13` (internal QAD SQL Server; value already committed in `appsettings.json`).
- Database: `QADPRO2` (actual `DB_NAME()` = `QADPro2`, `db_id` 87; case-insensitive match).
- `ConnectTimeoutSeconds` = 30.
- Connection parameters (`QadConnectionStringFactory`): `IntegratedSecurity=true`,
  `Encrypt=false`, `TrustServerCertificate=false`, `ApplicationName="KST v2"`. **No User ID /
  Password** — `QadConnectionOptions` has no credential fields.
- Operations present in application code: **SELECT only** (see §4). No write verbs.

**keytronicshortage surface detail (from `src/backend/Kst.Integrations.Shortages` and `Program.cs`):**

- `ShortagesDatabase.Server` = `null`, `ShortagesDatabase.Database` = `null` (not configured).
- `Program.cs` registers `DisabledShortagesConnectivityCheck`, which unconditionally returns
  `ShortagesConnectivityStatus.NotConfigured`.
- `ShortagesConnectionOptions` carries **no credential fields** (only `Server`, `Database`,
  `ConnectTimeoutSeconds`).
- **No connection factory, no SQL query, and no live connection** to keytronicshortage exists in
  the current code. The `Kst.Integrations.Shortages` project is a declared architectural boundary
  that has **not** been activated.

> **Discrepancy surfaced (not silently rewritten):** the task framing and the Operator/IT-Provided
> S0.2 baseline §13.3 describe keytronicshortage as using **SQL authentication with a dedicated KST
> application account and externally-stored credentials**. That is the **intended/future** posture.
> The **current repository state** is that the Shortages integration is **unconfigured and
> disabled** — there is no connection string, no connection factory, no credential field, and no
> live connection. S0.2 §13.3 itself records this: "Kst.Integrations.Shortages is currently
> unconfigured/disabled in the repository … so there is no current connection-string configuration
> to compare against." This pass therefore records keytronicshortage as **not currently connected**
> and does **not** invent runtime permission/transport evidence for a database KST does not access.

---

## 4. Application Permission-Need Matrix (benchmark)

Derived from actual code (`src/backend/Kst.Integrations.Qad`). A targeted source search across the
QAD integration for write verbs
(`INSERT INTO | UPDATE … SET | DELETE FROM | MERGE INTO | TRUNCATE | CREATE | ALTER | DROP |
EXEC/EXECUTE | GRANT | DENY | REVOKE`) returned **zero matches**. All QAD SQL is `SELECT`.

**QAD — objects actually referenced (all `qadpro2.dbo.*`):**

| Reader | Tables referenced |
|---|---|
| `QadPartInventoryReader` | `ld_det`, `loc_mstr`, `is_mstr` |
| `QadApprovedVendorReader` | `pt_mstr`, `vp_mstr`, `ad_mstr` |
| `QadWorkOrderMaterialReader` | `wo_mstr`, `wod_det`, `pt_mstr` |
| `QadWorkOrderSummaryReader` | `wo_mstr`, `wod_det` |
| `QadPartDetailReader` | `pt_mstr`, `ptp_det`, `pi_mstr`, `pid_det` |
| `QadComponentSourceReader` | `pt_mstr`, `ptp_det`, `sct_det` |
| `QadBomReader` | `ps_mstr`, `pt_mstr`, `ptp_det` |
| `QadMpsScopeResolver` | `pt_mstr`, `mrp_det` |
| `QadMpsSourceReader` | `mrp_det`, `wo_mstr`, `pt_mstr` |

**Distinct QAD objects KST reads (14):** `pt_mstr`, `ptp_det`, `ld_det`, `loc_mstr`, `is_mstr`,
`pi_mstr`, `pid_det`, `wo_mstr`, `wod_det`, `mrp_det`, `sct_det`, `vp_mstr`, `ad_mstr`, `ps_mstr`.

| Database | Purpose | Auth mode (declared) | Objects used | Operations used | Minimum apparent permission need |
|---|---|---|---|---|---|
| **QAD** | Read scheduling/inventory/work-order/BOM/MPS source data from the system of record | Windows Integrated | 14 `qadpro2.dbo` tables (above) | SELECT only | **SELECT** on those 14 tables (read-only). No write/DDL/EXECUTE needed. |
| **keytronicshortage** | (Intended) shortage-system data | (Intended) SQL auth, dedicated account | **None currently** (integration disabled) | **None currently** | **Not determinable from current code** — no connection exists. The intended CRUD/EXECUTE need must be derived when the integration is implemented; it is **not** assumed read-only. |

**Read-only vs least-privilege (QAD) — the benchmark distinction:**

- **Read-only** = the principal lacks production QAD mutation/admin authority.
- **Least privilege** = read authority is constrained to the objects/data KST actually needs (the
  14 tables above). A database-wide read role satisfies *read-only* but **not** *least privilege*.
  These are evaluated independently in §8 and §9.

---

## 5. QAD Authentication Evidence

Established via the existing safe connection path (a temporary read-only probe using the **same**
`Microsoft.Data.SqlClient` 6.1.1 and the **same** connection parameters as KST: Windows Integrated,
`Encrypt=false`, `TrustServerCertificate=false`, `ApplicationName="KST v2"`; no credentials).

| Property | Result | Evidence |
|---|---|---|
| Connection succeeds via Windows-Integrated path | **Verified** | `CONNECTED OK` to `KNWVM13`/`QADPRO2` using `IntegratedSecurity=true`, no User ID/Password. |
| Authentication is Windows-based (not SQL) | **Verified** | `SUSER_SNAME()` / `ORIGINAL_LOGIN()` / `USER_NAME()` all return a **Windows domain identity** (operator's Windows account; exact name withheld from this repository document — see §15). No SQL login is presented. |
| No username/password supplied by KST | **Verified** | `QadConnectionOptions` has no credential fields; `QadConnectionStringFactory` sets only `IntegratedSecurity=true`. Connection string contains no `User ID`/`Password`. |
| Current database is the expected QAD database | **Verified** | `DB_NAME()` = `QADPro2` (`db_id` 87); `ORIGINAL_DB_NAME()` = `QADPRO2`. |
| Application name | **Verified** | `APP_NAME()` = `KST v2`. |
| Specific authentication scheme (Kerberos vs NTLM) | **Unable to Verify from the application principal** | `sys.dm_exec_connections` requires `VIEW SERVER STATE`, which the principal does **not** hold (query denied). The scheme is not observable from the principal. (Windows authentication itself is established by the domain-identity result above; the Kerberos/NTLM distinction is not prescribed by repository policy and is not asserted.) |

**Authentication conclusion:** QAD access is **Windows Integrated** with **no SQL credential path**.
The specific scheme (Kerberos/NTLM) is Unable to Verify from the application principal and is not
required to be asserted by repository policy.

---

## 6. QAD Runtime Transport Evidence

| Property | Result | Evidence |
|---|---|---|
| Client-requested transport encryption | **`Encrypt=false`** (unencrypted requested) | `QadConnectionOptions.Encrypt` default `false`; `appsettings.json` does not override it; `QadConnectionStringFactory` sets `Encrypt=false`. This is the accepted legacy QAD transport requirement (S0.4A). |
| `TrustServerCertificate` | **`false`** | `QadConnectionOptions.TrustServerCertificate` default `false`; not overridden. (Not used as an encryption substitute, per S0.2-F003.) |
| Server-reported `encrypt_option` for the live session | **Unable to Verify from the application principal** | `sys.dm_exec_connections.encrypt_option` requires `VIEW SERVER STATE` (denied to the principal). The live server-side encryption state is not observable from the principal. |
| Transport topology (same-host / loopback / LAN) | **Unable to Verify from the application principal** | `sys.dm_exec_connections.net_transport` / `client_net_address` / `local_net_address` require `VIEW SERVER STATE` (denied). Not inferred from the connection-string name. |

**Transport conclusion:** The client **requests unencrypted** SQL transport (`Encrypt=false`),
consistent with the accepted legacy QAD infrastructure constraint. The **server-reported** live
encryption state and the network topology are **Unable to Verify from the current principal** and
are preserved as such (not inferred, not turned into evidence).

> **Legacy-transport organizational boundary (carried forward, not resolved here):** the formal
> organizational acceptance/disposition of the legacy **unencrypted** QAD SQL transport remains an
> **IT/security decision outside engineering authority** (S0.2-F003 / S0.4A). This pass establishes
> what the runtime requests/does; it does **not** invent formal company acceptance of unencrypted
> transport, and it does **not** reclassify the known legacy transport as newly accepted risk. That
> boundary is carried to S0.8 closeout.

---

## 7. QAD Effective Permissions (current principal)

All results below are **effective permissions as observed by the current principal** via
read-only metadata functions (`IS_SRVROLEMEMBER`, `IS_ROLEMEMBER`, `sys.fn_my_permissions`). They
establish what the session **can do**; they do **not** by themselves establish the administrative
**grant path** (see §10).

**SQL Server environment:** Microsoft SQL Server 2016 (SP2-GDR) (KB4583460) — 13.0.5108.50
(X64), Standard Edition, on Windows Server 2019.

**Server-role membership (`IS_SRVROLEMEMBER`):**

| sysadmin | securityadmin | serveradmin | setupadmin | processadmin | diskadmin | dbcreator | bulkadmin | public |
|---|---|---|---|---|---|---|---|---|
| 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | **1** |

→ The principal is a member of **`public` only**. **No server-level administrative role.**

**Database-role membership in QADPRO2 (`IS_ROLEMEMBER`):**

| db_owner | db_securityadmin | db_accessadmin | db_datareader | db_datawriter | db_ddladmin | db_backupoperator | db_denydatareader | db_denydatawriter | db_public |
|---|---|---|---|---|---|---|---|---|---|
| 0 | 0 | 0 | **1** | 0 | 0 | 0 | 0 | 0 | 0 |

→ The principal is a member of **`db_datareader` only** (the built-in read-only role). **No
`db_owner`, `db_datawriter`, `db_ddladmin`, `db_securityadmin`, or other write/DDL/admin role.**

**Effective DATABASE-level permissions (`sys.fn_my_permissions(NULL,'DATABASE')`):**

- `CONNECT`
- `SELECT`
- `VIEW ANY COLUMN ENCRYPTION KEY DEFINITION`
- `VIEW ANY COLUMN MASTER KEY DEFINITION`

→ No `INSERT`/`UPDATE`/`DELETE`/`ALTER`/`CREATE`/`CONTROL`/`TAKE OWNERSHIP`/`IMPERSONATE`/
`REFERENCES`/broad `EXECUTE` at the database level. The two `VIEW ANY COLUMN … KEY DEFINITION`
entries are read-only (Always Encrypted metadata) and are **not** required by KST (KST does not use
column-encryption keys); they are noted as part of the broader-than-needed read surface (§9).

**Effective OBJECT-level permissions on each of the 14 QAD tables KST reads
(`sys.fn_my_permissions('dbo.<table>','OBJECT')`):**

| Table | Effective object permission |
|---|---|
| `pt_mstr`, `ptp_det`, `ld_det`, `loc_mstr`, `is_mstr`, `pi_mstr`, `pid_det`, `wo_mstr`, `wod_det`, `mrp_det`, `sct_det`, `vp_mstr`, `ad_mstr`, `ps_mstr` | **`SELECT` only** (table + all columns) |

→ **No** `INSERT`/`UPDATE`/`DELETE`/`REFERENCES`/`ALTER`/`CONTROL` on any KST object.

**Write/admin authority present or absent:** **ABSENT.** The effective permission set contains no
mutation, DDL, control, ownership, or impersonation capability at the server, database, or object
level.

**Evidence limitations:** these are the principal's **self-observed effective permissions**. They
strongly support read-only but do not, by themselves, establish the administrative grant
configuration (login mapping, AD group contributions, explicit GRANT/DENY, role-assignment
authority) — that is the IT/DBA boundary (§10).

---

## 8. QAD Read-Only Assessment (Property A)

**Question:** Does available evidence show that the KST/QAD principal **lacks** production QAD
mutation/admin authority?

**Answer: YES — read-only is verified (metadata).**

- Server roles: `public` only — no `sysadmin`/`securityadmin`/`serveradmin`/etc.
- Database roles: `db_datareader` only — no `db_owner`/`db_datawriter`/`db_ddladmin`/`db_securityadmin`.
- Effective database-level permissions: `CONNECT`, `SELECT`, and two read-only `VIEW ANY COLUMN …
  KEY DEFINITION` — no write/DDL/control/ownership/impersonate.
- Effective object-level permissions on all 14 KST tables: `SELECT` only.

This is established by **permission metadata only** — no write was attempted (per the absolute
no-write-testing boundary). The S0.5 `QadReadOnlySqlTests` independently prove that
**application-emitted** SQL is read-only; together, application-side (emitted SQL is SELECT) and
server-side (principal lacks write authority) evidence both support read-only.

---

## 9. QAD Least-Privilege Assessment (Property B)

**Question:** Are read permissions constrained to the objects/data KST actually needs, or does the
identity possess materially broader access?

**Answer: NO — the read scope is materially broader than application need.**

Evidence:

- The principal is a member of **`db_datareader`**, which confers `SELECT` on **all** user tables in
  QADPRO2, not just the 14 KST reads.
- QADPRO2 contains **1810 user tables** (`SELECT COUNT(*) FROM sys.tables`).
- Breadth check on tables KST does **not** query: the principal has effective `SELECT` on
  **`po_mstr`** (purchase-order master) and **`so_mstr`** (sales-order master) — neither is read by
  KST. (`cust_mstr`, `inv_mstr`, `gl_mstr` returned no rows because those objects do not exist in
  this QAD database — confirmed via `sys.tables`, which lists only `po_mstr` and `so_mstr` of the
  five probed.)

**Confirmed least-privilege gap — recorded as finding `S0.7-F002` (§16):**
the QAD principal is **read-only** (Property A satisfied) but **not least-privilege** (Property B
not satisfied): it holds database-wide `SELECT` (via `db_datareader`) over the full QADPRO2 table
set (~1810 tables) plus two read-only column-encryption-key view permissions, whereas KST requires
`SELECT` on 14 tables. Because enacted KST security policy requires **both** read-only **and**
least privilege (S0.2 13.1), this is a **confirmed least-privilege mismatch**, not merely an
Unable-to-Verify distinction. It is recorded as finding **`S0.7-F002`** (§16). The administrative
grant path and the organizational rationale for the database-wide read grant remain **Awaiting
IT/DBA Evidence** (§10). No severity is assigned (enacted policy provides none for this finding
class), it is **NOT** Accepted Risk, and **no permission remediation is authorized in this pass**.

---

## 10. QAD Grant-Path / IT Evidence (G010 boundary)

**What the current-session evidence establishes (effective permissions):**

- The principal is Windows-Integrated, `db_datareader`-only, no server admin role, `SELECT`-only on
  all KST objects, no write/DDL/admin authority.

**What the current-session evidence does NOT establish (grant path / administrative configuration):**

- The **SQL login / Windows login mapping** for the KST runtime identity (how the Windows domain
  identity maps to a SQL Server principal).
- The **AD/Windows (and nested) group memberships** that contribute SQL permissions to the
  principal.
- The **authority that assigned** the `db_datareader` database-role membership and the two
  `VIEW ANY COLUMN … KEY DEFINITION` permissions (direct grant vs. group vs. role).
- The complete set of **explicit GRANT / DENY** entries for the principal.
- Confirmation that the **database-wide** `SELECT` scope is the intended, governed configuration
  (vs. object-scoped least privilege).

**Conclusion:** G010 is about **actual server-side grants**. The application principal's
self-observation strongly supports the read-only result but is **insufficient** to prove the
administrative grant configuration and group mappings that G010 requires. **Independent IT/DBA
grant inspection is required** before G010 can be `Covered / Resolved`. This is the expected human
evidence boundary, not a task failure.

**IT/DBA evidence request (precise; read-only; no passwords; no secret connection strings):**

For the **QAD** surface (`QADPRO2` on the internal QAD SQL Server), IT/DBA is asked to confirm, for
the **KST runtime Windows identity** (exact name provided to the owner out-of-band for reconciliation):

1. The actual **login / Windows-group grant path**: the SQL Server **login / Windows login
   mapping** for that identity (server principal type, SID, disabled state) and the AD/Windows
   (nested) groups that contribute SQL permissions to it.
2. **Server-role membership** (expected: `public` only).
3. **Database-role membership** in QADPRO2 (expected: `db_datareader` only).
4. All **explicit GRANT / DENY** entries for that identity (database- and object-level).
5. **Confirmation that production QAD mutation / DDL / control / ownership authority is absent**
   for that identity.
6. **Why `db_datareader` / database-wide `SELECT` is currently assigned** to this identity — the
   organizational / operational rationale for the broad read grant (finding `S0.7-F002`).
7. **Whether object/schema-scoped `SELECT` for KST's 14 required tables is feasible**, or whether
   an infrastructure constraint requires the broader (database-wide) read access.
8. **The two `VIEW ANY COLUMN … KEY DEFINITION` permissions and their source** — which grant, role,
   or group confers them, and whether they are intentional.

A **read-only SQL Server verification query packet** for IT to run is provided in **Appendix A**.
It contains SELECT / metadata inspection only and does **not** ask IT to prove read-only by
attempting a write. **IT is not asked to change any permission in this pass** — the request is for
evidence only. The AD-group mapping (item 1) additionally requires **AD-side** evidence outside the
SQL packet.

---

## 11. keytronicshortage Authentication Evidence

**Current repository state: the Shortages integration is unconfigured and disabled; KST makes no
connection to keytronicshortage.**

- `ShortagesDatabase.Server` / `.Database` = `null` (not configured).
- `DisabledShortagesConnectivityCheck` is the registered check (returns `NotConfigured`).
- `ShortagesConnectionOptions` has **no credential fields**; there is **no connection factory** and
  **no SQL query** for the Shortages surface.
- Therefore: **no current SQL authentication mode is in effect, no dedicated application identity is
  currently used, and no connection secret exists in the repository** for this surface.

**Operator/IT-Provided expectation (S0.2 §13.3, 2026-08-24) — intended/future posture, NOT current:**

- SQL authentication (distinct from QAD's Windows Integrated); a dedicated KST application account;
  credentials in external configuration / secret storage (not committed); explicitly scoped
  application permissions. S0.2 §13.3 explicitly notes the integration is currently
  unconfigured/disabled, so there is no current connection-string configuration to compare against.

**Secret-exposure check:** no keytronicshortage credential or connection string is present in
repository content (none exists to be exposed). No STOP condition triggered.

**Disposition:** keytronicshortage authentication is **Not Applicable / Unable to Verify at runtime**
in this pass because the integration is not connected. The intended SQL-auth / dedicated-account /
external-secret posture is recorded as the future requirement to be verified **when the integration
is implemented and connected**, not now.

---

## 12. keytronicshortage Transport / Topology

**Current repository state: no connection exists, so no transport or topology is observable.**

- The previously-noted "same-host" assumption for keytronicshortage is **NOT established** by any
  current repository or runtime evidence. S0.2 §13.3 records that the hosting relationship to QAD's
  server is **not established**.
- This pass does **not** infer a topology from a connection-string name (there is no connection
  string). The "same-host" question is preserved as **Unable to Verify** until the integration is
  connected and/or IT provides hosting evidence.

---

## 13. keytronicshortage Effective Permissions

**Current repository state: no connection exists, so no effective permissions are observable.**

- No current application principal, role membership, or object permission can be queried for this
  surface because KST does not connect to it.
- The correct future benchmark is **not** "read-only" but **"no more authority than the actual KST
  application requires"** — to be derived from the implemented integration's actual CRUD/EXECUTE
  need when it exists. The QAD read-only requirement is **not** applied to this database (per the
  governing boundary), because the current application requirements do not establish it as
  read-only.
- **Disposition:** Unable to Verify at runtime in this pass (integration not connected). No
  over-privilege finding is made for a surface KST does not access.

---

## 14. Code Need vs Actual Authority Matrix

| Database | Required by application | Observed current effective permissions | Authoritative grant evidence available? | Excess authority? | Insufficient authority? | Verification status |
|---|---|---|---|---|---|---|
| **QAD** | `SELECT` on 14 `qadpro2.dbo` tables (read-only) | `db_datareader` (db-wide `SELECT`, ~1810 tables); `SELECT` on all 14 KST tables; `CONNECT`; two read-only `VIEW ANY COLUMN … KEY DEFINITION`; no write/DDL/admin; server role `public` only | **No** — grant path (login mapping, AD groups, role-assignment authority, explicit GRANT/DENY) requires IT/DBA | **Yes (read scope)** — db-wide `SELECT` + 2 column-key view perms exceed the 14-table need; **no** write/admin excess | **No** — all 14 needed tables are readable | Read-only **Verified**; least-privilege **Not met** (`S0.7-F002`); grant path **Awaiting IT/DBA** |
| **keytronicshortage** | Not determinable (integration disabled; no current need) | **None observable** (no connection) | **No** (no connection) | n/a | n/a | **Unable to Verify** (not connected); intended posture = SQL auth + dedicated account + external secret (future) |

**QAD explicit answers:**

- Windows Integrated auth verified? **Yes.**
- SQL credentials absent? **Yes.**
- Read-only effective permission verified? **Yes** (metadata; no write attempted).
- Least-privilege scope verified? **No** — db-wide `SELECT` exceeds the 14-table need (**confirmed least-privilege gap — finding `S0.7-F002`**, §9/§16).
- Server/admin privilege absent? **Yes** (server role `public` only; no `db_owner`/`db_ddladmin`/etc.).
- Grant path sufficiently established? **No** — requires IT/DBA evidence (§10).
- Transport state observed? **Client requests `Encrypt=false` (verified from config/code); server-reported live `encrypt_option` and topology Unable to Verify from the principal.**

**keytronicshortage explicit answers:**

- Dedicated SQL principal verified? **No** — not connected (intended, not current).
- Credential source acceptable? **n/a** — no credential exists (none in repo).
- Permission scope matches application need? **Unable to Verify** — no connection, no current need.
- Unnecessary admin authority absent? **Unable to Verify** — no connection.
- Transport/topology established? **No** — "same-host" not established; Unable to Verify.

---

## 15. Sensitive-Evidence Handling

**Withheld from this repository document (operationally sensitive; available to the owner
out-of-band for IT reconciliation):**

- The exact **Windows domain identity** of the current QAD principal (the operator's Windows
  account). Recorded here only as "current Windows Integrated principal (operator's Windows domain
  identity)."
- The **client workstation** host name.
- Any **network addresses** (client/server IP) — not captured into the repository; the
  `sys.dm_exec_connections` address columns were not readable by the principal anyway.

**Recorded (not sensitive / already in repository):**

- QAD server name `KNWVM13` and database `QADPRO2`/`QADPro2` — already committed in
  `appsettings.json`; referenced for traceability.
- SQL Server version (2016 SP2, Standard Edition) — environment context, not a secret.
- QAD table names — already present in application source.

**Never recorded:** passwords, connection-string secrets, tokens. None were encountered. The
connection used Windows Integrated authentication with no credential, so no secret was handled.

**Raw-evidence handling:** the temporary read-only probe (source + output) was a disposable local
artifact outside the repository and is removed after this pass (§33 cleanup). No screenshots, SQL
permission dumps, AD group dumps, login listings, server names beyond the already-committed value,
usernames, or internal network addresses are committed. Durable repository evidence (this document)
summarizes what was reviewed, the authority class (current application principal via read-only
metadata), the date, the properties established, and what remains unresolved.

---

## 16. Findings

**One new S0.7 finding is created in this pass: `S0.7-F002`** (the confirmed QAD least-privilege
gap). The finding namespace `S0.7-Fxxx` was verified; `S0.7-F001` is the only prior ID, so
`S0.7-F002` is the next genuine ID (no reuse).

### S0.7-F002 — QAD Read Scope Exceeds KST Application Need / Least-Privilege Gap / Needs Human Review

- **Effective access is read-only** (verified, §8): the principal has no write/DDL/admin/ownership/
  impersonate authority at the server, database, or object level.
- **Effective `SELECT` scope is materially broader than KST's current 14-table need** (verified, §9):
  the principal is a `db_datareader` member with database-wide `SELECT` over ~1810 QADPRO2 user
  tables, plus `SELECT` on QAD tables KST does not use (`po_mstr`, `so_mstr`) and two read-only
  `VIEW ANY COLUMN … KEY DEFINITION` permissions.
- **Administrative grant path and organizational rationale remain Awaiting IT/DBA Evidence** (§10):
  why `db_datareader` / database-wide `SELECT` is assigned, whether object/schema-scoped `SELECT`
  for the 14 required tables is feasible, and the source of the two column-key view permissions are
  all open IT/DBA questions.
- **No severity is assigned** unless enacted policy provides one (enacted policy provides none for
  this finding class).
- **This is NOT Accepted Risk.**
- **No permission remediation is authorized in this pass** (the IT/DBA request is evidence-only;
  IT is not asked to change any permission yet).

**Other items (not new findings):**

- The existing finding **`S0.7-F001`** (Operational / Package-Identity Coexistence Issue) is
  **unchanged and remains Deferred for packaging/deployment decision / Non-blocking**. It is not
  remediated in this pass.
- **keytronicshortage disabled state (documentation clarification, not a finding):** the Shortages
  integration is unconfigured/disabled; the Operator/IT-Provided SQL-auth/dedicated-account posture
  is intended/future, not current. This is recorded to prevent the prompt's expectation from being
  mistaken for current runtime state. It is not a security finding.

No `Unable to Verify` item is converted into a vulnerability. The new finding `S0.7-F002` is a
confirmed least-privilege mismatch (enacted policy requires both read-only and least privilege),
not an invented severity and not a manufactured finding.

---

## 17. G010 Disposition

**`S0.3-G010` — Partially Verified / Awaiting Authoritative Infrastructure Evidence (IT/DBA).**

G010 may become `Covered / Resolved` only when sufficient evidence establishes the **actual
database/server grant state** for the KST identity within G010's canonical scope. For QAD this
requires, at minimum: the expected Windows-Integrated identity path (✓ established), no SQL
credential path (✓ established), production write/admin authority absent (✓ established from the
principal's effective permissions), read-permission scope understood (✓ established as db-wide via
`db_datareader`), and **grant/role configuration sufficiently established by authoritative
evidence (✗ — requires IT/DBA)**.

Because the **grant path** (login mapping, AD group contributions, role-assignment authority,
explicit GRANT/DENY, and confirmation that the db-wide read scope is the intended governed
configuration) is **not** established by the application principal's self-observation, G010 is
**not** resolved in this pass. It is **Partially Verified / Awaiting Authoritative Infrastructure
Evidence**. G010 is **not** resolved solely because all application SQL is SELECT, solely because
no write was observed, or solely because the principal reports one role.

The new finding **`S0.7-F002`** (QAD least-privilege gap, §16) **does not itself resolve G010**:
G010 remains **Partially Verified / Awaiting Authoritative Infrastructure Evidence** until the
grant path is established by IT/DBA. `S0.7-F002` records the confirmed least-privilege mismatch;
G010 tracks the grant-path verification.

---

## 18. Unable-to-Verify

| Item | Why Unable to Verify | How it could be resolved |
|---|---|---|
| QAD authentication scheme (Kerberos vs NTLM) | `sys.dm_exec_connections` requires `VIEW SERVER STATE` (denied to the principal) | IT/DBA or a privileged read of the connection metadata; not required by repository policy |
| QAD server-reported live `encrypt_option` | Same `VIEW SERVER STATE` limitation | IT/DBA; or a future infrastructure change to encrypted transport |
| QAD transport topology (same-host / loopback / LAN) | Same `VIEW SERVER STATE` limitation; not inferred from config | IT/DBA network evidence |
| QAD grant path (login mapping, AD groups, role-assignment authority, explicit GRANT/DENY) | Self-observation shows effective permissions, not administrative grant configuration | IT/DBA grant inspection (Appendix A packet + AD-side evidence) |
| keytronicshortage authentication / permissions / transport / topology | Integration is unconfigured/disabled; no connection exists | Verify when the integration is implemented and connected; IT hosting evidence for topology |

---

## 19. Organizational / IT Dependencies

1. **IT/DBA grant inspection for QAD (closes the G010 grant-path half):** the evidence request and
   read-only query packet in §10 / Appendix A. IT/DBA owns the access/authorization; S0.7
   coordinates the inspection.
2. **Formal IT/security disposition of the legacy unencrypted QAD SQL transport** (behind
   S0.2-F003 / S0.4A): an organizational risk/exception decision **outside engineering authority**.
   This pass does **not** invent acceptance and does **not** reclassify the known legacy transport
   as newly accepted risk. Carried to **S0.8 closeout**.
3. **keytronicshortage activation (future):** when the Shortages integration is implemented, its
   SQL-auth / dedicated-account / external-secret / scoped-permission posture (S0.2 §13.3) must be
   verified against the live connection, and its hosting/topology established. Not S0.7B work now.

No organizational approval is asserted anywhere in this document. No "IT approved / Security
accepted / DBA approved" statement is made, because no such decision is evidenced.

---

## 20. Remaining S0.7 Boundaries

- **S0.7B:** local/runtime permission evidence is **complete**; **awaiting IT/DBA grant evidence**
  to close the G010 grant-path half. S0.7B is **not** marked complete.
- **Installed Windows-package behavior:** remains **Unable to Verify** (no installation performed;
  the owner's production workstation is not an installer experiment). The installed-form half of
  G009 and the `S0.7-F001` package-identity boundary are dispositioned only if the owner later
  authorizes a safe installation environment.
- **`S0.7-F001`:** unchanged / Deferred / Non-blocking.
- **S0.8:** **NOT STARTED** (organizational surfaces, including the legacy-transport disposition,
  are S0.8 work).
- **Stage 9:** **BLOCKED PENDING S0 CLOSEOUT** (not started).

---

## 21. Conclusion

This S0.7B pass, using only existing permitted tooling and the existing safe Windows-Integrated
connection path, established with **read-only metadata evidence** (no write attempted, no DDL, no
permission change, no tool installation, no source/config/dependency change):

- **Database surfaces:** QAD (connected, Windows Integrated, SELECT-only, 14 tables) and
  keytronicshortage (unconfigured/disabled, not connected).
- **QAD authentication:** Windows Integrated verified; no SQL credential path; specific scheme
  (Kerberos/NTLM) Unable to Verify from the principal.
- **QAD transport:** client requests `Encrypt=false` (legacy constraint); server-reported live
  encryption state and topology Unable to Verify from the principal; legacy-transport
  organizational acceptance remains unresolved (carried to S0.8).
- **QAD effective permissions:** server role `public` only; database role `db_datareader` only;
  `SELECT`-only on all 14 KST tables; **no write/DDL/admin/ownership/impersonate authority**.
- **QAD read-only:** **Verified** (metadata). **QAD least-privilege:** **Not met** — db-wide
  `SELECT` (~1810 tables) + two read-only column-key view permissions exceed the 14-table need
  (**confirmed least-privilege gap — finding `S0.7-F002`**; no severity assigned; NOT Accepted
  Risk; no remediation authorized in this pass).
- **G010:** **Partially Verified / Awaiting Authoritative Infrastructure Evidence (IT/DBA)** — the
  grant path requires independent IT/DBA inspection (Appendix A packet provided).
- **keytronicshortage:** not connected; authentication/permissions/transport/topology Unable to
  Verify at runtime; intended SQL-auth/dedicated-account posture recorded as future.

**Status after this local evidence pass:**

- **S0.7 — IN PROGRESS.**
- **S0.7B — LOCAL EVIDENCE COMPLETE / AWAITING IT/DBA EVIDENCE.**
- **S0.3-G010 — Partially Verified / Awaiting Authoritative Infrastructure Evidence.**
- **S0.7-F002 — QAD Read Scope Exceeds KST Application Need / Least-Privilege Gap / Needs Human
  Review** (new; no severity assigned; NOT Accepted Risk; no remediation authorized in this pass).

S0.7 is **not** accepted. No S0.8 or Stage 9 work was performed. No remediation was performed.
This is evidence collection **awaiting project-owner review**; nothing is staged, committed, or
pushed.

---

## Appendix A — Read-Only IT/DBA Verification Query Packet (QAD)

> **For IT/DBA execution only** (an identity with sufficient server/database visibility — **not**
> the KST application principal). **READ-ONLY metadata inspection only.** No writes, no DDL, no
> permission changes. Do **not** prove read-only by attempting a write. Replace
> `<KST_WINDOWS_LOGIN>` with the exact Windows identity KST runs as (provided to the owner
> out-of-band). No passwords or secret connection strings are requested or used.

```sql
-- ============================================================
-- QAD grant-path verification (READ-ONLY). Run in QADPRO2.
-- ============================================================

-- A1. Server principal / login mapping for the KST runtime identity.
SELECT
    sp.name            AS server_principal,
    sp.sid,
    sp.type_desc,
    sp.is_disabled
FROM sys.server_principals sp
WHERE sp.name  = N'<KST_WINDOWS_LOGIN>'
   OR sp.sid   = SUSER_SID(N'<KST_WINDOWS_LOGIN>');

-- A2. Server-role membership for the KST principal (expected: public only).
SELECT
    r.name AS server_role,
    mp.name AS member
FROM sys.server_role_members m
JOIN sys.server_principals r  ON r.principal_id  = m.role_principal_id
JOIN sys.server_principals mp ON mp.principal_id = m.member_principal_id
WHERE mp.name = N'<KST_WINDOWS_LOGIN>';

-- A3. Database user mapping in QADPRO2 (expected: a mapped user for the identity).
SELECT
    dp.name          AS db_user,
    dp.sid,
    dp.type_desc,
    dp.is_fixed_role,
    dp.is_default
FROM sys.database_principals dp
WHERE dp.name = N'<KST_WINDOWS_LOGIN>'
   OR dp.sid  = SUSER_SID(N'<KST_WINDOWS_LOGIN>');

-- A4. Database-role membership in QADPRO2 (expected: db_datareader only).
SELECT
    r.name  AS database_role,
    mp.name AS member
FROM sys.database_role_members m
JOIN sys.database_principals r  ON r.principal_id  = m.role_principal_id
JOIN sys.database_principals mp ON mp.principal_id = m.member_principal_id
WHERE mp.name = N'<KST_WINDOWS_LOGIN>';

-- A5. Explicit GRANT / DENY entries for the KST principal (database- and object-level).
SELECT
    CASE p.class
        WHEN 1 THEN ISNULL(o.name, N'(database)')
        WHEN 3 THEN N'schema:' + ISNULL(s.name, N'')
        ELSE N'class=' + CAST(p.class AS nvarchar(10))
    END            AS scope,
    p.permission_name,
    p.state_desc,
    dp.name        AS grantee
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
LEFT JOIN sys.objects o ON o.object_id = p.major_id AND p.class = 1
LEFT JOIN sys.schemas s ON s.schema_id = p.major_id AND p.class = 3
WHERE dp.name = N'<KST_WINDOWS_LOGIN>'
ORDER BY scope, p.permission_name;

-- A6. Confirm the read-scope breadth: is SELECT database-wide (via db_datareader)
--     or object-scoped? (Role membership from A4 is authoritative; this is a cross-check
--     of how many user tables exist to frame the breadth.)
SELECT COUNT(*) AS total_user_tables FROM sys.tables;

-- A7. Confirm absence of write/DDL/admin authority at the object level for the
--     principal (metadata only; complements A4/A5). Expect only SELECT (or none).
SELECT
    o.name            AS object_name,
    p.permission_name,
    p.state_desc
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
JOIN sys.objects o              ON o.object_id     = p.major_id
WHERE dp.name = N'<KST_WINDOWS_LOGIN>'
  AND p.permission_name IN
      (N'INSERT', N'UPDATE', N'DELETE', N'ALTER', N'CONTROL', N'TAKE OWNERSHIP',
       N'CREATE TABLE', N'CREATE VIEW', N'CREATE PROCEDURE', N'EXECUTE', N'REFERENCES')
ORDER BY o.name, p.permission_name;
-- (An empty result set here corroborates the absence of explicit object-level
--  write/DDL grants for the principal.)
```

**AD-side evidence (outside the SQL packet):** IT is additionally asked to provide the
**AD/Windows group membership** (including nested groups) of `<KST_WINDOWS_LOGIN>` that
contributes SQL permissions, to complete the grant-path picture (item 2 in §10).
