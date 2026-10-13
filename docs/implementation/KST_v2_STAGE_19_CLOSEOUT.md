# Stage 19 — Historical Shipments: closeout

Closeout: **2026-10-13. COMPLETE / OWNER-ACCEPTED.** The owner explicitly accepted Part 1 —
Historical Shipments in Reports — Part 2 — Workspace Shipments — and Stage 19 overall on 2026-10-13.
This acceptance supersedes earlier phase/handoff statements that work or acceptance remained pending.
It does not assert release readiness or authorize a subsequent stage.

## Accepted implementation

### Part 1 — Historical Shipments in Reports

- Reports-owned historical invoice-detail listing with the accepted final fifteen default columns,
  existing filter repair, bounded immutable paging and XLSX export.
- Existing accepted On Time classification, nullable values, identifiers, signed quantities, source
  association and workbook semantics are retained.
- The owner accepted Part 1 on 2026-10-13. The owner-run filter observation and UI review are separate
  from offline tests and SQL construction fixtures.

### Part 2 — Workspace Shipments

- Workspace-owned, independent lifecycle and screen, with the same accepted fifteen columns.
- Every acquisition reloads enabled persisted assignment configuration and resolves membership as
  product-line-discovered parents UNION explicitly configured parents using the accepted MPS scope
  resolver. Mandatory workspace site/domain is enforced; no MPS snapshot is required.
- Optional exact Part Number intersects membership. Inclusive Ship Date filters, established ordering,
  query projection and On Time classification are retained. Empty membership fails closed.
- Parent membership uses one parameterized XML value table; XML extraction retains an original-length
  check to prevent a truncated long identifier from matching. No added dependency, database object or
  QAD write path.
- On Time Rate uses full-result counts accumulated in the existing bounded-memory acquisition pass:
  numerator = physical invoice details with On Time `1`; denominator = details with `1` or `0`.
  Null/unavailable values are excluded. Each physical detail is counted once, regardless of quantity or
  value. Zero eligible rows return counts `0/0` and display `—`; otherwise the UI shows one decimal
  place. Counts are tied to the immutable operation, not the current page. Refresh replaces them;
  paging preserves them; cancellation, departure and workspace change reject late replies.
- The owner reported the corrected scoping works and the screen looks acceptable, then accepted Part 2
  and Stage 19 overall on 2026-10-13. This bounded observation is not exhaustive live parity evidence.

## Evidence register

Implementation, source, tests and limitations are documented in:

- `docs/implementation/KST_v2_STAGE_19_PART_1_FOUNDATION_EVIDENCE.md`
- `docs/implementation/KST_v2_STAGE_19_PART_2_FOUNDATION_EVIDENCE.md`
- `docs/implementation/KST_v2_STAGE_19_PART_2_SCOPE_CORRECTION.md`
- `docs/api/HISTORICAL_SHIPMENTS.md` and `docs/api/WORKSPACE_SHIPMENTS.md`
- Generated contract pair: `docs/openapi/Kst.Api.json` and `src/frontend/src/generated/api.ts`

The final offline evidence includes a successful backend solution build with zero warnings/errors;
scoped API shipment/store (**22 passed**), QAD query/read-only (**32 passed**), architecture (**10 passed**)
and frontend Part 1/Part 2/workspace (**34 passed**) tests; frontend typecheck and scoped lint; generated
OpenAPI/TypeScript synchronization; and the focused XML-length-safeguard tests.
The recorded scoped counts are not additive to one another or a full-suite claim. Exact commands and
counts remain in the respective evidence records. The metric-specific API shipment filter passed 22
tests; Workspace Shipments + CustomerWorkspace frontend tests passed 19 tests; architecture tests passed
10; and 32 scoped QAD shipment/read-only tests include the XML membership/length coverage.
`git diff --check` and scoped .NET formatting verification passed during closeout.
Closeout reconciliation also verified version propagation: `scripts/check-version.ps1` passed for
`0.1.0-alpha.19`, and the focused `VersionConsistencyTests` passed 4 tests. The final alpha.19 backend
build regenerated OpenAPI; `npm run generate:types` then synchronized `src/frontend/src/generated/api.ts`.

## Owner acceptance versus verification evidence

Owner acceptance is the owner's decision on the integrated feature. It is recorded separately from:

- **Owner observations:** accepted Part 1 filter behavior; corrected Workspace Shipments scope works;
  screen looks acceptable; Part 1, Part 2 and Stage 19 overall accepted on 2026-10-13.
- **Offline tests:** synthetic/configuration fixtures, API/store tests, QAD SQL-construction and
  parameterization assertions, frontend tests and generated-contract/build checks.
- **Source/security review:** separate-context review of scope, operation ownership, cancellation and
  cleanup correction; no concrete blocker remained in the reviewed paths.
- **Artifact metadata:** local published and copied sidecar size and SHA-256 matched.

No live database query was performed for this closeout. SQL construction and mocked fixtures do not
prove SQL Server XML execution, source cardinality, full historical coverage, live collation behavior,
company grants, exhaustive numerical parity, retention or performance. The matching executable hash
does not establish packaged installer behavior or general release readiness.

## Final sidecar identity and version

The repository workflow `scripts/build-sidecar.ps1` published the final metric-enabled backend and
copied it to the Tauri sidecar path. Both files were verified locally:

| Artifact | Size | SHA-256 |
|---|---:|---|
| `publish/backend-sidecar/Kst.Api.exe` | 127,761,028 bytes | `A8B0DE3F138D9E55BF7E75DA939E0A81DD0D405F853054E07A35490EAB92917F` |
| `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` | 127,761,028 bytes | `A8B0DE3F138D9E55BF7E75DA939E0A81DD0D405F853054E07A35490EAB92917F` |

The final build also reports ProductVersion `0.1.0-alpha.19`. This supersedes the earlier post-metric
alpha.18 hash. The sidecar hash is offline file identity evidence; the second path is ignored by Git
and is not committed. Per `docs/development/VERSIONING.md`, active Stage 19 work uses `0.1.0-alpha.19`;
the numeric Windows installer version remains `0.1.0`. Advancing the stage-aligned alpha version does
not mark release readiness.

## Preserved limitations and boundaries

- Stages 9–11 remain locked and were not reopened.
- No customer/Corp Group workspace configuration exists; no such scope was inferred.
- No live QAD query, exhaustive live parity, full-history coverage, database-grant, retention or
  performance claim is made.
- The published executable was not used to claim packaged installer startup or connected desktop/QAD
  verification in this closeout.
- Known broad .NET formatting exceptions, the two locked Stage 10 lint errors and partial AFT
  diagnostics remain exceptions, not passes.
- Stage 21 export conformance and Stage 24 release readiness remain separate. Stage 19 acceptance
  does not start or authorize a later implementation stage.
