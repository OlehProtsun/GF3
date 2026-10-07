# Plan

## 1. Objective

Redesign the employee **Schedule** page at repository state `DEV2 / 55bf0e9` so that the separate **Public schedules** selection panel is removed and schedule selection is integrated directly into the existing schedule card, matching the approved mockup.

The final UI must have:

- the existing gray page background;
- one white schedule surface below the hero;
- `Schedules` as the section title;
- existing PDF action in the upper-right;
- selected schedule name, e.g. `F27`, directly inside this section;
- a small downward chevron indicating that the schedule can be changed;
- month/year directly under the schedule name, e.g. `October 2026`;
- `Updated 04 Oct 2026, 14:01` directly below it;
- the `Updated ...` text in a clearly visible light blue;
- existing `Adjust` action on the right;
- the schedule matrix directly below the compact header;
- no separate Public schedules card;
- no extra date/month selector;
- no month navigation arrows;
- no duplicated `F27 / October / 2026` pill row;
- tapping the selected schedule opens a **Select schedule** bottom sheet;
- the bottom sheet lists all published schedules with name, month/year and update time;
- selecting another schedule closes the sheet and immediately updates the displayed schedule.

The existing `EmployeeSchedule` model already provides `name`, `year`, `month` and `lastUpdatedAtUtc`, so this redesign requires no API/model expansion.

This plan is intentionally implementation-ready and scoped to the requested UI change.

---

## 2. Existing Components to Reuse

### REUSE — `ContainerGraphMatrix`

File:

`FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx`

Keep this component unchanged.

It already supports:

- `title: ReactNode`;
- `icon`;
- `headerRightSlot`;
- compact header mode;
- read-only schedule rendering.

It delegates these header values to the existing `CardSection`, so the new selector UI can be supplied from `EmployeeSchedulePage` without changing the shared matrix component.

### REUSE — `CardSection`

File:

`FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx`

Keep this component unchanged.

Continue using its existing title/header/right-slot composition rather than introducing another schedule-specific card primitive.

### REUSE — existing schedule data/state

Continue using:

- `useEmployeeScheduleListQuery`;
- `selectedScheduleId`;
- `selectedSchedule`;
- `formatScheduleLastUpdate`;
- `formatScheduleMonth(...)`;
- `formatScheduleMonthOnly(...)`;
- existing schedule matrix derivation;
- existing daily/matrix view switching;
- existing PDF export;
- existing Adjust / shift-correction flow.

The current selected schedule already drives matrix columns, cells, daily view and related state.

### REUSE — overlay infrastructure

Reuse:

`FrontEnd/src/shared/ui/ViewportOverlay.module.css`

through CSS Modules `composes`, as existing project dialogs already do.

### REUSE — icons

Reuse existing icons from:

`@shared/ui/icons`

Specifically:

- `CloseIcon`;
- `CheckIcon`;
- `EmployeeIcon`;
- existing `ScheduleIcon`.

Do not add a new icon library.

---

## 3. Constraints

- Do not change backend code.
- Do not change API endpoints.
- Do not change schedule persistence.
- Do not change `EmployeeSchedule`.
- Do not add npm dependencies.
- Do not modify `ContainerGraphMatrix`.
- Do not modify the shared `CardSection`.
- Do not alter the schedule table/matrix business logic.
- Do not alter PDF generation.
- Do not alter column customization.
- Do not alter Adjust / shift-correction behavior.
- Do not remove the existing daily/matrix presentation toggle.
- Do not redesign the hero.
- Do not redesign Work hours / salary sections.
- Do not introduce month navigation arrows.
- Do not introduce another date picker.
- Do not preserve the old large Public schedules panel alongside the new selector.
- Do not perform unrelated CSS cleanup/refactoring.

The task is a localized presentation/state-wiring change.

---

## 4. Implementation Steps

### Step 1 — Create the schedule-selection bottom sheet

**Action:** CREATE

**Files:**

- `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.module.css`

### Component contract

Create:

