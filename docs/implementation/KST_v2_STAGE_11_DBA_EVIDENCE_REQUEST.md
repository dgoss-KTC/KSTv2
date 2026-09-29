# Stage 11 — bounded DBA/IT evidence request

**Status: DEFERRED / RETAINED FOR FUTURE DIAGNOSTIC USE — 2026-09-29.** Stage 11 Workspace
Shortages is COMPLETE / ACCEPTED / LOCKED. The owner accepted its accuracy, usability, and observed
cold/cached performance. This packet is no longer required for Stage 11 completion; retain it
unchanged as the diagnostic starting point if production use reveals a material concurrency or
reliability issue.

2026-09-29. Design/evidence request; no database change is authorized by this packet. No database
index, schema, permission, isolation, server-setting, query-hint, or temporary-table acquisition
change is authorized by the Stage 11 closeout.

## Question and existing evidence

Shure SMT / 2140 (38 configured parents, approximately 600 purchased components) is the
primary owner-validation and worst-case stress scope. The same fact-query builder, split into
250-component requests, reduced observed latency substantially while increasing `mrp_det`
logical reads from approximately 89,333 to 442,698. SQL CPU also fell. This establishes a
latency/read-cost tradeoff, not its cause or an optimal batch size. SHOWPLAN was denied
(SQL error 262). KST will not bypass that denial.

## Minimum requested evidence

1. Existing index definitions (keys, includes, filters, uniqueness, disabled state) and relevant
   statistics age for `mrp_det`, `ld_det`, `loc_mstr`, `pt_mstr`, `ptp_det`, `code_mstr`,
   `pod_det`, and `po_mstr` involved in the two production query builders below.
2. Actual plans, preferably, or clearly identified estimated plans for representative 250- and
   500-component fact requests. Include the final smaller batch. Use the same complete component
   set partitioned deterministically, site/domain, horizon end, parameter types and lengths.
   Record exact parameter values internally or cardinalities/distributions with a locally retained
   reproducible parameter set. Do not put source parameter lists or connection settings in Git.
3. Per-request rows, received bytes, STATISTICS IO/TIME, CPU and elapsed, query-level waits when
   available, actual versus estimated rows, requested/granted/used memory, and spill warnings.
   Record concurrent workload, execution order and compilation/reuse context. A plan without
   runtime information must not be presented as actual runtime evidence.
4. QADPro2 replication/write-load constraints and an approved representative test window and
   concurrency budget. Is additional page-read volume acceptable at expected client concurrency?
5. Are same-connection session-scoped temporary component tables acceptable on this reporting
   replica? If so, specify policy authorization, permitted operations, tempdb budget, cleanup and
   cancellation requirements. Current KST read-only rules do **not** authorize CREATE/INSERT tests.
6. For any proposed index, confirm vendor support, replication/write amplification, maintenance
   ownership and rollout/rollback review. No candidate index DDL is proposed for execution here.

## Query identity and comparison protocol

Source: `src/backend/Kst.Integrations.Qad/LongTermShortages/QadLongTermShortageSourceReader.cs`,
`BuildBatchQuery` and `BuildPresentationQuery`. Use the current working-tree query text, not the
older HEAD version. The local performance harness retains the inputs and previous IO/TIME records.
IT can receive those through the approved internal channel. Record source revision/hash with results.

Keep the production `READ UNCOMMITTED` connection, due **or** release admission, no lower date
bound, exact `prrowid`, separate `mrp_line2`, textual PO-line comparison and confirmation ambiguity
guard. No cache flush, hints, server options, permissions, isolation changes or schema changes.
Use an externally bounded request and stop on the agreed deadline; the prior 500-component streaming
run exceeded 600 seconds despite a 60-second command timeout. Do not deliberately repeat that stall
without the DBA-approved window. Alternate comparable requests when operationally acceptable;
report application-cold and repeat reads separately, never call them SQL-cache-cold.

## Decisions this evidence should enable

Compare current VALUES batching with separately acquired component metadata/signed QOH and narrow
MRP events, retaining confirmation without wide metadata repetition. Compare transport choices only
after SQL Server version/compatibility and policy support are confirmed. An existing TVP type could
be evaluated if already supported; creating one is a schema change. JSON/XML transport support and
cardinality effects require evidence. No MRP × independent PO join is acceptable.

Join order, cardinality estimation, parameter sniffing, memory grants, waits and plan stability
remain unknown until the supplied evidence supports a conclusion. Low CPU/high elapsed alone
does not identify a wait. A faster one-client sample does not prove lower replica impact.
