# KST v2 API Documentation

**Status:** DRAFT — D0 API documentation foundation; awaiting project-owner acceptance

## Purpose

This directory is the human-facing semantic documentation for the KST v2 backend/API.

Its purpose is to answer questions that the generated OpenAPI contract cannot answer by itself, including:

- What business capability does an endpoint provide?
- What question is the capability intended to answer?
- What is the request scoped to?
- What is the response grain?
- What business rules govern the returned values?
- Which source systems and accepted mappings provide the data?
- What refresh, snapshot, cache, and stale-data behavior applies?
- What does missing, zero, null, stale, unavailable, or partial data mean?
- What limitations apply?
- What must an API consumer **not infer** from the response?

These documents are intended for:

- KST frontend developers;
- future alternate clients;
- backend maintainers;
- coding agents;
- reviewers;
- API consumers;
- project maintainers investigating current behavior.

They document backend capabilities by business meaning rather than by current frontend screen placement.

---

## Contract Authority

KST v2 uses the following API contract flow:

```text
C# DTOs and endpoint definitions
        ↓
Generated OpenAPI
        ↓
Generated TypeScript contracts
        ↓
Frontend API clients
        ↓
React presentation
```

The responsibilities of each layer are different.

### C# API implementation

`src/backend/Kst.Api/` is the implementation source for:

- endpoint definitions;
- request and response DTOs;
- HTTP mapping;
- Problem Details;
- OpenAPI generation;
- API-host concerns.

C# DTOs are the source from which the generated contract is produced.

### OpenAPI

`docs/openapi/Kst.Api.json` is the authoritative consumer-facing reference for the exact current mechanical HTTP contract, including:

- routes;
- HTTP methods;
- request parameters;
- request bodies;
- response schemas;
- required versus nullable fields;
- status codes;
- operation identifiers;
- OpenAPI formats.

Do not manually reproduce the complete OpenAPI schema in this directory.

If a human API document and OpenAPI disagree about an exact route, field, nullability rule, parameter, or status code, treat the disagreement as documentation or contract-generation drift that must be reconciled. Do not guess which shape a client should use.

### Generated TypeScript

`src/frontend/src/generated/api.ts` is generated from OpenAPI and must not be manually edited.

### Human API documentation

`docs/api/` explains the stable business and operational meaning of the contract.

It does not replace OpenAPI.

---

## Relationship to Other Repository Documentation

API documentation is one layer in the KST v2 documentation authority model defined by `AGENTS.md`.

For API research and maintenance, use the following sources according to their responsibility:

| Source | Primary responsibility |
|---|---|
| `AGENTS.md` | Enacted repository rules and authority model |
| `docs/status/CURRENT_PROJECT_STATUS.md` | Current accepted project state |
| `KST-v2-Master-Project-Checklist.md` | Current roadmap and stage status |
| `docs/architecture/` | Current architecture and project boundaries |
| `docs/data/qadpro2-data-map.md` | Curated QAD schema and validated source metadata |
| Accepted stage contracts, closeouts, and validation reports | Accepted implementation and business-rule evidence |
| Current backend implementation and tests | Implementation evidence when documentation requires clarification |
| `docs/openapi/Kst.Api.json` | Exact mechanical API contract |
| `docs/api/` | Human-facing API purpose, semantics, provenance, behavior, and limitations |

Historical plans, prototypes, legacy-system artifacts, and superseded implementation prompts remain useful evidence but do not silently override accepted current behavior.

When authoritative sources genuinely conflict, surface the conflict and resolve it through the repository authority rules rather than silently reconciling it.

---

## Documentation Status Labels

Every capability document must state its current documentation status.

### Implemented

Use **Implemented** when the documented behavior is present in the current repository and its current API contract can be confirmed from implementation/OpenAPI evidence.

Implemented does not mean that every future extension of the capability is complete.

### Accepted Design

Use **Accepted Design** when the project owner has accepted the business behavior or design, but the corresponding API implementation has not yet been completed or independently confirmed.

Do not present proposed route names, DTO fields, or other implementation details as current contract merely because the business design is accepted.

### Provisional

Use **Provisional** for implementation-specific detail that is actively evolving or has not yet been reconciled against the current repository.

Provisional material must be clearly separated from implemented contract.

### Unresolved

Use **Unresolved** when available evidence is insufficient or contradictory.

Do not resolve an Unresolved item by inference.

---

## Capability Documentation

D0 establishes this documentation foundation.

The intended capability backfill is:

```text
SYSTEM.md
WORKSPACES.md
MPS.md
PARTS.md
WORK_ORDERS.md
COMPONENTS.md
INVENTORY_SEMANTICS.md
IMMEDIATE_MATERIAL_ANALYSIS.md
```

These files should be added incrementally and reviewed at the documentation checkpoints defined for this workstream.

Do not create a documentation page merely because an internal class, database reader, or frontend component exists. Organize documentation around durable backend capabilities.

A shared backend rule may warrant its own semantic document even when it has no standalone HTTP endpoint. Conversely, several closely related endpoints may belong in one capability document.

---

## What Belongs in a Capability Document

A normal capability document should explain:

1. status and authority;
2. purpose;
3. business questions answered;
4. current contract reference;
5. request scope and identity;
6. response grain;
7. business semantics and calculations;
8. source/provenance;
9. refresh, snapshot, cache, and stale behavior;
10. errors and data-quality behavior;
11. limitations;
12. **Must Not Infer** rules;
13. related capabilities;
14. representative usage examples;
15. implementation/verification evidence;
16. unresolved or provisional items.

Exact request and response schemas remain in OpenAPI.

---

## Must Not Infer

KST v2 contains several capabilities whose outputs can appear similar while answering different business questions.

Human API documentation must explicitly identify important invalid inferences where ambiguity could affect scheduling decisions, implementation correctness, or future development.

Examples may include distinctions between:

- planning facts and live operational population;
- zero and missing data;
- current inventory and material coverage;
- an approved alternate and a purchasing commitment;
- contextual purchase-order information and supply netting;
- a structural BOM relationship and a work-order relationship.

Capability documents should state these boundaries directly rather than requiring consumers to infer them from implementation details.

---

## Source and Business-Rule Discipline

Do not invent:

- QAD tables;
- QAD fields;
- joins;
- predicates;
- calculations;
- fallback behavior;
- source precedence;
- business meanings.

Use `docs/data/qadpro2-data-map.md` as the primary curated QAD schema/business-metadata reference.

Use accepted stage/source-mapping evidence to explain how KST interprets those source facts for a particular capability.

If the mapping is missing or contradictory, mark the item Unresolved and identify the documentation gap.

---

## Frontend Independence

These documents describe backend capabilities and API semantics.

Do not define a capability by:

- which tab currently displays it;
- which modal currently opens it;
- current CSS or visual presentation;
- current frontend component names;
- temporary UI navigation.

Frontend examples may be mentioned for context, but the documented capability should remain useful to another client that consumes the same backend contract.

---

## Maintenance Rule

When a change establishes or modifies:

- an endpoint;
- a DTO;
- request scope;
- response grain;
- business semantics;
- source mapping;
- error behavior;
- snapshot/freshness behavior;
- an important limitation;

review the corresponding `docs/api/` capability document as part of the change.

Mechanical contract changes must continue to follow the established OpenAPI generation workflow.

Semantic documentation must never be used as a substitute for regenerating OpenAPI.