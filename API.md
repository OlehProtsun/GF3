# API.md

## 0. Executive Summary

Проаналізовано поточний стан .NET solution як backend для міграції з WPF у Web API + React.

- Backend-ядро (DAL/BLL/WebApi) уже реалізує основні доменні модулі: Employees, Shops, Containers, AvailabilityGroups, nested CRUD для графів/слотів/учасників/стилів, а також генерацію графіка через `POST /api/containers/{containerId}/graphs/{graphId}/generate`.
- Є глобальна уніфікована обробка помилок через `ApiExceptionMiddleware` (400/404/500).
- Дані зберігаються в SQLite через EF Core (`AppDbContext`, міграції + `Database.Migrate()` на старті Web API).
- **Backend complete for React MVP: YES (із застереженнями).**
- **Backend complete for full parity with WPF: NO.** Частина функцій лишається в WPF-шарі (Excel/SQL export workflows, DB admin UI flow, file dialogs/scripts, частина UX-орієнтованих сценаріїв).

---

## 1. Solution & Projects Map

## 1.1 Solution

- `GF3.sln`

## 1.2 Projects inventory (`*.csproj`)

1. `DataAccessLayer/DataAccessLayer.csproj`
   - TargetFramework: `net10.0`
   - Призначення: EF Core моделі, DbContext, міграції, репозиторії, SQLite admin service.
   - PackageReference:
     - `Microsoft.EntityFrameworkCore`
     - `Microsoft.EntityFrameworkCore.Design`
     - `Microsoft.EntityFrameworkCore.Sqlite`
     - `Microsoft.EntityFrameworkCore.Tools`

2. `BusinessLogicLayer/BusinessLogicLayer.csproj`
   - TargetFramework: `net10.0`
   - Призначення: business services/facades/validators/generator/orchestration.
   - ProjectReference:
     - `DataAccessLayer`

3. `GF3.WebApi/WebApi.csproj`
   - TargetFramework: `net10.0`
   - Призначення: ASP.NET Core host, controllers, API contracts/mappers, middleware.
   - PackageReference:
     - `Microsoft.AspNetCore.OpenApi`
     - `Swashbuckle.AspNetCore`
   - ProjectReference:
     - `BusinessLogicLayer`
     - `DataAccessLayer`

4. `WPFApp/WPFApp.csproj`
   - TargetFramework: `net10.0-windows`
   - OutputType: `WinExe`
   - `UseWPF=true`
   - Призначення: legacy/desktop UI (джерело функціоналу для parity-аудиту).
   - ProjectReference:
     - `BusinessLogicLayer`

5. `ArchitectureTests/ArchitectureTests.csproj`
   - TargetFramework: `net10.0`
   - Призначення: архітектурні unit tests.

## 1.3 Dependency graph

- `GF3.WebApi` → `BusinessLogicLayer` → `DataAccessLayer`
- `GF3.WebApi` → `DataAccessLayer` (прямий reference теж присутній)
- `WPFApp` → `BusinessLogicLayer`
- `ArchitectureTests` (без ProjectReference на core-проєкти у csproj; орієнтований на тести архітектури)

---

## 2. Runtime architecture (WebApi host, BLL, DAL, SQLite)

1. **Startup / host** (`GF3.WebApi/Program.cs`)
   - `AddControllers`, `AddProblemDetails`, Swagger.
   - Визначення connection string:
     - спочатку `ConnectionStrings:Default` з конфіга;
     - fallback: `%LocalAppData%/GF3/SQLite.db`.
   - `AddBusinessLogicStack(cs)` реєструє DAL + BLL + SQLite admin service.
   - На старті викликається `db.Database.Migrate()`.

2. **BLL orchestration**
   - Фасади (`IEmployeeFacade`, `IShopFacade`) для API-friendly CRUD.
   - Сервіси (`IContainerService`, `IAvailabilityGroupService`, ін.) координують репозиторії, валідацію й генерацію.

