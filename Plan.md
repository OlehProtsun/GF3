# Plan.md — GF3: System-manager-only tools and versioned UI presentation snapshot

## 0. Execution contract and pinned baseline

- **Repository:** `https://github.com/OlehProtsun/GF3`
- **Branch:** `DEV2`
- **Analyze/execute against commit:** `d8e752a8b75c8f1542fdb1d95a80b2ab39e1ad38`.
- **Task A:** display and allow access to `/information` and `/database` only for the existing persisted **system manager**; ordinary managers and employees must not see or enter these pages and must not be able to use their privileged APIs.
- **Task B:** bring the existing `docs/design-system.json` up to the pinned current UI and augment it into a comprehensive, deterministic design/interaction/motion reference for **future** presentation-video production. Do not produce videos or introduce capture infrastructure now.
- **Workflow:** implement only the numbered steps below and run their tests. No second full repository investigation or architecture redesign. The old in-repository `Plan.md` concerns an unrelated task: **do not edit that file as part of implementation**. This delivered `Plan.md` is an external execution artifact.
- **Baseline safety check:** run `git rev-parse HEAD`, `git branch --show-current`, `git status --short`. The source baseline is the pinned SHA. Preserve any pre-existing local changes. Stop and report a baseline conflict if local files in the target scope differ in ways that prevent applying the deltas safely; never discard uncommitted work.

## 1. Confirmed current state (reference, not another analysis task)

1. `BusinessLogicLayer/Services/ManagerAccountService.cs` bootstraps/updates a system-manager account (`IsSystem = true`); `GF3_BOOTSTRAP_MANAGER_PASSWORD` configures **its password**, not its role/name selection. The persisted model (`DataAccessLayer/Models/ManagerAccountModel.cs`) and business contract (`BusinessLogicLayer/Contracts/Managers/ManagerAccountModel.cs`) already expose `IsSystem`. A database with existing managers and no system manager is intentionally not automatically re-seeded: authorization must fail closed.
2. `BusinessLogicLayer/Services/AuthService.cs` creates `AuthenticatedSessionDto` after manager/employee login. `GF3.WebApi/Controllers/AuthController.cs` projects that object to the `SessionDto` used by `POST /api/auth/login`, and constructs a new `SessionDto` from authenticated claims for `GET /api/auth/session`.
3. `GF3.WebApi/Auth/JwtAuthenticationHandler.cs` already loads the manager account from `IManagerAccountRepository` for **every authenticated manager request** and validates its credential version. It currently has no system-manager claim. This existing database lookup is the trust boundary; no new token format or repository is required.
4. `FrontEnd/src/entities/auth/model/types.ts` exposes `AuthSession`; `FrontEnd/src/app/providers/AuthProvider.tsx` stores it on login and refresh. `FrontEnd/src/app/router/AppRouter.tsx` allows all manager sessions to match `/information` and `/database`. `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx` displays Information and the DataBase Settings entry for all managers.
5. The three relevant controller areas are `GF3.WebApi/Controllers/WorkflowLogsController.cs` (`/api/workflow-logs`), `GF3.WebApi/Controllers/AdminDbController.cs` (`/api/admin/db`) and `GF3.WebApi/Controllers/AdminRegulationsController.cs` (`/api/admin/regulations`). They currently authorize `manager` rather than `system manager`. The Database page embeds `RegulationsAdminPanel`.
6. `GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs` imposes additional pre-existing developer-password, enable/write and local-access checks for admin tooling. These **remain cumulative**, not replaced by the new policy.
7. Existing `docs/design-system.json` is a substantial non-runtime descriptive catalog: `foundations`, reusable UI/components and layouts, `pages`, `routeBindings`, `patterns`, `knownImplementationDivergences`. Its `project.commit` is `b4a08ad`; it is stale relative to the pinned target. Existing `routeBindings` include 2 public, 21 manager and 6 employee entries (29 total). The current TSX/CSS remain the actual runtime authority.
8. There are already Vitest/Testing Library front-end tests, xUnit back-end tests, GSAP transitions (`FrontEnd/src/app/router/PageTransition.tsx`) and CSS motion (`FrontEnd/src/shared/ui/motion.css`). Reuse their existing test and styling conventions.

## 2. Architecture and non-negotiable choices

### 2.1 One trusted authorization fact

`ManagerAccountModel.IsSystem` loaded from the **server-side database** is the only source of system-manager entitlement. A username such as `manager`, environment variable, request header, query string, JavaScript session object, or independently supplied JWT claim may **not** grant it. An employee never has that entitlement. There is no new user role: it is a boolean attribute of an existing `manager` account. `GF3_BOOTSTRAP_MANAGER_PASSWORD` stays the only relevant already-existing bootstrap env variable. Do not print its value, create a competing system username variable, or put a password in the UI design snapshot.

