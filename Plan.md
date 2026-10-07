# Plan

## 1. Objective

Redesign only the **Work Hours → Summary** section of the employee Schedule page at:

- Repository: `https://github.com/OlehProtsun/GF3`
- Branch: `DEV2`
- Commit: `6b35a1ab32a7bc543ca7838d13f9ab9e7e61d713`

The target is the approved compact mockup supplied by the user:

```text
╭──────────────────────────────────────╮
│ [chart] WORK HOURS          [208.5h] │
│         Summary                      │
│                                      │
│ [📅 July       ▼] [📅 2026       ▼] │
│                                      │
│ ┌──────────────────────────────────┐ │
│ │ DAY       HOURS       SCHEDULE   │ │
│ │ we./01    9.5h        8989       │ │
│ │ th./02    15h         8989       │ │
│ │ fr./03    14h         8989,...   │ │
│ │ ...                              │ │
│ │ Σ Total   208.5h      All...     │ │
│ └──────────────────────────────────┘ │
╰──────────────────────────────────────╯
```

The redesign must make the entire section:

- smaller;
- cleaner;
- more compact;
- more consistent with the recently redesigned Schedule UI;
- visually close to the approved generated reference;
- still based on the current GF3 glass/soft-card design language.

The current Summary already correctly calculates work hours across published schedules for the selected month/year and renders Day / Hours / Schedule plus a total row. That data logic must remain intact.

The current implementation additionally contains a complete Salary Calculator directly inside the Summary panel. The approved target does **not** contain this calculator, so it must be removed together with its now-unused page-local state/helpers/styles/tests.

No business logic, API, persistence, or backend behavior changes are required.

---

## 2. Existing Behavior to Preserve

### REUSE — work-hours aggregation

Keep:

```ts
buildScheduleHoursSummary(...)
```

unchanged.

It currently:

- filters schedules by the selected Summary year/month;
- filters slots for the current employee;
- combines hours from multiple schedules on the same day;
- deduplicates schedule names;
- creates rows for every day in the month;
- formats empty days as `-`;
- calculates total hours.

Do not move or redesign this logic.

---

### REUSE — Summary period state

Keep:

```ts
selectedSummaryPeriodKey
summaryPeriods
activeSummaryPeriod
summaryYears
summaryMonths
summaryMonthOptions
summaryYearOptions
```

The current code already resolves a Summary period independently while defaulting to the opened schedule's month/year when possible.

---

### REUSE — period change handlers

Keep the behavior of:

```ts
handleSummaryMonthChange(...)
handleSummaryYearChange(...)
```

unchanged.

Month selection must continue updating only to an available period within the active year.

Year selection must continue attempting to keep the same month first and falling back to an available period in the selected year.

---

### REUSE — `SearchableSelect`

Continue using:

`FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.tsx`

for month and year.

The component already supports the required Summary size, no-search mode, disabled state, portal dropdown, keyboard/Escape behavior, and ARIA semantics.

Do not replace it with native `<select>` or create a second dropdown component.

---

### REUSE — `StatisticsIcon`

Continue using the existing:

```ts
StatisticsIcon
```

for the Summary header.

Do not introduce another chart icon package.

---

## 3. Constraints

Do not change:

- schedule APIs;
- schedule DTOs/entities;
- backend;
- database;
- authentication;
- employee identification;
- work-hour calculation;
- handling of overnight shifts;
- Summary month/year behavior;
- schedule matrix;
- daily schedule view;
- schedule selector;
- PDF export;
- Adjust / shift correction;
- column ordering;
- hero;
- navigation.

Do not create:

- a new Summary component hierarchy unless required by compilation;
- a new dropdown implementation;
- another work-hours calculator;
- another date/period state;
- new dependencies.

The work is a focused UI redesign plus removal of the Salary Calculator.

---

# 4. Implementation Steps

## Step 1 — Remove Salary Calculator imports and helpers

**Action:** REMOVE

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Remove salary-specific functionality such as:

```ts
numberFormat
FormEvent
salaryAmountFormatter
formatSalaryHours(...)
parseSalaryValue(...)
```

Do not remove `dateTimeFormat`.

Do not remove `useEffect`; it is still used elsewhere on the page.

---

## Step 2 — Remove Salary Calculator state and handlers

**Action:** REMOVE

**File:**

`EmployeeSchedulePage.tsx`

Remove:

```ts
salaryHoursInput
setSalaryHoursInput

salaryRateInput
setSalaryRateInput

salaryResult
setSalaryResult

parsedSalaryHours
parsedSalaryRate

canCalculateSalary
```

Remove the effect that synchronizes:

```ts
salaryHoursInput
```

with:

```ts
scheduleHoursSummary.totalHours
```

Remove:

```ts
handleCalculateSalary(...)
handleResetSalaryHours(...)
```

The work-hours Summary computation itself must remain.

---

## Step 3 — Remove Salary Calculator markup

**Action:** REMOVE

**File:**

`EmployeeSchedulePage.tsx`

Remove the entire Salary Calculator section, including:

- `Quick estimate`;
- `Salary calculator`;
- Hours input;
- Hourly rate input;
- `=` action;
- Estimated pay;
- Reset.

The Summary panel must end immediately after:

```text
hoursSummaryGrid
```

or:

```text
hoursSummaryEmpty
```

depending on state.

Do not leave an empty separator or empty container where the calculator used to be.

---

# 5. Restructure the Summary Header

## Step 4 — Convert Summary header into one compact top row

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.tsx`

Replace the presentation structure with:

```text
hoursSummaryHeader
├── hoursSummaryHeading
│   ├── summaryIcon
│   └── title block
│       ├── WORK HOURS
│       └── Summary
│
└── hoursSummaryTotalPill
```

The total pill must now be physically inside the header flex row rather than positioned absolutely relative to the panel.

Conceptual JSX:

```tsx
<div className={styles.hoursSummaryHeader}>
    <div className={styles.hoursSummaryHeading}>
        ...
    </div>

    <span className={styles.hoursSummaryTotalPill}>
        {scheduleHoursSummary.totalHoursText}
    </span>
</div>
```

Then render the period controls as their own row **below** the header.

Final order:

```text
Header
↓
Period controls
↓
Table
```

---

## Step 5 — Keep the compact icon/title hierarchy

**Action:** MODIFY / REUSE

Continue rendering:

```text
StatisticsIcon
WORK HOURS
Summary
```

but make the block more compact like the reference.

Keep:

```tsx
<span className={workspaceStyles.panelEyebrow}>
  {t("Work hours")}
</span>

<h2 className={workspaceStyles.panelTitle}>
  {t("Summary")}
</h2>
```

Do not replace these texts.

Do not merge them into a single heading.

Target hierarchy:

```text
WORK HOURS     ← small muted uppercase
Summary        ← stronger dark title
```

---

# 6. Redesign Month / Year Controls

## Step 6 — Remove visible `MONTH` and `YEAR` labels

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.tsx`

Remove the visible `MONTH` and `YEAR` labels from the UI.

Do **not** remove accessibility names.

Continue using:

```ts
ariaLabel={t("Summary month")}
ariaLabel={t("Summary year")}
```

The target must visually show only:

```text
[calendar] July  ▼
[calendar] 2026  ▼
```

not:

```text
MONTH
[July]

YEAR
[2026]
```

---

## Step 7 — Render month and year as two equal-width controls

**Action:** MODIFY

**Files:**

- `EmployeeSchedulePage.tsx`
- `EmployeeSchedulePage.module.css`

Render the two existing `SearchableSelect` controls directly inside:

```text
summaryPeriodControls
```

Use one shared page class:

```text
summaryPeriodSelect
```

for both.

Target:

```text
┌────────────────┐  ┌────────────────┐
│ 📅  July    ▼ │  │ 📅  2026    ▼ │
└────────────────┘  └────────────────┘
```