3. **DAL / EF Core**
   - `AppDbContext` описує таблиці, індекси, FK, check constraints.
   - Репозиторії інкапсулюють доступ до БД (generic + спеціалізовані методи).

4. **DB**
   - SQLite, міграції в `DataAccessLayer/Migrations`.

---

## 3. DAL (Entities, Repositories, DB invariants)

## 3.1 AppDbContext

- Файл: `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`
- DbSet:
  - `Containers`
  - `Employees`
  - `Shops`
  - `Schedules`
  - `ScheduleEmployees`
  - `ScheduleSlots`
  - `ScheduleCellStyles`
  - `AvailabilityBinds`
  - `AvailabilityGroups`
  - `AvailabilityGroupMembers`
  - `AvailabilityGroupDays`

### Key Fluent configurations

- Container: required `Name`, unique index on `Name`.
- Employee: required first/last name, unique `(FirstName, LastName)`.
- Shop: required `Name`, `Address`, unique `Name`.
- Schedule:
  - FK to Container, Shop (`DeleteBehavior.Restrict`), optional FK to AvailabilityGroup (`SetNull`).
  - Indexes on container/shop/month combinations.
  - Check constraints для `month`, limits, shift string format/time ordering (частково через DB triggers за коментарем).
- ScheduleEmployee:
  - FK to Schedule/Employee (`Cascade`).
  - unique `(ScheduleId, EmployeeId)`.
- ScheduleSlot:
  - FK Schedule (`Cascade`), Employee (`SetNull`).
  - unique `(ScheduleId, DayOfMonth, FromTime, ToTime, SlotNo)`.
  - filtered unique `(ScheduleId, DayOfMonth, FromTime, ToTime, EmployeeId)` де `employee_id IS NOT NULL`.
  - check constraints: `day_of_month`, `slot_no`, status/employee consistency, time format/order.
- ScheduleCellStyle:
  - FK Schedule (`Cascade`), Employee (`Cascade`).
  - unique `(ScheduleId, DayOfMonth, EmployeeId)`.
- BindModel: unique `Key`.
- AvailabilityGroup: unique `(Year, Month, Name)`, month constraint.
- AvailabilityGroupMember: unique `(AvailabilityGroupId, EmployeeId)`.
- AvailabilityGroupDay: unique `(AvailabilityGroupMemberId, DayOfMonth)` + day/kind/interval check constraints.

## 3.2 Entities by module

- Employees/shops/containers:
  - `EmployeeModel`, `ShopModel`, `ContainerModel`
- Availability module:
  - `AvailabilityGroupModel`
  - `AvailabilityGroupMemberModel`
  - `AvailabilityGroupDayModel`
  - `BindModel`
- Schedule (graph) module:
  - `ScheduleModel`
  - `ScheduleSlotModel`
  - `ScheduleEmployeeModel`
  - `ScheduleCellStyleModel`
- Enums:
  - `DataAccessLayer/Models/Enums/SlotStatus.cs`
  - `DataAccessLayer/Models/Enums/AvailabilityKind.cs`

## 3.3 Repository layer

### Abstractions

- Generic: `IBaseRepository<TEntity>`.
- Domain-specific:
  - `IContainerRepository`
  - `IEmployeeRepository`
  - `IShopRepository`
  - `IScheduleRepository`
  - `IScheduleEmployeeRepository`
  - `IScheduleSlotRepository`
  - `IScheduleCellStyleRepository`
  - `IBindRepository`
  - `IAvailabilityGroupRepository`
  - `IAvailabilityGroupMemberRepository`
  - `IAvailabilityGroupDayRepository`

### Implementations

- Generic CRUD + tracker reset: `GenericRepository<TEntity>`.
- Important specialized methods:
  - `ScheduleSlotRepository.ReplaceForScheduleAsync(...)` (transaction + optional overwrite + bulk insert).
  - `AvailabilityGroupRepository.GetFullByIdAsync(...)` (members + days eager load).
  - `AvailabilityGroupDayRepository.GetByGroupIdAsync(...)` (join without N+1).
  - `EmployeeRepository.HasAvailabilityReferencesAsync/HasScheduleReferencesAsync`.
  - `ShopRepository.HasScheduleReferencesAsync`.
  - `BindRepository.UpsertByKeyAsync`.

