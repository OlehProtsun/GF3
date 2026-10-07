# Plan

## 1. Objective

Create exactly one new design-reference artifact:

- `docs/design-system.json`

The file must describe the **current UI design of GF3 at branch `DEV2`, commit `b4a08ad`** in a machine-readable form so that a developer or coding agent can later change existing UI or add new UI by referencing a single design inventory instead of rediscovering the frontend structure and styling.

This task is documentation/inventory work only. The JSON must describe the current implementation; it must **not** become a runtime dependency and must **not** redesign, normalize, or refactor the application.

For this plan, “complete design description” means:

- global visual foundations that are actually present in the code;
- manager, employee, and public/auth layout models;
- reusable UI primitives and their contracts;
- reusable record/form/profile patterns;
- the exported icon catalog;
- every routed page and its role-aware route binding;
- important page sections and user-visible/interactive controls;
- component variants;
- important visual/interaction states;
- responsive behavior;
- navigation and dialog behavior relevant to UI composition;
- accessibility semantics explicitly implemented in the source;
- localization usage relevant to labels/content;
- source-file provenance for every design entry;
- known implementation/design divergences that must not be silently “corrected” in the snapshot;
- maintenance rules for keeping the JSON aligned with future UI changes.

The JSON is a **descriptive snapshot**, not the runtime source of truth.

---

## 2. Existing Components and Sources to Reuse

### 2.1 Core frontend sources

**REUSE AS REFERENCE — do not modify**

- `FrontEnd/src/app/router/AppRouter.tsx`
  - authoritative source for current page/route/role bindings.
- `FrontEnd/src/index.css`
  - global font, page/background, base controls, scrolling, and common global styling.
- `FrontEnd/src/shared/ui/motion.css`
  - shared motion durations, easing, reveal animation, press animation, and motion-list behavior.
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx`
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.module.css`
  - manager shell, sidebar, navigation, collapse/open behavior, responsive layout.
- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`
- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`
  - employee shell, desktop/mobile navigation, unread indicators, responsive behavior.
- `FrontEnd/src/pages/shared/EmployeeWorkspacePage.module.css`
  - shared employee-page visual vocabulary.

### 2.2 Shared UI primitives

**REUSE AS REFERENCE — define each once in `components`**

At minimum catalog these existing components and their matching CSS modules when present:

- `FrontEnd/src/shared/ui/components/IosButton/IosButton.tsx`
- `FrontEnd/src/shared/ui/components/IosButton/IosButton.module.css`
- `FrontEnd/src/shared/ui/PageHeader/PageHeader.tsx`
- `FrontEnd/src/shared/ui/PageHeader/PageHeader.module.css`
- `FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx`
- `FrontEnd/src/shared/ui/sections/CardSection/CardSection.module.css`
- `FrontEnd/src/shared/ui/components/ErrorBanner/ErrorBanner.tsx`
- `FrontEnd/src/shared/ui/components/ErrorBanner/ErrorBanner.module.css`
- `FrontEnd/src/shared/ui/components/ListCardSection/ListCardSection.tsx`
- `FrontEnd/src/shared/ui/components/ListCardSection/ListCardSection.module.css`
- `FrontEnd/src/shared/ui/components/RecordGrid/RecordGrid.tsx`
- `FrontEnd/src/shared/ui/components/RecordGrid/RecordGrid.module.css`
- `FrontEnd/src/shared/ui/components/RecordTile/RecordTile.tsx`
- `FrontEnd/src/shared/ui/components/RecordTile/RecordTile.module.css`
- `FrontEnd/src/shared/ui/components/ProfileSummaryCard/ProfileSummaryCard.tsx`
- `FrontEnd/src/shared/ui/components/ProfileSummaryCard/ProfileSummaryCard.module.css`
- `FrontEnd/src/shared/ui/components/RecordProfileCard/RecordProfileCard.tsx`
- `FrontEnd/src/shared/ui/components/RecordProfileCard/RecordProfileCard.module.css`
- `FrontEnd/src/shared/ui/components/RecordDetailsFormCard/RecordDetailsFormCard.tsx`
- `FrontEnd/src/shared/ui/components/RecordDetailsFormCard/RecordDetailsFormCard.module.css`
- `FrontEnd/src/shared/ui/components/PresenceBadge/PresenceBadge.tsx`
- `FrontEnd/src/shared/ui/components/PresenceBadge/PresenceBadge.module.css`
- `FrontEnd/src/shared/ui/forms/Field/Field.tsx`
- the matching `Field` CSS module used by that source
- `FrontEnd/src/shared/ui/components/SearchableSelect/SearchableSelect.tsx`
- its matching CSS module
- `FrontEnd/src/shared/ui/components/EmployeeTargetCombobox/EmployeeTargetCombobox.tsx`
- its matching CSS module
- existing `ConfirmDialog`
- existing `SavingOverlay`
- existing `ManagerEditLockDialog`
- existing `ErrorAlertsViewport`
- existing `DetailList` / `DetailItem`
- existing `FormLayout` primitives used by entity forms.

Do not create replacement primitives and do not duplicate these definitions inside every page entry.

### 2.3 Icon source

**REUSE AS REFERENCE**

- `FrontEnd/src/shared/ui/icons/index.ts`

