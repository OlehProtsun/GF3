# Plan

## 1. Objective

Modernize motion on the Employee `/schedule` workflow without changing its business behavior, data contracts, or architecture.

The finished page must feel gradual, dynamic, energetic, colorful, minimal, and responsive through:

- coherent section entrance motion;
- smooth Matrix ↔ Daily state changes;
- animated schedule/day/summary state changes;
- responsive button, tab, selector, and dialog micro-interactions;
- smooth centering of the selected Daily day;
- lightweight list/dialog entrance choreography;
- immediate visual feedback for success/error states;
- a complete `prefers-reduced-motion` fallback;
- compositor-friendly animation that does not create continuous CPU/GPU work when the page is idle.

The implementation must reuse the existing Employee motion infrastructure and existing GSAP dependency. Do not add a new animation library.

## 2. Existing Components to Reuse

Reuse these existing mechanisms as the implementation foundation:

- `FrontEnd/src/app/layouts/employee-workspace-layout/useEmployeeMotion.ts`
  - existing scoped Employee GSAP orchestration;
  - existing `[data-employee-motion]` discovery;
  - existing `data-motion-key` replay mechanism;
  - existing `MutationObserver` cleanup and tween cancellation.
- `FrontEnd/src/shared/ui/motion.css`
  - global easing;
  - global press animation;
  - existing list-reveal convention.
- `gsap` and `@gsap/react` already installed in `FrontEnd/package.json`.
- `EmployeeWorkspaceLayout` motion scope. Do not create a second schedule-specific GSAP root/provider.
- `EmployeeScheduleHero`, `ContainerGraphMatrix`, `CardSection`, `SearchableSelect`, `ViewportOverlay`, and the three existing Schedule dialogs.
- Existing `aria-*`, focus restoration, keyboard behavior, and semantic roles.
- Existing schedule query/calculation/PDF/export/UI-state persistence logic.

Do not create a parallel motion service, context provider, event bus, animation store, or second observer.

## 3. Constraints

### Scope

- Frontend-only change.
- Target only Employee workflow `/schedule`, except for narrowly required shared reduced-motion safeguards.
- No backend, API, DTO, entity, persistence, database, dependency-injection, authentication, routing, or configuration changes.
- No changes to schedule calculations, shift-correction business rules, PDF generation, saved column-order semantics, or query/mutation contracts.
- Do not restore the removed Salary calculator or any other old Schedule content.

### Dependency constraints

- Do not add Framer Motion, Motion One, React Spring, ScrollTrigger, another GSAP package, or any new npm dependency.
- Do not change `FrontEnd/package.json`.
- Do not import GSAP directly into individual Schedule components unless explicitly listed in this plan. The page-level state choreography must continue through `useEmployeeMotion`.

### Motion/performance constraints

- Structural entrance/state animations must animate only compositor-friendly `opacity` and `transform`/GSAP `x`, `y`, `scale`.
- Color/border/background/shadow transitions are allowed only for short interaction feedback on a small number of controls.
- Do not animate `width`, `height`, `top`, `left`, grid tracks, table dimensions, or matrix cell geometry.
- Do not animate every `ContainerGraphMatrix` cell, every Schedule summary row, or all 28–31 Daily-day buttons independently.
- Do not add scroll listeners, pointer-move loops, `requestAnimationFrame` loops, intersection-observer choreography, or continuous idle animation.
- Do not animate the hero clock every second; its text may continue updating exactly as it does now, but the entrance animation must not replay on clock updates.
- Do not apply persistent `will-change` to large groups of elements.
- Keep list stagger bounded: only the first small set of visible dialog rows may be staggered; later rows enter together.
- On rapid state changes, existing tweens for the affected element must be killed before the next tween begins.

### Accessibility constraints

- Respect `prefers-reduced-motion: reduce` for JS and CSS motion.
- Reduced-motion mode must preserve all state changes and focus behavior but use immediate/near-immediate visual updates and non-smooth scrolling.
- Do not remove focus outlines, `aria-expanded`, `aria-pressed`, tab roles, dialog roles, or current focus restoration behavior.

## 4. Implementation Steps

### Step 1 — Extend the existing Employee GSAP motion presets and add reduced-motion handling