```ts
type EmployeeScheduleSelectDialogProps = {
  open: boolean;
  schedules: EmployeeSchedule[];
  selectedScheduleId: number | null;
  onSelect: (scheduleId: number) => void;
  onClose: () => void;
};
```

Responsibility:

Present the existing published schedules as a compact selector only. It must not own schedule fetching or selection state.

### Dialog structure

Render only while `open === true`.

Structure:

```text
overlay
└── sheet/dialog
    ├── drag handle
    ├── header
    │   ├── "Select schedule"
    │   └── close button
    └── schedule list
        ├── F27
        │   ├── October 2026
        │   └── Updated ...
        └── F35
            ├── October 2026
            └── Updated ...
```

Each schedule row must:

- be a `<button type="button">`;
- display `schedule.name`;
- display localized month + year;
- display last-update information using the existing `formatScheduleLastUpdate(...)`;
- use `aria-pressed={isSelected}`;
- visually distinguish the selected schedule;
- show a blue circular selected indicator containing `CheckIcon`;
- show an outlined empty indicator when not selected;
- use `EmployeeIcon` or the equivalent existing employee/schedule visual on the left.

For a schedule with `lastUpdatedAtUtc`:

```text
Updated 04 Oct 2026, 14:01
```

For a schedule without an update timestamp, display the existing fallback from `formatScheduleLastUpdate(...)` without constructing the awkward phrase `Updated Not recorded yet`.

### Interaction

Selecting a row must call:

```ts
onSelect(schedule.id)
```

The parent will own closing the sheet.

Support:

- close button;
- Escape key;
- clicking the overlay outside the sheet.

Do not create confirmation/cancel steps: selecting a schedule is immediate.

### Accessibility

The overlay must use:

```text
role="dialog"
aria-modal="true"
aria-labelledby=<dialog title id>
```

Use `useId()` for the title id.

When the sheet opens, autofocus the currently selected schedule row. If no row is selected, autofocus the close button.

### Styling

Reuse the existing overlay/surface primitives through:

```css
composes: overlay from "../../../shared/ui/ViewportOverlay.module.css";
```

and:

```css
composes: surface from "../../../shared/ui/ViewportOverlay.module.css";
```

Desktop/tablet:

- compact modal width around the existing 420–460px dialog vocabulary;
- white surface;
- rounded corners;
- existing project shadow/border language.

Mobile (`max-width: 640px`):

- attach sheet to the bottom of the viewport;
- width `100%`;
- no horizontal page margin;
- rounded top-left/top-right corners;
- bottom corners may be `0`;
- include `env(safe-area-inset-bottom)` in bottom padding;
- max-height approximately `80–85dvh`;
- list becomes vertically scrollable when necessary.

The underlying page must receive the existing dimmed + blurred overlay treatment.

Add a small horizontal drag handle at the top for visual parity with the approved mockup. It is decorative only.

### Colors

Selected schedule:

- blue border;
- very light blue background;
- solid blue selected indicator.

Normal schedule:

- white/light neutral surface;
- subtle neutral border.

Last update:

```css
color: #3b82f6;
```

Use this same update color in the selector and in the selected-schedule header.

---

### Step 2 — Replace the old schedule-card click handler with selector state

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Add:

```ts
const [isScheduleSelectOpen, setIsScheduleSelectOpen] = useState(false);
```

Keep:

```ts
const [selectedScheduleId, setSelectedScheduleId] = useState<number | null>(null);
```

Replace the existing `handleSelectSchedule(button, scheduleId)` implementation.

The existing handler currently depends on DOM lookup and Web Animations specifically for the old large schedule cards. Those cards will no longer exist.

Use the simplified responsibility:

```text
select ID
→ close selector
→ existing selectedSchedule memo recalculates
→ existing matrix/daily derived state recalculates
```

Conceptual contract:

```ts
const handleSelectSchedule = (scheduleId: number) => {
  setSelectedScheduleId(scheduleId);
  setIsScheduleSelectOpen(false);
};
```

Do not preserve:

- `button.querySelector(...)`;
- `.scheduleCardDate` lookup;
- `button.animate(...)`;
- badge rotation animation;
- `schedule-card-press` animation IDs.

Those only support the removed card selector.