The JSON icon catalog must include every icon exported by this file at `b4a08ad`, including:

`InfoIcon`, `WarnIcon`, `ErrorIcon`, `BackIcon`, `InboxIcon`, `BindIcon`, `StatisticsIcon`, `ShiftGiveAwayIcon`, `SwapOffersIcon`, `SwapHistoryIcon`, `ScheduleDetailsIcon`, `InformationIcon`, `SaveIcon`, `CodeIcon`, `AvailabilityIcon`, `ExcelIcon`, `ClearFormatAllIcon`, `ImportIcon`, `ClearFormatIcon`, `EmployeeIcon`, `ShopIcon`, `SearchIcon`, `NoteIcon`, `ScheduleIcon`, `ContainerInfoIcon`, `ContainerIcon`, `EyeIcon`, `EyeOffIcon`, `HomeIcon`, `DatabaseIcon`, `ArrowIcon`, `LogoutIcon`, `PlusIcon`, `CheckIcon`, `CloseIcon`, `ChevronLeftIcon`, `ChevronRightIcon`, `PinIcon`, `NewsIcon`, and `SettingsIcon`.

### 2.4 Reusable entity UI

**REUSE AS REFERENCE**

Shops:

- `FrontEnd/src/entities/shops/ui/ShopListCard.tsx`
- `FrontEnd/src/entities/shops/ui/ShopProfileCard.tsx`
- `FrontEnd/src/entities/shops/ui/ShopDetailsForm.tsx`

Employees:

- `FrontEnd/src/entities/employees/ui/EmployeeListCard.tsx`
- `FrontEnd/src/entities/employees/ui/EmployeeProfileCard.tsx`
- `FrontEnd/src/entities/employees/ui/EmployeeDetailsForm.tsx`

These entries must reference the shared primitives they compose rather than restating those primitive definitions.

---

## 3. Constraints

1. **CREATE only `docs/design-system.json`.**
2. Do not modify React, TypeScript, CSS, backend, test, configuration, or documentation files as part of this task.
3. Do not make the application import, parse, or consume `docs/design-system.json`.
4. Do not create a runtime theme engine or design-token provider.
5. Do not refactor CSS literals into CSS custom properties.
6. Do not add npm/NuGet dependencies.
7. Do not create a CSS/AST parser or generator.
8. Do not change routes, component APIs, page behavior, translations, icons, accessibility, or responsive behavior.
9. Do not “clean up” or normalize current styling while documenting it.
10. Do not infer design values that are not supported by the current source.
11. Do not treat a component-local CSS variable fallback as a global token unless the code actually establishes it globally.
12. Preserve role-dependent route meaning. A route path alone is not a unique page identity.
13. Source references in JSON must use repository-relative file paths and stable symbol/selector names; do not use fragile GitHub line numbers.
14. The JSON must contain no comments or trailing commas.
15. Use stable `kebab-case` IDs for catalog entries.
16. Text rendered through `t(...)` must be marked as localized. Do not claim English literals are the only supported copy.
17. When a shared component already represents an element, pages should reference that component by ID rather than duplicate its full style contract.
18. Page-specific controls that are not represented by a shared component must still be described on the page or page-specific component that owns them.
19. Keep business/domain data out of the JSON except where its presentation state changes UI design, for example presence tone, unread indicator, loading state, selected state, pinned state, or error state.
20. The snapshot metadata must explicitly identify commit `b4a08ad` so future readers can detect drift.

---

## 4. Required JSON Contract

### Step 1 — Create the design manifest shell

**Action:** CREATE

**File:**

- `docs/design-system.json`

Create these top-level keys in this exact order:

1. `schemaVersion`
2. `project`
3. `foundations`
4. `assets`
5. `layouts`
6. `components`
7. `pages`
8. `routeBindings`
9. `patterns`
10. `knownImplementationDivergences`
11. `maintenance`

Required metadata:

- `schemaVersion`: `"1.0.0"`
- `project.name`: `"GF3"`
- `project.repository`: `"https://github.com/OlehProtsun/GF3"`
- `project.frontendRoot`: `"FrontEnd"`
- `project.branch`: `"DEV2"`
- `project.commit`: `"b4a08ad"`
- `project.artifactPurpose`: concise statement that this is a design-reference snapshot.
- `project.runtimeSourceOfTruth`: `false`
- `project.sourceOfTruth`: state that React/TypeScript/CSS at the recorded commit remains authoritative.
- `project.scope`: state that the manifest covers user-visible frontend design and UI composition, not backend/domain architecture.

For every source-backed object, use this source-reference shape:

- `path`: repository-relative path.
- `symbolOrSelector`: exported symbol, component name, CSS selector, or CSS custom property.
- `notes`: optional, only when necessary to explain provenance.

Do not store line numbers.

**Behavior after change:**

A reader can immediately identify what project/snapshot the JSON describes and whether it is meant to control runtime styling.

---

### Step 2 — Populate `foundations`

**Action:** CREATE CONTENT IN THE NEW FILE

**Primary source files:**

- `FrontEnd/src/index.css`
- `FrontEnd/src/shared/ui/motion.css`
- shared primitive CSS modules;
- the two layout CSS modules;
- `FrontEnd/src/pages/shared/EmployeeWorkspacePage.module.css`.

