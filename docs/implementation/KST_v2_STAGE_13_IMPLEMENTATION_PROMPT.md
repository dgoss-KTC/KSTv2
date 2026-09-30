# KST v2 Stage 13 — Customer Open Orders Implementation Prompt

Implement the owner-accepted **Stage 13 — Customer Open Orders** plan in:

`/home/david/dev/KSTv2/`

Owner plan acceptance date: **2026-09-29**. Stage version: **`0.1.0-alpha.13`**.

This is a bounded implementation prompt, not blanket authorization. Begin only when the project
owner explicitly authorizes implementation. Stop at every checkpoint gate and present evidence for
owner review before continuing.

## 1. Authority and preflight

Before changing files:

1. Read every applicable `AGENTS.md` completely.
2. Inspect the current branch, worktree status, recent history, and attached worktrees/PRs.
3. Preserve all unrelated local work, including the existing untracked Stage 11 reference files.
4. Read:
   - `docs/prompts/STAGE_13_OPEN_ORDERS_PLANNING_PROMPT.md`
   - `docs/status/CURRENT_PROJECT_STATUS.md`
   - `KST-v2-Master-Project-Checklist.md`
   - `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`
   - `docs/architecture/API_CONTRACT_WORKFLOW.md`
   - `docs/development/OPENAPI_CLIENT_GENERATION.md`
   - `docs/data/qadpro2-data-map.md`
   - `SECURITY.md` and applicable security, dependency, and licensing policy
   - the current workspace, MPS scope, preference, snapshot, export, file-save, and test patterns
   - `/home/david/dev/Archive/kst/features/oor_sales.py`
   - the owner-supplied QXtend CSV templates
5. Treat the accepted planning prompt as the Stage 13 business baseline. If implementation evidence
   conflicts with it, stop and obtain an owner-approved amendment rather than silently changing the
   rule.

Never modify or absorb unrelated untracked work. Never commit or push unless separately requested.

## 2. Hard safety boundaries

- All company-database access is read-only and parameterized.
- Never INSERT, UPDATE, DELETE, MERGE, execute operational database changes, submit to QXtend, or
  trigger an import.
- QXtend output is a human-reviewable file saved only after the user chooses a destination.
- Do not log order contents, POs, comments, prices, customer data, connection strings, or secrets.
- Keep the backend loopback-only.
- Add no dependency without the repository's security and licensing admission process. The accepted
  plan currently requires no new dependency.
- Do not reopen locked Stage 9–11 behavior.

## 3. Accepted capability

Add **Customer Open Orders** before Component Orders in the customer-workspace module navigation.
The module is limited to the active workspace site and current MPS snapshot's resolved parent list.
It never performs cross-customer/global investigation.

A source line qualifies when its site and part are in that workspace scope and:

```text
sod_qty_ord - sod_qty_ship > 0
```

Do not add status, hold, completion, order-type, or RA exclusions.

### Report Mode

- Load the full field contract once.
- Perform column show/hide, reordering, filtering, and sorting without re-querying QAD.
- Default columns: Due Date, Order, PO, Line, Item Number, Site, Open, Stat, Ext Price.
- Expose all accepted legacy optional columns.
- Preserve legacy AND-combined filters and default Customer Name → Item Number → Due Date sort.
- Remember visibility and order per workspace assignment ID.
- Export all currently filtered workspace rows to XLSX using selected columns in displayed order.

### Planning Mode

- Make Due Date, Perform Date, Required Date, Dock Date, Order Qty, and Price editable.
- Force the planning fields plus Reason Code visible while Planning Mode is active.
- Retain original values, highlight proposals, calculate proposed Open and Ext Price, remove no-ops,
  and support row undo and Clear All.
- Reason Code values are exactly `Cust/PM`, `Buyers`, `Planning`, `Factory`, `C&R`, `Quality`, and
  `Engineer`; require one only for changed rows.
- Save Draft is off by default. When enabled, persist the draft locally per workspace. Restore only
  after fresh validation. Conflicts remain visible and cannot export.
- Export only changed rows, retain the draft after export, and never infer successful QXtend import.

## 4. Accepted field behavior