### Transactions / bulk ops

- Явна транзакція: `ScheduleSlotRepository.ReplaceForScheduleAsync` (`BeginTransactionAsync`, `ExecuteDeleteAsync`, `AddRangeAsync`, `CommitAsync`).
- Batch-like operations:
  - `AvailabilityGroupDayRepository.AddRangeAsync`
  - `AvailabilityGroupDayRepository.DeleteByMemberIdAsync`
  - `ExecuteDeleteAsync` у slot replace.

## 3.4 DB invariants (важливе)

- Унікальності (приклади):
  - employee full name
  - shop name
  - group `(year,month,name)`
  - group member `(group,employee)`
  - day `(member,dayOfMonth)`
  - slot uniqueness per time/day/slotNo
  - slot uniqueness per employee in same time window
  - cell style uniqueness `(schedule,day,employee)`
- Типові `DbUpdateException` сценарії:
  - duplicate slot keys;
  - duplicate group member;
  - duplicate availability day;
  - duplicate employee/shop/group names.
- Обробка вище:
  - для слотів у `ContainerService` перетворення у `BusinessLogicLayer.Common.ValidationException` з user-friendly message;
  - інші конфлікти, якщо не перехоплені в сервісі, доходять до middleware і стають 500 (або 400, якщо кинуто ValidationException/DataAnnotations ValidationException).

---

## 4. BLL (Services, Facades, Generator, Validation)

## 4.1 DI registration

- Файл: `BusinessLogicLayer/Extensions.cs`
- `AddBusinessLogicLayer()`:
  - scoped: main services/facades/builders (`IContainerService`, `IEmployeeService`, `IShopService`, `IScheduleService`, `IAvailabilityGroupService`, `IEmployeeFacade`, `IShopFacade`, тощо)
  - transient: `IScheduleGenerator`
- `AddBusinessLogicStack(...)`:
  - викликає `AddDataAccess(connectionString)` із DAL;
  - додає `ISqliteAdminService` singleton (через `SqliteAdminService(connectionString,databasePath)`).
- Connection string sources:
  - overload із явним рядком,
  - або `GF3_CONNECTION_STRING`,
  - або fallback `%LocalAppData%/GF3/SQLite.db`.

## 4.2 Service/facade layer

### Facades

- `IEmployeeFacade` / `EmployeeFacade`:
  - GetAll/GetByValue/Get/Create/Update/Delete.
- `IShopFacade` / `ShopFacade`:
  - аналогічний CRUD.

### Core services

- `IContainerService` / `ContainerService`:
  - container CRUD;
  - graph CRUD;
  - nested CRUD: graph slots/employees/cell-styles;
  - generate graph (`GenerateGraphAsync`).
- `IAvailabilityGroupService` / `AvailabilityGroupService`:
  - group CRUD;
  - nested CRUD: members + slots;
  - `LoadFullAsync`, `SaveGroupAsync`.
- `IScheduleService` / `ScheduleService`:
  - schedule CRUD/search/details, `SaveWithDetailsAsync`.
- `IEmployeeService`, `IShopService`, `IBindService`, `ISqliteAdminFacade` тощо.

## 4.3 Generator (core)

- Контракт: `IScheduleGenerator.GenerateAsync(schedule, availabilities, employees, progress, ct)`.
- Реалізація: `ScheduleGenerator` (алгоритм фазового розподілу слотів).
- `ContainerService.GenerateGraphAsync(...)` orchestration:
  1. lock per `graphId`: `ConcurrentDictionary<int, SemaphoreSlim>`;
  2. ownership check (`containerId`↔`graphId`);
  3. load graph, graph employees, linked availability group (`GetFullByIdAsync`);
  4. call generator;
  5. assign `ScheduleId`, reset ids;
  6. якщо `dryRun=false` → `ReplaceForScheduleAsync(graphId, slots, overwrite)`;
  7. повертає counts + slots.
