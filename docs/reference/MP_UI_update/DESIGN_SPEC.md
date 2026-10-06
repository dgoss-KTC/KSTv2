# KSTv2 Master Plan (Planning) redesign: design specification

| Item | Value |
|---|---|
| Date | 2026-10-06 |
| Status | Design direction reviewed and approved by the owner (David) for implementation planning |
| Replaces | The Stage 14.5 Planning UI recorded in `UI_Export` (app `0.1.0-alpha.14`, HEAD `686a031`, working tree `13f5290`) |
| Audience | The planning agent that will write the implementation prompt, and the coding agent that will build it |
| Companion files | `prototype/master-plan-prototype.html` (interactive), `screenshots/*.png` |

## 1. How to read this package

1. This document is the specification. Where it and the prototype differ, this document wins.
2. The prototype shows layout, visual language and workflow with invented sample data. Its arithmetic is simplified. It is a design reference, not code to port.
3. The accepted Stage 14 domain rules stay authoritative for every calculation and validation. Nothing here changes a rule in `planningAdjustments.ts`, `planningWorkbookSummary.ts` or `exactDecimal.ts`. Section 13 lists existing behaviour the prototype does not draw but that must be kept.
4. Section 16 lists decisions the owner has not made. Do not treat prototype behaviour in those areas as a requirement.

## 2. Why the screen is changing

Owner feedback on the Stage 14.5 screen: it looks and operates as a basic, low-fidelity copy of the v1 spreadsheet, it feels cramped, and there is no way to tailor which models are included.

| Problem in the current screen | Response in the redesign |
|---|---|
| Headers, notes and review tables take the top half of the window. The grid region is about 484 px tall at 1600 × 1000 and 298 px at 1024 × 900. | One toolbar row and one filter row. Notes move into tooltips. The grid region is about 715 px tall at 1440 × 900. |
| Ten rows per part, so roughly one part is visible at a time. | Three rows per model in report mode, four in planning mode. Dollar rows and forecast are optional. |
| Adjustment inputs are always visible. There is no report state. | Two explicit modes. Report mode is read-only. One button enters planning mode. |
| No way to choose models or rows. | A View panel (models, rows, density), quick filter chips, and collapsible model blocks. |
| Changes that need review open a table that pushes the grid down. | A Changes panel on the right lists every staged change and never moves the grid. |
| Export does not exist yet (Stage 14.6). | A review dialog shows the exact file contents before writing the .csv. |
| Full-row colour bands carry no meaning beyond row type. | Neutral rows. Colour is reserved for meaning: red is negative inventory, purple is a staged change, blue is forecast or planned-only supply. |

## 3. Scope

**In scope**

- Presentation and workflow of the Planning module: `components/PlanningWorkbookPanel.tsx` and `PlanningWorkbookPanel.css`.
- The export review dialog and .csv writer (Stage 14.6).
- View tailoring and its local persistence.
- A stub for customer forecast import.

**Unchanged**

- Domain calculations and validation: `planningWorkbook/planningAdjustments.ts`, `planningWorkbook/planningWorkbookSummary.ts`, `shared/exactDecimal.ts`.
- API contracts and error mapping in `api/planningWorkbookApi.ts`.
- Loading, horizon, refresh and rollover logic in `hooks/usePlanningWorkbook.ts`.
- Adjustment state, undo and draft logic in `hooks/usePlanningProposals.ts`, and its ownership in `CustomerWorkspace.tsx`.
- The fiscal calendar and quarter grouping.

**Needs a contract or backend change (do not fake in the frontend)**

- Planned-only MPS marking (section 12).
- Detection of MPS records that are not dated on a Monday (section 10.4).
- The customer forecast import itself (section 11, future iteration).

**Separate, optional**

- The compacted application shell shown in the prototype (section 14).

## 4. Owner-confirmed rules

1. The screen has two states. **Report mode** only displays. A button switches to **planning mode**, where adjustments can be made.
2. Adjustments export to a .csv file for the QAD mass MPS update.
3. The file has exactly three columns: part number, due date, change quantity.
4. The file has no header row.
5. Dates are written `m/d/yyyy` with no leading zeros, for example `10/5/2026`.
6. The change quantity is a signed change to existing MPS, not a new total.
7. MPS due dates are kept on Mondays where possible. The export dates each change on the Monday of its week.
8. The Forecast row shows the forecast currently loaded in QAD. It is still needed and may be an optional row chosen from the row selector.
9. Customer forecast import is a planning aid for comparison. It is not built in this iteration, only stubbed (section 11).
10. The redesign must keep every function of the v1 Master Plan spreadsheet that v2 already carries.

