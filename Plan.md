# Plan

## 1. Objective

Redesign **only the mobile employee bottom navigation**:

The current mobile navigation contains five employee destinations plus the collapse button. Every navigation item currently renders both its icon and mobile label, while the container uses five equal grid columns plus the collapse control.

Replace that visual behavior with the approved design:

```text
Inactive tabs: ICON ONLY

Active tab:
┌──────────────────┐
│  [icon]  Shifts  │
└──────────────────┘

Full navigation:

╭─────────────────────────────────────────────╮
│  [icon]  [icon]  [icon + LABEL] [icon] [icon]  (↩) │
╰─────────────────────────────────────────────╯
```

For example, on `/schedule`:

```text
[Alerts icon] [Avail icon] [ calendar  Shifts ] [Swap icon] [Profile icon] [↩]
                         ↑
                    active expanded pill
```

When the user navigates to another section:

```text
[Alerts icon + Alerts] [Avail icon] [Shifts icon] [Swap icon] [Profile icon] [↩]
```

The active pill moves conceptually to the newly selected section because that item becomes expanded while the previous one collapses.

### Required visual behavior

- Inactive navigation destinations show **only their icons**.
- The currently active destination shows icon + label immediately to the **right** of the icon.
- Active destination is wider than inactive destinations.
- Active destination is a horizontally expanded rounded pill.
- Outer navigation is a strongly rounded/capsule shape.
- Existing matte/frosted glass material of the outer navigation must be preserved.
- Existing blue GF3 active-navigation palette must be preserved.
- Collapse/back control remains at the far right as a separate circular control.
- Navigation transition should be smooth when route changes.
- Existing unread notification dots continue working.
- Existing collapse/reopen behavior continues working.

---

## 2. Existing Components to Reuse

### REUSE — `EmployeeWorkspaceLayout`

File:

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`

Continue using the existing `employeeNavItems` with:

- `/` → Alerts;
- `/availability` → Avail.;
- `/schedule` → Shifts;
- `/swap` → Swap;
- `/profile` → Profile.

Do not duplicate this navigation configuration.

Continue using the existing:

- `NavLink`;
- `isActive`;
- `aria-current`;
- translated `mobileLabel`;
- unread-target logic;
- `navUnreadDot`;
- collapse state;
- `mobileNavRef`;
- focus-transfer behavior;
- `mobileToggleButton`;
- `mobileOpenTab`.

Use the existing route-derived active state to control the new expanded pill. Do not introduce new active-tab React state.

### REUSE — current glass navigation material

File:

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Preserve the existing `.mobileTabs` material:

```text
background: rgba(255, 255, 255, 0.16)
border: 1px solid rgba(0, 0, 0, 0.08)
box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08)
backdrop-filter: blur(20px) saturate(180%)
```

Do **not** replace it with solid white, opaque gray, opaque blue, dark navigation, or a new glass library.

### REUSE — current colors

Continue using the existing navigation colors:

```text
muted/inactive: #64748b
active:         #1d4ed8
primary blue:   #2563eb
```

### REUSE — current icons

Do not replace or redraw:

- `NoteIcon`;
- `AvailabilityIcon`;
- `ScheduleIcon`;
- `EmployeeIcon`;
- `BackIcon`.

### REUSE — collapse mechanics

Keep:

```ts
isMobileTabsCollapsed
setIsMobileTabsCollapsed(...)
```

and existing focus behavior.

The right-side control must continue collapsing the navigation. The separate `mobileOpenTab` must continue reopening it.

---

## 3. Constraints

This task is a **mobile navigation visual redesign only**.

Do not:

- change routing;
- change employee destinations;
- add navigation destinations;
- remove destinations;
- alter notification fetching;
- alter unread-dot calculation;
- change authentication;
- change APIs;
- change backend code;
- change database code;
- change employee pages;
- add dependencies;
- introduce a navigation component library;
- replace existing icons;
- change desktop navigation design;
- redesign the collapse architecture;
- change the mobile navigation's fixed positioning;
- change its safe-area behavior;
- introduce JavaScript width calculations;
- use absolute positioning for normal tab layout;
- calculate widths from `window.innerWidth`;
- hardcode per-route positions.

Use CSS flex/layout state instead.

---

# 4. Implementation Steps

## Step 1 — Change the mobile navigation label markup

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`

Keep every label in the DOM but give it a dedicated mobile-navigation class:

```text
mobileTabLabel
```

The mobile item must conceptually become:

```tsx
<NavLink ...>
  <span className={styles.tabIcon}>
    {item.icon}
    {unread dot}
  </span>

  <span
    className={styles.mobileTabLabel}
    aria-hidden="true"
  >
    {item.mobileLabel ?? item.label}
  </span>
</NavLink>
```