- `dryRun`/`overwrite` semantics:
  - `dryRun=true`: в БД нічого не пишеться;
  - `overwrite=true`: попередні slots графа видаляються перед вставкою;
  - `overwrite=false`: пише поверх, можливі unique-conflicts.

## 4.4 Validation & errors in BLL

- `BusinessLogicLayer.Common.ValidationException` — lightweight domain validation exception.
- DataAnnotations `ValidationException` теж активно використовується (наприклад у `EmployeeService`, `ShopService`, `AvailabilityGroupService`).
- Duplicate slot `DbUpdateException` у `ContainerService` мапиться в `ValidationException` (400 через middleware).
- `KeyNotFoundException`/`InvalidOperationException` використовується для 404 (ownership checks, missing graph/member/slot/group).

---

## 5. Web API (Pipeline, Middleware, Contracts/Mappers)

## 5.1 Program.cs pipeline

- `AddControllers()`, `AddProblemDetails()`, Swagger/OpenAPI.
- Connection string:
  - appsettings `ConnectionStrings:Default`; fallback `%LocalAppData%/GF3/SQLite.db`.
- `AddBusinessLogicStack(cs)`.
- Auto migration: `db.Database.Migrate()` on startup.
- Middleware order:
  - Swagger
  - `ApiExceptionMiddleware`
  - `MapControllers()`

## 5.2 ApiExceptionMiddleware

- Файл: `GF3.WebApi/Middleware/ApiExceptionMiddleware.cs`
- Handles:
  - `System.ComponentModel.DataAnnotations.ValidationException` → 400 JSON `{ type: validation_error, ... }`
  - `BusinessLogicLayer.Common.ValidationException` → 400 same shape
  - `KeyNotFoundException` → 404 `application/problem+json`
  - `InvalidOperationException` → 404 `application/problem+json`
  - fallback `Exception` → 500 `application/problem+json` with generic message

## 5.3 Contracts & mappers

- Contracts structure:
  - `Contracts/Employees/*`
  - `Contracts/Shops/*`
  - `Contracts/Containers/*` (+ nested `Graphs/Slots|Employees|CellStyles`)
  - `Contracts/AvailabilityGroups/*` (+ `Members`, `Slots`)
- Mapper classes (manual):
  - `EmployeeMapper`, `ShopMapper`, `ContainerMapper`
  - `GraphMapper`, `ContainerGraphMapper`, `GraphSlotMapper`, `GraphEmployeeMapper`, `GraphCellStyleMapper`
  - `AvailabilityGroupMapper`, `AvailabilityGroupNestedMapper`
- Mapping style: explicit extension methods request→BLL models and BLL models→API DTOs.

---

## 6. Endpoints Catalog (таблиця)

> Базовано на `GF3.WebApi/Controllers/*`.

