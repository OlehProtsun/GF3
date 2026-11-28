# GF3 release QA · 2026-11-26

**Local qualification passed. Production readiness remains open.** The exact PR and PROD2 CI gates, independent review and authorized host checks are required before deployment.

## Baseline and environment

- Development `dev26.11.2026` started at `42fb0fa710a5bccaba9f4502ec6a6dd9981c521c`. PROD2 was `2eedc3618f502c444bd762263e0bb389fdee3526` and is an ancestor of development. Both remote tips still matched on November 28.
- Local verification used an isolated clean checkout of the pinned development revision and the working changes listed below. The original worktree's existing notepad controller test edits were retained there and incorporated into the candidate tests.
- The initial full-run source snapshot is recorded in `GF3.Tests/TestResults/release-2026-11-26/candidate-source-blobs.txt` with SHA-256 `8697c5db2b32cdd77eb67b0728a6191ae0d226865f2cbd6a6c9f8e675ccb6fd7`. Later browser assertion repairs are listed below. The QA record is outside this source snapshot.
- The corrected source snapshot is recorded in `GF3.Tests/TestResults/release-2026-11-26/repaired-source-blobs.txt` with SHA-256 `0ca2f6b4ab0b5e8145c61486347bec0507e5503310e395ee4503108b380534b1`.
- The source snapshot for the four fresh browser profile runs is recorded in `GF3.Tests/TestResults/release-2026-11-26/qualification-source-blobs.txt` with SHA-256 `f445a4b9ae83bc99d0a4af9afb8d6db30f1513f6dbdc0f33136b7dbe4be617f3`.
- The desktop draft assertion update is recorded in `GF3.Tests/TestResults/release-2026-11-26/qualification-readiness-source-blobs.txt` with SHA-256 `753399093d0b6f551613c78ba01f2bd649268228c0210fad81fcd69045aa57a4`. Its sole difference is in a desktop-only browser test. The corrected WebKit laptop run uses port 5217 and writes `qualification-readiness-webkit-laptop.json`.
- The snapshot for sequential WebKit qualification includes the sidebar case timeout update. It is listed in `GF3.Tests/TestResults/release-2026-11-26/qualification-serial-source-blobs.txt` with SHA-256 `b4931576664bc3f0f62752f90877cb99c2c03760279875b43709a154368f783b`.
- The latest four-profile run uses `GF3.Tests/TestResults/release-2026-11-26/qualification-final-source-blobs.txt` with SHA-256 `4e5575a50a1958a923c636283066fba476a512a3678a414b225d5fd4851c7bb7`.
- After the offer-dialog focus fixture correction, the recorded snapshot is `GF3.Tests/TestResults/release-2026-11-26/qualification-scroll-source-blobs.txt` with SHA-256 `f3bfcd3524f26004ea7663befb2a0f4b16e4f7477e8c733fe2ef902499d4ed0f`. Both remote branch tips still matched the baseline at this point.
- The contract fixture's route tracking correction is the sole source difference in `GF3.Tests/TestResults/release-2026-11-26/qualification-exact-source-blobs.txt`, SHA-256 `94f9545c0e3936d1f126f3d80cc363ec7305093b4e33bd9809798bf5e840dc84`.
- The scroll timing assertion is the sole further source difference in `GF3.Tests/TestResults/release-2026-11-26/qualification-sticky-source-blobs.txt`, SHA-256 `ad7549a32c3ac37d9274b69c8753f94f7810872966ecc3b067ec4f956e353591`.
- The column-order Escape fixture is the sole further source difference in `GF3.Tests/TestResults/release-2026-11-26/qualification-keyboard-source-blobs.txt`, SHA-256 `03478f3c635905b13a14ef44abdda51e5fb1f5753bfb6486de77719b8bddc4d9`.
- The pinned daily delta has 168 paths from `ed04abce13552c57fcbd2828f8c38a40d00a1245`. The PROD2-to-development delta has 160. Evidence under `GF3.Tests/TestResults/release-2026-11-26/` includes both filename inventories and the baseline SHA and worktree status.
- Windows 10 build 19045, .NET SDK 10.0.401, Node 22.14.0, npm 10.9.2, Python 3.10.6, Playwright 1.59.1, Chromium 147.0.7727.15 and WebKit 26.4. Browser persistence scenarios used the repository's isolated backend and disposable synthetic SQLite files.

## Baseline verification

| Command | Result and local evidence |
| --- | --- |
| `dotnet restore GF3.Tests/GF3.Tests.csproj` | Passed. `dotnet-restore.log` |
| Release .NET tests, `Category!=LocalOnly` | 1,929 passed, 31 failed, 0 skipped. `dotnet-baseline.log`, `baseline-dotnet.trx` |
| `python -m unittest discover -s deploy/security/tests -v` | 61 passed, 4 platform skips, 0 failed on the retry that allowed temporary Git subprocesses. `python-baseline-retry.log` |
| `npm ci`, `npm run lint`, `npm run build` | Install passed. ESLint reported 8 errors. TypeScript reported 6 errors before Vite started. `npm-ci.log`, `npm-lint.log`, `npm-build.log` |
| `npm run test` | 1,578 passed, 18 failed, 1 skipped. `npm-test.log` |
| Playwright install, prepare, mocked smoke and three specified baseline specs | Windows Chromium and WebKit installed. Prepare was blocked by TypeScript. Mocked smoke passed 12 tests. The targeted run recorded completed tests and individual failures without a final aggregate. Real isolated API tests from that baseline run lacked a published test backend. `playwright-install.log`, `e2e-prepare.log`, `e2e-smoke.log`, `e2e-targeted.log`, `playwright-focused/5179/` under test results. |

