# Plan — Shift Swap Safety Audit, Hardening, and Accept Confirmation

**Repository:** `https://github.com/OlehProtsun/GF3`  
**Branch:** `DEV2`  
**Immutable baseline:** `842f223ce923fdf2094e99e4089e3d65f6f675af`  
**Mode:** CODEX implementation handoff. **Do not execute this plan during planning.**

## 1. Objective and acceptance contract

Protect existing employee-created public/private shift offers and manager-created open shifts from invalid input, stale state, double processing, concurrent accept/cancel operations, and accidentally exposed employee contact information. Preserve partial-shift transfer and history. Add an explicit **Accept swap?** confirmation to the employee `/swap` page. A successful acceptance must transfer the specified offered interval **exactly once**, persist one coherent request/status/history change, and emit post-commit notifications only for committed operations. A failed action must leave the schedule, request and history unchanged.

This is **not** an implementation of two-way shift exchange, manager approval, new business limits, or a new scheduling subsystem. Preserve the existing meaning of “swap” (giving away an assigned interval / claiming an open shift).

## 2. Verified Analysis Context — baseline facts and audit findings

### 2.1 Confirmed code and mechanisms

- `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs`: `[Authorize(Roles = AuthRoles.Employee)]`, `GET /api/employee-shift-swaps`, `GET /employees`, `POST /`, `POST /{id}/accept`, `POST /{id}/cancel`.
- `Accept`: starts an EF transaction, reads request with schedule/slots, validates status/publication/target/container/manager lock/overlap, mutates tracked slot, records before/after history snapshots, commits, logs and sends SignalR notifications **after** commit.
- `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs`: partial transfer uses `ApplyAcceptedSwapPeriod(..., scheduleSlots)`, which now allocates non-conflicting `SlotNo`s. `GetAcceptanceUnavailableReason` is reused for DTO availability and acceptance checks.
- `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`: already has a **filtered unique index** `ux_shift_swap_open_slot` on `(ScheduleSlotId, Status)` for `status = 'Open'`, and unique/validity constraints on schedule slots. The new `SlotNo` regression tests are in `GF3.Tests/ShiftSwapSlotNoConflictTests.cs`. **Do not remove either mechanism.**
- `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.tsx`: `handleAccept` immediately calls `acceptSwapMutation.mutate(swap.id)` with no user confirmation; `SwapOfferCard` calls it directly. This page already uses `ConfirmDialog` to unpin swaps.
- `FrontEnd/src/shared/ui/ConfirmDialog/ConfirmDialog.tsx`: reusable dialog supports `open`, `title`, `message`, `variant`, `onConfirm`, `onCancel`, `confirmText`, `confirmDisabled`, `cancelDisabled`.
- `FrontEnd/src/entities/shift-swaps/api/queries.ts` + `shiftSwapsApi.ts`: existing accept mutation, cache invalidation, and backend `POST /{id}/accept` contract. Tests already exist at `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.test.tsx` (Vitest/Testing Library).
- `FrontEnd/src/shared/i18n/index.ts` uses English literal keys and `FrontEnd/src/shared/i18n/pl.json` for Polish translations.

### 2.2 Confirmed risks to remedy

**S1 — critical, stale/concurrent status changes.** `Accept` checks `swap.Status == Open` before mutating, then writes `Accepted` with ordinary tracked `SaveChanges`; employee `Cancel` independently checks Open then writes Cancelled. There is no **conditional, database-enforced Open → terminal-state claim** shared by these handlers. SQLite transactions mitigate some interleavings, but controller-level checks alone must not be relied on to guarantee first-writer-wins across request paths.

**S2 — medium, race in create.** `Create` does an `AnyAsync(Open)` precheck then inserts. The existing database unique index correctly prevents a second open offer for the same slot, but the Create path does not translate that specific losing write into a controlled user-facing conflict.

**S3 — medium, misleading availability / stale slot.** `GetAcceptanceUnavailableReason` validates swap status, permissions, manager lock and recipient overlap but does **not** validate that the source slot still belongs to the offering employee (or remains unassigned for manager-created offers), that the offered interval still fits the source slot, or that the referenced slot actually exists. `Accept` checks some of these later; GET can therefore incorrectly report `canAccept=true`, and `Accept` uses `First(...)`, which can throw if slot is missing.

