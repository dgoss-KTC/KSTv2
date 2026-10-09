# Stage18-I2F — compact Component MRP workbook presentation

2026-10-08. Implemented pending owner verification; Stage18 not accepted/complete.
Application alpha.18, installer0.1.0 unchanged. I2 independent security review remains pending.

Owner-approved formatting scope: one Component MRP worksheet; grouped identity/purchasing/planning
metadata above seven calculated rows, exact selected horizon, stored rounded numeric quantities,
no projection formulas or added sheet. Ledger D25 records separate amendment provenance.

## Actual layout

Title/context row1, full component row2, wrapped description row3, group headings row4, metadata
rows5–10; weekly heading row11, seven metrics rows12–18. A:L bounded metadata merges only over
empty cells; no metric-grid merges. Full values retained; null explicitly Unavailable. Important
warning row2, acquisition/complete warning/first-week note rows20–22. Compact rows expand for long
identity/description/warnings. Existing source values/UOM rounding/QCTC precision unchanged.
Calibri11, title16, gridlines hidden, contrasting slate heading, lightly tinted balance rows, light
horizontal separators, numeric right alignment/comma formats (EA/BX/PK zero decimals, others/missing
two). QCTC#,##0.##### no currency symbol. Negative balance conditional red. ColumnA36, all bucket
columns17; freezeA and rows1–11, zoom100. First partial-week dates/meaning unchanged.

## Printing and visual evidence

Installed Excel COM rendered only synthetic generator outputs; no KST startup/live reads. Actual
Excel range PNGs and PDF exports are in I2F handoff. Initial print trial showed split merged metadata
and redundant note pages; corrected bounded print areas: metadata A1:L10, actual weekly grid
A11:last18, notesA20:L22. One worksheet, three print sections; heading11 and labelA repeat,
component/site/as-of page header, page-number footer. LandscapeA3 fixed75% avoids squeezing longer
horizons to one page and fits complete metadata/notes panels. A3 is a presentation default, may
need owner paper preference verification. Intermediate failed previews retained separately.
At final default scale grid remains horizontally paged. Representative metadata/grid PDFs and
opening/later-column PNGs inspected: full long description, visible warning/unavailable values,
QCTC .00152, decimal8.38/2.25/-3.25, red negative balances, no #### in supplied fixtures.
Excel observations record zoom100/freezepanes/splits/printareas. Range pictures are Excel renders,
not screenshots of entire desktop. Physical printer output is not verified; printer-driver pagination
can differ. Owner should inspect representative PDFs and scroll final72week workbook at100%.

## Verification

Scoped workbook tests preserve independent F02/F03/F06 arithmetic and numeric types; all seven
F02 metrics/all27bucket values exactly compared to pre-format I2 synthetic XLSX by metric name
and bucket, not row address. Additional hand-established negative/decimal first/later buckets for
13/26/52/72 and missing UOM; merged-grid exclusion, one sheet/noformula, formatting/CF coverage.
Initial compile corrected actual ClosedXML properties; CF assertions corrected for save-time range
consolidation (coverage verified). Arithmetic expectations not weakened.14 workbook tests pass.
Backend build/sidecar republish and scoped formatting logs in handoff. No contract change; OpenAPI/
generated client unchanged on comparison. No I2 native or acquisition/algorithm modifications.
Known broad .NET/Rust formatting, lockedStage10lint and AFTinterrupted gaps remain outstanding.

Done: formatting and actual synthetic rendering. Decided: compact grouped layout and nonsqueezed
horizontal printing. Open: owner presentation/paper verification, I2 security/desktop/live gates.
Next checkpoint: owner review of actual files/previews; no acceptance inferred.