## 5. Screen anatomy

From top to bottom. Sizes are from the prototype at comfortable density.

| # | Region | Height | Contents |
|---|---|---|---|
| 1 | Application bar and module tabs | existing | Not part of this work. See section 14 for the optional compaction the prototype shows. |
| 2 | Planning toolbar | about 55 px, wraps when narrow | Left: title "Master plan", mode pill, status text. Right: horizon stepper, View button, mode-specific actions. |
| 3 | Filter row | about 45 px | Filter chips on the left. |
| 4 | Grid | fills the rest | Sticky column header, model blocks, pinned totals. Scrolls in both directions. Only the grid scrolls. |
| 5 | Right panel | 320 px wide, optional | Either the View panel or the Changes panel. Sits beside the grid and takes width from it, never height. |
| 6 | Dialogs | centred overlay | Export review, export confirmation, forecast import stub. |

### 5.1 Toolbar contents by mode

| Control | Report mode | Planning mode |
|---|---|---|
| Mode pill | "Report" (neutral chip) | "Planning" (purple fill, dark text) |
| Toolbar background | panel colour | panel colour tinted purple |
| Status text | "Data from {time}" plus a refresh icon button | "{N} changes · {M} need attention · not saved / draft saved {time} / exported {time}" |
| Horizon stepper | shown | shown |
| View button | shown. Label becomes "View · 5 of 7 models" when models are hidden | shown |
| Import forecast | hidden | shown, with a "Soon" tag |
| Primary action | "Start planning". Label becomes "Resume planning · {N}" when changes are staged | "Review and export" (purple). Disabled until the staged set is valid |
| Other actions | none | Undo, "Changes · {N}" (toggles the Changes panel), "Back to report" |

## 6. The two modes

| Aspect | Report mode | Planning mode |
|---|---|---|
| Purpose | Read the plan | Change the plan and export the changes |
| Adjust row | not rendered | rendered, weekly cells are inputs |
| MPS row | loaded MPS | proposed MPS where a change exists, with the loaded value struck through beside it |
| Projected on hand row | Est. On Hand | Adj On Hand, with Est. On Hand struck through where they differ |
| Totals | loaded values | proposed values, purple where they differ |
| "Short" counts and health marks | from Est. On Hand | from Adj On Hand |
| Changes panel | hidden | available, open by default on entry |
| Filter chips | All models, Short | All models, Short, Changed |

Switching modes never discards staged changes. "Back to report" keeps them in the session, exactly as the current retention rules do, and the report shows loaded values only. Entering planning mode makes no request and changes no data.

## 7. The grid

### 7.1 Columns

- **Model column**, pinned left, 256 px. Holds the model identity and the row labels.
- **Falldown**, one column, labelled "Falldown / past due". Not adjustable. It scrolls away horizontally, as it does today.
- **Week columns**, one per week in the horizon, 76 px (68 px compact). Header shows the Monday as "Oct 5". The current week is labelled "this week", uses the accent text colour, and its whole column carries a faint accent tint.
- **Quarter total column** after the last shown week of each fiscal quarter, 22 px wider than a week column, with a strong left border. Header: "Q2 total" with "FY27 · 12 wks" beneath. The existing facts about partial quarters and "+ Falldown" move to the header tooltip.

There is one header row. The separate quarter band row is removed. Column grouping still comes from `planningQuarters` and `planningColumns`.

### 7.2 Sticky behaviour

- The column header sticks to the top.
- Each model's header row sticks directly under the column header while that model's block is scrolling, so the part number is always visible.
- The model column sticks to the left.
- The totals rows stick to the bottom of the grid.

### 7.3 Model block

**Header row** (46 px):

- Collapse or expand button (chevron).
- Part number in IBM Plex Mono, semi-bold. Description beside it, truncated with an ellipsis.
- Second line: "On hand {netQuantityOnHand} · {price}". Keep the existing price formatting and the existing amber "Unpriced" badge. The prototype's two-decimal price is not a requirement.
- Badges on the second line: "{n} wks short" in red when any visible week has negative projected on hand, and "{n} changes" in purple in planning mode.
- **Health strip:** in the header's week cells, a 3 px red bar along the bottom of every week (and Falldown) whose projected on hand is negative. This shows where the trouble is without reading numbers.
- **Collapsed state:** the block shows only its header row, and the header's week cells show the projected on hand value for each week. See `screenshots/06_collapsed_overview_1440.png`.

