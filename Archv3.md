# Architecture v3 (Incremental boundary hardening)

## 1. Мета архітектури
Відокремити Presentation (WPF) від persistence implementation (DAL) через BLL boundary, DTO та facade-інтерфейси.

## 2. Шари та відповідальність
- **WPFApp**
  - MVVM orchestration, binding/state, dialogs, file path selection, UX notifications.
  - Не повинен напряму референсити DAL project.
- **BusinessLogicLayer.Contracts** (в межах BLL проекту)
  - DTO/contract-моделі між UI та BLL.
  - Додано database admin DTO: `DatabaseInfoDto`, `SqlExecutionResultDto`.
- **BusinessLogicLayer**
  - Use-cases/services/facades, mapping DAL ↔ DTO.
  - DI composition для DAL+BLL через `AddBusinessLogicStack`.
- **DataAccessLayer**
  - EF models, repositories, db context, migrations, sqlite administration implementation.

## 3. Dependency Rules
- Дозволено: `WPFApp -> BusinessLogicLayer`.
- Дозволено: `BusinessLogicLayer -> DataAccessLayer`.
- Заборонено: `WPFApp -> DataAccessLayer` direct project reference.

## 4. DTO boundary по модулях (факт)
- **Employee/Shop**: DTO+Facade вже були.
- **Database Admin**: додано DTO+Facade boundary (`ISqliteAdminFacade` + DTO).
- **Availability/Container/Schedule/Export**: частково legacy, в UI ще є DAL leakage (див. technical debt).

## 5. Facades / Services
- Existing: `IEmployeeFacade`, `IShopFacade`.
- Existing export builder: `IScheduleExportDataBuilder`.
- Added: `ISqliteAdminFacade` + `SqliteAdminFacade`.

## 6. Mapping strategy
- Database admin mapping локалізований у `SqliteAdminFacade` (DAL types -> DTO).
- Для решти великих feature потрібне подальше винесення в окремі mapper-и.

## 7. Що змінено в цьому проході
1. Прибрано direct `ProjectReference` WPF -> DAL.
2. Перенесено DAL bootstrap із WPF в BLL extension (`AddBusinessLogicStack`).
3. Прибрано DAL admin abstraction із WPF (`DatabaseViewModel` перейшов на `ISqliteAdminFacade`).
4. Додано arch guardrail script `tools/check-architecture.sh`.

## 8. Що ще лишилось (technical debt)
- WPF namespace usage `DataAccessLayer.*` все ще існує у модулях:
  - Availability
  - Container
  - Schedule
  - Export
  - Home
  - Matrix helpers/validation rules
- Потрібні DTO/request/facade boundaries для цих модулів та заміна DAL enum usage у WPF.

## 9. Приклад потоку даних
`WPF VM -> BLL Facade -> BLL Service -> DAL Repository/EF -> DB -> DAL Model -> BLL Facade Map -> DTO -> WPF VM`

## 10. Архітектурні перевірки / guardrails
- Додано `tools/check-architecture.sh`:
  - перевіряє відсутність direct DAL project reference в `WPFApp.csproj`;
  - перевіряє наявність/відсутність `DataAccessLayer` namespace usage у `WPFApp`.