Use CSS Grid:

```text
2 × minmax(0, 1fr)
```

rather than hardcoded widths.

Remove obsolete page styles:

```text
.summaryPeriodField
.summaryPeriodField > span
.summaryMonthSelect
.summaryYearSelect
```

after their JSX usage has been removed.

The controls should remain equal width on mobile.

---

# 7. Extend SearchableSelect for a Leading Icon

## Step 8 — Add an optional `leadingIcon` prop

**Action:** EXTEND

**File:**

`FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.tsx`

Add:

```ts
leadingIcon?: ReactNode;
```

to `SearchableSelectProps`.

Import:

```ts
type ReactNode
```

from React.

This prop must be optional.

All existing `SearchableSelect` usages without it must render exactly as before.

---

## Step 9 — Render the icon before the selected value

**Action:** EXTEND

**File:**

`SearchableSelect.tsx`

When `leadingIcon` is provided, render:

```text
leading icon
selected value
chevron
```

inside the existing select button.

Do not change:

- dropdown behavior;
- dropdown portal;
- option rendering;
- search;
- Escape;
- `onChange`;
- disabled state;
- ARIA label;
- listbox semantics.

The icon must be decorative:

```text
aria-hidden="true"
```

because the select already has an accessible name.

---

## Step 10 — Style the optional leading icon

**Action:** MODIFY

**File:**

`FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.module.css`

Add styles for the optional icon slot.

For Summary-sized selects, target approximately:

```text
icon container: 24–28px
icon size:      14–16px
border-radius:  8–9px
```

Visual direction:

```text
background: soft blue
color: #2563eb
```

When a leading icon exists:

- icon stays fixed-width;
- selected text receives the remaining width;
- chevron stays pinned to the right;
- long labels truncate;
- no wrapping occurs.

Do not change visual layout of selects without a leading icon.

---

## Step 11 — Supply calendar icons from Summary

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.tsx`

For both existing Summary `SearchableSelect`s add:

```tsx
leadingIcon={<ScheduleIcon size={...} />}
```

Reuse the already imported/shared `ScheduleIcon`.

Do not create separate Month and Year icon files.

Both controls intentionally use the same calendar vocabulary, matching the approved reference.

---

# 8. Summary Panel Styling

## Step 12 — Make the outer Summary panel more compact

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Update:

```text
.hoursSummaryPanel
```

Target:

- maintain the existing white/glass employee-panel appearance;
- compact vertical rhythm;
- rounded card;
- no calculator space underneath.

Recommended direction:

```text
gap: 10px
padding: ~14px 12–14px
border-radius: ~24px
```

Do not create another nested outer card.

---

## Step 13 — Compact the Summary header icon

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Make the Work Hours Summary icon visually closer to the reference:

```text
~36–40px
```

with:

- soft blue surface;
- blue Statistics icon;
- rounded-square shape;
- subtle border.

Do not make it smaller than a clearly recognizable UI icon.

---

## Step 14 — Remove absolute total-pill positioning

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Remove any absolute positioning from `.hoursSummaryTotalPill`.

Set `.hoursSummaryTotalPill` to participate normally in the flex header.

Keep:

- green text;
- pale-green background;
- subtle green border;
- pill radius.

Target height:

```text
~28–30px
```

Keep:

```text
flex: 0 0 auto
white-space: nowrap
```

Remove now-unnecessary heading padding that existed only to compensate for absolute positioning.

---

## Step 15 — Keep header on one row on mobile

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Remove mobile rules that turn `.hoursSummaryHeader` into a column.

Header must remain:

```text
[icon + Work Hours / Summary]        [208.5h]
```

at:

```text
360px
390px
430px
```

Use:

```text
min-width: 0
flex
gap
```

rather than wrapping the pill below the title.

---

# 9. Period Controls Styling

## Step 16 — Create compact two-control filter row

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Change `.summaryPeriodControls` to:

```text
display: grid
grid-template-columns: repeat(2, minmax(0, 1fr))
gap: 8px
width: 100%
```

The controls must look balanced and equal.

Do not stack Month and Year on normal mobile widths.

At extremely constrained width below the supported layout, content may truncate rather than cause horizontal overflow.

---

## Step 17 — Preserve the existing Summary select design foundation

**Action:** REUSE / REFINE

Keep using:

```ts
size="summary"
shadow="soft"
searchEnabled={false}
showSelectedHint={false}
```

Do not introduce a new button style from scratch.

Only refine spacing required for the new leading calendar icon.

---

# 10. Table Styling

## Step 18 — Keep existing table semantics and data

**Action:** REUSE

Keep:

```text
role="table"
role="row"
role="columnheader"
role="cell"
```

Keep columns:

```text
DAY
HOURS
SCHEDULE
```

Keep:

```text
row.dayLabel
row.hoursText
row.scheduleName
```

unchanged.

Do not alter data calculations to make the table match the screenshot.

---

## Step 19 — Make the table visually more compact

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Reduce visual density slightly toward the approved reference.

Target approximately:

```text
row min-height: 34–36px
cell padding:   7–8px 10px
```

Keep text readable.

Keep current responsive three-column proportions unless a small adjustment is required to prevent schedule text crowding.

Do not reduce touch-target requirements because the table itself is read-only.

---

## Step 20 — Preserve the sticky header and total row

**Action:** REUSE

Keep:

```text
hoursSummaryGridHeader → sticky top
hoursSummaryTotalRow   → sticky bottom
```

Do not remove scrolling.

Do not render all 28–31 days directly into page height.

---

## Step 21 — Match table header to the reference

**Action:** MODIFY / REFINE

Keep a pale slate/light-blue header.

Target:

```text
DAY        HOURS        SCHEDULE
```

with:

- small uppercase text;
- muted blue-gray foreground;
- strong weight;
- no heavy borders.

Keep current table border and rounded outer shell.

---

# 11. Total Row

## Step 22 — Add Sigma symbol before `Total`

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.tsx`

