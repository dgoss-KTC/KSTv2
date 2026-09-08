# KST v2 -- Stage 9.8 Live-QAD Validation

**Checkpoint:** 9.8 -- Live-QAD Validation  
**Date:** 2026-09-07  
**Status:** COMPLETE / ACCEPTED — retained live-QAD validation evidence.  
**Scope:** Read-only QAD investigation, the owner-authorized KSS source correction, deterministic coverage, and documentation. No database objects, configuration, dependencies, commit, or push changed.

## A. Scope and Method

This checkpoint began source-semantic reconciliation of the accepted Stage 9 behavior recorded in:

- `docs/implementation/KST_v2_STAGE_9_SOURCE_MAPPING.md`, including the owner-adopted hybrid allocation model.
- `docs/implementation/KST_v2_STAGE_9_SCENARIO_COVERAGE_AUDIT.md`.
- The accepted Stage 7R planning-window contract and Stage 8 effective-BOM behavior.

The source was the live `QADPRO2` environment on the configured QAD server, using Windows-integrated authentication and the established read-only convention (`SELECT` statements with `READ UNCOMMITTED` isolation). No credentials, connection string, customer data, or vendor data are recorded here. No production writes, DDL, stored procedures, or persistent database objects were used.

The approach was deliberately representative and bounded: selected fields only, `TOP` limits for row samples, a single mapped domain/site (`KTC` / `SW`) for the initial sample, and a 60-second command limit. Results were evaluated against the accepted mappings; unusual data was not treated as a reason to redefine a rule.

The initial investigation stopped after a follow-up, ostensibly bounded `TOP (5)` exploratory actual-material query exceeded the 60-second command limit. The first two read-only statements completed successfully; the timeout prevented a safe conclusion that the remaining planned validation queries were operationally reasonable in the live environment.

The owner accepted that stop and authorized a resumed, key-driven pass using supplied exact WOIDs and part numbers. The resumed pass did not repeat the broad exploratory shape. It identified a KSS/null-PO presentation defect, which was resolved only after the owner/QAD expert established the independent effective supplier-schedule source in section H. No broader Stage 9.8 investigation was resumed.

## B. Validation Matrix

