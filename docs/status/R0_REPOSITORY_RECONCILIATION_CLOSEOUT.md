# KST v2 — R0 Repository / Documentation Reconciliation Closeout

**Status:** COMPLETE / ACCEPTED — 2026-08-21. R0 verification and owner acceptance are complete.

## Purpose

R0 reconciled repository documentation after Stage 8 and UI Navigation & Keyboard Ergonomics A
completed, to:

- establish clear current-state authority (what is true now, and what comes next);
- preserve historical evidence without letting it masquerade as current authority;
- prepare a clean, internally-consistent documentation baseline before Security Foundation
  (S0) integration begins.

## Accepted scope

- **R0.1** — Read-only repository documentation inventory.
- **R0.2** — Documentation authority and disposition planning.
- **R0.3** — Core current-state reconciliation (`docs/status/CURRENT_PROJECT_STATUS.md`,
  `KST-v2-Master-Project-Checklist.md`).
- **R0.4** — Stage-history reconciliation (historical labeling, root `PLAN.md` relocation).
- **R0.5** — Architecture/development documentation reconciliation
  (`docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`).
- **R0.6** — Data/QAD reconciliation (`docs/data/qadpro2-data-map.*`), plus a follow-up
  correction to the Stage 6 `pt_mstr`/`ptp_det` source model.
- **R0.7** — Documentation navigation/authority integration (`AGENTS.md`, `README.md`).
- **R0.8** — This final verification/closeout pass.

## Major durable outcomes

- Stages 1–8 and UI Navigation & Keyboard Ergonomics A are consistently represented as
  COMPLETE/ACCEPTED across the canonical current-state documents.
- Stage 8 is consistently documented as an **informational** Component/BOM investigation
  capability; material-requirement/netting/coverage capability is explicitly recorded as
  deferred future work, not unfinished Stage 8 acceptance scope.
- The two frozen historical checklist copies under `docs/reference/` were labeled as historical
  snapshots with pointers to their current canonical equivalents, without rewriting their bodies.
- The generic root `PLAN.md` (a completed Stage 8D.4 implementation plan) was relocated to
  `docs/implementation/KST_v2_STAGE_8D4_BOM_FRONTEND_PLAN.md` via a git-tracked rename.
- `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md` now documents Stage 7 (Work Orders/Kitting)
  and Stage 8 (BOM/Component Information/Approved Alternates) backend boundaries.
- The Stage 6 `pt_mstr`/selected-site `ptp_det` field-sourcing model was corrected in
  `KST_v2_STAGE_6_PART_INFO_CONTRACT.md` and the master checklist to match the actual accepted,
  implemented, and tested query.
- `loc_mstr` (the shared inventory join table) and `in_price.inp_source` (the accepted QCTC
  filter column) were added to all three synchronized QAD data-map representations.
- `AGENTS.md` now states an explicit six-tier documentation authority model.
- `README.md` gained a concise Documentation Map answering the standard orientation questions.
- `docs/architecture/API_CONTRACT_WORKFLOW.md`'s per-stage endpoint sections were clarified as
  milestone/example documentation, not an exhaustive current endpoint inventory.

## Authority model

See `AGENTS.md` §1 ("Authority and Project Memory" / "Documentation Authority Tiers") for the
full six-tier model. It is not duplicated here.

## Verification result

R0.8 performed a cumulative diff review (all 16 tracked changes attributed to an accepted R0
checkpoint; no unexplained changes found) and targeted contradiction sweeps covering: project
stage/status consistency, Stage 8 scope (accepted-capability and deferred-capability wording),
Stage 6 `pt_mstr`/`ptp_det` sourcing (including Revision), Stage 7 (open-quantity derivation,
`RMABOM`, bounded candidate navigation), navigation/Escape wording, workspace-scope assumptions
(customer code/IOS code), backend-language assumptions, architecture-document consistency, the
API contract workflow framing, data-map synchronization, `AGENTS.md`/README authority and
navigation wording, historical/reference labeling, root/link integrity, security-boundary
wording, and Stage 9 boundary wording.

No unresolved current-authority contradictions remain. All matches found were classified as
current-and-correct, historical/contextualized, technical identifiers, or explicitly deferred
future capability.

## Known intentional future work

```text
S0 — Security Foundation Integration (after R0 owner acceptance)
Stage 9 — Immediate Shortages (after appropriate S0 foundation work)
```

## Security status

The Security Foundation draft (`docs/reference/security/`) existed throughout R0 as unenacted
design source. R0 did not enact or remediate security policy, and no security tooling was
installed or configured.

## Baseline status

R0 verification and owner acceptance are complete. This reconciliation baseline is being
committed as the repository baseline preceding S0. S0 — Security Foundation Integration has not
begun; Stage 9 has not begun.