The corner cell of the column header has one text button that reads "Collapse all" or "Expand all".

### 7.4 Rows

Row order is fixed as listed. "Metric id" refers to `METRICS` in the current `PlanningWorkbookPanel.tsx`.

| Row label | Default | Existing metric id | Behaviour |
|---|---|---|---|
| Sales orders | on | `so` | Sales order quantity. |
| SO $ | off | `soDollars` | Sales order dollars. |
| MPS | on | `mps` merged with `proposedMps` | Report mode shows loaded MPS. In planning mode a cell with a nonzero adjustment shows Proposed MPS in purple on a purple tint, with the loaded value struck through to its left. Quarter cells follow the same rule. A negative Proposed MPS shows in red on a red tint. |
| MPS $ | off | `mpsDollars` merged with `adjustedMpsDollars` | Same merge as MPS. Unpriced shows "—" as today. |
| Forecast (QAD) | off | `forecast` | The forecast currently loaded in QAD. Display only. No quarter total. Turns on automatically while a customer forecast is loaded. |
| Customer forecast | future | new | Planning mode only. See section 11. |
| Adjust | planning mode only | `adjustment` | Weekly cells are text inputs. Falldown is blank and not editable. Quarter cells show the signed sum, for example "+50". |
| Projected on hand | on | `estimatedOnHand` merged with `adjustedOnHand` | Report mode shows Est. On Hand. Planning mode shows Adj On Hand, purple with the Est. value struck through where the two differ. Quarter cells show the balance at the quarter's last shown week. |

Each row label has a small square colour key instead of a full-row colour band: green for sales orders, amber for MPS, blue for forecast, purple for Adjust, grey for on hand.

### 7.5 Cell states

| State | Treatment |
|---|---|
| Exact zero | Blank, as today, in every numeric grid cell. In a changed cell the value still prints, so "0" appears when a change brings MPS to zero. |
| Negative projected on hand | Red text, semi-bold, red tint. Tooltip "Projected on hand is negative". Display only. It never blocks export. |
| Changed by a staged adjustment | Purple text, semi-bold. MPS cells also get a purple tint. The previous value is struck through to the left in small muted text. |
| Invalid adjustment | Red input border and text. The MPS cell shows the negative Proposed MPS in red. The tooltip carries the issue message. |
| Planned-only MPS | Blue text, tooltip "Planned order, not yet released". Needs a contract change (section 12). |
| Adjust input, empty | Dark field with a neutral border. |
| Adjust input, has value | Purple border and tint. |
| Unpriced | Unchanged from today. |

If the struck-through previous value does not fit in the cell, show the new value only and put the previous value in the tooltip.

### 7.6 Totals

Two rows pinned to the bottom of the grid: **Total SO $** and **Total MPS $**. They play the part of the frozen summary rows at the top of the v1 sheet. In planning mode Total MPS $ shows the proposed value in purple, with the loaded value in the tooltip.

The existing rule stays: when any included part is unpriced, MPS $ totals are blank, with the explanation in a tooltip instead of a permanent note. Inventory balances are never summed across parts.

In the prototype the totals cover the models currently shown and the label says so ("7 models"). Whether that is right is an open decision (section 16, item 4).

## 8. Tailoring the view

### 8.1 Filter chips

A single-select row above the grid: **All models**, **Short**, and in planning mode **Changed**. Each chip shows a count. The Short count is red when it is above zero.

- A model is "short" when at least one visible week has negative projected on hand. Falldown does not count, because it cannot be adjusted.
- A model is "changed" when it has at least one staged entry.
- Chips filter the tailored set from the View panel. They do not change it.
- If the filter leaves no models, the grid shows "No models match this filter." with a "Show all models" button.

### 8.2 View panel

Opened by the View button, as a 320 px panel on the right. Sections:

- **Models.** A search box that matches part number or description. One checkbox per model in workbook order, with a red "short" flag where it applies. Two buttons: "Show all" and "Only short models". A count reads "5 of 7 shown".
- **Rows.** Checkboxes for Sales orders, SO $, MPS, MPS $, Forecast (QAD), Projected on hand.
- **Density.** Comfortable (34 px rows, 76 px weeks) or Compact (28 px rows, 68 px weeks).

