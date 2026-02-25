# Architecture v2

## 1) Мета архітектури
Перейти на стабільну шарову архітектуру `Presentation → BLL.Contracts/Facades → BLL → DAL` з чітким DTO boundary, щоб UI не залежав від EF/DAL сутностей та enum-ів напряму.

## 2) Шари та відповідальність

### Presentation (`WPFApp`)
- Містить View/ViewModel, UI-валидацію, навігацію, форматування для UX.
- Працює через BLL інтерфейси/facades.
- Не має використовувати DAL entities як вхід/вихід API.

### BusinessLogicLayer.Contracts
- Контрактні типи між UI і BLL.
- DTO для читання (`XxxDto`) та окремі request-моделі для запису (`SaveXxxRequest`).
- Після рефакторингу експортний контракт `ScheduleSqlExportData` переведений на DTO (без DAL type leakage).

### BusinessLogicLayer
- Реалізує use-cases, інкапсулює бізнес-правила.
- Реалізує facade/service інтерфейси.
- Мапить DAL models ↔ contract DTO.

### DataAccessLayer
- EF Core entity models, repositories, DbContext, migrations.
- Внутрішній persistence шар.

## 3) Правила залежностей (Dependency Rules)
1. UI залежить від `BusinessLogicLayer.Contracts` та facade/service interfaces.
2. BLL залежить від DAL repositories/моделей тільки всередині бізнес-шару.
3. DAL не знає про UI/BLL.Contracts.
4. Обмін між UI і BLL має бути через DTO/request.

## 4) DTO Boundary

### Employee
- Read: `EmployeeDto`
- Write: `SaveEmployeeRequest`
- Access: `IEmployeeFacade`

### Shop
- Read: `ShopDto`
- Write: `SaveShopRequest`
- Access: `IShopFacade`

### Export (оновлено)
- `ScheduleSqlExportData` тепер містить тільки контрактні DTO:
  - `ScheduleSqlDto`
  - `ScheduleEmployeeSqlDto`
  - `ScheduleSlotSqlDto`
  - `ScheduleCellStyleSqlDto`
  - `AvailabilityGroupSqlDto`
  - `AvailabilityGroupMemberSqlDto`
  - `AvailabilityGroupDaySqlDto`
- DAL enum значення для export переводяться у string на boundary.

## 5) Facades / Services
- `IEmployeeFacade` → `EmployeeFacade`
- `IShopFacade` → `ShopFacade`
- `IScheduleExportDataBuilder` → `ScheduleExportDataBuilder` (з mapping на контрактні export DTO)

## 6) Mapping Strategy
- Мапінг локалізовано в facade/builder класах (explicit mapping methods `Map(...)`).
- Принцип: на виході з BLL назовні тільки contract DTO.
- Для export: DAL model → SQL export DTO у `ScheduleExportDataBuilder`.

## 7) Що було виправлено
1. `CS1061` в `ScheduleExportDataBuilder`:
   - `DayOfWeek` замінено на фактичне поле `DayOfMonth` для `ScheduleSlotModel` і `AvailabilityGroupDayModel`.
2. `CS0133` в `ContainerScheduleEditViewModel`:
   - alias-поля з `const` переведені на `static readonly`.
3. `CS0029/CS0019` в `EmployeeViewModel`:
   - розділено `request` (`SaveEmployeeRequest`) і `created/latest` (`EmployeeDto`), додано `savedEmployeeId`.
4. `XDG0008` для `ContainerScheduleEditView.xaml`:
   - прибрано некоректний design-time DataContext reference, runtime DataContext не зачеплено.

## 8) Що ще залишилось (technical debt)
- У `WPFApp` ще існують прямі `using DataAccessLayer...` у модулях Availability/Container/Schedule/Export.
- Для повного закриття boundary потрібно послідовно винести DTO/facade для цих модулів (особливо Schedule/Container/Availability), після чого прибрати `DataAccessLayer` project reference з WPF.
- Поточний крок закрив критичні compile blockers і прибрав leakage в export contracts.

## 9) Приклад потоку даних
`WPF ViewModel -> IEmployeeFacade -> EmployeeFacade (BLL mapping) -> EmployeeService -> Repository (DAL) -> DB`

Назад:
`DB -> DAL entity -> BLL mapping -> EmployeeDto -> WPF ViewModel`.
