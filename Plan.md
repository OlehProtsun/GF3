# Plan

## 1. Objective

Update the employee **Schedule** section UI at:

- Repository: `https://github.com/OlehProtsun/GF3`
- Branch: `DEV2`
- Commit: `c91ca1e56360f13df86d1eaac2eb1f8e595b5a96`

The goal is to make the schedule header and schedule selector match the approved reference image while preserving all existing schedule behavior.

The current implementation already has:

- matrix and daily schedule modes;
- `scheduleViewMode`;
- `handleToggleScheduleView`;
- focus restoration through `scheduleViewToggleRef`;
- schedule selection bottom sheet;
- PDF export;
- Adjust / shift-correction flow.

Currently, however, the schedule-view mode is changed by clicking the large icon on the left side of `Schedules`. The schedule selector below uses a second `EmployeeIcon`, creating the duplicated-icon appearance the redesign is intended to remove.

The target layout is:

```text
┌───────────────────────────────────────┐
│ Schedules                  [view] PDF │
│ ───────────────────────────────────── │
│                                       │
│ ╭───────────────────────────────────╮ │
│ │ [calendar] │ F27 ▼       Adjust  │ │
│ │            │ July 2026            │ │
│ │            │ Updated ...          │ │
│ ╰───────────────────────────────────╯ │
│                                       │
│ schedule matrix / daily view          │
└───────────────────────────────────────┘
```

The target must reproduce these key characteristics from the approved mockup:

- `Schedules` is a clean text heading with **no icon attached to it**.
- View-mode switching becomes a **dedicated icon-only button immediately before PDF**.
- PDF remains a pale-red pill.
- The selected schedule becomes one visually unified rounded/capsule control.
- The selector has one calendar icon on the left.
- A subtle vertical separator appears after that icon.
- Schedule name, month/year and updated timestamp form one information block.
- `Adjust` is visually inside the same rounded selector surface, but remains its own button.
- `Updated ...` stays light blue.
- The matrix/daily content begins directly below the selector.

No business logic, API, persistence, or backend behavior changes are required.

---

## 2. Existing Components to Reuse

### REUSE — existing view-mode state and behavior

File:

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Keep unchanged:

```ts
const [scheduleViewMode, setScheduleViewMode] =
  useState<"matrix" | "daily">("matrix");

const [hasSwitchedScheduleView, setHasSwitchedScheduleView] =
  useState(false);

const scheduleViewToggleRef = useRef<HTMLButtonElement>(null);

const handleToggleScheduleView = () => {
  setHasSwitchedScheduleView(true);
  setScheduleViewMode(current =>
    current === "matrix" ? "daily" : "matrix"
  );
};
```

Also keep the existing `useLayoutEffect` that restores focus to the toggle after changing mode.

The redesign changes **where the button is rendered**, not how schedule mode works.

---

### REUSE — `ContainerGraphMatrix`

File:

`FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx`

Keep this shared component unchanged.

It already supports:

- `title`;
- `icon`;
- `headerCenterSlot`;
- `headerRightSlot`;
- page-specific header classes.

Therefore the new composition can be implemented entirely from `EmployeeSchedulePage` without changing matrix architecture.

---

### REUSE — `CardSection`

File:

`FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx`

Keep unchanged.

Its structure already provides:

```text
sectionTitle
headerCenter
headerRight
```

and accepts page-specific classes.

Use the same header composition for both matrix and daily modes.

---

### REUSE — existing schedule selector dialog

Keep:

`FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`

unchanged unless compilation exposes an unrelated existing issue.

The compact selector in the card must continue opening this existing bottom sheet.

Existing tests already verify:

- opening it;
- selected row;
- changing schedule;
- metadata update;
- Escape;
- close button;
- overlay closing;
- missing update timestamp fallback.

Do not redesign the bottom sheet in this task.

---

### REUSE — existing icons

Use existing icons from:

`@shared/ui/icons`

Specifically:

- `ScheduleIcon` → calendar icon inside the selected-schedule capsule;
- `ScheduleDetailsIcon` → new schedule-view mode button near PDF;
- `StatisticsIcon` → continue using where already used.