For all UI paths and APIs, missing/null/false entitlement means **deny**. Client gating is UX only; authorization must be enforced by the API server.

### 2.2 Existing framework and components to REUSE

- ASP.NET Core authentication claims, authorization policies, controller-level `[Authorize]` and current `JwtAuthenticationHandler`.
- `IManagerAccountRepository.GetByIdAsync` already called by the authentication handler, business `ManagerAccountModel.IsSystem`, existing login/session endpoints and `SessionDto`.
- React `useAuth()`, `AuthProvider`, `OverlaySidebarLayout`, `AppRouter`, existing `Navigate`, `renderMatched`.
- The current developer-password/configuration guard, application roles, error/401/403 handling, SPA page components, and RegulationsAdminPanel.
- `docs/design-system.json` as the **one canonical design snapshot**; the existing TSX/CSS component definitions, page entries, page `controls`, `states`, `dialogs`, responsive descriptors, navigation and motion definitions.

### 2.3 What NOT to introduce

No database migration, new privilege table, secondary repository, fresh authentication mechanism, JWT codec change, new npm/runtime dependency, video renderer, screenshot automation, CSS refactor, new design JSON under a different name, or changes to unrelated swap/business features.

## 3. Implementation steps — Task A: Privileged pages and APIs

### Step A1 — EXTEND the internal login session contract

**File:** `BusinessLogicLayer/Contracts/Auth/AuthenticatedSessionDto.cs`