Create these foundation groups:

- `typography`
- `colors`
- `surfaces`
- `spacing`
- `radii`
- `borders`
- `shadows`
- `motion`
- `responsive`

#### 2.1 Typography

Capture:

- global font-family stack;
- global/default text color;
- font weights and sizes only when they represent a reusable/currently repeated visual role;
- component-specific typography must remain under the component when it is not a global/repeated role.

#### 2.2 Colors and surfaces

At minimum capture supported semantic roles discovered in the current source, such as:

- application/page background;
- primary text;
- secondary/muted text;
- primary blue/accent;
- danger/red;
- neutral/secondary button;
- glass/surface background;
- border/focus colors where reusable;
- employee-shell blue/slate surface roles where those are shared by employee pages.

Each token entry must include:

- `value`;
- `semanticRole`;
- `sources`.

If the same semantic role has different values in manager vs employee UI, do not collapse them into one token. Use separate contextual entries.

#### 2.3 Spacing, radii, borders, shadows

Only promote values that are meaningfully reusable in the current implementation.

Examples that must be represented from the existing code when applicable:

- pill/fully-rounded button radius;
- card radius;
- sidebar/nav-item radius;
- PageHeader/card surface radius;
- common card/glass shadows;
- border styles and opacity;
- manager layout inset/sidebar width if they are layout constants.

Do not invent a synthetic spacing scale such as `4/8/12/16` unless the source actually defines one.

#### 2.4 Motion

Capture the actual shared values from `motion.css`:

- shared easing;
- fast duration;
- enter duration;
- reveal animation;
- press animation;
- stagger/list behavior.

Also record employee-layout motion overrides separately if they differ from the shared values.

#### 2.5 Responsive

Record breakpoints and their owner/source.

Do not merge all breakpoints into a fictional global breakpoint system. For each breakpoint record:

- value;
- owner/context;
- effect;
- source.

Include manager-layout, employee-layout, PageHeader/CardSection, and employee shared-page breakpoints that materially change composition.

---

### Step 3 — Populate `assets.icons`

**Action:** CREATE CONTENT IN THE NEW FILE

**Source:**

- `FrontEnd/src/shared/ui/icons/index.ts`

Create one icon entry per exported icon.

Each icon entry must contain:

- stable ID;
- exported component name;
- category/purpose when obvious from current usage/name;
- source reference;
- known default/used sizing only if supported by source or repeated usage;
- pages/components that use it when this is important for navigation or core actions.

Do not copy SVG path data into the JSON. The design manifest should point to the existing icon implementation.

---

### Step 4 — Populate reusable `components`

**Action:** CREATE CONTENT IN THE NEW FILE

For each component, use this common entry contract:

- `id`
- `name`
- `category`
- `responsibility`
- `source`
- `styleSources`
- `composition`
- `publicContract`
- `variants`
- `elements`
- `states`
- `visual`
- `responsive`
- `interactions`
- `accessibility`
- `localization`
- `usedBy`

`publicContract` must describe only props that affect appearance, visible content, interaction, or composition. Do not dump irrelevant implementation-only props.

Each `elements` item must describe a user-visible or interaction-relevant element and contain as applicable:

- `id`
- `type`
- `componentRef`
- `content`
- `purpose`
- `action`
- `visualRef`
- `states`
- `accessibility`

#### 4.1 `ios-button`

Capture the existing contract, including:

- label;
- optional icon;
- `primary` / `secondary`;
- `default` / `compact`;
- disabled;
- custom color;
- custom border color;
- button type;
- click action;
- hover;
- active/press;
- focus-visible;
- disabled visual state.

Record the current CSS-backed dimensions, radius, colors, shadow, typography, and compact sizing.

Do not silently treat the inline padding anomaly as canonical intended spacing; record it later in `knownImplementationDivergences`.

#### 4.2 `page-header`

Capture:

- eyebrow;
- title;
- subtitle;
- back navigation/action;
- back label;
- right action slot;
- search metadata;
- search control;
- `card` / `plain` variant;
- full-bleed/gutter/max-width behavior;
- collapse/expand state;
- fixed/glass surface behavior;
- responsive changes.

#### 4.3 `card-section`

Capture:

- optional icon;
- title;
- centered/right header slots;
- children/content;
- glass surface;
- width constraint;
- radius;
- padding;
- border/shadow/blur;
- responsive header composition.

#### 4.4 `error-banner`

Capture:

- alert purpose;
- dismissible behavior;
- danger surface/text/border treatment;
- dismiss button;
- accessibility role/semantics that are explicitly present.

#### 4.5 Record/list/profile/form primitives

Create separate entries for:

- `list-card-section`
- `record-grid`
- `record-tile`
- `profile-summary-card`
- `record-profile-card`
- `record-details-form-card`
- `presence-badge`
- `text-input`
- `text-area`
- `labeled-field`
- `error-pill`
- `searchable-select`
- `employee-target-combobox`
- `detail-list`
- `detail-item`
- form-row/layout primitives used by shop/employee forms.

Required state coverage includes where applicable:

- loading;
- load error;
- empty;
- search empty;
- interactive;
- selected;
- pinned;
- default/compact density;
- wrap/stacked metadata;
- online/offline/inactive;
- invalid field;
- disabled;
- saving.

For `RecordTile`, explicitly record keyboard activation for Enter/Space, pin button semantics, `aria-pressed`, and the selected/pinned states.

#### 4.6 Dialogs and overlays

Create component entries for existing:

- `confirm-dialog`
- `saving-overlay`
- `manager-edit-lock-dialog`
- `error-alerts-viewport`

Capture:

- open/closed state;
- blocking vs non-blocking behavior;
- visible actions;
- destructive confirmation styling when present;
- loading/saving state;
- edit-lock state;
- dismiss behavior;
- relevant accessibility semantics from source.

---

### Step 5 — Populate `layouts`

**Action:** CREATE CONTENT IN THE NEW FILE

Create exactly these layout identities:

- `manager-overlay-sidebar`
- `employee-workspace`
- `public-auth`

#### 5.1 Manager layout

Source:

- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx`
- matching CSS module.

Describe:

- fixed sidebar;
- collapsed/expanded/open-tab behavior;
- backdrop behavior;
- main content relationship to sidebar;
- manager navigation items and icon references;
- settings/database destination;
- profile/account footer;
- logout action;
- auxiliary `ManagerSystemNews`;
- auxiliary `ManagerNotepad`;
- wide-content route behavior;
- responsive behavior and breakpoints.

Manager navigation destinations must include:

- Home
- Employee
- Shop
- Availability
- Container
- Information
- Message/Communications
- DataBase/Settings
- manager profile/account.

#### 5.2 Employee layout

Source:

- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`
- matching CSS module;
- `FrontEnd/src/pages/shared/EmployeeWorkspacePage.module.css`.

Describe:

- desktop navigation;
- mobile/bottom-tab navigation;
- collapsed/open state;
- unread indicators;
- employee communication dialog;
- shared employee-page hero/card/panel vocabulary;
- employee blue/slate surface treatment;
- responsive changes.

Employee navigation destinations:

- Notifications
- Availability
- Schedule
- Swap
- Profile.

#### 5.3 Public/auth layout

Describe the public unauthenticated presentation used by Login and Password Recovery. This may be represented as a lightweight layout identity even if there is no dedicated shared React layout component.

Do not invent a shared runtime component.

---

### Step 6 — Populate reusable entity/pattern entries

**Action:** CREATE CONTENT IN THE NEW FILE

Create component/pattern entries for the existing reusable shop and employee presentation components.

#### Shops

`shop-list-card`

- composes `list-card-section`, `record-grid`, `record-tile`, `ios-button`;
- includes Add New and Clear Search actions;
- supports pinned records;
- displays address metadata and optional description;
- loading/error/empty/search-empty states.

`shop-profile-card`

- composes `record-profile-card`;
- includes shop identity/details;
- Edit Shop action;
- destructive Delete Shop action using the existing custom red button treatment.

`shop-details-form`

- composes `record-details-form-card`;
- Name;
- Address;
- Description;
- field error state;
- Cancel;
- Save;
- loading/load-error/saving states.

#### Employees

`employee-list-card`

- composes list/grid/tile primitives;
- compact/stacked record presentation;
- presence badge;
- pinned state;
- Add New and Clear Search;
- loading/error/empty/search-empty states.

`employee-profile-card`

- composes `record-profile-card`;
- identity/details/presence;
- Edit Employee;
- optional Kick Employee;
- destructive Delete Employee;
- pending/disabled states.

`employee-details-form`

Capture its actual existing fields and visible validation, including:

- first name;
- last name;
- email;
- phone;
- username;
- password/PIN field behavior as implemented;
- Cancel/Save;
- field-level errors;
- loading/error/saving states.

Do not move validation logic into the JSON. Record only the visual/interaction contract that the current form exposes.

---

### Step 7 — Populate `pages` and `routeBindings`

**Action:** CREATE CONTENT IN THE NEW FILE

Pages must be keyed by stable page IDs, not by route strings.

Each page entry must contain:

- `id`
- `name`
- `source`
- `styleSources`
- `layoutRef`
- `roles`
- `purpose`
- `composes`
- `sections`
- `controls`
- `dialogs`
- `states`
- `responsive`
- `navigation`
- `localization`
- `accessibility`

`routeBindings` must be a separate array with records containing:

- `role`: `public`, `manager`, or `employee`;
- `path`;
- `pageRef`;
- optional `notes`.

This separation is mandatory because `/` and `/availability` are role-dependent.

#### 7.1 Public route coverage

Create bindings for exactly:

| Role | Path | Page |
|---|---|---|
| public | `/login` | `login-page` |
| public | `/password-recovery` | `password-recovery-page` |

`login-page` must capture:

- auth card;
- username field;
- PC/Phone authentication mode switch;
- password field in PC mode;
- six-digit/PIN keypad presentation in Phone mode as implemented;
- PIN dots;
- delete/backspace action;
- forgot-password action;
- Sign In action;
- error/loading/disabled states present in source.

`password-recovery-page` must capture:

- username step;
- send-code action;
- code input;
- new-password input;
- change-password action;
- Back to sign in;
- step/error/loading states implemented in source.

#### 7.2 Manager route coverage