`ScheduleDetailsIcon` already exists and is exported by the shared icon package, so no new SVG/icon dependency is required.

Remove `EmployeeIcon` from this page if it becomes unused after the redesign.

---

## 3. Constraints

Do not:

- change schedule fetching;
- change `selectedScheduleId`;
- change schedule selection logic;
- change `EmployeeScheduleSelectDialog` behavior;
- change matrix construction;
- change daily-view calculations;
- change PDF export logic;
- change shift-correction logic;
- change column ordering;
- change localStorage/server synchronization for column order;
- change Work hours;
- change Salary calculator;
- change the hero;
- change navigation;
- change backend;
- change API;
- change DTOs;
- change database;
- add dependencies;
- modify `ContainerGraphMatrix`;
- modify shared `CardSection`.

Do not create another view-mode state.

Do not move the view-mode behavior into a new component.

Do not nest the `Adjust` button inside the schedule-selection `<button>` because nested interactive controls are invalid HTML.

Do not use absolute positioning or viewport-width hacks to reproduce the reference.

---

# 4. Implementation Steps

## Step 1 — Update schedule-page icon imports

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Current imports include:

```ts
EmployeeIcon
ScheduleIcon
```

Change the icon imports so the page uses:

```text
ScheduleIcon
ScheduleDetailsIcon
StatisticsIcon
AvailabilityIcon
```

Remove `EmployeeIcon` from this file if no other usage remains.

Responsibilities:

- `ScheduleIcon` = selected schedule;
- `ScheduleDetailsIcon` = switch matrix/daily display mode.

Do not create a new icon.

---

## Step 2 — Convert the current schedule selector into the main interactive area of a capsule

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

The current selector is a standalone button containing:

```text
EmployeeIcon
schedule name
chevron
month/year
updated timestamp
```

Preserve:

```text
aria-haspopup="dialog"
aria-expanded
aria-label="Select schedule. Current schedule: ..."
onClick={() => setIsScheduleSelectOpen(true)}
```

Change its visual content to:

```text
[ ScheduleIcon ] | schedule name ▼
                   month/year
                   Updated ...
```

### Required DOM concept

Inside `scheduleSelectTrigger`, use:

```text
scheduleSelectTriggerIcon
scheduleSelectDivider
scheduleSelectTriggerCopy
```

Conceptually:

```tsx
<button className={styles.scheduleSelectTrigger}>
    <span className={styles.scheduleSelectTriggerIcon}>
        <ScheduleIcon />
    </span>

    <span className={styles.scheduleSelectDivider} />

    <span className={styles.scheduleSelectTriggerCopy}>
        ...
    </span>
</button>
```

The divider is decorative:

```text
aria-hidden="true"
```

Do not change the text/data formatting logic.

Keep:

```text
selectedSchedule.name
formatScheduleMonth(selectedSchedule)
formatScheduleLastUpdate(...)
```

unchanged.

---

## Step 3 — Create one composite schedule-control row

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

The approved UI visually makes the selected schedule and `Adjust` action one element.

Do **not** put `Adjust` inside `scheduleSelectTrigger`.

Instead create one ReactNode, conceptually:

```text
scheduleSelectorRow
```

with this structure:

```text
<div class="scheduleSelectorRow">

    <button class="scheduleSelectTrigger">
        calendar
        divider
        schedule metadata
    </button>

    <button class="openScheduleCorrectionButton">
        Adjust
    </button>

</div>
```

### Selector row responsibility

The wrapper is presentation only.

It owns:

- rounded capsule background;
- border;
- horizontal layout;
- shared padding;
- spacing.

It owns **no business state**.

### Main trigger

Click:

```text
→ opens EmployeeScheduleSelectDialog
```

### Adjust

Click:

```text
→ setShiftCorrectionError(null)
→ setIsShiftCorrectionDialogOpen(true)
```

This is exactly the current Adjust behavior and must remain unchanged.

Build this schedule-control row once and reuse the same ReactNode in both matrix and daily rendering branches.

Do not duplicate independently authored versions.

---

## Step 4 — Move the schedule-view toggle to the top-right actions

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

### Remove current behavior from `icon`

Currently both matrix and daily views pass an interactive button through the `icon` prop.

