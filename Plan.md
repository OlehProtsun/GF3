# Plan — Fix partial shift-swap acceptance (GF3, DEV2 @ f2257ba)

## 1. Objective

Fix the reproducible **database uniqueness collision** when accepting a private, partial shift swap. Scenario: schedule **F35**, **2026-10-23**, Anastasia SAS offers **15:00–22:00** of her assigned **09:00–22:00** slot to Oleh Protsun; Oleh presses **Accept** and receives:

> This swap could not be accepted because the resulting schedule would conflict.

The intended result is to retain Anastasia's **09:00–15:00** assignment and transfer **15:00–22:00** to Oleh, while preserving other assignments, request/history state, authorization, and notifications.

**Baseline:** `OlehProtsun/GF3`, `DEV2`, immutable commit `f2257baeeae67ec983045f6140a71b792ba47a51`. Implement against that commit; do not silently rebase onto newer DEV2 code.

## 2. Confirmed diagnosis / Analysis Context (Phase 1 handoff)

### 2.1 Verified in the source code at the specified commit

- `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs`: `POST /api/employee-shift-swaps/{id}/accept` calls `ShiftSwapRules.ApplyAcceptedSwapPeriod(...)` for employee-created swaps (currently around line 264), then `SaveChangesAsync`. Its `catch (DbUpdateException)` returns the exact generic conflict error (around lines 322–325). The message **does not mean the overlap / hour-limit checks rejected the request**: it is the database-update exception translation.
- `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs`: `ApplyAcceptedSwapPeriod(ScheduleSlotModel slot, int originalEmployeeId, int acceptingEmployeeId, ShiftSwapPeriod period)` mutates the original slot to the offered interval and accepting employee. It creates remaining parts with `CreateRemainingSlot`, which **copies `sourceSlot.SlotNo` unchanged** (around lines 293–332). It has no visibility into other slots occupying the new time intervals.
- `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`: `ConfigureScheduleSlot` enforces uniqueness of `(ScheduleId, DayOfMonth, FromTime, ToTime, SlotNo)` and separately of `(ScheduleId, DayOfMonth, FromTime, ToTime, EmployeeId)` for assigned employees. `SlotNo` must be >= 1; `FromTime < ToTime`.
- `DataAccessLayer/Models/ScheduleSlotModel.cs`: `SlotNo` denotes a *position in the same time interval*, **not an employee ID**. Existing per-interval positions are therefore meaningful and cannot be blindly reused after splitting a different time interval.
- `DataAccessLayer/Models/ShiftSwapRequestModel.cs`: the request already persists `OfferedFromTime`, `OfferedToTime`, `ScheduleSlotId`, `FromEmployeeId`, `TargetEmployeeId`, `Visibility`, `Status`, and acceptance metadata. No new columns are needed.
- `ShiftSwapRules.GetAcceptanceUnavailableReason(...)` already rejects a recipient's actual time overlap (`"You already work during this time."`). Preserve it.
- The controller already wraps acceptance + history in an EF database transaction, records snapshots, highlights cells, and notifies after commit. Reuse these mechanisms.

### 2.2 Verified from attached SQL export

The provided `GF3_Graph_33_20261008_0530.sql` is a **schedule export**, not a complete live database / swap-request export. It contains 202 shift-slot rows for F35 in October 2026. The important rows on **23 October** are:

| Employee | Assigned interval | SlotNo |
|---|---|---:|
| Anastasia SAS | 09:00–22:00 | 1 |
| Olena Romadanova | 09:00–15:00 | 1 |
| Anna Minieieva | 09:00–15:00 | 2 |
| Margarita Isachenko | 15:00–22:00 | 1 |
| Wiktoria Bartoszek | 15:00–22:00 | 2 |
| Iryna Barysik | 16:30–22:00 | 1 |

Oleh has **no assignment on 23 October** in this export; he works **15:00–22:00** on both 21 and 22 October. The proposed split reuses `SlotNo=1` for both new periods: it collides with **Olena's 09:00–15:00 SlotNo 1** and **Margarita's 15:00–22:00 SlotNo 1**. The smallest free position is **SlotNo 3** for each interval. The export has no pre-existing duplicate of the *exact* unique key. A targeted isolated SQLite reproduction with the **same two uniqueness definitions** rejected the existing split with `UNIQUE constraint failed: schedule_slot.schedule_id, schedule_slot.day_of_month, schedule_slot.from_time, schedule_slot.to_time, schedule_slot.slot_no`; assigning SlotNo 3 to both resultant intervals passed those database constraints. This is a **SQL-level repro**, not a claim to have run the .NET application.