| Module | Method | Route | Request body | Response body | Typical statuses |
|---|---|---|---|---|---|
| Health | GET | `/api/health` | — | `{ status, canConnect }` | 200 |
| Employees | GET | `/api/employees` | — | `IEnumerable<EmployeeDto>` | 200, 500 |
| Employees | GET | `/api/employees/{id}` | — | `EmployeeDto` | 200, 404, 500 |
| Employees | POST | `/api/employees` | `CreateEmployeeRequest` | `EmployeeDto` | 201, 400, 500 |
| Employees | PUT | `/api/employees/{id}` | `UpdateEmployeeRequest` | — | 204, 400, 404, 500 |
| Employees | DELETE | `/api/employees/{id}` | — | — | 204, 400, 404, 500 |
| Shops | GET | `/api/shops` | — | `IEnumerable<ShopDto>` | 200, 500 |
| Shops | GET | `/api/shops/{id}` | — | `ShopDto` | 200, 404, 500 |
| Shops | POST | `/api/shops` | `CreateShopRequest` | `ShopDto` | 201, 400, 500 |
| Shops | PUT | `/api/shops/{id}` | `UpdateShopRequest` | — | 204, 400, 404, 500 |
| Shops | DELETE | `/api/shops/{id}` | — | — | 204, 404, 500 |
| Containers | GET | `/api/containers` | — | `IEnumerable<ContainerDto>` | 200, 500 |
| Containers | GET | `/api/containers/{id}` | — | `ContainerDto` | 200, 404, 500 |
| Containers | POST | `/api/containers` | `CreateContainerRequest` | `ContainerDto` | 201, 400, 500 |
| Containers | PUT | `/api/containers/{id}` | `UpdateContainerRequest` | — | 204, 400, 404, 500 |
| Containers | DELETE | `/api/containers/{id}` | — | — | 204, 404, 500 |
| Graphs | GET | `/api/containers/{containerId}/graphs` | — | `IEnumerable<GraphDto>` | 200, 404, 500 |
| Graphs | GET | `/api/containers/{containerId}/graphs/{graphId}` | — | `GraphDto` | 200, 404, 500 |
| Graphs | POST | `/api/containers/{containerId}/graphs` | `CreateGraphRequest` | `GraphDto` | 201, 400, 404, 500 |
| Graphs | PUT | `/api/containers/{containerId}/graphs/{graphId}` | `UpdateGraphRequest` | — | 204, 400, 404, 500 |
| Graphs | DELETE | `/api/containers/{containerId}/graphs/{graphId}` | — | — | 204, 404, 500 |
| Graph Generate | POST | `/api/containers/{containerId}/graphs/{graphId}/generate` | `GenerateGraphRequest` | `GenerateGraphResponse` | 200, 400, 404, 500 |
| Graph Slots | GET | `/api/containers/{containerId}/graphs/{graphId}/slots` | — | `IEnumerable<GraphSlotDto>` | 200, 404 |
| Graph Slots | POST | `/api/containers/{containerId}/graphs/{graphId}/slots` | `CreateGraphSlotRequest` | `GraphSlotDto` | 201, 400, 404 |
| Graph Slots | PUT | `/api/containers/{containerId}/graphs/{graphId}/slots/{slotId}` | `UpdateGraphSlotRequest` | — | 204, 400, 404 |
| Graph Slots | DELETE | `/api/containers/{containerId}/graphs/{graphId}/slots/{slotId}` | — | — | 204, 404 |
| Graph Employees | GET | `/api/containers/{containerId}/graphs/{graphId}/employees` | — | `IEnumerable<GraphEmployeeDto>` | 200, 404 |
| Graph Employees | POST | `/api/containers/{containerId}/graphs/{graphId}/employees` | `AddGraphEmployeeRequest` | `GraphEmployeeDto` | 201, 404 |
| Graph Employees | PUT | `/api/containers/{containerId}/graphs/{graphId}/employees/{graphEmployeeId}` | `UpdateGraphEmployeeRequest` | — | 204, 404 |
| Graph Employees | DELETE | `/api/containers/{containerId}/graphs/{graphId}/employees/{graphEmployeeId}` | — | — | 204, 404 |
| Graph CellStyles | GET | `/api/containers/{containerId}/graphs/{graphId}/cell-styles` | — | `IEnumerable<GraphCellStyleDto>` | 200, 404 |
| Graph CellStyles | PUT | `/api/containers/{containerId}/graphs/{graphId}/cell-styles` | `UpsertGraphCellStyleRequest` | `GraphCellStyleDto` | 200, 404 |
| Graph CellStyles | DELETE | `/api/containers/{containerId}/graphs/{graphId}/cell-styles/{styleId}` | — | — | 204, 404 |
| Availability groups | GET | `/api/availability-groups` | — | `IEnumerable<AvailabilityGroupDto>` | 200, 500 |
| Availability groups | GET | `/api/availability-groups/{id}` | — | `AvailabilityGroupDto` | 200, 404, 500 |
| Availability groups | GET | `/api/availability-groups/{id}/items` | — | `IEnumerable<AvailabilityGroupItemDto>` | 200, 404, 500 |
| Availability members | GET | `/api/availability-groups/{groupId}/members` | — | `IEnumerable<AvailabilityGroupMemberDto>` | 200, 404, 500 |
| Availability members | POST | `/api/availability-groups/{groupId}/members` | `CreateAvailabilityGroupMemberRequest` | `AvailabilityGroupMemberDto` | 201, 400, 404, 500 |
| Availability members | PUT | `/api/availability-groups/{groupId}/members/{memberId}` | `UpdateAvailabilityGroupMemberRequest` | — | 204, 400, 404, 500 |
| Availability members | DELETE | `/api/availability-groups/{groupId}/members/{memberId}` | — | — | 204, 404, 500 |
| Availability slots | GET | `/api/availability-groups/{groupId}/slots` | — | `IEnumerable<AvailabilitySlotDto>` | 200, 404, 500 |
| Availability slots | POST | `/api/availability-groups/{groupId}/slots` | `CreateAvailabilitySlotRequest` | `AvailabilitySlotDto` | 201, 400, 404, 500 |
| Availability slots | PUT | `/api/availability-groups/{groupId}/slots/{slotId}` | `UpdateAvailabilitySlotRequest` | — | 204, 400, 404, 500 |
| Availability slots | DELETE | `/api/availability-groups/{groupId}/slots/{slotId}` | — | — | 204, 404, 500 |
| Availability groups | POST | `/api/availability-groups` | `CreateAvailabilityGroupRequest` | `AvailabilityGroupDto` | 201, 400, 500 |
| Availability groups | PUT | `/api/availability-groups/{id}` | `UpdateAvailabilityGroupRequest` | — | 204, 400, 404, 500 |
| Availability groups | DELETE | `/api/availability-groups/{id}` | — | — | 204, 404, 500 |