**S4 — medium, malformed input treated as public.** `Create` converts a supplied `TargetEmployeeId <= 0` to null, silently turning invalid private-recipient identifiers into a **public** offer. If one of the requested times is blank, the current resolver substitutes a source boundary; malformed partial offers can silently expand.

**S5 — medium, unnecessary PII in employee picker API.** `GET /api/employee-shift-swaps/employees` returns all employee emails and phone numbers even though the swap recipient UI uses only ID and display names. This endpoint is available to every authenticated employee. Limit the response to the fields actually necessary for recipient selection.

**S6 — UX, irreversible-feeling action without confirmation.** `Accept` takes the shift immediately on one click. Reuse the project's existing `ConfirmDialog` rather than adding a second dialog library or custom modal.

**Targeted robustness findings:** the backend `ParseTimeMinutes` currently ignores a third colon-separated component (e.g. `12:30:45`) despite requiring `HH:mm`; malformed graph-note base64 can throw `FormatException` while consuming manager-created manual-slot metadata. Validate/handle at the existing boundaries; never swallow a corrupt record and claim a successful transfer.

### 2.3 Boundaries and unknowns

The immutable GitHub source was read; **the application and concurrent .NET integration tests were not executed in this planning environment**. For targeted manager cancel/delete entry points, route contracts are verified from `FrontEnd/src/entities/shift-swaps/api/shiftSwapsApi.ts`, but their **controller filenames were not available** through the source browser. Step 2 includes a **bounded symbol lookup**, not a whole-repository investigation. No claims are made about an already demonstrated double-accept in production, or about atomicity across external realtime transports.

## 3. Technical Decision (completed Reasoning phase)

Use existing EF Core/SQLite, controllers, `ShiftSwapRules`, DTOs, transaction, notifications, UI component, and test fixtures. Do **not** introduce a swap service framework, separate lock service, external queue, migration, new dependency, or a new approval state.

1. Make every **Open → Accepted** and **Open → Cancelled** transition in the relevant endpoints a **conditional database update** (`WHERE Id = ... AND Status = Open`) and require **exactly one affected row**. For `Accept`, execute the conditional update **inside the existing transaction**, after preconditions and before applying slot changes; maintain consistency with the already tracked swap model. Concurrent losers receive the existing validation/conflict mechanism and commit nothing. For cancellation, use an atomic conditional transition as the write of record, not stale tracked `SaveChanges`.
2. Reuse the existing **filtered unique index** and translate a Create insert conflict to a clear duplicate-offer response. Do not replace the index with application-only checks.
3. Consolidate *existing* static acceptance preconditions in `GetAcceptanceUnavailableReason`; add only missing source-slot/period validity checks. Ensure the same validation is used at read/accept time, with authorization enforced on the server.
4. Reject invalid target IDs and partially supplied time boundaries. Keep the established full-slot fallback **only when both optional times are absent/blank**. Enforce strict normalized `HH:mm` for supplied values.
5. Reduce exposure from employee picker to `id`, `firstName`, `lastName`, `displayName`; leave private-target behavior intact (do not invent a new cross-container policy).
6. Add a pending-swap state and reuse the current `ConfirmDialog` with the actual sender, schedule, date, time interval and hours. **Only the second explicit confirmation click** calls the existing mutation.

**Why:** minimal change radius, SQLite provides the authoritative uniqueness/atomic write boundary, and existing UI infrastructure provides accessible confirmation without architectural work. **Do not** attempt to fix manager-edit concurrency by assuming the in-memory edit-lock service is cross-process; that is a separate architectural concern. This plan can recheck the existing lock, but cannot promise global cross-process edit coordination.

## 4. Change inventory