Remove these `icon={...}` props from both:

- `ContainerGraphMatrix`;
- `CardSection`.

After this change:

```text
Schedules
```

must be plain title text.

The title must no longer have a clickable icon beside it.

---

## Step 5 — Create one top-right actions ReactNode

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Create one shared ReactNode for:

```text
[view-mode icon] [PDF]
```

Conceptually:

```text
scheduleHeaderActions
```

The order must be:

```text
View mode button
PDF button
```

matching the approved reference.

### View-mode button

Reuse:

```text
scheduleViewToggleRef
handleToggleScheduleView
```

Use:

```tsx
<ScheduleDetailsIcon size={...} />
```

instead of `ScheduleIcon`.

It must remain icon-only.

Do not render text such as:

```text
View
Mode
Daily
Matrix
```

inside the control.

### Accessibility

When matrix is currently displayed:

```text
aria-label="Show daily schedule view"
title="Show daily schedule view"
aria-pressed="false"
```

When daily is currently displayed:

```text
aria-label="Show schedule matrix view"
title="Show schedule matrix view"
aria-pressed="true"
```

Derive these values from `scheduleViewMode`, instead of maintaining two separate manually authored copies.

### PDF

Keep:

```text
handleExportPdf
aria-label="Export schedule to PDF"
title="Export schedule to PDF"
```

unchanged.

---

## Step 6 — Reuse the new header composition in matrix mode

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

For `ContainerGraphMatrix`, the final header props must conceptually become:

```text
title = "Schedules"

icon = NONE

headerRightSlot =
    [view-mode button] [PDF]

headerCenterSlot =
    [rounded schedule selector + Adjust]
```

Keep matrix data/behavior props unchanged.

Do not change matrix behavior.

---

## Step 7 — Reuse exactly the same header composition in daily mode

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

For daily `CardSection`, use the same:

```text
title = "Schedules"

icon = NONE

headerRightSlot =
    [view-mode button] [PDF]

headerCenterSlot =
    [rounded schedule selector + Adjust]
```

Do not create a separate CSS/DOM version specifically for daily mode.

Keep daily content unchanged:

- day tabs;
- active day;
- scrolling;
- workers;
- related schedules;
- empty-day UI.

---

# 5. Styling Changes

## Step 8 — Restyle the top header

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

Keep the existing general three-row structure:

```text
row 1
divider
row 3
```

Target:

```text
grid-template-columns:
    minmax(0, 1fr) auto

row 1:
Schedules             View PDF

row 2:
divider

row 3:
schedule selector row spanning all columns
```

### Title

Update `.openScheduleHeaderTitle` so it no longer expects an icon column.

Remove the page-specific two-column title grid.

Style the title as a simple text heading.

Target:

```text
color: #0f172a
font-weight: 850–900
```

Use a size visually consistent with the reference while keeping the current compact page proportions.

Do not make it an oversized page-level H1.

---

## Step 9 — Change `.openScheduleHeaderRight` / `.openScheduleActions`

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

The current implementation uses `display: contents` to place PDF and Adjust into different grid rows.

This is no longer appropriate.

After the redesign:

- top-right contains only View + PDF;
- Adjust belongs to `scheduleSelectorRow`.

Change the top-right action container to a normal flex group.

Target:

```text
display: flex
align-items: center
justify-content: flex-end
gap: 8px
```

Do not use `display: contents`.

---

## Step 10 — Style the new view-mode button

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Reuse `.scheduleViewToggle` but change it to match the approved icon-only button near PDF.

Target visual:

```text
╭──────╮
│ list │
╰──────╯
```

Recommended geometry:

```text
width: 34–38px
height: 34–38px
padding: 0
border-radius: 12–14px
```

Visual:

```text
border:
1px solid rgba(37, 99, 235, 0.20)

background:
rgba(239, 246, 255, 0.94)

color:
#2563eb
```

Hover:

- very small upward shift or background increase;
- no large movement.

Focus-visible:

- existing blue focus ring.

Daily-mode active state may use slightly stronger blue border/background but must remain visually within the same component style.

Do not make the button solid blue.

---

## Step 11 — Reduce the current toggle pulse animation

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Replace the existing strong pulse/rotation with a subtler interaction.