## Candidate verification

| Area | Observed result |
| --- | --- |
| Notepad API/auth and SQLite | 95 focused .NET tests passed, including both immutable migration contracts in assertion mode, real SQLite session contention, revisions, release/expiry, role and workspace authorization, and redacted realtime invalidation. Three migration export and rollback checks passed separately. `step3.trx`, `step3-export.trx` |
| Maintenance and deployment | The final Python run passed 67 tests with four expected Ubuntu advisory-lock skips in `GuardTests`. The Python recovery helper verified all three EF-exported prefixes. The migration verifier checked the completed transition and the exact reviewed source blobs. The local Docker engine was unavailable. The production Docker build, Linux nftables rehearsals and HTTP/HTTPS ingress gates require a disposable CI runner. `candidate-python.log`, `final-prefix-verification-retry.log` |
| Notepad browser | Both desktop engines passed private-note creation, formatted persistence, history restore, pin, lock and delete. The two-tab public lease test passed in both. The phone flows exercise explicit save, viewport and focus geometry, failed writes, and clipboard code paths on both mobile profiles. Native keyboard and clipboard behavior still requires device evidence. `step6-desktop-rerun.log`, `step6-desktop-pin.log`, `candidate-e2e-focused.log` |
| Employee public schedules and corrections | All 553 focused .NET tests passed across public schedules, exports, corrections, swaps, contract approvals, Home and realtime. Browser tests passed on Chromium and WebKit laptop for a nonmember who requests a public assignment, remains unenrolled while pending and joins the schedule after Home approval. Direct selector tests cover catalog and preview retry using the exact ID, unavailable previews and scroll/focus restoration. `step7-8.trx`, `candidate-e2e-focused.log`, `step7-8-dialogs.log` |
| Home, workspace, and query cache | Six focused Home review tests passed, including lock, pending, exact 404 and conflict handling. A separate 58-test run passed across presence, Phone Home, and the local query client, including coalescing and keeping confirmed data until the fresh response settles. Both desktop browser engines passed explicit Home approval and deleted request navigation. `step8-home-retry.log`, `step8-regressions.log`, `candidate-e2e-focused.log` |
| Graph drafts, matrix and layout | Both desktop browser engines passed the isolated three-graph save/reopen scenario and the matrix actions test. The latter verified actual control clicks and draft persistence after republishing the fixed CSS. The targeted graph/workspace run passed 30 cases with four profile skips and the two matrix failures that cleared on republish. `step8-focused-browser.log`, `step8-matrix-published.log`, `playwright-focused/5188/` and `playwright-focused/5193/` under test results. |
| Production frontend and isolated API | Final `npm ci` and Windows browser installation passed. After the browser fixes, ESLint passed with 0 errors and 23 warnings, Vitest passed 1,626 tests across 170 files with one private-fixture skip, and production build and isolated Release publish passed. `final-npm-ci.log`, `repaired-lint.log`, `repaired-test.log`, `repaired-build.log`, `repaired-e2e-prepare.log` |
| Browser smoke and discovery | Release API publish and the mocked Playwright smoke passed, with 12 tests across Chromium and WebKit iPhone X. The normal configuration discovered 2,080 tests across 47 files and four profiles. The initial run recorded 449 passes, 16 failures and 30 skips. The replacement run recorded 902 passes, eight failures and 37 skips, including six cases interrupted by Windows sleep. The latest full run executes `npm run test:e2e -- --project=<profile> --workers=2 --reporter=list,json` for each configured profile in sequence, with distinct loopback ports and reports. `repaired-e2e-prepare.log`, `repaired-smoke.log`, `candidate-e2e-list.log`, `initial-e2e-partial.json`, `suspended-run-partial.json` |
| .NET LocalOnly | 405 passed, 0 failed and 0 skipped. Launcher tests use temporary files and reflection. Both generation suites use synthetic fixed seeds. `candidate-localonly.trx` |
| Full .NET Release | 1,986 passed, 0 failed and 0 skipped under `Category!=LocalOnly`. Restore and test discovery passed. Both schema contract export variables were unset, so the suite asserted the committed immutable contracts and exported only synthetic database/media artifacts. `candidate-dotnet-restore.log`, `candidate-dotnet.trx`, `final-dotnet-list.log` |
| Opt-in manual-shift real browser | With `GF3_RUN_LIVE_BROWSER=1`, the existing `ManualShiftHighlightTests.RealHttp_CrossRoleSettingAndAcceptance_PersistBothCells` passed against synthetic SQLite and its loopback backend. Its browser case passed in all four profiles. The normal Playwright reports count this opt-in case among each profile's skips. `qualification-manual-shift-live.log`, `GF3.Tests/TestResults/manual-live-browser.log` |
| Earlier Chromium browser results | Desktop Chromium passed 514 of 520 cases with six intentional skips and zero failures before the final assertion corrections. Mobile Chrome passed 491 of 520 cases with 29 intentional skips and zero failures. The later sidebar deadline was also verified in both profiles by the focused test. `qualification-chromium.json`, `qualification-readiness-chromium.json`, `qualification-mobile-chrome.json`, `qualification-sidebar-deadline.log` |
| Earlier WebKit laptop | 513 passed, seven intentional profile skips and zero failures. The full run includes both high-volume graph saves and the corrected contract workflow. `qualification-final-webkit-laptop.json` |
| WebKit iPhone X before the offer-dialog fixture correction | 489 passed, 30 intentional profile skips and one failure. The failed case subsequently passed across all four profiles with the corrected focus setup. `qualification-final-webkit-iphone-x.json`, `qualification-swap-scroll.log` |
| Chromium before the route and summary scroll fixture corrections | 512 passed, six intentional profile skips and two failures. Both failures came from test synchronization. The following Mobile Chrome run was stopped before completion to run the latest snapshot after the focused checks. `qualification-final-chromium.json`, `qualification-final-mobile-chrome.log` |
| Chromium after the summary scroll correction | 514 passed, six intentional profile skips and zero failures in 21.9 minutes. `qualification-sticky-chromium.json` |
| Interrupted WebKit iPhone X after the summary scroll correction | Windows slept at 22:27:51 Warsaw time on November 27 and woke at 10:32:21 on November 28. The stopped run recorded 125 passes, 25 skips and two failures, including a 12.1-hour case duration. The source snapshot was reverified and the iPhone X profile restarted on a new port. The completed Chromium result was recorded before sleep. `qualification-sticky-webkit-iphone-x.log` |
| WebKit iPhone X on the final source snapshot | 490 passed, 30 intentional profile skips and zero failures in 44.9 minutes. The run includes the corrected contract, Escape, offer-dialog and mobile summary checks. `qualification-keyboard-webkit-iphone-x.json` |
| WebKit laptop on the final source snapshot | 513 passed, seven intentional profile skips and zero failures in 41.2 minutes. `qualification-keyboard-webkit-laptop.json` |
| Mobile Chrome on the final source snapshot | 491 passed, 29 intentional profile skips and zero failures in 24.1 minutes. `qualification-keyboard-mobile-chrome.json` |
| Chromium on the final source snapshot | 514 passed, six intentional profile skips and zero failures in 23.2 minutes. `qualification-keyboard-chromium.json` |
| Final Playwright aggregate | 2,008 passed, 72 intentional profile skips, zero failures and zero flaky outcomes across 2,080 discovered cases. The four full JSON reports match the same source snapshot. |

