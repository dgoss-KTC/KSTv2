# KST v2 API Conventions

**Status:** DRAFT — D0 API documentation foundation; awaiting project-owner acceptance

## Purpose

This document defines the shared conventions used when documenting and consuming the KST v2 backend API.

It is not an exhaustive endpoint reference and does not duplicate the generated OpenAPI schema.

For the exact current HTTP contract, use:

```text
docs/openapi/Kst.Api.json
```

For the contract-generation workflow, use:

```text
docs/architecture/API_CONTRACT_WORKFLOW.md
docs/development/OPENAPI_CLIENT_GENERATION.md
```

For capability-specific business meaning, use the relevant document under `docs/api/`.

---

# 1. API Boundary

`Kst.Api` is the local ASP.NET Core HTTP boundary for KST v2.

Its responsibilities include:

- endpoint definitions;
- request and response DTO mapping;
- Problem Details;
- dependency-injection composition;
- OpenAPI generation;
- API-host concerns.

Business rules belong in the appropriate Domain/Application layers.

QAD-specific SQL and schema knowledge belong in the QAD integration boundary.

Clients should consume scheduling concepts exposed through API contracts rather than depend on database structure.

---

# 2. Local Application Boundary

KST v2 is a Windows desktop application whose backend is hosted as a local sidecar process.

The backend is intentionally loopback-only.

The API should not be documented or treated as a general network service merely because it uses HTTP.

Networking and exposure rules are security properties governed by the repository security architecture.

API documentation must not imply that broadening the listener, exposing the API to a LAN, or creating an external service endpoint is a normal API configuration change.

---

# 3. Versioned and Diagnostic Routes

Current business/application API routes use the versioned prefix:

```text
/api/v1/
```

Process diagnostics currently use unversioned routes such as:

```text
/health
/ready
```

Do not assume an unversioned diagnostic route establishes a convention for business endpoints.

Do not invent a future API version or versioning policy that is not represented in current architecture.

---

# 4. Contract Ownership and Generation

The contract pipeline is:

```text
C# DTOs and endpoint definitions
        ↓ dotnet build
docs/openapi/Kst.Api.json
        ↓ npm run generate:types
src/frontend/src/generated/api.ts
```

Rules:

- never manually edit generated TypeScript contracts;
- regenerate OpenAPI after endpoint or DTO changes using the established backend build;
- regenerate TypeScript contracts after OpenAPI changes;
- fix downstream type failures at their real source;
- synchronize generated contract artifacts when the mechanical API changes.

Human Markdown must not become a competing hand-maintained schema.

---

# 5. Request Scope

KST endpoints should expose business/API identities rather than QAD implementation details.

Common API identities include concepts such as:

- workspace assignment;
- parent part;
- component part;
- work-order ID;
- snapshot/freshness identity where required by the capability.

Site and domain handling is capability-specific.

Do not require clients to supply QAD-specific source keys, database joins, or source-system implementation details unless an accepted API design explicitly makes them part of the contract.

For workspace-scoped capabilities, the workspace is normally the server-side source of accepted scope such as site and configured part ownership.

Capability documentation must explain what the caller selects and what the backend resolves.

---

# 6. Identity Is Semantic

Identifiers that look similar are not automatically interchangeable.

For each capability, document the accepted identity explicitly.

Examples include distinctions among:

- workspace assignment ID;
- parent part number;
- component part number;
- scheduler-facing work-order ID;
- structural BOM occurrence identity;
- snapshot ID.

Do not infer identity from field names alone.

Do not substitute another source identifier because it appears more convenient or more familiar.

---

# 7. Response Grain

Every capability document must state the response grain.

Examples of grain include:

- one workspace;
- one parent schedule;
- one planning bucket;
- one work order;
- one work-order material line;
- one BOM occurrence;
- one component;
- one approved-alternate relationship.

Response grain is especially important when the same part or work order may legitimately occur more than once in a structural or planning context.

Do not introduce `DISTINCT`, deduplication, consolidation, or shared-demand aggregation merely because repeated values appear in an API response.

Such behavior requires an accepted business rule.

---

# 8. Typed JSON Contracts

Normal application responses use typed JSON DTOs.

The exact schema, required fields, nullable fields, formats, and collection shapes are defined by current OpenAPI.

Consumers must respect the distinction among:

- a numeric zero;
- an empty collection;
- a nullable field containing `null`;
- a missing or unavailable capability;
- a stale last-known-good value;
- an error response.

These states are not interchangeable.

Capability documentation must explain their business meaning where it matters.

---

# 9. Null Is Not Zero

A `null` value must not automatically be interpreted as numeric zero, false, empty text, or “none required.”

Likewise, zero must not automatically be interpreted as missing data.

Examples differ by capability:

- zero qualifying inventory may be a valid measured result;
- unavailable planning, cost, or source information may be represented as null;
- some source failures result in an error rather than a partial result.

