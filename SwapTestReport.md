# Swap execution report — Plan (39)

## Source and environment

- Baseline and tested HEAD: `DEV2`, `a17e462d907c539d8e5265a969a07be8317b4a4e`; changes are uncommitted. Initial working tree was clean.
- Machine: LAPTOP-ANFS629E; Windows NT 10.0.19045.0, 12 logical processors.
- .NET SDK 10.0.401; EF Core / Microsoft.EntityFrameworkCore.Sqlite / Microsoft.Data.Sqlite 10.0.9; EF CLI 10.0.12; Node v22.14.0. Execution started October 24 and continued October 25, Europe/Warsaw. Dependencies installed with the existing npm lockfile; no package changes.
- SQLite tests use disposable files through `SqliteTestDatabase`. The baseline was extracted from HEAD to `%TEMP%/gf3-plan39-baseline-a17e462`; no branch or checkout changes.
- Browser projects: Chromium desktop, WebKit laptop, WebKit iPhone X, Mobile Chrome (Pixel 7 emulation). Emulation is not physical-device/PWA verification.

## Implemented behavior

- Independent per-schedule manual acceptance color, default `#BAE6FD`; EF upgrade/down migration; manager GET/PUT with edit-lock, validation, authorization and graph/container guards.
- Acceptance writes the recipient style and matching manual negative-ID Note style inside the existing transaction. Existing text colors, unrelated metadata, Give/Exchange colors, history, residual offers and notifications remain supported.
- Manual card uses the shared color dialog, dedicated query/cache key, confirmed-server color and translated EN/PL/UK labels.
- Source-tagged generated columns are first after Day in editor/profile, cannot be drag sources or targets, and retain styled empty cells through regeneration.
- Clean editors hydrate externally accepted changes. Before saving a draft, the editor checks the persisted Note/version timestamp and rejects stale data with a refresh message.

## Validation commands and results

Results below are execution evidence. `GF3.Tests/TestResults` contains local TRX/log/JSON files and is not a distributable dependency of this report.

| Command / suite | Pass / fail / skipped | Result / duration |
| --- | --- | --- |
| `git branch --show-current`, `git rev-parse HEAD`, `git status --short` | n/a | DEV2 / exact pinned SHA / initially clean |
| Baseline `dotnet test ... --filter "FullyQualifiedName~ShiftSwap\|FullyQualifiedName~ManualShiftDemand"` in isolated pinned source | 298 / 0 / 0 | PASS; 3m43s |
| Baseline focused frontend Vitest (six named model/card/API/query files) | 66 / 0 / 0 | PASS; 8.24s |
| `dotnet restore GF3.sln` | n/a | PASS |
| Final `dotnet build GF3.sln --no-restore` | n/a | PASS; zero warnings/errors; 4.58s |
| `npm ci` | n/a | PASS after releasing Vite esbuild lock; existing audit findings: 18 (2 low, 4 moderate, 12 high), no upgrades |
| `npm run lint` | n/a | PASS; zero errors, 16 warnings outside changed files |
| `npm run build` | n/a | PASS; 10.61s; SignalR annotation and large-bundle warnings |
| `npm test` | 909 / 0 / 0 | PASS; 107 files; 106.19s |
| Live HTTP plus backend benchmark invocation (`GF3_RUN_LIVE_BROWSER=1`, filter RealHttp/SwapRegressionBenchmark) | 3 / 0 / 0 | PASS; 2m19s; includes 2 real-browser cases (21.9s) |
| Generated responsive color flow: EN/PL/UK, Chromium + WebKit iPhone | 6 / 0 / 0 | PASS; 2.6m; mocked APIs, widths 320–1280 |
| Rejected color PUT in all four browser projects | 4 / 0 / 0 | PASS; 54.3s; error banner, dialog stays open, Escape cancel, confirmed Sky remains |
| Full backend suite, first completed run | 848 / 1 / 0 | 11m26s; old matching manual s background assertion failed, corrected to exact new color; successful final rerun below |
| Final manual-highlight + ContainerService focused run in isolated artifacts | 28 / 0 / 0 | PASS; 28s, includes real graph PUT persistence |
| Final manual setting/version/HTTP/browser and list-query-growth checks | 22 / 0 / 0 | PASS; 1m6s; final real-browser cases included; new query-growth case adds one to the 849-case full run |
| Full backend final rerun | 849 / 0 / 0 | PASS; 12.1913 minutes; final production changes |
| Initial three-file Playwright invocation (`--workers=2`) | incomplete | Stopped to reschedule serial-file work; no pass claim. The final three-file run includes these same cases and later added tests. |
| Initial full Playwright invocation (`--workers=2`) | incomplete | Stopped to reschedule independent test cases; initial discovery 1504 cases. Artifacts retained; no full-pass claim. |
| Final full Playwright (`--fully-parallel --workers=6`) | BLOCKED / incomplete | 1508 discovered, at least 96 started; stopped after confirming missing required `https://localhost:54294` service. No aggregate pass claim. |
| Final required three-file Playwright (`--fully-parallel --workers=6`) | 597 / 67 / 8 | FAILED; 672 cases; 43,494.332s recorded wall duration (12.08h), including five WebKit setup/teardown/test timeouts with >42,000s recorded case duration. Assertions/timeouts unchanged. |
| Sequential timeout recheck, WebKit laptop (`--workers=1`) | 5 / 0 / 0 | PASS; 80.387s; EN generated responsive flow, both combined-claim cases, EN/UK badge cases |
| Sequential timeout recheck, Chromium (`--workers=1`) | 1 / 0 / 0 | PASS; 15.817s; combined strict overlap |
| Sequential timeout recheck, Mobile Chrome (`--workers=1`) | 1 / 0 / 0 | PASS; 20.997s; combined partial/final claim |
| `git diff --check` (whole worktree after artifact restoration) | n/a | PASS; scoped 37-path inventory below |

## Coverage matrix

| ID | Area / executed cases | Evidence and persisted assertions |
| --- | --- | --- |
| B-GIVE | Public/private Give; full/prefix/suffix/middle/one-minute periods; owner and recipient guards; invalid time/calendar, changed source, overlap, duplicate open offer | [ShiftSwapGiveMatrixTests](GF3.Tests/ShiftSwapGiveMatrixTests.cs), [ShiftSwapSafetyTests](GF3.Tests/ShiftSwapSafetyTests.cs), [ShiftSwapSlotNoConflictTests](GF3.Tests/ShiftSwapSlotNoConflictTests.cs): minute conservation, adjacency, untouched-table snapshots on rejected operations, unique slot numbers, history/version counts. |
| B-EXCHANGE | Same/cross schedule; day/month/year relations; source/return periods; recipient/privacy, invalid/occupied/stale/locked/reserved return; cancellation and archive | [ShiftSwapExchangeMatrixTests](GF3.Tests/ShiftSwapExchangeMatrixTests.cs), [ShiftSwapExchangeTests](GF3.Tests/ShiftSwapExchangeTests.cs), [ShiftSwapLifecycleMatrixTests](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs): atomic assignments and both graph versions/events; no leaked private offer; original identities retained. |
| B-MANUAL | Public/private; strict/flexible; combined/duplicate/adjacent capacity; partial and final claim; consecutive recipients; invalid demand/cell; lock and cancellation | [ShiftSwapPartialClaimTests](GF3.Tests/ShiftSwapPartialClaimTests.cs): `Manager_PartialAndFinalClaims_PreserveResidualDemandAndMetadata`, `Manager_IndependentAndDuplicateCapacity_CancelReleasesOnlyReservation`; [ManualShiftDemandRulesTests](GF3.Tests/ManualShiftDemandRulesTests.cs); [ShiftSwapLifecycleMatrixTests](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs). |
| B-COLOR | Default/per-graph isolation; normalized round trip; invalid hex, wrong graph/container, lock; full/prefix/suffix/middle acceptance | [ManualShiftHighlightTests](GF3.Tests/ManualShiftHighlightTests.cs): `Setting_PersistsNormalizedColor_IndependentlyPerGraph`, `InvalidSetting_LeavesEveryTableUnchanged`, `MissingOrWrongContainer_LeavesEveryTableUnchanged`, `LockedSetting_LeavesEveryTableUnchanged`, `ManualAcceptance_HighlightsBothCells_AndPersistsHistory`. Recipient/style coordinates and ARGB match; no negative-FK rows; persisted Note equals latest version snapshot. |
| B-NOTE | Raw/encoded compact and legacy style precedence; repeat upsert; retained text color; missing/invalid coordinates; unrelated metadata | `NoteUpsert_UsesEffectiveRepresentation_PreservesTextAndMetadata` and `NoteUpsert_RejectsMissingColumnOrInvalidCoordinates` in [ManualShiftHighlightTests](GF3.Tests/ManualShiftHighlightTests.cs). Deep equality for m/g/o/t/u/unknown metadata, one effective style per coordinate. |
| B-FUTURE | Changing manual setting between residual acceptances affects future recipient and manual cell only | `Manager_PartialAndFinalClaims_PreserveResidualDemandAndMetadata` in [ShiftSwapPartialClaimTests](GF3.Tests/ShiftSwapPartialClaimTests.cs): previous recipient remains Sky; second recipient/manual cell become Peach; no open residual after final claim. |
| B-RACE | Two acceptors; accept versus cancel; duplicate manager publication; SQLite busy; slot unique constraint; failed history/version/slot write | [ShiftSwapSafetyTests](GF3.Tests/ShiftSwapSafetyTests.cs), [ShiftSwapPartialClaimTests](GF3.Tests/ShiftSwapPartialClaimTests.cs), [ShiftSwapLifecycleMatrixTests](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs): one winner, no duplicated history/assignment, full persisted-table equality after failure, no pre-commit notification. |
| B-MANAGER | Graph/container logs, manual publish/cancel, deleting accepted log | `Manager_delete_log_never_undoes_accepted_assignments`, `Manager_lock_rejects_manual_creation_without_writes`, and manager workflow cases in [ShiftSwapControllerWorkflowTests](GF3.Tests/ShiftSwapControllerWorkflowTests.cs). |
| B-SEQUENCE | Three deterministic seeds, forty controller operations each | `Forty_controller_operations_preserve_persisted_invariants` in [ShiftSwapInvariantSequenceTests](GF3.Tests/ShiftSwapInvariantSequenceTests.cs): persisted no-overlap, minute conservation, history/version deltas, rejected mutation equality. |
| B-MIGRATION | New database; existing database rollback, copy, upgrade; old ordinary color preserved; EF model drift | `Migration_UpgradeCopyAndRollback_PreservesExistingSchedule` in [ManualShiftHighlightTests](GF3.Tests/ManualShiftHighlightTests.cs); [DatabaseMigrationStartupTests](GF3.Tests/DatabaseMigrationStartupTests.cs). |
| F-SETTING | Dedicated endpoint; browser-rejected PUT preserves confirmed color and dialog; confirmed cache isolated from ordinary color; failed save keeps confirmed color; keyboard swatch and disabled unsaved graph | [shiftSwapsApi.test.ts](FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.test.ts), [queries.test.tsx](FrontEnd/src/entities/shift-swaps/api/queries.test.tsx), [ContainerGraphManualColumnsCard.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.test.tsx). |
| F-ORDER | Multiple/duplicate/removed generated IDs; idempotence; source tag rather than label; pinned drag source/drop target; old saved order in profile | [graphColumnOrder.test.ts](FrontEnd/src/entities/containers/model/graphColumnOrder.test.ts), [ContainerGraphMatrix.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.test.tsx), [ContainerGraphProfileWorkspace.phone.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.phone.test.tsx). |
| F-RETENTION | Zero/one/multiple vacancies, real calendar bounds, duplicate/adjacent capacity, reservation protection, repeated generate, save/reload, undo/session, accepted styled empty cell | [generatedManualShifts.test.ts](FrontEnd/src/entities/containers/model/generatedManualShifts.test.ts), [ContainerGraphEditPage.generatedManualShifts.test.tsx](FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.generatedManualShifts.test.tsx), [graphNote.test.ts](FrontEnd/src/entities/containers/model/graphNote.test.ts). Custom columns/text survive. |
| F-STALE | External acceptance hydrates clean editor; dirty stale Save fails before any workspace mutation | `externally accepted shift refreshes clean editor and blocks a stale dirty save` in [ContainerGraphEditPage.generatedManualShifts.test.tsx](FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.generatedManualShifts.test.tsx). |
| UI-LIVE | Real HTTP 401/403/400/404 plus persisted PUT, manual publish and employee acceptance; two independent browser auth contexts | `RealHttp_CrossRoleSettingAndAcceptance_PersistBothCells` in [ManualShiftHighlightTests](GF3.Tests/ManualShiftHighlightTests.cs) and `real backend manual acceptance persists and refreshes both colored cells` in [generated-manual-shifts.spec.ts](FrontEnd/e2e/generated-manual-shifts.spec.ts): Chromium and WebKit laptop, 390 px employee context; real SignalR updates and reload; exact computed colors on both manual/recipient td cells; no mocked routes. |
| UI-MOCKED | EN/PL/UK; 320/375/390/430/1280 px; bounded shift list; strict/partial claims and successor offers; exchange wizard/restoration | [generated-manual-shifts.spec.ts](FrontEnd/e2e/generated-manual-shifts.spec.ts), [swap-exchange-workflow.spec.ts](FrontEnd/e2e/swap-exchange-workflow.spec.ts), [swap-restoration.spec.ts](FrontEnd/e2e/swap-restoration.spec.ts). Route fixtures test browser interactions/geometry, not database persistence. |