Target concept:

```text
start  0.96
middle 1.04
end    1
```

Allow a small blue glow.

Remove:

- strong rotation;
- solid dark-blue flash;
- large pulse ring.

Keep:

```text
hasSwitchedScheduleView
scheduleViewTogglePulse
```

logic unchanged.

---

## Step 12 — Keep and refine PDF button styling

**Action:** MODIFY / REUSE

**File:**

`EmployeeSchedulePage.module.css`

Keep the existing semantic colors:

```text
#fff1f2 background
#fecdd3 border
#e11d48 text
```

Adjust only sizing if necessary so that PDF aligns visually with the new View button.

Target:

```text
height: roughly equal to view button
padding-inline: 12–14px
border-radius: 999px
```

Do not make PDF solid red.

---

# 6. Selected Schedule Capsule

## Step 13 — Create `.scheduleSelectorRow`

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Add a dedicated class:

```text
.scheduleSelectorRow
```

Target visual:

```text
╭────────────────────────────────────────────╮
│ [calendar] │ F27 ▼                 Adjust │
│            │ July 2026                    │
│            │ Updated ...                  │
╰────────────────────────────────────────────╯
```

Recommended structure:

```text
display: flex
align-items: center
gap: 8–10px
min-width: 0
```

Target surface:

```text
border:
1px solid rgba(37, 99, 235, 0.14–0.18)

background:
light blue/white glass gradient

border-radius:
20–24px

padding:
8–10px
```

Recommended background direction:

```css
linear-gradient(
    135deg,
    rgba(239, 246, 255, 0.88),
    rgba(255, 255, 255, 0.96)
)
```

Keep it subtle.

It should appear as **one component**, not another large section.

---

## Step 14 — Restyle `.scheduleSelectTrigger`

**Action:** MODIFY

Remove the current isolated-button visual behavior.

The trigger now lives inside `.scheduleSelectorRow`.

Target:

```text
flex: 1 1 auto
min-width: 0
display: grid
```

Its own background should remain transparent so the outer capsule remains the visual surface.

Do not create an additional rounded rectangle on hover.

A subtle internal hover/focus effect is acceptable, but the outer capsule must remain visually unified.

---

## Step 15 — Use a calendar icon for the schedule

**Action:** MODIFY

Style:

```text
.scheduleSelectTriggerIcon
```

Target:

```text
44px × 44px
border-radius: 14–16px
background: #dbeafe / equivalent translucent blue
color: #2563eb
```

Render:

```text
ScheduleIcon
```

not `EmployeeIcon`.

There must now be only one prominent schedule/calendar icon in the selector area.

---

## Step 16 — Add the selector's internal vertical divider

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Create:

```text
.scheduleSelectDivider
```

Target:

```text
width: 1px
align-self: stretch
```

Use a subtle color such as:

```text
rgba(148, 163, 184, 0.24)
```

It should visually divide:

```text
calendar icon | metadata
```

Do not extend beyond the internal padded height of the capsule.

---

## Step 17 — Keep metadata hierarchy

**Action:** MODIFY / REUSE

Keep:

```text
.scheduleSelectNameRow
.scheduleSelectName
.scheduleSelectChevron
.scheduleSelectPeriod
.scheduleSelectUpdated
```

Target hierarchy:

```text
F27 ▼                  darkest / boldest

July 2026              muted

Updated ...            blue
```

Keep:

```css
.scheduleSelectUpdated {
    color: #3b82f6;
}
```

Long schedule names must:

```text
overflow hidden
text-overflow ellipsis
white-space nowrap
```

---

## Step 18 — Move Adjust into the selector capsule

**Action:** MODIFY

Use the existing correction behavior and existing blue semantic styling.

The Adjust button becomes the right-side sibling inside:

```text
.scheduleSelectorRow
```

Target:

```text
light-blue pill
thin blue border
blue text
```

Keep colors based on existing:

```text
#eff6ff
#bfdbfe
#2563eb
```

Target geometry:

```text
min-height: 32–36px
padding-inline: 12–14px
border-radius: 999px
flex: 0 0 auto
```

Do not allow Adjust to shrink or wrap.

---

# 7. Header Grid Finalization