Rules:

- Hiding a model never removes its staged changes. They stay in the Changes panel and in the export.
- The View panel and the Changes panel share the right-hand slot. Opening one closes the other.

### 8.3 Horizon

A stepper (minus, "12 wk", plus) replaces the numeric input. Keep the existing rules: minimum 12, maximum from the API, stored under `kst.planningWorkbook.horizonWeeks.v1`. The prototype stops at 16 only because its sample data does.

### 8.4 Persistence

Hidden models, row choices, density and collapsed models are remembered per workspace on this computer. Suggested: `localStorage`, keyed by assignment id, following the horizon key's pattern, for example `kst.planningWorkbook.view.v1.{assignmentId}`. The active filter chip is not remembered.

## 9. Planning mode in detail

### 9.1 Adjust inputs

- All existing rules apply unchanged: signed whole numbers, optional leading `-`, no `+`, decimals, commas, spaces or exponents. Blank, `0` and `-0` are no-ops. A negative Proposed MPS is invalid. Use `parseAdjustment` and `evaluatePlanning` as they are.
- Each keystroke re-evaluates, so MPS, Projected on hand, totals, badges and the Changes panel update as the user types.
- Tab moves to the next week's input in the same row.
- Keep the existing `aria-label` ("Adjustment for {part}, week of {date}"), `aria-invalid`, and the valid-input tooltip text.

### 9.2 Changes panel

This replaces the "Adjustments needing review" table and the toolbar status sentence. It lists **every** staged change, not only the ones that need attention.

- Header: "Changes" with a count, and a close button. The toolbar's "Changes · {N}" button toggles it.
- One card per staged entry, in workbook part order and then week order:
  - Part number, and the signed change ("+50", "-50") in purple.
  - "Week of Oct 19 · MPS 0 → 50".
  - A remove button. Removing is one undo step, as today.
- An entry with an issue gets a red border, shows the change in red, and prints the issue message in red. Use the existing `ISSUE_LABELS` and `ISSUE_MESSAGES` text. The prototype's shorter messages are placeholders. Every existing issue kind must appear here: invalid number, not a whole number, negative Proposed MPS, week passed, part not in loaded MPS, outside horizon, beyond latest available week.
- Empty state: "Nothing staged yet. Type a signed quantity in any Adjust cell, such as 50 to add or -50 to pull back. Each change lands here."
- Footer:
  - "Net change": net units, and net dollars for priced parts.
  - "Save draft" and "Discard all". Discard all is the existing Clear All and is one undo step.
  - "Drafts stay on this computer. Nothing reaches QAD until you load the exported file."

### 9.3 Undo and drafts

- Undo keeps its existing step definition and history limit. It becomes a toolbar button in planning mode.
- Save draft calls the existing save. The status text shows "draft saved {time}".
- Review Saved Draft, Restore and Delete Saved Draft are not drawn in the prototype and must be kept. Suggested placement: when a saved draft exists, show "Review saved draft" and "Delete saved draft" in the Changes panel, and show the existing saved-versus-current comparison in a dialog instead of inline above the grid.

## 10. Review and export (Stage 14.6)

### 10.1 Entry condition

"Review and export" is enabled only when at least one valid nonzero adjustment exists and no staged entry has an issue. This is the existing `isValid`, subject to open decision 1.

### 10.2 Review dialog

See `screenshots/03_export_review_1440.png`.

- Title "Export MPS changes". Subtitle "{n} lines for the QAD mass MPS update".
- A preview table with one line per exported row. The column headings (Part number, Due date, Change qty) are interface labels only and are not written to the file.
- The file name.
- A note: "Due date is the Monday of the adjusted week, written m/d/yyyy. The file has no header row. Quantities are signed changes to existing MPS, not new totals."
- Buttons: Cancel, and "Export .csv".

### 10.3 File format

- One line per valid, nonzero adjustment.
- Three comma-separated fields: part number, due date, change quantity.
- No header row.
- Due date: the entry's Monday week label formatted `m/d/yyyy` with no leading zeros. The existing `formatPlanningDate` already produces this.
- Change quantity: a signed whole number. Negative values carry a leading `-`. Positive values carry no sign.
- Order: workbook part order, then week.