The Playwright skips cover PC-only editor, review and graph stress scenarios, touch-only viewport and Phone notepad scenarios, the Chromium-only service worker check and the separately executed manual-shift browser fixture. Each project reports exactly 520 cases. The preceding interrupted and failing runs are retained as investigation evidence.

### Repairs and evidence

- The standalone lease migration's original `Down` crossed the SQLite provider's unsupported standalone `DropColumnOperation` path. Its four operations now use SQLite drop statements in the same order. `Up` and the preceding migration's Git blob stayed unchanged. The pinned source blobs are `bd8834f57687335fa424f18bac750dfff2bcba9e` and `dc7c6d4ef1b4020b0b519fb95cf8fcd4e84f7529`. Historical migration files and the historical fixture remained unedited. The two reviewed paths preserve rows, history, sequence and media, with separate immutable SQLite schema contracts.
- `ManagerNotepadControllerTests`, `EmployeePublicSchedulesTests`, `EmployeeSchedulePdfControllerTests` and migration summary tests contained stale public editing, publication/export and migration count assertions. `dotnet-baseline.log` records all 31 original failures. `step7-preview-retry.log` captures three additional reproductions for presence writes on public list, detail and PDF requests and one duplicate synthetic shop name. The middleware now leaves public discovery read-only while preserving ordinary presence tracking. The host test uses a unique synthetic shop.
- The frontend compile and lint failures covered obsolete query/router methods, runtime clipboard capability detection, a hidden error and React render-time ref reads. Focused tests caught a dropped lease misclassified as unavailable and a stale save acknowledgement on a changed selection. The frontend preserves dirty text and confirmed cache data, including a detached PC notepad callback after a workspace switch. Literal and reviewed finite translations have Polish and Ukrainian test coverage.
- Component/browser baseline assertions used obsolete labels, placeholder Phone Home, wrong scroll wrappers and old matrix actions. Updated assertions exercise the implemented Home dashboard, real selection paths, focused review and delete confirmation. The desktop notepad rerun verifies the server's lock and pin state via authenticated test reads.
- The matrix wrapper moved the swap comparison styles off the scroll viewport. Corrected the selectors and verified desktop paired-table scrolling, narrow dialog scrolling and exchange switching in all four browser profiles. The pinned-column browser case also reproduced an adjacent header covering the left-side add button. The matrix fix gives that control priority at the header boundary. `step8-swap-scroll.log`, `step8-focused-browser.log`
- The first full browser run found stale employee empty-state assertions, a missing correction catalog mock and an obsolete scroll wrapper in the column-size check. The corrected employee route, mobile keyboard/layout and column-size runs passed 4, 10 and 4 cases respectively. `final-e2e.log`, `final-employee-routing-retry.log`, `final-employee-phone-retry.log`, `final-employee-column-order-retry.log`
- Browser assertion repairs also use the notepad's dialog role, the phone profile heading and visible tooltip anchors within the matrix's nested scroll areas. The corrected viewport and tooltip checks each passed on all four profiles. The corrected phone workspace case passed at six widths in WebKit iPhone X. `final-viewport-retry.log`, `final-tooltip-workspace.log`, `final-tooltip-visible-anchors.log`
- A WebKit vacation save completed before its invoking button became focusable again. Focus now returns after the editor closes and the write settles. The existing dirty/failure scenario and month navigation passed in WebKit iPhone X. All 29 manager vacation unit tests passed. `final-tooltip-urlop-retry.log`, `final-urlop-monthly-retry.log`, `final-urlop-unit.log`
- The contract workflow timed out near its last actions at the three-minute limit in both the replacement run and a focused retry. Its assertions were retained and the case budget increased to four minutes. The focused WebKit iPhone X retry passed. `repaired-e2e.log`, `repaired-contract-retry.log`, `repaired-contract-deadline.log`
- Windows recorded sleep at 09:32:02 Warsaw time and a wake diagnostic at 18:32:08 on November 27. Six in-flight browser cases subsequently reported nine-hour timeouts. These interrupted results cannot qualify the release. After wake, both remote branches still matched their pinned SHAs. `repaired-e2e.log`, `playwright/5205/` under test results.
- Focused post-wake checks reproduced browser test timing issues. A fixed date keeps the 50-switch draft test within its intended cache freshness window. The 11-screen dialog test has an independent 60-second case budget. Each passed on both desktop engines. `after-suspend-corrected.log`
- The desktop note tests now separate explicit flushing from timed autosave, and the two-tab case waits for the committed revision and asserts the actual simulated conflict response. Both tests passed on both desktop engines after these changes. The latest edited browser specs passed ESLint. `after-suspend-notepad-clock.log`, `latest-browser-spec-lint.log`
- Two WebKit laptop draft round-trip tests sampled the first matrix before the related schedule annotations had arrived. They now wait for the exact related schedule labels before recording the initial matrix. Both variants, including the HTTP 429 retry, passed on Chromium and WebKit laptop. The desktop profiles started before this test correction. `qualification-webkit-laptop.log`, `qualification-graph-readiness.log`
- The manager sidebar test reached its eighth route as the 30-second case budget expired in WebKit iPhone X. It still checks every route at four widths and now allows 60 seconds. The targeted run passed in all four profiles. `qualification-webkit-iphone-x.log`, `qualification-sidebar-deadline.log`
- Parallel WebKit runs reached their time limits during high-volume graph editing, save verification and promo capture. The saved partial results show the exact phases and screenshots. WebKit focus checks and full profiles now run sequentially after Chromium finished. `qualification-five-graphs-retry.log`, `qualification-readiness-webkit-laptop-partial.json`, `qualification-webkit-iphone-x-partial.json`
- The unchanged five-graph and ten-graph save scenarios both passed with one WebKit laptop worker after the other browser projects ended. The tests took 1.8 and 2.0 minutes, respectively. Both unchanged promo capture cases also passed in WebKit iPhone X, in 16.8 and 17.1 seconds. `serial-graph-stress.json`, `serial-promo.json`
- In the mocked contract scenario, WebKit reported page errors as navigation interrupted a burst of graph API requests. The fixture now waits for API activity to settle before navigation, and the assertion against browser errors remains in place. Its focused WebKit laptop repeat and the final browser-spec lint passed. `qualification-serial-webkit-laptop.log`, `serial-contract-quiet.log`, `qualification-final-spec-lint.log`
- In WebKit iPhone X, the offer-list test could sample scroll before the browser finished auto-scrolling its focused launcher. The fixture now focuses without scrolling and still verifies the exact position and returned focus after dialog close. The corrected case passed in all four profiles and its spec passed ESLint. `qualification-final-webkit-iphone-x.log`, `qualification-swap-scroll.log`, `qualification-scroll-spec-lint.log`
- A Chromium repeat exposed missing browser completion events in the contract fixture's quiescence check. The test now counts its active mocked route handlers and releases each registration when the handler settles. The browser error assertion and other workflow checks are retained. The corrected scenario passed in all four profiles with one worker. Both revised browser specs passed ESLint. `qualification-final-chromium.log`, `playwright/5228/` under test results, `qualification-contract-retry.json`, `qualification-exact-spec-lint.log`
- Chromium sampled the last employee summary row immediately after the scroll-to-bottom call at 375px. The test now uses the same settling assertion as the adjacent statistics case. It still requires the entire row to clear the navigation. All 48 sticky-axis and mobile-summary cases and the focused ESLint check passed. `qualification-final-chromium.log`, `playwright/5228/` under test results, `qualification-sticky-scroll.json`, `qualification-sticky-spec-lint.log`
- The resumed iPhone X run recorded a column-order dialog which remained open after the second Escape. Its failure snapshot showed the canceled drag announcement and focus returned to the original handle. Three traced repeats of the original case passed. The test now waits for the drop marker to disappear and sends the second Escape through the intended handle, with the close and zero-write assertions retained. The corrected case passed three times in each of the four profiles. The previous full iPhone X run was stopped after 102 passes, 25 skips and the identified failure so the latest source could be tested. `qualification-resumed-webkit-iphone-x.log`, `playwright/5244/` under test results, `qualification-column-escape-repeat.json`, `qualification-column-escape-focused.json`