## Step 19 — Make the center slot span the full header width

**Action:** MODIFY

**File:**

`EmployeeSchedulePage.module.css`

Change the center-slot rule so the new selector capsule spans:

```text
grid-column: 1 / -1
grid-row: 3
```

This gives the selector the full inner width of the Schedule card.

The Adjust button will therefore align naturally inside the capsule rather than being aligned through the outer header grid.

---

## Step 20 — Preserve the horizontal divider

**Action:** REUSE

Keep the current header divider between:

```text
Schedules / View / PDF
```

and:

```text
schedule selector capsule
```

Only spacing may be adjusted.

Do not remove it.

---

# 8. Responsive Behavior

## Step 21 — Verify compact mobile layouts

**Action:** MODIFY CSS ONLY WHERE REQUIRED

Target viewport widths:

```text
360px
390px
430px
520px
```

Required behavior:

### Top row

Always remain:

```text
Schedules          [view] [PDF]
```

Do not wrap View or PDF to a second line under normal supported mobile widths.

### Selector

Always remain conceptually:

```text
[calendar] | metadata         Adjust
```

At narrower widths:

- metadata receives remaining flexible width;
- schedule name truncates;
- Updated text may wrap;
- Adjust stays visible;
- no horizontal overflow.

Do not hide Adjust.

Do not hide the schedule icon.

Do not introduce viewport-width calculations.

Use:

```text
min-width: 0
flex
grid
gap
```

---

# 9. Tests

## Step 22 — Update `EmployeeSchedulePage.test.tsx`

**Action:** MODIFY

**File:**

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

Keep the existing schedule-mode switching tests using the accessible names:

```text
Show daily schedule view
Show schedule matrix view
```

### Add/adjust assertions

Initial matrix state:

```text
button "Show daily schedule view"
aria-pressed="false"
```

After clicking it:

```text
button "Show schedule matrix view"
aria-pressed="true"
```

Click again:

```text
schedule matrix exists
```

This confirms that moving the button did not change the functionality.

---

## Step 23 — Preserve selector tests

**Action:** REUSE / UPDATE ONLY IF DOM TEXT STRUCTURE REQUIRES

Do not weaken the existing selector tests.

They must continue verifying:

- trigger opens selector;
- selected schedule is focused;
- switching to another schedule updates metadata;
- matrix receives new schedule;
- Escape closes;
- close button closes;
- overlay closes;
- missing timestamp fallback works.

The schedule-selection `aria-label` must remain:

```text
Select schedule. Current schedule: ...
```

---

## Step 24 — Preserve PDF and Adjust accessibility

Ensure tests still find:

```text
Export schedule to PDF
Request a shift correction
```

Do not change those accessible names merely for the redesign.

---

# 10. Data / API / Persistence Changes

## API

None.

## Backend

None.

## DTOs / entities

None.

## Persistence

None.

## Database

None.

## Dependency Injection

None.

## Configuration

None.

## New dependencies

None.

---

# 11. Error and Edge Case Requirements

### No selected schedule

Preserve the current loading/empty states.

Do not render the selector row when no selected schedule exists.

### One schedule

The capsule still renders normally.

Clicking it can still open the selector containing one schedule.

### Long schedule name

Truncate name with ellipsis.

Do not allow it to collide with Adjust.

### Missing `lastUpdatedAtUtc`

Preserve:

```text
Not recorded yet
```

Do not produce:

```text
Updated Not recorded yet
```

### Daily/matrix mode

View toggle remains available in both modes at exactly the same visual location.

### Focus after toggle

Continue focusing `scheduleViewToggleRef` after a mode switch.

Moving the button must not break this behavior.

### Selector + Adjust interaction

Clicking the selector opens the schedule dialog.

Clicking Adjust must **not** open the schedule selector.

They are sibling controls, not nested controls.

---

# 12. Verification

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

If focused tests pass:

```bash
npm test
```

---

# 13. Manual Visual Verification

Test:

```text
360px
390px
430px
>= 860px
```

Both:

```text
matrix mode
daily mode
```

Final visual target:

```text
┌───────────────────────────────────────┐
│ Schedules                  [▤]  PDF  │
│ ───────────────────────────────────── │
│                                       │
│ ╭───────────────────────────────────╮ │
│ │ [📅] │ F27 ▼             Adjust │ │
│ │      │ July 2026                  │ │
│ │      │ Updated 04 Jul ...         │ │
│ ╰───────────────────────────────────╯ │
│                                       │
│ Day              OLEH PROTSUN         │
│ ...                                   │
└───────────────────────────────────────┘
```

Verify:

- no calendar/view icon exists to the left of `Schedules`;
- `Schedules` is plain heading text;
- dedicated icon-only view button appears before PDF;
- view button toggles display mode;
- selector is one rounded surface;
- one calendar icon exists inside selector;
- internal vertical divider exists;
- Adjust is inside the same visual surface;
- Adjust remains an independent button;
- PDF style matches reference;
- View style matches reference;
- selector opening still works;
- bottom sheet still works;
- Updated text remains blue;
- matrix and daily view both retain the same header structure.

---

# 14. Acceptance Checklist

- [ ] `Schedules` no longer has an interactive icon to its left.
- [ ] Schedule-mode switching is moved to a dedicated icon-only button near PDF.
- [ ] View-mode button uses `ScheduleDetailsIcon`.
- [ ] View-mode button still calls `handleToggleScheduleView`.
- [ ] Matrix → daily works.
- [ ] Daily → matrix works.
- [ ] Focus restoration still works.
- [ ] View button has correct dynamic `aria-label`.
- [ ] View button has correct `aria-pressed`.
- [ ] PDF remains immediately beside the view button.
- [ ] PDF export behavior is unchanged.
- [ ] Schedule selector is one rounded capsule.
- [ ] Selector uses `ScheduleIcon`.
- [ ] `EmployeeIcon` is removed from the inline selector.
- [ ] Selector includes a subtle vertical divider.
- [ ] Schedule name and chevron are visible.
- [ ] Month/year is visible.
- [ ] Updated timestamp remains light blue.
- [ ] Adjust appears inside the selector capsule visually.
- [ ] Adjust remains a separate button semantically.
- [ ] Adjust functionality is unchanged.
- [ ] Clicking Adjust does not open schedule selection.
- [ ] Clicking the schedule portion opens the existing selector bottom sheet.
- [ ] Selector bottom-sheet behavior is unchanged.
- [ ] Matrix content is unchanged.
- [ ] Daily content is unchanged.
- [ ] No API changes.
- [ ] No backend changes.
- [ ] No persistence changes.
- [ ] No new dependencies.
- [ ] Focused tests pass.
- [ ] Lint passes.
- [ ] Build passes.

---

# 15. Exact Change Scope

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

Responsibilities:

- replace selector's Employee icon with Schedule icon;
- add internal divider;
- create unified selector + Adjust row;
- move mode toggle from `icon` prop into top-right header actions;
- use `ScheduleDetailsIcon`;
- remove `icon` prop from matrix/daily header;
- reuse one header action composition across modes;
- preserve all existing behavior.

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

Responsibilities:

- restyle header;
- top-right View + PDF actions;
- selector capsule;
- internal vertical divider;
- calendar icon block;
- Adjust placement;
- responsive behavior;
- subtle toggle animation.

### MODIFY

`FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

Responsibilities:

- preserve view-mode integration test;
- verify relocated mode toggle semantics;
- preserve selector/PDF/Adjust behavior.

### REUSE UNCHANGED

`FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`

`FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx`

`FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx`

`FrontEnd/src/shared/ui/icons/ScheduleIcon.tsx`

`FrontEnd/src/shared/ui/icons/ScheduleDetailsIcon.tsx`

---

# 16. Do Not Change

Do not modify:

```text
BusinessLogicLayer/
DataAccessLayer/
GF3.WebApi/
GF3.Launcher/
```

Do not change:

- schedule API;
- schedule entities;
- shift-correction API;
- PDF generation;
- Work hours logic;
- salary logic;
- column ordering;
- employee UI-state persistence;
- schedule hero;
- employee navigation;
- global CardSection styling;
- global matrix styling.

Do not create a new shared abstraction for this page-only visual change.

The task must remain a focused Schedule-header/selector redesign.
