# Coverage Map: Presentation Layer

This map enumerates each presenter method and handler, with planned scenarios (positive, negative, exception, cancellation).

## MainPresenter
- `MainPresenter(IMainView, IMdiViewFactory)`
- `NavigateAsync` via events:
  - `ShowEmployeeView` ➜ normal navigation, reuse existing view, create new after dispose, cancellation.
  - `ShowAvailabilityView` ➜ normal navigation, reuse, cancellation.
  - `ShowContainerView` ➜ normal navigation, reuse, cancellation.
- `ShowMdi`
  - Normal show (bring to front, show called).
  - Cancellation token already canceled (throws, no view created).

## EmployeePresenter
- `InitializeAsync`
  - Loads list (positive), service exception (error), cancellation (no UI update).
- `OnSearchEventAsync`
  - Empty term ➜ `GetAllAsync` (positive).
  - Non-empty term ➜ `GetByValueAsync` (positive).
  - Service exception ➜ `ShowError`.
  - Cancellation/race: rapid searches ➜ stale results ignored, canceled operation no update.
- `OnAddEventAsync`
  - Clears inputs/errors, sets edit flags, switches to edit.
- `OnEditEventAsync`
  - No current item ➜ no-op.
  - Current item ➜ populate fields, set `IsEdit`, `CancelTarget`, switch to edit.
- `OnSaveEventAsync`
  - Invalid model ➜ validation errors, no service calls.
  - Create ➜ `CreateAsync`, reload, success message, switch back.
  - Update ➜ `UpdateAsync`, reload, success message, switch back.
  - Service exception ➜ `ShowError`, state preserved.
  - Cancellation ➜ no UI update.
- `OnDeleteEventAsync`
  - No current item ➜ no-op.
  - Confirm false ➜ no delete.
  - Confirm true ➜ delete, reload, success message, list mode.
  - Service exception ➜ `ShowError`.
- `OnCancelEventAsync`
  - Clears errors, switch back to list/profile target.
- `OnOpenProfileAsync`
  - No current item ➜ no-op.
  - Current item ➜ set profile, switch to profile.

## AvailabilityPresenter
- `InitializeAsync`
  - Loads groups, employees, binds (positive), exception handling.
- `OnSearchEventAsync`
  - Empty term ➜ `GetAllAsync`.
  - Non-empty term ➜ `GetByValueAsync`.
  - Exception ➜ `ShowError`.
  - Cancellation ➜ no error.
- `OnAddEventAsync`
  - Clears inputs/errors, reset matrix, edit mode.
- `OnEditEventAsync`
  - No current item ➜ no-op.
  - Load full, populate group fields, matrix columns, codes, edit mode.
- `OnSaveEventAsync`
  - Invalid group (validator) ➜ validation errors.
  - No employees selected ➜ error.
  - Invalid payload ➜ error.
  - Valid save ➜ `SaveGroupAsync`, reload, success, switch list/profile.
  - Exception ➜ `ShowError`.
- `OnDeleteEventAsync`
  - Confirm false ➜ no delete.
  - Confirm true ➜ delete, reload, success, list mode.
- `OnCancelEventAsync`
  - Clears errors, switch to list/profile target.
- `OnOpenProfileAsync`
  - Load full group, set profile, switch to profile.
- Bind flows:
  - `OnAddBindAsync` ➜ add row.
  - `OnUpsertBindAsync` ➜ validation errors, normalize hotkey, create/update, reload binds.
  - `OnDeleteBindAsync` ➜ delete existing or remove new row, reload binds.
- Group matrix:
  - `OnAddEmployeeToGroupAsync` / `OnRemoveEmployeeFromGroupAsync` ➜ error on missing selection, info if already present/absent.

## ContainerPresenter
- `InitializeAsync`
  - Load containers and lookups.
- Container flows:
  - Search: empty ➜ `GetAllAsync`; value ➜ `GetByValueAsync`.
  - Add/Edit: set fields, edit mode, cancel targets.
  - Save: invalid ➜ validation errors; create/update ➜ service call, reload, message.
  - Delete: confirm false/true ➜ delete, reload, message.
  - Open profile: set profile, load schedules, switch mode.
  - Exception handling: service throws ➜ `ShowError`.
- Schedule flows:
  - Search: no container ➜ error; with container ➜ `GetByContainerAsync`.
  - Add: load lookups, clear schedule fields, switch to edit.
  - Edit: load lookups, get detailed, populate fields, switch to edit.
  - Save: validation errors; valid save ➜ `SaveWithDetailsAsync`, reload, message.
  - Delete: confirm false/true ➜ delete, reload, message.
  - Open profile: get detailed, set profile, switch to profile.
  - Generate: validation errors; no groups selected; valid generate ➜ `GenerateAsync`, update list/matrix.
  - Exception handling and cancellation behavior.