## Regression matrix

| Coverage | Test evidence |
| --- | --- |
| Authentication and accounts | `AuthServiceRoutingTests` covers password and role routing. `ManagerWorkspaceModeApiTests.LoginExchangeRefreshAndNewLoginPreserveIdentity`, the mode guard and SignalR tests cover exchange and mode authorization. Chromium passed `phone keypad accepts presses along all four edges`, `profile handles HTTP 401 without an endless loading state`, the anonymous legal pages and account language isolation. |
| Manager core | `HomeApprovalDialog.test.tsx`, `homeSchedule.test.ts`, `manager-home-dashboard.spec.ts` and `ManagerPhonePages.test.tsx` exercise exact approval targets, Home and manager phone views. Chromium passed `loads every manager sidebar route without request error` and `rate-limited save keeps the draft and lost response after commit stays unconfirmed`. |
| Schedule integrity | `ContainerGraphEditPage.saveIntegrity.test.tsx` and `manager-matrix-actions.spec.ts` exercise edits, save boundaries and manual columns. Chromium passed `three linked schedule drafts save and reopen with their own SQLite data` and both `editor tab round trips preserve a text draft` variants with and without HTTP 429. `EmployeePublishedGraphExportTests` and `ExportLayerDeepTests` cover manager and public exports. |
| Employee workflows | `EmployeeScheduleSelectDialog.test.tsx`, `rankEmployeeSchedules.test.ts` and `EmployeePublicSchedulesTests` cover public previews and own-schedule ranking. Chromium passed `an unenrolled employee joins a public schedule only when Home approves the addition`, the real pickup approval and rejection cases, `compact offers and review details 390x844`, and the Urlop submit, approve and availability isolation scenario. |
| Notepad | `ManagerNotepadEditSessionTests`, `usePublicNoteSession.test.tsx`, `useNotepadAutosave.test.tsx`, `notepadExport.test.ts` and `manager-notepad.spec.ts` cover concurrency, authorization, drafts, persistence and clipboard. |
| Realtime and communication | `PresenceProvider.test.tsx`, `RealtimeMiddlewareAndGenericCoverageTests` and `ManagerWorkspaceSignalRTests` cover role-scoped events and presence. Chromium passed `ordered photo, text and GIF survive composer preview, save and employee dismissal`, all three `employee notification lifecycle` locales and the PWA document-only launch scenarios. |
| Responsive UI and accessibility | `manager-workspace-modes.spec.ts`, `manager-notepad.spec.ts`, `ActionMenu.test.tsx` and `i18nCoverage.test.ts` cover phone viewport, dialog behavior, keyboard interactions and translations. Chromium passed the sticky axes and mobile summary matrix, populated dialog stress, and the reduced motion dock boundaries. Physical iOS Safari/PWA and Android Chrome/PWA checks are pending. |
| SQLite, backup, deployment and security | `DatabaseMigrationStartupTests`, `test_gf3_maintenance.py` and `test_gf3_prepare_release.py` cover schema, backup and review binding. The expanded Docker and fence paths in `rehearse_release.py` and existing ingress workflow require Linux CI. |