| Family | Accepted mapping or rule | Representative evidence collected | Relevant QAD tables / fields | Classification | Qualifier / disposition |
| --- | --- | --- | --- | --- | --- |
| 1. Actual requirement authority | R, A, and E with type F use actual `wod_det`; component, operation, required, issued, UOM, and master P/M come from the accepted `wod_det` / `pt_mstr` joins. EA is floored per line before issued subtraction; non-EA decimals remain meaningful; remaining demand clamps after overissue. | A `TOP (12)` `KTC` / `SW` sample returned actual `R` rows with component, `wod_op`, `wod_qty_req`, `wod_qty_iss`, `pt_um`, and `pt_pm_code`. A 38,979-row aggregate over the same accepted-authority population observed 4,125 fractional EA rows, 1,412 fractional non-EA rows, and 10,331 rows with issued quantity above required quantity. The initial bounded R/A/E+F selection completed in 4,094 ms. | `wo_mstr.wo_status`, `wo_type`, `wo_lot`; `wod_det.wod_part`, `wod_op`, `wod_qty_req`, `wod_qty_iss`; `pt_mstr.pt_um`, `pt_pm_code` | CONFIRMED WITH QUALIFIER | The evidence confirms that fractional EA, fractional non-EA, and overissue are real source conditions, supporting the accepted calculation treatment. This pass did not independently establish the historical source reason for scrap already being represented in `wod_qty_req`; no source-semantic contradiction was observed. |
| 2. Projected requirement authority | Ordinary E, F, and P use projected BOM authority. `BuildQty = max(wo_qty_ord - wo_qty_comp - wo_qty_rjct, 0)`. Effective BOM date is usable WO release date, otherwise Today, independent of UI Due/Release selection. | The initial `TOP (12)` query over unfinished E/F/P WOs returned ordinary E rows with ordered, completed, rejected, computed build quantity, release date, and due date. The observed sample had populated release dates and `BuildQty` equal to ordered minus completed minus rejected. | `wo_mstr.wo_status`, `wo_type`, `wo_qty_ord`, `wo_qty_comp`, `wo_qty_rjct`, `wo_rel_date`, `wo_due_date`; `ps_mstr` | CONFIRMED WITH QUALIFIER | The TOP ordering yielded ordinary E examples before F/P examples. The fallback-to-Today case, F/P source records, and effective-BOM expansion could not be safely pursued after the timeout. No contradiction was observed. |
| 3. Effective BOM / phantom behavior | Release-date-effective Stage 8 BOM source; phantoms traverse but do not become final shortage rows; manufactured structural rows remain; repeated physical requirements may consolidate; component UOM is `pt_mstr.pt_um`. | Not queried after the stop condition. Existing Stage 8/9 implementation and deterministic coverage are not substituted for new Stage 9.8 live evidence. | `ps_mstr`, `pt_mstr.pt_um`, `pt_mstr.pt_phantom`, `ptp_det` | NOT OBSERVED IN AVAILABLE SAMPLE | Requires a reviewed, performance-safe representative BOM query plan before resuming. |
| 4. Hard/detail allocation | `lad_dataset = 'wod_det'`; WOID = `lad_nbr`; operation = `lad_line`; component = `lad_part`; quantity = `lad_qty_all`; site/domain and lot/location identify the reservation. Hard allocations can exist without `wod_det`, are not window-bounded, remain in physical QOH until issue, and are removed from free inventory rather than physical QOH. | Owner-supplied WOID `32505271` has an exact `KTC` / `SW` `wod_det` allocation row: component `354075-1-ALT`, operation `50`, location `jh400`, lot `32439225`, `lad_qty_pick = 8`, and `lad_qty_all = 0`. The exact five-key correlation returned the matching `wod_det` row with WOID `32505271`, operation 50, same component, required 250, issued 90; its physical lot QOH was 8 in STOCK/nettable inventory. Exact positive-`lad_qty_all` reads for both supplied committed WOIDs (`32505271` and `32505464`) returned no rows. | `lad_det.lad_dataset`, `lad_nbr`, `lad_line`, `lad_part`, `lad_site`, `lad_domain`, `lad_loc`, `lad_lot`, `lad_qty_all`, `lad_qty_pick`; `ld_det.ld_qty_oh`; `wod_det` | CONFIRMED WITH QUALIFIER | This confirms the accepted WOID/operation/component/domain/site/lot identity and that picked allocation remains represented by physical lot QOH. The supplied pair does not independently confirm positive current hard-allocation coverage, another-WO allocation, outside-window allocation, or a hard row without current `wod_det`. |
| 5. Inventory usability and context buckets | Usable quantity is positive physical QOH in STOCK, nettable, non-RMA, and not expired/expiring at `ExpirationDate <= Today + IssueDays`. Transit, Inspection, Non-Net, MRB, NCM Inspection, RIP, RMA, and expiration remain non-covering context as accepted. Safety stock is ignored. | Owner-supplied part `188A51050` has two positive, non-expiring `KTC` / `SW` STOCK/nettable lots (13 and 2) and one positive TRAN/nettable lot (170). The `KTC` / `SW` issue-days value is 1. | `ld_det.ld_qty_oh`, `ld_lot`, `ld_expire`; `loc_mstr.loc_status`; `is_mstr.is_status`, `is_nettable`; `icc_ctrl.icc_iss_days` | CONFIRMED WITH QUALIFIER | STOCK/nettable usable and Transit contextual source states are confirmed. Inspection, Non-Net, MRB, NCM Inspection, RIP, RMA/RA, and expiry-threshold examples were not observed before the KSS contradiction stop. |
| 6. Reconstructed committed R-to-A ordering | Site-wide immediate-window committed allocation is R before A. Falldown and Forward-Due order due date then WOID; Forward-Release orders release date then WOID. It is independent of parent pegging. | The supplied pair is a shared-component committed population at `KTC` / `SW`: WOID `32505271` is R and WOID `32505464` is A. Both have Due Date 2026-09-07 and Release Date 2026-09-04; 11 common components were returned by the exact two-WOID material query, including purchased component `89790-1`. | `wo_mstr.wo_status`, `wo_due_date`, `wo_rel_date`, `wo_lot`, `wo_site`, `wo_domain`; `wod_det` | CONFIRMED WITH QUALIFIER | The source records support the R-before-A tiering input and WOID tie-break requirement for equal dates. The supplied pair does not demonstrate Falldown placement or a differing Due-versus-Release ordering. |
| 7. Uncommitted / advisory WOs | Eligible uncommitted WOs evaluate independently against the common residual free usable pool and do not sequentially consume one another; results are advisory and non-additive. | The projected initial selection observed ordinary E rows, but it did not test residual-pool behavior across WOs. | `wo_mstr.wo_status`, `wo_type`; Stage 9 inventory and allocation sources | NOT OBSERVED IN AVAILABLE SAMPLE | Live source records alone do not establish the accepted non-additive application behavior; a later safe comparison must use a deliberately selected small component/WOs. |
| 8. Issue-policy precedence | Site `ptp_det.ptp_iss_pol`, then master `pt_mstr.pt_iss_pol`, then true default. Informational only, never a shortage gate. | Exact `KTC` / `SW` probes for supplied and shared components returned site-present rows. Both true and false site values were observed; shared components `61281-18`, `78362`, `79135-2`, and `99188` had false site policy with true master policy, resolving false at the site level. | `ptp_det.ptp_domain`, `ptp_site`, `ptp_part`, `ptp_iss_pol`; `pt_mstr.pt_domain`, `pt_part`, `pt_iss_pol` | CONFIRMED WITH QUALIFIER | The site-present precedence is confirmed for both true and false values. No supplied key demonstrated master fallback or the true default. |
| 9. PO / KSS validation | Open quantity is `pod_qty_ord - pod_qty_rcvd`; line-level open/noncancelled qualification applies. KSS is an independent effective supplier-schedule relationship. PO number, due date, open quantity, confirmation, tracking `pod__chr06`, and KSS schedule context are informational only and never reduce `Short`. | Exact `KTC` / `SW` supplied-part query returned 29 qualifying PO lines for `134075` / `136525`: blank/open line state, positive calculated open quantity, PO number, due date, confirmation, tracking, and schedule fields. `134075` included a partially received line (5,900 ordered, 5,800 received, 100 open) with tracking; all observed schedule flags were false. The owner-approved effective-schedule predicate returned one `po_sched = 1` relationship for each vendor-managed KSS part `126615` and `105808`, even though neither had a qualifying conventional open PO. | `pod_det.pod_qty_ord`, `pod_qty_rcvd`, `pod_status`, `pod_nbr`, `pod_due_date`, `pod__chr06`, `pod_sched`, `pod_end_eff##1`; `po_mstr.po_nbr`, `po_confirm`, `po_sched`, `po_eff_to`, `po_stat` | CONFIRMED | The source distinction is now reconciled. KSS no longer depends on the conventional positive-open PO query; normal KSS without a conventional PO is represented as KSS, not `NO PO`. Conventional PO context remains available for an exceptional KSS PO. |
| 10. `po_mstr.po_stat` investigation | A master-status predicate must not be introduced unless live evidence establishes reliable qualifying and excluded values beyond the accepted line-level qualification. | The 29 supplied clearly open PO-line examples for `134075` / `136525` all had blank `po_stat`, blank/open `pod_status`, and positive open quantity. No owner-approved closed PO identifier was supplied; no broad closed-status discovery was performed. | `po_mstr.po_stat`; correlated `pod_det.pod_status`, quantities, and dates | OWNER/QAD EXPERT DECISION REQUIRED | Blank `po_stat` is associated with the observed open lines, but one state cannot establish a reliable inclusion/exclusion predicate. Retain the current line-level predicate; no master-status rule is proposed. |
| 11. Cross-site / domain boundaries | Stage 9 readers retain domain/site predicates, particularly for `ptp_det`, `wod_det`, `lad_det`, `ld_det`, and PO sources. | Every resumed WO, allocation, inventory, policy, and PO read used supplied exact identities plus explicit `KTC` / `SW` predicates where applicable. A keyed `icc_ctrl` comparison returned issue days independently for `KTC` / `SW` and `KTV` / `KV`, both 1. | Domain/site columns in the named Stage 9 source tables; `icc_ctrl.icc_domain`, `icc_site` | CONFIRMED WITH QUALIFIER | Query predicate and separately returned control records confirm scoped source access for the sampled paths. The supplied records do not establish an enterprise-wide non-leakage proof. |
| 12. Query practicality / performance | Stage 9 source queries must be operationally reasonable against live QAD; unsafe behavior is reported, not tuned in production. | Resumed exact-key queries completed in 45-6,749 ms: WO header 367 ms (2 rows), shared two-WOID material 1,448 ms (22 rows), exact allocation identity 275 ms (1 row), exact allocation-to-lot/material correlation 4,362 ms (1 row), supplied inventory 86-87 ms (3 rows), issue days 75 ms (2 rows), policy 45-90 ms (5/11 rows), KSS 166/818 ms (2 rows), supplied PO lines 158 ms (29 rows), and supplied WO `32505464` positive-allocation lookup 6,749 ms (zero rows). The initial `32505271` positive-allocation lookup took 18,990 ms and returned zero rows. | Keyed reads over the named Stage 9 tables | CONFIRMED WITH QUALIFIER | The resumed key-driven shapes completed below the command limit and are materially safer than the prior broad exploration. The 18,990-ms zero-row positive-allocation lookup remains a notable qualifier; it is not close enough to the implemented site-wide hard-allocation reader to prove that reader's live performance. |

