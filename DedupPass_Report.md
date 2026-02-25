# Dedup Pass Report

## 1) Baseline duplication map

### High impact
- **BLL CRUD mapping boilerplate** (`GetAsync`, `GetAllAsync`, `CreateAsync`, `GetByValueAsync`) repeated across service classes with the same DAL→Contract mapping shape.
  - Locations: `BusinessLogicLayer/Services/ContainerService.cs`, `ScheduleEmployeeService.cs`, `ScheduleSlotService.cs`, `BindService.cs`, `AvailabilityGroupService.cs`, `ScheduleService.cs`, `ShopService.cs`, `EmployeeService.cs`.
- **Availability code token and parse bridging duplication** (`+`/`-` markers and redundant enum conversion in UI adapter).
  - Locations: `BusinessLogicLayer/Availability/AvailabilityCodeParser.cs`, `WPFApp/Applications/Matrix/Availability/AvailabilityCellCodeParser.cs`.

### Medium impact
- **MVVM navigation/orchestration pattern repetition** (`SwitchToListAsync` / `SwitchToEditAsync` / `SwitchToProfileAsync`, reload/reselect flows, `NotifyDatabaseChanged(...)`).
  - Locations sampled: `WPFApp/ViewModel/Shop/ShopViewModel.cs`, `WPFApp/ViewModel/Employee/EmployeeViewModel.cs`, `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.*`, `WPFApp/ViewModel/Container/Edit/ContainerViewModel.*`.
- **Schedule matrix helper layering overlap** (BLL canonical pieces + WPF adapter/wrapper pieces).
  - Locations sampled: `BusinessLogicLayer/Schedule/*`, `WPFApp/Applications/Matrix/Schedule/*`.

### Low impact
- Minor naming/structure inconsistencies and repeated comments in partial classes.

## 2) What was consolidated

- Added a shared internal mapping helper for common service mapping flows:
  - `GetMappedAsync(...)`
  - `GetMappedListAsync(...)`
  - `CreateMappedAsync(...)`
  - `ExecuteAndMapAsync(...)`
- Replaced duplicated CRUD mapping blocks in BLL services with the shared helper while preserving each service API and behavior.
- Centralized availability marker constants (`+` and `-`) in BLL parser and reused them in WPF adapter.
- Removed redundant enum roundtrip (`Enum.TryParse(parsedKind.ToString())`) in WPF availability parser adapter and used canonical parsed enum directly.
- Replaced direct DAL enum literal check in `AvailabilityGroupService` with BLL mapper-based conversion path (`AvailabilityKind.INT.ToDal()`).

## 3) What was not touched

- VM orchestration duplication across Shop/Employee/Container/Availability ViewModels was identified but not changed in this pass to avoid broad behavioral risk in navigation/selection/save UX flows.
- `WPFApp/Applications/Matrix/Schedule/ScheduleMatrixEngine` deep extraction was not changed in this pass because it contains large, behavior-sensitive table-building and conflict logic.

## 4) Impact summary

- Reduced repeated mapping boilerplate in BLL CRUD/query methods by routing common DAL→Contract flow through one reusable helper.
- Availability parsing path is more canonical (single source for `AnyMark`/`NoneMark`, no redundant enum conversion path in UI adapter).
- No architecture boundary regression introduced (WPF still has no DAL usage).

## 5) Verification

- `tools/check-architecture.sh` — **PASSED**.
- `rg "using DataAccessLayer|DataAccessLayer\." WPFApp` — **no matches**.
- `dotnet build` — **not executed successfully** (`dotnet` CLI unavailable in environment).