Keep the existing effect that resets the selected daily day when `selectedSchedule.id` changes.

---

### Step 3 — Remove the separate Public schedules section

**Action:** REMOVE

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Remove the complete render block currently beginning with:

```tsx
{schedules.length > 0 ? (
  <section className={`${workspaceStyles.panel} ${styles.publicSchedulesPanel}`}>
```

and containing:

- Public schedules eyebrow;
- `{n} schedules`;
- period pill;
- `scheduleSwitcher`;
- schedule cards;
- date badge;
- shop/container metadata;
- Last Update field.

Do not replace it with another standalone panel.

After this step, when schedules exist, the first schedule content after the hero must be the actual unified schedule card.

Loading and no-schedule states must remain unchanged.

---

### Step 4 — Build one compact selected-schedule header

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Replace the existing title metadata:

```text
Schedules
[F27] [October] [2026]
```

with the approved compact layout:

```text
Schedules                         PDF

[icon] F27 ▼                     Adjust
       October 2026
       Updated 04 Oct 2026, 14:01

---------------------------------------
schedule matrix
```

### Selected-schedule trigger

Inside the existing `openScheduleTitleBlock`, render a button representing the active schedule.

It must contain:

- a compact blue/light-blue icon surface;
- `EmployeeIcon`;
- selected schedule name;
- CSS-created downward chevron;
- month + year;
- last update.

Do not introduce a new chevron SVG/icon file. Create the small chevron with CSS using borders/rotation.

The button must:

```text
type="button"
aria-haspopup="dialog"
aria-expanded={isScheduleSelectOpen}
```

and have an accessible name equivalent to:

```text
Select schedule. Current schedule: F27
```

Click:

```ts
setIsScheduleSelectOpen(true)
```

### Period

Render one combined period string:

```text
October 2026
```

Do not render separate:

```text
October
2026
```

pills.

Do not add left/right arrows.

Do not add another month selector.

### Updated text

Use:

```text
Updated <formatted last update>
```

when a timestamp exists.

Make only this metadata line light blue:

```css
color: #3b82f6;
```

It should be visually distinct from:

- schedule name: dark;
- month/year: muted slate.

Use the same presentation in matrix and daily modes.

### Reuse one ReactNode

Build the compact schedule title once in `EmployeeSchedulePage` and pass the same ReactNode to:

- `ContainerGraphMatrix` in matrix mode;
- `CardSection` in daily mode.

Do not duplicate two independently authored copies of the selector header.

---

### Step 5 — Preserve PDF, Adjust, and matrix/daily switching

**Action:** REUSE / MODIFY LAYOUT ONLY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Keep the existing:

```tsx
headerRightSlot={
  <div className={styles.openScheduleActions}>
```

and existing actions.

PDF must continue calling:

```ts
handleExportPdf
```

Adjust must continue opening the existing shift correction dialog.

These handlers already operate on `selectedSchedule`; therefore changing schedule selection will naturally make them operate on the new selection.

Keep the existing `ScheduleIcon` button and:

```ts
handleToggleScheduleView
```

unchanged.

The redesign must not silently remove daily view.

---

### Step 6 — Mount the schedule selector dialog once

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Import:

```ts
EmployeeScheduleSelectDialog
```

Render a single instance near the other page dialogs.

Pass:

```text
open = isScheduleSelectOpen
schedules = schedules
selectedScheduleId = selectedSchedule?.id ?? null
onSelect = handleSelectSchedule
onClose = () => setIsScheduleSelectOpen(false)
```

The dialog must not perform queries.

The dialog must not mutate server state.

Selection remains local UI state exactly as it does now.

---

### Step 7 — Convert the existing schedule card to the single white content surface

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

Preserve the gray application/page background.

The actual schedule card must remain visually separated as one white layer.

Update both:

- `.openScheduleMatrix`
- `.dailyScheduleCard`

so they share the same visual vocabulary:

- white/near-white background;
- 24px rounded card;
- current subtle border;
- current subtle shadow;
- same width;
- no second surrounding Public schedules card.

Do not modify the global page background or global `CardSection` styles.