**Action:** EXTEND

**File:**
- `FrontEnd/src/app/layouts/employee-workspace-layout/useEmployeeMotion.ts`

**Changes:**

1. Keep the current public hook contract unchanged:
   - `useEmployeeMotion(scope, pathname)` remains the only exported API.
2. Preserve the existing navigation-indicator measurement/positioning logic and `ResizeObserver`.
3. Add a `prefers-reduced-motion` check to navigation-indicator movement:
   - when reduced motion is not requested, retain the existing animated route-indicator movement;
   - when reduced motion is requested, kill any active indicator tween and position the indicator immediately with `gsap.set`.
4. Keep `data-employee-motion="from-top"` excluded from GSAP because the Schedule hero owns its CSS entrance.
5. Refactor the `reveal(...)` branch into explicit presets selected by `element.dataset.employeeMotion`:
   - existing/default Employee reveal: preserve the current visual behavior and duration for non-Schedule consumers;
   - `schedule-panel`: `opacity 0.35 → 1`, `y 18 → 0`, no scale change, duration approximately `0.52s`, `power2.out`;
   - `schedule-content`: `opacity 0.45 → 1`, `y 12 → 0`, `scale 0.99 → 1`, duration approximately `0.42s`, `power2.out`;
   - `schedule-value`: `opacity 0.60 → 1`, `y 0`, `scale 0.96 → 1`, duration approximately `0.30s`, `power2.out`.
6. For Schedule presets, cap sequential reveal delay at four items and use approximately `45ms` between items. Keep the current default delay behavior for non-Schedule presets.
7. Preserve `clearProps: "opacity,transform"` after each completed tween so transformed elements do not keep unnecessary inline animation state.
8. Preserve the existing `WeakMap` + `data-motion-key` replay semantics.
9. Preserve the existing removed-node tween cleanup.
10. Wrap the reveal observer branch in a reduced-motion condition:
    - in `no-preference`, create the existing scoped `MutationObserver` and perform GSAP reveals;
    - in `reduce`, do not create a reveal observer solely for animation and do not run entrance/state tweens; DOM nodes must render naturally in their final state.
11. Use GSAP/context cleanup already provided by `useGSAP`; do not create a global animation registry.

**Behavior after change:**

- `/schedule` can request predictable motion categories without creating local GSAP code.
- Other Employee routes retain their existing default animation.
- Employee navigation and reveals stop moving when the OS/browser requests reduced motion.

**Dependencies:**
- Existing GSAP and `@gsap/react` only.

**Do not:**
- change the hook signature;
- add route-specific DOM queries for individual Schedule CSS classes;
- animate matrix/table descendants from this hook.

---

### Step 2 — Add shared reduced-motion safeguards for existing CSS motion primitives

**Action:** EXTEND

**Files:**
- `FrontEnd/src/shared/ui/motion.css`
- `FrontEnd/src/shared/ui/ViewportOverlay.module.css`
- `FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.module.css`

**Changes in `motion.css`:**

1. Keep all existing keyframes and default behavior unchanged for `prefers-reduced-motion: no-preference`.
2. Add `@media (prefers-reduced-motion: reduce)` rules that:
   - disable the global `gf3-press` active animation;
   - disable `[data-motion-list]` child entrance animations.
3. Do not globally disable color/focus-state changes.

**Changes in `ViewportOverlay.module.css`:**

1. Add a reduced-motion rule for `.surface` that disables its entrance animation.
2. Do not change overlay sizing, scrolling, overscroll, or viewport behavior.

**Changes in `SearchableSelect.module.css`:**

1. Keep normal SearchableSelect animation unchanged.
2. Under reduced motion:
   - disable `.dropdown` entrance animation;
   - make chevron/state transitions immediate;
   - remove active translate movement from `.selectButton` and `.option` while preserving visual selected/focus states.
3. Do not modify `SearchableSelect.tsx` or its public props/API.

**Behavior after change:**

- Existing shared controls used by `/schedule` stop spatial motion when reduced motion is requested.
- Normal animation on other pages remains unchanged.

**Do not:**
- redesign shared component visuals;
- change normal-duration tokens application-wide;
- change SearchableSelect portal positioning or listbox behavior.

---

