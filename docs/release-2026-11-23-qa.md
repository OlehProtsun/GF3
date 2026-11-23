# GF3 release QA · 2026-11-23

**BLOCKED · MERGE NO-GO · DEPLOY BLOCKED**

## Baseline

- Source `dev22.11.2026` at `b99fe90a098d6be68d0cbe33e75265aee972bab2`.
- Target `PROD2` and merge base at `1175c39461e149a7bb35f428babed28279727899`.
- Clean starting checkout. Four commits ahead and zero behind. The complete tree diff contains 399 paths (302 added, 97 modified).

| Commit | Paths touched | Main changes |
| --- | ---: | --- |
| `d45ae20` | 78 | Disjoint swaps and PWA |
| `619b335` | 79 | Corrections, schedules and launch log |
| `7482e29` | 213 | App Guide |
| `b99fe90` | 59 | Communications and media |

The per-commit counts include paths touched in multiple commits. The inventory below lists each path once using `git diff --name-status origin/PROD2...HEAD`.

## Baseline verification

| Command or area | Result |
| --- | --- |
| `git fetch origin PROD2 dev22.11.2026` and `git ls-remote origin refs/heads/PROD2 refs/heads/dev22.11.2026` | PASS. Both remote tips match the pinned SHAs. |
| `dotnet restore GF3.Tests/GF3.Tests.csproj` | PASS. All four projects restored or up to date. |
| `dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --no-restore --filter 'Category!=LocalOnly'` | BLOCKED in compilation. `SixLabors.ImageSharp.targets(35,5): error : No Six Labors license found.` Test discovery did not run. |
| `python -m unittest discover -s deploy/security/tests -v` | PASS on Windows retry, 53 passed and 4 Ubuntu-only advisory-lock tests skipped. The first run hit 23 sandbox errors in `test_gf3_prepare_release.py:49`, where `git push origin PROD2` exited 128 against the fixture's temporary bare repository. All 23 cleared on retry. Linux host-guard verification is pending. |
| `npm ci` | PASS after running outside the process-spawn restriction (`EPERM`). 325 packages installed. npm reported 25 advisories. |
| `npm run lint` | PASS, 0 errors and 21 warnings. |
| `npm run test` | FAIL, 1,448 passed, 56 failed, 1 skipped across 145 test files. Failure evidence below. |
| `npm run build` | FAIL in `tsc -b`. `QueryProvider.tsx:57` raises TS2339 and `entities/communications/api/queries.ts:17,29` raise TS2353 because the mutation types lack `meta`. Vite build did not start. |
| `npx playwright install chromium webkit` | PASS. Both browser engines launched in the mocked PWA run. |
| `node node_modules/@playwright/test/cli.js test e2e/pwa-launch.spec.ts --reporter=line --output=../GF3.Tests/TestResults/plan78-baseline-pwa` | FAIL, 4 passed and 8 failed. On each of Chrome desktop, mobile Chrome, WebKit laptop and WebKit iPhone X, the unauthenticated redirect passed and the obsolete expectations at lines 35 and 64 failed. An earlier run with an incorrect Vite root was discarded and rerun after `/login` returned HTTP 200 with the app root. |
| GitHub `ci.yml` for `dev22.11.2026` | No run found. No open PR from the source branch to `PROD2` when checked. |
| Full E2E, Docker and ingress | NOT RUN. The Release backend build is blocked. The local Docker Desktop Linux engine was also unavailable. |
| Physical iOS and Android devices | NOT VERIFIED. |

### Vitest failure evidence

| Failed file | Count | First failure evidence or affected scenarios |
| --- | ---: | --- |
| `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx` | 30 | `QueryClientProvider is missing` at `shared/lib/tanstack/react-query.tsx:337`, called by `useEmployeeShiftSwapsQuery` from `EmployeeSchedulePage.tsx:312`. PDF, Daily, Matrix, summary and schedule-selection cases failed. |
| `FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.test.tsx` | 16 | `Cannot read properties of undefined (reading 'filter')` at `EmployeeShiftCorrectionDialog.tsx:85` on the `schedules` prop. |
| `FrontEnd/src/pages/login/ui/LoginPage.test.tsx` | 3 | Username/password mode restoration, numeric mode persistence, and requiring username before opening. |
| `FrontEnd/src/pages/home/ui/HomePage.test.tsx` | 2 | Work-header date controls across day/month boundaries, and stale in-flight activity after switching tab/month. |
| `FrontEnd/src/pages/information/ui/InformationPage.test.tsx` | 2 | System PC presence/launch history/account list and workflow-log visibility after usage failure. |
| `FrontEnd/src/shared/i18n/i18nCoverage.test.ts` | 2 | Dynamic Guide/schedule/correction translations lack coverage review, and Polish server templates fail coverage. |
| `FrontEnd/src/shared/i18n/i18n.test.ts` | 1 | Polish translation check at line 107 rejects the Cyrillic language name in an App Guide instruction. |

## Baseline Release build blocker · resolved

Plan step 0 requires a baseline Release .NET run before test changes. The direct `SixLabors.ImageSharp` 3.2.0 reference in `GF3.WebApi/WebApi.csproj` invokes a license validation target before `CoreCompile`. The package accepts `SixLaborsLicenseKey`, `SixLaborsLicenseFile`, or `sixlabors.lic`. No local license input was available. The repository's CI workflow and Dockerfile provide no license input, and GitHub reported no repository secret named for Six Labors or ImageSharp. The Dockerfile also publishes the same project in Release configuration.