Example, matching the prototype's sample changes:

```
SA-1042,10/19/2026,50
SA-1042,12/14/2026,-50
HA-3310,10/5/2026,50
DS-5120,10/5/2026,30
```

The file name in the prototype, `MPS_Update_{workspace}_{yyyy-MM-dd}.csv`, is a suggestion. Line endings, encoding and where the file is saved are open (section 16, item 11).

### 10.4 Monday rule

The workbook buckets MPS by week, and the export dates every change on that week's Monday. Because due dates are kept on Mondays only "where possible", a week can hold an MPS record on another day. A decrease exported against Monday could then target a date with nothing on it. Recommended guard: flag any week that holds off-Monday MPS before export. This needs data the frontend does not have today. See open decision 3.

### 10.5 After export

A confirmation dialog: "Exported {n} lines", the file name, and "Nothing changes in QAD until you run the mass MPS update with this file. Your changes stay staged here until MPS is reloaded." Buttons: "Keep planning" and "Back to report". The status text reads "exported {time}" until the staged set changes again.

**Risk to resolve before building:** today, staged adjustments are re-applied to a replaced MPS snapshot. After the user loads the file into QAD and reloads MPS, the same changes would be applied a second time on screen and could be exported again. See open decision 2.

## 11. Customer forecast import

### 11.1 Build now: the stub

- An "Import forecast" button in the planning-mode toolbar, carrying a "Soon" tag.
- It opens a dialog titled "Import customer forecast" with a "Future iteration" tag, an inert drop zone ("Drop a forecast file here (.xlsx or .csv)", "File import is not built yet"), a short explanation, and a Close button.
- Reserve a `customerForecast` row type in the row model, positioned between Forecast (QAD) and Adjust.
- The prototype's "Preview with sample data" button exists only to demonstrate the future row. Do not ship it.

### 11.2 Future iteration: intended behaviour

Background: today the owner loads the customer's forecast into the adjustment row, then edits each value before exporting. The owner prefers the approach below, because raw forecast quantities never sit in cells that export.

- Import a customer file of weekly quantities by part.
- In planning mode only, show a **Customer forecast** row directly above Adjust for each part in the file. Parts not in the file get no row.
- Turn the Forecast (QAD) row on automatically so the two forecasts sit together.
- Highlight each week where the customer quantity differs from the loaded forecast (blue text on a blue tint). The tooltip gives both values and the difference.
- The quarter cell shows the sum.
- See `screenshots/05_customer_forecast_1440.png`.

Not decided: the file layout and column mapping, how dates map to weeks, how part numbers are matched, and whether clicking a highlighted cell should copy the difference into Adjust.

## 12. v1 parity notes

Read from the v1 generator (`master_plan.py`):

| v1 feature | Status in this design |
|---|---|
| Per-part rows: SO, SO $, MPS, MPS $, FORECAST, Est On-Hand, Adj On-Hand, ADJUSTMENT | All present. Proposed and adjusted values are merged into the MPS and on-hand rows in planning mode. |
| Falldown column, weekly columns, fiscal quarter summary columns | Present. |
| Negative values in red | Present for projected on hand. |
| Weekly summary rows frozen at the top (SO, SO $, MPS, MPS $, ADJUSTMENT) | Replaced by the pinned totals. Only the two dollar rows are drawn. See open decision 4. |
| MPS in blue when the quantity is planned orders only (Rep = 0, Planned > 0), and a cell comment "Total / Rep / Planned" when both exist. v1 folds open work orders into the Rep quantity. | Drawn in the prototype as blue text with a tooltip. The v2 bucket exposes a single `mps` value in the files reviewed, so this needs a contract change. Open decision 6. |
| "Include forecast" option | The Forecast (QAD) row toggle. |
| Roadmap block: Roadmap, Shipped, MPS Remaining, Finished Goods, Over/(Under) = Shipped + MPS Remaining + Finished Goods − Roadmap | Not in the v2 baseline and not in this design. Open decision 5. |
| Open Orders detail sheet | Covered by the Customer Open Orders module. No action. |

## 13. Existing behaviour to keep (not drawn in the prototype)