Other schedule facts (not the cause of this exception) include Anastasia's 177.5 scheduled hours against a configured monthly minimum of 190, and a pre-existing six-day run with a configured limit of five consecutive days. **Do not modify these business settings or weaken them to solve this database-key bug.** The error is thrown by a `DbUpdateException` handler, not by a monthly-hour validation in the acceptance path.

### 2.3 Verification boundary

The current GitHub source files and database model were inspected, and the SQL rows were loaded into an isolated in-memory SQLite schema to verify their values. The **application itself was not executed** here (no full working checkout/.NET SDK in this environment), and the SQL export omits live `shift_swap_request` state. The specific SQLite provider constraint code / stack trace still needs to be asserted by the implementation regression test; do not claim an end-to-end test has already passed.

## 3. Technical Decision / Reasoning Context (Phase 2 handoff)

**One chosen fix:** make the **existing** `ShiftSwapRules.ApplyAcceptedSwapPeriod` assign an unused `SlotNo` for every newly produced `(schedule, day, from, to)` time interval when a partial shift is split. Supply the parent schedule's current slots to this existing method. Keep the original `ScheduleSlotModel` row (and its ID) as the transferred offered interval, as the current implementation does. Create zero, one, or two remaining intervals for the original employee with their own valid position numbers.

Allocate deterministically using the **smallest positive integer not already used by another slot in that exact `(ScheduleId, DayOfMonth, FromTime, ToTime)` group**. Exclude the original slot being changed from the occupied-position lookup, and include every slot allocated earlier in the same operation. Do **not** alter slot numbers of unrelated shifts. For a whole-slot transfer with no interval splitting, preserve the original `SlotNo` to avoid an unnecessary change.

This respects the existing EF Core schema, current request identifiers, transaction, validation, public/private flows, snapshots, notifications, and frontend API. It needs **no migration, dependency, DI, configuration, or API-contract change**.

## 4. Scope classification

| Action | File / component | Precise responsibility |
|---|---|---|
| **MODIFY** | `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs` | Extend partial-transfer method to accept existing slots and assign unique `SlotNo` values for transferred and remainder intervals. |
| **MODIFY** | `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs` | Pass `swap.Schedule.Slots` into the existing method at the single employee-created swap call site. Keep other Accept behavior intact. |
| **CREATE** | `GF3.Tests/ShiftSwapSlotNoConflictTests.cs` | xUnit regression/integration tests with real EF Core SQLite unique indexes for the original F35 case and edge cases. |
| **REUSE, no changes** | `DataAccessLayer/Models/ScheduleSlotModel.cs`, `DataAccessLayer/Models/ShiftSwapRequestModel.cs`, `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs` | Existing entities, relationship and unique-key constraints. |
| **REUSE, no changes** | Existing controller transaction, `ShiftSwapRules` period / overlap validation, `ShiftSwapHistorySnapshotBuilder`, `IWorkflowLogService`, `IRealtimeNotifier`, and test DB setup | Preserve behavior and avoid duplicated infrastructure. |
| **DO NOT TOUCH** | Frontend, launcher, migrations, CI workflow, scheduling generator, manager-created open-shift branch, authentication, localization | Out of scope. |

## 5. Deterministic implementation steps (Phase 3)

### Step 0 — Pin baseline; targeted source check only

**Action:** REUSE / VERIFY; no code modifications.

