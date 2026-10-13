# Stage 19 Part 2 — Workspace Shipments

Historical implementation plan dated 2026-10-13. Part 2 and Stage 19 overall were subsequently
COMPLETE / OWNER-ACCEPTED on 2026-10-13; see `KST_v2_STAGE_19_CLOSEOUT.md`. Original requirements and
verification expectations remain historical planning evidence.

## Accepted scope and repository discovery

Part 1 is COMPLETE / OWNER-ACCEPTED per the owner's checkpoint instruction. Its uncommitted
implementation, filter repair, fifteen defaults and documented limitations are the protected baseline.
The current Part 1 status prose contains earlier phase statements; implementation evidence's final
default amendment and the owner's explicit acceptance govern this task.

`WorkspaceAssignment`, `WorkspaceAssignmentDto`, `WorkspaceConfigurationService` and
`JsonWorkspaceConfigurationStore` establish the actual persisted configuration: site, product-line
bounds and explicit parent parts; there are **no customer or Corp Group configuration fields**.
Domain is derived by `QadSiteDomainMap`. Workspace Shipments scope is the assignment's site/domain
intersected with the authoritative resolved parent population from `QadMpsScopeResolver` (product-line
discovery UNION explicit parents). Display name, MPS snapshot state and other panels' filters/selections
are not shipment scope. No customer/group field or inferred membership is added.
If customer/group workspace configuration is introduced later, its explicit contract must be integrated
before claiming that capability. Part 1's membership EXISTS semantics remain available and unchanged.

## Foundation design and sequence

1. Define C# request/spec contracts and typed workspace routes, build OpenAPI and generate TypeScript.
2. Add independent application orchestration resolving the enabled assignment from persisted
   configuration. Reuse the disk-backed store through a separate workspace-owned instance/root;
   owners include assignment and visit session. No export routes.
3. Reuse authoritative catalog, projection/parser, header resolution, retro prices and On Time rule.
   Add a distinct Ship Date/exact-part query path: inclusive calendar bounds, descending raw Ship Date,
   ascending physical detail ID; SQL NULL dates sort last. Carry the complete server-resolved parent set
    as one parameterized XML value table and join it into the detail query. Preserve report query output/defaults.
4. Add generated-contract client and local-calendar ninety-day initialization helper, abort/release,
   bounded paging and superseded-reply guards. Hand off to Sonnet before UI implementation.
5. Original sequence expected UI integration/regressions, separate-context review, correction,
   sidecar republish and owner checks. Completed evidence and acceptance are in the Stage 19 closeout;
   this list remains historical planning, not outstanding work.

## Activation and lifecycle

`CustomerWorkspace` conditionally mounts modules except Dashboard. Add Workspace Shipments immediately
after Finished Goods and mount only while selected. The shell currently reuses CustomerWorkspace on
assignment changes: key the shipment panel by assignment ID, or explicitly recreate its visit on ID
change. Each mount gets initial local today-minus-89 through today and blank part; no persistence.
Apply uses edited filters; Refresh uses applied filters with a new immutable operation. Release and
abort on replacement/departure; never collect all pages. No MPS acquisition/selection prerequisite.

## Verification and constraints

Offline tests independently cover persisted assignment/site/domain, resolved parent population,
literal/blank/leading-zero part, Ship versus Invoice Date, inclusive independent bounds/end timestamps,
all-history/null dates/ties, physical detail identity, fixed fifteen columns, disk ownership, cancellation,
late publication/replies, workspace isolation and report isolation. SQL construction/parser fixtures are
not executed SQL Server evidence. Use repository build/type-generation/test workflows. Retain known
full-formatting, locked Stage 10 lint and incomplete AFT exceptions. No live reads, dependencies,
permissions, company writes, exports, commits or pushes. Final evidence packet stays below 5 MB.