The candidate uses SkiaSharp 4.157.0. No license input is needed. Once the build was unblocked, the first complete Windows Release test run produced **1,895 passed, 2 failed, 0 skipped**. The failures were an out-of-date migration ID assertion and a day-off race test fixture that waited inside a lock held by the transaction. The final Release run produced **1,931 passed, 0 failed, 0 skipped** under the CI filter `Category!=LocalOnly`.

## Candidate changes and verification

| Area | Reproduction and result |
| --- | --- |
| Licensing and photos | Replaced ImageSharp 3.2.0 with SkiaSharp 4.157.0 and the Linux NoDependencies native package. Tests cover JPEG/PNG/WebP, all eight EXIF orientations, transparent pixels, discarded metadata, corrupt and oversized uploads, legacy GIF and opaque media keys. Replaced MPL-2.0 WebPush with MIT Lib.Net.Http.WebPush, preserved PDFsharp 6.2.4 (MIT) and bundled Carlito (OFL 1.1). See [resolved licenses and outstanding notices](third-party-licenses.md). |
| .NET across platforms | Windows Release **1,931 passed, 0 failed, 0 skipped** under the CI filter (`GF3.Tests/TestResults/plan78-release-candidate.trx`). Windows LocalOnly **405 passed, 0 failed, 0 skipped** (`plan78-localonly.trx`). In a network-disabled Linux container with the .NET 10.0.401 SDK, **46 passed, 0 failed, 0 skipped** across focused photo, PDF and push tests. The Linux test container used a cached runtime. The production image build has a separate result below. |
| Swap integrity | A manager-created exchange failed to reserve its entire return-side slot in the employee wizard. Fixed reservation logic and added interval, concurrent accept and duplicate-request tests in `swapReservedWindows.test.ts` and `ShiftSwapDisjointReservationsTests.cs`. Tested disjoint acceptance and surviving-offer rebinding in Chromium. |
| Shift corrections | A swap created after the request caused approval to return validation 400. Approval now returns 409 and preserves both workflows. The employee wizard disables removal when the published workday has an active swap. Added multi-shift removal, review races, rollback and scope coverage in `ShiftCorrectionRemovalTests.cs` and the wizard tests. The employee submission and manager approval browser scenario passed in Chromium. |
| Public schedules and PDF | Added SQLite scope/assignment checks, authorized and rejected PDF responses, missing/deleted scope and sanitized failure tests, plus Daily/Matrix, ranking and pending status coverage. The schedule page fixtures supply all required query data and use the current grouped chooser. |
| Communications | An SQLite trigger rejection left new media orphaned on create, and an update propagated the raw SQLite constraint error. Failed multipart writes now discard only newly staged files, and update rolls back as one transaction. Same-key media failure carried over to the next message until the media subtree was keyed by both message and attachment. Protected read/dismiss headers, legacy JSON editing, cleanup and validation are covered by backend and UI tests. Chromium and iPhone WebKit passed the ordered photo/text/GIF preview, save, employee view and dismiss scenario. |
| TypeScript and test contracts | Restored mutation metadata propagation for local error handlers. `npm run build` passed TypeScript and the Vite bundle. Updated stale tests for the correction wizard, distinct Home period/date controls, Login PIN and system PC workflow history. The PWA fixtures now verify same-tab dismissal, independent tabs, startup splash and one launch per document. |
| Vitest regression suite | Baseline **1,448 passed, 56 failed, 1 skipped**. A parallel attempt hit the existing 5-second timeout in the specific-exchange wizard while Playwright was active. An independent run of all **159 files** with four workers passed **1,544 tests, 0 failed, 1 pre-existing skip**. No test timeout or retry settings changed. |
| Guide, i18n and assets | Covered Guide language persistence, topics and navigation, plus catalog coverage for the current dynamic strings. Chromium decoded **216** PNG/WebP/GIF assets and verified all **186** distinct Guide references against declared dimensions. All eight release GIFs pass the size limit. This covers file integrity and dimensions. Asset provenance and physical-device visual QA remain open. |
| Lint and Playwright CI wiring | Lint passed with **0 errors and 21 existing warnings**. The exact two-browser smoke configuration passed **12 tests, 0 failed, 0 skipped**. CI YAML parses and the smoke job precedes the existing Docker job. |
| Host guard | Windows **53 passed, 0 failed, 4 skipped** (Linux-only advisory locking). A disposable Linux run with a read-only repository mount passed **57 tests, 0 failed, 0 skipped**. |
| Dependency advisory checks | Patched the production lock entry for MIT `ws` from 7.5.10 to 7.5.14 within SignalR's version range. `npm audit --omit=dev` and `dotnet list GF3.WebApi/WebApi.csproj package --vulnerable --include-transitive` found zero current advisories in the queried scopes. The unfiltered npm install reported **24 advisories** in the development dependency scope. |
| Production Docker | Local Release publish passed and contains the font, licenses, native binaries and frontend. `docker build --pull=false --progress=plain -t gf3-plan78:local .` and direct SDK image pull failed resolving MCR manifests with EOF. The first failure was on `dotnet/sdk:10.0`; the retry failed on `dotnet/aspnet:10.0`. The production image and local HTTP(S) ingress rehearsal remain unverified. |

