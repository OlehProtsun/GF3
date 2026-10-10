# GF3 — Application Context

Source baseline: DEV2 @ b4a08ad

This document describes the implemented state of `OlehProtsun/GF3` at this baseline, not a future roadmap.

## 1. Project Essence

GF3 is a workforce scheduling and workforce-planning application. Managers organize workforce data and schedules; employees consume published scheduling information and perform self-service workflows. Scheduling is connected with availability, shift changes, communications, regulations, notifications and administrative operations.

The interface is a browser-based React SPA backed by an ASP.NET Core HTTP API and SQLite persistence. The repository does not establish an expansion of the name GF3 or a specific employer or business vertical.

## 2. Primary Actors

### Manager

The Manager is the administrative and planning role. Its application shell provides a home/dashboard, shops, employees, availability, containers and nested graph/schedule editing, information, communications, database administration and manager profile areas.

- Manage employees, shops, containers and nested schedules/graphs.
- Create, edit and publish availability groups for employee submissions.
- Manage employee communications and manager profile/preferences.
- Work with workflow/system information and guarded, configurable database tools.
- Participate in realtime synchronization and edit-lock coordination.

### Employee

The Employee is the self-service role. Its application shell provides profile, notifications, shift swap, schedule and availability areas.

- View published schedule information and submit availability for visible published groups.
- Work with shift swaps and receive schedule, availability and swap updates.
- View notifications, receive communications and dismiss eligible messages.
- Access profile/account flows and review/accept regulations.

Both authenticated shells include the regulation-acceptance gate. Access remains subject to API role authorization and resource visibility rules.

## 3. Core Domain Concepts

- **Employee / Employee Account:** workforce identity and employee login/account state.
- **Manager Account:** manager authentication, profile, preferences and persisted personal state.
- **Shop:** a managed workplace/location entity referenced by scheduling.
- **Container:** the manager-facing aggregate organizing nested graph/schedule resources.
- **Schedule / Graph:** scheduled workforce data with employees, slots, cell styles, presets, publication state, versions and last-update tracking. “Graph” is the project's schedule-resource term.
- **Availability Group:** a month/group-oriented collection of members and per-day availability, publishable for employee self-service; persisted day transfers are also represented.
- **Availability Bind:** a persisted availability-related binding used by manager workflows.
- **Shift Swap / Shift Correction:** schedule-change workflows with persisted requests and, for swaps, history.
- **Communication:** a manager-created employee message with visibility/deadline lifecycle and employee dismissal state.
- **Regulation:** a document with acceptance state/history; pending acceptance can gate authenticated users in the UI.
- **Workflow Log:** an audit-style operational log of important actions, with persisted settings.
- **Realtime Edit Lock:** coordination state constraining conflicting manager edits to mutable resources.

## 4. Major Manager Workflows

1. **Employee management:** Manager UI → Employees API → `IEmployeeFacade` in BusinessLogicLayer → EF Core/SQLite → realtime change notification. Mutable operations check applicable edit locks; employee session revocation is also represented.
2. **Shop management:** Manager UI → Shops API → `IShopFacade` in BusinessLogicLayer → EF Core/SQLite → realtime change notification.
3. **Schedule/container editing:** Container/graph UI → Containers API → `IContainerService` → schedule/container persistence → workflow log and realtime notification. Managers organize nested schedules, employees, slots, styles and presets, and control schedule publication and swap availability. Edit-lock checks constrain changes to resources being edited elsewhere.
4. **Availability management:** Manager availability UI → AvailabilityGroups API → `IAvailabilityGroupService` → availability persistence → publication and realtime updates. Published groups become visible to eligible employees for self-service submissions.
5. **Communications:** Manager communications UI → Communications API → `ICommunicationService` → persisted message → realtime employee-facing update. Managers control the message lifecycle; employees retrieve pending messages and dismiss them.
6. **Administrative/database operations:** the database area exposes administration/backup infrastructure behind role authorization and configuration/guard middleware. It is not an unrestricted public feature; controls govern enablement, remote access and SQL writes.