- Loading, unavailable, MPS-not-loaded, snapshot-changed and not-found states, with their existing copy and Retry.
- The stale-workbook and refresh-failed warning banners.
- Refresh Planning semantics: it also refreshes the workspace's Customer Open Orders report, it never refreshes MPS, and staged adjustments are kept. The permanent note becomes the refresh button's tooltip, with the full existing wording.
- The freshness facts (acquired time, prices as of, today). The prototype shortens these to "Data from {time}". Keep the full detail in a tooltip.
- The business-week rollover check.
- Exact-decimal arithmetic and formatting everywhere. No conversion to JavaScript numbers.
- The zero-display rule and the unpriced rules, including blank workspace MPS $ totals.
- Retention of staged entries across module switches, horizon changes, refresh, rollover and snapshot replacement.
- The unsaved-change confirmations for workspace switch, General, archive, delete, reset and window close.
- Draft review, restore, delete and the corrupt-draft state.
- Existing `data-testid` values and accessible names that tests rely on, for example `planning-change-count` and `mps-dollar-note`. Move them to the equivalent new elements instead of dropping them.
- Light theme and the accent options. The prototype is dark only. Build with the theme tokens so light theme follows, then check contrast.

## 14. Shell compaction (optional, separate decision)

The prototype also tightens the application shell around Planning:

- The application bar and workspace tabs share one row.
- "Backend connected" moves to the top bar, and the bottom status bar is removed.
- The workspace meta line ("7 active parts · MPS loaded …") moves to the right end of the module tab row, and the separate workspace header is removed.

This saves roughly 90 px of height for every module. It touches `ApplicationShell`, `TopApplicationBar`, `WorkspaceTabBar`, `BottomStatusBar` and `CustomerWorkspace`, so it is outside the Planning panel and needs its own decision. The Planning redesign does not depend on it.

## 15. Visual language

### 15.1 Colour

Use the existing tokens in `index.css`. The prototype uses the dark-theme values literally.

| Use | Token | Dark value |
|---|---|---|
| Window background | `--app` | `#0d1117` |
| Toolbar and panels | `--panel` | `#151b24` |
| Buttons and cards | `--panel2` | `#1b232f` |
| Chips | `--chip` | `#1f2935` |
| Borders: soft, normal, strong | `--bd-soft`, `--bd`, `--bd-strong` | `#1b2331`, `#222c3a`, `#324054` |
| Text: primary, secondary | `--tx`, `--tx2` | `#e8edf4`, `#94a3b6` |
| Grid background, row, Falldown column | `--grid`, `--row`, `--row-alt` | `#0f141c`, `#141a23`, `#11161e` |
| Column header, quarter column, totals | `--sticky` | `#1a212c` |
| Sales orders key | `--r-tx` | `#5ec79a` |
| MPS key | `--a-tx` | `#e0a85e` |
| Forecast, planned-only MPS, text links | `--f-tx` / `--accent-text` | `#71b6e8` |
| Staged change | `--p-tx`, `--p-bg` | `#a78bfa`, purple at about 16% |
| Negative or invalid | `--danger-tx`, `--danger-bg`, `--danger-bd` | `#ec7d74`, red at about 22%, `#bd5750` |
| Primary action (report) | `--accent-solid` | `#5b9dd9` |

Values in the prototype with no token today. Add tokens or use the nearest existing one:

- `#c3ccd8`: row labels and secondary numbers, between `--tx` and `--tx2`.
- `#161d27`: model header row, between `--row` and `--sticky`.
- `#18222f`: current-week column tint (row colour plus about 8% accent).
- `#1f2038`: planning-mode toolbar tint (panel colour plus about 10% purple).

Rules:

- Text on the accent blue and on the purple fill is dark (`#0d1117`). White on `#5b9dd9` is about 3:1 and fails contrast. The current primary button uses white.
- Do not use `--tx3` for text. It is too faint on the dark backgrounds.
- No full-row colour bands and no coloured left edges on rows.

### 15.2 Type and size

- IBM Plex Sans for the interface, IBM Plex Mono for part numbers and the file preview. The baseline noted that IBM Plex Sans is not bundled, so the app falls back to Segoe UI. Bundle both fonts with the app.
- Base text 13 px. Grid numbers 13 px with tabular numerals. Row labels 12.5 px. Captions 11.5 to 12 px. Title 17 px semi-bold.
- Toolbar controls 34 px high. Chips 28 px. Adjust inputs are the row height minus 8 px.
- Corner radius: 6 px on controls, 8 px on cards, 10 px on dialogs, 4 px on inputs.
- A visible focus ring on every button and input.