The matrix currently already owns the compact page-specific card dimensions and 24px radius, so extend this page-level styling instead of changing the shared component.

---

### Step 8 — Replace obsolete Public-schedules CSS

**Action:** MODIFY / REMOVE

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

Remove selectors used exclusively by the removed standalone selector:

```text
.publicSchedulesPanel
.publicSchedulesHeader
.publicSchedulesHeading
.publicSchedulesHeaderMeta
.lastUpdateField
.lastUpdateDot
.selectedSchedulePill
.scheduleSwitcher
.scheduleButton
.scheduleButtonActive
.scheduleCardDate
.scheduleButtonDetails
.scheduleButtonContent
.scheduleButtonName
.scheduleButtonMeta
```

Add page-specific classes for the new compact header, for example:

```text
.scheduleSelectTrigger
.scheduleSelectTriggerIcon
.scheduleSelectTriggerCopy
.scheduleSelectName
.scheduleSelectNameRow
.scheduleSelectChevron
.scheduleSelectPeriod
.scheduleSelectUpdated
```

Requirements:

- no browser-default button background/border;
- full trigger remains visibly clickable;
- name is bold/dark;
- period is smaller muted text;
- updated line uses `#3b82f6`;
- trigger has a visible keyboard focus ring;
- long schedule names truncate or wrap without pushing PDF/Adjust outside the card;
- mobile layout must remain inside viewport.

Do not remove unrelated CSS such as:

- daily schedule day tabs;
- matrix variables;
- salary styles;
- shift correction styles;
- hours-summary styles.

---

## 5. Data / API / Persistence Changes

### API

None.

### Backend

None.

### Models / DTOs

None.

The existing schedule DTO already contains all information required by this UI.

### Persistence

None.

Do not persist the selected schedule as part of this task.

### Dependency Injection

None.

### Configuration

None.

### New dependencies

None.

---

## 6. Error and Edge Case Requirements

### Zero schedules

Preserve the existing:

- loading state;
- `No published schedules`;
- Availability link.

Do not show the selector when no schedule exists.

### One schedule

Still render the selected-schedule trigger and metadata consistently.

Opening the selector is allowed and shows the single selected schedule.

Do not special-case the entire header into a different design.

### Multiple schedules

All schedules returned by `useEmployeeScheduleListQuery` must appear in the sheet.

### Schedule without `lastUpdatedAtUtc`

Use the existing fallback formatter.

Do not display an empty line.

Do not render an invalid `<time dateTime="">`.

### Schedule changes

After selecting another schedule:

- bottom sheet closes;
- trigger displays new schedule name;
- month/year updates;
- update timestamp updates;
- matrix switches to the selected schedule;
- PDF uses selected schedule;
- Adjust uses selected schedule;
- daily schedule state resets using the existing selected-schedule effect.

### Long schedule names

Do not allow the name to push:

- PDF;
- Adjust;
- card boundaries;
- table width.

Use ellipsis/wrapping within the compact trigger.

### Many schedules

The bottom-sheet list must scroll internally instead of exceeding viewport height.

### Mobile safe area

Bottom sheet content must not be hidden behind the device home indicator.

---

## 7. Tests

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

### Update existing expectations

The current test expects:

- `2 schedules`;
- two `Last Update` fields;

because those values come from the old separate Public schedules section.

Remove those obsolete assertions.

Replace them with assertions that initial render contains:

- selected schedule name in the compact trigger;
- period;
- formatted updated text;
- matrix for the first/default schedule.

### Add schedule-selector integration test

Test flow:

1. Mock at least two schedules.
2. Render page.
3. Confirm first schedule is active.
4. Click the `Select schedule...` trigger.
5. Assert dialog `Select schedule` appears.
6. Assert both schedules are listed.
7. Assert first one is selected.
8. Assert update text is visible in the sheet.
9. Click second schedule.
10. Assert dialog closes.
11. Assert compact header now displays second schedule.
12. Assert matrix receives second schedule (`graph.id` changed).
13. Assert month/update metadata corresponds to the second schedule.

### Add close-behavior test