Create bindings for all of the following:

| Role | Path | Page |
|---|---|---|
| manager | `/` | `home-page` |
| manager | `/shop` | `shop-list-page` |
| manager | `/shop/new` | `shop-edit-page` |
| manager | `/shop/:shopId` | `shop-profile-page` |
| manager | `/shop/:shopId/edit` | `shop-edit-page` |
| manager | `/availability` | `availability-page` |
| manager | `/availability/new` | `availability-edit-page` |
| manager | `/availability/:availabilityId` | `availability-profile-page` |
| manager | `/availability/:availabilityId/edit` | `availability-edit-page` |
| manager | `/container` | `container-page` |
| manager | `/container/:containerId/graphs/new` | `container-graph-edit-page` |
| manager | `/container/:containerId/graphs/:graphId` | `container-graph-profile-page` |
| manager | `/container/:containerId/graphs/:graphId/edit` | `container-graph-edit-page` |
| manager | `/information` | `information-page` |
| manager | `/communications` | `communications-page` |
| manager | `/database` | `database-page` |
| manager | `/manager-profile` | `manager-account-page` |
| manager | `/employee` | `employee-list-page` |
| manager | `/employee/new` | `employee-edit-page` |
| manager | `/employee/:employeeId` | `employee-profile-page` |
| manager | `/employee/:employeeId/edit` | `employee-edit-page` |

Required manager page sources include:

- `FrontEnd/src/pages/home/ui/HomePage.tsx`
- `FrontEnd/src/pages/shop-list/ui/ShopListPage.tsx`
- `FrontEnd/src/pages/shop-profile/ui/ShopProfilePage.tsx`
- `FrontEnd/src/pages/shop-edit/ui/ShopEditPage.tsx`
- `FrontEnd/src/pages/availability-edit/ui/AvailabilityEditPage.tsx`
- `FrontEnd/src/pages/availability-profile/ui/AvailabilityProfilePage.tsx`
- `FrontEnd/src/pages/container/ui/ContainerPage.tsx`
- the existing Availability list page loaded by `AppRouter`;
- the existing Container Graph Profile page loaded by `AppRouter`;
- the existing Container Graph Edit page loaded by `AppRouter`;
- the existing Information page loaded by `AppRouter`;
- the existing Communications page loaded by `AppRouter`;
- the existing DataBase page loaded by `AppRouter`;
- the existing Manager Account page loaded by `AppRouter`;
- `FrontEnd/src/pages/employee-list/ui/EmployeeListPage.tsx`
- `FrontEnd/src/pages/employee-profile/ui/EmployeeProfilePage.tsx`
- `FrontEnd/src/pages/employee-edit/ui/EmployeeEditPage.tsx`.

For source files whose exact directory name is already resolved by `AppRouter` imports during execution, write that exact repository-relative path into the JSON. Do not shorten it to a display name.

#### 7.3 Employee route coverage

Create bindings for:

| Role | Path | Page |
|---|---|---|
| employee | `/` | `employee-notifications-page` |
| employee | `/notifications` | `employee-notifications-page` |
| employee | `/availability` | `employee-availability-page` |
| employee | `/schedule` | `employee-schedule-page` |
| employee | `/swap` | `employee-swap-page` |
| employee | `/profile` | `employee-account-page` |

Required employee pages:

- Employee Notifications
- Employee Availability
- Employee Schedule
- Employee Swap
- Employee Account/Profile.

Each must reference `employee-workspace` and the shared employee workspace visual pattern.

---

### Step 8 — Capture complex page design without duplicating internals

**Action:** CREATE CONTENT IN THE NEW FILE

The following pages are too complex to represent as a flat list of CSS declarations. Describe them compositionally.

#### `home-page`

Record:

- `PageHeader` with title/current-time context;
- “Who Works Today?” section;
- employee table/list presentation;
- employee-focus action;
- “This Month” section;
- collapse/expand state;
- summary cards/pill lists;
- “Active Schedules This Month” section;
- expand/collapse-all controls;
- schedule cards;
- `ContainerGraphMatrix` usage and related dialog where present.

#### `employee-notifications-page`

Record:

- Notifications/News tabs;
- unread/count indicators;
- Mark all read;
- empty states;
- notification list;
- per-notification Mark as read;
- navigation link/action;
- optional news image/video presentation.

#### `employee-availability-page`

Record the main page shell plus:

- calendar;
- day selection;
- day/detail dialog;
- preset editor;
- save/update actions;
- loading/error/empty/disabled states;
- responsive layout.

Do not copy scheduling business rules.

#### `employee-schedule-page`

Record:

- hero/summary;
- schedule matrix/container graph presentation;
- search/select controls;
- column order controls/dialog;
- shift correction action/dialog;
- loading/error/empty states;
- responsive changes.

#### `employee-swap-page`

Record:

- major swap tabs/sections;
- employee-target combobox;
- search/filter controls;
- pinning;
- offer/request cards or lists;
- confirmation dialogs;
- create/accept/reject/cancel actions as present;
- empty/loading/error/pending states.

#### `employee-account-page`

Record:

- profile/account sections;
- language selector;
- password reset/change controls;
- regulations/history presentation;
- logout;
- error/loading/success states.