Do not conditionally mount/unmount it based on `isActive`.

Keeping it present allows CSS to animate:

```text
width/max-width
opacity
transform
```

when the tab becomes active.

Keep the existing `aria-label` on `NavLink`. Because that already provides the accessible navigation name, make the visual label `aria-hidden="true"` to avoid redundant screen-reader output.

Do not change unread information in the `aria-label`.

---

## Step 2 — Remove the animated background indicator from the MOBILE navigation only

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`

For the **mobile navigation only**, remove the `data-employee-nav-indicator` span.

Keep the indicator in `desktopTabs` unchanged.

Mobile selection styling will now belong directly to `.mobileTabActive` instead of a separately positioned background indicator.

Do not modify:

`FrontEnd/src/app/layouts/employee-workspace-layout/useEmployeeMotion.ts`

---

## Step 3 — Convert the mobile navigation from equal-column grid to adaptive flex layout

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Replace the current equal-column grid layout with flex layout:

```text
display: flex
align-items: center
```

Keep a small consistent gap between controls.

The five `NavLink` items must share the available width flexibly.

Conceptual sizing:

```text
inactive tab → flex-grow approximately 1
active tab   → flex-grow approximately 2–2.2
collapse     → fixed width
```

Use CSS flex factors, **not viewport calculations**.

The intended geometry at mobile widths is approximately:

```text
| icon | icon | icon + Shifts | icon | icon | circle |
```

The layout must automatically redistribute when another item becomes active.

---

## Step 4 — Change the outer navigation shape to a capsule

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Change the outer `.mobileTabs` shape to a stronger capsule.

Target:

```css
border-radius: 999px;
```

Preserve exactly:

- fixed positioning;
- left/right safe-area calculations;
- bottom safe-area calculation;
- z-index;
- glass background;
- glass border;
- glass shadow;
- backdrop blur;
- saturation;
- expanded/collapsed transition.

Do not increase opacity enough to lose the matte-glass appearance.

---

## Step 5 — Redesign inactive mobile tabs

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Change `.mobileTab` to centered inline-flex content.

Required base behavior:

```text
display: inline-flex
align-items: center
justify-content: center
min-width: 0
```

Keep:

- transparent base border;
- muted `#64748b` color;
- sufficient touch height;
- rounded shape;
- existing focus-visible behavior.

Inactive tab:

```text
[ icon ]
```

The `.mobileTabLabel` must be visually collapsed.

### Inactive label state

Implement with CSS equivalent to:

```text
max-width: 0
opacity: 0
overflow: hidden
white-space: nowrap
transform: translateX(-4px)
```

Do not use `display: none` because the label should transition smoothly when active state changes.

---

## Step 6 — Implement the expanded active tab

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

`.mobileTabActive` becomes the selected expanded capsule.

### Structure

```text
╭──────────────────╮
│ [icon]  Shifts   │
╰──────────────────╯
```

### Active layout

Use:

```text
display: inline-flex
align-items: center
justify-content: center
```

with approximately `6–8px` gap between icon and label.

### Active width

Increase its flex share relative to inactive controls.

Target concept:

```text
inactive flex factor: 1
active flex factor:   ~2.1
```

Exact value may be adjusted within the stylesheet only to make the layout fit cleanly at required widths, but do not use fixed per-route pixel positions.

### Active surface

Use a light-blue glass/pill treatment consistent with GF3:

```text
very-light-blue translucent background
thin blue border
blue foreground
soft blue shadow
subtle white inset highlight
```

Use existing GF3 blue values.

Target visual direction:

```text
background:
linear-gradient(
  180deg,
  rgba(239, 246, 255, ~0.96),
  rgba(219, 234, 254, ~0.9)
)

border:
rgba(59, 130, 246, ~0.24)

text:
#1d4ed8

icon:
#2563eb
```

Do not make the selected pill solid dark blue.

---

## Step 7 — Reveal the active label beside the icon

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Add styles for:

```text
.mobileTabLabel
.mobileTabActive .mobileTabLabel
```

Inactive:

```text
width collapsed
opacity 0
slightly shifted left
```

Active:

```text
max-width enough for compact mobile label
opacity 1
translateX(0)
```

Active label must:

- stay on one line;
- use existing compact mobile label text;
- use `overflow: hidden`;
- use `text-overflow: ellipsis` if localized text becomes too long;
- never push neighboring buttons outside the navigation.

Continue using:

```text
Alerts
Avail.
Shifts
Swap
Profile
```

through the existing translation mechanism.