### Step 3 — Wire deterministic motion keys into the `/schedule` state flow

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`

**Changes:**

1. Keep all existing query, memoized calculation, persistence, mutation, and PDF logic unchanged.
2. Keep `hasSwitchedScheduleView` only for the existing post-toggle focus behavior. Stop using it as the trigger for CSS entrance animation.
3. Remove `styles.scheduleViewEnter` from both:
   - `ContainerGraphMatrix.className`;
   - `CardSection.className` for Daily mode.
4. Remove `styles.scheduleViewTogglePulse` from the view-toggle class list. The button already has global press feedback and its active state; view-content motion will provide the transition feedback.
5. Add one stable Schedule view-stage wrapper rendered whenever `selectedSchedule` exists:
   - class: `styles.scheduleViewStage`;
   - `data-employee-motion="schedule-content"`;
   - `data-motion-key` exactly derived from selected schedule id and view mode, for example `${selectedSchedule.id}:${scheduleViewMode}`.
6. Render either the existing Matrix `ContainerGraphMatrix` or existing Daily `CardSection` inside that wrapper without changing their props or business behavior.
7. Preserve `scheduleViewToggleRef` focus restoration with `preventScroll: true`.
8. Change the existing Daily tabpanel motion preset from `schedule-panel` to `schedule-content`; keep its existing key based on `selectedSchedule.id` + selected day so day changes replay only the Daily content transition.
9. Add reduced-motion-aware Daily-day centering:
   - default/no-preference: `scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" })`;
   - reduced motion: keep `behavior: "auto"`;
   - preserve `block` and `inline` values.
   - use a small safe helper local to this module for the media query; do not create a new shared hook solely for this call.
10. Do not animate every day tab while scrolling. The strip itself scrolls normally; only the selected chip changes visual state.
11. Make the schedule-selector chevron reflect the already existing `isScheduleSelectOpen` state with a CSS modifier class, while preserving `aria-expanded`.
12. Animate feedback states through the existing Employee motion layer:
   - wrap each conditional `ErrorBanner` in a neutral layout wrapper with `data-employee-motion="schedule-content"` and a motion key derived from the current message/type;
   - add `data-employee-motion="schedule-content"` and a message-derived motion key to the shift-correction success status.
13. Animate Summary state changes without animating individual table rows:
   - add `data-employee-motion="schedule-value"` to `.hoursSummaryTotalPill`;
   - set its `data-motion-key` from `activeSummaryPeriod.key` plus `scheduleHoursSummary.totalHoursText`;
   - add `data-employee-motion="schedule-content"` to the Summary grid or the Summary empty-state element;
   - key that content by the active period key and whether it is `rows` or `empty`.
14. Keep the outer Summary panel's existing initial `data-employee-motion="schedule-panel"` reveal.
15. Do not add `data-motion-list` to:
   - the 28–31 Daily date tabs;
   - Summary rows;
   - matrix cells/columns.
16. Preserve stable worker keys (`worker.employeeId`). Do not force remounts just to restart CSS animations; the keyed parent Daily body is the state-transition animation boundary.

**Behavior after change:**

- Initial Schedule sections reveal coherently.
- Selecting another published schedule animates the large Schedule content once.
- Matrix ↔ Daily transitions animate through one central GSAP path rather than duplicate CSS keyframes.
- Changing a Daily date animates only the tabpanel content and smoothly centers the selected tab.
- Changing Summary month/year animates only the total and Summary content container.
- Rapid changes are safe because `useEmployeeMotion` kills the previous tween before starting the next.

**Dependencies:**
- Step 1 presets.

**Do not:**
- change Schedule selection semantics;
- change matrix props;
- change Summary calculations;
- remount data-heavy tables solely for animation.

---

