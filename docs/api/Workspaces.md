# Workspaces

## Status and Authority

**Implementation status:** Implemented
**Documentation status:** Implemented, with one Unresolved scope-resolution issue identified below
**Verification baseline:** `main` at `3e2805d70e5797b37317857c54d1184feda8e12c`

**Mechanical contract authority:** `docs/openapi/Kst.Api.json`

Accepted design/current-state evidence:

- `KST-v2-Master-Project-Checklist.md`
- `docs/implementation/STAGE_4_PHASE_1_PROGRESS.md`
- current workspace implementation and tests

---

## Purpose

A KST workspace represents a scheduler-managed set of parent-level parts at a specific site.

Workspaces establish the user-configured scheduling scope from which later backend capabilities resolve their authoritative data context.

A workspace is application-owned configuration.

It is not a QAD record and does not write scheduling ownership back to QAD.

---

## Questions Answered

The Workspace API answers questions such as:

- Which scheduling workspaces are configured locally?
- Which site does each workspace belong to?
- What product-line and/or explicit-parent configuration defines the workspace?
- Is the workspace active or archived?
- Is it temporary?
- What is its display order?
- Can the configuration be created, updated, archived, restored, reordered, or removed?

The Workspace API does not itself answer:

- Which QAD parent parts currently have MRP activity?
- Which work orders exist?
- Which parts are short?
- Whether a configured parent exists in QAD.
- Whether business data for the workspace has loaded.

Those are resolved by downstream capabilities.

---

# Contract Reference

Current workspace operations:

```text
GET /api/v1/workspaces
Operation: ListWorkspaces

POST /api/v1/workspaces
Operation: CreateWorkspace

PUT /api/v1/workspaces/{assignmentId}
Operation: UpdateWorkspace

POST /api/v1/workspaces/{assignmentId}/archive
Operation: ArchiveWorkspace

POST /api/v1/workspaces/{assignmentId}/restore
Operation: RestoreWorkspace

DELETE /api/v1/workspaces/{assignmentId}
Operation: DeleteWorkspace

DELETE /api/v1/workspaces
Operation: ResetWorkspaces

PUT /api/v1/workspaces/order
Operation: ReorderWorkspaces
```

Exact request/response schemas and status codes: `docs/openapi/Kst.Api.json`

---

# Request Scope and Identity

The durable identity of a saved workspace is its:

```text
assignmentId
```

The accepted workspace model contains the concepts:

```text
AssignmentId
DisplayName?
Site
ProductLineFrom?
ProductLineTo?
ParentParts[]
IsTemporary
CoverageEndsOn?
IsEnabled
SortOrder
```

Customer number is not part of current workspace scope.

IOS code is not part of current workspace scope.

---

# Workspace Scope

The accepted structural workspace rule is:

```text
Site
AND
(
    Product Line From
    OR
    at least one explicit Parent Part
)
```

Supported configurations include:

```text
Site + Product Line
Site + Product Line Range
Site + Explicit Parent Parts
Site + Product Line + Explicit Parent Parts
Site + Product Line Range + Explicit Parent Parts
```

Site is required.

Domain is not a workspace input. QAD integration resolves domain from site where required.

Product-line ranges are inclusive.

---

## Product-Line Scope

Product line is the normal workspace mechanism for scheduler responsibility.

Product-line configuration is persisted as application configuration.

Later MPS scope resolution uses QAD evidence to determine the applicable parent population for that configured product-line scope.

Merely saving a product line does not mean the Workspace API itself queried QAD.

---

## Explicit Parent Parts

Explicit parent parts support exceptional or split scheduling responsibility.

Accepted normalization includes:

- trim leading/trailing whitespace;
- discard blank entries;
- deduplicate identical normalized entries;
- do not require numeric-only values;
- preserve potentially meaningful internal characters.

The configured explicit-parent list is durable workspace configuration.

An explicitly configured parent does not require current MRP activity merely to remain configured.

---

## Combined Product-Line + Explicit-Parent Resolution

When Product Line scope and Explicit Parent Parts are configured together, KST combines the two populations.

Conceptually:

```text
Resolved Product-Line Parents
        UNION
Explicit Parent Parts
        =
Workspace Parent Population
```

Explicit Parent Parts therefore mean **additional explicitly managed parents** when Product Line scope is also configured.

Examples:

```text
Product Line only
    → use the resolved Product Line population

Explicit Parent Parts only
    → use the explicitly configured parents

Product Line + Explicit Parent Parts
    → include both populations
```

This is the accepted current behavior.

The earlier Stage 4B design described explicit parent parts as narrowing Product Line scope. That rule is superseded by the accepted current implementation and project-owner decision.

If a scheduler needs a workspace containing only a small set of specific parents, the Product Line fields may be left blank and those parents configured explicitly.

A possible future enhancement could allow explicit parents to be individually marked for inclusion or exclusion, but no exclusion behavior is part of the current workspace contract.


---

# Response Grain

`GET /api/v1/workspaces` returns one application-level workspace list.

Each workspace assignment is one persisted scheduler configuration.

The list response may include a nonfatal configuration warning.

Workspace order is represented explicitly rather than inferred from response position alone.

---

# Lifecycle Semantics

## Create

```text
POST /api/v1/workspaces
```

Creates and persists a new workspace after backend validation and normalization.

Successful creation returns the created workspace assignment.