## 16. Open decisions for the owner

Prototype behaviour in these areas is a placeholder, not a requirement.

1. **Entries outside the visible horizon.** The baseline already records this as unresolved. Today they count as needing attention and block validity. The prototype lists them with a neutral note ("Beyond the weeks shown") and still exports them.
2. **Staged changes after export.** Decide how to avoid applying exported changes twice once MPS is reloaded from QAD. Options: clear the staged set on export, clear it when a new MPS snapshot arrives after an export, or ask.
3. **Off-Monday MPS guard** (section 10.4). Needs backend support.
4. **Totals.** Do they cover the models shown (prototype) or the whole workspace (current)? Are quantity totals and an adjustment total needed beside the two dollar rows?
5. **Roadmap block** from v1. Bring it back or leave it out.
6. **Planned-only MPS marking.** Needs the Rep versus Planned split in the contract.
7. **Shell compaction** (section 14).
8. **Density.** The app already has a global density preference with different sizes (32 and 42 px rows). Decide whether Planning keeps its own control, as drawn, or follows the global one.
9. **Discard all.** The prototype has no confirmation, because Undo restores it. The current Clear All confirms inline.
10. **Forecast import details** (section 11.2).
11. **File details.** Line endings, encoding, file name pattern, and whether a native save dialog chooses the location.

## 17. Prototype limits

- All parts, quantities, prices and dates are invented. The "current week" is fixed at the week of 10/5/2026, inside FY27 Q2.
- Plain integer arithmetic and whole-dollar totals. Production keeps exact decimals and the existing money formats.
- Dark theme only. Horizon limited to 12 through 16 weeks.
- Export writes no file. Drafts are not stored. Refresh only updates the time label.
- Issue messages are shortened. Only three issue kinds are simulated.
- The file contains a small template runtime (`sc-for`, `sc-if`, `{{ }}`) so it can run from one HTML file. That runtime is scaffolding. Build the real screen in the existing React and TypeScript components.
- `renderVals()` in the prototype is the quickest reference for how each displayed value, colour and state is derived.

## 18. Suggested implementation slices

Each slice leaves the app working and can be reviewed alone.

1. **Grid presentation, no behaviour change.** Single header row, neutral rows with colour keys, merged MPS and on-hand rows, sticky model headers, health strip, pinned totals, notes moved to tooltips.
2. **Modes and the Changes panel.** Report and planning modes, the toolbar by mode, the Changes panel replacing the review table, Undo in the toolbar, draft actions relocated.
3. **View tailoring.** Filter chips, View panel, collapse and expand, density, horizon stepper, persistence.
4. **Export (Stage 14.6).** Validity gate, review dialog, file writer, confirmation, and the decision on staged changes after export.
5. **Forecast import stub.**

## 19. Acceptance checklist

- [ ] Opening Planning shows report mode. No input is visible and nothing is editable.
- [ ] "Start planning" switches to planning mode without a request. Adjust rows and the Changes panel appear.
- [ ] "Back to report" hides Adjust rows and proposed values and keeps staged changes. The button then reads "Resume planning · {N}".
- [ ] With default rows, a model occupies a header plus three rows in report mode and a header plus four rows in planning mode.
- [ ] Typing a valid adjustment updates the MPS cell, every later Projected on hand cell, the totals, the model's badges and the Changes panel on each keystroke.
- [ ] An adjustment that makes Proposed MPS negative, or that is not a whole number, is marked in the cell and in the Changes panel, and "Review and export" is disabled.
- [ ] Removing a change in the Changes panel clears its input and is undoable.
- [ ] Hiding a model in the View panel keeps its staged changes in the Changes panel and in the export.
- [ ] The Short chip shows only models with a negative projected on hand in a visible week.
- [ ] Collapsing a model leaves one row that shows projected on hand per week.
- [ ] The column header, the current model's header, the model column and the totals stay visible while scrolling.
- [ ] View settings survive closing and reopening the app, per workspace.
- [ ] The exported file has no header row, three comma-separated fields per line, dates as `m/d/yyyy` without leading zeros, each dated on a Monday, and signed whole-number quantities.
- [ ] The review dialog shows exactly the lines that are written.
- [ ] Every item in section 13 still works.
- [ ] The Import forecast button opens the stub dialog and changes no data.