- `Open = OrderQty - ShippedQty`.
- `ExtPrice = Price * Open` using exact decimal arithmetic.
- Order Qty edits target total ordered quantity, not Open quantity. A proposal below freshly read
  shipped quantity is invalid; equality is allowed.
- One Price edit populates both QXtend List Price and Price. Use plain invariant decimal text without
  a currency symbol, grouping, or scientific notation. Set Reprice/Edit to `TRUE`.
- Accept valid calendar dates or intentional empty dates. Do not invent ordering or past-date rules.
  QXtend date text is `M/d/yyyy`.
- Preserve the legacy report's consignment Unit Price, Site QOH, comment, customer, ship-to, and
  extended-price behavior unless verified source limitations require an owner decision.

## 5. QXtend format

Treat the owner-supplied templates as exact header and column-order evidence:

- `UpdateQuantities.csv`
- `UpdatePrices.csv`
- `DateChange_with_dock.csv`

Generate up to three separate CSV files. A row with multiple change families belongs in each
applicable output.

For every output:

1. Sort Sales Order then Line ascending.
2. Put `M` in detail Operation column C on every data row.
3. Put Sales Order in detail column D on every data row.
4. On the first row for each Sales Order, copy C to parent column A and D to parent column B.
5. Leave A and B blank on later rows for that Sales Order.
6. Preserve exact headers, CRLF endings, UTF-8 without BOM, correct CSV escaping, and no blank
   template rows.

Date output includes all four effective dates for any row with a date change. Quantity output sets
Quantity Ordered. Price output sets Reprice/Edit, List Price, and Price. Every output repeats the
row's Reason Code.

## 6. Architecture and contracts

Follow existing boundaries:

- `Kst.Integrations.Qad`: QAD SQL, batching, source rows, and targeted current-line rereads.
- `Kst.Domain`: line identity, exact calculations, Reason Code, proposal comparison, and validation.
- `Kst.Application`: workspace/MPS scope, Open Orders snapshot lifecycle, drafts, stale comparison,
  and export eligibility.
- `Kst.Infrastructure`: in-memory snapshot cache and atomic local JSON draft/preference storage.
- `Kst.Exports`: XLSX report and three CSV serializers.
- `Kst.Api`: typed endpoints, DTO mapping, Problem Details, file responses, and DI.
- Frontend: typed client, grid state, accessibility, filters, editing, layouts, draft controls, and
  explicit Save As behavior.

Use C# DTOs as contract authority, regenerate `docs/openapi/Kst.Api.json`, then regenerate
`src/frontend/src/generated/api.ts`. Never edit generated TypeScript manually.

Expected concepts include `OpenOrderLine`, `OpenOrderLineKey`, `OpenOrderEditableValues`,
`ProposedOpenOrderChange`, `OpenOrderDraft`, `OpenOrdersSnapshot`, `OpenOrderExportKind`, and
structured validation issues. Names may follow repository conventions, but do not collapse distinct
source identity, snapshot identity, original values, and proposed values.

Use workspace routes under `/api/v1/workspaces/{assignmentId}/open-orders`. Provide typed read,
refresh, draft load/save/delete, report export, and QXtend export operations. Require the caller's
current MPS snapshot ID and Open Orders snapshot ID where applicable.

## 7. Freshness and failure semantics

The Open Orders result has its own snapshot ID and acquisition time. A compatible last-good report
may remain visible after a refresh failure with explicit stale/warning state. It cannot be used to
generate QXtend output.

Immediately before QXtend generation, reread only the affected line identities and verify:

- workspace and MPS scope still match;
- the line exists and is currently open;
- every original editable value still matches;
- current shipped quantity does not exceed the proposed Order Qty;
- proposals and Reason Codes are valid.

Fail closed with normal Problem Details. Use 400 for invalid input, 404 for missing workspace/line,
409 for stale scope/snapshot/source conflict, and 503 for source unavailability. Never return an
empty successful file for an unavailable source. Save cancellation and filesystem failure retain
the draft unchanged.

## 8. Source-verification stop gate

Before implementing production SQL, reconcile the legacy and documented source names, including:

- legacy `sod__dte01` versus documented `sod_dock` for Dock Date;
- legacy `so_partial` for Partials;
- legacy `sod_qty_all` for Allocated.

