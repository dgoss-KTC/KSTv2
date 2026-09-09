# KST v2 -- Pre-Stage-10 Customer Workspace UI Foundation Closeout

**Status:** COMPLETE / ACCEPTED
**Owner acceptance:** Accepted first-pass review
**Classification:** Inter-stage UI-foundation update; not a numbered Stage 10 checkpoint

## Purpose and Scope

This update restructures the customer/workspace UI shell after the accepted Stage 9 Dashboard
workflow so later scheduling modules have a clear workspace-level location. It is frontend-only.
Stage 9 remains **COMPLETE / ACCEPTED / LOCKED**.

## Accepted Behavior

- A customer/workspace header appears directly below the workspace tabs and displays the workspace
  display name and `Active Parts`.
- `Active Parts` is sourced from the active workspace MPS snapshot's
  `resolvedParentPartCount`; it is not separately calculated or hard-coded.
- Customer-level module navigation provides Dashboard, Planning, Component Orders, and Finished
  Goods.
- Dashboard retains the accepted MPS grid, selected-parent, Part Info, BOM, Work Orders, Shortages,
  selected Work Order, Escape, and focus-restoration workflows. Its MPS state is shared with the
  workspace header without duplicate MPS requests.
- Planning, Component Orders, and Finished Goods are intentionally unavailable module surfaces with
  no invented business data, rules, sample rows, or workflow behavior.
- The bottom status bar is the single application connection-status location. The duplicate
  top-right backend-status area is removed.
- The bottom-right status item displays a state-colored dot beside `Backend: <state>`: connected is
  green with the established subtle glow, starting/waiting is muted neutral, and unavailable/error
  is red.
- When workspace configuration has a warning, the bottom status item uses an amber dot, retains the
  backend state text, shows `Configuration warning`, and exposes the full warning through its title
  tooltip.
- The global bottom Refresh, Snapshot, and Last successful refresh controls remain removed because
  they represented unrelated system-wide status rather than the active workspace MPS snapshot.

## Non-Goals and Stage Boundary

This update does not start, implement, or complete Stage 10. Stage 10 field discovery,
purchase-order rules, API contracts, QAD mappings, Component Orders data population, filtering,
ordering, buyer notes, PO coverage, and detail workflow remain unstarted and require separate
authorization.

## Affected Frontend Areas

- The application shell composes a customer-workspace container and passes configuration-warning
  visibility to the bottom status item.
- The customer-workspace container owns the active workspace MPS state and customer module
  selection.
- The MPS Dashboard consumes that shared state while retaining its established drill-down ownership.
- The top application bar contains branding, subtitle, and version only; the bottom status bar owns
  connection and configuration-warning presentation.

## Verification

- Focused component coverage verifies customer-header Active Parts behavior, module switching without
  a duplicate MPS request, Dashboard selection preservation, refresh-driven Active Parts updates,
  connection-dot states, top-bar status removal, and configuration-warning tooltip visibility.
- `npm run typecheck` passed.
- `npm run lint` passed.
- `npm test` passed: 17 test files, 316 passing tests, 3 existing skipped tests.
- `npm run build` passed.
- `git diff --check` passed.

## Acceptance Statement

The customer-workspace UI foundation and backend-status-indicator cleanup were accepted through the
first-pass owner review. This record documents that accepted inter-stage UI update only; it does not
alter Stage 9's locked status or authorize Stage 10 work.