| Action | Exact file or narrow code owner | Responsibility |
|---|---|---|
| MODIFY | `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs` | Claim-once Accept/Cancel, input and conflict handling, no duplicate side effects, privacy projection |
| MODIFY | `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs` | Source/offer validity available to both DTO and Accept, strict time parsing, malformed note handling |
| MODIFY | `GF3.WebApi/Contracts/ShiftSwaps/ShiftSwapEmployeeDto.cs` | Remove unused email/phone fields |
| MODIFY | Manager swap cancel action(s) found by **route-bound targeted lookup** in `GF3.WebApi/Controllers` | Prevent stale manager cancel from overwriting an accepted/cancelled state; retain manager authorization and delete semantics |
| MODIFY | `FrontEnd/src/entities/shift-swaps/model/types.ts` | Remove unused employee-picker email/phone type fields; no ShiftSwap API shape change |
| MODIFY | `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.tsx` | Explicit Accept confirmation and one-shot mutation |
| MODIFY | `FrontEnd/src/shared/i18n/pl.json` | Polish strings for confirmation and new user-facing messages where translated |
| MODIFY | `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.test.tsx` | UI confirmation regressions |
| EXTEND | `GF3.Tests/ShiftSwapSlotNoConflictTests.cs` | Existing SQLite acceptance regression cases stay green |
| CREATE | `GF3.Tests/ShiftSwapSafetyTests.cs` | SQLite-backed authorization, validation, atomicity, rollback tests |
| REUSE AS-IS | `AppDbContext` slot and swap filtered unique indexes, `ShiftSwapHistorySnapshotBuilder`, notifier, workflow log, `PostCommitActions`, `ConfirmDialog`, React Query mutations, `SqliteTestDatabase` | Existing platform mechanisms |
| DO NOT TOUCH | launcher, CI, auth architecture, unrelated schedule generator, broad styling, app shell, other frontend pages, existing historic swaps | Out of scope |

No entity field, schema/index, EF migration, DI registration, environment setting, external package or HTTP route is required. The picker response intentionally drops unused contact fields; `ShiftSwapDto` and the Accept request/response contract remain unchanged.

## 5. Ordered implementation steps

### Step 0 — Baseline and bounded inspection

**Action:** VERIFY ONLY. `git rev-parse HEAD` must equal `842f223ce923fdf2094e99e4089e3d65f6f675af`; if working on a later commit, stop rather than silently adapt this plan. `git status --short`; protect unrelated uncommitted edits. Inspect **only** files in §4 and the manager swap action owners from this bounded search:

```bash
rg -n 'ShiftSwapRequests|shift-swaps|ShiftSwapStatus\.(Open|Cancelled|Accepted)' GF3.WebApi/Controllers --glob '*.cs'
```

Map just the existing manager endpoints `POST /api/containers/{containerId}/graphs/{graphId}/shift-swaps/{id}/cancel`, `POST /api/containers/{containerId}/shift-swaps/{id}/cancel`, and `DELETE /api/containers/{containerId}/shift-swaps/{id}` to their current actions. Change only those actions that perform **Open → Cancelled**; do not alter any deliberate hard-delete/archive behavior for already-terminal swaps. Use `GF3.Tests/Infrastructure/SqliteTestDatabase.cs` for persistence tests.

### Step 1 — Shared acceptance preconditions (server)

**Action:** MODIFY `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs`.

1. Change the **existing** `GetAcceptanceUnavailableReason(ShiftSwapRequestModel swap, int employeeId, bool isScheduleLocked, IReadOnlyList<ScheduleSlotModel> employeeMonthSlots, ScheduleSlotModel slot, ShiftSwapPeriod offeredPeriod)` to return a specific existing-style reason when:
   - `swap.ScheduleSlotId` is null or does not match `slot.Id`, or slot/schedule IDs differ;
   - a manager-created offer's `slot.EmployeeId` is no longer null;
   - an employee-created offer has null `FromEmployeeId` or `slot.EmployeeId != swap.FromEmployeeId`;
   - the normalized offered interval no longer fits the current slot's valid interval;
   - the current request is not Open, swaps are disabled, a manager lock is active, user is source, private target is wrong, or recipient has overlapping **published** shifts (existing checks preserved).
