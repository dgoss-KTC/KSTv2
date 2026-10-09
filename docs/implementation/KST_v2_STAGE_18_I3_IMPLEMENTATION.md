# Stage18-I3 — implemented; review/owner verification pending

2026-10-08. alpha.18 / installer0.1.0 unchanged. Owner I3 direction supersedes earlier
seven-row/off-default/new-file-only presentation without changing locked calculations.

## Owner amendments (separate provenance)

D26: five export rows Gross Requirements, Scheduled Receipts, Projected QOH, Planned Orders Due,
Planned Orders Release. No screen freeze/split or section bands; compact metadata, actual warnings,
acquired UTC note. Projected QOH is selected-mode ending balance, excludes planned orders.
D27: standalone Include Unconfirmed POs default true, explicit false retained; amber receipt cells
use classified contributing fact presence, including net-zero signed facts. Short legend.
D28: Day/Week/Month (calendar), Week default, identical exclusive forecast end for13/26/52/72.
Day starts asof; Month intersects exact window. Flows by due/release dates, balances last included
day, aggregate raw decimals before UOM rounding. Period/mode are frozen preparation metadata.
D29: host-confirmed selected-destination overwrite authorized. New attempts remain no-replace.
Local deliberate namespace manipulation limitation retained; no universal failure/durability guarantee.
D30: single contiguous print area, USLetter landscape assumed pending owner printer verification.

## Implementation

`ComponentMrpPeriods` uses unchanged builder's classified Evidence/Past. Week uses original buckets;
Day/Month daily balances preserve demand-first and firm+confirmed(+unconfirmed) order. Unknown PO
confirmation validation remains locked builder responsibility before regrouping. No raw dedup changes.
C# request default true/Week; prepared DTO adds period/mode/end. OpenAPI generated then TS regenerated;
host transport forwards period and serializes returned context. UI option edits invalidate generation.

`component_mrp_save` captures target presence at native chooser callback. Installed rfd0.16 Windows
build_save_file leaves IFileSaveDialog options unchanged (default FOS_OVERWRITEPROMPT documented by
Microsoft). API returns path, no affirmative-consent receipt or original target-state token. Explicit
host YesNo is therefore used for an existing selected file (may prompt twice); decline cancels and
retains bytes. New-at-selection remains FALSE even if a file subsequently appears. Consent local
to this attempt; no frontend replacement flag/path. Production `publish_selected` changes only ordinary
FileRenameInfo ReplaceIfExists first byte; full exact verified/flushed temp before rename unchanged.
No target delete/direct write. Same original-handle cleanup on failure; committed path no cleanup.
Actual synthetic lock/read-only refusal preserved target bytes. Operation IDs/status/artifact identity,
preferences/retry and saved/open separation retained. No extra permissions/dependency.

## Evidence

Windows `C:\dev\kst_v2_tmp\Stage 18\I3_evidence_20261008_c31b7f02` retains full logs, hashes,
synthetic XLSX/Excel PNG/PDF. Focused copy packet separate I3_20261008_c31b7f02.
16 domain/workbook tests,3 application,5 adapter,3API,10architecture;8frontend;13Rust passed.
Projection fixtures hand-established cross-month/partial/end boundary, overdue receipts not seeded,
release versus due, empty carry, rounding .004+.004→.01, zero-net amber, daily-weekly locked equality.
Historical seven fixture arrays retained, export checks select remaining five; lowest/planning
calculation evidence unchanged. API omitted default/explicitfalse/context tests; UI late period change.
Actual native new/no-consent/precommit/lock/read-only/replacement/cleanup/artifact tests; repeated
operation evidence is retained state-level test, not chooser end-to-end automation.

Installed Excel opened only generated synthetic XLSX read-only/macrodisabled.100% renders Day/Week/
Month inspected; no panes via Excel observations. Letter65% preserves full A:I metadata on first
page, later pages repeat metricA + weekly heading + headercontext, not whole forecastfit.
Long descriptive merges can fragment on continuation with repeatedA; owner laptop/printer review
remains final presentation check. First page/sample decimal/QCTC/amber readable; no physical print.
Native chooser/host-confirm end-to-end interaction unexecuted; source confirms mechanism, synthetic
native proves publication cases, neither is owner/live acceptance. No live source or business parity.
Known broad formatting/lockedStage10lint/AFTinterrupted diagnostics remain exceptions, not passes.
Scoped checks/sidecar evidence in logs. Independent actual-source security review remains pending.