The generated combined-claim WebKit laptop timeout passed unchanged on serial recheck (2/2, 1.1m); the initial aggregate failure remains recorded as a transient browser-run failure.

The executed per-method results below define the coverage actually observed; the matrix does not imply every Cartesian combination was tested. Native installed PWA, physical iPhone/Android, deployment and push delivery were not run.

| Coverage IDs | Executed result |
| --- | --- |
| B-GIVE, B-EXCHANGE, B-MANUAL, B-COLOR, B-NOTE, B-FUTURE, B-RACE, B-MANAGER, B-SEQUENCE, B-MIGRATION | PASS for the concrete backend cases listed below. |
| F-SETTING, F-ORDER, F-RETENTION, F-STALE | PASS in the completed 909-test frontend suite. |
| UI-LIVE | PASS, two browser projects against real HTTP/SQLite. |
| UI-MOCKED | MIXED; focused color/error checks pass, broad existing Swap assertions have failures detailed below. |
| Legacy service-dependent full browser suite | BLOCKED; required local API is not listening. |
| Physical-device/PWA/native push | NOT RUN. |

| Test method / parameter cases | Result | Passed / cases |
| --- | --- | ---: |
| [EmployeeShiftSwapVersionTests.Accept_AfterCheckout_BranchesFromCheckedOutParent](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 1 / 1 |
| [EmployeeShiftSwapVersionTests.Accept_ManagerOffer_VersionsAssignmentAndRemovedNoteCell](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 1 / 1 |
| [EmployeeShiftSwapVersionTests.Accept_PostCommitActionsFail_SwapAndVersionsRemainCommitted](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 1 / 1 |
| [EmployeeShiftSwapVersionTests.Accept_PreservesBeforeAndCompleteAfterSnapshots](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 4 / 4 |
| [EmployeeShiftSwapVersionTests.Accept_RejectedOrRepeated_DoesNotAppendVersions](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 5 / 5 |
| [EmployeeShiftSwapVersionTests.Accept_VersionInsertFails_RollsBackEntireTransaction](GF3.Tests/EmployeeShiftSwapVersionTests.cs) | PASS | 2 / 2 |
| [ManualShiftDemandRulesTests.Capacity_DuplicatesAndTouchingRanges](GF3.Tests/ManualShiftDemandRulesTests.cs) | PASS | 1 / 1 |
| [ManualShiftDemandRulesTests.Consume_DuplicateLeavesOtherUnit](GF3.Tests/ManualShiftDemandRulesTests.cs) | PASS | 1 / 1 |
| [ManualShiftDemandRulesTests.Consume_SubtractsOnlyClaimedUnit](GF3.Tests/ManualShiftDemandRulesTests.cs) | PASS | 4 / 4 |
| [ManualShiftDemandRulesTests.Invalid_RejectsEntireCell](GF3.Tests/ManualShiftDemandRulesTests.cs) | PASS | 6 / 6 |
| [ManualShiftDemandRulesTests.NoteConsumption_PreservesPrefixAndUnrelatedMetadata](GF3.Tests/ManualShiftDemandRulesTests.cs) | PASS | 4 / 4 |
| [ManualShiftHighlightTests.InvalidSetting_LeavesEveryTableUnchanged](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 3 / 3 |
| [ManualShiftHighlightTests.LockedSetting_LeavesEveryTableUnchanged](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 1 / 1 |
| [ManualShiftHighlightTests.ManualAcceptance_HighlightsBothCells_AndPersistsHistory](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 4 / 4 |
| [ManualShiftHighlightTests.Migration_UpgradeCopyAndRollback_PreservesExistingSchedule](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 1 / 1 |
| [ManualShiftHighlightTests.MissingOrWrongContainer_LeavesEveryTableUnchanged](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 1 / 1 |
| [ManualShiftHighlightTests.NoteUpsert_RejectsMissingColumnOrInvalidCoordinates](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 4 / 4 |
| [ManualShiftHighlightTests.NoteUpsert_UsesEffectiveRepresentation_PreservesTextAndMetadata](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 4 / 4 |
| [ManualShiftHighlightTests.RealHttp_CrossRoleSettingAndAcceptance_PersistBothCells](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 1 / 1 |
| [ManualShiftHighlightTests.Setting_PersistsNormalizedColor_IndependentlyPerGraph](GF3.Tests/ManualShiftHighlightTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptAdjacentCustomPeriod_AllowsTouchingTargetShiftWithoutOverlap](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptCanOnlySucceedOnce](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptCustomPeriod_PreservesOnlyTheUntransferredEdges](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 3 / 3 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptGeneratedManualOffer_PreservesProvenanceAndOtherMetadata](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptManagerCreatedOffer_AssignsSlotAndRemovesManualCell](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_AcceptRejectsOfferWhenSourceOwnershipChanged](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_CancelRequiresOwnerAndMarksOpenOfferCancelled](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_CreatePrivateOfferForEmployeeOutsideSchedule_AddsEmployeeWhenAccepted](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_CreateRejectsBadTimeTextAndOutsidePeriod](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_CreateRejectsInvalidSessionScheduleSlotOwnershipAndTarget](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_CreateVisibleAndAcceptPartialSwap_UpdateSlotsAndDtos](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_GetEmployees_ExcludesCurrentEmployeeAndSortsByName](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_GetVisible_AggregatesMonthAndBlocksOverlapAcrossSchedules](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_GetVisible_MarksOpenOffersLocked_WhenScheduleEditLockActive](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_RejectsDuplicateOpenOfferAndOverlappingAcceptance](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_RejectsLockedSchedulesAndInvalidPeriods](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.EmployeeShiftSwaps_TwelveSequentialCustomSwaps_PreserveHoursAndNeverOverlap](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_Container_CanListCancelAndDeleteEmployeeOfferWithoutDeletingSlot](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_Container_CancellingManagerOfferRemovesItsManualSlot](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_Container_DeleteAcceptedSwapRemovesRequestAndHistory](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_CreateListAndCancelManualOffers](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_CreateManualOfferRejectsDuplicateDayAndMissingTarget](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_HighlightSetting_RoundTripsPerGraph](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwapLogsController_ValidatesManualOfferInputsAndMissingGraph](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwaps_AreScopedBySharedContainerAcrossScheduleMonths](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapControllerWorkflowTests.ShiftSwaps_DisabledScheduleRejectsEmployeeCreateAcceptAndManagerManualOffer](GF3.Tests/ShiftSwapControllerWorkflowTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeMatrixTests.Calendar_and_graph_boundaries_preserve_dates_and_distinct_versions](GF3.Tests/ShiftSwapExchangeMatrixTests.cs) | PASS | 5 / 5 |
| [ShiftSwapExchangeMatrixTests.Candidates_paginate_over_31_rows_across_schedules_without_container_disclosure](GF3.Tests/ShiftSwapExchangeMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeMatrixTests.Invalid_exchange_has_no_persisted_mutation](GF3.Tests/ShiftSwapExchangeMatrixTests.cs) | PASS | 8 / 8 |
| [ShiftSwapExchangeMatrixTests.Only_target_can_accept_and_stale_return_rejects_atomically](GF3.Tests/ShiftSwapExchangeMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeMatrixTests.Pairwise_exact_transfers_preview_and_persistence](GF3.Tests/ShiftSwapExchangeMatrixTests.cs) | PASS | 10 / 10 |
| [ShiftSwapExchangeTests.Accept_FailureRollsBackBothSchedules](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 5 / 5 |
| [ShiftSwapExchangeTests.Accept_TransfersBothPeriodsAndVersions](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 6 / 6 |
| [ShiftSwapExchangeTests.AcceptanceRaces_CommitAtMostOnce](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeTests.Candidates_AuthorizedPaginatedAndPrivate](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.Candidates_ExposeShortSafeIntervalsWithoutIgnoringConflicts](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.CompletionSnapshots_SurviveRemovalOfBothSlotReferences](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeTests.Create_InvalidExchangeLeavesSchedulesUntouched](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 18 / 18 |
| [ShiftSwapExchangeTests.CrossMonthAndMinutePrecision_PreviewMatchesAcceptance](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 3 / 3 |
| [ShiftSwapExchangeTests.EditedSlot_RejectsAcceptanceEvenWhenTheSelectedPeriodStillFits](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 10 / 10 |
| [ShiftSwapExchangeTests.GivePreview_CountsUniqueDatesAndUntouchedRemaindersAcrossGraphs](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.GraphDelete_RejectsOpenExchangeOnEitherSide](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeTests.ManagerCancel_LeavesBothIntervalsUnchangedAndKeepsExchangeInLogs](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.Migration_UpgradeAndRollbackPreserveLegacyGiveHistoryAndIndexes](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.MissingReturnSlot_RemainsReadableAndCanBeCancelledWithOriginalDetails](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.Notifications_AfterCommitAndNeverRepeated](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeTests.ProjectedConflicts_MatchIndependentMinuteOccupancyReference](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.ProjectedEligibility_UsesStrictMinuteBoundariesForBothParticipants](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 6 / 6 |
| [ShiftSwapExchangeTests.ReservedReturn_CannotBecomeAnotherSourceOrReturn_AndPrivateReadsStayScoped](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.ReturnGraph_CheckoutBlockedWhileOpenAndArchivesBothSidesAfterAcceptance](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 1 / 1 |
| [ShiftSwapExchangeTests.SameDay_OutgoingIntervalsAreSubtractedBeforeConflictCheck](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 2 / 2 |
| [ShiftSwapExchangeTests.UnequalPeriods_PreviewCreateAndAcceptUseActualHours](GF3.Tests/ShiftSwapExchangeTests.cs) | PASS | 3 / 3 |
| [ShiftSwapGiveMatrixTests.Adjacent_is_allowed_overlap_is_rejected](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapGiveMatrixTests.Calendar_dates_are_validated_and_omitted_times_mean_full_slot](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 3 / 3 |
| [ShiftSwapGiveMatrixTests.Different_recipient_receives_public_or_private_give](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapGiveMatrixTests.Give_accept_rejects_source_edited_to_invalid_calendar_date_without_mutation](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapGiveMatrixTests.Give_exact_intervals_conservation_history_and_stats](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 6 / 6 |
| [ShiftSwapGiveMatrixTests.Invalid_time_does_not_mutate_any_rows](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 10 / 10 |
| [ShiftSwapGiveMatrixTests.Public_offer_does_not_cross_container_and_cancel_releases_reservation](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapGiveMatrixTests.Repeated_chain_and_split_remainder_preserve_all_minutes](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapGiveMatrixTests.Targeting_and_owner_rules](GF3.Tests/ShiftSwapGiveMatrixTests.cs) | PASS | 4 / 4 |
| [ShiftSwapInvariantSequenceTests.Forty_controller_operations_preserve_persisted_invariants](GF3.Tests/ShiftSwapInvariantSequenceTests.cs) | PASS | 3 / 3 |
| [ShiftSwapInvariantSequenceTests.ListQueryCount_DoesNotGrowWithOpenOfferCount](GF3.Tests/ShiftSwapInvariantSequenceTests.cs) | PASS | 1 / 1 |
| [ShiftSwapInvariantSequenceTests.SwapRegressionBenchmark_RecordsEquivalentSqliteFixtures](GF3.Tests/ShiftSwapInvariantSequenceTests.cs) | PASS | 2 / 2 |
| [ShiftSwapLifecycleMatrixTests.Competing_exchange_creations_claim_return_slot_exactly_once](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapLifecycleMatrixTests.Cross_graph_events_are_after_commit_and_failure_is_best_effort](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapLifecycleMatrixTests.Failed_write_rolls_back_every_table_and_sends_no_notification](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapLifecycleMatrixTests.Manager_delete_log_never_undoes_accepted_assignments](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapLifecycleMatrixTests.Manager_lock_rejects_manual_creation_without_writes](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapLifecycleMatrixTests.Manual_invalid_inputs_and_schedule_guards_leave_database_unchanged](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 5 / 5 |
| [ShiftSwapLifecycleMatrixTests.Manual_offer_accept_or_cancel_preserves_unrelated_assignments](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 4 / 4 |
| [ShiftSwapLifecycleMatrixTests.Opposite_reservations_block_give_and_exchange_until_cancelled](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 1 / 1 |
| [ShiftSwapLifecycleMatrixTests.Private_exchange_visibility_and_original_identity_survive_archiving](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapLifecycleMatrixTests.Two_recipients_or_owner_cancel_race_has_exactly_one_winner](GF3.Tests/ShiftSwapLifecycleMatrixTests.cs) | PASS | 2 / 2 |
| [ShiftSwapPartialClaimTests.CompetingPartialClaims_OneWinnerOneResidualOneHistory](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 2 / 2 |
| [ShiftSwapPartialClaimTests.Employee_PartialClaim_OffersOnlyUnclaimedPublishedTime](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 2 / 2 |
| [ShiftSwapPartialClaimTests.InvalidClaim_RollsBackEveryTable](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 6 / 6 |
| [ShiftSwapPartialClaimTests.Manager_DoublePublishRace_CannotOversubscribe](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 1 / 1 |
| [ShiftSwapPartialClaimTests.Manager_IndependentAndDuplicateCapacity_CancelReleasesOnlyReservation](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 1 / 1 |
| [ShiftSwapPartialClaimTests.Manager_PartialAndFinalClaims_PreserveResidualDemandAndMetadata](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 1 / 1 |
| [ShiftSwapPartialClaimTests.Migration_EmptyAndPopulatedUpgrade_DefaultStrictAndRollback](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 2 / 2 |
| [ShiftSwapPartialClaimTests.OccupiedSecondHalf_AdvisoryAndAcceptanceRespectFreeWindow](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 2 / 2 |
| [ShiftSwapPartialClaimTests.PartialWriteFailure_RollsBackSlotsOffersNoteHistoryAndVersions](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 4 / 4 |
| [ShiftSwapPartialClaimTests.Strict_CustomRejected_AndLegacyNoBodyAcceptsFullOffer](GF3.Tests/ShiftSwapPartialClaimTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Accept_AlreadyWorks_Rejects](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 5 / 5 |
| [ShiftSwapSafetyTests.Accept_FailedHistoryOrSlotWrite_RollsBackEntireClaim](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.Accept_NotTargetAndNoSharedPublicContainer_Denied](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.Accept_OpenOffer_PersistsExactlyOnce](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Accept_OwnerAcceptAndWrongAccount_Denied](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Accept_RacesOwnerCancel_OnlyOneTerminalState](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.Accept_SameOfferTwice_SecondIsConflict](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Accept_StaleSourceSlot_IsUnavailable](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 5 / 5 |
| [ShiftSwapSafetyTests.Accept_WhenDisallowedOrLockedOrUnpublished_Rejects](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 4 / 4 |
| [ShiftSwapSafetyTests.Cancel_OnlyOwnerCanCancelAndOnlyOnce](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Cancel_StaleTrackedOpenOffer_CannotOverwriteAccepted](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.Create_DuplicateOpenSlot_ExistingIndexEnforced](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.Create_InvalidTargetId_DoesNotPublish](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.Create_MalformedTime_IsRejected](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 7 / 7 |
| [ShiftSwapSafetyTests.Create_PartiallyProvidedPeriod_IsRejected](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 3 / 3 |
| [ShiftSwapSafetyTests.Create_ValidPeriod_IsAccepted](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 3 / 3 |
| [ShiftSwapSafetyTests.GetEmployees_ExcludesContactData_AndEnforcesHttpRolePolicy](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.ManagerCancel_RacesAccept_PreservesWinningState](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSafetyTests.ManagerManualNote_MalformedMetadataFailsSafely](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 4 / 4 |
| [ShiftSwapSafetyTests.ManagerManualNote_ValidCellIsConsumedWithHistoryAndHighlight](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSafetyTests.WriterBusy_ReturnsControlledConflictWithoutMutation](GF3.Tests/ShiftSwapSafetyTests.cs) | PASS | 3 / 3 |
| [ShiftSwapSlotNoConflictTests.Accept_AlreadyAcceptedRequest_DoesNotTransferTwice](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_FullSlotSwap_PreservesSlotNumber](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_ManagerCreatedOpenShift_PreservesExistingPath](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_ManagerLockedSchedule_RejectsWithoutMutation](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_MiddlePartialSwap_AllocatesBothRemainders](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_NotIntendedPrivateRecipient_RejectsWithoutMutation](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_PartialPrivateF35Swap_ReallocatesConflictingSlotNumbers](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_PartialSwap_PersistsAcceptedRequestAndHistory](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_PrefixPartialSwap_AllocatesRemainderPosition](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_RecipientAlreadyWorksOfferedTime_RejectsWithoutMutation](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 1 / 1 |
| [ShiftSwapSlotNoConflictTests.Accept_StaleOrEditedSourceSlot_RejectsWithoutMutation](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 2 / 2 |
| [ShiftSwapSlotNoConflictTests.Sqlite_UniqueSlotIndexes_RemainEnforced](GF3.Tests/ShiftSwapSlotNoConflictTests.cs) | PASS | 2 / 2 |

## Defects, fixes and deviations

1. Manual acceptance previously used ordinary swap color and never styled the source manual cell. Reproduce by publishing a manual period and accepting it as an employee. Fix: dedicated schedule setting, transactional Note style upsert, manual color selection for real recipient. Regressions: `ManualAcceptance_HighlightsBothCells_AndPersistsHistory`, partial/final claim, and live browser tests. Severity: functional.
2. Generated columns could appear after employee/custom columns and be dragged away. Fix: one source-based pinning helper after sanitization and reconciliation, with matrix drag restrictions. Regressions: pure order, matrix, editor and profile tests. Severity: functional UI.
3. Reconciliation dropped empty accepted generated columns/styles. Fix: style-retained IDs join existing reservation protection; no synthetic demand is added. Regression: `accepted style retains empty generated columns without reviving demand`, editor styled-empty test. Severity: persisted visual state.
4. Dirty manager drafts could save stale Note after external acceptance. Fix: existing query hydration for clean drafts plus persisted graph Note/timestamp preflight for Save. Regression: external acceptance/stale Save test. Severity: data correctness.
5. During retention validation, a surviving legacy generated column became canonical only on the next generate and changed its label. Normalize the first surviving canonical label immediately; repeated reconciliation is stable. Covered by the empty-retention idempotence test.
6. Two existing manual workflow color expectations changed from Mint (`-4458544`) to the approved independent Sky default (`-4528387`). Assertions remain exact; the generated-manual metadata assertion now verifies the new matching s entry coordinates/color while preserving g/o/t/u/extra. Give/Exchange tests keep their ordinary color expectations.
7. There is no `en.json` in this checkout. English remains the existing `t()` source-string fallback; added translations only to existing `pl.json` and `uk.json`. No second localization system or new locale file.
8. Graph Save reset separately configured manual/ordinary colors because the contract mapper creates a fresh DAL model. `ContainerService.UpdateGraphAsync` now carries both settings from the already ownership-checked persisted graph, without extra queries or contract changes. `RealHttp_CrossRoleSettingAndAcceptance_PersistBothCells` now sets both colors, performs the real graph PUT, verifies both settings and accepts the manual offer. This directly necessary persistence fix adds `BusinessLogicLayer/Services/ContainerService.cs` to the planned file scope.
9. Added a dependency-free benchmark script and SQL-counter benchmark cases in the existing invariant test class. No production profiling dependency, API/schema expansion beyond the planned color column/routes, CI edit, branch switch, commit or push.

## Performance evidence

Pure frontend command, from `FrontEnd`: `node scripts/benchmark-manual-shifts.mjs`. One warmup, five timed repetitions, deterministic output comparison. Fixtures: 31 days, two templates, 10/50/100 employees, staffing 0/25/100%, people-per-shift 1/2/5; reconciliation and option enumeration with 1/5/20 columns and 0/100/500 reservations. Median and observed maximum are reported; five samples do not establish a useful p95 or SLA.

Backend command: `dotnet test GF3.Tests/GF3.Tests.csproj --no-restore --filter FullyQualifiedName~SwapRegressionBenchmark`. Same benchmark source was copied into the isolated pinned baseline for comparison. Debug/.NET 10/SQLite; fresh equivalent fixture per repetition, one warmup, five measured repeats, 31 days and 10/50 total employees across the related schedules (8/48 initially enrolled in the source schedule). SQL interception counts statements in each measured controller operation, excluding fixture setup and invariant checks. No security caching or eligibility shortcut was introduced.

| Backend operation | Employees | Baseline median ms | Final median ms | Baseline/final SQL statements |
| --- | ---: | ---: | ---: | --- |
| employee-list | 10 | 27.122 | 29.434 | 2 / 2 (constant in all five repeats) |
| employee-list | 50 | 1205.922 | 1265.307 | 2 / 2 (constant in all five repeats) |
| exchange-accept | 10 | 104.306 | 133.642 | 58 / 58 (constant in all five repeats) |
| exchange-accept | 50 | 2909.754 | 3126.514 | 58 / 58 (constant in all five repeats) |
| graph-log | 10 | 3.328 | 3.875 | 4 / 4 (constant in all five repeats) |
| graph-log | 50 | 13.567 | 14.092 | 4 / 4 (constant in all five repeats) |
| manual-full-accept | 10 | 86.200 | 88.816 | 33 / 33 (constant in all five repeats) |
| manual-full-accept | 50 | 3041.872 | 2992.685 | 33 / 33 (constant in all five repeats) |
| manual-partial-accept | 10 | 85.656 | 102.479 | 37 / 37 (constant in all five repeats) |
| manual-partial-accept | 50 | 2801.921 | 2768.719 | 37 / 37 (constant in all five repeats) |
| manual-post | 10 | 25.517 | 23.420 | 5 / 5 (constant in all five repeats) |
| manual-post | 50 | 843.129 | 844.737 | 5 / 5 (constant in all five repeats) |

In-process pure-function comparison: `node scripts/benchmark-manual-shifts.mjs --compare-baseline`. The script temporarily loads the exact HEAD source and removes that scratch module in `finally`. Both variants run against the same fixtures under the same machine load.

| Pure fixture | Baseline median ms | Final median ms |
| --- | ---: | ---: |
| vacancies/10/1/0 | 7.001 | 4.313 |
| vacancies/10/1/25 | 4.663 | 5.653 |
| vacancies/10/1/100 | 5.283 | 6.057 |
| vacancies/10/2/0 | 5.532 | 4.198 |
| vacancies/10/2/25 | 4.230 | 6.304 |
| vacancies/10/2/100 | 8.772 | 4.336 |
| vacancies/10/5/0 | 4.177 | 4.544 |
| vacancies/10/5/25 | 3.862 | 4.049 |
| vacancies/10/5/100 | 7.523 | 4.004 |
| vacancies/50/1/0 | 18.379 | 13.200 |
| vacancies/50/1/25 | 16.404 | 13.128 |
| vacancies/50/1/100 | 14.412 | 13.176 |
| vacancies/50/2/0 | 8.825 | 12.252 |
| vacancies/50/2/25 | 12.224 | 14.458 |
| vacancies/50/2/100 | 12.803 | 21.789 |
| vacancies/50/5/0 | 18.547 | 18.777 |
| vacancies/50/5/25 | 22.716 | 23.407 |
| vacancies/50/5/100 | 22.665 | 21.610 |
| vacancies/100/1/0 | 46.851 | 30.098 |
| vacancies/100/1/25 | 35.485 | 49.128 |
| vacancies/100/1/100 | 40.332 | 33.279 |
| vacancies/100/2/0 | 28.556 | 27.894 |
| vacancies/100/2/25 | 35.752 | 35.847 |
| vacancies/100/2/100 | 41.653 | 33.511 |
| vacancies/100/5/0 | 28.625 | 35.243 |
| vacancies/100/5/25 | 41.115 | 39.767 |
| vacancies/100/5/100 | 34.869 | 37.546 |
| reconcile/1/0 | 0.023 | 0.022 |
| reconcile/1/100 | 12.969 | 16.313 |
| reconcile/1/500 | 53.617 | 56.906 |
| reconcile/5/0 | 0.006 | 0.007 |
| reconcile/5/100 | 15.215 | 13.283 |
| reconcile/5/500 | 31.857 | 28.558 |
| reconcile/20/0 | 0.021 | 0.019 |
| reconcile/20/100 | 28.937 | 31.545 |
| reconcile/20/500 | 70.046 | 64.929 |

SQL statement counts did not grow with employee count. The dense 50-employee fixture is already slow on baseline (~0.8s manual publication, ~1.2s visible list, ~3s acceptance); it has large eagerly loaded schedule data, without increasing statement count. Final medians are comparable. No measured material regression requiring an algorithm/query rewrite was established. Browser suites ran concurrently, so these wall times are regression observations, not isolated throughput/SLA numbers. The additional `ListQueryCount_DoesNotGrowWithOpenOfferCount` regression verifies one versus twenty valid open manual offers with SQL statements **2 versus 2** and unchanged persisted tables. Process memory was not measured; p95 was not inferred from five observations.

Option enumeration uses the unchanged `manualShiftIntervals.ts`; measurements range with concurrent browser load. Original baseline and later standalone final 20-column/500-reservation medians were 165.653 ms and 350.149 ms; the in-process run observed 358.894 ms. This is recorded as load-sensitive, not evidence of an implementation change in that function.

## Earlier execution environment and verification (before follow-up)

- The full browser suite also includes older live-service tests using Vite's proxy target `https://localhost:54294`. An unsandboxed health request confirmed connection refused. Their helper login/fixture failures are environment-blocked, not proof of a product regression. The required manager/employee acceptance flow uses its own successfully tested real HTTP/SQLite host and is independent of this unavailable service.

- Sandboxed Vitest failed at esbuild startup with `spawn EPERM`; the authorized unsandboxed run passed. The first sandboxed backend run stalled and was terminated; the exact baseline was subsequently rerun successfully in an isolated directory.
- First EF migration command reported build failure while baseline output was in use; an explicit successful backend build followed by EF `--no-build` generated the inspected migration and designer.
- Initial `npm ci` failed because the existing Vite server on 5188 held `esbuild.exe`. The server was temporarily stopped, `npm ci` completed, and the same Vite command/port was restored hidden. No dependency upgrades were performed.
- Lint warnings and build bundle warnings are recorded separately from errors. No timeout/assertion weakening or CI changes.
- Playwright's default output cleanup removed tracked historical test artifacts. Those task-caused changes are restored before completion; subsequent output is directed to ignored `GF3.Tests/TestResults` directories.

## Earlier acceptance status (before follow-up)

- PASS: exact DEV2 baseline, scoped changes, no dependency upgrades or unrelated refactor.
- PASS: independent persisted manual color, accessible shared picker, EN/PL/UK labels, manager authorization/validation/lock guards and unchanged rejected-write snapshots.
- PASS: migration fresh/upgrade/copy/rollback/default/model-drift checks; ordinary color preserved.
- PASS: full/partial acceptance colors both correct coordinates, no negative style FK, Note metadata/text colors preserved, real HTTP and browser reload verified.
- PASS: existing Give/Exchange color and transaction/history/version/notification invariants in executed backend suites.
- PASS: generated-source pinning, drag restrictions, non-generated order, styled-empty retention, deterministic regeneration, clean refresh and stale dirty Save protection in unit/integration tests.
- PASS: recorded regression measurements and SQL counts; no demonstrated material algorithmic regression. Memory and meaningful p95 NOT MEASURED.
- PASS: backend build, 849-case full backend run plus additional query-growth test, 909 frontend tests, lint/build, focused color/error browser checks and real-backend cross-role flow.
- INCOMPLETE: broad browser acceptance. Final three-file results are recorded below; the complete browser suite requires the unavailable legacy local API and has reproduced baseline failures. This report does not claim every browser case passed or that every Cartesian use-case combination was tested.
- NOT RUN: physical devices, installed PWA, deployment and native push delivery.


### Reproduced baseline browser failures

Exact pinned frontend and pinned test sources were served at an isolated 5190 dev server with the same locked dependency tree. Command: `npx playwright test e2e/generated-manual-shifts.spec.ts e2e/swap-restoration.spec.ts e2e/responsive-text.spec.ts e2e/session-errors.spec.ts --grep "manager combined|restored swap renders|manager profile truncates|HTTP 401" --project=webkit-laptop --project=webkit-iphone-x --workers=1`. Result: 3 passed, 9 failed, 268.26s.

| Project | Baseline case | Result |
| --- | --- | --- |
| webkit-iphone-x | manager combined offer and occupied recipient partial and final claim | FAILED |
| webkit-iphone-x | manager combined offer and occupied recipient strict overlap | PASSED |
| webkit-laptop | manager combined offer and occupied recipient partial and final claim | PASSED |
| webkit-laptop | manager combined offer and occupied recipient strict overlap | PASSED |
| webkit-iphone-x | manager profile truncates long identity text inside its section | FAILED |
| webkit-laptop | manager profile truncates long identity text inside its section | FAILED |
| webkit-iphone-x | profile handles HTTP 401 without an endless loading state | FAILED |
| webkit-laptop | profile handles HTTP 401 without an endless loading state | FAILED |
| webkit-iphone-x | restored swap renders and opens panels in en | FAILED |
| webkit-iphone-x | restored swap renders and opens panels in pl | FAILED |
| webkit-laptop | restored swap renders and opens panels in en | FAILED |
| webkit-laptop | restored swap renders and opens panels in pl | FAILED |

Baseline issues: profile truncation expects ellipsis but receives clip; session error test cannot find Sign in; restoration EN expects the old “Select a shift” region while the current baseline exposes “Select your shift”; restoration landscape expects near-full viewport width while the established compact stage is 422 px. These are recorded as existing test/UI contract mismatches; changing unrelated profile/session/Swap layout is outside this plan. The isolated baseline iPhone combined claim timed out at click; laptop strict/partial passed, and final laptop serial recheck passed 2/2. No assertion or timeout was relaxed.

Four additional unchanged Swap cases were reproduced against the same pinned baseline with `--project=webkit-laptop --workers=1 --grep "compact offers and review details 390x844|details lock eligibility|History viewport and independent scrolling 390x844|short History uses natural card height with 1 entries"`: **0 passed, 4 failed, 72.416s**. Evidence: `playwright-baseline-details.json` and `baseline-details/`.

| Baseline case | Observed existing failure | Disposition |
| --- | --- | --- |
| compact offers and review details 390x844 | Receive badge expects `rgb(19, 128, 90)`; existing CSS yields `rgb(0, 132, 95)` | Existing visual assertion mismatch; no palette redesign. |
| details lock eligibility, restore History and show creator transfer direction | Expects two modal dialogs after History detail; existing behavior has one | Existing UI/test contract mismatch; no dialog navigation redesign. |
| History viewport and independent scrolling 390x844 | History list exceeds its horizontal client width | Reproduced baseline layout defect; unresolved. |
| short History uses natural card height with 1 entries | History list exceeds its horizontal client width | Same reproduced baseline layout defect; unresolved. |

At that point History overflow remained a known existing defect, and the original implementation had not changed production Swap/History layout files. The follow-up investigation below fixes this defect and records its regression results.

## Earlier completed browser-suite results (597/67/8)

Command, from `FrontEnd`: `PLAYWRIGHT_BASE_URL=http://127.0.0.1:5188 npx playwright test e2e/generated-manual-shifts.spec.ts e2e/swap-exchange-workflow.spec.ts e2e/swap-restoration.spec.ts --fully-parallel --workers=6 --reporter=line,json --output=../GF3.Tests/TestResults/playwright-focused-final`. JSON reporter output was directed to `playwright-focused-final.json`; route fixtures supply backend responses. This completed run is FAILED, independently of later focused rechecks.

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| WebKit iPhone X | 151 | 15 | 2 |
| WebKit laptop | 146 | 20 | 2 |
| Chromium | 150 | 16 | 2 |
| Mobile Chrome | 150 | 16 | 2 |
| Total | 597 | 67 | 8 |

| Failing family | Cases | Observed classification |
| --- | ---: | --- |
| compact offers and review details, six widths/four projects | 24 | Receive badge color mismatch; one iPhone landscape click timeout. Representative baseline color failure reproduced; every width/project variant was not separately rerun on baseline. |
| details lock eligibility / History / creator direction | 4 | Existing one-versus-two-modal assertion mismatch, reproduced on baseline. |
| History viewport / independent scrolling, four sizes/four projects | 16 | Existing History list horizontal overflow, representative baseline case reproduced. |
| short History with one/four entries, four projects | 8 | Same existing History overflow, representative baseline case reproduced. |
| restored Swap EN/PL, four projects | 8 | Existing region-name/compact-width mismatches, reproduced on baseline WebKit laptop/iPhone. |
| Generated responsive EN, combined claims and badge setup/teardown | 7 | Timeouts; isolated unchanged sequential rechecks recorded separately below. |

The eight skips are the environment-gated real-backend case in all four ordinary projects and the pre-existing skipped release-notice case in all four projects. The real-backend case was separately executed successfully on Chromium and WebKit laptop through its dedicated real HTTP host. No skipped case is counted as passed.

The completed suite therefore does not establish a green broad browser acceptance gate. Baseline reproduction identifies existing failure families; it does not prove every individual failure variant has the same root cause. Existing History overflow and stale visual/navigation assertions remain explicitly unresolved. No assertions, global timeouts or CI configuration were relaxed.

All **seven timeout cases passed unchanged on sequential recheck**: five WebKit laptop cases in 80.387s, Chromium combined strict overlap in 15.817s, and Mobile Chrome combined partial/final claim in 20.997s. Evidence: `playwright-timeout-recheck-webkit.json`, `playwright-timeout-recheck-chromium.json`, `playwright-timeout-recheck-mobile.json`. Each invocation used `--workers=1` and the same 5188 frontend, without altering implementation, assertions or timeouts. The original completed run still has 67 failures; rechecks do not rewrite that result. The remaining 60 failures belong to the existing visual/History/restoration families above.

## Changed files

- [BusinessLogicLayer/Services/ContainerService.cs](BusinessLogicLayer/Services/ContainerService.cs)
- [DataAccessLayer/Migrations/20261024211323_AddManualShiftHighlightColor.cs](DataAccessLayer/Migrations/20261024211323_AddManualShiftHighlightColor.cs)
- [DataAccessLayer/Migrations/20261024211323_AddManualShiftHighlightColor.Designer.cs](DataAccessLayer/Migrations/20261024211323_AddManualShiftHighlightColor.Designer.cs)
- [DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs](DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs)
- [DataAccessLayer/Models/DataBaseContext/AppDbContext.cs](DataAccessLayer/Models/DataBaseContext/AppDbContext.cs)
- [DataAccessLayer/Models/ScheduleModel.cs](DataAccessLayer/Models/ScheduleModel.cs)
- [FrontEnd/e2e/generated-manual-shifts.spec.ts](FrontEnd/e2e/generated-manual-shifts.spec.ts)
- [FrontEnd/scripts/benchmark-manual-shifts.mjs](FrontEnd/scripts/benchmark-manual-shifts.mjs)
- [FrontEnd/src/entities/containers/model/generatedManualShifts.test.ts](FrontEnd/src/entities/containers/model/generatedManualShifts.test.ts)
- [FrontEnd/src/entities/containers/model/generatedManualShifts.ts](FrontEnd/src/entities/containers/model/generatedManualShifts.ts)
- [FrontEnd/src/entities/containers/model/graphColumnOrder.test.ts](FrontEnd/src/entities/containers/model/graphColumnOrder.test.ts)
- [FrontEnd/src/entities/containers/model/graphColumnOrder.ts](FrontEnd/src/entities/containers/model/graphColumnOrder.ts)
- [FrontEnd/src/entities/containers/ui/ContainerGraphEditor.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphEditor.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.module.css](FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.module.css)
- [FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.test.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphManualColumnsCard.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.test.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphMatrix.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.phone.test.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.phone.test.tsx)
- [FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.tsx](FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.tsx)
- [FrontEnd/src/entities/shift-swaps/api/queries.test.tsx](FrontEnd/src/entities/shift-swaps/api/queries.test.tsx)
- [FrontEnd/src/entities/shift-swaps/api/queries.ts](FrontEnd/src/entities/shift-swaps/api/queries.ts)
- [FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.test.ts](FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.test.ts)
- [FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.ts](FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.ts)
- [FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.generatedManualShifts.test.tsx](FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.generatedManualShifts.test.tsx)
- [FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx](FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx)
- [FrontEnd/src/shared/api/queryKeys.ts](FrontEnd/src/shared/api/queryKeys.ts)
- [FrontEnd/src/shared/i18n/pl.json](FrontEnd/src/shared/i18n/pl.json)
- [FrontEnd/src/shared/i18n/uk.json](FrontEnd/src/shared/i18n/uk.json)
- [GF3.Tests/ManualShiftHighlightTests.cs](GF3.Tests/ManualShiftHighlightTests.cs)
- [GF3.Tests/ShiftSwapControllerWorkflowTests.cs](GF3.Tests/ShiftSwapControllerWorkflowTests.cs)
- [GF3.Tests/ShiftSwapInvariantSequenceTests.cs](GF3.Tests/ShiftSwapInvariantSequenceTests.cs)
- [GF3.Tests/ShiftSwapPartialClaimTests.cs](GF3.Tests/ShiftSwapPartialClaimTests.cs)
- [GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs](GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs)
- [GF3.WebApi/Controllers/ShiftSwapLogsController.cs](GF3.WebApi/Controllers/ShiftSwapLogsController.cs)
- [GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs](GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs)
- [SwapTestReport.md](SwapTestReport.md)

## Failed Playwright Tests — Investigation and Fixes

Follow-up execution on DEV2 at the same HEAD, preserving all 37 original uncommitted paths. The preceding report describes the earlier run and is retained as historical evidence. Results in this section supersede its unresolved Swap classifications where explicitly verified.

| Original category | Original failures | Actual cause and classification | Correction |
| --- | ---: | --- | --- |
| A: Receive badge / compact details | 24 | Stale wizard-palette expectations after the intentional details redesign in a17e462. The details stylesheet defines green Receive (`#00845f` on `#ddf6ec`) and purple Give (`#6655b6` on `#eef0ff`). A second, genuine landscape hit-testing defect was exposed once earlier assertions passed. | Keep current details palette; assert exact text AND background colors. For short landscape viewports, arrange search/filters on one row and keep the list inside the backdrop's interactive area above the History cutout. No forced clicks. |
| B: History horizontal overflow | 24 | Genuine baseline product bug. `.offerMeta` had `width: 100%` plus 32px padding in content-box sizing. At 320px, the History scroller had clientWidth 233px and scrollWidth 249px. | Set border-box sizing on the existing scoped metadata rule. Preserve wrapping, vertical scrolling, native dialog sizing and all width assertions. |
| C: History/details dialogs | 4 | Stale expectation. `handleViewDetails` explicitly suspends History before opening details; `closeDetails` restores History. Existing unit tests already require restored History, scroll locks and focus. | Assert one native modal, hidden History during details, then visible History and restored launcher focus after Escape. Eligibility/creator/transfer assertions retained. |
| D: restored Swap EN/PL | 8 | Stale accessible name and width expectations. The current picker is “Select your shift”; the workspace explicitly caps at 430px with 4px gutters. | Use current accessible names; assert both lower and upper width bounds `min(viewport, 430) - 8`. Preserve picker, modal, navigation, scroll and viewport assertions. |
| E: browser timeouts | 7 | Five WebKit cases recorded >42,000 seconds despite 60/90-second configured limits, proving a prolonged execution interruption. Two generated-flow click timeouts were non-reproducing in the prior isolated rechecks. Newly reachable History geometry also sampled an unfinished entry animation. Fixtures use independent Page/BrowserContext state. | Bound default workers to two (CLI overrides preserved); wait for actual History animations to finish before comparing header positions. No global timeout increase, fixed sleep, retries or disabled cases. Sequential and controlled parallel results recorded below. |

### UI reproduction and regression evidence

- History overflow: open Swap History at 320px with one accepted offer whose schedule name is `Long employee name — Aleksandra Maria Kowalska-Wisniewska`. The metadata box previously exceeded its parent by 16px. `short History` and `History viewport` retain exact `scrollWidth <= clientWidth`, long-name, list-scroll, fixed-header, focus and background-restoration checks. Original diagnostic: `fix-investigation.log`; before screenshot in `fix-investigation/`.
- Short landscape hit target: open `/swap` at 667x375 with three offers, open/close the first offer, then click the second. The original scroller extended into the decorative bottom notch; `.swapStage` intercepted pointer events. The compact toolbar and reserved bottom area keep the list reachable. `fix-families-second/...667x375.../compact-exchange.png` shows the corrected layout; clicks run normally without `force` or coordinate bypasses.
- History header animation: open History, open its offer details, return with Escape, scroll to the last entry. Comparing the header during the replayed 640ms entry animation produced a 0.513px discrepancy. Tests now await `getAnimations(...).finished` before each baseline/restored measurement; the original 0.5px tolerance remains unchanged.
- First corrected-family run: 27 passed / 5 failed / 0 skipped. It exposed the landscape hit target and unfinished-animation measurements beyond the original blocking assertions. After those fixes, the focused landscape/History rerun passed 10/10 (41.6s), Chromium plus iPhone WebKit.

### Backend service and test isolation

The expected API endpoint comes from `GF3.WebApi/Properties/launchSettings.json`; Vite proxies `/api` to `https://localhost:54294` with the local development certificate. Started the actual backend with:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__Default='Data Source=C:/Users/Oleg/Desktop/CSharp/GF3/GF3/GF3.Tests/TestResults/swap-browser-fixes.sqlite'
dotnet run --project GF3.WebApi/WebApi.csproj --no-build --no-launch-profile --urls=https://localhost:54294
```

The new isolated database was migrated by normal application startup. `curl.exe -k https://localhost:54294/api/health` returned HTTP 200. No production/user database, database-selection setting or external service was modified. Browser output now defaults to ignored `GF3.Tests/TestResults/playwright`, preventing cleanup of tracked historical `FrontEnd/test-results` artifacts.

Mocked UI suites use their existing route fixtures. The real authenticated manager/employee acceptance test starts its own real HTTP/SQLite host and runs two independent browser contexts. Legacy integration suites use the actual local backend above; external SMTP/native push/device checks are not substituted with mocked integration claims.

### Follow-up commands and completed results

| Command / suite | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| `dotnet build GF3.sln` | n/a | 0 | n/a | 7.38s; zero warnings/errors; `swap-fixes-backend-build.log` |
| `dotnet test GF3.sln` | 850 | 0 | 0 | 6m26s; `swap-fixes-backend.trx` |
| Badge/details family, four projects, workers=2 | 24 | 0 | 0 | `swap-fixes-family-badges.json` |

Further completed browser and frontend results are appended below; no running or unexecuted command is counted as passed.

| Completed follow-up verification | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| badges family, all four projects, workers=2 | 24 | 0 | 0 | 130.542s; `swap-fixes-family-badges.json` |
| history family, all four projects, workers=2 | 28 | 0 | 0 | 167.764s; `swap-fixes-family-history.json` |
| modals family, all four projects, workers=2 | 4 | 0 | 0 | 11.687s; `swap-fixes-family-modals.json` |
| restoration family, all four projects, workers=2 | 8 | 0 | 0 | 33.402s; `swap-fixes-family-restoration.json` |
| EN/PL full restored-picker workflow, all projects | 8 | 0 | 0 | 29.3s; `swap-fixes-restoration-expanded.json` |
| `npm run lint` | n/a | 0 | n/a | 16 existing warnings, no errors |
| `npm run build` | n/a | 0 | n/a | 16.06s; existing bundle warnings |
| `npm test` | 909 | 0 | 0 | 107 files, 212.82s; `swap-fixes-frontend-tests.log` |
| Real HTTP manager/employee + real browser acceptance | 1 backend + 2 browser | 0 | 0 | `swap-fixes-live-http.trx`, `manual-live-browser.log`; Chromium and WebKit |

### Legacy real-backend fixture corrections

The unavailable API was only the first legacy-suite blocker. The shared `e2e/helpers.ts` login helper still selected English-only guest headings and treated the real `type=password` field as an implicit textbox. Guest login now intentionally defaults to Polish. The helper now accepts current EN/PL accessible labels, selects PC password entry through the UI, locates the password by its label, and selects the actual PC workspace button. After real authentication it persists the English preference through the actual `/api/account-language` endpoint, reloads, and verifies `html[lang=en]`, matching these legacy fixtures' explicit English assertions. Authentication, authorization and manager-mode exchange are not mocked or bypassed. Completed real-service smoke, graph checks and full-suite outcomes are recorded below.

Read-only resource snapshot during the sequential run: 16,215 MB physical RAM, 2,320 MB free, CPU load 21%. This supports bounded browser concurrency on this host, but does not reconstruct historical memory usage or prove the precise cause of the two original 10-second click timeouts. The five >42,000-second case records establish interrupted execution; those are not valid normal-operation latency measurements.

Follow-up changed paths (in addition to the preserved original implementation): `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.module.css`, `FrontEnd/e2e/swap-exchange-workflow.spec.ts`, `FrontEnd/e2e/swap-restoration.spec.ts`, `FrontEnd/e2e/helpers.ts`, `FrontEnd/e2e/manager-layout.spec.ts`, `FrontEnd/e2e/employee-swap-deeplink.spec.ts`, `FrontEnd/e2e/employee-notifications.spec.ts`, `FrontEnd/e2e/schedule-hero.spec.ts`, `FrontEnd/playwright.config.ts`, and this report. The independent Manual color, Note persistence, generated-column retention/pinning and stale-editor implementation has not been reverted or rewritten.

| Later completed verification | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Complete focused suite, `--workers=1` | 664 | 0 | 8 | 4,113.072s; `swap-fixes-focused-sequential.json`; no flaky outcomes |
| Real-backend login, language persistence and sidebar interaction smoke | 1 | 0 | 0 | `swap-fixes-real-login-final.json`; actual local backend, no auth mocks |

The initial real-service smoke authenticated successfully and persisted English, then exposed an obsolete unscoped `aside` selector: the current page has navigation, System news and Manager notepad asides. The test now selects the aside containing the Collapse sidebar control, preserving hover/scroll/collapse/reopen assertions. This adds `FrontEnd/e2e/manager-layout.spec.ts` to the necessary legacy test corrections. Its unchanged-behavior rerun passed. Original failure evidence is retained in `swap-fixes-real-login.json`.

The sequential 672-case run preserved the original eight environment skips: four dedicated real-host gates (critical scenario separately passed on real Chromium/WebKit) and four release-polling checks requiring production preview metadata. No previously failing case was skipped. All 67 originally failing cases passed in this sequential run; the completed controlled parallel repeat is reported below.

### Verified UI screenshots

- History, before: [overflow at 320px](GF3.Tests/TestResults/fix-investigation/swap-exchange-workflow-sho-774bc--card-height-with-1-entries-chromium/test-failed-1.png).
- History, after: [one-entry list fits at 320px](GF3.Tests/TestResults/swap-fixes-family-history/swap-exchange-workflow-sho-774bc--card-height-with-1-entries-chromium/short-history.png).
- Landscape, before: [blocked second-card hit target](GF3.Tests/TestResults/fix-families-first/swap-exchange-workflow-com-214f5--and-review-details-667x375-chromium/test-failed-1.png).
- Landscape, after: [compact toolbar and reachable offer card](GF3.Tests/TestResults/fix-families-second/swap-exchange-workflow-com-214f5--and-review-details-667x375-chromium/compact-exchange.png).

Actual-service fixture preflight also passed: manager login and PC exchange, disposable employee creation, employee login, account-language GET returning Polish, and cleanup through the real employee DELETE endpoint. The returned role was employee. No tokens were printed and no SMTP/native-push delivery claim is made. All requests targeted the isolated test database through the normal Vite API proxy.

| Completed focused stability repeat | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Complete focused suite, `--fully-parallel --workers=2` | 664 | 0 | 8 | 2,146.974s; `swap-fixes-focused-parallel.json`; no flaky outcomes |

Both full focused runs completed successfully without retries or changed timeouts: sequential 664/0/8 and controlled parallel 664/0/8. Relative to the original 597/67/8, all 67 originally failed cases now pass in both executions. The exact original case inventory is recorded below. The complete broader Playwright suite is a separate verification run with the actual API service; its outcomes are not inferred from these focused successes.

### Every originally failed case

Each row is joined by source file, project and unchanged test title against the original JSON and both completed follow-up JSON reports. None of these 67 cases was skipped.

| Category | Project | Original test | Before | Sequential | Parallel |
| --- | --- | --- | --- | --- | --- |
| E | webkit-laptop | [generated shifts responsive flow en](FrontEnd/e2e/generated-manual-shifts.spec.ts) | FAIL | PASS | PASS |
| E | mobile-chrome | [manager combined offer and occupied recipient partial and final claim](FrontEnd/e2e/generated-manual-shifts.spec.ts) | FAIL | PASS | PASS |
| E | webkit-laptop | [manager combined offer and occupied recipient partial and final claim](FrontEnd/e2e/generated-manual-shifts.spec.ts) | FAIL | PASS | PASS |
| E | chromium | [manager combined offer and occupied recipient strict overlap](FrontEnd/e2e/generated-manual-shifts.spec.ts) | FAIL | PASS | PASS |
| E | webkit-laptop | [manager combined offer and occupied recipient strict overlap](FrontEnd/e2e/generated-manual-shifts.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 1280x800](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 1280x800](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 1280x800](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 1280x800](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 320x568](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 320x568](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 320x568](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 320x568](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 375x667](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 375x667](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 375x667](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 375x667](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | chromium | [compact offers and review details 667x375](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | mobile-chrome | [compact offers and review details 667x375](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-iphone-x | [compact offers and review details 667x375](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| A | webkit-laptop | [compact offers and review details 667x375](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| C | chromium | [details lock eligibility, restore History and show creator transfer direction](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| C | mobile-chrome | [details lock eligibility, restore History and show creator transfer direction](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| C | webkit-iphone-x | [details lock eligibility, restore History and show creator transfer direction](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| C | webkit-laptop | [details lock eligibility, restore History and show creator transfer direction](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [History viewport and independent scrolling 320x640](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [History viewport and independent scrolling 320x640](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [History viewport and independent scrolling 320x640](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [History viewport and independent scrolling 320x640](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [History viewport and independent scrolling 375x812](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [History viewport and independent scrolling 375x812](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [History viewport and independent scrolling 375x812](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [History viewport and independent scrolling 375x812](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [History viewport and independent scrolling 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [History viewport and independent scrolling 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [History viewport and independent scrolling 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [History viewport and independent scrolling 390x844](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [History viewport and independent scrolling 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [History viewport and independent scrolling 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [History viewport and independent scrolling 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [History viewport and independent scrolling 430x932](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| E | webkit-laptop | [partial badge stays visible and compact at 320px (uk)](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| E | webkit-laptop | [partial badge stays visible and compact at 375px (en)](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [short History uses natural card height with 1 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [short History uses natural card height with 1 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [short History uses natural card height with 1 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [short History uses natural card height with 1 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | chromium | [short History uses natural card height with 4 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | mobile-chrome | [short History uses natural card height with 4 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-iphone-x | [short History uses natural card height with 4 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| B | webkit-laptop | [short History uses natural card height with 4 entries](FrontEnd/e2e/swap-exchange-workflow.spec.ts) | FAIL | PASS | PASS |
| D | chromium | [restored swap renders and opens panels in en](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | mobile-chrome | [restored swap renders and opens panels in en](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | webkit-iphone-x | [restored swap renders and opens panels in en](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | webkit-laptop | [restored swap renders and opens panels in en](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | chromium | [restored swap renders and opens panels in pl](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | mobile-chrome | [restored swap renders and opens panels in pl](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | webkit-iphone-x | [restored swap renders and opens panels in pl](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |
| D | webkit-laptop | [restored swap renders and opens panels in pl](FrontEnd/e2e/swap-restoration.spec.ts) | FAIL | PASS | PASS |

### Additional Swap regression checks exposed by the full-suite attempt

The product source remained unchanged during the full-suite attempt. Related test corrections were made after failures were observed in that run; subsequent independently completed reruns establish their corrected results. The original aggregate is retained unchanged.

- `FrontEnd/e2e/employee-swap-deeplink.spec.ts`: the logged-out return-to-card scenario used obsolete English-only guest labels and did not select the existing PC password-input mode. It now uses the actual EN/PL accessible labels and UI mode control. The URL and exact target-card focus assertions are unchanged. Independent rerun: **4 passed, 0 failed, 0 skipped**, `swap-fixes-deeplink-login.json`, 22.0s. Authentication is mocked in this UI suite; this is not a real-backend login claim.
- `FrontEnd/e2e/employee-notifications.spec.ts`: the read-state test reloaded after the mock handler received a write, before the resulting authoritative GET completed. WebKit reported an interrupted `/api/employee-ui-state` fetch. The test now waits for the GET containing the persisted read ID and its completed response before reloading. Its first corrected WebKit rerun was **3 passed / 1 failed**: the remaining expiry assertion exposed `fastForward` coalescing all interval callbacks into a single instant while React effects were settling. `runFor` now executes every virtual tick over the same 90 seconds. No real sleep, timeout increase, ignored page error or relaxed expiry assertion was added. Final all-project EN/PL rerun: **8 passed, 0 failed, 0 skipped**, `swap-fixes-notifications-timers.json`, 47.2s. Reload persistence, unread state, five-day expiry and an empty page-error list remain asserted. These are mocked UI lifecycle tests; transactional notification persistence is separately covered by the real backend tests above.

The initial full-suite attempt also demonstrates a separate real-service fixture limitation: `AuthAttemptFilter` deliberately permits ten logins per account per fifteen minutes. Legacy tests repeatedly use the same bootstrap manager for UI login and employee setup. Failure snapshots show the actual HTTP 429 error “Too many attempts”, not an unavailable API. The production limiter has not been weakened or disabled. Broader guest-login, profile and motion failures are recorded separately from Swap acceptance results below.

Production-preview verification: the built application was served at `http://127.0.0.1:5191`; `/version.json` returned JSON and `/api/health` returned 200 through Vite preview's inherited API proxy. Command: `npx playwright test e2e/swap-exchange-workflow.spec.ts --grep "release notice preserves" --workers=1 --reporter=line,json --output=../GF3.Tests/TestResults/swap-fixes-production-wizard` with `PLAYWRIGHT_BASE_URL=http://127.0.0.1:5191`. Result: **4 passed, 0 failed, 0 skipped**, `swap-fixes-production-wizard.json`. This independently exercises the four production-only Swap checks skipped in the ordinary dev-server focused runs. They remain recorded as skipped in those original aggregate results.

Legacy real-service graph verification: `npx playwright test e2e/manager-graph-create.spec.ts --project=chromium --workers=1 --reporter=line,json --output=../GF3.Tests/TestResults/swap-fixes-real-graph`, using the normal 5188 Vite proxy and isolated 54294 API, completed **2 passed, 0 failed, 0 skipped** in 20.1s (`swap-fixes-real-graph.json`). These tests perform real employee/shop/container/availability setup, graph creation and reload, generated-preview POST, positive persisted graph-ID and no-zero-ID request checks, then actual API cleanup. No API routes are mocked. The phone editor hit-target failure in the full run remains a separately recorded existing layout issue; desktop success does not count it as passed.

Schedule-view regression protection: the full attempt exposed a Mobile Chrome Polish `schedule-hero.spec.ts` alignment failure (2.01084px against an unchanged strict 2px limit). The test read two bounding boxes in separate browser calls during entrance animation. Atomic measurement alone exposed a second timing component: `.day` cards deliberately start 50ms apart (170ms and 220ms CSS delays). That first recheck was **4 passed / 4 failed** (`swap-fixes-schedule-hero.json`). The final test waits for the hero's actual subtree animations to finish and measures both headings in one browser call. Both headings, every original overflow/countdown/day-off assertion, and the strict `< 2` bound remain checked. Final all-project EN/PL recheck: **8 passed, 0 failed, 0 skipped**, 52.1s, `swap-fixes-schedule-hero-settled.json`. No schedule component or animation design was changed; this is a deterministic geometry-test correction.

### Completed full Playwright attempt with the actual backend

From `FrontEnd`, with the isolated API running and the ordinary dev server at 5188:

```powershell
$env:PLAYWRIGHT_BASE_URL = 'http://127.0.0.1:5188'
$env:PLAYWRIGHT_JSON_OUTPUT_FILE = '../GF3.Tests/TestResults/swap-fixes-full.json'
npx playwright test --fully-parallel --workers=2 --reporter=line,json --output=../GF3.Tests/TestResults/swap-fixes-full
```

**FAILED: 1,279 passed, 219 failed, 10 skipped**, 6,656.006s, exit code 1, no retries/flaky outcomes. The 219 failures comprise 158 assertion/action failures and 61 timed-out tests. Evidence: `GF3.Tests/TestResults/swap-fixes-full.json`, `swap-fixes-full.log`, and screenshots/error contexts under `swap-fixes-full/`. This is a completed 1,508-case execution, not a stopped partial run. API health was independently rechecked at 14:34 local time and returned HTTP 200; its stderr was empty.

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| WebKit iPhone X | 318 | 56 | 3 |
| WebKit laptop | 320 | 54 | 3 |
| Chromium | 321 | 54 | 2 |
| Mobile Chrome | 320 | 55 | 2 |
| Total | 1,279 | 219 | 10 |

The ten skips are the original four real-host gates and four production-preview gates, plus two pre-existing WebKit service-worker lifecycle gates. The dedicated real-host scenario and all four production-only Swap wizard cases were executed separately as recorded above; raw skipped cases remain skipped in this aggregate.

An exact file/project/title join against the original 67 failures found **67 passed, 0 failed/missing** in this full execution as well. Thus all original failures passed in three completed executions: focused sequential, focused controlled parallel, and the broader full run.

The raw full result includes four failures corrected while it was running: two notification lifecycle cases, one logged-out Swap deep-link case, and one schedule-hero case. Their final independently completed reruns passed 8/8, 4/4 and 8/8 respectively. These reruns do not rewrite the raw 219-failure aggregate. The other 215 raw failed cases remain outside the corrected Swap families and are not reported as passed.

| Full-run file | Raw failures | Investigation / remaining disposition |
| --- | ---: | --- |
| `app-update.spec.ts` | 16 | Environment plus stale guest labels: dev `/version.json` is HTML, and guest sign-in is Polish. Production-only Swap wizard update checks separately passed 4/4 on preview. The generic app-update tests remain red. |
| `employee-critical-pages.spec.ts` | 10 | Eight tests expect an Inbox heading while the current UI exposes an Inbox tab. Two PDF activation/retry tests assert unchanged window scroll (expected 0, observed 368/239); that product/test-scroll contract remains unresolved. PDF fixtures use real account/login APIs but mock schedule/PDF responses, so these are hybrid tests, not real PDF-export integration. |
| `employee-motion.spec.ts` | 40 | Unresolved animation/DOM contract failures: absent named pulse animations, missing nav indicator or motion-key nodes, and entrance/sampling expectations, including reduced motion. No motion assertions were relaxed. |
| `employee-notifications.spec.ts` | 2 | Corrected synchronization/virtual-clock timing; final 8/8 passed as documented above. |
| `employee-swap-deeplink.spec.ts` | 1 | Corrected guest labels/password mode; final 4/4 passed. |
| `legal-documents.spec.ts` | 8 | Stale English accessible names on the intentionally Polish guest screen. |
| `login-keypad.spec.ts` | 4 | Stale guest/password accessible names. Keypad behavior is not inferred as passing from a selector failure. |
| `login-phone-pin-dialog.spec.ts` | 28 | English-only Enter password/PIN/password labels do not match the Polish guest screen; repeated 30-second selector timeouts. |
| `manager-critical-pages.spec.ts` | 8 | English Sign in assertions after unauthenticated access/logout; current guest screen is Polish. |
| `manager-graph-create.spec.ts` | 6 | Two phone Save hit-target defects, independently reproduced on pinned HEAD below; four actual login rate-limit failures. Separate real desktop graph creation/generation passed 2/2. |
| `manager-layout.spec.ts` | 8 | Shared-manager account exhausts the real ten-login/fifteen-minute limit. Snapshots show Too many attempts before workspace selection. Independent real sidebar smoke passed; the fixture limitation remains. |
| `manager-workspace-modes.spec.ts` | 45 | Stale English guest/chooser/legal labels and a mojibake Polish close-button regex. One Mobile Chrome matrix case additionally detached during `scrollIntoViewIfNeeded`; that setup/timing failure remains unresolved. |
| `motion.spec.ts` | 12 | Stale English guest labels and an expectation of `gf3-enter` under reduced motion where the UI renders `none`. |
| `promo-video.spec.ts` | 1 | WebKit laptop reduced-motion/capture test expects the manager scene visible but it is hidden; unresolved promo test/product behavior. |
| `responsive-text.spec.ts` | 4 | Manager profile expects strong-element ellipsis and receives clip; reproduced on the earlier pinned baseline. Remains an unresolved profile visual/test contract. |
| `schedule-hero.spec.ts` | 1 | Corrected staggered-animation geometry sampling; final 8/8 passed with the same strict 2px limit. |
| `session-errors.spec.ts` | 4 | Redirect reaches login, then English Sign in selector fails on the Polish guest screen. |
| `viewport-dialogs.spec.ts` | 4 | Two WebKit availability-transfer dialogs overflow by 4px (376 versus 372 client width). Two Chromium calendar-resize assertions observe bottom 524/588 against viewport bounds 501/565. Layout/timing investigation remains necessary; geometry assertions are intact. |
| `viewport-pages.spec.ts` | 1 | Mobile Chrome manager case times out at the Open notepad click waiting for visibility/stability; unresolved UI/test timing. |
| `viewport-stress.spec.ts` | 16 | Twelve version-history fixtures expect Commit 25 while current Polish buttons expose Wersja 25; four phone availability-transfer overflow failures. |

Every raw failed case retains its exact project, title, source line, error and artifact path in `swap-fixes-full.json`. Reproduce any listed family with `npx playwright test e2e/<file> --project=<project> --workers=1` from `FrontEnd`, using the API startup and base URL above. To reproduce account-budget coupling, use the complete parallel command above: running only one login case may remain under the real limiter. Fixing the shared authentication fixture requires isolation/reuse of authenticated test sessions; disabling or raising the production security limit was deliberately avoided.

### Phone editor baseline reproduction

The retained `a17e462` frontend snapshot was served at `http://127.0.0.1:5190`, with an independent Vite cache and the actual isolated API proxy. Its `ContainerGraphEditor.tsx`, `ContainerGraphEditor.module.css` and `ContainerGraphEditPage.module.css` were compared with `git show HEAD:<path>` and matched after newline normalization. No branch/reset operation was used.

```powershell
$env:PLAYWRIGHT_BASE_URL = 'http://127.0.0.1:5190'
npx playwright test e2e/manager-graph-create.spec.ts --project=webkit-iphone-x --grep 'new graph makes no zero-ID' --workers=1 --reporter=line,json --output=../GF3.Tests/TestResults/swap-fixes-baseline-phone-save
```

Result: **0 passed / 1 failed**, 27.328s, `swap-fixes-baseline-phone-save.json`. Real login/setup completed; both baseline and current UI failed because `.employeeActionRow`/header actions intercept the first Save button. This confirms a pre-existing phone PC-editor layout defect, not a regression caused by the new manual-color/pinning implementation. Reproduction in the current tree uses the same command with base URL 5188. It remains an explicit broader-suite failure; no forced click or skipped case hides it.

### Follow-up acceptance and limits

- **Focused Swap gate: PASS**, 664/0/8 sequential and 664/0/8 controlled parallel; original 67 also pass inside the full run. Dedicated actual HTTP/SQLite manager/employee browser acceptance and production-preview wizard update checks passed separately.
- **Frontend/backend gates: PASS**, lint (0 errors, 16 existing warnings), build, 909 frontend tests, solution build, 850 backend tests. Final lint also passed after all additional test corrections. `git diff --check` passed.
- **Broader Playwright gate: FAILED**, raw 1,279/219/10 as above. Remaining profile, phone PC-editor, availability-transfer, animation, login-fixture and selector failures are not counted as passed or dismissed because they predate this task.
- Prior Manual/Give/Exchange implementation is preserved. Executed tests cover independent ordinary/manual colors, both manual cells, accepted generated-column retention/pinning, stale dirty-editor protection, strict/partial claims, transactional history/notifications and EN/PL/UK responsive workflows. Broader unresolved cases above limit any claim about all profile/editor/browser behavior.
- No architecture, technology, dependency, public API or schema changes were introduced by this follow-up. Necessary additional corrections were limited to scoped landscape interaction and related regression-test timing/accessible selectors. All original uncommitted work remains on DEV2 at the same HEAD; no commit or push.
- Mocked UI suites, hybrid PDF fixtures and genuine real-backend checks are distinguished above. No unavailable external SMTP, physical-device/PWA or native-push delivery test is represented as successful integration.
- Cleanup completed: only task-owned API, preview and baseline server processes were stopped after ownership checks. The pre-existing 5188 Vite server remains running. Isolated SQLite databases, logs and screenshots remain available for review; no user database was altered.
