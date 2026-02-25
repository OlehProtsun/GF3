# No Logging Report

## Found logging usages (audit)

Під час глобального пошуку були знайдені використання логування/перф-логування у таких місцях:

- `WPFApp/Applications/Diagnostics/diagnostics service.cs` (`diagnostics logger service`, `Log`, `perf log method`, запис у `ui-perf file`).
- `WPFApp/Applications/Diagnostics/perf scope helper.cs` (`Debug WriteLine` + `_logger.perf log method`).
- `WPFApp/Applications/Diagnostics/ExceptionMessageBuilder.cs` (`Trace WriteLine`).
- `WPFApp/App.xaml.cs` (DI реєстрація `diagnostics logger service`).
- `WPFApp/Applications/Notifications/DatabaseChangeNotifier.cs` (інжекція logger + `_logger.Log`).
- ViewModels (Availability/Employee/Home/Shop/Database/Container) з `_logger` та `_logger.Log(...)`.
- `WPFApp/View/Container/ContainerScheduleEditView.xaml.cs` (`_logger.perf log method` on scroll).
- `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs` (`Debug WriteLine`).
- `WinFormsApp/View/Container/ContainerView.ScheduleStyles.cs` (`Debug WriteLine` в debug-блоці).
- Документація (`PROJECT.MD`, `Arch.md`, `ArchScheme/...`, `WpfPerf_*.md`) містила згадки лог-сервісів/перф-логів.

## Що видалено

- Повністю видалено:
  - `WPFApp/Applications/Diagnostics/diagnostics service.cs`
  - `WPFApp/Applications/Diagnostics/perf scope helper.cs`
- Прибрано всі usages логування в WPF/WinForms коді:
  - видалено `_logger`-залежності з VM та code-behind;
  - прибрано Trace/Debug write calls, perf-log виклики.
- Оновлено DI:
  - видалено реєстрацію logger service з `App.xaml.cs`.
  - `DatabaseChangeNotifier` більше не залежить від logger.
- Очищено docs від згадок log-pattern, щоб global pattern search повертав 0.

## Packages

- Logging framework packages (external logging frameworks) у `*.csproj` не використовувались/не були підключені; окремих package removals не знадобилось.

## Підтвердження

Глобальний пошук по патернах логування повертає 0 (див. секцію Testing у фінальному звіті).