2. Put **nonthrowing** interval checking inside the availability path (return reason for malformed times/slots rather than throwing and breaking the whole GET list). The mutating Accept path must still reject the same invalid data.
3. Accept must look up the slot using `FirstOrDefault`, fail with the project's existing `ValidationException` if absent, and call the shared predicate with the resolved period. Keep server-side authorization/visibility checks; `canAccept` is only a UI hint, not authorization.
4. Make `ParseTimeMinutes` reject any non-`HH:mm` shape (exactly two two-digit groups, ASCII `:` separator, valid 00–23 / 00–59), including `12:30:45`, `9:30`, signs, and out-of-range values. Keep valid normalized stored intervals and zero-length/end-before-start rejection. Reuse `NormalizePeriod`, `EnsurePeriodWithinSlot`, `TimesOverlap` and `ApplyAcceptedSwapPeriod`.
5. When decoding malformed manual graph-note metadata, catch **narrow parse/decode exceptions** locally (`FormatException` from base64 and JSON decode exceptions) and return an invalid-metadata indication to the manager-created validation at item 7. Do not silently return an unchanged note and then commit the accepted swap if `ManualColumnId` proves that a specific manual cell was expected. Preserve readable note text and rollback the whole Accept on invalid metadata.
6. In `GetVisible`, exclude a malformed **open** request whose source slot is missing and cannot support any truthful `ShiftSwapDto` (the current `ToDto` would otherwise throw). For an extant but stale slot, use `GetAcceptanceUnavailableReason` to expose `CanAccept=false` plus a reason. Do not change archived-snapshot projections for valid historic records, and test both paths.
7. For a manager-created manual offer with `ManualColumnId.HasValue`, validate the referenced graph-note manual cell can be parsed and located before consuming it; malformed base64, missing cell or invalid metadata must cause a **controlled validation failure and transaction rollback**, not silent acceptance leaving an undeleted duplicate manual cell. Reuse existing note parsing/manipulation helpers; for a normal manager-created open slot with no `ManualColumnId`, do not require graph-note metadata.

**Dependency:** existing rules are the source for server-side condition checks; no new public service.

### Step 2 — Atomic status transitions and rollback

**Action:** MODIFY `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs` and **only relevant Open → Cancelled manager actions** located in Step 0.

**Accept** (`POST /api/employee-shift-swaps/{id}/accept`):

1. Preserve the existing `BeginTransactionAsync`, eager-loading, recipient membership for public offers, direct target for private offers, status, schedule publication, `AllowSwap`, manager-lock checks, slot validation and recipient overlapping published shifts.
2. Compute `beforeSnapshotJson` **before** touching the shift. After all validations but **before** `ApplyAcceptedSwapPeriod`, issue ONE database-atomic EF Core `ExecuteUpdateAsync` against `db.ShiftSwapRequests.Where(s => s.Id == id && s.Status == ShiftSwapStatus.Open)`; set `Status=Accepted`, `AcceptedByEmployeeId=employeeId`, `AcceptedAtUtc=acceptedAtUtc`. This must run inside the *same* transaction. Require `affectedRows == 1` or throw an existing-style controlled conflict; no slot changes or notification for `0` rows.
3. Because `ExecuteUpdateAsync` bypasses EF tracking, explicitly synchronize the loaded `swap` object to the **same** status/recipient/timestamp. Do **not** call `Reload`/`Clear` on the entire tracked schedule graph. The normal existing `SaveChangesAsync` may persist tracked changes later in the same transaction; no second independent status transition.
4. Keep `ApplyAcceptedSwapPeriod`'s existing non-conflicting `SlotNo` algorithm, manager-created handling, `EnsureScheduleEmployee`, highlights, before/after snapshots, **one** history record, existing two SaveChanges boundaries as needed for snapshot generation, and Commit. **Never** commit the conditional status claim separately from schedule and history.
5. If the claim, slot update, history insertion or Commit fails, roll back the entire transaction (including the claim), return the existing controlled validation/conflict response for expected races/uniqueness problems, and emit **zero** success notifications/workflow logs. Limit catch translation to known concurrency/SQLite constraint codes; do not hide unrelated `DbUpdateException`s as a false time-overlap diagnosis.
6. After commit, reuse existing post-commit log/SignalR calls exactly once. Do not retry the **whole** acceptance flow automatically on `SQLITE_BUSY`/`SQLITE_LOCKED`; treat a genuine writer conflict as retryable-by-user, return a clear conflict response, and avoid recording a second history row.