## Browser matrix and release gates

| Scope | Chromium | Mobile Chrome | WebKit laptop | iPhone WebKit | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| PWA, Guide and ordered communication media on the production frontend preview | 6 passed | 6 passed | 6 passed | 6 passed | **24 passed, 0 failed, 0 skipped** |
| Exact Vite-backed hermetic CI configuration | 6 passed | Outside scope | Outside scope | 6 passed | **12 passed, 0 failed, 0 skipped** |
| Complete 44-file Playwright suite | Pending | Pending | Pending | Incomplete | **60 of 1,968 cases started, 22 failures logged** before stopping. No reliable final pass/skip totals from the interrupted runner. |

The first complete-suite attempt used Vite dev as required by the plan. Production-update tests require `/version.json` and a production frontend bundle. The second attempt used the compiled frontend at loopback port 4173 and the same isolated Development API. It also exposed stale browser fixtures that left the employee installation invite open and expected outdated Daily/related assignment labels. Dedicated PWA tests retain the prompt behavior. Following fixture corrections, Daily schedule and notification cases passed for Chromium and iPhone WebKit. The employee-to-manager correction case passed in Chromium and in an isolated iPhone touch rerun.

The real-account fixture repeatedly signs in with the same manager username. The running disposable API returned HTTP 429 with `Retry-After: 559` during the broad run. It enforces a per-account authentication limit even on successful sign-ins. The limiter was left active. Release-wide browser sign-off still requires a runner arrangement that avoids saturating the shared manager account, plus a clean run of all four projects. Compare fixture failures, timing failures and verified product defects before reusing partial counts.

One separate PDF rerun was stopped after discovering that the restated connection override was ignored and the local development SQLite database received test traffic. Its results were discarded. The following run used the verified `ConnectionStrings:Default` override with a confirmed temporary SQLite file. Production data was outside these runs.

**Open release gates** are GSAP and `@gsap/react`, final native and asset attribution, a completed full browser run, production image and ingress validation, protected SQLite maintenance, coordinated media restore and device verification. The physical device checklist covers iOS Safari and Android Chrome installation, cold start, Guide, rotation, Back/Close, photo and GIF playback, and reduced motion. The draft PR is held under the user's updated instructions. Merge and deploy are blocked.

## BLOCKER — PRODUCTION MAINTENANCE

The protected preflight was exercised locally with a read-only Git adapter using `migration_plan(git, before, target, [migration])` for each blocked ID. The baseline and target were the pinned SHAs above. The exact release migration chain is:

| Migration ID | Schema impact |
| --- | --- |
| `20261122150944_AllowDisjointOpenSwapIntervals` | Replaces the open-swap uniqueness index to include the interval. Protected preflight reports `Maintenance supports generated nullable column migrations`. |
| `20261122151455_TrackEmployeeLaunchModes` | Adds nullable browser and PWA launch timestamps. |
| `20261122160000_AddWorkflowLogLaunchMode` | Adds nullable workflow launch mode. |
| `20261122173000_AddShiftRemovalCorrections` | Adds the operation and removal snapshot, makes requested times nullable and changes constraints. Protected preflight reports `Maintenance migration contains an unsupported operation`. |
| `20261123120000_AddCommunicationGifAttachment` | Adds nullable legacy GIF attachment metadata. |
| `20261123130000_AddCommunicationPhotoAttachment` | Adds nullable legacy photo attachment metadata. |
| `20261123140000_AddCommunicationContentBlocks` | Adds nullable ordered attachment/text block metadata. |

- `DatabaseMigrationStartupTests.cs` passed the disposable EF chain, legacy row retention, immutable source and backup hash checks, `PRAGMA integrity_check`, foreign-key checks, migrations history, model/snapshot equality and index/constraint checks. The separate swap restoration test verified the previous index and applied migration history. These are disposable integration results.
- A separate scratch SQLite database contained one committed row referencing a sentinel in `communication-gifs/`. Both `gf3_release.backup` and `gf3_maintenance.final_backup` produced a valid restored row with `integrity_check=ok`, zero foreign-key errors and the same database SHA-256 `c3a72be26e6a1ef04b31f8286d0ae2ecaf606c9c93c3a36a964b81cc5023f55d`. Both backups omitted the attachment.
- Production attachments live under `/data/communication-gifs`. The Dockerfile assigns `/data` to `app:app`. The live volume's ownership and permissions, coordinated backup of SQLite and attachments, production preflight for the full chain, abort and rollback before the first live write remain unverified. They require the separately reviewed maintenance procedure in Plan 78.
- Physical iOS and Android verification remains open. The browser checks use desktop Chromium, mobile emulation and Windows WebKit.
- **Application QA incomplete.** The ordered migration, swap, correction, schedule/PDF, PWA/guide, communications/media, and cross-module verification steps remain pending. There is no candidate SHA or candidate CI evidence.

## Diff inventory

### Swaps (25)

