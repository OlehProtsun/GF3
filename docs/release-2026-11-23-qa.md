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
| Asset metadata | Inspected PNG and WebP container metadata in **207** Guide, splash and icon files. Found 121 WebP color profiles. EXIF, XMP and PNG text chunks were absent in this scope. The shipped ScheduleTemplate workbook contains author and last-modified-by fields. The capture contents and the workbook attribution remain subject to release review. |
| Lint and Playwright CI wiring | Lint passed with **0 errors and 21 existing warnings**. The exact two-browser smoke configuration passed **12 tests, 0 failed, 0 skipped**. CI YAML parses and the smoke job precedes the existing Docker job. |
| Host guard | Windows **53 passed, 0 failed, 4 skipped** (Linux-only advisory locking). A disposable Linux run with a read-only repository mount passed **57 tests, 0 failed, 0 skipped**. |
| Dependency advisory checks | Patched the production lock entry for MIT `ws` from 7.5.10 to 7.5.14 within SignalR's version range. `npm audit --omit=dev` and `dotnet list GF3.WebApi/WebApi.csproj package --vulnerable --include-transitive` found zero current advisories in the queried scopes. The unfiltered npm install reported **24 advisories** in the development dependency scope. |
| Production Docker | Local Release publish passed. Docker registry access initially returned EOF. Loaded the official amd64 base manifests and layers after verifying SHA-256 digests over IPv4, then built the unchanged production Dockerfile successfully with `docker build --pull=false --platform linux/amd64 --progress=plain -t gf3-plan78:local .`. The image is `sha256:fcdb3615fc8de5bfe540dd42c1bc1aa8791c0524639ba0f819e8362a2484f193`. Verified the native linkages, .NET 10.0.15, the font and bundled notices in that image. |
| Production container HTTP | Passed in a network-isolated non-root container using a read-only disposable SQLite snapshot copied into a temporary writable mount. Verified health, build metadata, authenticated manager PC, photo/text/GIF order, actual WebP and GIF responses, private cache and nosniff headers, and HTTP 400 for corrupted image input. |
| Isolated Docker ingress | The workflow's ingress harness passed locally against the production image with an isolated Compose project and loopback ports. Covered HTTP, HTTPS, ACME, SignalR, PWA, quotas, IPv4, IPv6, geo filtering and safe logs. Cleanup removed the test containers, networks and disposable volume. |

## Browser matrix and release gates

| Scope | Chromium | Mobile Chrome | WebKit laptop | iPhone WebKit | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| PWA, Guide and ordered communication media on the production frontend preview | 6 passed | 6 passed | 6 passed | 6 passed | **24 passed, 0 failed, 0 skipped** |
| Exact Vite-backed hermetic CI configuration | 6 passed | Outside scope | Outside scope | 6 passed | **12 passed, 0 failed, 0 skipped** |
| Employee PDF share, download and activation on disposable SQLite | 3 passed | Outside scope | Outside scope | 3 passed | **6 passed, 0 failed, 0 skipped** after modeling the same synthetic shop for the approved-leave request. The pre-fix Chromium trace recorded a 404 against the synthetic shop. |
| Holiday and day-off states | 2 passed | 2 passed | 2 passed | 2 passed | **8 passed, 0 failed, 0 skipped** after modeling prior install-invite dismissal in this fixture. The pre-fix Chromium reproduction showed the invite intercepting the click. |
| Complete 44-file Playwright suite | Pending | Pending | Pending | Incomplete | **60 of 1,968 cases started, 22 failures logged** before stopping. No reliable final pass/skip totals from the interrupted runner. |

The first complete-suite attempt used Vite dev as required by the plan. Production-update tests require `/version.json` and a production frontend bundle. The second attempt used the compiled frontend at loopback port 4173 and the same isolated Development API. It also exposed stale browser fixtures that left the employee installation invite open and expected outdated Daily/related assignment labels. Dedicated PWA tests retain the prompt behavior. Following fixture corrections, Daily schedule and notification cases passed for Chromium and iPhone WebKit. The employee-to-manager correction case passed in Chromium and in an isolated iPhone touch rerun.

The real-account fixture repeatedly signs in with the same manager username. The running disposable API returned HTTP 429 with `Retry-After: 559` during the broad run. It enforces a per-account authentication limit even on successful sign-ins. The limiter was left active. Release-wide browser sign-off still requires a runner arrangement that avoids saturating the shared manager account, plus a clean run of all four projects. Compare fixture failures, timing failures and verified product defects before reusing partial counts.

