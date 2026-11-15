# GF3 — Technology Stack

Source baseline: DEV2 @ b4a08ad

This inventory describes `OlehProtsun/GF3` at this baseline. Versions and version ranges come from project/package/build manifests at that commit; npm ranges are declarations, not claims about every resolved lockfile version. Product concepts and workflows are documented in `appcontext.md`.

## 1. Stack at a Glance

| Area | Technology | Role |
| --- | --- | --- |
| Backend runtime | .NET 10 / C# | Backend application and tests. |
| API | ASP.NET Core | HTTP controllers, middleware and production SPA host. |
| Persistence | Entity Framework Core 10 (EF Core) + SQLite | Central persisted application data. |
| Frontend | React 19 + TypeScript 5.9 + Vite 7 | Browser SPA and asset toolchain. |
| Realtime | ASP.NET Core SignalR + `@microsoft/signalr` | Authenticated updates, presence and edit coordination. |
| Desktop launcher | Windows Forms / `net10.0-windows` | Local runtime bootstrap and tray UI. |
| Testing | xUnit + Vitest + Testing Library + Playwright | Backend, frontend and browser verification tooling. |
| Deployment | Docker + Docker Compose + nginx | Container build, orchestration and public proxy. |
| CI | GitHub Actions | Verification and dependent Docker build. |

## 2. Languages and Runtime Targets

- C# backend projects use .NET 10. `DataAccessLayer`, `BusinessLogicLayer`, `GF3.WebApi` and `GF3.Tests` target `net10.0`.
- `GF3.Launcher` targets `net10.0-windows`.
- The frontend uses TypeScript `~5.9.3` with JavaScript/ES modules (`type: module`).
- README declares Node.js **20+** as the minimum requirement; Docker and CI use **Node 22** as their build environment. These are minimum-requirement and build-environment facts, respectively.

Sources: `GF3.sln`, the five `.csproj` files, `FrontEnd/package.json`, `README.md`, `Dockerfile` and `.github/workflows/ci.yml`.

## 3. Backend

`GF3.WebApi/WebApi.csproj` uses `Microsoft.NET.Sdk.Web`; the solution project is named `WebApi`. ASP.NET Core Web API supplies controllers/MVC, built-in dependency injection, middleware, authorization, response compression and Problem Details. Swagger/OpenAPI is configured through API explorer and Swashbuckle; Swagger UI is enabled in development. GF3.WebApi also hosts the compiled SPA in production.

| Package | Version |
| --- | --- |
| `Microsoft.AspNetCore.OpenApi` | `10.0.9` |
| `Microsoft.AspNetCore.SpaProxy` | `10.0.9` |
| `Microsoft.AspNetCore.SpaServices.Extensions` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.9` |
| `Swashbuckle.AspNetCore` | `10.2.3` |

GF3.WebApi references both BusinessLogicLayer and DataAccessLayer. Composition and hosting are defined in `GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs` and `GF3.WebApi/Program.cs`.

## 4. Business Logic Layer

BusinessLogicLayer contains business contracts, services/facades and domain orchestration and references DataAccessLayer. Its SDK is `Microsoft.NET.Sdk`.

| Package | Version | Role |
| --- | --- | --- |
| `ClosedXML` | `0.105.0` | Excel workbook/export-related operations. |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.0` | Configuration-bound options. |

Source: `BusinessLogicLayer/BusinessLogicLayer.csproj`. Excel templates are configured through the existing `ExportTemplates` infrastructure and copied by the WebApi project.

## 5. Data Access and Database