## Open gate evidence

Vitest skipped `replays the supplied F42/F35 snapshots through the actual editor builder` in `FrontEnd/src/entities/containers/model/graphSaveIntegrity.test.ts` because no private fixture was supplied. Windows skipped four Ubuntu advisory-lock cases in `test_gf3_guard.GuardTests`: `test_atomic_noop_validation_and_reload_rollback`, `test_manual_ban_allow_unallow_unban_and_restart`, `test_missing_docker_and_lock_contention` and `test_source_isolation_refresh_and_seven_day_expiry`. Linux Docker, nftables and HTTP/HTTPS ingress evidence remains a required CI gate.

Authorized staging and physical iOS and Android devices were unavailable. Native Safari, Chrome and installed PWA verification remains open for OS keyboard dismissal, focus zoom, saving and reopening notes, formatted multiline Polish and Ukrainian text pasted into a native editor, permission-denied clipboard fallback, and restoration of underlying scroll and focus. Desktop manager and employee staging smoke also remains pending. Obtain explicit owner sign-off for the device limitation before production.

Independent release review, protected tool installation, host rehearsal, deployment and live smoke checks remain pending.

The final source gate verified every blob in the candidate manifest, coverage assignments for 168 pinned daily paths and 76 additional source and test paths, both migration source pins and `git diff --check`. The QA record is the only additional file outside the tested source manifest.

## Pinned daily delta by coverage

`git diff --name-status ed04abce13552c57fcbd2828f8c38a40d00a1245..42fb0fa710a5bccaba9f4502ec6a6dd9981c521c`

<details>
<summary>Employee own schedule, public preview and exports (22)</summary>

- `M BusinessLogicLayer/Services/Export/GraphNoteSqlExportBuilder.cs`
- `M FrontEnd/src/entities/employee-schedule/model/types.ts`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleDailyBody.test.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleDailyBody.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.module.css`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.module.css`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.module.css`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.test.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/PendingCorrectionBadge.module.css`
- `M FrontEnd/src/pages/employee-schedule/ui/PendingCorrectionBadge.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/rankEmployeeSchedules.ts`
- `M FrontEnd/src/pages/employee-schedule/ui/scheduleShiftStatuses.test.ts`
- `M FrontEnd/src/pages/employee-schedule/ui/scheduleShiftStatuses.ts`
- `M GF3.Tests/EmployeePublicSchedulesTests.cs`
- `A GF3.Tests/EmployeeScheduleCorrectionStatusTests.cs`
- `M GF3.WebApi/Contracts/EmployeeSchedules/EmployeeScheduleDto.cs`
- `M GF3.WebApi/Controllers/EmployeeSchedulesController.cs`
- `M GF3.WebApi/Middleware/EmployeePresenceMiddleware.cs`

</details>

<details>
<summary>SQLite schema, migration and fixture preservation (11)</summary>