Do not hardcode these strings in CSS or duplicate them in another data structure.

---

## Step 8 — Add smooth active-tab transitions

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Animate the change using CSS only.

Use the existing `--motion-ease` for the main expansion.

Animate relevant properties such as:

```text
flex-grow
background
border-color
box-shadow
color
gap
```

and for the label:

```text
max-width
opacity
transform
```

Target duration:

```text
~260–360ms for expansion
~160–220ms for label opacity
```

Do not introduce another GSAP animation for this behavior.

---

## Step 9 — Remove the old mobile active-icon vertical jump

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Current styling applies a vertical lift/scale to active icons.

Split the rule.

### Mobile

Active icon must remain vertically centered:

```text
translate: 0 0
```

A subtle scale around `1.04–1.08` is acceptable.

### Desktop

Preserve the existing desktop behavior.

Do not visually redesign desktop navigation.

---

## Step 10 — Preserve unread notification dots

**Action:** REUSE

No logic changes.

Existing unread dot remains a child of `.tabIcon` and must remain visible on both active and inactive tabs.

Do not move unread-dot logic into the label.

Do not change its business rules.

---

## Step 11 — Make the collapse control circular

**Action:** MODIFY

**File:**

`FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`

Keep `mobileToggleButton` at the far right of the expanded navigation.

Change its visual shape to a circle:

```css
border-radius: 999px;
```

Preserve its current dimensions depending on breakpoint.

Keep:

- `BackIcon`;
- click behavior;
- glass background;
- border;
- hover;
- active;
- focus-visible behavior.

Do not merge this control into the active tab.

---

## Step 12 — Keep collapsed-navigation mechanics untouched

**Action:** REUSE

Do not change:

```text
.mobileTabsExpanded
.mobileTabsCollapsed
.mobileOpenTab
.mobileOpenTabVisible
.mobileOpenTabHidden
.shellTabsCollapsed
```

except for any minimal inherited layout compatibility caused by changing `.mobileTabs` from grid to flex.

Existing inert/focus behavior must remain unchanged.

---

## Step 13 — Preserve safe-area and fixed positioning

**Action:** REUSE

Do not alter the current safe-area-aware left/right/bottom positioning.

Do not move the navigation flush against the screen edges.

Do not make it full-bleed.

The navigation must continue to visually float above the lower edge.

---

# 5. Final Expected States

## Alerts active

```text
╭─────────────────────────────────────────────────╮
│ [📝 Alerts]    [✓]    [▦]    [⇄]    [👤]   (↩) │
╰─────────────────────────────────────────────────╯
```

## Availability active

```text
╭─────────────────────────────────────────────────╮
│ [📝]    [✓ Avail.]    [▦]    [⇄]    [👤]   (↩) │
╰─────────────────────────────────────────────────╯
```

## Shifts active

```text
╭─────────────────────────────────────────────────╮
│ [📝]    [✓]    [▦ Shifts]    [⇄]    [👤]   (↩) │
╰─────────────────────────────────────────────────╯
```

## Swap active

```text
╭─────────────────────────────────────────────────╮
│ [📝]    [✓]    [▦]    [⇄ Swap]    [👤]    (↩) │
╰─────────────────────────────────────────────────╯
```

## Profile active

```text
╭─────────────────────────────────────────────────╮
│ [📝]    [✓]    [▦]    [⇄]    [👤 Profile] (↩) │
╰─────────────────────────────────────────────────╯
```

Only **one** destination label is visible at any time.

---

# 6. Data / API / Persistence Changes

## API

None.

## Backend

None.

## Models / DTOs

None.

## Persistence

None.

## Dependency Injection

None.

## Configuration

None.

## Dependencies

None.

---

# 7. Error and Edge Case Requirements

### Very narrow mobile viewport

At `360px` width:

- all five destination icons remain visible;
- collapse button remains visible;
- active label remains visible when practical;
- overly long localized active labels truncate with ellipsis rather than overflowing;
- no horizontal page scrolling is introduced.

### Long translations

The label container must have:

```text
min-width: 0
overflow: hidden
text-overflow: ellipsis
white-space: nowrap
```

or equivalent behavior.

### Route switch

When active route changes:

- previous tab loses its label;
- previous tab returns to icon-only state;
- new active tab expands;
- new label appears;
- only one active label remains.

No additional component state should be required.

### Unread tab

If an inactive item contains an unread dot, the dot remains attached to its icon.

If that item becomes active, the dot remains visible unless existing application logic marks it read.

### Collapsed navigation

Changing the visual structure must not affect:

- collapse click;
- reopen click;
- `inert`;
- focus restoration.