- A `DataAccessLayer/Migrations/20261122150944_AllowDisjointOpenSwapIntervals.Designer.cs`
- A `DataAccessLayer/Migrations/20261122150944_AllowDisjointOpenSwapIntervals.cs`
- M `FrontEnd/e2e/swap-exchange-workflow.spec.ts`
- A `FrontEnd/public/guide/swaps/swap-exchange.webp`
- M `FrontEnd/src/entities/shift-swaps/api/queries.ts`
- M `FrontEnd/src/entities/shift-swaps/model/types.ts`
- A `FrontEnd/src/pages/employee-install/model/shiftSwapGuide.ts`
- A `FrontEnd/src/pages/employee-swap/model/swapReservedWindows.test.ts`
- A `FrontEnd/src/pages/employee-swap/model/swapReservedWindows.ts`
- M `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.test.tsx`
- M `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.tsx`
- M `FrontEnd/src/pages/employee-swap/ui/EmploymentSwapApproval.test.tsx`
- M `FrontEnd/src/pages/employee-swap/ui/SwapCreateWizard.module.css`
- M `FrontEnd/src/pages/employee-swap/ui/SwapCreateWizard.test.tsx`
- M `FrontEnd/src/pages/employee-swap/ui/SwapCreateWizard.tsx`
- M `FrontEnd/src/pages/employee-swap/ui/SwapShiftPickerDialog.module.css`
- M `FrontEnd/src/pages/employee-swap/ui/SwapShiftPickerDialog.tsx`
- A `GF3.Tests/ShiftSwapDisjointReservationsTests.cs`
- M `GF3.Tests/ShiftSwapExchangeTests.cs`
- M `GF3.Tests/ShiftSwapLifecycleMatrixTests.cs`
- M `GF3.Tests/ShiftSwapSafetyTests.cs`
- M `GF3.WebApi/ShiftSwaps/ShiftSwapExchangeRules.cs`
- M `GF3.WebApi/ShiftSwaps/ShiftSwapExecutionService.cs`
- A `GF3.WebApi/ShiftSwaps/ShiftSwapOpenOfferRebinder.cs`
- A `GF3.WebApi/ShiftSwaps/ShiftSwapReservationRules.cs`

### Shift corrections (16)

- A `DataAccessLayer/Migrations/20261122173000_AddShiftRemovalCorrections.Designer.cs`
- A `DataAccessLayer/Migrations/20261122173000_AddShiftRemovalCorrections.cs`
- A `DataAccessLayer/Models/Enums/ShiftCorrectionOperation.cs`
- M `DataAccessLayer/Models/ShiftCorrectionRequestModel.cs`
- M `FrontEnd/src/entities/containers/ui/ContainerGraphShiftCorrectionsCard.tsx`
- A `FrontEnd/src/entities/shift-corrections/model/presentation.ts`
- M `FrontEnd/src/entities/shift-corrections/model/types.ts`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.module.css`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.tsx`
- A `FrontEnd/src/pages/employee-schedule/ui/PendingCorrectionBadge.module.css`
- A `FrontEnd/src/pages/employee-schedule/ui/PendingCorrectionBadge.tsx`
- A `FrontEnd/src/pages/employee-schedule/ui/pendingScheduleCorrections.ts`
- M `GF3.WebApi/Contracts/ShiftCorrections/CreateShiftCorrectionRequest.cs`
- M `GF3.WebApi/Contracts/ShiftCorrections/ShiftCorrectionRequestDto.cs`
- M `GF3.WebApi/Controllers/ShiftCorrectionsController.cs`
- A `release-assets/2026-11-23/gifs/03-shift-correction.gif`

### Employee and public schedules, PDF (19)