- Add `public bool IsSystemManager { get; set; }` with default `false` (normal C# bool default).
- Keep role, IDs, token-related version fields and their types unchanged.

**File:** `BusinessLogicLayer/Services/AuthService.cs`

- In the existing **manager** return object of `AuthenticateAsync(...)`, add `IsSystemManager = managerAccount.IsSystem` after manager credentials have been verified.
- The **employee** return object stays false by default; set explicitly to `false` only if consistency makes it clearer.
- Do not change password checking, marking successful login, account bootstrapping, or session versioning.

**Dependency:** existing manager contract `IsSystem`; no new DI registration.

### Step A2 — CREATE one authorization policy/claim constant holder

**File (new):** `GF3.WebApi/Auth/AuthPolicies.cs`

- **Namespace:** `WebApi.Auth`.
- **Type:** `public static class AuthPolicies`.
- **Public constants:**
  - `public const string SystemManager = "GF3.SystemManager";` (policy name).
  - `public const string SystemManagerClaim = "gf3.system_manager";` (trusted claim type).
- No authorization logic in this class. Do not duplicate the string values in controllers or handler.

### Step A3 — EXTEND trusted claims in the existing authentication handler

**File:** `GF3.WebApi/Auth/JwtAuthenticationHandler.cs`

- Within `HandleAuthenticateAsync`, retain the existing manager-account lookup and credential-version check exactly as the gate for a valid manager identity.
- Preserve the loaded manager account for use while constructing the `List<Claim>` (e.g., a nullable `isSystemManager` boolean declared before the manager branch, assigned only from `account.IsSystem` **after** successful validation).
- If **and only if** `session.Role == AuthRoles.Manager`, the repository account exists, the credential version matches **and** `account.IsSystem` is true, append `new Claim(AuthPolicies.SystemManagerClaim, "true")` to the resulting claims.
- For regular managers, employees, or absent privileges, add **no** system-manager claim.
- If manager record is missing or version mismatches, preserve existing failed-authentication behavior rather than creating an anonymous principal with a privileged claim.
- **Do not modify** `GF3.WebApi/Auth/JwtTokenCodec.cs` or token payload creation. Existing signed tokens continue to work; newly derived privilege is reevaluated against persisted `IsSystem` on every authenticated request.

### Step A4 — REGISTER standard authorization policy

**File:** `GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs`

- In existing `AddApiMvc(...)`, replace plain `services.AddAuthorization();` with the same call configured via an options lambda registering **one** policy named `AuthPolicies.SystemManager`.
- Policy requirements must all hold:
  1. authenticated principal: `.RequireAuthenticatedUser()`;
  2. manager role: `.RequireRole(AuthRoles.Manager)`;
  3. claim: `.RequireClaim(AuthPolicies.SystemManagerClaim, "true")`.
- Keep default global controller authentication filter, bearer authentication scheme and other service registrations intact.
- No extra lifetime/DI service; built-in authorization handles it.

### Step A5 — EXTEND public session DTO and both session projections

**File:** `GF3.WebApi/Contracts/Auth/SessionDto.cs`

- Add `public bool IsSystemManager { get; set; }`, serialized as `isSystemManager` by existing JSON naming conventions. The field is non-nullable and false by default.

**File:** `GF3.WebApi/Controllers/AuthController.cs`

- `ToSessionDto(AuthenticatedSessionDto session)` used by `POST /api/auth/login`: set `IsSystemManager = session.IsSystemManager`.
- `GetSession()` used by `GET /api/auth/session`: set `IsSystemManager` to **exactly** `User.IsInRole(AuthRoles.Manager) && User.HasClaim(AuthPolicies.SystemManagerClaim, "true")`; preserve existing role, name and ID projections.
- Never use `User.Identity.Name`, a password/environment variable, or `managerId` alone as a shortcut to privilege.
- Preserve route, status codes, JWT token structure and existing login/logout/password recovery behavior.

**API contract delta:** both successful session payloads gain `isSystemManager: boolean`. No other API request/response or database field changes. Older frontend consumers may ignore the additive property; older tokens remain valid subject to existing credential-revocation rules.

### Step A6 — MODIFY authorization for privileged controllers

**Files:**

1. `GF3.WebApi/Controllers/WorkflowLogsController.cs` — `[Route("api/workflow-logs")]`.
2. `GF3.WebApi/Controllers/AdminDbController.cs` — `[Route("api/admin/db")]`.
3. `GF3.WebApi/Controllers/AdminRegulationsController.cs` — `[Route("api/admin/regulations")]`.

- Replace each controller-level `[Authorize(Roles = AuthRoles.Manager)]` with `[Authorize(Policy = AuthPolicies.SystemManager)]` (add `using WebApi.Auth` only where missing).
- Protect **every action** on each controller, including GET/list/export/settings and POST/PUT/DELETE; no action should allow a regular manager by an override or `[AllowAnonymous]`.
- Preserve methods, routes, request/response DTOs, status handling, business logic, file-upload size checks and existing permission/feature flags.
- The manager system news area (`/api/admin/system-news`) is **out of scope**; do not change its permissions, since the ordinary-manager sidebar news feature remains intact. Likewise, leave employee-facing regulation acceptance/download routes outside `AdminRegulationsController` unchanged.
- Developer password, `GF3_ADMIN_ENABLED`, `GF3_ADMIN_ALLOW_REMOTE`, `GF3_ADMIN_ALLOW_WRITE`, and middleware rules remain a second independent layer. System manager authorization does **not** bypass them.

**Expected status semantics:** authenticated but non-system manager/employee receives HTTP 403 for protected endpoints when the request reaches ASP.NET Core authorization. Absent/invalid authentication receives HTTP 401 where auth runs first. Existing admin guard can separately return its currently implemented denial/status; **do not normalize or weaken it**.

### Step A7 — EXTEND front-end session type, preserve provider

**File:** `FrontEnd/src/entities/auth/model/types.ts`

- Extend `AuthSession` with `isSystemManager?: boolean`. Optional only to maintain type compatibility with existing test fixtures and cached/legacy objects. **All new backend login/session responses contain a concrete boolean.**
- Consumers check `=== true`, not truthiness/coercion, to fail closed when absent.

**File:** `FrontEnd/src/app/providers/AuthProvider.tsx` — **REUSE AS-IS**. It already consumes the session object from login and restore. No custom localStorage privilege variable, no independent entitlement cache and no new provider/service.

### Step A8 — MODIFY navigation visibility

**File:** `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx`

- Adjacent to existing `const isManager = session?.role === "manager";`, compute `const isSystemManager = isManager && session?.isSystemManager === true;`.
- In manager `mainNavItems` retain all existing links/order/icons except wrap the `/information` Information item in a conditional inclusion for `isSystemManager`, keeping Message and all other manager entries visible to regular managers. Use existing array/conditional-spread pattern, not a new sidebar component.
- Render the **entire** existing `Settings` section (including title and `/database` item) only when `isSystemManager`. No empty Settings heading for regular managers.
- Keep collapse, active nav, responsive behavior, localizations, account link, and news/notepad exactly as they are.

### Step A9 — MODIFY direct route protection

**File:** `FrontEnd/src/app/router/AppRouter.tsx`

- In `RoutedContent`, after the existing loading/unauthenticated/public-route redirects and **before** calling `renderMatched`, add a deny rule for authenticated managers who request exactly `/information` or `/database` without `session?.isSystemManager === true`.
- Use existing `<Navigate to="/" replace />` for denial. Do not instantiate/render lazy privileged pages while denied.
- Employees already use `employeeRoutes`, so they keep their current fallback to `/`; do not change employee route definitions.
- Preserve both existing entries in `managerRoutes`, lazy imports, suspense fallback, and layouts; only the additional guard changes access.
- On refresh/session restore, the provider's initial `loading` state must still show the existing loading fallback. Once the authoritative `GET /api/auth/session` result resolves, privileged routes open only for `isSystemManager:true`; if it is missing/false, redirect to `/`.

## 4. Implementation steps — Task B: Accurate presentation-oriented design snapshot

### Step B1 — REUSE and synchronize existing canonical JSON (no new catalog)

**File:** `docs/design-system.json` (MODIFY, not replace/rebuild).

- Preserve existing top-level schema domains (`foundations`, component/layout inventories, `pages`, `routeBindings`, `patterns`, `knownImplementationDivergences`) and existing stable IDs whenever those UI entities still exist.
- Update `project.commit` to the **full** pinned SHA `d8e752a8b75c8f1542fdb1d95a80b2ab39e1ad38`, `project.branch` to `DEV2`, `project.sourceOfTruth` to explicitly identify React/TypeScript/CSS at that SHA, and `project.artifactPurpose` to include future presentation/scene planning.
- Keep `project.runtimeSourceOfTruth: false`. This file is a **snapshot/reference**, not a runtime theme/configuration driver.
- Synchronize actual UI changes since catalog baseline `b4a08ad`: run **only** the bounded targeted diff `git diff --name-only b4a08ad d8e752a8b75c8f1542fdb1d95a80b2ab39e1ad38 -- FrontEnd/src` (and check changed design-linked `FrontEnd/public` assets) to identify affected page, layout, shared UI, CSS and motion sources. Open those changed sources and the current catalog entries they own; also inspect `AppRouter.tsx`, `OverlaySidebarLayout.tsx`, `PageTransition.tsx`, `FrontEnd/src/shared/ui/motion.css`, and the two protected pages. **Do not rescan unrelated backend, domain or entire repository**.
- For each affected existing design entry, amend factual `source`, `styleSources`, `composes`, `sections`, `controls` (`id`, type, purpose, componentRef, action, states), `dialogs`, `states`, responsive breakpoints, transitions and localization/accessibility notes to match **real TSX/CSS behavior at the pinned SHA**. Preserve IDs for unchanged elements. Remove claims contradicted by current code; add changed UI elements and state transitions that really exist. Do not invent components or interactions for dramatic effect.
- In particular update `employee-swap-page` against the pinned swap UI (including any **actually implemented** acceptance confirmation dialog, modal states, mobile behavior and real button actions), and keep all currently implemented schedule/animation patterns documented if changed since `b4a08ad`.
- For `information-page` and `database-page`, record the new privilege metadata `"requiresSystemManager": true`, retaining `roles:["manager"]`; regular manager pages must omit this property or set false. Mirror `"requiresSystemManager": true` on the corresponding `routeBindings` entries only. Preserve route strings exactly.
- Document sidebar distinction for `manager` vs `system manager` (visibility of Information and entire Settings section) in the existing layout entry. Include source links to `OverlaySidebarLayout.tsx`/`AppRouter.tsx` as applicable. The metadata must describe access to existing pages, not create a third role/parallel layout.
- Keep `knownImplementationDivergences` accurate and source-linked; resolve/remove an entry only when code at pinned SHA proves it is obsolete.
- Refresh motion descriptions from `PageTransition.tsx`, shared motion CSS and affected page CSS/GSAP sources. Capture real duration/easing/stagger/enter/exit, reduced-motion behavior and conditions, **only where code provides them**. Never write a duration without source evidence.
- Do **not** insert real users, email addresses, credentials, API keys, production shifts, employee records, actual log content, or screenshots into JSON.

### Step B2 — EXTEND snapshot with compact future-recording manifest

**File:** `docs/design-system.json` (same file as B1).

Add one top-level `presentationCapture` object with a stable, documented structure. Set `schemaVersion` from `1.0.0` to `1.1.0` because this is an **additive metadata-schema extension** (do not change existing readers' field meanings).

**Mandatory `presentationCapture` fields and exact semantics:**

- `purpose`: string — static guide for future UI showcase production, **not an executable capture script**.
- `snapshotCommit`: pinned full SHA, matching `project.commit`.
- `dataPolicy`: string — use **synthetic-only** demo data; redact/hide credentials, private names, access tokens, sensitive admin SQL, personal logs and business records.
- `viewports`: stable array with the following identifiers/dimensions as **planned presentation frame sizes** (not claims about design breakpoints):
  - `{ "id": "desktop", "width": 1440, "height": 900, "pixelRatio": 1 }`;
  - `{ "id": "mobile", "width": 390, "height": 844, "pixelRatio": 1 }`.
  These are two recording targets for all scenes; existing page `responsive` descriptors remain the source for actual behavior at each size.
- `scenes`: **one descriptor for each `routeBindings` entry**, including both public routes and all existing manager and employee entries (29 scenes at baseline). Keep the scene order identical to `routeBindings`. Each descriptor must have:
  - `id`: unique stable slug `<role>-<pageRef>-<route-suffix>`; for multiple paths with the same page, use a distinct suffix from route segments to avoid collisions.
  - `role`: exactly the routeBinding role (`public`, `manager`, `employee`), **not** a new role string.
  - `path`: exact router template from routeBinding (keep `:parameter` placeholders, no real IDs).
  - `pageRef`: same existing page ID as routeBinding.
  - `layoutRef`: existing `pages` item's layoutRef, not a fabricated layout.
  - `requiresSystemManager`: boolean; `true` **only** for manager `/information` and `/database`.
  - `viewportRefs`: `["desktop","mobile"]`.
  - `initialState`: `"default"` when the page has an ordinary loaded view; if it does not, use one existing page state exactly.
  - `captureStates`: array of **real page state IDs** from `pages[].states` and/or dialog `states`; include a default/loaded view and meaningful available states (`loading`, `empty`, `error`, `confirmation-open`, `success`, etc. *only if documented in that page*). For pages with empty states, use `[]` and explicitly describe the ordinary view with `initialState:"default"`. Do not create fake UI states for completeness.
  - `controlRefs`: local IDs from that page's existing `controls` array (all real available controls; `[]` if none), enabling future video work to highlight every actual interactive element without duplicating its definition. For complex screens include the page's real dialog IDs in `dialogRefs`.
  - `dialogRefs`: IDs of existing `pages[].dialogs` for that page, or empty array.
  - `motionRefs`: stable IDs existing under `foundations.motion`, where relevant (otherwise `[]`), plus reference to the page's existing per-element motion description if applicable; do not invent animations.
  - `fixtureNeeds`: an array of **generic synthetic data prerequisites** such as `"at least one mock shop"`, `"a synthetic swap offer"`, `"a system-manager test account with synthetic logs"`; no real record values. Parameterized routes must specify fixtures sufficient to fill each path parameter.
  - `sourceRefs`: one or more `{ "path": "FrontEnd/...", "symbolOrSelector": "..." }` objects pointing to the existing page TSX and any route/layout/motion files necessary to locate the scene.
- For pages whose default content depends on API data, specify only fixture **types**, not fixed personally identifying data or assertions about actual current runtime records.
- Represent interactions **through** `controlRefs` → existing `pages[].controls[].action` and `dialogRefs` → existing dialog descriptions; do not duplicate a second potentially divergent action catalog in `presentationCapture`.
- For system-manager-only scenes, fixture needs must include an authorized synthetic system account; the extra developer-password/admin flags remain prerequisites for an actual `/database` backend recording, but **never** store the password or token in the snapshot.
- Reuse page `localization`, responsive and accessibility entries; scenes should reference them via `pageRef`, not copy huge descriptions 29 times.

**Source of truth / contract:** `routeBindings` remains the sole list of routes, `pages` remains the sole catalog of controls/states/dialogs, `foundations.motion` remains the sole common motion token collection, and `presentationCapture` remains an index joining them for future creators. Keep the JSON human-readable and properly indented.

### Step B3 — CREATE lightweight snapshot consistency checker (Node standard library only)

**New file:** `scripts/validate-design-system.mjs` (repo-root script, no npm package change/dependency).

**Responsibility:** fail with nonzero exit status and a clear error for invalid/mismatched static snapshot data. Use `node:fs`, `node:path`, built-in JS. No browser, React import, screenshots or external schema library.

Required checks:

1. `JSON.parse` of `docs/design-system.json` succeeds; schemaVersion is `1.1.0`; `project.commit === presentationCapture.snapshotCommit === pinned SHA`; `project.runtimeSourceOfTruth === false`.
2. `routeBindings` is an array with unique compound keys `${role}|${path}`; `presentationCapture.scenes` is an array with **exactly one scene per compound route key**, no missing/extra entries or duplicate `id`; scene's `role`, `path`, `pageRef` and ordering match the corresponding binding.
3. Every `pageRef` resolves to a `pages[].id`; every `layoutRef` resolves to an existing documented layout id; scene `controlRefs`/`dialogRefs` reference IDs of that same page's `controls`/`dialogs`; `motionRefs` resolve to `foundations.motion[].id`.
4. `initialState` is `default` or one of that page's real `states`/dialog states. Every `captureStates` item resolves to an existing page state or dialog state. No duplicate refs per scene.
5. Viewport IDs are unique with integer positive widths/heights and positive pixel ratio; each `viewportRefs` resolves to an existing viewport id.
6. All `sourceRefs` and page `source.path`/`styleSources[].path` that are relative repo paths resolve to existing files (repo-root based); do not treat a source selector as a file path.
7. Exactly two route bindings (`manager|/information` and `manager|/database`) and their scenes carry `requiresSystemManager:true`, and both respective pages carry that property; no other binding/scene/page is accidentally privileged. Note: other routes may reuse pages; validate this via IDs rather than assuming one page per route.
8. Required scene fields have the declared types and `fixtureNeeds` contains descriptions rather than actual credentials or private records; perform simple deny-pattern checks for common secret keys and never write secrets to the error output.
9. Print short success summary: total routes, scenes, pages, components, and snapshot commit; on failure, print only item ID/path and reason, no sensitive content.

**Execution:** `node scripts/validate-design-system.mjs` from repository root. Do not add a brittle custom parser for TSX/JSX: independently compare the routes in `AppRouter.tsx` against snapshot once during B1 and re-run the validator for snapshot-internal consistency.

**Dependencies:** B1 and B2; checker validates the completed JSON.

## 5. Tests (explicit, focused coverage)

### Step T1 — EXTEND frontend sidebar unit tests

**File:** `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.test.tsx`.

- Reuse current Testing Library/Vitest patterns and existing `useAuth()` mock, make mock session privilege adjustable **without** changing production AuthProvider.
- Verify system manager shows `Information`, `DataBase` and `Settings` section.
- Verify regular manager (`role:manager, isSystemManager:false`) and legacy manager (`isSystemManager` omitted) show **none** of the three, while `Home`, `Employee`, `Shop`, `Availability`, `Container`, `Message`, manager profile, notepad/news (subject to existing mocks) remain available.
- Verify employee layout behavior remains unchanged by this flag; no privilege is inferred from `userName:"manager"`.
- Exercise the existing collapse/expand nav interaction so the conditional array does not break layout.

### Step T2 — CREATE frontend routing regression tests

**New file:** `FrontEnd/src/app/router/AppRouter.test.tsx`.

- Reuse existing Vitest, Testing Library and router/component mocks. Mock `useAuth` return values, use `window.history.replaceState` before mounting the actual `AppRouter`; assert resulting route/content or canonical current pathname after `<Navigate>`.
- Both `/information` and `/database`: `isSystemManager:true` manager may render matching protected page; false/missing manager redirects to `/`; employee redirects under existing route rules; unauthenticated redirects to `/login`.
- Keep `loading` state on a protected deep-link as fallback until auth has resolved; on transition to authenticated **non-system** manager redirect without protected page mounting.
- Verify `/communications` stays reachable to a regular manager and that unrelated manager and employee routes are unaffected.
- Never call live backend or use an actual privileged account for unit tests.

### Step T3 — CREATE backend authentication + policy regression tests

**New file:** `GF3.Tests/SystemManagerAccessTests.cs` (xUnit; use existing test helpers and the real registration/auth stack where supported).

- Reuse existing `GF3.Tests` testing patterns (SQLite test DB / `WebApplication` in-process or loopback HTTP tests as currently used in `GF3.Tests/ShiftSwapSafetyTests.cs`). No new `TestServer` dependency is required.
- Seed separate synthetic persisted accounts: **system manager** (`IsSystem=true`), **regular manager** (`IsSystem=false`), employee; use only synthetic credentials and disposable database. Include an account with username literally `manager` **but** `IsSystem=false` to prove name grants nothing.
- `POST /api/auth/login`: successful system manager session response has JSON `isSystemManager:true`; regular manager/employee have false; no changes to token issuance.
- `GET /api/auth/session`: same booleans after using the issued bearer tokens. Test old-format/current signed tokens without a special claim still authorize the system manager because the handler derives the claim from the database.
- Test `JwtAuthenticationHandler` principal claims: system manager gets exactly the trusted true claim; regular manager/employee no such claim. Missing manager row, revoked credential version, or cleared `IsSystem` must not confer system rights; **clearing `IsSystem` while the token remains otherwise valid must remove privilege on the next request**.
- Policy assertion/endpoint tests for all three controllers: anonymous/invalid token blocked; regular manager and employee forbidden **for both safe reads and at least one write action**; system manager may reach the controller if all *existing* developer-password, enable/local/write guard requirements also hold. Assert policy denial before any DB mutation; no privileged action actually modifies a persistent developer database.
- Specifically test at least these representatives: `GET /api/workflow-logs`, `GET /api/admin/db/...` selecting a real route from that controller, `GET /api/admin/regulations`, and one respective mutation endpoint per controller where present (e.g. settings update, DB execute, regulation creation). Do not invent route templates; use controller attributes already defined.
- Verify an ordinary-manager endpoint and manager system news existing flow retain their current auth semantics. Verify employee-facing published regulation acceptance remains available under its existing rules.
- Test the system manager **still** fails existing admin guard when required flags/password are absent. Expected errors may originate from either guard or policy according to current middleware order; assert denial, not a guessed uniform error body.
- Use the same auth handler, service registration and authorization policy as runtime. No hand-rolled test-only privilege logic that can pass while production handler is incorrect.

### Step T4 — VERIFY design checker, shape and privacy

- Execute the new validator against the real design JSON.
- Temporarily (without committing) use in-memory transformed JSON fixtures or throwaway copies to prove the checker rejects: missing scene, duplicate role/path, broken `controlRef`, broken `layoutRef`, stale commit, incorrect `requiresSystemManager`, invalid viewport. Restore source immediately afterward; no modified fixture files retained.
- Manually inspect at least `employee-swap-page`, `information-page`, `database-page`, the manager layout, a public page and an employee page, tracing `controls`, `dialogs`, layout, CSS and GSAP/motion sources to current code.
- Verify all 29 baseline role/path keys are represented and no snapshot scene includes live or sensitive values. If the pinned router differs from the documented 29 baseline, the **actual router at pinned SHA wins** and the catalog/scene count is updated accordingly; document that observation in CODEX's execution summary rather than silently creating fake routes.

## 6. Error, security and compatibility behavior

- **No system account present:** no one gains privileged UI or controller access; no `username=="manager"` fallback. Do not alter bootstrap behavior as an unrelated migration/recovery project.
- **Privilege removed in DB:** next successful manager authentication handling does not produce the claim, `/api/auth/session` reports false and all protected controllers deny; after client refresh or next session restoration, links/routes are hidden. Already-mounted clients may temporarily show stale nav until session refresh, but server blocks operations immediately; do not introduce polling or websockets for this task.
- **Expired or invalid token:** current 401/login path; do not return system-manager status based on stale local user objects.
- **Legacy frontend mocks/session without property:** deny both pages by default; no crash or unsafe assumption.
- **Developer password / admin enable flags:** existing middleware continues to enforce it even for system account; any UI-only visibility change must not cause the admin guard to be relaxed.
- **Role vs privilege:** use existing `manager` role and additional `IsSystem` claim; never grant access merely because a session claims `role:manager` on the client.
- **Confidentiality:** snapshot may identify that a sensitive UI control/dialog exists, but must not include passwords, raw SQL contents, real employee/shift data, user identifiers from production, or access tokens.
- **API changes:** only additive boolean `isSystemManager` to login/session DTOs; no endpoint renaming or HTTP verb changes.
- **Persistence:** no schema change, no migration, no index, no changed write flow. Read existing `manager_account.is_system` through existing repository lookup.
- **DI:** authorization policy registration only; no new services or lifetimes.
- **Frontend visual regression:** no CSS modifications required for hiding the two nav entries; preserve current spacing and responsive layout.

## 7. Verification sequence for CODEX

Run from the pinned branch/worktree; commands are scoped and must be completed **after** code and snapshot edits:

```bash
git rev-parse HEAD
git branch --show-current
git status --short
# Scope discovery only for presentation snapshot delta, not a full new analysis:
git diff --name-only b4a08ad d8e752a8b75c8f1542fdb1d95a80b2ab39e1ad38 -- FrontEnd/src FrontEnd/public

# Backend:
dotnet build GF3.Tests/GF3.Tests.csproj --configuration Release
dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "FullyQualifiedName~SystemManagerAccessTests"
dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release

# Frontend (existing dependencies must already be installed, otherwise npm ci):
cd FrontEnd
npm run test -- src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.test.tsx src/app/router/AppRouter.test.tsx
npm run build
npm run lint
cd ..

# Snapshot and patch integrity:
node scripts/validate-design-system.mjs
git diff --check
git status --short
git diff --stat
```

Expected runtime smoke checks (isolated local test setup, **never** against production data):

1. Sign in as a system manager backed by `IsSystem=true`. Both sidebar links and direct URLs open; matching controllers obey both the new policy and existing AdminTools guard.
2. Sign in as a separate regular manager. Both links and the empty Settings block are hidden; direct URLs redirect to `/`; requests to privileged API endpoints are blocked. Ordinary manager pages, profile, communications, and system-news widget remain functional.
3. Sign in as an employee: existing employee sidebar/workspace and routes remain unchanged; attempting privileged URLs/API endpoints does not grant access.
4. Reload `/information` or `/database` under each account, checking auth-loading and restored session behavior. Logout and login with another account must not leak the previous account's privileges.
5. Review `docs/design-system.json` with checker success, realistic coverage of shared and page-level controls, dialogs, responsive values, transitions and source file pointers. Verify JSON has no secrets or production snapshots.

If a test is unavailable because a local tool or runtime prerequisite is missing, document **which command was not executed**, the missing prerequisite and the remaining risk; never claim it passed.

## 8. Acceptance checklist

### Task A — visible behavior and security

- [ ] Existing persisted `IsSystem=true` is the sole entitlement source; `GF3_BOOTSTRAP_MANAGER_PASSWORD` continues to control bootstrap password only.
- [ ] Login **and** restore-session DTOs return concrete correct `isSystemManager` booleans; any missing property defaults to deny on the client.
- [ ] API handler derives privilege from an existing database account, not self-asserted claims, username, or client state.
- [ ] Standard built-in policy requires authentication, manager role and trusted system claim.
- [ ] `/api/workflow-logs`, `/api/admin/db` and `/api/admin/regulations` deny every action to ordinary managers/employees and remain available to qualifying system manager subject to existing extra admin restrictions.
- [ ] `/information` and `/database` sidebar entries are absent for ordinary managers; no empty Settings section; direct-link/reload protection works.
- [ ] No change to unrelated navigation, employee routes, ordinary manager abilities, manager system news, employee regulations acceptance or other APIs.
- [ ] Previous JWTs still authenticate under normal credential/version rules; removing `IsSystem` removes authorization on next request.
- [ ] No database migration, new env keys or credential exposure.

### Task B — future-video presentation reference

- [ ] Only existing `docs/design-system.json` serves as design catalog; source-of-truth declaration remains descriptive and pinned to full SHA.
- [ ] Current page/control/dialog/state, shared UI, layout, responsive and actual motion/GSAP declarations are accurately reflected, especially recent changes since `b4a08ad`.
- [ ] Every actual router route is represented in `routeBindings` and exactly one `presentationCapture.scenes` descriptor, with source, local control/dialog IDs, states, viewport targets, synthetic fixture prerequisites and real motion refs.
- [ ] Only `/information` and `/database` are marked system-manager-only; each scene uses an existing role, page and layout.
- [ ] `node scripts/validate-design-system.mjs` passes and fails correctly on negative fixtures.
- [ ] No production data or credentials in snapshot. No video/capture infrastructure installed or built.

### Quality / scope

- [ ] Backend build/tests and frontend tests/build/lint pass (or missing environment issues are explicitly reported).
- [ ] `git diff --check` clean; changed files confined to steps A1–A9, B1–B3, T1–T3 (plus only a **directly necessary** adjacent test fixture update).
- [ ] No unrelated refactors, feature changes, persisted data changes or overwritten old repo `Plan.md`.

## 9. Explicit DO NOT TOUCH list

- `BusinessLogicLayer/Services/ManagerAccountService.cs` bootstrap logic, `IManagerAccountRepository`/EF schema/migrations; existing `IsSystem` is already persisted.
- `GF3.WebApi/Auth/JwtTokenCodec.cs`, token transport/token issuance, JWT signing configuration.
- `GF3.WebApi/Program.cs`, runtime middleware ordering, and `GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs`.
- `GF3.WebApi/Controllers` other than the four named controller files (`AuthController` and three privileged controllers); specifically avoid system news and employee-facing regulations endpoints.
- `FrontEnd/src/pages/information/ui/InformationPage.tsx`, `FrontEnd/src/pages/database/ui/DataBasePage.tsx`, `FrontEnd/src/entities/regulations/ui/RegulationsAdminPanel.tsx` (no need to adjust their page logic).
- `FrontEnd/src/app/providers/AuthProvider.tsx`, `FrontEnd/src/app/router/PageTransition.tsx`, motion CSS and unrelated page styling/runtime elements (read for reference only).
- Existing design reference IDs unless the corresponding component has genuinely been removed/renamed in pinned code.
- Root `Plan.md` in the repository, existing `.env` or secrets, deployment configuration, and swap/shift workflows.

**End state:** focused account-level privilege gate in API and React UI, plus one reliable, verified, version-pinned JSON catalog ready for future UI video planning. No implementation is performed by this plan artifact itself.