---

## 7. Key Execution Flows (детальні)

## 7.1 Create graph slot flow

1. HTTP: `POST /api/containers/{containerId}/graphs/{graphId}/slots`.
2. `ContainersController.CreateGraphSlot(...)`:
   - map request → BLL model.
3. `IContainerService.CreateGraphSlotAsync(...)`:
   - `EnsureGraphOwnershipAsync` (container існує, graph існує, graph belongs to container);
   - set `ScheduleId=graphId`;
   - `_slotRepo.AddAsync(...)`;
   - `DbUpdateException` => `ValidationException("Duplicate slot...")`.
4. DAL `GenericRepository.AddAsync` → `SaveChangesAsync`.
5. DB enforce unique/check constraints.
6. Response: `201 Created` з `GraphSlotDto` або `400` validation.

## 7.2 Generate graph flow

1. HTTP: `POST /api/containers/{containerId}/graphs/{graphId}/generate`.
2. Controller передає `overwrite`, `dryRun` у `GenerateGraphAsync`.
3. BLL `ContainerService.GenerateGraphAsync`:
   - lock on `graphId` (`ConcurrentDictionary + SemaphoreSlim`);
   - ownership checks;
   - load graph + graph employees + availability group (якщо прив’язана);
   - call `IScheduleGenerator.GenerateAsync(...)`;
   - normalize output slots (`Id=0`, `ScheduleId=graphId`);
   - якщо не dryRun: `_slotRepo.ReplaceForScheduleAsync(..., overwrite)`;
   - map result.
4. DAL `ReplaceForScheduleAsync`:
   - transaction begin;
   - optional delete old slots;
   - add range;
   - save + commit.
5. Controller формує `GenerateGraphResponse`:
   - `Slots` повертаються якщо `dryRun || returnSlots`.

## 7.3 Availability member slot CRUD flow