## C. PO Master-Status Findings

The resumed exact-part open-PO query returned 29 clearly qualifying PO lines for supplied parts `134075` and `136525`. Every observed `po_mstr.po_stat` value was blank, with blank/open `pod_det.pod_status` and positive calculated open quantity. This is evidence that blank `po_stat` occurs with active conventional PO lines in this sample.

No master-status predicate can safely be proposed. The owner supplied no closed PO identifier, and no broad discovery query was permitted or run. The accepted current behavior remains the line-level qualification in `pod_det`: positive `pod_qty_ord - pod_qty_rcvd`, with cancelled/closed line states excluded.

## D. Contradictions

The resumed owner-supplied KSS evidence initially contradicted the Stage 9 implementation behavior, not the accepted business clarification: vendor-managed parts `126615` and `105808` had no qualifying conventional PO rows, while the service mapped every null PO result to `NO PO`. The owner/QAD expert subsequently established the independent effective supplier-schedule source, and the authorized KSS blocker correction resolved that presentation/source-classification defect. It did not change shortage arithmetic.

No unresolved live source-field meaning contradicts an accepted Stage 9 mapping.

The actual-authority aggregate observed conditions that the accepted calculations explicitly handle: fractional EA lines, fractional non-EA lines, and overissued lines. These conditions are confirmations of the need for the accepted normalization and remaining-demand treatment, not contradictions.