DataAccessLayer uses `Microsoft.NET.Sdk` and the central `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs` EF model.

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.9` |
| `SQLitePCLRaw.bundle_e_sqlite3` | `3.0.3` |

SQLite is the primary persisted database at this commit. Without an explicit connection string, startup resolves the GF3 local application-data database workspace, defaulting to `SQLite.db`. Docker uses `/data/SQLite.db` on the persistent `gf3-data` volume.

Application startup applies EF migrations. Existing databases undergo SQLite `PRAGMA quick_check` before migrations, and integrity is checked afterward. Configurable pre-migration backups apply when an existing database has pending migrations; an existing-database guard supports production deployments. Hosted services provide automatic backups and employee-notification cleanup.

Sources: `DataAccessLayer/DataAccessLayer.csproj`, `GF3.WebApi/Infrastructure/StartupConfiguration.cs`, `GF3.WebApi/Infrastructure/DatabaseMigrationStartup.cs`, service registration and `deploy/docker-compose.yml`.

## 6. Authentication and Security

- Project-specific `IJwtTokenService` / `JwtTokenService` and `JwtAuthenticationHandler` / authentication scheme implement JWT authentication.
- ASP.NET authorization and manager/employee role checks enforce API access. A global MVC authorization filter requires authenticated controller requests by default, with explicit anonymous auth/recovery exceptions.
- `AuthAttemptLimiter` provides authentication attempt limiting.
- JWT signing settings come from configuration/environment; production startup requires an explicit signing key.
- Administration uses configurable `AdminTools` controls and API guard middleware.
- Password recovery uses `IEmailSender` / `SmtpEmailSender` with configured SMTP options.

These mechanisms are registered in `GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs`; representative authorization is visible in `GF3.WebApi/Controllers/AuthController.cs` and role-specific controllers. Authentication does not use an ASP.NET Core Identity, OAuth/OIDC or `Microsoft.AspNetCore.Authentication.JwtBearer` dependency in this verified stack.

## 7. Realtime

The server uses ASP.NET Core SignalR; the frontend declares `@microsoft/signalr` `^10.0.0`. The client supplies the session access token to its hub connection and supports reconnects. Server hub mapping closes connections on authentication expiration.

`IRealtimeNotifier` / `RealtimeNotifier` and React `PresenceProvider` connect presence, schedule/manager data changes, shift-swap updates, workflow logs, edit-lock synchronization, session revocation and employee notifications with query-cache invalidation hooks.

`ManagerEditLockService` is registered as an in-process singleton and shared with the schedule-lock interface. This baseline does not establish distributed locking across application replicas.

Sources: service registration, `GF3.WebApi/Program.cs`, `FrontEnd/src/app/providers/PresenceProvider.tsx` and `FrontEnd/package.json`.

## 8. Frontend

External runtime dependencies declared by `FrontEnd/package.json`:

| Package | Version range |
| --- | --- |
| `react` | `^19.2.0` |
| `react-dom` | `^19.2.0` |
| `@microsoft/signalr` | `^10.0.0` |
| `gsap` | `^3.15.0` |
| `@gsap/react` | `^2.1.2` |

Build/dev tooling:

| Package | Version range |
| --- | --- |
| `typescript` | `~5.9.3` |
| `vite` | `^7.3.1` |
| `@vitejs/plugin-react` | `^5.1.1` |
| `vite-plugin-svgr` | `^4.5.0` |
| `@types/node` | `^24.10.1` |
| `@types/react` | `^19.2.7` |
| `@types/react-dom` | `^19.2.3` |

ESLint and associated quality packages are listed in section 10. Vite's `@app`, `@pages`, `@features`, `@entities` and `@shared` aliases map to the corresponding `FrontEnd/src/` directories. They organize project source; they are not an external framework.

## 9. Important Project-Local Frontend Infrastructure

`FrontEnd/vite.config.ts` maps apparently external import names to local source:

| Import name | Local implementation |
| --- | --- |
| `react-router-dom` | `FrontEnd/src/shared/lib/react-router-dom.tsx` |
| `@tanstack/react-query` | `FrontEnd/src/shared/lib/tanstack/react-query.tsx` |
| `@tanstack/react-query-devtools` | `FrontEnd/src/shared/lib/tanstack/react-query-devtools.tsx` |

**React Router and TanStack Query are not npm dependencies at this commit.** Their import names resolve through Vite aliases to lightweight project-local router/navigation and query/cache APIs exposing compatible names. The devtools import is likewise a local implementation/shim, not an installed devtools package. `QueryProvider` scopes the local query client to the current account.

Localization uses custom/shared frontend i18n utilities under `@shared/i18n` and `LanguageProvider`. No third-party i18n package is present in `FrontEnd/package.json`.

## 10. Frontend Testing and Quality

Declared tools in `FrontEnd/package.json`:

| Package | Version range | Role |
| --- | --- | --- |
| `vitest` | `^4.1.5` | Frontend tests. |
| `jsdom` | `^29.1.1` | Test DOM environment. |
| `@testing-library/react` | `^16.3.2` | React behavior tests. |
| `@testing-library/jest-dom` | `^6.9.1` | DOM assertions. |
| `@testing-library/user-event` | `^14.6.1` | User-interaction simulation. |
| `@playwright/test` | `^1.59.1` | Browser/E2E tests. |
| `eslint` | `^9.39.1` | Linting. |
| `@eslint/js` | `^9.39.1` | JavaScript ESLint configuration. |
| `typescript-eslint` | `^8.48.0` | TypeScript ESLint support. |
| `eslint-plugin-react-hooks` | `^7.0.1` | React hooks lint rules. |
| `eslint-plugin-react-refresh` | `^0.4.24` | React refresh lint rules. |
| `globals` | `^16.5.0` | Global identifier definitions for linting. |

Vite's test configuration selects jsdom and `FrontEnd/src/test/setup.ts`, excluding E2E files from Vitest.

Run scripts from `FrontEnd`:

| Command | Manifest action |
| --- | --- |
| `npm run dev` | Vite development server. |
| `npm run build` | TypeScript project build (`tsc -b`), then Vite build. |
| `npm run lint` | `eslint .` |
| `npm run test` | `vitest run` |
| `npm run test:watch` | `vitest` |
| `npm run test:e2e` | `playwright test` |
| `npm run preview` | `vite preview` |

## 11. Backend Testing

`GF3.Tests/GF3.Tests.csproj` uses `Microsoft.NET.Sdk`, targets `net10.0` and references BusinessLogicLayer, DataAccessLayer and GF3.WebApi, plus the `Microsoft.AspNetCore.App` framework.

| Package | Version |
| --- | --- |
| `xunit` | `2.9.3` |
| `xunit.runner.visualstudio` | `3.1.4` |
| `Microsoft.NET.Test.Sdk` | `17.14.1` |
| `coverlet.collector` | `6.0.4` |
| `Microsoft.EntityFrameworkCore` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Relational` | `10.0.9` |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.9` |
| `ClosedXML` | `0.105.0` |

These dependencies support backend/integration-oriented tests, SQLite persistence checks and workbook-related testing. The backend test framework is xUnit.

## 12. Desktop Launcher

`GF3.Launcher/GF3.Launcher.csproj` uses `Microsoft.NET.Sdk`, `net10.0-windows`, `UseWindowsForms=true` and `WinExe` output.

`GF3.Launcher/Program.cs` and `LauncherApplicationContext.cs` implement single-instance startup, runtime/database path resolution, backend and optional workspace/development frontend process management, health readiness checks, tray controls, browser launch and local/LAN access information.

Windows Forms provides a launcher/bootstrap shell around the browser application; it is not an alternative implementation of the main product UI.

## 13. Build and SPA Integration

- **Development:** WebApi's `SpaRoot` points to `FrontEnd`; SpaProxy launches `npm run dev` on Vite port `5173`. A Debug build restores dependencies with `npm install` when `node_modules` is absent. ASP.NET Core proxies frontend navigation to Vite, while Vite proxies `/api` traffic to the development API with WebSocket support.
- **Production publish:** unless `SkipFrontendBuild=true`, the WebApi publish target restores missing frontend dependencies, runs `npm run build` and publishes `FrontEnd/dist` as API `wwwroot` assets. ASP.NET Core serves static files and an `index.html` fallback.
- **Docker build:** the frontend is built separately; backend publish sets `SkipFrontendBuild=true`, and Docker copies the already-built assets into published `wwwroot`.

Sources: `GF3.WebApi/WebApi.csproj`, `GF3.WebApi/Program.cs`, `FrontEnd/vite.config.ts` and `Dockerfile`.

## 14. Deployment and Operations

`Dockerfile` defines these stages:

| Stage | Image | Responsibility |
| --- | --- | --- |
| Frontend build | `node:22-alpine` | `npm ci` and frontend build. |
| Backend publish | `mcr.microsoft.com/dotnet/sdk:10.0` | API restore/publish and SPA asset assembly. |
| Runtime | `mcr.microsoft.com/dotnet/aspnet:10.0` | Published ASP.NET Core application. |

The runtime listens on port `8080`, runs as the image's `app` user and uses a container health check against `/api/health`.

`deploy/docker-compose.yml` places `nginx:stable-alpine` in front of the app as the public reverse proxy, with persistent `gf3-data` mounted at `/data` for `/data/SQLite.db` and database backups. Deployment values are supplied through environment variables and Docker `.env` configuration.

HTTP deployment and an optional HTTPS Compose overlay/certificate setup are documented under `deploy/` and `docs/deploy.md`. Startup backup-before-migration and existing-database safeguards protect the database lifecycle; operations guidance includes backup before updates. Vite does not serve the production application.

## 15. CI/CD

`.github/workflows/ci.yml` uses GitHub Actions with `ubuntu-latest`, .NET `10.0.x` and Node `22`.

The verification job restores `GF3.Tests/GF3.Tests.csproj`, runs backend tests in Release, then runs frontend `npm ci`, lint, tests and build. The Docker job depends on successful verification and builds `gf3-web:ci` from `Dockerfile`.

The workflow triggers on pushes to main/master, pull requests and manual dispatch. This description covers the verified CI build workflow and does not assert registry publication.

## 16. Configuration Surface

Existing configuration mechanisms include `GF3.WebApi/appsettings.json`, `appsettings.Development.json`, environment-variable overrides and deployment `.env` values.

- **Database:** connection-string configuration, local-workspace fallback and Docker database path/volume settings.
- **JWT:** issuer, audience, access-token duration and signing-key settings; production requires an explicit signing key.
- **SMTP:** host, port, TLS, sender identity and credentials configured for recovery email.
- **ExportTemplates:** template-directory and local application-data seeding options.
- **AdminTools:** enablement, remote access, SQL-write permission and size limits, with supported `GF3_ADMIN_*` environment overrides and developer-password configuration.
- **DatabaseStartup:** `RequireExistingDatabase` and `BackupBeforeMigrate`; Compose supplies corresponding deployment controls.
- **Deployment:** image, public ports, persistent volume and environment/configuration overrides.

Sources: the two appsettings files, `GF3.WebApi/Infrastructure/StartupConfiguration.cs`, service registration, `DatabaseMigrationStartup.cs` and `deploy/docker-compose.yml`. Only configuration categories/key names are documented; no secret values are included.

## 17. Deliberately Not Used / Common Misreadings

At `DEV2 @ b4a08ad`, the verified stack has:

- No external React Router or TanStack Query package in `FrontEnd/package.json`; their import names map to local implementations.
- No ASP.NET Core Identity evidence or OAuth/OIDC integration in the verified authentication stack, and no JwtBearer package dependency.
- No MongoDB, PostgreSQL or SQL Server as primary persistence; SQLite is used.
- No identified microservice platform, message broker or external cache infrastructure. The local frontend query cache is application code.
- No third-party frontend i18n dependency.

These are baseline-specific observations. Verify later source/manifests before applying them to future versions.