Use repository evidence and only the minimum separately authorized read-only source verification.
Do not guess a replacement field. Record the result in a Stage 13 field/source ledger and stop for
owner review if evidence remains contradictory.

## 9. Performance requirements

- Use one set-based read over the exact parent population; batch with the established MPS pattern.
- Avoid per-row customer, item, inventory, or comment queries.
- Return the complete scoped result without silent truncation.
- Measure query elapsed time and row count without logging row contents.
- Keep column operations and normal filtering local after load.
- Add client-side paging or a rendering window only when measured volume requires it and keyboard
  focus/accessibility remain correct.
- Targeted export validation rereads only changed lines.

## 10. Checkpoints and mandatory stops

### 13.1 — Source contract and deterministic fixtures

- Produce the final field/source/rule ledger.
- Resolve the source-verification items.
- Add source-reader query-shape tests proving domain/site/parent/open scope and read-only SQL.
- Create deterministic report and QXtend fixtures.
- Run focused backend tests.

**Stop and obtain owner acceptance before 13.2.**

### 13.2 — Read-only backend and API

- Implement domain contracts, QAD reader, Open Orders snapshot store/service, DTOs, endpoints, DI,
  Problem Details, and API documentation.
- Regenerate OpenAPI and generated TypeScript together.
- Cover loaded-empty, unknown workspace, MPS-not-loaded, snapshot-changed, QAD unavailable,
  last-good stale report, and refresh retry.
- Run focused and full backend verification.

**Stop and obtain owner acceptance before 13.3.**

### 13.3 — Report Mode UI and layout persistence

- Add navigation and the read-only report.
- Add legacy filters/sort, all fields, default layout, accessible ordering, reset, per-workspace
  persistence, refresh, loading/empty/error/stale states, and XLSX report export.
- Test keyboard access, cancellation, save failure, workspace switching, corrupt preferences, and
  unknown saved columns.
- Present representative legacy comparison evidence.

**Stop for owner desktop review and acceptance before 13.4.**

### 13.4 — Planning Mode and draft persistence

- Add editing, previews, Reason Code, validation, undo/clear, change counts, Plan Mode, and Save Draft.
- Implement atomic local draft persistence, restoration validation, conflict presentation, archive
  preservation, and permanent-delete cleanup.
- Test all field rules, no-ops, mixed changes, invalid quantities/prices/dates, stale drafts, and
  workspace isolation.

**Stop for owner desktop review and acceptance before 13.5.**

### 13.5 — QXtend exporters

- Implement fresh targeted validation and the three exact CSV serializers.
- Add byte-level golden tests for headers, CRLF/no-BOM encoding, escaping, date/decimal formatting,
  first-row parent grouping, multiple lines, multiple orders, and mixed change families.
- Add explicit Save As behavior, Export All behavior, cancellation, retry, and failure tests.
- Have the owner inspect generated files and validate them through the external QXtend process.

**Stop for owner acceptance before 13.6.**

### 13.6 — Integrated verification and closeout

- Run full backend and frontend tests, typecheck, lint, production build, OpenAPI synchronization,
  relevant security checks, and changed-file review.
- Verify no direct writes, automatic imports, sensitive logging, new unadmitted dependency, or
  unrelated Stage 9–11 change.
- Record measured representative performance and all owner-validation evidence.
- Update checklist, status, API/capability documentation, version evidence, and Stage 13 closeout.

Do not declare Stage 13 complete until the owner explicitly accepts this checkpoint.

## 11. Non-scope

- Cross-customer/global Open Orders: Stage 18.
- Planning Workbook, forecast/MPS projections, and MPS adjustment files: Stage 14.
- Automatic QXtend submission/import and direct database writes: prohibited.
- Named multiple layouts, speculative grid frameworks, and unmeasured optimization: Stage 22
  candidates.
- General product-wide export consistency work: Stage 21, without removing Stage 13 deliverables.
- Any change to locked Stage 9–11 algorithms.

## 12. Final reporting

At each checkpoint report:

- files changed;
- behavior delivered;
- tests and checks run with exact results;
- source/contract evidence;
- security, dependency, filesystem, and licensing impact;
- remaining limitations or owner decisions;
- confirmation that unrelated and untracked work was preserved;
- confirmation that no commit or push occurred unless separately requested.