## E. Not-Observed Scenarios

The following were not observed in the available Stage 9.8 sample because the checkpoint stopped before their query families could execute:

- Actual E with `wo_type = F` as a separately documented representative row and scrap-specific source evidence for `wod_qty_req`.
- Projected F/P, missing/unusable release date, and zero-clamped build quantity.
- Effective BOM selection, phantom traversal, manufactured structural rows, and repeated-component consolidation.
- Positive own, other-WO, outside-window, and no-current-`wod_det` `lad_det` hard reservations.
- Inventory context categories beyond STOCK/Transit, expiry-threshold boundaries, RIP, and RMA/RA lots.
- Falldown and differing Due-versus-Release ordering; independent advisory-pool examples.
- Issue-policy master fallback and true-default cases.
- A conventional KSS bridge-buy PO and a closed PO `po_mstr.po_stat` correlation.
- Enterprise-wide cross-site/domain comparison.

## F. Stage 9.9 Handoff Items

No implementation or reconciliation work begins from this checkpoint.

1. `po_mstr.po_stat` remains an owner/QAD-expert evidence question. A closed PO identifier or another approved highly selective key is required before a master-status predicate could be considered.
2. The resumed key-driven query shapes were practical, but any future live validation must remain selective. Do not revisit the timed-out exploratory shape.
3. If the owner authorizes broader Stage 9.8 resumption, limit it to the explicitly not-observed cases above. Do not add a `po_mstr.po_stat` predicate unless accepted evidence establishes the rule.

