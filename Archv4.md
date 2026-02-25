# Architecture v4 — Clean UI Boundary (WPF без DAL leakage)

## 1. Мета архітектури
Забезпечити strict layered boundary: Presentation (WPF) не має compile-time/type-level залежностей на DAL namespace/types.

## 2. Шари та відповідальність
- **WPFApp**: MVVM orchestration, UX/navigation, export file orchestration, binding/view-state.
- **BusinessLogicLayer.Contracts**: DTO/transport models + contract enums + request/response контракти.
- **BusinessLogicLayer**: use-cases/services/facades, mapping DAL↔Contracts, validation/business rules.
- **DataAccessLayer**: EF entities, repositories, db context, migrations.

## 3. Dependency Rules
- `WPFApp -> BusinessLogicLayer` (contracts + interfaces через BLL assembly).
- `BusinessLogicLayer -> DataAccessLayer`.
- `WPFApp -X-> DataAccessLayer` (заборонено і перевіряється guardrails).

## 4. DTO boundary по модулях
- Employee: `EmployeeDto`, `SaveEmployeeRequest` + facade.
- Shop: `ShopDto`, `SaveShopRequest` + facade.
- Database Admin: `DatabaseInfoDto`, `SqlExecutionResultDto`, `ISqliteAdminFacade`.
- Availability: UI працює з contract-моделями (`AvailabilityGroupModel`, `AvailabilityGroupMemberModel`, `AvailabilityGroupDayModel`) через `IAvailabilityGroupService`.
- Container: UI працює з contract `ContainerModel` через `IContainerService`.
- Schedule: UI працює з contract `ScheduleModel`, `ScheduleEmployeeModel`, `ScheduleSlotModel`, `ScheduleCellStyleModel`, `SlotStatus` через `IScheduleService`.
- Export: export context в WPF перейшов на contract-моделі (без DAL type).
- Home: агрегування в UI використовує contract schedule/container моделі.

## 5. Facades / Services
- Existing facades: `IEmployeeFacade`, `IShopFacade`, `ISqliteAdminFacade`.
- Service boundary contracts (for UI-facing modules):
  - `IContainerService`
  - `IScheduleService`
  - `IAvailabilityGroupService`
  - `IEmployeeService`
  - `IShopService`
  - `IBindService`

## 6. Mapping strategy
- Єдине місце DAL↔Contracts мапінгу: `BusinessLogicLayer/Mappers/ModelMapper.cs`.
- Service implementations маплять DAL repositories -> contract models і навпаки.
- DAL enums (`AvailabilityKind`, `SlotStatus`) мапляться в contract enums.

## 7. Що змінено в цьому проході
1. Повністю прибрано `DataAccessLayer.*` usage з `WPFApp` source.
2. Переведено UI-facing BLL service interfaces на contract models/enums.
3. Переписано service implementations для DAL↔Contract mapping.
4. Додано architecture test project `ArchitectureTests` + source-level boundary tests.
5. Оновлено guardrail script (використано для підтвердження clean boundary).

## 8. Підтвердження clean boundary
- У `WPFApp.csproj` немає `ProjectReference` на DAL.
- У `WPFApp` немає `using DataAccessLayer...`.
- У `WPFApp` немає `DataAccessLayer.` usage.

## 9. Guardrails / architecture tests
- `tools/check-architecture.sh` (script guardrail).
- `ArchitectureTests/WpfBoundaryTests.cs`:
  - перевірка відсутності WPF->DAL project reference;
  - перевірка відсутності `DataAccessLayer` namespace usage у WPF source.

## 10. Залишковий technical debt
No DAL leakage in WPF.