1. HTTP: `POST/PUT/DELETE /api/availability-groups/{groupId}/slots...`.
2. `AvailabilityGroupsController` map request DTO → BLL model.
3. `AvailabilityGroupService`:
   - `EnsureGroupExistsAsync`;
   - ownership check member↔group (`EnsureMemberBelongsToGroupAsync`);
   - create/update/delete через `_dayRepo`.
4. DAL repository записує в `availability_group_day`.
5. DB constraints enforce unique `(member,dayOfMonth)` + valid kind/interval/day range.
6. Помилки → middleware (400/404/500).

## 7.4 API error flow

1. Будь-який controller/service кидає exception.
2. `ApiExceptionMiddleware` перетворює у стандартизований JSON.
3. Типи:
   - Validation → 400;
   - KeyNotFound/InvalidOperation → 404;
   - інше → 500 generic.

---

## 8. Backend Completion Audit (WPF decoupling analysis)

## 8.1 Is backend 100% complete?

### Already exposed via Web API (coverage)

- CRUD: Employees, Shops, Containers.
- Graph CRUD within Containers.
- Graph nested CRUD:
  - slots,
  - graph employees,
  - graph cell-styles.
- Graph generation endpoint with `dryRun/overwrite/returnSlots` behavior.
- AvailabilityGroups CRUD + nested members/slots + `/items` aggregation.
- Health endpoint (`/api/health`).
- Global error middleware.

### WPF scan findings

Пошук робився по `WPFApp/**` на прямі залежності/фічі:
- **Direct DAL access in WPF:** `Not found` (search: `using DataAccessLayer|DataAccessLayer.`).
- **BLL usage in WPF:** `Found` (ViewModels/App DI inject BLL services/facades).
- **Features still only in WPF flows (not exposed by WebApi controllers):**
  1. **Export to Excel / SQL scripts** (`WPFApp/Applications/Export/ScheduleExportService.cs`, `IScheduleExportService.cs`).
  2. **Database admin interactive tooling** (execute arbitrary SQL, import `.sql`, hash, file metadata) through WPF `DatabaseViewModel` + `ISqliteAdminFacade`.
  3. **File-system/dialog driven workflows** (`OpenFileDialog`, file load/save around export/import).
  4. **WPF-specific matrix/UI composition flows** (table rendering helpers, dialog UX), even якщо частина обчислень делегована у BLL.

### Feature parity table

| Feature / Module | Де реалізовано зараз | Endpoint є? | Що зробити |
|---|---|---|---|
| Employees CRUD | BLL + WebApi + WPF UI | Yes | React-клієнт поверх існуючих endpoints |
| Shops CRUD | BLL + WebApi + WPF UI | Yes | React integration |
| Containers CRUD | BLL + WebApi + WPF UI | Yes | React integration |
| Graph CRUD + nested slots/employees/styles | BLL + WebApi + WPF UI | Yes | React graph editor |
| Graph generation | BLL + WebApi + WPF UI | Yes | React generate UX + polling/progress (за потреби) |
| AvailabilityGroups CRUD + members/slots/items | BLL + WebApi + WPF UI | Yes | React availability UI |
| Health check | WebApi | Yes | Використати в launcher readiness flow |
| SQL/Excel Export service | WPF (+ BLL helper builder partially) | **No** | Додати API export endpoints або backend job-service |
| Database admin (execute/import sql) | WPF + BLL (`ISqliteAdminFacade`) | **No** | Якщо потрібно в React: додати secure admin endpoints |
| UI dialogs/color picker | WPF only | N/A | React UI equivalent (backend не потрібен) |

### Conclusion

- **Backend complete for React MVP:** **YES, з умовою**, що MVP не включає WPF-specific export/admin workflows.
- **Backend complete for full parity with WPF:** **NO**.

#### Blockers / risks