Controllers are transport/orchestration boundaries and generally delegate business rules to BusinessLogicLayer services/facades rather than implementing persistence directly.

## 5. Major Employee Workflows

1. **Login/session bootstrap:** login obtains an access token and session; the auth provider restores a stored session through the API and selects the employee shell from the role.
2. **View schedule:** the employee schedule area retrieves scheduling information visible to the authenticated employee; realtime schedule changes invalidate related cached data.
3. **Submit/update availability:** Employee UI → `api/employee-availability` → `IAvailabilityGroupService` → persisted availability day/slot data → workflow log and realtime manager-data update. The API limits access to visible published groups and checks submission eligibility and applicable edit locks.
4. **Shift swap:** the swap area exposes employee shift-swap workflows backed by persisted request/history state. Realtime swap events refresh relevant views; schedule-change correction requests are also represented in persistence/workflows.
5. **Notifications/realtime updates:** the notifications area combines employee-facing information with persisted read/pinned-swap state and realtime updates. Communications have their own pending/dismissal lifecycle.
6. **Regulations acceptance:** the authenticated gate retrieves pending regulations, allows document review and records acceptance; acceptance history remains accessible through the API.
7. **Profile/account access:** the employee profile area exposes account flows; configured email-based password recovery is available through the public recovery flow.

## 6. Authentication and Authorization

- Authentication uses the project's custom JWT token service and ASP.NET authentication handler/scheme.
- Controllers require authentication globally by default. Manager and employee role claims select authorized API operations and frontend shells.
- Explicitly anonymous auth endpoints include login, logout and password-recovery operations. Recovery uses the configured SMTP email sender.
- `AuthProvider` persists the access token in browser local storage, restores the session by calling the API, and clears invalid/401 authentication state.
- SignalR connections obtain the current access token. Session-revocation events clear client authentication, and the server closes hub connections on authentication expiration.

Sources: `GF3.WebApi/Controllers/AuthController.cs`, `GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs`, `FrontEnd/src/app/providers/AuthProvider.tsx` and `FrontEnd/src/app/providers/PresenceProvider.tsx`.

## 7. Realtime and Concurrent Editing

ASP.NET Core SignalR is cross-cutting infrastructure. Server changes pass through `IRealtimeNotifier` and the hub to React `PresenceProvider`, which updates realtime state and invalidates affected query caches.

Events cover employee presence, schedule changes, manager data changes, shift swaps, workflow-log updates, schedule/manager edit-lock state and session revocation. These events also drive employee notifications and schedule/availability/swap refreshes.

Lock targets include schedule, availability group, employee, shop, container, availability bind, manager profile and communication. Connected clients can observe changes while server lock checks constrain conflicting edits. The registered `ManagerEditLockService` is an in-process singleton shared with the schedule-lock interface; this is not evidence of distributed locking across independent application replicas.

## 8. Data and Persistence

EF Core and SQLite provide persistence through the central `AppDbContext`. Persisted areas include workforce/accounts, manager preferences/state, scheduling/versioning, availability, shift requests/history, communications, regulations/acceptances, news, notification state and workflow logs/settings.

Without a configured connection string, startup resolves a database workspace under GF3's local application-data directory, using `SQLite.db` as the default database filename. Docker mounts the persistent `gf3-data` volume at `/data` and uses `/data/SQLite.db`.

Startup checks existing SQLite integrity, applies EF migrations and checks integrity afterward. Configurable pre-migration backups snapshot existing databases when migrations are pending; an existing-database guard can reject an unintended empty deployment. Hosted services provide automatic backups and employee-notification cleanup. Database administration is protected by configurable guard middleware.

Sources: `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`, `GF3.WebApi/Infrastructure/StartupConfiguration.cs`, `GF3.WebApi/Infrastructure/DatabaseMigrationStartup.cs` and `deploy/docker-compose.yml`.

## 9. Application Architecture

GF3 is a layered application/monolith with these responsibilities:

| Component | Responsibility |
| --- | --- |
| `DataAccessLayer` | Persistence model, EF Core/SQLite and database administration. |
| `BusinessLogicLayer` | Business contracts, services/facades and domain orchestration; references DataAccessLayer. |
| `GF3.WebApi` | API transport, authentication, DI composition, middleware, realtime, contracts/mappers and SPA hosting; references BusinessLogicLayer and DataAccessLayer. The solution project is named `WebApi`. |
| `FrontEnd` | React/TypeScript SPA organized through app/pages/features/entities/shared source aliases; communicates through HTTP and SignalR. |
| `GF3.Launcher` | Windows Forms process/bootstrap/tray launcher for local browser-based use. |
| `GF3.Tests` | Backend/integration-oriented xUnit tests referencing BusinessLogicLayer, DataAccessLayer and WebApi. |

Frontend imports named `react-router-dom` and `@tanstack/react-query` resolve through Vite aliases to project-local router/navigation and query/cache implementations. They are not installed npm packages at this baseline; the query-devtools import is also a local shim.

Sources: `GF3.sln`, project manifests, `FrontEnd/vite.config.ts` and the local implementations under `FrontEnd/src/shared/lib/`.

## 10. End-to-End Runtime Flow

```text
User
  → React SPA
  → HTTP /api or SignalR
  → GF3.WebApi
  → BusinessLogicLayer
  → DataAccessLayer / EF Core
  → SQLite

Server business change → IRealtimeNotifier / SignalR hub
  → React PresenceProvider → state updates / query-cache invalidation
```

- **Development:** Vite serves the SPA; its `/api` proxy reaches the API, including realtime traffic. ASP.NET Core proxies frontend navigation to Vite.
- **Production:** React is compiled into static assets copied into the published ASP.NET Core `wwwroot`; ASP.NET Core serves them with an `index.html` fallback. Vite is a development/build tool, not the production web server.
- **Docker:** Browser → nginx public reverse proxy → ASP.NET Core container → SQLite on a persistent Docker volume. Compose supports HTTP and an optional HTTPS overlay.
- **Local launcher:** the WinForms launcher enforces a single instance, resolves runtime/database paths, starts and monitors the backend and an optional workspace/development frontend process, waits for health readiness, provides a tray UI and opens the browser. It also exposes local/LAN access information; the main application UI remains browser-based.

Sources: `GF3.WebApi/Program.cs`, `GF3.WebApi/WebApi.csproj`, `FrontEnd/vite.config.ts`, launcher startup/context files, `Dockerfile`, `deploy/docker-compose.yml` and `docs/deploy.md`.

## 11. Cross-Cutting Concerns

- **Validation/errors:** controllers, exception filters and API middleware use Problem Details and validation responses.
- **Workflow logging:** important operations record operational entries; realtime events update connected views.
- **Realtime invalidation/edit locks:** `PresenceProvider` coordinates client refreshes and editing state with server notifications and lock checks.
- **Configuration:** appsettings and environment-variable overrides configure database, authentication, SMTP, export templates and guarded administration.
- **Database protections:** integrity checks, pre-migration/automatic backups and existing-database guards support the database lifecycle.
- **Localization:** shared frontend i18n utilities and `LanguageProvider` provide localization; no third-party frontend i18n dependency is declared.
- **Excel/export:** ClosedXML and configured workbook-template infrastructure support Excel-related exports.

## 12. Implemented Scope Snapshot

The baseline includes authenticated Manager and Employee shells, the domain workflows described above, notifications/realtime/edit locks and workflow logging. Database administration, backups/migrations, a desktop launcher, backend/frontend test infrastructure and Docker/nginx deployment are represented in the implementation.

This is an implementation snapshot, not a claim that every edge case or possible product requirement is complete. Exact tooling and package versions are recorded in `TechStacks.md`.

## 13. Known Boundaries / Unknowns

The repository does not establish what GF3 stands for, a named customer/company, business requirements outside the implementation/current documentation, or a future roadmap. This document does not invent those facts or derive scope from the removed TODO file.

Future developers and agents should verify source when this baseline document and later implementation diverge.