### Step 4 — Replace duplicate Schedule view keyframes with lightweight interaction motion

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`

**Changes:**

1. Add `.scheduleViewStage` with layout-only rules required to preserve the current page geometry:
   - `min-width: 0`;
   - `width: 100%`;
   - no new fixed height or overflow clipping.
2. Add a neutral wrapper class for animated feedback/error blocks if required by Step 3; it must not alter ErrorBanner sizing or semantics.
3. Remove the obsolete `.scheduleViewEnter` animation rule/keyframes because Matrix/Daily entrance is now owned by `useEmployeeMotion`.
4. Remove the obsolete `.scheduleViewTogglePulse` rule/keyframes.
5. Keep the existing blue/white Employee visual language; do not redesign the page.
6. Refine existing interactive controls using short transitions in the approximate `140–220ms` range:
   - `.scheduleSelectTrigger`;
   - `.openSchedulePdfButton` / correction action;
   - `.scheduleViewToggle`;
   - `.dailyScheduleDay`.
7. Transitions may use subtle translate (`1–2px`), color, border-color, background, and small shadow changes. Do not use large bounce/spring movement.
8. Use `@media (hover: hover)` for hover-only movement so touch devices do not retain hover transforms.
9. Add an open-state class for `.scheduleSelectChevron` with a small rotation/translation transition. The state must derive from `isScheduleSelectOpen`; no independent animation state.
10. Improve selected Daily-day feedback with a restrained transform/outline/shadow while keeping existing work-day/off-day color semantics and `aria-selected` behavior.
11. Keep non-interactive worker rows and Summary rows visually stable. Do not add hover movement that would imply clickability.
12. Under `@media (prefers-reduced-motion: reduce)`:
    - disable spatial transforms and transition motion for Schedule controls;
    - keep instant color/border/selected-state feedback;
    - disable any Schedule-local keyframe animation added in this file.

**Behavior after change:**

- Controls feel responsive and modern without competing with page-level GSAP motion.
- Duplicate Matrix/Daily animation logic is removed.
- Touch and reduced-motion users do not receive unnecessary transform effects.

**Dependencies:**
- Step 3 markup classes.

**Do not:**
- add animated gradients;
- animate large shadows continuously;
- change responsive layout breakpoints unless a motion rule requires only a selector-specific override.

---

### Step 5 — Stage the hero entrance without reanimating its live clock

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleHero.module.css`

**Changes:**

1. Keep `EmployeeScheduleHero.tsx` unchanged unless a CSS selector cannot target an existing structural element. Prefer CSS-only work here.
2. Keep `data-employee-motion="from-top"`; do not move the hero into the JS reveal pipeline.
3. Refine `.hero` entrance so it remains gradual but less mechanically large:
   - approximately `600–680ms`;
   - opacity fade plus translate from roughly `-24px` to `0`, rather than the current larger `-44px` travel;
   - use the existing Employee cubic-bezier style.
4. Add one-time nested entrance choreography using existing structural elements:
   - `.heading`: subtle opacity + `y` reveal after the hero begins;
   - `.shifts`: subtle opacity + `y` reveal after the heading;
   - the two `.day` columns: small stagger after `.shifts`.
5. Keep delays bounded; the complete hero should settle in well under one second.
6. Do not attach animation to `.clock`, `.countdown`, or text values individually. Their existing periodic re-render must not replay animation.
7. Add `@media (prefers-reduced-motion: reduce)` that removes hero/nested entrance animations and leaves the final layout visible immediately.

**Behavior after change:**

- Hero arrives with layered but restrained depth.
- Clock/countdown updates remain static and inexpensive after initial render.

**Do not:**
- add parallax;
- add infinite shimmer/pulse/background animation;
- change the existing timer interval or shift logic.

---

### Step 6 — Animate the Schedule selection dialog and its list with bounded CSS choreography

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.module.css`

**Changes:**

1. Preserve the existing portal, dialog semantics, focus handling, close behavior, and markup.
2. Add a short overlay opacity entrance; do not animate `backdrop-filter` itself.
3. Override/augment the sheet entrance with a Schedule-specific `opacity + translateY + slight scale` animation around `280–340ms`.
4. On mobile bottom-sheet layout, use a slightly larger vertical start offset and no excessive scale.
5. Add row entrance choreography:
   - `.row` uses a short opacity/translate entrance;
   - stagger only the first approximately 6 rows using `nth-child` delays of roughly `20–30ms`;
   - all later rows share the final capped delay rather than extending the animation timeline indefinitely.
6. Add short hover/focus/selected transitions for `.row`, `.close`, and `.indicator` using the existing blue palette.
7. Keep selected-state contrast and focus-visible outline intact.
8. Add reduced-motion rules that disable overlay/sheet/row spatial entrance and transform transitions while preserving immediate state styling.

**Behavior after change:**

- Opening the Schedule picker feels like a deliberate sheet/list reveal.
- Large Schedule lists do not produce an unbounded stagger or long animation chain.

**Do not:**
- add JS timers or presence state for exit animation;
- modify `EmployeeScheduleSelectDialog.tsx` unless required only to expose an already existing state class; CSS-only implementation is preferred.

---

### Step 7 — Add restrained dialog/list micro-motion to column ordering

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleColumnOrderDialog.module.css`