**Employee Cancel** (`POST /api/employee-shift-swaps/{id}/cancel`):

7. Preserve creator-only authorization; replace tracked check-then-save status mutation with `ExecuteUpdateAsync` filtered by `Id`, `FromEmployeeId=employeeId`, `Status=Open`; set `Status=Cancelled`, `CancelledAtUtc=now`. If `affectedRows == 0`, return a controlled no-longer-open error. Synchronize/re-query the response DTO without another stale tracked write. Log/notify only after the successful atomic write.

**Manager Cancel** (existing actions from Step 0):

8. For every manager action that transitions Open → Cancelled, apply the same `Status=Open` guarded database update **together with its existing manager/container authorization**, inside its existing transaction when the action also modifies slots or archived data; preserve existing archive/log/notification semantics. A manager cancellation must not overwrite `Accepted`; an employee Accept must not overwrite `Cancelled`. Do not modify unrelated manager hard-delete behavior or allow an employee to bypass manager rules.

**Invariant:** across concurrent Accept/Accept and Accept/Cancel contenders only **one terminal transition** succeeds for a request; the losing request returns a conflict and cannot alter schedule, history or send acceptance events.

### Step 3 — Create validation and duplicate conflicts

**Action:** MODIFY `EmployeeShiftSwapsController.Create(...)`, `ShiftSwapRules.ResolveRequestedPeriod(...)`.

1. `TargetEmployeeId == null` means public. `TargetEmployeeId > 0` means targeted private. **Reject** any supplied ID `<= 0` with `ValidationException.ForField(nameof(request.TargetEmployeeId), ...)`; never quietly publish instead. Preserve self-target and nonexistent-target rejection.
2. Both `FromTime` and `ToTime` absent/whitespace => whole source slot; both valid supplied values => normalized bounded partial offer; **exactly one supplied** => validation error on the missing side, not fallback to the opposite source boundary. The UI always supplies both.
3. Keep `hasOpenRequest` as an early friendly preflight and the existing `ux_shift_swap_open_slot` filtered unique index as the authoritative last line of defense.
4. Catch only the unique-index failure resulting from concurrent inserts for **this** slot. Prefer checking the provider-specific SQLite constraint and then querying for an existing Open swap to distinguish from other DB failures (a fresh context/query if the current context is unusable). Return the project's existing validation/conflict response: `This shift already has an open swap offer.` Do not suppress or retry unknown write errors. Do not log/notify/CreateAtAction on a failed insert.
5. Do not change `ShiftSwapRequestModel`, the table, the filtered unique index, or allow multiple Open swaps against the same source slot even if the offered periods do not overlap.

### Step 4 — Minimize employee-picker data

**Action:** MODIFY `GF3.WebApi/Contracts/ShiftSwaps/ShiftSwapEmployeeDto.cs`, the `GetEmployees` projection in the controller, and `FrontEnd/src/entities/shift-swaps/model/types.ts`.

1. Keep `Id`, `FirstName`, `LastName`, `DisplayName`; remove `Email` and `Phone` from **this picker DTO/API response and frontend type only**. No employee table or general-profile API changes.
2. Preserve existing employee name sorting and the ability to select a named recipient as currently implemented. Do not introduce a speculative target-container restriction or leak whether a private swap exists through the picker.
3. Add a response-contract test verifying JSON does not include email/phone and that only authenticated employees access the picker through existing role policy. Do not write real contact data to test snapshots.

### Step 5 — Add explicit Accept confirmation UI

**Action:** MODIFY `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.tsx`; REUSE `FrontEnd/src/shared/ui/ConfirmDialog/ConfirmDialog.tsx` **without editing it**.