- M `BusinessLogicLayer/Services/Abstractions/IGraphTemplateExportService.cs`
- M `BusinessLogicLayer/Services/GraphTemplateExportService.cs`
- M `FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.module.css`
- M `FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx`
- M `FrontEnd/src/entities/employee-schedule/api/employeeScheduleApi.ts`
- M `FrontEnd/src/entities/employee-schedule/model/types.ts`
- A `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleDailyBody.tsx`
- A `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleHeaderActions.tsx`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.module.css`
- M `FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`
- A `FrontEnd/src/pages/employee-schedule/ui/employeeScheduleMatrix.ts`
- A `FrontEnd/src/pages/employee-schedule/ui/rankEmployeeSchedules.ts`
- A `FrontEnd/src/pages/employee-schedule/ui/scheduleShiftStatuses.ts`
- A `FrontEnd/src/pages/employee-schedule/ui/useSchedulePdfExport.ts`
- A `GF3.WebApi/Contracts/EmployeeSchedules/PublicScheduleSummaryDto.cs`
- M `GF3.WebApi/Controllers/EmployeeSchedulePdfController.cs`
- M `GF3.WebApi/Controllers/EmployeeSchedulesController.cs`

### PWA and launch (40)

- A `DataAccessLayer/Migrations/20261122151455_TrackEmployeeLaunchModes.Designer.cs`
- A `DataAccessLayer/Migrations/20261122151455_TrackEmployeeLaunchModes.cs`
- A `DataAccessLayer/Migrations/20261122160000_AddWorkflowLogLaunchMode.Designer.cs`
- A `DataAccessLayer/Migrations/20261122160000_AddWorkflowLogLaunchMode.cs`
- A `FrontEnd/e2e/pwa-launch.spec.ts`
- M `FrontEnd/index.html`
- A `FrontEnd/public/guide/getting-started/login-pin-launch.webp`
- A `FrontEnd/public/icons/gf-icon-maskable-512.png`
- M `FrontEnd/public/manifest.json`
- A `FrontEnd/public/splash/ios-1125x2436.png`
- A `FrontEnd/public/splash/ios-1170x2532.png`
- A `FrontEnd/public/splash/ios-1179x2556.png`
- A `FrontEnd/public/splash/ios-1206x2622.png`
- A `FrontEnd/public/splash/ios-1242x2688.png`
- A `FrontEnd/public/splash/ios-1284x2778.png`
- A `FrontEnd/public/splash/ios-1290x2796.png`
- A `FrontEnd/public/splash/ios-1320x2868.png`
- A `FrontEnd/public/splash/ios-640x1136.png`
- A `FrontEnd/public/splash/ios-750x1334.png`
- A `FrontEnd/public/splash/ios-828x1792.png`
- A `FrontEnd/scripts/generate-ios-startup.py`
- A `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeePwaInstallDialog.module.css`
- A `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeePwaInstallDialog.test.tsx`
- A `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeePwaInstallDialog.tsx`
- A `FrontEnd/src/app/providers/EmployeeLaunchReporter.test.tsx`
- A `FrontEnd/src/app/providers/EmployeeLaunchReporter.tsx`
- A `FrontEnd/src/app/router/AppReadySignal.tsx`
- A `FrontEnd/src/entities/employee-launches/api/employeeLaunchesApi.ts`
- A `FrontEnd/src/entities/employee-launches/api/queries.ts`
- A `FrontEnd/src/entities/employee-launches/model/types.ts`
- A `FrontEnd/src/shared/lib/pwaDisplayMode.test.ts`
- A `FrontEnd/src/shared/lib/pwaDisplayMode.ts`
- A `FrontEnd/src/shared/lib/pwaSplash.test.ts`
- A `FrontEnd/src/shared/lib/pwaSplash.ts`
- M `FrontEnd/src/test/app-icons.test.ts`
- A `GF3.Tests/EmployeeLaunchUsageTests.cs`
- A `GF3.WebApi/Contracts/EmployeeLaunches/EmployeeLaunchUsageDto.cs`
- A `GF3.WebApi/Contracts/EmployeeLaunches/RecordEmployeeLaunchRequest.cs`
- A `GF3.WebApi/Controllers/EmployeeLaunchesController.cs`
- A `GF3.WebApi/Services/EmployeeLaunchService.cs`

### App Guide and assets (206)

- A `FrontEnd/public/guide/README.md`
- A `FrontEnd/public/guide/availability-urlop/availability-calendar.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-custom.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-day.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-edit-presets.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-editor.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-multiple.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-nav.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-period.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-presets.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-restrictions.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-save.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-saved.webp`
- A `FrontEnd/public/guide/availability-urlop/availability-urlop.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-colleagues.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-date.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-month-open.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-month.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-panel.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-selected.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-shops-open.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-status.webp`
- A `FrontEnd/public/guide/availability-urlop/urlop-submit.webp`
- A `FrontEnd/public/guide/corrections/adjust-add-shift.webp`
- A `FrontEnd/public/guide/corrections/adjust-comment.webp`
- A `FrontEnd/public/guide/corrections/adjust-day-off.webp`
- A `FrontEnd/public/guide/corrections/adjust-review.webp`
- A `FrontEnd/public/guide/corrections/adjust-schedule.webp`
- A `FrontEnd/public/guide/corrections/adjust-send.webp`
- A `FrontEnd/public/guide/corrections/adjust-shift.webp`
- A `FrontEnd/public/guide/corrections/adjust-success.webp`
- A `FrontEnd/public/guide/corrections/adjust-time.webp`
- A `FrontEnd/public/guide/corrections/adjust.webp`
- A `FrontEnd/public/guide/flags/poland.svg`
- A `FrontEnd/public/guide/flags/ukraine.svg`
- A `FrontEnd/public/guide/flags/united-kingdom.svg`
- A `FrontEnd/public/guide/getting-started/android-add-to-home-screen.webp`
- A `FrontEnd/public/guide/getting-started/android-choose-install.webp`
- A `FrontEnd/public/guide/getting-started/android-chrome-menu.webp`
- A `FrontEnd/public/guide/getting-started/android-chrome-open.webp`
- A `FrontEnd/public/guide/getting-started/android-confirm-install.webp`
- A `FrontEnd/public/guide/getting-started/ios-home-screen.webp`
- A `FrontEnd/public/guide/getting-started/login-modes.webp`
- A `FrontEnd/public/guide/getting-started/login-password.webp`
- A `FrontEnd/public/guide/getting-started/login-pin-keypad.webp`
- A `FrontEnd/public/guide/getting-started/login-submit.webp`
- A `FrontEnd/public/guide/getting-started/login-workspace.webp`
- A `FrontEnd/public/guide/getting-started/login.webp`
- A `FrontEnd/public/guide/getting-started/safari-gf-login.webp`
- A `FrontEnd/public/guide/getting-started/safari-home-screen-confirm.webp`
- A `FrontEnd/public/guide/getting-started/safari-share-actions.webp`
- A `FrontEnd/public/guide/getting-started/safari-share-button.webp`
- A `FrontEnd/public/guide/getting-started/safari-share-sheet.webp`
- A `FrontEnd/public/guide/notifications/alerts-all-read.webp`
- A `FrontEnd/public/guide/notifications/alerts-example-accepted.webp`
- A `FrontEnd/public/guide/notifications/alerts-example-availability.webp`
- A `FrontEnd/public/guide/notifications/alerts-example-open-shift.webp`
- A `FrontEnd/public/guide/notifications/alerts-example-schedule.webp`
- A `FrontEnd/public/guide/notifications/alerts-example-swap.webp`
- A `FrontEnd/public/guide/notifications/alerts-link.webp`
- A `FrontEnd/public/guide/notifications/alerts-news-read.webp`
- A `FrontEnd/public/guide/notifications/alerts-news.webp`
- A `FrontEnd/public/guide/notifications/alerts-read.webp`
- A `FrontEnd/public/guide/notifications/alerts-tabs.webp`
- A `FrontEnd/public/guide/notifications/alerts-unread.webp`
- A `FrontEnd/public/guide/notifications/nav-alerts.webp`
- A `FrontEnd/public/guide/profile/nav-profile.webp`
- A `FrontEnd/public/guide/profile/profile-code.webp`
- A `FrontEnd/public/guide/profile/profile-edit.webp`
- A `FrontEnd/public/guide/profile/profile-email.webp`
- A `FrontEnd/public/guide/profile/profile-guide.webp`
- A `FrontEnd/public/guide/profile/profile-language-open.webp`
- A `FrontEnd/public/guide/profile/profile-language.webp`
- A `FrontEnd/public/guide/profile/profile-logout.webp`
- A `FrontEnd/public/guide/profile/profile-new-password.webp`
- A `FrontEnd/public/guide/profile/profile-overview.webp`
- A `FrontEnd/public/guide/profile/profile-password-confirm.webp`
- A `FrontEnd/public/guide/profile/profile-password-fields.webp`
- A `FrontEnd/public/guide/profile/profile-password.webp`
- A `FrontEnd/public/guide/profile/profile-save.webp`
- A `FrontEnd/public/guide/profile/profile-verification.webp`
- A `FrontEnd/public/guide/schedules/availability-day.webp`
- A `FrontEnd/public/guide/schedules/availability-editor.webp`
- A `FrontEnd/public/guide/schedules/availability-period.webp`
- A `FrontEnd/public/guide/schedules/availability-urlop.webp`
- A `FrontEnd/public/guide/schedules/nav-schedule.webp`
- A `FrontEnd/public/guide/schedules/schedule-complete-bubble.webp`
- A `FrontEnd/public/guide/schedules/schedule-complete.webp`
- A `FrontEnd/public/guide/schedules/schedule-correction-bubble.webp`
- A `FrontEnd/public/guide/schedules/schedule-correction.webp`
- A `FrontEnd/public/guide/schedules/schedule-daily.webp`
- A `FrontEnd/public/guide/schedules/schedule-day-off.webp`
- A `FrontEnd/public/guide/schedules/schedule-eye.webp`
- A `FrontEnd/public/guide/schedules/schedule-matrix.webp`
- A `FrontEnd/public/guide/schedules/schedule-more.webp`
- A `FrontEnd/public/guide/schedules/schedule-open-bubble.webp`
- A `FrontEnd/public/guide/schedules/schedule-open.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-apply.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-dialog.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-drag.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-handle.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-header.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-reset-apply.webp`
- A `FrontEnd/public/guide/schedules/schedule-order-reset.webp`
- A `FrontEnd/public/guide/schedules/schedule-overview.webp`
- A `FrontEnd/public/guide/schedules/schedule-pdf.webp`
- A `FrontEnd/public/guide/schedules/schedule-picker.webp`
- A `FrontEnd/public/guide/schedules/schedule-preview.webp`
- A `FrontEnd/public/guide/schedules/schedule-public.webp`
- A `FrontEnd/public/guide/schedules/schedule-selector.webp`
- A `FrontEnd/public/guide/schedules/urlop-date.webp`
- A `FrontEnd/public/guide/schedules/urlop-history.webp`
- A `FrontEnd/public/guide/schedules/urlop-panel.webp`
- A `FrontEnd/public/guide/schedules/urlop-submit.webp`
- A `FrontEnd/public/guide/swaps/nav-swap.webp`
- A `FrontEnd/public/guide/swaps/swap-accept.webp`
- A `FrontEnd/public/guide/swaps/swap-claim-window.webp`
- A `FrontEnd/public/guide/swaps/swap-confirm.webp`
- A `FrontEnd/public/guide/swaps/swap-daily-completed-bubble.webp`
- A `FrontEnd/public/guide/swaps/swap-daily-completed.webp`
- A `FrontEnd/public/guide/swaps/swap-daily-pending-bubble.webp`
- A `FrontEnd/public/guide/swaps/swap-daily-pending.webp`
- A `FrontEnd/public/guide/swaps/swap-everyone.webp`
- A `FrontEnd/public/guide/swaps/swap-exception-example.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-actions.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-close.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-confirm.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-details.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-accepted.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-approval.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-finished.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-held.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-history.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-locked.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-mine.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-private.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-labels-public.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-list.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-open-card.webp`
- A `FrontEnd/public/guide/swaps/swap-guide-unavailable.webp`
- A `FrontEnd/public/guide/swaps/swap-history-button.webp`
- A `FrontEnd/public/guide/swaps/swap-history.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-accept-full.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-accepted.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-approval-action.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-approval-history.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-cancel.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-cancelled.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-claim.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-confirm-full.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-confirm-part.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-conflict.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-copy.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-exact.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-exchange-review.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-filters.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-first-open.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-flexible.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-full-review.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-full-success.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-full.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-give.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-half-review.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-held.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-incoming.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-reserved.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-residual.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-return-action.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-return-schedule.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-return-shift.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-search.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-second-time.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-shift.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-specific-review.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-time.webp`
- A `FrontEnd/public/guide/swaps/swap-manual-two.webp`
- A `FrontEnd/public/guide/swaps/swap-offer.webp`
- A `FrontEnd/public/guide/swaps/swap-page.webp`
- A `FrontEnd/public/guide/swaps/swap-partial.webp`
- A `FrontEnd/public/guide/swaps/swap-review.webp`
- A `FrontEnd/public/guide/swaps/swap-rule-example.webp`
- A `FrontEnd/public/guide/swaps/swap-schedule.webp`
- A `FrontEnd/public/guide/swaps/swap-send.webp`
- A `FrontEnd/public/guide/swaps/swap-shift.webp`
- A `FrontEnd/public/guide/swaps/swap-specific.webp`
- A `FrontEnd/public/guide/swaps/swap-success.webp`
- A `FrontEnd/public/guide/work-hours/hours-columns.webp`
- A `FrontEnd/public/guide/work-hours/hours-location.webp`
- A `FrontEnd/public/guide/work-hours/hours-month-open.webp`
- A `FrontEnd/public/guide/work-hours/hours-month.webp`
- A `FrontEnd/public/guide/work-hours/hours-period-controls.webp`
- A `FrontEnd/public/guide/work-hours/hours-read-total.webp`
- A `FrontEnd/public/guide/work-hours/hours-total.webp`
- A `FrontEnd/public/guide/work-hours/hours-year-open.webp`
- A `FrontEnd/public/guide/work-hours/hours-year.webp`
- A `FrontEnd/scripts/capture-app-guide.mjs`
- A `FrontEnd/src/pages/employee-install/index.ts`
- A `FrontEnd/src/pages/employee-install/model/guideCaptures.json`
- A `FrontEnd/src/pages/employee-install/model/guideContent.ts`
- A `FrontEnd/src/pages/employee-install/model/guideInstructions.ts`
- A `FrontEnd/src/pages/employee-install/model/guideLanguage.ts`
- A `FrontEnd/src/pages/employee-install/ui/EmployeeInstallPage.module.css`
- A `FrontEnd/src/pages/employee-install/ui/EmployeeInstallPage.tsx`
- A `FrontEnd/src/pages/employee-install/ui/GuideInterfaceIllustration.tsx`
- A `FrontEnd/src/pages/employee-install/ui/GuideLanguageDialog.tsx`
- A `FrontEnd/src/pages/employee-install/ui/GuideVisual.tsx`

### Communications and media (36)

- A `BusinessLogicLayer/Contracts/Communications/CommunicationContentBlock.cs`
- A `BusinessLogicLayer/Contracts/Communications/CommunicationGifAttachment.cs`
- M `BusinessLogicLayer/Contracts/Communications/CommunicationMessageDto.cs`
- M `BusinessLogicLayer/Contracts/Communications/CreateCommunicationMessageRequest.cs`
- M `BusinessLogicLayer/Contracts/Communications/UpdateCommunicationMessageRequest.cs`
- M `BusinessLogicLayer/Services/Abstractions/ICommunicationService.cs`
- M `BusinessLogicLayer/Services/CommunicationService.cs`
- A `DataAccessLayer/Migrations/20261123120000_AddCommunicationGifAttachment.Designer.cs`
- A `DataAccessLayer/Migrations/20261123120000_AddCommunicationGifAttachment.cs`
- A `DataAccessLayer/Migrations/20261123130000_AddCommunicationPhotoAttachment.Designer.cs`
- A `DataAccessLayer/Migrations/20261123130000_AddCommunicationPhotoAttachment.cs`
- A `DataAccessLayer/Migrations/20261123140000_AddCommunicationContentBlocks.Designer.cs`
- A `DataAccessLayer/Migrations/20261123140000_AddCommunicationContentBlocks.cs`
- M `DataAccessLayer/Models/CommunicationMessageModel.cs`
- M `DataAccessLayer/Repositories/Abstractions/ICommunicationRepository.cs`
- M `DataAccessLayer/Repositories/CommunicationRepository.cs`
- M `FrontEnd/src/entities/communications/api/communicationsApi.ts`
- M `FrontEnd/src/entities/communications/api/dto.ts`
- M `FrontEnd/src/entities/communications/api/queries.ts`
- M `FrontEnd/src/entities/communications/index.ts`
- A `FrontEnd/src/entities/communications/model/contentBlocks.ts`
- A `FrontEnd/src/entities/communications/ui/CommunicationGif.module.css`
- A `FrontEnd/src/entities/communications/ui/CommunicationGif.tsx`
- A `FrontEnd/src/entities/communications/ui/CommunicationMessageContent.tsx`
- A `FrontEnd/src/entities/communications/ui/CommunicationPreview.tsx`
- M `FrontEnd/src/entities/communications/ui/EmployeeCommunicationDialog.module.css`
- M `FrontEnd/src/entities/communications/ui/EmployeeCommunicationDialog.tsx`
- A `FrontEnd/src/pages/communications/ui/CommunicationBlockEditor.tsx`
- M `FrontEnd/src/pages/communications/ui/CommunicationsPage.module.css`
- M `FrontEnd/src/pages/communications/ui/CommunicationsPage.tsx`
- A `GF3.Tests/CommunicationGifTests.cs`
- A `GF3.WebApi/Contracts/Communications/CommunicationGifForm.cs`
- M `GF3.WebApi/Contracts/Communications/CommunicationMessageDto.cs`
- M `GF3.WebApi/Contracts/Communications/UpdateCommunicationMessageRequest.cs`
- M `GF3.WebApi/Controllers/CommunicationsController.cs`
- A `GF3.WebApi/Services/CommunicationGifStorage.cs`

### Cross-cutting (57)

- M `DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs`
- M `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`
- M `DataAccessLayer/Models/EmployeeAccountModel.cs`
- M `DataAccessLayer/Models/WorkflowLogEntryModel.cs`
- M `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`
- M `FrontEnd/src/app/providers/PresenceProvider.tsx`
- M `FrontEnd/src/app/providers/QueryProvider.tsx`
- M `FrontEnd/src/app/router/AppRouter.test.tsx`
- M `FrontEnd/src/app/router/AppRouter.tsx`
- M `FrontEnd/src/entities/workflow-logs/model/types.ts`
- M `FrontEnd/src/main.tsx`
- M `FrontEnd/src/pages/employee-account/ui/EmployeeAccountPage.module.css`
- M `FrontEnd/src/pages/employee-account/ui/EmployeeAccountPage.tsx`
- M `FrontEnd/src/pages/home/api/managerHomeActivityApi.ts`
- M `FrontEnd/src/pages/home/ui/HomePage.module.css`
- M `FrontEnd/src/pages/home/ui/HomePage.tsx`
- M `FrontEnd/src/pages/home/ui/homeActivity.ts`
- M `FrontEnd/src/pages/information/ui/InformationPage.module.css`
- A `FrontEnd/src/pages/information/ui/InformationPage.test.tsx`
- M `FrontEnd/src/pages/information/ui/InformationPage.tsx`
- M `FrontEnd/src/pages/login/ui/LoginPage.tsx`
- M `FrontEnd/src/shared/api/httpClient.ts`
- M `FrontEnd/src/shared/api/queryKeys.ts`
- M `FrontEnd/src/shared/i18n/index.ts`
- M `FrontEnd/src/shared/i18n/pl.json`
- M `FrontEnd/src/shared/i18n/serverMessages.json`
- M `FrontEnd/src/shared/i18n/uk.json`
- M `FrontEnd/src/shared/lib/tanstack/react-query.test.tsx`
- M `FrontEnd/src/shared/lib/tanstack/react-query.tsx`
- A `FrontEnd/src/shared/ui/icons/BookOpenIcon.tsx`
- A `FrontEnd/src/shared/ui/icons/MonitorIcon.tsx`
- A `FrontEnd/src/shared/ui/icons/SmartphoneIcon.tsx`
- M `FrontEnd/src/shared/ui/icons/index.ts`
- M `GF3.Tests/DatabaseMigrationStartupTests.cs`
- M `GF3.WebApi/Contracts/ManagerHome/ManagerHomeActivityDtos.cs`
- M `GF3.WebApi/Contracts/WorkflowLogs/WorkflowLogDto.cs`
- M `GF3.WebApi/Controllers/WorkflowLogsController.cs`
- M `GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs`
- M `GF3.WebApi/Realtime/EmployeePresenceHub.cs`
- M `GF3.WebApi/Realtime/RealtimeNotifier.cs`
- M `GF3.WebApi/Services/ManagerHomeActivityReader.cs`
- M `GF3.WebApi/Services/WorkflowLogService.cs`
- M `GF3.WebApi/WebApi.csproj`
- A `release-assets/2026-11-23/README.md`
- A `release-assets/2026-11-23/announcements/features-en.md`
- A `release-assets/2026-11-23/announcements/features-pl.md`
- A `release-assets/2026-11-23/announcements/features-uk.md`
- A `release-assets/2026-11-23/announcements/main-en.md`
- A `release-assets/2026-11-23/announcements/main-pl.md`
- A `release-assets/2026-11-23/announcements/main-uk.md`
- A `release-assets/2026-11-23/gifs/01-schedule-selection.gif`
- A `release-assets/2026-11-23/gifs/02-schedule-views.gif`
- A `release-assets/2026-11-23/gifs/04-request-day-off.gif`
- A `release-assets/2026-11-23/gifs/05-partial-shift-swap.gif`
- A `release-assets/2026-11-23/gifs/06-swap-status.gif`
- A `release-assets/2026-11-23/gifs/07-work-hours.gif`
- A `release-assets/2026-11-23/gifs/08-app-guide.gif`