**Changes:**

1. Preserve existing dialog markup, working-order state, focus behavior, save/reset behavior, and column-order persistence.
2. Add a short overlay/surface entrance consistent with Step 6; do not animate blur.
3. Add a bounded initial reveal for `.row` items, capped after the first approximately 6 rows.
4. Add transitions for:
   - `.row` / `.activeRow` border/background state;
   - `.position` color/background state;
   - `.controls button`, `.resetButton`, and `.closeButton` hover/press feedback.
5. Keep actual row reorder layout updates immediate in this task. Do not add GSAP Flip or force React remounts solely to animate list reordering; preserving focus/stability and bundle/runtime simplicity has priority.
6. Reduced-motion rules disable spatial entrance/press transitions and preserve immediate selected/disabled/focus states.

**Behavior after change:**

- The dialog and list appear smoothly, and every interactive control has immediate feedback.
- Reordering remains reliable and focus-safe with no layout-animation measurement overhead.

**Do not:**
- change the column order algorithm;
- change stable React keys;
- introduce FLIP/layout animation in this task.

---

### Step 8 — Add state motion to the Shift Correction dialog without changing its workflow

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.module.css`

**Changes:**

1. Preserve all existing Shift Correction component logic, validation, mutation inputs, pending states, and dialog semantics.
2. Add a short overlay/dialog entrance consistent with the other Schedule dialogs.
3. Add subtle staged entrance for the existing `.stepSection` blocks; keep the total delay bounded.
4. Add short interaction transitions to:
   - `.dayList button` / `.dayActive`;
   - `.shiftCard` / pending state;
   - `.boundaryChoices button` / `.boundarySelected`;
   - `.timeEditor > button`;
   - `.closeButton`.
5. Give conditionally rendered `.adjustmentPanel`, `.validation`, and similar feedback blocks a short one-time opacity/translate reveal when they mount.
6. Do not animate text input values or cause any movement on every keystroke.
7. Do not automatically scroll the horizontal day list unless existing behavior already requires it; no new scroll-management logic is needed for this dialog.
8. Add reduced-motion rules disabling spatial/keyframe motion while preserving state-color changes and focus styles.

**Behavior after change:**

- The multi-step correction workflow communicates progression and selected states clearly without changing its logic.
- No animation runs continuously while the user edits times.

**Do not:**
- change mutation payloads or validation;
- add new React state only for decorative animation;
- add timers for exit transitions.

---

### Step 9 — Update Schedule tests for motion-aware scrolling and preserve current behavior

**Action:** MODIFY

**File:**
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`

**Changes:**

1. Add a deterministic `window.matchMedia` mock/helper used by this test file.
2. Default the helper to `prefers-reduced-motion: no-preference` in `beforeEach` and reset it between tests.
3. Update the existing Daily-view scroll expectation:
   - normal mode must expect `behavior: "smooth"`;
   - keep `block: "nearest"` and `inline: "center"`.
4. Add a focused test with reduced motion enabled and verify Daily-day centering uses `behavior: "auto"`.
5. Keep the existing assertion that the Matrix/Daily toggle returns focus to the toggle button.
6. Add a small DOM-wiring assertion for the state animation boundary:
   - the rendered Matrix/Daily stage has `data-employee-motion="schedule-content"`;
   - its `data-motion-key` changes when switching view or selecting another schedule.
7. Add/extend a Summary test so that changing month/year changes the Summary content/total motion key without changing calculated values.
8. Preserve all current Schedule behavior tests, including:
   - matrix data;
   - Daily worker ordering;
   - empty Daily state;
   - column ordering;
   - PDF behavior;
   - summary calculations;
   - loading/empty/error states;
   - explicit absence of the Salary calculator.