## G. Resumed Key-Driven Evidence -- 2026-09-07

The owner/QAD expert supplied WOIDs `32505271` and `32505464`; inventory part `188A51050`; KSS/vendor-managed parts `126615` and `105808`; and conventional open-PO parts `134075` and `136525`. Every resumed query used one of those keys, with domain/site predicates where the source shape required them. No `TOP` query was used as a surrogate bound.

| Purpose | Supplied key / predicate | Rows | Elapsed time | Result |
| --- | --- | ---: | ---: | --- |
| Committed WO header | WOIDs `32505271`, `32505464` | 2 | 367 ms | R and A respectively; same Due and Release dates. |
| Shared material | The same two WOIDs | 22 | 1,448 ms | 11 shared components; validates a small shared committed-demand population. |
| Positive hard allocation | WOID `32505271`, `lad_dataset='wod_det'`, positive `lad_qty_all` | 0 | 18,990 ms | No current positive hard allocation for the supplied WO. |
| Allocation identity | WOID `32505271` | 1 | 275 ms | `lad_nbr`/`lad_line`/`lad_part` identify the current picked allocation. |
| Allocation correlation | Exact domain/site/WOID/operation/component/location/lot | 1 | 4,362 ms | Matches `wod_det`; physical QOH equals picked quantity. |
| Inventory state | Part `188A51050` | 3 | 87 ms | STOCK/nettable and TRAN states observed. |
| Issue days | Exact `KTC`/`SW` and `KTV`/`KV` keys | 2 | 75 ms | Both returned issue days of 1. |
| Issue policy | Five supplied part keys at `KTC`/`SW` | 5 | 90 ms | Site and master policy present/true for each. |
| KSS conventional-PO context | Parts `126615`, `105808` at `KTC`/`SW` | 2 | 166 ms | No qualifying conventional PO rows for either vendor-managed KSS part. |
| Conventional PO context | Parts `134075`, `136525` at `KTC`/`SW` | 29 | 158 ms | Positive open quantities, blank/open line status, confirmation, tracking/scheduling fields, and blank master `po_stat`. |
| Related positive hard allocation | WOID `32505464`, `lad_dataset='wod_det'`, positive `lad_qty_all` | 0 | 6,749 ms | No current positive hard allocation for the supplied A WO. |
| Complete supplied inventory population | `KTC`/`SW`, part `188A51050`, positive QOH | 3 | 86 ms | The existing two STOCK/nettable and one TRAN records are the full supplied-part sample. |
| Site-policy precedence | The 11 known shared components at `KTC`/`SW` | 11 | 45 ms | Site policy overrides observed true master policy for four false site-policy components. |

## H. KSS Blocker Correction -- 2026-09-07

The project owner and QAD expert established that KSS/vendor-managed classification is independent of conventional PO open-quantity qualification. Its authoritative source is an effective `pod_det` part/site schedule relationship joined to effective supplier-scheduled `po_mstr`:

- matching domain, site, and part;
- `(pod_end_eff##1 IS NULL OR pod_end_eff##1 >= Today)`;
- matching `po_mstr` domain and PO number;
- `po_sched = 1`; and
- `(po_eff_to IS NULL OR po_eff_to >= Today)`.

The SQL Server mirror maps Progress `pod_end_eff[1]` to `pod_end_eff##1` (`datetime`). `po_eff_to` is `datetime` and `po_sched` is `bit`. The data-map Markdown, JSON, and YAML records were updated with the two newly established effectivity fields.

The exact `KTC` / `SW` read-only confirmation returned one effective supplier-scheduled relationship for each supplied part, `126615` and `105808`, in 818 ms. Both used supplier-schedule PO `FUTKSS`; both effective-to fields were null and `po_sched` was true. The query deliberately did not apply positive conventional open quantity or conventional PO-line-state predicates.