#### `availability-page`, `availability-profile-page`, `availability-edit-page`

Record the existing:

- list/profile/editor modes;
- headers;
- groups/cards;
- related-hint UI;
- transfer dialog;
- edit-lock dialog;
- confirm dialog;
- unsaved-change dialog;
- save/saving overlay;
- current editor controls and states.

Reference existing `AvailabilityGroup*` components by component identity instead of re-describing their internals on every page.

#### `container-page`

Record:

- current list/profile/edit mode structure;
- `ContainerListCard`;
- `ContainerProfileWorkspace`;
- `ContainerDetailsForm`;
- export actions;
- confirmation;
- edit lock;
- saving overlay;
- error banner;
- primary/secondary actions.

#### `container-graph-profile-page`

Record:

- session tabs;
- profile workspace;
- export actions;
- compact-size toggle;
- confirmation;
- error state;
- header actions.

#### `container-graph-edit-page`

Record:

- session tabs;
- main graph editor;
- toolbar/control groups;
- selectable/editable graph/matrix surface;
- current editor modes;
- dialogs;
- destructive/confirm actions;
- save/saving/error/validation states;
- responsive/large-content behavior.

For the 2,000+ line editor, catalog subcomponents and visible control families; do not duplicate internal data algorithms.

#### `information-page`

Record:

- workflow/log list;
- date-time field/control;
- search/filter;
- delete/clear/settings actions;
- confirmation dialog;
- empty/loading/error states.

#### `communications-page`

Record:

- create/edit communication form;
- title;
- visible-from;
- visible-to;
- message;
- Cancel;
- Create/Save Changes;
- published message board;
- status/badges;
- edit/delete actions;
- confirmation dialog;
- empty/loading/error states.

#### `database-page`

Record:

- header;
- developer-access/password dialog;
- database selection;
- schema list/search;
- query executor;
- import/manual-copy controls;
- regulations admin panel;
- error banner;
- button/action hierarchy;
- protected/locked/unlocked states.

Do not document backend database schema as part of the design file.

#### `manager-account-page`

Record:

- manager profile;
- team/account details;
- language selector;
- regulation/history sections;
- relevant dialogs/actions;
- loading/error states.

---

### Step 9 — Populate `patterns`

**Action:** CREATE CONTENT IN THE NEW FILE

Create reusable UX/design patterns that are demonstrably present:

- `glass-surface`
- `manager-fixed-page-header`
- `employee-hero-card`
- `record-list`
- `record-profile`
- `record-edit-form`
- `search-with-result-meta`
- `pinned-record`
- `empty-state`
- `search-empty-state`
- `load-error-state`
- `saving-state`
- `destructive-confirmation`
- `manager-edit-lock`
- `unsaved-changes-guard`
- `responsive-navigation`
- `motion-list-reveal`

Each pattern must:

- reference existing component IDs/pages;
- describe when it is used;
- point to source files;
- avoid inventing new runtime abstractions.

---

### Step 10 — Populate `knownImplementationDivergences`

**Action:** CREATE CONTENT IN THE NEW FILE

This section exists to keep the design snapshot truthful.

At minimum include an entry for the current `IosButton` implementation:

- ID: `ios-button-inline-padding`
- source: `FrontEnd/src/shared/ui/components/IosButton/IosButton.tsx`
- observed current state:
  - inline vertical padding values conflict with the fixed-height CSS model;
  - inline horizontal padding uses negative CSS length strings, which are invalid for padding.
- treatment:
  - document this as an implementation divergence;
  - do not alter the component in this task;
  - do not promote these values to shared foundation tokens;
  - use the valid CSS module contract when describing the reusable visual component, while explicitly preserving the divergence record.

Also record, as a structural divergence rather than an error:

- styling/token values are distributed across global CSS, layout CSS modules, and component CSS modules;
- many CSS variables are component-local fallbacks and are not global design tokens.

Do not add speculative bugs or aesthetic opinions.

---

### Step 11 — Populate `maintenance`

**Action:** CREATE CONTENT IN THE NEW FILE

Add rules for future updates:

1. Any PR that changes a cataloged component’s visible contract must update its JSON entry.
2. Any added/removed route must update `pages` and `routeBindings`.
3. Any newly shared visual token must be added to `foundations` with source provenance.
4. A component-local override must stay component-local unless the implementation itself becomes shared.
5. New icons exported from `shared/ui/icons/index.ts` must be added to `assets.icons`.
6. New states/variants must be added to the owning component/page.
7. If an implementation divergence is fixed in production code, remove or update the matching divergence entry in the same change.
8. Update `project.branch`/`project.commit` when intentionally refreshing the full design snapshot.
9. Keep IDs stable across snapshots unless the underlying design entity is removed or renamed.
10. Do not make future runtime code depend on this file without a separate architectural decision.

---

## 5. Data, API, Persistence, DI, and Configuration Changes

### API

**NONE**

Do not add or modify HTTP endpoints, request DTOs, response DTOs, authorization, or SignalR behavior.

### Persistence

**NONE**

Do not change SQLite, EF Core entities, repositories, migrations, seed data, or persistence configuration.

### Dependency Injection

**NONE**

Do not register any service for this feature.

### Runtime configuration

**NONE**

