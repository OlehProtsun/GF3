# Dedup Pass 3 Report

## 1) Baseline duplication map (remaining duplicates)

### High
- **Container orchestration wrappers (manual nav-status flow)**: duplicated `show working -> idle wait -> try/catch -> success/hide` blocks in `ContainerViewModel.Containers`, `ContainerViewModel.Schedules.Open`, and `ContainerViewModel.ScheduleEditor` for edit/open/delete/reselect flows.
- **Schedule edit bootstrap flow duplication**: repeated `CancelBackgroundWork + CancelScheduleEditWork + LoadLookupsAsync + ResetScheduleFilters + ClearValidationErrors` in schedule Add/Edit/MultiOpen entry points.
- **Reload/reselect/profile sync duplication**: repeated `ProfileVm.SetProfile + ListVm.SelectedItem sync` blocks around container profile transitions and post-save profile restore.

### Medium
- **Validation dictionary remap duplication**: `ShopEditViewModel` had local dictionary remap + local last-segment key normalization despite shared helper availability.
- **Mixed nav completion paths**: same methods had custom early-return hide paths mixed with success/hide wrappers.

### Low
- **Minor style duplication**: local inline utility formatting / comments around unchanged save-schedule flow.

## 2) Що уніфіковано в Container/Schedule
- Container `EditSelectedAsync`, `SaveAsync`, `OpenProfileAsync` переведені на `UiOperationRunner` (єдиний nav orchestration pattern).
- Schedule `StartScheduleAddAsync`, `EditSelectedScheduleAsync`, `MultiOpenSchedulesAsync`, `OpenScheduleProfileAsync`, `DeleteSelectedScheduleAsync` переведені на `UiOperationRunner`.
- `SelectScheduleBlockAsync` (schedule-tab reselection) також переведений на `UiOperationRunner`.
- Спільний schedule bootstrap (`PrepareScheduleEditContextAsync`) винесений для Add/Edit/MultiOpen.
- Спільний profile/list synchronization (`SetProfileAndSelectionAsync`) винесений для profile-open і post-save profile restore.

## 3) Що уніфіковано глобально (reload/reselect/validation/navigation)
- Навігаційний orchestration pattern у Container/Schedule уніфіковано через один runner сценарій (включно з reselect transitions).
- Validation key remap у Shop VM переведено на `ValidationDictionaryHelper` (remap + normalize last segment), прибрано локальний словниковий boilerplate.

## 4) Нові/оновлені shared helpers
- **Оновлено `UiOperationRunner`**: додано overload з `Func<CancellationToken, Task<bool>>` для сценаріїв, де body може завершитись без success (early-exit без exception).
- **Використано існуючий `ValidationDictionaryHelper`** для Shop validation wiring.
- **Додано вузькі локальні helper-и в Container VM**:
  - `PrepareScheduleEditContextAsync`
  - `SetProfileAndSelectionAsync`

## 5) Що прибрано як дублікати
- Прибрані повтори inline `ShowNavWorking + Dispatcher idle + try/catch/finally hide/success` у відповідних Container/Schedule flows.
- Прибрані повтори schedule-edit bootstrap підготовки.
- Прибрані повтори profile/list selection sync.
- Прибрані ручні цикли remap validation dictionary в Shop edit VM.

## 6) Behavior preservation notes
- Послідовність UX-етапів (working/success/hide, confirmations, navigation targets) збережена.
- Business/domain logic не переносилась у helper-и; винесено тільки orchestration boilerplate.
- WPF clean boundary збережено (DAL usage у WPF відсутній).

## 7) Verification results
- `tools/check-architecture.sh`: PASSED.
- `rg "using DataAccessLayer|DataAccessLayer\." WPFApp`: no matches.
- `dotnet` CLI недоступний в середовищі (`dotnet: command not found`), тому build/test не виконувались.

## 8) Що лишилось без змін (тільки фактично, без рекомендацій)
- `ContainerViewModel.Schedules.SaveGenerate` зберігає окремий save-status orchestration (`ShowSaveWorkingAsync/ShowSaveSuccessThenAutoHideAsync`) і власну валідаційну/summary pipeline логіку.
- Database reload pipeline (`ContainerViewModel.DatabaseReload`) лишився функціонально тим самим.