At minimum verify Escape:

1. open selector;
2. press Escape;
3. dialog disappears;
4. selected schedule remains unchanged.

### Add missing-update coverage

Use a schedule with no `lastUpdatedAtUtc`.

Verify:

- fallback text is rendered;
- no invalid update date is produced.

### Preserve all existing tests for

- PDF layout;
- matrix data;
- touching-shift merging;
- daily schedule mode;
- salary calculation;
- column ordering;
- summary month/year selection;
- loading/no schedules.

Do not weaken those assertions merely to make the redesign pass.

### Adjust matrix test mock if required

The page already passes a React node as `title`; make the test mock's `title` type `ReactNode` rather than `string` if needed so the new interactive title/header renders correctly in tests.

---

## 8. Verification

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

If the focused tests pass, run the complete frontend suite:

```bash
npm test
```

### Manual visual verification

Check `/schedule` with at least two schedules at:

```text
360px mobile width
390px mobile width
>= 860px desktop layout
```

Verify visually:

```text
Hero

[ ONE white Schedules card                  ]
[ Schedules                           PDF   ]
[                                             ]
[ icon  F27 ▼                       Adjust ]
[       October 2026                        ]
[       Updated 04 Oct 2026, 14:01          ]
[                                             ]
[ schedule matrix                            ]
```

Confirm:

- gray page remains visible around the card;
- Public schedules card is gone;
- no large vertical gap exists between selector and matrix;
- no month navigation control exists;
- no arrows for changing month/date exist;
- `Updated ...` is light blue;
- PDF/Adjust remain readable;
- bottom navigation does not cover content;
- schedule selector opens as a bottom sheet on mobile;
- page behind the sheet is dimmed/blurred;
- the selected row is clearly marked.

---

## 9. Acceptance Checklist

- [ ] Separate `Public schedules` section is removed.
- [ ] Schedule selector and actual schedule are visually one card.
- [ ] Existing gray page background remains unchanged.
- [ ] Unified schedule card is white/near-white.
- [ ] `Schedules` remains visible as section title.
- [ ] Selected schedule name is visible.
- [ ] Selected schedule has a downward selection chevron.
- [ ] Month and year appear as one line.
- [ ] No duplicate F27/October/2026 pills remain.
- [ ] No date/month navigation arrows are introduced.
- [ ] Last-update text appears directly below the month/year.
- [ ] Last-update text uses light blue `#3b82f6`.
- [ ] PDF remains available.
- [ ] Adjust remains available.
- [ ] Matrix remains directly below the compact header.
- [ ] Daily/matrix toggle still works.
- [ ] Tapping selected schedule opens `Select schedule`.
- [ ] Selector is a bottom sheet on mobile.
- [ ] All published schedules appear in the sheet.
- [ ] Each row shows name, period and update status.
- [ ] Selected schedule has a blue check indicator.
- [ ] Selecting a schedule closes the sheet.
- [ ] Matrix updates immediately.
- [ ] PDF/Adjust subsequently operate on the newly selected schedule.
- [ ] Missing update timestamps remain valid.
- [ ] Zero/one/many schedule states remain valid.
- [ ] Existing schedule business logic is unchanged.
- [ ] No backend/API/database changes are made.
- [ ] No new npm dependency is introduced.
- [ ] Focused page tests pass.
- [ ] Full frontend build succeeds.
- [ ] Lint succeeds.

---

## 10. Do Not Change

Do not modify:

```text
BusinessLogicLayer/
DataAccessLayer/
GF3.WebApi/
GF3.Launcher/
```

Do not modify schedule APIs or DTO contracts.

Do not modify:

```text
FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx
FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx
FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleHero.tsx
```

unless compilation proves an unavoidable existing incompatibility. In that case, stop and report the blocker rather than expanding scope automatically.

Do not redesign:

- employee navigation;
- Availability;
- Swap;
- Profile;
- Work hours summary;
- salary calculator;
- shift correction;
- column customization.

Do not perform general schedule-page refactoring even though `EmployeeSchedulePage.tsx` is large.

The execution goal is only the approved schedule-selection UX redesign.