Change the first total-row cell from simple `Total` text to:

```text
Σ Total
```

Use a dedicated class such as:

```text
hoursSummaryTotalLabel
```

The Sigma symbol is decorative:

```text
aria-hidden="true"
```

Screen readers must continue to hear simply:

```text
Total
```

Do not add a new icon dependency for Sigma.

---

## Step 23 — Style the total row as the strongest table accent

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Keep the existing pale-green total background.

Refine it to match the mockup:

```text
Σ Total      208.5h      All schedules
```

with:

- Sigma green;
- Total green and bold;
- hours green and bold;
- `All schedules` muted slate;
- no excessive height.

Do not color `All schedules` green.

---

# 12. Remove Obsolete Salary Styles

## Step 24 — Delete Salary Calculator CSS

**Action:** REMOVE

**File:**

`EmployeeSchedulePage.module.css`

Remove all styles exclusively associated with the removed calculator:

```text
.salaryCalculator
.salaryCalculatorHeader
.salaryCalculatorMark
.salaryCalculatorForm
.salaryCalculatorField
.salaryCalculatorInputShell
.salaryCalculatorEquals
.salaryCalculatorResult
.salaryCalculatorReset
```

including:

- hover states;
- focus states;
- disabled states;
- child selectors;
- calculator-specific responsive rules.

Also remove calculator-only rules from mobile media queries.

Do not remove unrelated responsive rules.

---

# 13. Clean Up Obsolete Summary CSS

## Step 25 — Remove old layout rules no longer used

**Action:** REMOVE / CONSOLIDATE

**File:**

`EmployeeSchedulePage.module.css`

After JSX restructuring, remove unused:

```text
.hoursSummaryActions
.summaryPeriodField
.summaryPeriodField > span
.summaryMonthSelect
.summaryYearSelect
```

if no references remain.

Replace with the new:

```text
.summaryPeriodControls
.summaryPeriodSelect
```

Do not leave old declarations merely overridden later in the stylesheet.