- `A DataAccessLayer/Migrations/20261126170000_ExpandManagerNotepad.cs`
- `A DataAccessLayer/Migrations/20261126180000_AddManagerNoteEditSessions.cs`
- `M DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs`
- `M DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`
- `A DataAccessLayer/Models/ManagerNoteFolderModel.cs`
- `M DataAccessLayer/Models/ManagerNoteModel.cs`
- `A DataAccessLayer/Models/ManagerNoteShareModel.cs`
- `A DataAccessLayer/Models/ManagerNoteTagLinkModel.cs`
- `A DataAccessLayer/Models/ManagerNoteTagModel.cs`
- `A DataAccessLayer/Models/ManagerNoteVersionModel.cs`
- `M GF3.Tests/DatabaseMigrationStartupTests.cs`

</details>

<details>
<summary>Graph matrix, drafts, persistence and query invalidation (23)</summary>

- `M FrontEnd/e2e/generated-manual-shifts.spec.ts`
- `M FrontEnd/e2e/manager-graph-save-rate-limit.spec.ts`
- `A FrontEnd/e2e/manager-graph-tab-save.spec.ts`
- `A FrontEnd/e2e/manager-matrix-actions.spec.ts`
- `M FrontEnd/src/entities/availability-groups/api/queries.ts`
- `M FrontEnd/src/entities/containers/api/queries.ts`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphEditor.module.css`
- `A FrontEnd/src/entities/containers/ui/ContainerGraphEditor.test.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphEditor.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.module.css`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.test.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.module.css`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.test.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.tsx`
- `M FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.generatedManualShifts.test.tsx`
- `M FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.module.css`
- `M FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.saveIntegrity.test.tsx`
- `M FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx`
- `M FrontEnd/src/shared/lib/tanstack/react-query.test.tsx`
- `M FrontEnd/src/shared/lib/tanstack/react-query.tsx`
- `A FrontEnd/src/shared/ui/icons/ColumnsIcon.tsx`

</details>

<details>
<summary>Home review, activity and daily schedule (15)</summary>

- `M FrontEnd/e2e/manager-home-dashboard.spec.ts`
- `M FrontEnd/src/pages/home/api/managerHomeActivityApi.ts`
- `A FrontEnd/src/pages/home/ui/HomeApprovalDialog.module.css`
- `A FrontEnd/src/pages/home/ui/HomeApprovalDialog.test.tsx`
- `A FrontEnd/src/pages/home/ui/HomeApprovalDialog.tsx`
- `M FrontEnd/src/pages/home/ui/HomePage.module.css`
- `M FrontEnd/src/pages/home/ui/HomePage.test.tsx`
- `M FrontEnd/src/pages/home/ui/HomePage.tsx`
- `M FrontEnd/src/pages/home/ui/homeActivity.ts`
- `A FrontEnd/src/pages/home/ui/homeSchedule.ts`
- `A FrontEnd/src/pages/manager-phone/ui/ManagerPhoneHomePage.module.css`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneHomePage.tsx`
- `M GF3.Tests/ManagerHomeActivityControllerTests.cs`
- `M GF3.WebApi/Contracts/ManagerHome/ManagerHomeActivityDtos.cs`
- `M GF3.WebApi/Services/ManagerHomeActivityReader.cs`

</details>

<details>
<summary>Phone workspace, read-only views and mode exchange (20)</summary>

- `M FrontEnd/e2e/manager-workspace-modes.spec.ts`
- `M FrontEnd/src/app/layouts/manager-phone-layout/ManagerPhoneLayout.module.css`
- `M FrontEnd/src/app/layouts/manager-phone-layout/ManagerPhoneLayout.tsx`
- `A FrontEnd/src/app/layouts/manager-phone-layout/ManagerPhoneNotepadContext.ts`
- `M FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.module.css`
- `M FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx`
- `M FrontEnd/src/features/manager-workspace-mode/ui/ManagerWorkspaceModeSwitch.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneAvailabilityPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneContainerDetailPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneContainersPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneEmployeeDetailPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneEmployeesPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneMorePage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhonePage.module.css`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhonePage.test.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhonePage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhonePages.test.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneShopDetailPage.tsx`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneShopsPage.tsx`
- `M GF3.Tests/Auth/ManagerWorkspaceModeApiTests.cs`

</details>

<details>
<summary>Correction, contract approval and enrollment (22)</summary>