The correction adds an independent Application KSS reader and QAD `QadKssScheduleReader`. For each purchased Short row, the service reads KSS status and the existing next qualifying conventional PO concurrently. The latter's positive-open-quantity formula and line-status qualification remain unchanged. A null conventional PO now becomes `NO PO` only when the independent KSS read is false. A true KSS read retains KSS with nullable conventional PO fields; when a qualifying exceptional conventional PO exists, its fields are retained together with KSS.

The existing public contract already independently expresses `isKss` and nullable conventional PO fields, so no API/OpenAPI/generated TypeScript change was necessary. The existing frontend KSS presentation was minimally corrected to show conventional PO details whenever an actual PO number is present, including an exceptional KSS PO.

Direct deterministic coverage now verifies non-KSS plus PO, non-KSS plus no PO, KSS plus no conventional PO, KSS plus conventional PO, and unchanged OnHand, Unknown/Data Issue, and Manufactured no-inference behavior. The reader SQL-shape test verifies domain/site/part scoping, effective schedule predicates, `po_sched = 1`, and absence of conventional-PO open-quantity or line-status qualification.

## I. Final Stage 9.8 Disposition -- 2026-09-08

The authorized final exact-key pass did not reveal an unresolved contradiction. It did not broaden the sample beyond the supplied committed WOIDs, their already identified shared components, and the supplied inventory part.

Final validation-family classifications are: one CONFIRMED (PO/KSS); eight CONFIRMED WITH QUALIFIER (Actual authority, Projected authority, hard/detail allocation, inventory usability/context, committed R-to-A ordering inputs, issue-policy precedence, cross-site/domain scoping, and query practicality); two NOT OBSERVED IN AVAILABLE SAMPLE (effective BOM/phantom behavior and advisory/uncommitted source records); one OWNER/QAD EXPERT DECISION REQUIRED (`po_mstr.po_stat`); and zero unresolved CONTRADICTED items.

The final related-WOID hard-allocation query returned no positive hard-allocation rows for `32505464` in 6,749 ms. Along with the prior `32505271` zero-result read, the supplied committed pair cannot provide a live positive-current, another-WO, outside-window, or no-current-`wod_det` hard-allocation example. The prior accepted hard-allocation mapping is not challenged.

The complete positive-QOH sample for `188A51050` remained exactly two STOCK/nettable lots totaling 15 and one TRAN/nettable lot totaling 170; no additional accepted context bucket or expiry/RMA case was present. The exact shared-component policy query added direct site-precedence evidence: four parts had false `ptp_det.ptp_iss_pol` and true `pt_mstr.pt_iss_pol`, with the resulting policy false. Master fallback and default remain unobserved.

The supplied R/A pair continues to establish R before A and equal-date WOID tie-break inputs only. It does not establish Falldown or a differing Due-versus-Release ordering. No exact related projected WO/BOM key or uncommitted WO key was available, so those families remain honestly not observed. `po_mstr.po_stat` remains unresolved pending an owner/QAD-expert-approved closed-PO key; no predicate is proposed.

This completes Stage 9.8 evidence collection. Any future work is limited to owner-directed reconciliation of the explicitly listed gaps; it must not resume broad live-QAD exploration.

## Verification

- QAD activity performed for this checkpoint, including the resumed pass and KSS confirmation, was read-only: `SELECT` statements plus `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED`.
- The final closeout pass used only exact supplied WOIDs/parts and already identified shared components; no `TOP`-bounded discovery query or broad enterprise scan was used.
- The KSS blocker correction changed only the independent KSS source reader, Application orchestration, dependency injection, direct tests, the existing KSS/PO display condition, and source-validation documentation.
- No Stage 9 shortage arithmetic, hard allocation, residual R-to-A, advisory pool, P/M, issue-policy, inventory-usability, conventional PO open-quantity, or `po_mstr.po_stat` qualification behavior changed.
- No Stage 9.9, Stage 9.10, or later implementation work began.
- Existing uncommitted Stage 9 work was preserved.
- Repository verification after this documentation change: inspect intended diff, `git diff --check`, and `git status`.
