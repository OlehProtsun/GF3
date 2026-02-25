# Dedup Pass 2 Report

## 1) Baseline duplication map (WPF/MVVM)

Під час baseline-сканування WPF/MVVM шару було зафіксовано повторення в таких категоріях:

- **Save orchestration**: повторюваний ланцюжок `validation -> save(create/update) -> notify -> reload -> navigation -> success/hide` у `ShopViewModel`, `EmployeeViewModel`, `AvailabilityViewModel.Groups`, `ContainerViewModel.Containers`.
- **Load/reload/reselect**: повтори `Load*Async` + відновлення selection/profile після save/delete і DB notifications.
- **Validation wiring**: повторювані цикли мапінгу validation keys + побудова `Dictionary<string,string>` у `EmployeeEditViewModel`, `EmployeeViewModel`, `AvailabilityEditViewModel.Validation`.
- **Navigation switching/List-Edit-Profile flows**: ідентичні переходи з `CancelTarget` та відкриттям profile/list після CRUD.
- **Busy/loading wrappers**: однаковий шаблон `ResetNavUiCts -> ShowNavWorkingAsync -> Dispatcher idle -> try/catch -> HideNavStatusAsync/ShowError` у модулях Shop/Employee/Availability/Container.

## 2) Що об’єднано (по модулях)

- **Shop**: StartAdd/EditSelected/Save/Delete/OpenProfile переведені на спільний runner для nav/busy orchestration.
- **Employee**: StartAdd/EditSelected/Save/Delete/OpenProfile переведені на спільний runner; save validation key remap переведено на shared helper без reflection invoke boilerplate.
- **Availability**: StartAdd/EditSelected/Save/Delete/OpenProfile (Groups partial) переведені на спільний runner; validation key remap в edit-validation partial переведено на shared helper.
- **Container**: у container CRUD partial спільний runner застосовано для StartAdd/Delete; додано shared idle helper в основний VM partial для уніфікації orchestrated UI idle wait.

## 3) Нові shared helpers / base methods

- `WPFApp/ViewModel/Shared/UiOperationRunner.cs`
  - `RunNavStatusFlowAsync(...)`: уніфікований wrapper для nav-status orchestration (working/idle/body/success/cancel/error).
- `WPFApp/ViewModel/Shared/ValidationDictionaryHelper.cs`
  - `RemapFirstErrors(...)`: уніфіковане перетворення validation dictionary з dedup по ключах.
  - `NormalizeLastSegment(...)`: спільна нормалізація ключа validation (останній сегмент після `.`).
- Нові локальні thin-method wrappers `WaitForUiIdleAsync()` у owner VM partials для reuse замість inline dispatcher-idle блоків.

## 4) Що прибрано як дублікати

- Повторювані try/catch/finally-like nav orchestration блоки в CRUD/navigation flows (Shop/Employee/Availability та частково Container).
- Повторювані цикли мапінгу validation dictionary (`foreach` + `ContainsKey`) у Employee/Availability validation wiring.
- Reflection-based виклик private mapper у Employee save flow замінено на прямий shared-flow map helper.

## 5) Behavior preservation notes

- Збережено порядок кроків CRUD потоків: validation -> save -> notify -> reload -> reselect/profile/list navigation.
- `CancelTarget`, `Mode`, `CurrentSection` transitions не змінені по сценаріях.
- DB-change boundary/guardrails збережені; DAL usage у WPF не додано.
- Існуючі XAML binding names/property contracts не перейменовувались.

## 6) Verification results

- `tools/check-architecture.sh` — PASSED.
- `rg "using DataAccessLayer|DataAccessLayer\." WPFApp` — 0 matches.
- `dotnet build` / `dotnet test` — не виконано, бо `dotnet` CLI відсутній у середовищі.
