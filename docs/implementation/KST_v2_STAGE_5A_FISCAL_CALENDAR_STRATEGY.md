# KST v2 — Stage 5A Frontend Fiscal Calendar Strategy

**Status:** Accepted strategy  
**Stage:** 5A — Data Inventory and Data Strategy  
**Capability:** MPS fiscal planning/display calendar

---

## 1. Purpose

KST needs fiscal year, fiscal week, fiscal period, and fiscal quarter information for planning headers and display.

QAD does not own these concepts for KST, and the backend does not need them to query or aggregate MPS data. Fiscal-calendar semantics are therefore a **frontend-only planning/display concern**.

The design must avoid annual source-code maintenance.

---

## 2. Authoritative anchor

The initial known-good anchor is:

```text
Fiscal Year: FY26
Fiscal Year Start: Sunday, June 29, 2025
```

KST generates later fiscal-year boundaries from this anchor.

A normal fiscal year contains 52 weeks. A configured 53-week exception contains 53 weeks and shifts the start of all subsequent generated fiscal years by one week.

---

## 3. Standard fiscal pattern

The standard fiscal year uses twelve periods in a 4-4-5 pattern:

```text
Q1: 4, 4, 5
Q2: 4, 4, 5
Q3: 4, 4, 5
Q4: 4, 4, 5
```

Total: 52 weeks.

Quarter mapping is deterministic:

```text
P1-P3   -> Q1
P4-P6   -> Q2
P7-P9   -> Q3
P10-P12 -> Q4
```

Business/calendar weeks run Sunday through Saturday. The MPS displays Monday as the visible week label.

---

## 4. 53-week fiscal years

A 53-week year is an exception to the standard pattern.

The application should persist the business fact, not a hard-coded twelve-element period array:

```text
FiscalYearException
- FiscalYear
- ExtraWeekPeriod   // 1 through 12
```

Example:

```text
FY27
Extra week -> Period 4
```

The frontend derives that year's actual period lengths by adding one week to the selected standard period.

If the business later moves the extra week to another period, the user changes the exception setting. No code release is required.

---

## 5. Settings UI

For the current application, add a **Fiscal Calendar** section to the existing Settings page.

Do not spend Stage 5B effort reorganizing all Settings navigation yet. A Calendar tab/sub-menu can be introduced later if the final settings surface becomes large enough to justify it.

Suggested settings content:

```text
Fiscal Calendar

Anchor fiscal year:      FY26
Anchor start date:       Jun 29, 2025
Standard pattern:        4-4-5

53-Week Exceptions
---------------------------------
Fiscal Year   Extra Week Period
FY27          Period 4

[ + Add Exception ]
```

The anchor should normally be protected from accidental editing or require an explicit edit action because changing it redefines every generated fiscal year.

---

## 6. Configuration model

Conceptual frontend settings contract:

```text
FiscalCalendarSettings
- AnchorFiscalYear: number
- AnchorStartDate: string       // ISO local date, e.g. 2025-06-29
- Exceptions: FiscalYearException[]

FiscalYearException
- FiscalYear: number
- ExtraWeekPeriod: number
```

The standard 4-4-5 pattern is application behavior, not year-by-year configuration.

The settings should be persisted through the application's normal local settings mechanism. The fiscal calculation itself remains frontend-owned.

---

## 7. Calendar generation

For a given fiscal year, the frontend determines its start by walking from the known anchor:

```text
Normal year     -> next start = current start + 52 weeks
53-week year    -> next start = current start + 53 weeks
```

Only exception years require user maintenance.

This avoids a dictionary containing one entry for every year and eliminates annual developer/code-maintainer updates.

---

## 8. Fiscal date resolution

For any calendar date or MPS week, the frontend derives:

```text
FiscalDisplayInfo
- FiscalYear
- FiscalWeek
- FiscalPeriod
- FiscalQuarter
```

Conceptual service:

```text
FiscalCalendarService
- getFiscalYear(date)
- getFiscalYearStart(fiscalYear)
- getFiscalWeek(date)
- getFiscalPeriod(date)
- getFiscalQuarter(date)
- getPeriodWeekSpan(fiscalYear, period)
- getFiscalDisplayInfo(date)
```

The service uses the configured anchor + exceptions as its only fiscal-calendar authority.

---

## 9. No backend fiscal contract

The following fields must **not** be added to backend `MpsSourceRow` or `MpsBucket` solely for display:

```text
FiscalYear
FiscalWeek
FiscalPeriod
FiscalQuarter
FiscalPeriodSpan
```

The backend returns ordinary schedule weeks. The frontend overlays fiscal metadata when rendering headers and planning bands.

This keeps QAD integration and backend business logic independent from a company-specific display calendar.

---

## 10. User-maintenance philosophy

The design intentionally distinguishes routine years from exceptional years:

- routine 52-week years require no user action,
- a 53-week year requires one settings entry,
- the settings entry also identifies which period receives the extra week,
- no source-code modification is required.

Because 53-week placement is a business decision and can move between periods, the application should not try to infer the extra period automatically.

A future enhancement may provide a multi-year preview or review warning when generated fiscal-year starts appear to be drifting from the company's normal early-July convention, but such a warning must not silently invent an exception.

---

## 11. Validation cases

Stage 5B frontend tests should include:

1. FY26 resolves to a start date of Sunday, June 29, 2025.
2. A standard year produces 52 weeks and period lengths 4-4-5 × 4.
3. A configured 53-week year produces exactly 53 weeks.
4. The selected exception period receives the additional week.
5. Periods after the extra week shift by one week within that fiscal year.
6. The next fiscal year's start shifts by one week after a 53-week year.
7. Quarter boundaries continue to follow P1-P3 / P4-P6 / P7-P9 / P10-P12.
8. Sunday and Saturday resolve to the same business week.
9. MPS Monday labels resolve to the intended Sunday-Saturday fiscal week.
10. Fiscal calculations remain stable across a 72-week MPS horizon.
11. Editing an exception updates generated future fiscal boundaries without any backend refresh or QAD query.
12. Fiscal metadata is absent from backend MPS API payloads.

---

## 12. Stage 5A disposition

Fiscal calendar source/ownership is resolved:

> KST owns the fiscal planning/display calendar in frontend settings. FY26 beginning June 29, 2025 is the anchor. Standard years are generated automatically with 4-4-5 × 4. Users maintain only exceptional 53-week years and specify the period that receives the extra week. The backend and QAD remain fiscal-calendar agnostic.