- `M FrontEnd/e2e/shift-corrections.spec.ts`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphContractApprovalsCard.test.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphContractApprovalsCard.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphShiftCorrectionsCard.module.css`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphShiftCorrectionsCard.test.tsx`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphShiftCorrectionsCard.tsx`
- `M FrontEnd/src/entities/shift-corrections/api/queries.test.tsx`
- `M FrontEnd/src/entities/shift-corrections/api/queries.ts`
- `M FrontEnd/src/entities/shift-corrections/api/shiftCorrectionsApi.ts`
- `M FrontEnd/src/entities/shift-corrections/model/types.ts`
- `A FrontEnd/src/entities/shift-swap-approvals/api/queries.test.tsx`
- `M FrontEnd/src/entities/shift-swap-approvals/api/queries.ts`
- `M FrontEnd/src/entities/shift-swap-approvals/api/shiftSwapApprovalsApi.ts`
- `M GF3.Tests/ContractShiftApprovalWorkflowTests.cs`
- `M GF3.Tests/ShiftCorrectionControllerTests.cs`
- `A GF3.Tests/ShiftCorrectionScheduleCandidatesTests.cs`
- `A GF3.WebApi/Contracts/ShiftCorrections/ShiftCorrectionScheduleDto.cs`
- `M GF3.WebApi/Controllers/ManagerContractShiftApprovalsController.cs`
- `M GF3.WebApi/Controllers/ShiftCorrectionsController.cs`
- `A GF3.WebApi/Services/ScheduleEmployeeEnrollment.cs`
- `M GF3.WebApi/ShiftSwaps/ContractShiftApprovalService.cs`
- `M GF3.WebApi/ShiftSwaps/ShiftSwapExecutionService.cs`

</details>

<details>
<summary>Realtime, authorization and session isolation (12)</summary>

- `M FrontEnd/src/app/providers/PresenceProvider.test.tsx`
- `M FrontEnd/src/app/providers/PresenceProvider.tsx`
- `M FrontEnd/src/shared/api/httpClient.ts`
- `M FrontEnd/src/shared/api/queryKeys.ts`
- `M GF3.Tests/Auth/ManagerWorkspaceModeGuardTests.cs`
- `M GF3.Tests/EmployeeOnlineWorkflowLogTests.cs`
- `M GF3.Tests/RealtimeMiddlewareAndGenericCoverageTests.cs`
- `M GF3.Tests/WebApiEmployeeSurfaceTests.cs`
- `M GF3.WebApi/Middleware/ManagerWorkspaceModeGuardMiddleware.cs`
- `M GF3.WebApi/Realtime/EmployeePresenceHub.cs`
- `M GF3.WebApi/Realtime/RealtimeNotifier.cs`
- `M GF3.Tests/Auth/ManagerWorkspaceSignalRTests.cs`

</details>

<details>
<summary>Notepad access, editor, history and leases (24)</summary>

- `M FrontEnd/src/entities/manager-notepad/api/managerNotepadApi.ts`
- `M FrontEnd/src/entities/manager-notepad/api/queries.ts`
- `M FrontEnd/src/entities/manager-notepad/index.ts`
- `M FrontEnd/src/entities/manager-notepad/model/types.ts`
- `M FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.module.css`
- `M FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.test.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadEditorView.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadIcon.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadListView.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadNoteCard.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadSettingsMenu.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadShareRecipients.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadVersionHistoryView.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadViews.module.css`
- `A FrontEnd/src/features/manager-notepad/ui/NotepadVisibilityControl.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/notepadExport.ts`
- `A FrontEnd/src/features/manager-notepad/ui/notepadRichText.test.ts`
- `A FrontEnd/src/features/manager-notepad/ui/notepadRichText.ts`
- `A FrontEnd/src/features/manager-notepad/ui/useNotepadAutosave.test.tsx`
- `A FrontEnd/src/features/manager-notepad/ui/useNotepadAutosave.ts`
- `A FrontEnd/src/features/manager-notepad/ui/usePublicNoteSession.ts`
- `M GF3.Tests/ManagerNotepadControllerTests.cs`
- `M GF3.WebApi/Controllers/ManagerNotepadController.cs`

</details>

<details>
<summary>Layout, interactions, accessibility and locale (18)</summary>

- `M FrontEnd/src/features/manager-system-news/ui/ManagerSystemNews.module.css`
- `M FrontEnd/src/pages/information/ui/InformationPage.module.css`
- `M FrontEnd/src/pages/information/ui/InformationPage.test.tsx`
- `M FrontEnd/src/pages/information/ui/InformationPage.tsx`
- `M FrontEnd/src/shared/i18n/pl.json`
- `M FrontEnd/src/shared/i18n/uk.json`
- `M FrontEnd/src/shared/lib/useDropdownPosition.ts`
- `M FrontEnd/src/shared/ui/ConfirmDialog/ConfirmDialog.tsx`
- `M FrontEnd/src/shared/ui/PageHeader/PageHeader.module.css`
- `A FrontEnd/src/shared/ui/components/ActionMenu/ActionMenu.module.css`
- `A FrontEnd/src/shared/ui/components/ActionMenu/ActionMenu.test.tsx`
- `A FrontEnd/src/shared/ui/components/ActionMenu/ActionMenu.tsx`
- `A FrontEnd/src/shared/ui/components/ActionMenu/index.ts`
- `A FrontEnd/src/shared/ui/icons/DoubleCheckIcon.tsx`
- `A FrontEnd/src/shared/ui/icons/PencilIcon.tsx`
- `A FrontEnd/src/shared/ui/icons/RequestReviewIcon.tsx`
- `A FrontEnd/src/shared/ui/icons/TrashIcon.tsx`
- `M FrontEnd/src/shared/ui/icons/index.ts`

</details>

<details>
<summary>API documentation (1)</summary>

- `M docs/api-map.md`

</details>

## Additional candidate changes by coverage

<details>
<summary>Release tooling, CI and runbook (9)</summary>

- `M .github/workflows/ci.yml`
- `M GF3.Tests/GF3.Tests.csproj`
- `M deploy/security/gf3_maintenance.py`
- `M deploy/security/gf3_prepare_release.py`
- `M deploy/security/tests/rehearse_release.py`
- `M deploy/security/tests/test_gf3_maintenance.py`
- `M deploy/security/tests/test_gf3_prepare_release.py`
- `M docs/gfctl-prod2-database-maintenance.md`
- `?? deploy/security/tests/fixtures/reviewed-notepad-schema-prefix-contracts.json`

</details>

<details>
<summary>SQLite schema, migration and fixture preservation (2)</summary>

- `M DataAccessLayer/Migrations/20261126180000_AddManagerNoteEditSessions.cs`
- `M GF3.Tests/DatabaseMigrationStartupTests.cs`

</details>

<details>
<summary>Home review, activity and daily schedule (6)</summary>

- `M FrontEnd/e2e/manager-home-dashboard.spec.ts`
- `M FrontEnd/src/pages/home/ui/HomeApprovalDialog.test.tsx`
- `M FrontEnd/src/pages/home/ui/HomePage.test.tsx`
- `M FrontEnd/src/pages/home/ui/homeActivity.test.ts`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhoneHomePage.tsx`
- `?? FrontEnd/src/pages/home/ui/homeSchedule.test.ts`