1. Add `pendingAcceptSwap: ShiftSwap | null` local state. Change `handleAccept(swap)` so it only opens confirmation when `swap.status === 'open'`, `swap.canAccept === true`, and no swap action is pending. **Remove the direct mutation from this first-click handler**.
2. Render a second `ConfirmDialog` via `createPortal(..., document.body)` in the same place/pattern as existing unpin dialog, not within a hidden scrolling card. Set `variant='confirm'`, title `Accept this shift?`, confirm button `Accept shift`, cancel button `Back` (localize using `t`). The message must identify `fromEmployeeName` (or `manualColumnName` / `Open shift` for manager-created), `scheduleName`, `shopName` if present, `formatSwapDate`, `fromTime–toTime`, and `shiftHours`; say explicitly that acceptance updates the user's schedule. Use **existing** formatters and safe `t` placeholders.
3. On **Back**, Escape or backdrop cancel, clear `pendingAcceptSwap`; **no API call**. Disable confirm and cancel while `acceptSwapMutation.isPending`. Guard against double submit with an immediate local in-flight ref/state to cover repeated clicks before React rerenders. Never send the API request more than once for one confirmation interaction.
4. On confirm, check the current query cache/list for the same swap ID and `status === 'open'` and `canAccept`; if absent or now ineligible, close, invalidate/refetch the swap query, show a localized stale-offer message and **do not** submit. If eligible, call **existing** `acceptSwapMutation.mutate(id, { onSuccess, onError })` once. On success close/clear pending state; existing mutation invalidates swaps and employee schedules.
5. On error close/clear pending state, release busy guard, call existing `getErrorMessage` / `ErrorBanner`, and invalidate swaps/schedules to show authoritative server state. Never optimistically reassign UI schedule prior to HTTP success. The server independently rechecks all invariants even if UI data was fresh.
6. Make the same confirmation behavior apply to employee-created public offers, employee-created private offers and manager-created open shifts. Do not insert confirmation for creating, pinning, cancelling or history browsing (except existing unpin confirmation).

### Step 6 — Polish localization

**Action:** MODIFY `FrontEnd/src/shared/i18n/pl.json`.

Add translations for **each new UI literal** from Step 5 (`Accept this shift?`, `Accept shift`, `Back`, confirmation detail template, `Open shift`, stale/unavailable offer message). Keep English via literal source strings. Reuse existing translations for shared keys. If new backend validation text is displayed and belongs to `serverMessages.json`'s explicit translation catalogue, add matching entries to `FrontEnd/src/shared/i18n/serverMessages.json` and `pl.json` using current translation conventions; no new translation engine.

### Step 7 — SQLite security/consistency integration tests

**Action:** CREATE `GF3.Tests/ShiftSwapSafetyTests.cs`; EXTEND existing `GF3.Tests/ShiftSwapSlotNoConflictTests.cs` **only if required** by changed rules/contracts.

Reuse `SqliteTestDatabase.CreateAsync()` with real EF SQLite migrations/indexes and fresh contexts per parallel actor. Reuse existing claims principal/controller fixture and fakes. Do not use EF InMemory for concurrency/unique checks.

Implement focused tests with named scenarios:

1. `Create_InvalidTargetId_DoesNotPublish`: `0`, `-1` rejected; no request inserted, no notification.
2. `Create_PartiallyProvidedPeriod_IsRejected`: only From or only To; original slot intact.
3. `Create_MalformedTime_IsRejected`: `12:30:45`, `9:30`, end-before-start, outside assigned slot; valid `09:00–15:00` accepted.
4. `Create_DuplicateOpenSlot_ExistingIndexEnforced`: two distinct contexts create same slot offer; exactly one Open persisted; losing write returns controlled error instead of 500. **Make concurrency deterministic** with barriers where supported; also exercise a controlled unique insert race directly if SQLite lock scheduling makes HTTP interleaving nondeterministic.
5. `Accept_OpenOffer_PersistsExactlyOnce`: accepted slot (including partial split), status, recipient, timestamp, exactly one before/after history; maintain `SlotNo` and hours. No duplicate notifications.
6. `Accept_SameOfferTwice_SecondIsConflict`: sequential and two-context racing requests; one success, one controlled rejection, one history, transfer once, one success notification. Use bounded timeout/barriers, avoid flaky sleeps.
7. `Accept_RacesOwnerCancel_OnlyOneTerminalState`: across separate contexts, accepted **or** cancelled, never both, no orphan slot edits/history, expected side effects only for winner.
8. `Accept_NotTargetAndNoSharedPublicContainer_Denied`: non-target private request and unrelated-container public request cannot be accepted or leaked through list; no slot mutations.
9. `Accept_OwnerAcceptAndWrongAccount_Denied`: current employee cannot take their own offer; API role authorization enforced by existing integration/auth test facilities (not merely hand-created principal).
10. `Accept_StaleSourceSlot_IsUnavailable`: source owner changed, manager-created slot assigned, offered times no longer fit, request source slot missing where fixture permits; `CanAccept=false` or record omitted safely, POST controlled rejection, zero writes.
11. `Accept_AlreadyWorks_Rejects`: cross-schedule published overlap in same calendar month, exact adjacent non-overlap allowed, no conflict in other months/days.
12. `Accept_WhenDisallowedOrLockedOrUnpublished_Rejects`: guard status/publication/AllowSwap/manager lock; no history.
13. `Accept_PartialSplitWithOccupiedSlotNos_RemainsValid`: original F35 regression stays green for prefix, suffix, middle and whole-slot transfers.
14. `Accept_FailedHistoryOrSlotWrite_RollsBackEntireClaim`: deliberately trigger a **real SQLite constraint violation** inside transaction; request remains Open, original slot unchanged, history absent, no success SignalR.
15. `Cancel_OnlyOwnerCanCancelAndOnlyOnce`: unauthorized and already terminal requests never mutate; legitimate owner cancels once, correct timestamp.
16. `ManagerCancel_RacesAccept_PreservesWinningState`: invoke existing manager action for Open offers; no Accepted → Cancelled overwrite. Test manager-created offer acceptance unaffected.
17. `GetEmployees_ExcludesContactData`: picker JSON includes expected names/IDs and **never** email/phone.
18. `ManagerManualNote_MalformedMetadataFailsSafely`: malformed base64/JSON does not cause an unhandled 500 nor accidentally delete the note/cell or produce success.

For concurrency tests, use **separate `AppDbContext` objects and connections** backed by the same `SqliteTestDatabase` file, capture return statuses/exceptions, query committed DB state using a third fresh context, and keep test data synthetic. If a tested manager behavior is intentionally archival rather than cancellation, assert its existing semantics without inventing a terminal transition.

### Step 8 — Frontend tests

**Action:** MODIFY `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.test.tsx`.

Reuse the current mock `acceptMutate`, current `createSwap(...)` fixture and Testing Library/user-event. Update the existing `accepts open offers, cancels own offers, and disables locked offers` test: first click opens dialog and **does not call** `acceptMutate`; explicit `Accept shift` does.

Add tests for: public/private/manager-created summary data; Back/Escape/backdrop sends zero requests; double-click Confirm sends exactly one request; pending disables controls; success closes and cache mutation is invoked once; server error displays `ErrorBanner` and can be retried via a fresh confirmation; stale swap disappears or becomes `canAccept=false` during dialog so confirmation refuses mutation; locked/overlap/own offers never expose Accept action; existing unpin confirmation still works. Use current `pl.json` translation conventions rather than snapshotting English in Polish mode.

### Step 9 — Verify in this order

**Targeted backend:**

```bash
dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "FullyQualifiedName~ShiftSwapSafetyTests|FullyQualifiedName~ShiftSwapSlotNoConflictTests"
```

**Backend CI-style tests and build:**

```bash
dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "Category!=LocalOnly"
dotnet build GF3.Tests/GF3.Tests.csproj --configuration Release
```

**Frontend (from `FrontEnd/`):**

```bash
npx vitest run src/pages/employee-swap/ui/EmployeeSwapPage.test.tsx
npm run build
npm run lint
```

`npm install` only if dependencies are missing; respect the existing lockfile. Do not modify unrelated frontend warnings merely to satisfy a scoped swap plan.

**Diff/quality:** `git diff --check`, `git diff --stat`, verify only §4 files (plus verified manager action owner / server translation catalogue if needed) changed, no migrations/packages/launcher/CI changes. Report test commands and results accurately; never claim success if they were not run.