Do not modify Vite configuration, environment variables, app settings, build configuration, or frontend providers.

### Dependencies

**NONE**

Do not add npm or NuGet packages.

---

## 6. Error and Edge-Case Requirements

The JSON itself has no runtime failure path, but its content must handle these documentation edge cases deterministically.

1. **Role-colliding routes**
   - Never use the route path as the page key.
   - Model manager and employee route bindings separately.

2. **One page bound to multiple routes**
   - Reuse one page entry and add multiple `routeBindings`.
   - Example: employee notifications for `/` and `/notifications`.

3. **One page component used for create/edit**
   - Reuse one page entry.
   - Describe create/edit visual state differences.
   - Add separate route bindings.

4. **Shared component + page-specific override**
   - Keep the base definition in `components`.
   - Keep the override in the owning page/entity entry.
   - Do not mutate the base component description to fit a single page.

5. **Localized labels**
   - Mark copy sourced through `t(...)` as localized.
   - Record the current semantic label/purpose, not an assumption that only the displayed English string exists.

6. **Dynamic/user data**
   - Describe format and presentation role, not snapshot values.
   - Example: document “employee presence badge” rather than hardcoding an employee’s current presence.

7. **Conditional UI**
   - Record the condition/state name and resulting visible element.
   - Examples: unread dot, edit lock, saving overlay, load error, empty state, collapsed sidebar, selected/pinned tile.

8. **Implementation anomalies**
   - Put confirmed anomalies in `knownImplementationDivergences`.
   - Do not silently normalize them and do not fix them.

9. **CSS values with uncertain semantic status**
   - Keep them under the component/layout where authored.
   - Do not promote to `foundations` without evidence of shared use or global definition.

10. **Very large editor components**
    - Describe visible surfaces and composed subcomponents.
    - Do not dump internal algorithms, DTOs, or every DOM wrapper.

---

## 7. Tests and Static Validation

Do not create a new automated test project or test file for this documentation-only change.

### 7.1 JSON syntax validation

From the repository root run:

```bash
node -e "const fs=require('fs'); JSON.parse(fs.readFileSync('docs/design-system.json','utf8')); console.log('design-system.json: valid JSON')"
```

Expected result:

- exit code `0`;
- output confirms valid JSON.

### 7.2 Required top-level sections

Run:

```bash
node -e "const fs=require('fs'); const d=JSON.parse(fs.readFileSync('docs/design-system.json','utf8')); const k=['schemaVersion','project','foundations','assets','layouts','components','pages','routeBindings','patterns','knownImplementationDivergences','maintenance']; const m=k.filter(x=>!(x in d)); if(m.length){console.error('Missing:',m);process.exit(1)} console.log('required sections: ok')"
```

### 7.3 Snapshot metadata

Verify:

- `project.branch === "DEV2"`
- `project.commit === "b4a08ad"`
- `project.runtimeSourceOfTruth === false`

### 7.4 Route coverage validation

The implementation agent must compare `routeBindings` against `FrontEnd/src/app/router/AppRouter.tsx`.

Required route-role pairs are exactly the 29 bindings listed in Step 7:

- 2 public;
- 21 manager;
- 6 employee.

Do not count unique path strings because role collisions are intentional.

The validation must fail the task if any `AppRouter` route at `b4a08ad` is absent from the JSON.

### 7.5 Shared primitive coverage

Verify that JSON contains component entries for at least:

- `ios-button`
- `page-header`
- `card-section`
- `error-banner`
- `list-card-section`
- `record-grid`
- `record-tile`
- `profile-summary-card`
- `record-profile-card`
- `record-details-form-card`
- `presence-badge`
- `text-input`
- `text-area`
- `labeled-field`
- `searchable-select`
- `employee-target-combobox`
- `confirm-dialog`
- `saving-overlay`
- `manager-edit-lock-dialog`.

### 7.6 Icon coverage

Compare `assets.icons` to exports in:

- `FrontEnd/src/shared/ui/icons/index.ts`

Every exported icon at `b4a08ad` must have exactly one catalog entry.

### 7.7 Source-reference validation

For every `source.path` / `styleSources[].path` in the JSON:

- the referenced file must exist at `b4a08ad`;
- paths must be repository-relative;
- no path may point to a file created only by the plan other than `docs/design-system.json`;
- no GitHub URL or line-number reference should be used instead of a repository-relative path.

### 7.8 Existing frontend verification

Because no production source should change, verify that the existing frontend remains healthy:

```bash
cd FrontEnd
npm run lint
npm run build
```

Do not fix unrelated pre-existing lint/build failures as part of this plan. If either command already fails on the untouched snapshot, report the pre-existing failure separately rather than expanding scope.

---

## 8. Manual Completeness Verification

Before considering the task complete, perform one focused pass using the manifest against the known UI source set.

### Foundations

- [ ] Global font/background/text behavior is represented.
- [ ] Shared motion values are represented.
- [ ] Manager-specific layout constants are represented.
- [ ] Employee-specific layout/shared-page values are represented.
- [ ] Component-local values have not been falsely promoted to global tokens.

### Layouts

- [ ] Manager sidebar/navigation/collapse behavior is represented.
- [ ] Employee desktop/mobile navigation is represented.
- [ ] Public/auth presentation is represented.
- [ ] Role-specific auxiliary UI is represented.