The capability document defines the semantic meaning.

OpenAPI defines whether the field is mechanically nullable.

---

# 10. Dates and Timestamps

Use the formats defined by OpenAPI.

A date-only business value and a timestamp are different concepts.

Capability documentation must state the business meaning of important dates, including where relevant:

- Due Date;
- Release Date;
- effective date;
- business-week label;
- source-loaded time;
- refresh time.

Do not infer that all date values are interchangeable or that all date filtering uses the same date basis.

Do not infer UTC/local-business-time semantics when the capability evidence does not establish them.

---

# 11. HTTP Success Responses

A successful HTTP response means the requested API operation completed according to that capability's contract.

It does not necessarily mean all returned data was freshly retrieved from an authoritative source at the time of the request.

Some lazy-loaded capabilities may explicitly return a compatible last-known-good value after a source reload failure.

Where stale fallback is supported, the response must expose its stale/warning semantics through the implemented contract, and the capability documentation must explain them.

Consumers must not determine freshness from `200` alone.

---

# 12. Problem Details

KST uses ASP.NET Core Problem Details for normal API error responses.

OpenAPI represents these responses as:

```text
application/problem+json
```

The exact documented status codes for an endpoint remain authoritative in OpenAPI.

Capability documentation should explain the business condition associated with those statuses.

Common current meanings include:

### 400 — Bad Request

Used for invalid or incomplete request input, such as invalid query parameters or validation failures.

A `400` is a request-contract problem, not evidence of empty business data.

### 404 — Not Found

Used when a requested workspace or business resource cannot be resolved under the capability's accepted rules.

The exact meaning is capability-specific.

For example, “not found” may distinguish among an unknown workspace, an unknown source record, or a resource that is not valid in the requested workspace scope.

Do not generalize one endpoint's `404` meaning to another.

### 409 — Conflict

Used by current capabilities when the request is structurally valid but required application state has not yet been established.

A current example is a detail capability requiring an MPS snapshot that has not yet been loaded.

The capability document must explain the prerequisite state.

### 503 — Service Unavailable

Used by current source-backed capabilities when required authoritative-source data cannot be obtained and the capability has no valid fallback result to return.

A `503` must not be silently converted into an empty successful response.

---

# 13. Do Not Invent Generic Error Meaning

The shared meanings above describe current patterns, not permission to invent a status mapping for a new capability.

When documenting an endpoint:

1. confirm its current OpenAPI status codes;
2. inspect accepted implementation behavior;
3. explain the capability-specific trigger;
4. identify differences from other endpoints.

Do not document undocumented `500`, retry, partial-response, stale, or recovery behavior by assumption.

---

# 14. Snapshot and Freshness Semantics

KST uses snapshots and in-memory detail caches for several planning capabilities.

Snapshot behavior is capability-specific, but the following documentation rules apply.

### Snapshot identity

Where a `snapshotId` participates in an API contract, document what generation or freshness boundary it identifies.

Do not assume every endpoint uses the same snapshot identity in the same way.

### Successful refresh

A successful source refresh may establish a new snapshot generation and may make detail caches eligible for reload.

Document the actual behavior of the capability rather than saying simply that the cache is “cleared.”

### Failed refresh

Some accepted KST workflows preserve the previous good snapshot when a refresh attempt fails.

A failed refresh therefore does not automatically mean that all previously loaded detail data becomes invalid.

### Stale last-known-good data

Some lazy detail capabilities can return a compatible cached value after a fresh source attempt fails.

When supported, document:

- the compatibility boundary;
- how stale state is represented;
- any warning;
- conditions under which stale fallback is forbidden.

Do not generalize stale-fallback support to a capability whose implementation does not provide it.

### Process lifetime

Where a cache or snapshot is intentionally in-memory only, document that it is not persisted across application sessions.

---

# 15. Empty, Missing, Partial, Stale, and Unavailable Are Different

Capability documentation should distinguish these states explicitly.

```text
Empty
    The request succeeded and the valid result contains no rows/items.

Zero
    The request succeeded and the measured/calculated numeric value is zero.

Partial
    The request succeeded, but one or more optional source-backed values are unavailable
    and are represented according to the accepted contract.

Stale
    A compatible last-known-good result was returned after a newer load could not be completed.

Unavailable
    The capability could not produce a valid result under its accepted rules.

Unknown / Data Issue
    A business classification explicitly says the available source facts are insufficient
    or contradictory for the requested determination.
```

Do not collapse these into one generic “no data” state.

Not every capability supports every state.

---

# 16. Source and Provenance

Human API documentation should identify meaningful source lineage without turning API documentation into a duplicate database dictionary.

Use:

```text
docs/data/qadpro2-data-map.md
```

for curated QAD field/table metadata.

A capability document should summarize:

- authoritative source system;
- major source facts needed to understand the capability;
- accepted transformations or calculations;
- important source precedence;
- relevant data-quality limitations.