Keep Summary styling in one coherent block.

---

# 14. Tests

## Step 26 — Remove Salary Calculator test

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

Remove the current test for calculating estimated salary and restoring the Work hours total.

Do not retain an obsolete test for functionality that no longer exists.

---

## Step 27 — Add regression assertion that calculator is absent

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.test.tsx`

Inside the main Summary/render test, add an assertion equivalent to:

```text
Salary calculator region is not present
```

This protects the approved simplified UI from accidental reintroduction.

Do not test CSS geometry through Vitest.

---

## Step 28 — Preserve work-hours Summary aggregation test

**Action:** REUSE

Keep the existing test verifying that work from multiple public schedules appears in:

```text
Schedule hours summary
```

Do not weaken these assertions.

---

## Step 29 — Preserve period-selection integration test

**Action:** REUSE / MINIMAL UPDATE ONLY

Keep the existing test that lets the employee choose the month and year used by Summary.

The accessible names must remain:

```text
Summary month
Summary year
```

Therefore this test should continue working even though visible `MONTH` / `YEAR` labels are removed.

Only update the test if the leading icon affects DOM text unexpectedly.

Do not change the functional assertions.

---

## Step 30 — Add optional leading-icon regression coverage

**Action:** MODIFY

**Preferred file:**

`EmployeeSchedulePage.test.tsx`

Verify indirectly through the Summary page that:

- Summary month control exists;
- Summary year control exists;
- both remain usable;
- their period-selection behavior remains correct.

The icon itself is decorative and does not require a semantic assertion.

---

# 15. Data / API / Persistence Changes

### API

None.

### Backend

None.

### DTO / entity changes

None.

### Persistence

None.

### Database migration

None.

### Dependency injection

None.

### Configuration

None.

### Dependencies

None.

---

# 16. Error and Edge Case Requirements

### No schedules

Preserve the existing page loading/empty states.

Do not render Summary when the current condition prevents it today.

### Only one available month

Continue disabling the month select exactly as the current logic does.

### Only one available year

Continue disabling the year select exactly as the current logic does.

Do not alter this behavior only for visual consistency.

### Month unavailable in newly selected year

Preserve the current year-handler fallback to the first available period for that year.

### Empty working month

Keep the existing:

```text
No assigned shifts in this period.
```

UI.

### 28 / 29 / 30 / 31 day months

Keep `buildScheduleHoursSummary(...)` as the source of rows.

Do not hardcode the number of visible days from the mockup.

### Long schedule names

Keep schedule cells able to wrap.

Do not increase the entire table width.

### Large month

Table must scroll internally and retain:

- sticky header;
- sticky Total row.

### Small mobile width

At `360px`:

- heading and total pill remain on the same row;
- Month and Year remain side-by-side;
- no horizontal page overflow;
- period labels may truncate;
- table remains usable.

---

# 17. Verification

From:

```bash
cd FrontEnd
```

run:

```bash
npm test -- src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx
```

Then:

```bash
npm run lint
```

Then:

```bash
npm run build
```

Then:

```bash
npm test
```

---

# 18. Manual Visual Verification

Verify the Summary at:

```text
360px
390px
430px
520px
>= 860px
```

The mobile target must visually read approximately as:

```text
╭────────────────────────────────────╮
│ [▥] WORK HOURS           [208.5h] │
│     Summary                        │
│                                    │
│ [📅 July     ▼] [📅 2026      ▼] │
│                                    │
│ DAY        HOURS       SCHEDULE    │
│ we./01     9.5h        8989        │
│ th./02     15h         8989        │
│ fr./03     14h         8989,...    │
│ ...                                │
│ Σ Total    208.5h      All...      │
╰────────────────────────────────────╯
```

Confirm:

- no Salary Calculator exists;
- no `Quick estimate`;
- no Hours input;
- no Hourly rate input;
- no Reset;
- no `=` calculator action;
- Summary card becomes substantially shorter;
- total-hours pill is top-right;
- icon/title remain top-left;
- Month and Year are immediately below;
- both selects contain calendar icons;
- no visible `MONTH` / `YEAR` labels;
- table is compact;
- Total row has Sigma;
- existing period switching still works.

---

# 19. Acceptance Checklist

- [ ] Summary visually matches the selected reference.
- [ ] Salary Calculator is completely removed.
- [ ] Salary calculator state is removed.
- [ ] Salary calculator helpers are removed.
- [ ] Salary calculator CSS is removed.
- [ ] Salary calculator test is removed.
- [ ] `numberFormat` is no longer imported by this page if unused.
- [ ] `FormEvent` is no longer imported by this page if unused.
- [ ] Work Hours calculation remains unchanged.
- [ ] Summary month/year selection remains unchanged.
- [ ] `Work hours` eyebrow remains.
- [ ] `Summary` title remains.
- [ ] Statistics icon remains.
- [ ] Total hours pill appears in the top-right header.
- [ ] Total pill no longer uses absolute positioning.
- [ ] Month and Year controls are directly below the header.
- [ ] Visible `MONTH` label is removed.
- [ ] Visible `YEAR` label is removed.
- [ ] Summary Month retains accessible name `Summary month`.
- [ ] Summary Year retains accessible name `Summary year`.
- [ ] Month and Year controls are equal width.
- [ ] Both controls contain a calendar icon.
- [ ] `SearchableSelect.leadingIcon` is optional.
- [ ] Existing SearchableSelect usages without an icon are unchanged.
- [ ] Table columns remain Day / Hours / Schedule.
- [ ] Table remains scrollable.
- [ ] Header remains sticky.
- [ ] Total row remains sticky.
- [ ] Sigma appears before `Total`.
- [ ] Total and total hours use green accent.
- [ ] `All schedules` stays muted.
- [ ] Empty Summary state still works.
- [ ] No API changes.
- [ ] No backend changes.
- [ ] No database changes.
- [ ] No new dependencies.
- [ ] Focused tests pass.
- [ ] Full tests pass.
- [ ] Lint passes.
- [ ] Build passes.

---

# 20. Exact Change Scope

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Responsibilities:

- restructure Summary header;
- move total pill into header;
- simplify period controls;
- add leading calendar icons;
- add Sigma to Total;
- remove Salary Calculator markup;
- remove Salary Calculator state/helpers/handlers/imports.

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

Responsibilities:

- compact Summary card;
- compact Summary header;
- inline total pill;
- two-column period selector;
- compact table;
- Total/Sigma styling;
- remove calculator styles;
- remove obsolete Summary styles;
- maintain mobile responsiveness.

### EXTEND

`FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.tsx`

Responsibility:

- add optional `leadingIcon?: ReactNode`;
- render the icon decoratively before selected value;
- preserve all existing behavior for calls without it.

### MODIFY

`FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.module.css`

Responsibility:

- support leading-icon alignment;
- style Summary leading icon consistently;
- preserve existing select appearance when no icon is supplied.

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

Responsibilities:

- remove calculator behavior test;
- protect calculator removal;
- preserve Summary calculations;
- preserve month/year interaction tests.

---

# 21. Do Not Change

Do not modify:

```text
BusinessLogicLayer/
DataAccessLayer/
GF3.WebApi/
GF3.Launcher/
```

Do not modify:

```text
EmployeeScheduleSelectDialog
EmployeeShiftCorrectionDialog
EmployeeScheduleColumnOrderDialog
ContainerGraphMatrix
CardSection
EmployeeScheduleHero
employee workspace navigation
```

Do not modify:

- schedule endpoint contracts;
- schedule persistence;
- employee UI state persistence;
- PDF generation;
- shift-correction requests;
- matrix/daily switching;
- schedule-selector behavior;
- summary aggregation rules.

Do not perform unrelated refactoring.

This plan intentionally limits the change to the approved Work Hours Summary redesign and the small reusable `SearchableSelect` extension required by the reference UI.
