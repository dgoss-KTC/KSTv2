# Stage 13 Open Orders Planning Prompt

Continue KST v2 planning for **Stage 13 — Open Orders** in the repository:

`/home/david/dev/KSTv2/`

This is a new planning conversation. Verify the current repository rather than relying on this
prompt as evidence. The Release 1 roadmap review is complete and owner-approved. Stage 13 combines
the former Customer Open Orders and General Open Orders stages and now precedes Stage 14 Planning
Workbook. The stage-aligned planning version is `0.1.0-alpha.13`.

## Authority and preflight

Before proposing Stage 13 implementation:

1. Read every applicable `AGENTS.md`.
2. Inspect the current branch, `git status`, and recent history.
3. Preserve all local and untracked work. In particular, do not silently add, remove, relocate, or
   absorb the existing untracked Stage 11 reference files.
4. Read at minimum:
   - `docs/status/CURRENT_PROJECT_STATUS.md`
   - `docs/status/RELEASE_1_SCOPE_REVIEW_2026-09-29.md`
   - `KST-v2-Master-Project-Checklist.md`
   - `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`
   - current API conventions and contract workflow documentation
   - current database/source documentation relevant to sales and open orders
   - accepted Stage 4–11 closeouts where their contracts are reused
   - `SECURITY.md` and applicable security, dependency-admission, and licensing policies
   - export and external-update-file documentation relevant to QXtend
5. Inspect the current implementation and legacy/reference evidence before suggesting new models,
   services, queries, endpoints, components, or dependencies.

Apply the authority tiers in `AGENTS.md`. Accepted implementation and closeout evidence outrank
older planning assumptions. Do not reopen locked Stage 9–11 behavior unless current evidence exposes
a genuine conflict and the owner explicitly approves an amendment.

## Planning boundary

This conversation is for collaborative Stage 13 discovery and implementation planning. Do not yet:

- implement production code;
- run live QAD or Shortages database investigations;
- install or admit dependencies or tools;
- create or modify database objects, permissions, indexes, schema, or server configuration;
- change established business behavior;
- commit or push.

After the owner accepts a bounded Stage 13 plan, implementation must be authorized separately.

## Accepted Stage 13 intent

Stage 13 is one coherent Open Orders capability with two complementary workflows:

1. **Customer-focused Open Orders and date changes**
   - Inspect a selected customer's open order lines.
   - Stage only approved date-field changes locally.
   - Validate changes.
   - Produce a human-reviewable QXtend-compatible external update file.
   - Never write directly to QAD or another company database.

2. **General cross-customer Open Orders investigation**
   - Search and filter open orders across the authorized site/customer scope.
   - Provide safe sorting, result limits or pagination, and useful column selection/order.
   - Support customer-focused navigation without creating a second independent data model.

The ordinary Open Orders application-view export is decided and completed in Stage 21. The
QXtend-compatible date-change file is an operational update artifact and belongs to Stage 13. The
standalone Open Order Excel Report belongs to Stage 18. Keep those three outputs distinct while
identifying data contracts and infrastructure that can legitimately be shared.

## Questions the planning conversation must resolve

Work with the owner in manageable groups. Recover evidence first, then ask only questions that the
repository cannot answer.

### Workflow and scope

- Who uses each Open Orders workflow and what decision or action does it support?
- What is the default entry point: current workspace/customer, general search, or both?
- What site, customer, product-line, planner, and workspace boundaries apply?
- What constitutes an open order and which statuses are included or excluded?
- What result limits, pagination, refresh, and stale-data behavior are required?
- Are saved layouts required for Release 1 or a Stage 22 refinement candidate?

### Fields, filters, and editing

- Confirm authoritative mappings for sales-order number, customer PO, line, item/revision, ship-to,
  status, quantities, on hand, extended price, ship date, perform date, required date, and dock date.
- Identify required and optional filters, default sorting, selectable columns, and column order.
- Identify exactly which date fields may be staged for change.
- Define validation, frozen/closed/order-status restrictions, clearing, confirmation, and change-count
  behavior without guessing business rules.

### QXtend-compatible change file

- Recover the authoritative legacy/current input mapping, format, naming, validation, and operational
  handoff procedure.
- Define the exact relationship between displayed data, staged edits, validation messages, and file
  rows.
- Preserve human review and external processing; no automatic submission and no direct database
  writes.
- Identify security, filesystem, licensing, and audit requirements.

### Architecture and reuse

- Identify existing workspace, snapshot, QAD adapter, cache, endpoint, generated-contract, grid,
  preferences, and export/update-file patterns that should be reused.
- Keep QAD schema knowledge in `Kst.Integrations.Qad` and business orchestration in the accepted
  Domain/Application boundaries.
- Use the C# DTO → OpenAPI → generated TypeScript contract flow.
- Avoid speculative shared abstractions; extract shared behavior only from demonstrated Stage 13 and
  existing use cases.

### Validation and acceptance

- Define deterministic fixtures and representative owner-validation scenarios.
- Include cross-customer size/performance cases, empty and partial data, invalid filters, invalid
  edits, stale snapshots, QAD unavailable, file cancellation/failure, and safe retry behavior.
- Define comparison evidence for the existing Open Orders workflow and QXtend file.
- Identify which checkpoints require owner review before proceeding.

## Required discussion deliverables

Produce, for owner review before implementation:

1. A concise current-state and evidence map.
2. A proposed Stage 13 user workflow and screen/state model.
3. A field/source/rule discovery ledger separating known, inferred, and unknown items.
4. Proposed domain/application contracts and architecture boundaries.
5. The QXtend-compatible file boundary and its distinction from Stage 18 and Stage 21 outputs.
6. A performance, caching, pagination/result-limit, and failure-handling plan.
7. A security, dependency, filesystem, and licensing impact assessment.
8. A bounded checkpoint sequence with verification and owner-acceptance gates.
9. Focused owner questions that cannot be answered from repository evidence.
10. A clear list of Stage 13 non-scope and deferred Stage 22 refinements.

Do not produce a build prompt or begin implementation until the owner has reviewed and accepted the
Stage 13 plan.