Link to existing stage/source evidence for deeper implementation history.

Do not manually copy hundreds of QAD field definitions into `docs/api/`.

---

# 17. Read-Only Source Behavior

KST's company-database integrations are read-only.

API documentation must not imply that an endpoint:

- updates QAD;
- reserves inventory in QAD;
- changes a work order;
- commits purchasing activity;
- writes planning decisions back to a production database;
- automatically executes an operational change.

Where KST produces a recommendation, classification, analysis, note, preference, local configuration, or export, describe that behavior accurately.

Do not describe an informational or analytical response as an operational transaction.

---

# 18. Business Terminology vs. Technical Names

Prefer accepted scheduler/business terminology in explanatory prose.

Preserve exact technical names when identifying:

- routes;
- DTOs;
- operation IDs;
- source fields;
- implementation types.

When technical naming and user-facing terminology intentionally differ, document the distinction rather than renaming one mentally.

For example, an existing technical contract may retain historical implementation terminology while the scheduler-facing concept has an accepted different name.

The human documentation should make that translation explicit.

---

# 19. Backend Semantics vs. Frontend Presentation

The backend should expose semantic business values rather than UI colors or transient presentation state.

Human API documentation therefore describes concepts such as:

- status;
- classification;
- requirement;
- inventory quantity;
- planning basis;
- stale state;
- warning;
- provenance.

It should not define a semantic API state as “the red cell,” “the green badge,” or “the modal value.”

Frontend presentation may change without changing the backend capability.

---

# 20. Must Not Infer

Every business-sensitive capability document should contain a **Must Not Infer** section.

Use this section for plausible but invalid interpretations that could cause incorrect scheduling logic, incorrect UI behavior, or future implementation drift.

Examples of the form include:

```text
Do not infer X from Y.
```

or:

```text
This response establishes A. It does not establish B.
```

Particular Must Not Infer rules belong in the capability document where the distinction is established.

Do not add speculative warnings merely to make the section longer.

---

# 21. Capability Documentation Status

Use one or more of these labels:

- **Implemented**
- **Accepted Design**
- **Provisional**
- **Unresolved**

When a document contains mixed maturity, identify the status by section.

Example:

```text
Business semantics: Accepted Design
API route/DTO contract: Provisional
Implemented contract: Not yet confirmed
```

This is preferred to assigning the entire document an ambiguous single status.

---

# 22. Standard Capability Document Structure

Use this structure unless a capability clearly requires a different organization:

```markdown
# <Capability>

## Status and Authority

## Purpose

## Questions Answered

## Contract Reference

## Request Scope and Identity

## Response Grain

## Business Semantics

## Source / Provenance

## Refresh, Snapshot, Cache, and Stale Behavior

## Error and Data-Quality Behavior

## Limitations

## Must Not Infer

## Related Capabilities

## Usage Examples

## Verification / Evidence

## Open / Provisional Items
```

Do not create empty sections solely for template compliance. If a section genuinely does not apply, omit it or state why briefly.

---

# 23. Contract Reference Style

A capability document should identify current routes and operations, but it should not manually duplicate the complete schema.

Preferred form:

```text
GET /api/v1/...
Operation: <operationId>
Exact schema: docs/openapi/Kst.Api.json
```

Then explain:

- why to call it;
- how its scope works;
- what each semantically important value means;
- errors that affect consumer behavior.

Do not reproduce every DTO property merely to create a second schema reference.

---

# 24. Examples

Examples in human documentation should demonstrate behavior or semantics rather than act as a second contract definition.

Examples should:

- use representative synthetic identifiers unless real identifiers are required as accepted evidence;
- avoid credentials or production-sensitive information;
- avoid implying undocumented optional fields;
- remain subordinate to current OpenAPI.

If exact payload examples are maintained, verify them against the current contract.

---

# 25. Documentation Verification

Before accepting a capability documentation change:

1. verify exact routes and operation identifiers against current OpenAPI;
2. verify request/response semantics against accepted implementation evidence;
3. verify QAD mappings against the curated data map and accepted source-mapping evidence;
4. verify terminology against current accepted project language;
5. identify provisional implementation details explicitly;
6. search for superseded business rules that may have been accidentally reintroduced;
7. verify that deferred capabilities have not been described as implemented;
8. verify that the document does not contradict enacted architecture or security rules.

If verification finds a material contradiction, stop and resolve it rather than writing around it.

---

# 26. Change Discipline

A human API documentation change does not itself authorize:

- an endpoint change;
- a DTO change;
- a business-rule change;
- a source-mapping change;
- a QAD query change;
- a frontend contract change.

If documentation research reveals that implementation and accepted business rules conflict, surface the conflict to the project owner.

Do not silently “fix” production behavior while performing documentation work.

Likewise, do not rewrite accepted business semantics merely to describe accidental implementation behavior.

The repository authority model determines which discrepancy requires correction.