**Manual in a throwaway/local DB (never production data):** create a public offer, a targeted private offer, and a manager-created manual open shift; verify Cancel on dialog leaves no change, Confirm takes only offered hours, monthly summary and shift table reload correctly, history snapshot is coherent, manager manual note/cell/highlight remains correct. Try a stale card from two tabs (accept in A, confirm in B): B sees a controlled rejection and refresh. Try owner Cancel racing another employee's Accept. Reload and verify persistence/notifications.

## 6. Data/API/persistence/error guarantees

**Existing routes (unchanged):**

- `GET /api/employee-shift-swaps`: valid visible offers and accepted/cancelled history. `CanAccept` must not be true for a stale/invalid source.
- `GET /api/employee-shift-swaps/employees`: employee picker now returns only four name/ID fields (intentional minimal response change).
- `POST /api/employee-shift-swaps`: 201 on success; invalid input and duplicate-open-offer return project-standard validation/conflict responses, not 500.
- `POST /api/employee-shift-swaps/{id}/accept`: 200 on successful single transfer; missing ID keeps existing NotFound mapping, unauthorized/forbidden keeps existing auth policy, no-longer-open/stale/conflicting is a controlled 4xx.
- `POST /api/employee-shift-swaps/{id}/cancel`: 200 on successful owner-only cancellation; already-terminal is controlled 4xx.
- Existing manager swap cancel/delete routes keep methods, paths, roles, and archival semantics; guard Open → Cancelled writes against stale state.

**DB:** existing `shift_swap_request`, `schedule_slot`, `shift_swap_history`, `schedule_cell_style` only. **No schema/migration/index changes**; preserve filtered `ux_shift_swap_open_slot` and both schedule-slot unique indexes. The guarded status claim and slot/history changes must be in **one** acceptance transaction. Duplicate request creation is suppressed by the existing unique index.

**Logging and notifications:** only on successful committed operations. Post-commit notification failure cannot retroactively roll back an accepted swap; retain `PostCommitActions`' existing warning/reporting approach and do not falsely report a successful rollback.

**DI/config:** no new bindings, options, environment variables or dependencies. **No new services.**

**Error policy:** expected stale/unauthorized/invalid input returns controlled 4xx via existing app error pipeline; unexpected DB or programming defects must still surface through standard error logging (do not catch-all-and-return-success). Preserve cancellation-token flow.

## 7. Acceptance checklist

- [ ] All changes are based on immutable `842f223`; baseline and `git status` recorded.
- [ ] Valid public, private, full-period, partial-period, and manager-created accepts still work.
- [ ] Exactly one winner for concurrent Accept/Accept, Accept/Cancel and manager-cancel/Accept, with no double history or orphan edits.
- [ ] DB transaction rollbacks leave original slots, status, and history unchanged on error.
- [ ] No open request can be created twice for a slot; losing racing create returns controlled error.
- [ ] Invalid target IDs never become public offers; malformed/partial time inputs are rejected.
- [ ] GET availability matches POST eligibility for stale source slots, while server remains authority.
- [ ] Unrelated-container public and non-target private requests cannot be accepted.
- [ ] Recipient-picker response contains no employee email/phone.
- [ ] First click on Accept only opens a clear, localized confirmation with accurate shift details; Back/Escape/backdrop has no side effects.
- [ ] Confirm calls the existing mutation exactly once, guards pending/stale state and refreshes after success/error.
- [ ] Previously fixed partial `SlotNo` uniqueness behavior is unchanged; previous snapshot/highlight/SignalR tests remain green.
- [ ] Targeted backend/frontend tests, backend CI-style tests, frontend build/lint pass (or exact failures documented).
- [ ] No unrelated refactors, new deps, migration, launcher or CI changes.

## 8. Explicit exclusions / implementation-agent instructions

Do not rerun a full architecture analysis. Do not replace the existing Shift Swap functionality with a new request workflow or introduce two-way exchange/manager approval. Do not change business working-hour limits, company policy, employee roles, unrelated schedule generation, or source-slot ID preservation. Do not assume manager-edit locks protect multiple API instances; this plan protects swap status transitions, but **does not** solve an unrelated manager save using arbitrarily stale schedule data. Do not invent new API status enums, frontend state libraries, or database tables. If an existing manager cancellation does not perform a status transition, preserve its actual behavior and record why no CAS change was necessary.