9. Do not attempt to assert frame timing or CSS keyframe duration in Vitest/JSDOM.

**Behavior after change:**

- Tests protect the new JS behavior and ensure animation wiring does not alter functional behavior.

**Do not:**
- add snapshot tests for generated CSS class names;
- make tests depend on real animation timing.

## 5. Data / API / Persistence Changes

### Data model

None.

### API

None.

### Persistence

None.

The following existing flows must remain exactly as they are:

- published Schedule query;
- Employee UI-state query/save;
- local column-order storage;
- shift-correction query/mutation;
- PDF export generation.

### Dependency injection

None.

### Configuration

None.

### npm dependencies

None. `package.json` and lockfiles must not change for this task.

## 6. Error and Edge Case Requirements

1. **No schedules / loading:** existing panels remain usable and receive only their existing `schedule-panel` entrance.
2. **Query/PDF/preference errors:** the message itself must remain readable immediately; animation must never delay or hide it after settling.
3. **Shift-correction success:** status remains `role="status"`; animation is presentation-only.
4. **Rapid Matrix/Daily toggles:** previous tween is killed and the newest state wins; no stacked GSAP timelines.
5. **Rapid Daily-day changes:** only the current tabpanel finishes visible; no stale opacity/transform inline styles after GSAP cleanup.
6. **Rapid Summary period changes:** only the current total/grid key is animated; no per-row tween accumulation.
7. **Same state selected twice:** unchanged `data-motion-key` must not deliberately replay animation.
8. **`prefers-reduced-motion`:** all functionality and focus changes work with no spatial entrance animation and with non-smooth day centering.
9. **`matchMedia` unavailable in the test/runtime environment:** the local scroll helper must fail safely and treat the environment as normal/no-preference rather than throwing.
10. **Large lists:** list stagger remains capped; do not make duration proportional to list length.
11. **Mobile:** no entrance transform may cause horizontal page overflow; dialog/sheet animations must keep current safe-area and viewport sizing.
12. **Keyboard:** animation must not move focus, trap focus differently, or require pointer input.
13. **Live hero clock:** every-second updates must not recreate/restart hero animation.

## 7. Tests

### Automated tests

Run from `FrontEnd`:

```bash
npm test -- EmployeeSchedulePage.test.tsx
npm run lint
npm run build
npm test
```

Required automated coverage:

- normal Daily switch uses smooth selected-day centering;
- reduced-motion Daily switch uses automatic centering;
- Matrix/Daily focus restoration remains intact;
- motion key changes for view/schedule state;
- Summary state key changes while summary values remain correct;
- all existing Schedule behavior tests still pass.

### Manual interaction verification

With `npm run dev`, verify `/schedule` on desktop and a narrow mobile viewport:

1. First load:
   - hero settles first with layered internal reveal;
   - Schedule content and Summary follow without a long blocking sequence.
2. Matrix ↔ Daily:
   - button press feedback is immediate;
   - content transition is smooth and does not flash/reflow;
   - toggle retains focus.
3. Daily day strip:
   - selected date centers smoothly in normal mode;
   - only Daily body content transitions;
   - 28–31 tabs do not independently cascade into view.
4. Schedule picker:
   - overlay/sheet/list entrance is clean;
   - selected/focus/hover states remain clear;
   - long list entrance remains bounded.
5. Column-order dialog:
   - open/list/control feedback is animated;
   - moving rows still works and does not lose focus because no layout FLIP is added.
6. Shift Correction:
   - sections and conditional panels appear smoothly;
   - selecting a day/boundary and editing time remains immediate.
7. Summary:
   - month/year dropdowns retain existing SearchableSelect behavior;
   - total and grid transition on period change as a whole;
   - rows are not individually animated.
8. Error/success feedback:
   - messages enter once and remain static/readable.
9. Reduced motion:
   - emulate `prefers-reduced-motion: reduce`;
   - no hero/section/dialog/list spatial animation;
   - no button press scaling;
   - selected-day scroll is not smooth;
   - all state and focus feedback remains understandable.

### Performance verification

Use browser Performance/Rendering tools during the manual checks:

- page must become idle after entrance animations settle;
- no newly introduced recurring timers, RAF loops, scroll listeners, or animation observers should produce work while idle;
- structural animation should show transform/opacity compositing rather than repeated layout of the matrix/table;
- rapidly switch views/days/summary periods and verify old tweens are cancelled rather than accumulating;
- verify no animation is applied per matrix cell or per Summary row.

## 8. Verification

### Build

- `npm run build` completes successfully.

### Lint

- `npm run lint` completes successfully.

### Tests

- targeted Schedule test file passes;
- full `npm test` passes.

### Integration

- `EmployeeWorkspaceLayout` still owns the Employee GSAP scope;
- `/schedule` uses `data-employee-motion` + `data-motion-key` for dynamic block transitions;
- no second motion system/provider/observer exists.

### Runtime

- no animation leaves an element stuck transparent or transformed;
- dialogs, Schedule selection, Matrix/Daily, Daily-day switching, Summary selection, PDF export, column order, and shift correction all remain functional;
- desktop and mobile layouts remain unchanged except for motion/micro-interaction styling.

### Regression

- other Employee routes keep their normal existing animation when reduced motion is not requested;
- reduced-motion users get less movement throughout the shared Employee/shared-control primitives touched by this task;
- SearchableSelect functionality and ViewportOverlay layout remain unchanged;
- Salary calculator remains absent from `/schedule`.

### Scope

- no backend files changed;
- no package/lock files changed;
- no unrelated refactor.

## 9. Acceptance Checklist

- [ ] `/schedule` has one coherent motion language instead of independent competing Matrix/Daily keyframes.
- [ ] Existing `useEmployeeMotion` remains the only page-level JS motion orchestrator.
- [ ] No new animation dependency was added.
- [ ] Hero entrance is layered, restrained, and does not replay on clock updates.
- [ ] Loading/empty panels reveal smoothly.
- [ ] Error and success feedback enters once and remains accessible.
- [ ] Selecting another schedule replays only the Schedule content boundary.
- [ ] Matrix ↔ Daily content transition is smooth and focus remains on the toggle.
- [ ] Daily selected day centers with smooth scrolling in normal mode.
- [ ] Daily selected day centers instantly under reduced motion.
- [ ] Daily tabpanel content reanimates on day change through `data-motion-key`.
- [ ] Schedule picker sheet and a bounded subset of list rows reveal progressively.
- [ ] Column-order and Shift Correction dialogs have responsive control/state motion without changing logic.
- [ ] Summary total and grid animate on period changes as containers, not as dozens of row animations.
- [ ] Buttons/tabs/selectors provide short press/hover/selected feedback without bounce-heavy motion.
- [ ] `prefers-reduced-motion: reduce` suppresses JS GSAP spatial reveal, global press animation, overlay entrance, SearchableSelect dropdown entrance, Schedule hero/dialog/list motion, and smooth scrolling.
- [ ] No matrix-cell animation was added.
- [ ] No Summary-row animation was added.
- [ ] No scroll listener, RAF loop, ScrollTrigger, or continuous decorative animation was added.
- [ ] No persistent `will-change` was added to large element groups.
- [ ] `npm run lint` passes.
- [ ] `npm run build` passes.
- [ ] targeted Schedule tests pass.
- [ ] full Vitest suite passes.
- [ ] existing functional Schedule behavior is unchanged.
- [ ] Salary calculator remains absent.
- [ ] No unrelated files were modified.

## 10. Do Not Change

Do not modify as part of this task:

- backend projects, controllers, services, DTOs, persistence, migrations, or API contracts;
- Employee Schedule domain/calculation helpers except where a test import is mechanically required;
- `ContainerGraphMatrix` internals or its public API;
- `CardSection` internals;
- `SearchableSelect.tsx` public API/behavior;
- Employee Schedule PDF-generation behavior;
- schedule/shift-correction API calls;
- column-order persistence format or storage keys;
- authentication/session logic;
- route definitions;
- the hero timer/update interval;
- dependencies in `package.json` or lockfiles;
- old/removed Salary calculator functionality;
- unrelated Employee pages or Manager workflows;
- repository-level `Plan.md` or unrelated documentation during implementation unless the user separately requests documentation updates.