1. In the Codex workspace run `git rev-parse HEAD` and verify `f2257baeeae67ec983045f6140a71b792ba47a51` (or explicitly checkout that commit before starting). Run `git status --short` and do not overwrite unrelated changes.
2. Open only the two MODIFY files, the three verified data-model files, and directly related swap test fixtures. Locate **all** call sites of `ApplyAcceptedSwapPeriod` and `CreateRemainingSlot` with a targeted search under `GF3.WebApi` and `GF3.Tests`; update their signatures only if affected by Step 1. Do not analyze the whole repository.
3. Record the existing tests' construction pattern for `AppDbContext`, `EmployeeShiftSwapsController`, and an employee claims principal. Reuse their SQLite/transaction setup rather than inventing a new test framework.
4. Confirm the duplicated `SlotNo=1` example above from the isolated fixture (do not import the user's entire SQL export into any real database).

### Step 1 — Correct interval-position allocation at the existing rule

**Action:** MODIFY `GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs`.

1. Extend the existing method contract by a final argument representing the parent schedule's currently loaded slots:

   `ApplyAcceptedSwapPeriod(ScheduleSlotModel slot, int originalEmployeeId, int acceptingEmployeeId, ShiftSwapPeriod period, IReadOnlyCollection<ScheduleSlotModel> scheduleSlots)`

   Preserve the return type `IReadOnlyList<ScheduleSlotModel>` and the **current mutating-original-slot** behavior. This is an internal method; do not introduce a public service/interface.

2. Keep the existing call to `EnsurePeriodWithinSlot` in the controller and the existing `ParseTimeMinutes` / `ShiftSwapPeriod` calculations. Compute up to two non-empty remainder periods: `[original start, offered start)` and `[offered end, original end)`.
3. **Whole-slot case:** no remainder is generated. Reassign original slot to accepting employee as today, preserving its original `Id`, `SlotNo`, `FromTime`, `ToTime`, and `Status=ASSIGNED`.
4. **Partial case:** for every resultant period (the original row mutated to the transferred period, plus each new original-employee remainder):
   - Identify the occupied positive `SlotNo` values in `scheduleSlots` matching **same schedule ID, day, exact start time, and exact end time**, excluding the original source slot by persistent ID.
   - Include earlier staged results from this same swap while calculating later positions (never allow an in-operation duplicate).
   - Choose the **smallest free positive integer**, starting at 1; do not copy the original `SlotNo` automatically and do not invent a maximum `SlotNo` based on `PeoplePerShift`.
   - Set the chosen position on the mutated original row and on each new remainder. Leave all other existing rows untouched.
   - Set transferred row `EmployeeId=acceptingEmployeeId`, `Status=ASSIGNED`, `FromTime/ToTime=offered period`; remainder `EmployeeId=originalEmployeeId`, `Status=ASSIGNED`, `FromTime/ToTime=remainder period`.
   - Keep `ScheduleId`, `DayOfMonth`, the original source row's ID, and request foreign-key relationship unchanged.
5. Adjust `CreateRemainingSlot` only to **accept the selected `slotNo` as an argument**. It must not silently read `sourceSlot.SlotNo` for newly generated periods. Keep `CreateRemainingSlot` in the same class and preserve its existing modeling responsibility.
6. Ensure allocation takes account of `scheduleSlots` **before** the original row is mutated, by excluding the source ID (rather than depending on the source's current interval), to make the result independent of the order of property assignments.
7. Do not add an overlapping-time checker here: `GetAcceptanceUnavailableReason` already checks whether the **accepting employee** works during the offered interval; duplicate human assignments and duplicate exact `(time, employee)` keys remain guarded by the existing application/DB rules.

**Expected F35 output (same day 2026-10-23):**

- Existing Anastasia source row (same DB ID) → **Oleh**, **15:00–22:00**, **SlotNo=3**.
- One newly inserted remainder row → **Anastasia**, **09:00–15:00**, **SlotNo=3**.
- Olena/Anna/Margarita/Wiktoria/Iryna rows preserve their original IDs, employees, times, and `SlotNo` values.
- No shift is lost; total shifted hours remain 13 hours across Anastasia + Oleh for the original full-day source row; ownership changes for exactly 7 hours.

### Step 2 — Wire only the existing Accept call site

**Action:** MODIFY `GF3.WebApi/Controllers/EmployeeShiftSwapsController.cs`.

1. In `Accept(int id, CancellationToken cancellationToken)`, change only the employee-created branch invocation to supply the already eager-loaded `swap.Schedule.Slots` as the final argument of `ApplyAcceptedSwapPeriod`.
2. Continue adding the returned `ScheduleSlotModel` remainders to `db.ScheduleSlots`; continue to let the method mutate the tracked original `slot` to the recipient.
3. Keep `GetAcceptanceUnavailableReason`, `EnsurePeriodWithinSlot`, employee authorization, private target check, manager lock, publication check, manager-created branch, `EnsureScheduleEmployee`, cell highlights, history before/after snapshots, transaction boundary, status/accepted-by timestamp, workflow log, and SignalR notifications unchanged.
4. Preserve the existing `DbUpdateException` → validation-error conversion for genuinely conflicting concurrent/invalid writes. The bug fix should prevent **this specific key collision** upstream; do not suppress database integrity exceptions or return success after a failed `SaveChangesAsync`.
5. Do not add a second `SaveChangesAsync`, transaction, or status transition.

### Step 3 — Add focused SQLite regression tests

**Action:** CREATE `GF3.Tests/ShiftSwapSlotNoConflictTests.cs`.

Use **xUnit**, **EF Core SQLite with actual schema constraints** (not EF Core InMemory), and the repository's existing integration/controller test helpers. If existing helpers are insufficient, construct only a minimal SQLite-backed `AppDbContext` test setup inside this test file and minimal fakes for controller ctor dependencies; no new NuGet packages or production DI registration. The tests must cover the real `EmployeeShiftSwapsController.Accept` and persist via `SaveChangesAsync`, not only assert an in-memory object graph.

Required cases:

1. **`Accept_PartialPrivateF35Swap_ReallocatesConflictingSlotNumbers`**: published swap-enabled F35 schedule in October 2026 with the six 23 October assignments from §2.2, a private `ShiftSwapRequestModel` from Anastasia to Oleh referencing Anastasia's 09:00–22:00 row, offered 15:00–22:00, and an authorized Oleh principal. Execute `Accept(id, ...)` against real SQLite. Expect HTTP success, original row owned by Oleh at **15:00–22:00 SlotNo 3**, a new Anastasia row at **09:00–15:00 SlotNo 3**, and all pre-existing unrelated rows unchanged. Re-query from a fresh EF context to verify persistence and unique-key integrity.
2. **`Accept_PartialSwap_PersistsAcceptedRequestAndHistory`**: same fixture; request status `Accepted`, `AcceptedByEmployeeId=Oleh`, UTC acceptance timestamp set, exactly one `ShiftSwapHistoryModel` for this acceptance, snapshots show original and resultant ownership/intervals; no notification before commit. Existing post-commit side effects must retain their behavior.
3. **`Accept_FullSlotSwap_PreservesSlotNumber`**: 15:00–22:00 entire source slot is transferred; verify no remainder row, same source ID and `SlotNo` retained, no unique-index error.
4. **`Accept_PrefixPartialSwap_AllocatesRemainderPosition`**: offer starts at original start but ends earlier; allocate a valid position for the trailing source remainder when its exact interval already has SlotNo 1 occupied.
5. **`Accept_MiddlePartialSwap_AllocatesBothRemainders`**: offer lies strictly inside a longer shift; both resulting original-employee remainders and the accepting employee's interval get non-conflicting positions, with hours conserved.
6. **`Accept_RecipientAlreadyWorksOfferedTime_RejectsWithoutMutation`**: recipient already has an overlapping assignment on 23 October; expect the existing specific rejection, unchanged swap status, no new slots/history, no notification.
7. **`Accept_NotIntendedPrivateRecipient_RejectsWithoutMutation`**: another employee tries to accept; reject using existing private-target policy.
8. **`Accept_AlreadyAcceptedRequest_DoesNotTransferTwice`**: second sequential Accept fails with existing non-open error and creates no extra slots/history.
9. **`Accept_StaleOrEditedSourceSlot_RejectsWithoutMutation`**: after creation, alter source assignment/times so offer is no longer inside the source or no longer owned by creator; expect existing validation and no partial persistence.

For scenario 1, add test seed rows for Oleh on **21 and 22 October 15:00–22:00** if needed to exercise the existing month-load path. Do not copy the full SQL export or real employee emails into the test project; construct a minimal self-contained fixture with synthetic IDs and only required model fields. Reuse the project's authorized test account setup; do not bypass authentication by modifying production code.

### Step 4 — Regression and final checks

**Action:** VERIFY; do not modify unrelated code.

1. Execute targeted tests first:

   `dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "FullyQualifiedName~ShiftSwapSlotNoConflictTests"`

2. Execute the same non-local backend test command used by CI at the specified commit:

   `dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "Category!=LocalOnly"`

3. Run the full backend build through the test project if the standard build passes: `dotnet build GF3.Tests/GF3.Tests.csproj --configuration Release`. Do not modify Windows-only launcher or CI to make this focused fix pass.
4. Inspect `git diff --check`, `git diff --stat`, and confirm only the two stated production files plus the new test file changed (unless an **existing swap test file** requires a signature update, which is allowed but must be documented).
5. Manually smoke-test on a **copy** of the schedule (never production): publish/allow swap, create Anastasia private offer for Oleh 2026-10-23 15:00–22:00, accept as Oleh, check UI schedule and history, and confirm the private swap leaves the open list and becomes accepted. Reload page to verify persistence.
6. Reproduce the same test with an actual recipient overlap; confirm a legitimate conflict remains blocked. Confirm the manager-created open-shift acceptance path still works unchanged.
7. If a test shows another constraint violation, inspect only the failing EF `SqliteException.SqliteErrorCode`, `SqliteExtendedErrorCode` and failing SQL in the test environment. Do **not** blindly remove unique indexes or catch-and-ignore writes. Report an unrelated violation as a separate blocker rather than widening this patch.

## 6. Data flow and contract guarantees

`POST /api/employee-shift-swaps/{id}/accept`
→ existing authorization/publication/lock checks
→ resolve stored offered interval
→ existing overlap and ownership validation
→ **`ApplyAcceptedSwapPeriod` with parent schedule slots / collision-free interval positions**
→ tracked existing slot update + 0–2 new remainder slots
→ existing EF transaction and `SaveChangesAsync`
→ existing before/after snapshot history and request `Accepted` state
→ commit
→ existing workflow log and SignalR updates
→ existing `ShiftSwapDto` response.

**API:** No new route, HTTP verb, request field, response field, or authentication rule. Retain 200 on success and existing validation/problem-response mapping on failure.

**Persistence:** Existing `schedule_slot`, `shift_swap_request`, `shift_swap_history`, and `schedule_cell_style` tables. No field/index/migration changes. The two EF unique indexes must remain enforced. Existing transaction semantics remain intact.

**DI / config / dependencies:** No changes. No new packages.

## 7. Error handling and edge cases

- Entire vs partial swap: reallocate only if a split changes intervals; preserve original `SlotNo` for an unchanged interval.
- Exact boundary intervals are non-overlapping; do not create zero-length remainders.
- A strictly internal offered interval may create **two** new remainders; choose positions independently for both.
- Allocate positions **per exact interval**, not per employee or per whole day. An overlapping shift with a different interval is not itself a duplicate of this database key.
- The recipient's true overlapping hours remain forbidden by existing `GetAcceptanceUnavailableReason`; do not mistake this for a `SlotNo` collision.
- Existing/open/stale request, private target, manager lock and source ownership checks retain their current behavior.
- A failed SQLite write must roll back assignment, request status and history atomically; never retry with a random `SlotNo` after an exception or mask the failure as success.
- Leave any unrelated pre-existing monthly-hour/consecutive-day conditions untouched. They do not explain the **specific caught `DbUpdateException`** being fixed.

## 8. Acceptance checklist

- [ ] Regression reproduces the original F35 failure **before** the fix using real SQLite (red test).
- [ ] Same test passes **after** the fix (green test).
- [ ] Anastasia retains 09:00–15:00 on 2026-10-23, SlotNo 3, after offering 15:00–22:00.
- [ ] Oleh receives only 15:00–22:00 on 2026-10-23, SlotNo 3.
- [ ] Olena, Anna, Margarita, Wiktoria and Iryna are unchanged.
- [ ] No duplicate `(ScheduleId, DayOfMonth, FromTime, ToTime, SlotNo)` or `(ScheduleId, DayOfMonth, FromTime, ToTime, EmployeeId)` exists.
- [ ] All transferred/retained hours are conserved.
- [ ] Exactly one request acceptance and one history record persist; before/after snapshots are valid.
- [ ] Existing recipient-overlap, private-target, stale-request, manager-lock and manager-created safeguards remain operational.
- [ ] The backend test project builds, targeted tests pass, and the CI non-local backend suite passes.
- [ ] No new migration, dependency, DTO, endpoint, frontend change, or unrelated refactoring.

## 9. Execution guardrails / do not change

Do not modify `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs` or its indexes; do not migrate database or renumber existing independent assignments; do not reinterpret `SlotNo` as a user ID; do not loosen business rules; do not alter the entire shift when only a subset is offered; do not change the existing `shift_swap_request.ScheduleSlotId` identity/reference semantics; do not rewrite the swap feature, notification system, transaction, `GF3.Launcher`, or CI; do not scan/rename unrelated modules. Make the three-file scoped correction and prove it with SQLite tests.