Invalid configuration returns Problem Details.

---

## Update

```text
PUT /api/v1/workspaces/{assignmentId}
```

Updates the mutable configuration of an existing workspace.

Workspace identity is preserved.

Unknown assignment IDs return `404`.

Invalid configuration returns `400` Problem Details.

---

## Archive

```text
POST /api/v1/workspaces/{assignmentId}/archive
```

Archive is the normal nondestructive removal of a workspace from the active set.

Archiving sets the workspace inactive while preserving the persisted assignment for restoration.

The assignment retains its identity and scope configuration.

---

## Restore

```text
POST /api/v1/workspaces/{assignmentId}/restore
```

Restores an archived workspace to the enabled set.

---

## Permanent Delete

```text
DELETE /api/v1/workspaces/{assignmentId}
```

Permanently removes the specified persisted workspace configuration.

Successful deletion returns `204`.

Unknown assignment IDs return `404`.

---

## Reset All Workspaces

```text
DELETE /api/v1/workspaces
```

Removes all workspace assignments.

The operation is idempotent and returns `204` even when the workspace collection is already empty.

This resets workspace configuration, not unrelated application preferences.

---

## Reorder

```text
PUT /api/v1/workspaces/order
```

Updates persisted ordering for the active workspace set.

The submitted identity collection must satisfy backend validation.

Duplicate IDs or an invalid/mismatched assignment set return `400`.

---

# Validation and Normalization

The backend owns workspace validation.

Current accepted rules include:

- Site is required and normalized according to backend rules.
- A workspace requires product-line scope and/or at least one nonblank explicit parent.
- `ProductLineTo` cannot stand alone without `ProductLineFrom`.
- Product-line range rules remain backend-owned.
- Parent values are normalized before persistence.
- Equivalent active workspace scopes are rejected by duplicate-scope validation.

Clients should submit user intent and display backend validation messages rather than recreate authoritative scope validation independently.

---

# Persistence

Workspace configuration is persisted locally by KST.

The current infrastructure uses the application-owned workspace configuration store.

The frontend does not directly modify the underlying JSON file.

Accepted backward-compatibility behavior includes:

- old configuration containing obsolete `customerNumber` can load without making customer number authoritative again;
- older configuration without `parentParts` can load with an empty explicit-parent collection;
- corrupt configuration is handled through the application's established recovery/warning behavior.

---

# Source / Provenance

Workspace configuration itself is local application data.

It is not read from QAD.

When downstream business capabilities need a QAD domain, product-line membership, parent description, MPS facts, or other production data, those capabilities resolve them through the QAD integration boundary.

This separation is intentional:

```text
Local workspace configuration
        ↓
Capability-specific scope resolution
        ↓
Read-only QAD source access
```

---

# Error and Data-Quality Behavior

Current Workspace API patterns include:

- `400` for invalid configuration or reorder input;
- `404` for operations targeting an unknown assignment;
- `201` for successful creation;
- `204` for successful deletion/reset operations.

Exact status codes remain authoritative in OpenAPI.

A configuration warning in a successful list response is nonfatal and should not be treated as an empty workspace list error.

---

# Limitations

The Workspace API does not:

- validate every configured parent against QAD during ordinary workspace persistence;
- expose QAD domain as a user-managed workspace field;
- use customer number as authoritative scheduler scope;
- use IOS code as authoritative scheduler scope;
- load MPS merely because configuration was saved;
- create QAD scheduling assignments;
- write to QAD.

CSV-based convenience import of parent lists is not part of the current implemented API.

---

# Must Not Infer

Do not infer that:

- a configured parent necessarily has current MRP activity;
- a configured product line is itself the resolved MPS parent population;
- customer number identifies workspace ownership;
- IOS identifies workspace ownership;
- saving a workspace validates all business data in QAD;
- archiving deletes the workspace;
- deleting a workspace deletes production data;
- workspace sort order has scheduling priority meaning;
- temporary coverage changes QAD demand or supply;
- a successful workspace operation means MPS is loaded.

Until the combined-scope conflict is resolved, do not independently infer whether Product Line + Explicit Parent Parts means union or intersection/narrowing.

---

# Related Capabilities

- `SYSTEM.md` — application and source status
- `MPS.md` — resolves workspace configuration into current planning scope and snapshot
- `PARTS.md` — detail for a resolved MPS parent
- `API_CONVENTIONS.md` — shared error/status conventions

---

# Verification / Evidence

Relevant evidence includes:

- `src/backend/Kst.Api/Endpoints/WorkspaceEndpoints.cs`
- `src/backend/Kst.Api/Dtos/WorkspaceDtos.cs`
- `src/backend/Kst.Application/Workspaces/`
- `src/backend/Kst.Domain/Workspaces/`
- `src/backend/Kst.Infrastructure/Workspaces/JsonWorkspaceConfigurationStore.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/WorkspaceEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/WorkspaceReorderEndpointTests.cs`
- `src/backend/tests/Kst.Application.Tests/Workspaces/`
- `KST-v2-Master-Project-Checklist.md`
- `docs/implementation/STAGE_4_PHASE_1_PROGRESS.md`
- `docs/openapi/Kst.Api.json`

---

# Open / Provisional Items

No unresolved Workspace API contract items were identified during D1.

Explicit-parent exclusion or Add/Remove behavior is a possible future workspace enhancement and is not part of the current implemented contract.
