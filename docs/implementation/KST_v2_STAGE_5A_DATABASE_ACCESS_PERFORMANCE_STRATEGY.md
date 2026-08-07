# KST v2 — Stage 5A QAD Database Access, Security, and Performance Strategy

**Status:** Accepted Stage 5A technical strategy  
**Stage:** 5A — Data Inventory and Data Strategy  
**Initial capability:** MPS direct QAD query

---

## 1. Existing architectural decisions

The technical foundation already reserves `Kst.Integrations.Qad` as the QAD ERP integration boundary and identifies the intended stack as:

- `Microsoft.Data.SqlClient`,
- Dapper,
- Windows-integrated authentication,
- explicit SQL adapters.

`QadConnectionOptions`, `IQadConnectivityCheck`, and `DisabledQadConnectivityCheck` already exist as foundation concepts. Stage 5B should extend this boundary rather than introducing database dependencies into `Kst.Domain` or `Kst.Application`.

---

## 2. Connection configuration

- QAD connection settings are bound through the application's existing .NET configuration/options mechanism using `QadConnectionOptions`.
- The exact current configuration key/file binding should be confirmed by Stage 5B repository inspection rather than creating a parallel mechanism.
- Connection strings and credentials must not be committed to source control.
- Windows-integrated authentication is the accepted authentication mode.
- Production credentials/account context must be read-only for QADPRO2.

---

## 3. Query ownership

The initial MPS query belongs in `Kst.Integrations.Qad`.

The adapter owns:

- SQL text,
- SQL parameters,
- part-list batching,
- QAD-specific result records,
- raw-to-normalized source mapping,
- QAD connectivity/command diagnostics.

Business-week bucketing, MPS status classification, and snapshot orchestration remain outside the integration SQL layer.

---

## 4. Parameterization and batching

All workspace parent-part values must be SQL parameters. Never concatenate part numbers into executable SQL text.

Use a generated `VALUES` scope table (or equivalent parameterized form) in bounded batches.

Initial implementation default:

```text
Maximum parent-part parameters per query batch: 500
```

This keeps the statement comfortably below SQL Server's parameter ceiling after adding site/domain/horizon parameters and avoids very large SQL command text. The constant may be tuned later from measured Stage 5B behavior; it is not a user setting.

Results from multiple batches are merged before normalization/bucketing. Avoid N+1 queries per part.

---

## 5. Timeouts and cancellation

Initial interactive defaults:

```text
Connection timeout: use the established connection-string/default configuration unless the existing repository already overrides it.
Command timeout: 60 seconds for MPS source reads.
```

The command timeout should be configurable through backend options if the repository's options pattern supports it, but it should not be exposed as an end-user setting initially.

Every QAD async operation must accept and propagate a .NET `CancellationToken` through connection open/query execution where supported.

Do not add hidden automatic retry loops initially. User-initiated Retry/Refresh is the recovery mechanism.

---

## 6. Read-only requirement

The MPS path is strictly read-only. Stage 5B must verify the deployed connection/account can execute the required SELECT query but does not rely on QAD write privileges.

KST does not write to QAD as part of MPS retrieval or refresh. Future export/mass-update capabilities remain separate workflows.

---

## 7. Logging and diagnostics

Backend logs may include:

- operation name,
- workspace/site/domain context where appropriate,
- parent-part count,
- batch count,
- returned row count,
- elapsed duration,
- cancellation/timeout/failure category,
- snapshot/refresh correlation ID.

Do not log:

- passwords or credentials,
- full connection strings,
- authentication tokens,
- raw SQL text containing expanded user/source values unless explicitly needed in a protected developer diagnostic environment.

Detailed database exceptions stay in backend logs. The frontend receives the approved user-facing database-unavailable message rather than SQL exception details.

---

## 8. Performance strategy

Stage 5A does not impose an invented hard latency SLA. Stage 5B must measure the real query and end-to-end refresh envelope using representative workspaces.

At minimum record:

- resolved parent-part count,
- query batch count,
- returned source-row count,
- database elapsed time,
- normalization/bucketing elapsed time,
- total snapshot refresh time.

The design already limits unnecessary database work by:

- querying one workspace site/domain at a time,
- querying only resolved parent parts,
- excluding closed WOs and RMAs at source,
- avoiding dynamic pivots,
- avoiding N+1 per-part queries,
- reusing the same source snapshot for Due/Release and horizon changes,
- not re-querying QAD for fiscal display calculations.

---

## 9. Stage 5B verification gates

1. Confirm repository binding path for `QadConnectionOptions`.
2. Confirm real Windows-integrated connection to QADPRO2.
3. Verify production/test account is read-only for this path.
4. Verify 500-part batching against representative large workspaces and tune only if evidence supports it.
5. Verify 60-second command timeout and cancellation behavior.
6. Confirm timeout/database failures map to the approved UI error behavior.
7. Record representative row counts and refresh timings.
8. Confirm logs contain useful diagnostics without connection secrets.

---

## 10. Stage 5A disposition

Database access, authentication, batching, timeout, cancellation, logging, and performance-measurement strategy are sufficiently defined for Stage 5B planning. Repository-specific option binding and real environment verification are implementation tasks, not remaining business-design questions.