1. Відсутні API endpoint-и для export/import/admin сценаріїв, які наразі є тільки у WPF.
2. Потенційний ризик безпеки при винесенні “execute arbitrary SQL” у WebApi (потрібна рольова модель/локальний-only режим).
3. Відсутній React hosting strategy в WebApi (CORS/static fallback ще не налаштовані).
4. Відсутній launcher-oriented порт/ready orchestration у коді WebApi.

---

## 9. Next Steps (React hosting + exe launcher + remaining backend work)

## 9.1 Remaining backend work for parity

1. Додати export endpoint-и (мінімум):
   - schedule → SQL file/script,
   - schedule → Excel,
   - container template export.
2. Якщо потрібен Database screen у React:
   - endpoints для DB info/hash/import script/execute sql (дуже обережно з безпекою).
3. Явно визначити API contracts для цих фіч (DTO, error model, authorization mode).

## 9.2 For React integration

- Якщо React запускається окремо під час dev: потрібен `UseCors` + policy.
- Якщо React буде served from backend у runtime:
  - publish frontend build у `wwwroot`;
  - додати static files middleware + `MapFallbackToFile("index.html")`.
- У поточному `Program.cs`: **CORS/static hosting not found** (search в `GF3.WebApi/Program.cs`).

## 9.3 For exe launcher scenario

1. Publish WebApi як self-contained executable.
2. Launcher flow:
   - стартує процес backend;
   - опитує `/api/health` до `status=ok && canConnect=true`;
   - відкриває браузер на локальний URL.
3. Port strategy:
   - deterministic fixed port (просте рішення) або
   - dynamic free-port + передачa порта в React/launcher context.
4. Connection string strategy:
   - керувати через `GF3_CONNECTION_STRING` у launcher env, або лишити локальний fallback.

## 9.4 Testing recommendations

- Мінімум для regression:
  - підтримувати `GF3.WebApi/ApiSamples.http` як smoke сценарії CRUD + generate.
- Recommended integration tests (не реалізовано тут):
  1. end-to-end CRUD per module;
  2. generate dryRun/overwrite behavior;
  3. duplicate-slot constraint mapping to 400;
  4. ownership 404 cases (`graph not in container`, `slot not in graph`);
  5. migration/startup health scenario.

---

## 10. Appendix (packages, entry points, config)

## 10.1 External packages (core backend)

- DAL:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.Design`
  - `Microsoft.EntityFrameworkCore.Sqlite`
  - `Microsoft.EntityFrameworkCore.Tools`
- WebApi:
  - `Microsoft.AspNetCore.OpenApi`
  - `Swashbuckle.AspNetCore`

## 10.2 Entry points

- Web API host entry: `GF3.WebApi/Program.cs`
- WPF entry (legacy app): `WPFApp/App.xaml.cs`

## 10.3 Config keys / env vars

- `ConnectionStrings:Default` (WebApi appsettings).
- `GF3_CONNECTION_STRING` (BLL stack helper fallback source).
- Local fallback DB path pattern:
  - `%LocalAppData%/GF3/SQLite.db`.

## 10.4 Additional “Not found” checks

- WebApi controllers for export/import/sqlite-admin features: **Not found** in `GF3.WebApi/Controllers` (search by `Bind|Sqlite|Export|Import|Database`).
- Direct WPF usage of `DataAccessLayer` namespace: **Not found** in `WPFApp/**`.

---

## Backend ready checklist

### ✅ Must-have done

- CRUD API для Employees/Shops/Containers/AvailabilityGroups.
- Nested CRUD API для графа (slots/employees/cell-styles, members/availability slots).
- Graph generation endpoint з `dryRun/overwrite`.
- Global exception middleware.
- EF Core migrations + startup migrate.

### ⚠️ Should-do soon

- React hosting strategy (CORS або static hosting + fallback).
- Launcher orchestration (health-based readiness + port strategy).
- Інтеграційні smoke/regression тести для endpoint contract stability.

### ❌ Missing for full parity

- WebApi endpoints для Excel/SQL export workflows.
- WebApi endpoints для DB-admin сценаріїв (якщо їх треба перенести у React).