### Components

- [ ] Shared buttons, headers, cards, banners, record primitives, profile primitives, form primitives, selectors/comboboxes, dialogs, overlays, and badges are cataloged.
- [ ] Variants and important states are cataloged.
- [ ] Keyboard/ARIA behavior explicitly implemented in source is represented.
- [ ] Reusable components are not redundantly redefined in pages.

### Pages

- [ ] Every route from `AppRouter.tsx` maps to a page.
- [ ] Shop and employee list/profile/edit compositions are represented.
- [ ] Home major sections are represented.
- [ ] Employee Notifications major controls/states are represented.
- [ ] Employee Availability major controls/states are represented.
- [ ] Employee Schedule major controls/dialogs are represented.
- [ ] Employee Swap major controls/dialogs are represented.
- [ ] Employee Account major controls are represented.
- [ ] Availability list/profile/editor major controls/dialogs are represented.
- [ ] Container list/profile/edit surfaces are represented.
- [ ] Container Graph profile/editor surfaces are represented.
- [ ] Information controls are represented.
- [ ] Communications form/board/actions are represented.
- [ ] Database page protected/tooling surfaces are represented.
- [ ] Manager Account sections/actions are represented.
- [ ] Login and Password Recovery flows are represented.

### Provenance

- [ ] Every component/page/layout has at least one source reference.
- [ ] Style-owning entries have style source references.
- [ ] Known `IosButton` divergence is recorded.
- [ ] Snapshot commit and branch are present.

---

## 9. Acceptance Checklist

The task is complete only when all items below are true.

- [ ] `docs/design-system.json` exists.
- [ ] It is valid JSON with no comments or trailing commas.
- [ ] `schemaVersion` is `"1.0.0"`.
- [ ] The snapshot identifies repository GF3, branch `DEV2`, commit `b4a08ad`.
- [ ] It explicitly states that the file is descriptive and not the runtime source of truth.
- [ ] Foundations describe the actual current typography, colors/surfaces, relevant spacing/radii/borders/shadows, motion, and responsive rules with source provenance.
- [ ] Manager, employee, and public/auth layouts are represented.
- [ ] Shared UI primitives are represented once and reused by reference.
- [ ] The complete exported icon set is represented.
- [ ] Shop reusable list/profile/form design is represented.
- [ ] Employee reusable list/profile/form design is represented.
- [ ] All 29 role-route bindings from the analyzed `AppRouter` snapshot are represented.
- [ ] Role collisions for `/` and `/availability` are unambiguous.
- [ ] Public login and password-recovery design flows are represented.
- [ ] Complex manager and employee pages are documented compositionally without duplicating business logic.
- [ ] Loading, error, empty, search-empty, selected, pinned, collapsed/expanded, disabled, saving, destructive-confirmation, and edit-lock states are represented where applicable.
- [ ] Responsive behavior is tied to its actual owner/source instead of being invented as one global system.
- [ ] Localized UI text is identified as localized where sourced through `t(...)`.
- [ ] Explicit accessibility semantics in the source are captured.
- [ ] `IosButton` implementation divergence is documented and not fixed.
- [ ] No production React/TypeScript/CSS file was changed.
- [ ] No backend/API/persistence/DI/configuration file was changed.
- [ ] No dependency was added.
- [ ] No runtime import or loader for the JSON was created.
- [ ] JSON syntax validation passes.
- [ ] Top-level-section validation passes.
- [ ] Route coverage validation passes.
- [ ] Icon coverage validation passes.
- [ ] Source-reference validation passes.
- [ ] Existing frontend lint/build behavior is unchanged.
- [ ] The final diff contains the intended new JSON artifact only.

---

## 10. Do Not Change

**DO NOT TOUCH**

- `BusinessLogicLayer/`
- `DataAccessLayer/`
- `GF3.WebApi/`
- `GF3.Launcher/`
- `GF3.Tests/`
- backend contracts or database schema;
- `FrontEnd/src/app/router/AppRouter.tsx`;
- `FrontEnd/src/index.css`;
- `FrontEnd/src/shared/ui/motion.css`;
- manager or employee layout implementation;
- shared UI primitives;
- page components;
- entity UI components;
- localization implementation/translations;
- icon implementations;
- API/query/mutation code;
- presence/edit-lock behavior;
- authentication/authorization;
- build configuration;
- package manifests/lockfiles.

These files are sources for the design inventory, not implementation targets.

---

## 11. Execution Rule for the Coding Agent

Execute this plan as a **source-grounded extraction task**, not as a new architecture/design task.

The coding agent may open the exact source files listed above and the components directly imported by those files to extract the current visual/interaction facts required by the JSON. That inspection is for populating the already-decided schema only.

The coding agent must not:

- perform another full repository architecture analysis;
- choose a different output format;
- split the design description across several files;
- introduce a schema library;
- redesign the UI;
- refactor CSS;
- make the JSON executable/runtime-driven;
- fix unrelated code;
- change the scope defined by this plan.

If a UI fact cannot be proven from the `b4a08ad` source, omit it or mark the corresponding JSON field as unknown/not explicitly defined rather than inventing a value.