</details>

<details>
<summary>Graph matrix, drafts, persistence and query invalidation (8)</summary>

- `M FrontEnd/e2e/manager-matrix-actions.spec.ts`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphEditor.test.tsx`
- `M FrontEnd/src/shared/lib/tanstack/react-query.test.tsx`
- `M FrontEnd/e2e/manager-priority.spec.ts`
- `M FrontEnd/src/entities/containers/ui/ShiftSwapHistoryDialog.module.css`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.module.css`
- `M FrontEnd/e2e/generated-manual-shifts.spec.ts`
- `M FrontEnd/e2e/manager-graph-save-rate-limit.spec.ts`

</details>

<details>
<summary>Phone workspace, read-only views and mode exchange (3)</summary>

- `M FrontEnd/e2e/manager-workspace-modes.spec.ts`
- `M FrontEnd/src/pages/manager-phone/ui/ManagerPhonePages.test.tsx`
- `M GF3.Tests/Auth/ManagerWorkspaceModeApiTests.cs`

</details>

<details>
<summary>Correction, contract approval and enrollment (4)</summary>

- `M FrontEnd/e2e/employment-contract-rules.spec.ts`
- `M FrontEnd/e2e/shift-corrections.spec.ts`
- `M FrontEnd/src/entities/containers/ui/ContainerGraphContractApprovalsCard.test.tsx`
- `M GF3.Tests/ShiftCorrectionControllerTests.cs`

</details>

<details>
<summary>Realtime, authorization and session isolation (5)</summary>

- `M FrontEnd/src/app/providers/PresenceProvider.test.tsx`
- `M FrontEnd/src/app/providers/PresenceProvider.tsx`
- `M GF3.Tests/Auth/ManagerWorkspaceModeGuardTests.cs`
- `M GF3.Tests/RealtimeMiddlewareAndGenericCoverageTests.cs`
- `M GF3.Tests/Auth/ManagerWorkspaceSignalRTests.cs`

</details>

<details>
<summary>Notepad access, editor, history and leases (16)</summary>

- `M FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.test.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/NotepadEditorView.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/NotepadSettingsMenu.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/NotepadVersionHistoryView.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/NotepadVisibilityControl.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/notepadExport.ts`
- `M FrontEnd/src/features/manager-notepad/ui/useNotepadAutosave.test.tsx`
- `M FrontEnd/src/features/manager-notepad/ui/useNotepadAutosave.ts`
- `M FrontEnd/src/features/manager-notepad/ui/usePublicNoteSession.ts`
- `M GF3.Tests/ManagerNotepadControllerTests.cs`
- `?? FrontEnd/e2e/manager-notepad.spec.ts`
- `?? FrontEnd/src/entities/manager-notepad/api/queries.test.tsx`
- `?? FrontEnd/src/features/manager-notepad/ui/notepadExport.test.ts`
- `?? FrontEnd/src/features/manager-notepad/ui/usePublicNoteSession.test.tsx`
- `?? GF3.Tests/ManagerNotepadEditSessionTests.cs`

</details>

<details>
<summary>Employee own schedule, public preview and exports (13)</summary>

- `M FrontEnd/e2e/employee-critical-pages.spec.ts`
- `M FrontEnd/e2e/employee-phone-layout.spec.ts`
- `M FrontEnd/e2e/employee-schedule-column-order.spec.ts`

- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.test.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.tsx`
- `M FrontEnd/src/pages/employee-schedule/ui/rankEmployeeSchedules.test.ts`
- `M GF3.Tests/EmployeePublicSchedulesTests.cs`
- `M GF3.Tests/EmployeePublishedGraphExportTests.cs`
- `M GF3.Tests/EmployeeScheduleCorrectionStatusTests.cs`
- `M GF3.Tests/EmployeeSchedulePdfControllerTests.cs`
- `M GF3.Tests/ExportLayerDeepTests.cs`
- `M GF3.WebApi/Middleware/EmployeePresenceMiddleware.cs`
- `?? FrontEnd/src/pages/employee-schedule/ui/EmployeeScheduleSelectDialog.test.tsx`

</details>

<details>
<summary>Layout, interactions, accessibility and locale (8)</summary>

- `M FrontEnd/e2e/manager-layout.spec.ts`
- `M FrontEnd/e2e/swap-exchange-workflow.spec.ts`
- `M FrontEnd/e2e/viewport-dialogs.spec.ts`
- `M FrontEnd/e2e/viewport-pages.spec.ts`

- `M FrontEnd/src/shared/i18n/i18nCoverage.test.ts`
- `M FrontEnd/src/shared/i18n/pl.json`
- `M FrontEnd/src/shared/i18n/uk.json`
- `M FrontEnd/src/shared/lib/react-router-dom.tsx`

</details>

<details>
<summary>Vacation editor focus and navigation (2)</summary>

- `M FrontEnd/e2e/urlop-workflow.spec.ts`
- `M FrontEnd/src/pages/manager-urlop/ui/ManagerUrlopPage.tsx`

</details>