### Desktop boundary

At `min-width: 860px`, the existing desktop navigation must continue replacing the mobile navigation.

---

# 8. Tests

This is primarily a CSS/layout change.

Do not create a dedicated testing abstraction or visual-regression framework for this task.

No backend tests are required.

### Existing automated suite

Run the existing frontend tests after implementation.

### Manual interaction verification

For each route:

```text
/
/availability
/schedule
/swap
/profile
```

verify:

1. route navigation works;
2. only selected tab displays its label;
3. previous tab collapses to icon only;
4. selected pill expands horizontally;
5. unread dot remains correct;
6. collapse button works;
7. reopen button works.

---

# 9. Verification

From:

```bash
cd FrontEnd
```

run:

```bash
npm test
```

then:

```bash
npm run lint
```

then:

```bash
npm run build
```

### Responsive visual verification

Verify manually at:

```text
360px
390px
430px
520px
859px
860px
```

### 360 / 390 / 430 / 520 / 859

Expected:

- mobile glass navigation visible;
- outer capsule shape;
- one expanded selected pill;
- selected icon + label horizontally aligned;
- all other destinations icon-only;
- circular collapse button;
- no horizontal overflow;
- no labels below inactive icons;
- no active icon vertical jump;
- matte glass still clearly visible.

### 860px

Expected:

- mobile navigation disappears;
- existing desktop navigation appears;
- desktop navigation is visually unchanged.

---

# 10. Acceptance Checklist

- [ ] Mobile navigation keeps the current matte/frosted glass background.
- [ ] Outer navigation is capsule-shaped.
- [ ] Alerts inactive state shows icon only.
- [ ] Availability inactive state shows icon only.
- [ ] Shifts inactive state shows icon only.
- [ ] Swap inactive state shows icon only.
- [ ] Profile inactive state shows icon only.
- [ ] Exactly one active destination displays its label.
- [ ] Active label is positioned to the right of its icon.
- [ ] Active tab expands horizontally.
- [ ] Inactive tabs shrink back to icon-only controls.
- [ ] Selected tab uses light-blue GF3 styling.
- [ ] Selected tab is not a solid dark-blue block.
- [ ] Active change animates smoothly.
- [ ] Mobile tab icons remain vertically centered.
- [ ] Existing desktop active icon animation remains unchanged.
- [ ] Existing unread dots continue working.
- [ ] Existing translations continue being used.
- [ ] Existing `aria-label` navigation naming remains intact.
- [ ] Existing focus-visible behavior remains intact.
- [ ] Collapse button is circular.
- [ ] Collapse button still collapses navigation.
- [ ] Open-navigation button still restores navigation.
- [ ] Expanded/collapsed focus transfer continues working.
- [ ] Mobile safe-area positioning remains unchanged.
- [ ] No horizontal overflow at 360px.
- [ ] Desktop navigation is unchanged at `>=860px`.
- [ ] No API change.
- [ ] No backend change.
- [ ] No database change.
- [ ] No dependency added.
- [ ] `npm test` passes.
- [ ] `npm run lint` passes.
- [ ] `npm run build` passes.

---

# 11. Exact Change Scope

## MODIFY

```text
FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx
```

Responsibilities of change:

- add dedicated class to visual mobile labels;
- mark visual label presentation-only;
- remove mobile-only animated indicator element;
- preserve all routing/accessibility/unread/collapse logic.

## MODIFY

```text
FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css
```

Responsibilities of change:

- capsule outer navigation;
- grid → flex mobile layout;
- icon-only inactive tabs;
- horizontally expanded active pill;
- active label reveal;
- responsive width redistribution;
- transitions;
- circular collapse button;
- split mobile vs desktop active-icon transform.

## REUSE UNCHANGED

```text
FrontEnd/src/app/layouts/employee-workspace-layout/useEmployeeMotion.ts
```

Desktop indicator continues to use it; mobile no longer requires an indicator.

---

# 12. Do Not Change

Do not modify:

```text
BusinessLogicLayer/
DataAccessLayer/
GF3.WebApi/
GF3.Launcher/
```

Do not change employee page implementations:

```text
employee-notifications
employee-availability
employee-schedule
employee-swap
employee-account/profile
```

Do not change:

- route definitions;
- employee navigation destination list;
- data queries;
- SignalR/realtime behavior;
- unread-notification calculations;
- notification read-state persistence;
- authentication/session behavior;
- schedule logic;
- availability logic;
- swap logic;
- communication dialog;
- global design system architecture.

Do not update desktop navigation merely to make its appearance match mobile.

The implementation should remain a focused two-file mobile navigation redesign.