One separate PDF rerun was stopped after discovering that the restated connection override was ignored and the local development SQLite database received test traffic. Its results were discarded. The following run used the verified `ConnectionStrings:Default` override with a confirmed temporary SQLite file. Production data was outside these runs.

The [first hosted CI run](https://github.com/OlehProtsun/GF3/actions/runs/45271812769) on `10833d7d1f70d483177a16c17d235371af379f0f` passed Build and test. Playwright browser installation failed because the moving `ubuntu-latest` runner resolved to Ubuntu 26.04, which the locked Playwright build rejects. The smoke job now uses Ubuntu 24.04. The first CI Docker job was skipped because it depends on the smoke job.

The [candidate CI rerun](https://github.com/OlehProtsun/GF3/actions/runs/45274656553) on `93c9e9a4cddd3520fde7d951d1887a3508500373` ended in **failure**. [Build and test](https://github.com/OlehProtsun/GF3/actions/runs/45274656553/job/138043186000) passed with 1,931 backend tests, 57 host-guard tests and 1,544 Vitest tests passing. Vitest retained its one existing skip. [Playwright smoke](https://github.com/OlehProtsun/GF3/actions/runs/45274656553/job/138050699005) passed all 12 tests on Ubuntu 24.04. The [Docker job](https://github.com/OlehProtsun/GF3/actions/runs/45274656553/job/138057323139) built the image and pulled the stock Nginx image. Its cutover and rollback rehearsal failed at `deploy/security/tests/rehearse_release.py:63` on `assert target[-2:] == added`. The fixture assumes the November 14–15 additive migrations end the history. The candidate ends with the November 23 communication migrations. Hosted ingress was consequently skipped. The matching ingress harness passed locally.

At this point the release gates included GSAP and `@gsap/react`, non-permissive OS packages, final native and asset attribution, a passing full browser run, hosted CI, protected SQLite maintenance, coordinated media restore and device verification. The follow-up below records the later CI run and browser results.

The candidate commits change 71 paths relative to the plan's source baseline, including the Web API, repositories, frontend, relevant tests, Dockerfile, CI, release assets README and license audit. The QA evidence from the final CI run is recorded locally while the exact tested candidate remains the feature branch tip.

## BLOCKER — PRODUCTION MAINTENANCE

### Safe production procedure

Current PROD2 code and data must be taken from the managed host's active `release.json`, immutable release bundle and attached `/data` volume. Do not infer them from a workstation database or from the branch name. Before any update, the operator must record the complete active SHA, target SHA and database migration history on that host, verify the immutable deployed Docker image and exact protected tooling checksums, and ensure the current `gfctl` guard is healthy. The current `gfctl` rejects the two non-additive migrations listed below. This candidate therefore **cannot currently be applied to the host**. Do not proceed to [11] Deploy PROD2 or change the protected migration whitelist as an operational workaround.

The next allowed server-side step is a separate reviewed maintenance-tooling change. That change must provide an exact-SHA reviewed transition, take the existing exclusive guard and writer fence, quiesce every database writer and the media directory, and create a single recovery bundle containing a SQLite online backup plus the entire `communication-gifs` tree. Record SHA-256, paths, file sizes, ownership and permissions for both the database and every media object. Test restoring this bundle on a disposable clone. Keep the normal guard and require `PRAGMA integrity_check`, `PRAGMA foreign_key_check`, the full migration history, the old swap indexes and checks, legacy row comparisons, and media hashes to pass before accepting it as recoverable.

For the candidate, run the separately reviewed maintenance dry run against an isolated copy made from the current active production release. Exercise success, migration failure, activation failure, recovery and abort before write access resumes. The recovery path must preserve forensic copies and stop if any writer, schema fingerprint, row inventory, migration history, SQLite sidecar or media hash changes unexpectedly. Record the exact host, active and target SHAs, Docker digest, seven migration IDs, before/after schema, row counts, constraints, media manifests, backup locations and recovery outcome. Only after this rehearsal passes may a separately authorized release review consider deployment. This procedure documents the required sequence. It is **not** a completed host rehearsal or deployment authorization.

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
- The ordinary release guard's `migration_fingerprint` returned `06513a51c0bd32ce80543366f749e780abcdf51ebc78a05adf71b0be7af493b1` for PROD2 and `4828e7836f01d682f6867373e7920af0a8f5849dc84937efe1fa2456b61bb69b` for the candidate. The ordinary stage path requires equality.
- A separate scratch SQLite database contained one committed row referencing a sentinel in `communication-gifs/`. Both `gf3_release.backup` and `gf3_maintenance.final_backup` produced a valid restored row with `integrity_check=ok`, zero foreign-key errors and the same database SHA-256 `c3a72be26e6a1ef04b31f8286d0ae2ecaf606c9c93c3a36a964b81cc5023f55d`. Both backups omitted the attachment.
- Production attachments live under `/data/communication-gifs`. The Dockerfile assigns `/data` to `app:app`. The live volume's ownership and permissions, coordinated backup of SQLite and attachments, production preflight for the full chain, abort and rollback before the first live write remain unverified. They require the separately reviewed maintenance procedure in Plan 78.
- Physical iOS and Android verification remains open. The browser checks use desktop Chromium, mobile emulation and Windows WebKit.

## Follow-up · CI rehearsal and E2E isolation

- `06f9eae` updates the disposable migration fixture. The previous CI check expected the November 14–15 migrations at the end of the history. The Docker fixture now derives the exact history from Git and exercises the three additive communications migrations after `20261122173000_AddShiftRemovalCorrections`. The rehearsal checks the SQLite integrity, the unchanged historical rows and history versions, field definitions, schema and expected nullable defaults. A duplicate-column failure exercises recovery of the frozen copy. Kernel fence and rollback scenarios still run with the pinned historical source and target images that implement the established protocol. [Hosted CI for the rehearsal fix](https://github.com/OlehProtsun/GF3/actions/runs/45280701725)
- `GF3_CONNECTION_STRING` is the EF design-time override. `AddWebApiCore` uses `ConnectionStrings:Default` at runtime. The full Playwright runner now serves intercepted cases on a loopback Vite preview with no backend proxy. Cases that write to a real API start a new Web API instance on an ephemeral loopback port and attest its one-use temporary SQLite path before seeding data. The child process and database are cleaned up after the case, including on assertion failure. The app rejects a missing test-mode switch, a stale environment override, a nonempty scratch directory and a mismatched runtime database before migration. A Chromium PDF case and the isolated four-browser manual-shift acceptance and refresh passed. The permanent local database received no writes during this follow-up.
- The read-only check of `%LOCALAPPDATA%/GF3/SQLite.db` identified two active synthetic records from the earlier run. Employee `56`, account `13`, username `employee.pdf.e2e.1795469708530-imbrtj`. Employee `57`, account `14`, username `employee.pdf.e2e.1795469708062-0cvmym`. Each has one login and two online workflow entries. Both remain in place pending authorization for cleanup. Production was outside this inspection.
- The disposable SQLite and media restore exercise confirmed that both shipped backup functions omit `communication-gifs`. After a simulated candidate failure, the original SQLite restored with matching SHA-256 `93b9cb8e0d51f974f56a0045c9f703872335ef2305a36d1b548a3e27bd1253a2`, intact history and foreign keys. A separate scratch copy of the fixture GIF restored with matching SHA-256 `978b08c1c3d5ba751491d4a991942f05231ffa59c79fc2338f441b1c839706a5`. This is a proof on a disposable fixture, independent of the protected deployment procedure.
- The license inventory now links Webflow's GSAP product terms and commercial-use announcement. They confirm that GF3's observed application and promo usage can use GSAP without library fees under the custom license. Remaining native binaries and static component notices, shipped asset rights and the final release notice bundle are recorded in the [license report](third-party-licenses.md).

### Hosted CI and full browser run

GitHub Actions run [45293016387](https://github.com/OlehProtsun/GF3/actions/runs/45293016387) passed all three jobs for `ba4942faf44e0e0774c0caa144295dfbf62bd4af`: Build and test, Playwright smoke, and Docker image. CI recorded **1,931 backend tests passed**, **57 host guard tests passed**, **1,544 Vitest tests passed and 1 skipped**, and **0 lint errors and 21 warnings**. The Docker job built the image and passed the disposable cutover, rollback, HTTP and HTTPS ingress steps. A separate local production image smoke passed authentication, ordered photo/text/GIF delivery, WebP conversion, private headers and corrupt-file rejection. The protected PROD2 data and media restore gate remains open. The hosted result applies to the published commit, before the local browser follow-up changes.

The browser fixes are now on `dev22.11.2026` at `d65fee2ee9eb38f8158bc0cdbd56b10e2b1ee67a`. [CI 45383107806](https://github.com/OlehProtsun/GF3/actions/runs/45383107806) was dispatched for that exact SHA. The latest targeted Chromium run on this commit passed **3 of 3** for persisted graph creation, draft generation, and cross-schedule swap comparison (`GF3.Tests/TestResults/plan78-targeted-5310.json`).

CI run 45383107806 completed successfully on `d65fee2ee9eb38f8158bc0cdbd56b10e2b1ee67a`. All three jobs passed: Build and test, Playwright smoke, and Docker image. The Build and test job reported **1,931 .NET tests and 57 host guard tests passed**, **1,544 Vitest tests passed and 1 skipped**, and completed lint/build. The Docker job passed the release rehearsal and ingress steps. It is existing CI evidence for this code SHA; the local report edits are a subsequent documentation-only delta.

The full local Playwright run completed all four project groups on the earlier checkout, before the latest fixture and application fixes:

| Browser project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Chromium | 364 | 126 | 2 |
| Mobile Chrome | 355 | 129 | 8 |
| WebKit laptop | 356 | 133 | 3 |
| WebKit iPhone X | 348 | 135 | 9 |
| Total | 1,423 | 523 | 22 |

The configured skips include project-specific viewport coverage, Chromium service-worker coverage and the separate isolated live acceptance case. The opt-in manual-shift acceptance passed separately using a disposable real backend and all four browser projects. The iPhone X profile is emulation, so installed iOS and Android PWA checks still require physical devices.

The full Chromium repeat at port `5290` finished with **480 passed, 10 failed and 2 skipped** (`GF3.Tests/TestResults/plan78-final-chromium.json`). It used an earlier revision of the local fixtures. Failures included the older Home dashboard expectations, manager route navigation, graph preview teardown, swap scroll-start state, Urlop offset after a click, and viewport stress fixtures. Later targeted runs passed manager navigation, draft generation, the 390×844 swap scroll/focus test, the 768 px Urlop offset test, Home workspace switching and all four populated dialog sizes in Chromium. On the current committed `d65fee2`, persisted graph creation, draft generation and cross-schedule swap passed in Chromium. On iPhone WebKit, graph creation, cross-schedule swap, Home switching, and narrow contract approval passed. The other three full browser project reruns remain in progress at this report revision. Early failures in the full runs still include tests to triage. There is no final pass count for those runs.

Follow-up failures traced to stale fixtures include the Polish guest and workspace chooser labels, the explicit swap confirmation after sending, the Home period control, and the employee PWA install dialog intercepting unrelated workflows. The approval pulse on Home now respects reduced motion. The employee account language control now also meets its 44 px measured touch target in iPhone WebKit. Dedicated PWA tests still exercise the install dialog.

- Targeted reruns passed **48 PIN and swap cases** in Chrome Mobile and both WebKit profiles, **12 PWA, Guide and communications smoke cases** in Chromium and iPhone WebKit, and **3 day-off shift request cases** in Chromium (EN, PL and UK). The SQLite migration rerun passed **9 of 9** on disposable databases. The separate restore fixture reproduced the missing media in both standard backups and verified the hashes after restoring SQLite and the separately copied GIF.
- A later sequential iPhone WebKit run passed **2 of 2** for saving and reopening a persisted graph and for comparing a swap across two schedules (`GF3.Tests/TestResults/plan78-targeted-5308.json`). The graph case confirmed HTTP 201, the saved name via the real API and the expected activity requests for the persisted graph. The case closes the editor panels on the narrow viewport before selecting Save. The comparison case exercised keyboard scrolling and activation in mobile WebKit, and the targeted Chromium run exercised wheel and keyboard scrolling.
- The follow-up Windows .NET Release run under `Category!=LocalOnly` passed **1,931 of 1,931**, with no skips (`GF3.Tests/TestResults/plan78-followup-release.trx`). Local TypeScript/Vite build and ESLint passed, with **21 lint warnings**. The local concurrent Vitest run had **1,543 passed, 1 timeout and 1 skipped**. All three tests in the file that timed out passed in a separate run with the original timeout.
- A complete sequential Vitest rerun passed **1,544**, failed **0**, skipped **1** across all 159 files. It used one worker with the original per-test timeouts (`GF3.Tests/TestResults/plan78-d65fee2-vitest-one-worker.json`). The earlier concurrent timeout passed again in isolation.
- The local production Docker build with the follow-up UI changes succeeded (`linux/amd64`, image index `sha256:feee34343e538a578cc16dba2597cc2c7133ef0de121f2f3960239ad6739c8be`). With networking disabled and SQLite on a scratch volume, the container passed health and build metadata, login, ordered photo/text/GIF, WebP conversion, private headers and corrupt-file rejection. It contained the original SkiaSharp license and notices and Carlito OFL text. The Playwright viewport fixture was absent from the production frontend.
- A repeat read-only query of the working Windows database found the same two previously identified synthetic employee accounts under the checked E2E prefixes. Production data was outside that check.
- A Windows fail-fast repeat started the published Web API twice with invalid SQLite configuration. The runtime database mismatch and the EF design-time override each stopped startup with the expected guard error before creating any database file.
- The runtime advisory queries returned zero matches for the candidate npm runtime lock (`npm audit --omit=dev`) and for the resolved Web API NuGet graph including transitive packages (`dotnet list GF3.WebApi/WebApi.csproj package --vulnerable --include-transitive --no-restore`). These results cover the registries' reported advisories for those package selections.

Release status stays **BLOCKED** pending a clean complete E2E rerun, exact-head CI, physical-device verification, and the production maintenance and license gates. No draft PR was opened.

### Reproducing the remaining release checks

1. In `FrontEnd`, run `npm ci`, `npx playwright install chromium webkit` and `npm run test:e2e:prepare`. Run `npm run test:e2e -- --project=<project>` for `chromium`, `mobile-chrome`, `webkit-laptop` and `webkit-iphone-x`. Use distinct `GF3_E2E_PORT` values for concurrent runs. The runner stores results separately by port. In PowerShell, set `$env:GF3_RUN_LIVE_BROWSER='1'` and separately run `dotnet test GF3.Tests/GF3.Tests.csproj -c Release --filter 'FullyQualifiedName~ManualShiftHighlightTests.RealHttp_CrossRoleSettingAndAcceptance'` from the repository root to cover the existing opt-in host. Use separate physical iOS Safari and Android Chrome devices to verify installation and cold start, navigation, keyboard rotation, Guide, reduced motion and ordered photo/GIF.
2. Dispatch `CI` for the exact validated branch SHA and verify Build and test, Playwright smoke and Docker cutover, rollback and ingress. Review the license report against the corresponding published image and frontend artifact.
3. Under the separately reviewed and approved Plan 78 maintenance procedure, rehearse the whole seven-migration chain on a disposable clone with a synchronized SQLite and `communication-gifs` checkpoint taken under one verified writer fence. Compare attachment hashes and permissions, SQLite integrity, foreign keys, migration history and legacy rows. Exercise failed migration and activation, abort and restore before writes resume. Verify that a failure after the write fence opens cannot overwrite new data. Record the candidate SHA and all outcomes before authorizing PROD2.

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
# 2026-11-24 coordinated media backup and release-gate checkpoint

Candidate source baseline: `cb3376dffe1b6bcd2fea7d085f8e4af067c10115`. The changes in this checkpoint are uncommitted and have no remote CI result. No production or PROD2 host was accessed or modified.

- Application SQLite backups now publish versioned database/media bundles from an online SQLite snapshot, validate referenced immutable GIF files and hashes, and materialize selected archives into a separate restored workspace. Missing referenced media prevents publication. Host ordinary/fenced backup and recovery paths now include referenced/full media manifests and preserve media through restore; restore progress is journaled.
- The protected host tool recognizes only the pinned seven-migration transition and exact source blobs. Its migration and recovery validators deliberately fail closed because the eight immutable EF-generated schema-prefix contracts and corresponding chain rehearsal have not been completed. This transition is not release-ready.
- The five iOS and five Android Guide captures were replaced with original generic SVG illustrations. License policy and GSAP verdict were updated for commercial SaaS use; final built-artifact notices and native component obligations remain unverified.
- Validation completed: Release `dotnet build GF3.Tests/GF3.Tests.csproj --no-restore` passed with 0 warnings/errors; focused `dotnet test GF3.Tests/GF3.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~SqliteMediaBackupTests|FullyQualifiedName~SqliteAdministrationTests|FullyQualifiedName~DatabaseMigrationStartupTests"` passed 19/19; `python -m unittest discover -s deploy/security/tests -v` passed 61 tests with 4 platform skips; focused `python -m unittest deploy.security.tests.test_gf3_maintenance -v` passed 18/18; targeted Guide content Vitest passed 2/2; `git diff --check` passed. Full .NET, supported-Linux host, frontend build/lint/full Vitest, four-profile Playwright, Docker/artifact review, vulnerability scans, exact-head CI, and physical-device checks remain outstanding.
- Next engineering action: add and test the eight reviewed schema-prefix contracts, then run isolated full-chain migration/recovery rehearsal. Do not authorize merge/deploy until all mandatory engineering gates pass. Host runbook changes are procedural only and were not executed.

## Plan 79 continuation evidence

Feature work starts from `cb3376dffe1b6bcd2fea7d085f8e4af067c10115`; the only reviewed PROD2 migration baseline is `1175c39461e149a7bb35f428babed28279727899`. The previous feature tip's [CI run 45390273645](https://github.com/OlehProtsun/GF3/actions/runs/45390273645) succeeded. It is historical evidence. The candidate's exact dispatch and job conclusions belong in the PR or final execution report.

- The embedded contract covers eight EF schema prefixes and the seven pinned source/Designer pairs. In disposable Docker, the full chain and high-water check preserved legacy rows, history, constraints and media. The rehearsal rejected data drift, an unreviewed intermediate schema and a failure within the chain. HTTP/TLS failure, Git activation failure and recovery preserved the original SQLite volume and marker. The CI-only Linux kernel fence exercises also cover a failed Guard, interrupted restore between database and media installation, loss of the fence and failures after the write boundary.
- The Release backend suite passed 1936/1936 before the Linux portability adjustments to the fixtures. The Release build after those adjustments passed with no warnings/errors. An isolated Linux .NET run passed 33/33 covering the revised fixtures, schema history, backup, restore and authenticated media access. The opt-in real HTTP manual-shift Playwright scenario passed. The Linux Python suite passed 63/63 including rejection of missing, failed or wrong-SHA Playwright smoke jobs.
- Frontend lint reported 0 errors and 21 pre-existing warnings. Vitest passed 1545 with one existing skip. The initial Chromium run found five responsive assertion failures (485 passed, two skipped). The updated dock, manager phone layout and popup resize checks passed 24/24 across the four profiles. The full final four-profile matrix is a separate gate. Record only results from the final revision; do not mix in the earlier Chromium results.
- Production Linux amd64 Docker build passed (image ID `sha256:bdff4b1e97120735d9475fa0e8873f90003fb15c78ab183049fa5424d0f847cc`). The image uses the app account (UID 1654) and the HTTP healthcheck. The browser and server artifacts include their notices, both GSAP banners, Carlito OFL, the original SkiaSharp notices and the libwebp patent grant. The final browser bundle omits the removed platform screenshots and test assets. Runtime advisory checks found zero npm and NuGet advisories. See [license evidence and rights still to confirm](third-party-licenses.md).
- Before merge, confirm rights for flagged shipped assets and verify physical iOS Safari/installed PWA and Android Chrome/installed PWA using disposable staging accounts. Check installation, cold startup and splash, browser versus PWA launch logging, keyboard and rotation, Guide instructions and language, photo/GIF ordering, reduced motion and dismissal, plus schedule/correction/swap navigation. Before deployment, the operator separately confirms the exact PROD2 push CI, installed tools, active release, retained private backup and protected rehearsal on the host. [Operator runbook](gfctl-prod2-database-maintenance.md)

The [first feature dispatch](https://github.com/OlehProtsun/GF3/actions/runs/45436055783) passed Build and test (.NET 1936/1936, Linux host 64/64, Vitest 1545 with one opt-in private fixture skip), Playwright smoke (12/12) and Docker build. Its Linux rollback scenario exposed a fail-closed check after the correction-table rebuild, before the verifier restored the deleted ID high-water mark. The verifier now checks complete schema, history and existing rows before any sequence repair. Recovery accepts the original sequence or the exact EF rebuild value only after that migration committed, and verifies the live media stamp before restore. A final dispatch must confirm the corrected Linux cutover scenarios. The final Chromium run passed 490 with two conditional skips. One skip is the isolated manual shift acceptance that passed its independent opt-in run. The other is the narrow touch viewport scenario assigned to mobile profiles.
