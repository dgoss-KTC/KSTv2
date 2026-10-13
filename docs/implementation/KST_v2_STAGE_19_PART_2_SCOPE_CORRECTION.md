# Stage 19 Part 2 — workspace parent-scope correction

Date: 2026-10-13. Historical parent-scope correction evidence. Part 1, Part 2 and Stage 19 overall
were subsequently COMPLETE / OWNER-ACCEPTED on 2026-10-13; see `KST_v2_STAGE_19_CLOSEOUT.md`.

## Cause and authoritative rule

The initial `WorkspaceShipmentsService` loaded the enabled assignment but returned only its site to
the reader. `WorkspaceShipmentsFilter` contained no parent membership, and the shipment SQL constrained
only site/domain plus optional exact part. Therefore it correctly exposed site-wide shipment history,
including parts outside the active assignment's configured scope. The React panel and parent-selection
state were not the source of the scope leak.

Current workspace contract `docs/api/Workspaces.md` §§118–215 establishes site plus product-line
and/or explicit parent ownership. Accepted implementation `QadMpsScopeResolver.ResolveAsync` resolves
the union: discovered product-line parents when configured plus all explicitly configured parents.
Product-line discovery retains its existing site/domain/product-line, planner/status, MRP dataset and
supply-type predicates; explicit parents are retained without current MRP activity or master metadata.
This correction reuses that resolver and does not change its logic or load an MPS snapshot. Current
workspace configuration has no customer/Corp Group field.

## Correction

`WorkspaceShipmentsService` reloads the enabled workspace and resolves membership on each acquisition.
The resulting immutable list is attached to the internal filter, never accepted from the browser.
QAD query intersects it with mandatory site/domain, Ship Date predicates and (if supplied) literal
exact part equality before rows reach the disk-backed acquisition store. A different same-site
assignment resolves its own population. An empty resolved population produces an empty complete
operation without opening QAD and cannot generate a site-wide fallback query. A new Refresh operation
re-reads configuration and resolves again; old ready operations retain immutable rows for paging.

To avoid the SQL Server parameter ceiling, database compatibility-level dependency or silent chunk
truncation for large populations, the QAD adapter passes the complete resolved identifiers as one
parameterized XML value and uses the existing SQL Server XML `.nodes()` mechanism to form a distinct
membership value table. XML serialization escapes special characters; part identifiers do not enter
SQL text. The XML node's original character length is compared to the invoice-detail part length so a
value beyond the extraction width cannot be truncated into another member. No fixed parent-count cap
was added. This mechanism was tested structurally/offline, not
against SQL Server in this authorized pass.

## Offline evidence

See `docs/implementation/KST_v2_STAGE_19_PART_2_FOUNDATION_EVIDENCE.md` and `WorkspaceShipmentsTests.cs`
/ `WorkspaceShipmentsQueryTests.cs`. Tests include two same-site assignments, line+explicit union,
outside same-site item, blank and exact filters, leading zero, empty membership, changed configuration
on refresh, old-result paging immutability, Ship Date bounds, 1,200 members without truncation,
parameterization/read-only source guard and unchanged Part 1 invoice-date/query behavior.

Not asserted here: live QAD connectivity, live XML `.nodes()` execution, source row counts/collation,
historical availability, performance, or company grants. Those are distinct from offline evidence.

## Focused security review and release

Independent review used separate context. Initial review found that unregistered acquisition IDs could
trigger scope work and consume permanent operation-ID capacity. The service now owner-checks the
registered operation and state before request validation/configuration access and defers the resolver
to the store's owned acquisition callback. The reviewer then found a duplicate-acquisition cleanup race;
service-level release-on-error was removed so only the store cleans up its own acquired work. Request
criteria are validated before configuration or resolver access. Follow-up review confirmed both findings
corrected, found no concrete remaining blocker in the reviewed path, and verified the status document.
Tests cover unknown IDs (20 attempts, zero resolver calls), invalid filters, a concurrently submitted
duplicate operation (rejected while winner reaches ready), XML parameterization, ownership and read-only
SQL guards. No tests executed by the reviewer; our local test run is recorded in the foundation evidence.

The initial post-metric sidecar hash recorded during implementation was later superseded by the
Stage 19 alpha.19 closeout build. Use the final size/hash in `KST_v2_STAGE_19_CLOSEOUT.md`.
Offline publish/copy does not establish packaged-app startup or live QAD behavior. No live database
queries, company writes, dependencies or capability changes were performed.
