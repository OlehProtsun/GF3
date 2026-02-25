# Project Architecture (AS-IS, Updated)

## 1. Executive Summary
GF3 is a desktop scheduling system with three active layers:
- **DAL** (`DataAccessLayer`): EF Core entities, repositories, SQLite persistence, migrations, DB admin tooling.
- **BLL** (`BusinessLogicLayer`): business services, validation, parsers, feature facades, and export data preparation.
- **Presentation** (`WPFApp`): MVVM UI, dialogs, matrix/grid shaping, view-specific validation UX, Excel/SQL file orchestration.

This refactor focused on boundary hardening without changing the 3-layer style:
- ✅ `SqliteAdminService` remains in DAL and is injected into WPF via DAL interface.
- ✅ **Shop** WPF flow now uses BLL DTO contracts + BLL facade (`ShopDto`, `SaveShopRequest`, `IShopFacade`).
- ✅ **Employee** WPF flow now uses BLL DTO contracts + BLL facade (`EmployeeDto`, `SaveEmployeeRequest`, `IEmployeeFacade`).
- ✅ Availability cell parsing in WPF now delegates canonical parsing semantics to BLL parser (`AvailabilityCodeParser`).
- ✅ Schedule pure-time/totals logic started extraction into BLL schedule services with WPF adapters.
- ✅ Export split started by adding BLL export data builder (`IScheduleExportDataBuilder`) for SQL export data shaping.

## 2. Solution and Project Structure
- `DataAccessLayer` → DAL
- `BusinessLogicLayer` → BLL
- `WPFApp` → Presentation
- `WinFormsApp` → additional presentation client (legacy/parallel)

Major folders:
- DAL: `Models`, `Repositories`, `Migrations`, `Administration`
- BLL: `Services`, `Services/Abstractions`, `Contracts`, `Availability`, `Schedule`, `Generators`
- WPF: `ViewModel`, `View`, `Applications`, `UI`, `MVVM`, `Resources`

## 3. Layered Architecture Overview

### 3.1 Presentation (WPFApp)
Responsibilities:
- MVVM orchestration and state transitions.
- `DataTable`/`DataView` shaping and DataGrid columns/styles.
- dialogs, notifications, status indicators, file-save UX.
- Excel template path resolution and workbook write orchestration.

Boundary updates:
- Shop and Employee ViewModels now rely on BLL contracts/facades, not DAL model types.
- WPF validation rules for these features now validate request DTOs.

### 3.2 BLL
Responsibilities:
- CRUD business rules via existing services (`IShopService`, `IEmployeeService`, etc.).
- Presentation-facing facades:
  - `IShopFacade` / `ShopFacade`
  - `IEmployeeFacade` / `EmployeeFacade`
- Contracts:
  - Shops: `ShopDto`, `SaveShopRequest`
  - Employees: `EmployeeDto`, `SaveEmployeeRequest`
- Availability canonical parsing:
  - `AvailabilityCodeParser`
- Schedule pure logic extraction:
  - `ScheduleMatrixEngine.TryParseTime`
  - `ScheduleTotalsCalculator`
  - `ScheduleMatrixConstants`
- Export domain shaping (initial split):
  - `IScheduleExportDataBuilder` / `ScheduleExportDataBuilder`
  - `ScheduleSqlExportData`

### 3.3 DAL
Responsibilities remain unchanged:
- EF entities (`ShopModel`, `EmployeeModel`, `ScheduleModel`, etc.)
- repositories and persistence abstractions
- migrations and db context factory
- administration (`ISqliteAdminService`, `SqliteAdminService`, `SqlExecutionResult`, `DatabaseInfo`)

## 4. Dependency Flow and Boundaries
Intended and current flow:
- Presentation → BLL contracts/facades/services
- BLL → DAL repositories/entities
- DAL → EF Core/SQLite

Hard boundaries implemented:
- Shop WPF no longer imports DAL `ShopModel`.
- Employee WPF no longer imports DAL `EmployeeModel`.

Remaining known leaks (outside this targeted pass):
- Some schedule/container/home Presentation areas still use DAL entities directly.
- Export context types in WPF still include DAL entities in service-level context objects.

## 5. Feature-by-Feature Boundary Map

### Shop
- Presentation: `ShopViewModel`, `ShopListViewModel`, `ShopEditViewModel`, `ShopProfileViewModel`, `ShopDisplayHelper`
- BLL: `IShopFacade`, `ShopFacade`, `ShopDto`, `SaveShopRequest`, `IShopService`
- DAL: `ShopModel`, `IShopRepository`
- Flow: WPF DTO/request ↔ BLL facade mapping ↔ DAL model/service/repository.

### Employee
- Presentation: `EmployeeViewModel`, `EmployeeListViewModel`, `EmployeeEditViewModel`, `EmployeeProfileViewModel`, `EmployeeDisplayHelper`
- BLL: `IEmployeeFacade`, `EmployeeFacade`, `EmployeeDto`, `SaveEmployeeRequest`, `IEmployeeService`
- DAL: `EmployeeModel`, `IEmployeeRepository`
- Flow mirrors Shop.

### Availability
- Presentation: matrix engine + UI validation wrappers.
- BLL: `AvailabilityCodeParser` as canonical parser/interval normalization truth.
- DAL: availability enums/models persisted by existing services.

### Schedule
- Presentation: DataGrid-specific matrix building/rendering remains in WPF.
- BLL: core parse/totals constants extracted and called by WPF wrappers.

### Export
- Presentation: file/template discovery, dialog pathing, workbook write.
- BLL: SQL export data shaping introduced via export builder and export DTO.
- Pending: deeper migration of WPF export context classes to BLL contracts.

## 6. Detailed Folder-by-Folder Map

### `DataAccessLayer`
- `Administration/`: SQLite admin service (DAL-safe, no WPF deps).
- `Models/`: persistence entities + enums + dbcontext.
- `Repositories/`: repository abstractions/implementations.
- `Migrations/`: EF migration history.

### `BusinessLogicLayer`
- `Contracts/Shops`, `Contracts/Employees`, `Contracts/Export`: presentation-safe DTO boundaries.
- `Services/`: domain/business services and facades.
- `Services/Abstractions/`: interfaces for DI and decoupling.
- `Availability/`: parsing/validation helpers.
- `Schedule/`: extracted pure schedule logic.
- `Generators/`: schedule generation.

### `WPFApp`
- `ViewModel/`: MVVM feature composition and command orchestration.
- `Applications/Matrix`: WPF matrix orchestration and adapters to BLL pure logic.
- `Applications/Export`: template/file orchestration.
- `MVVM/Validation/Rules`: view-model validation UX using request DTOs.
- `UI/`, `View/`, `Resources/`: WPF-specific artifacts.

## 7. File-by-File Reference (targeted high-value)

### BLL added/updated
- `BusinessLogicLayer/Services/ShopFacade.cs`: maps Shop DTO/request ↔ DAL entity via `IShopService`.
- `BusinessLogicLayer/Services/EmployeeFacade.cs`: maps Employee DTO/request ↔ DAL entity via `IEmployeeService`.
- `BusinessLogicLayer/Services/Abstractions/IEmployeeFacade.cs`: presentation contract for employee CRUD/search.
- `BusinessLogicLayer/Contracts/Employees/*`: employee DTO/request contracts.
- `BusinessLogicLayer/Schedule/*`: extracted pure schedule constants/parser/totals logic.
- `BusinessLogicLayer/Contracts/Export/ScheduleSqlExportData.cs`: BLL export data contract.
- `BusinessLogicLayer/Services/Abstractions/IScheduleExportDataBuilder.cs` and `Services/Export/ScheduleExportDataBuilder.cs`: BLL export-data builder.
- `BusinessLogicLayer/Extensions.cs`: DI registrations for facades + export data builder.

### Presentation updated
- `WPFApp/ViewModel/Shop/*`: now depends on `ShopDto` + `SaveShopRequest` + `IShopFacade`.
- `WPFApp/ViewModel/Employee/*`: now depends on `EmployeeDto` + `SaveEmployeeRequest` + `IEmployeeFacade`.
- `WPFApp/MVVM/Validation/Rules/ShopValidationRules.cs`: validates `SaveShopRequest`.
- `WPFApp/MVVM/Validation/Rules/EmployeeValidationRules.cs`: validates `SaveEmployeeRequest`.
- `WPFApp/Applications/Matrix/Availability/AvailabilityCellCodeParser.cs`: delegates canonical parsing to BLL.
- `WPFApp/Applications/Matrix/Schedule/ScheduleMatrixConstants.cs`: delegates constants to BLL.
- `WPFApp/Applications/Matrix/Schedule/ScheduleTotalsCalculator.cs`: delegates totals/formatting to BLL.
- `WPFApp/Applications/Matrix/Schedule/ScheduleMatrixEngine.cs`: delegates `TryParseTime` to BLL.

## 8. DI and Startup Composition
- BLL DI extension now registers:
  - `IShopFacade -> ShopFacade`
  - `IEmployeeFacade -> EmployeeFacade`
  - `IScheduleExportDataBuilder -> ScheduleExportDataBuilder`
- Existing DAL/BLL registrations remain intact.
- WPF startup still provides DAL admin service with primitive connection info from `DatabasePathProvider`.

## 9. Cross-Cutting Concerns
- Diagnostics/notifications remain in Presentation (`DiagnosticsService`, `IDatabaseChangeNotifier`).
- Validation split:
  - BLL owns core business/service validation.
  - WPF keeps UX-triggered per-field/request validation adapters.
- Export split started:
  - BLL prepares structured SQL export data.
  - WPF handles template files and actual file writing.

## 10. Remaining Architectural Risks and Next Recommended Steps

### DONE in this refactor
- DTO/facade boundaries for Shop and Employee in WPF.
- Availability parser canonicalization routed to BLL.
- Initial schedule pure-logic extraction and WPF adapters.
- Initial export data-builder extraction in BLL.

### NEXT (within same 3-layer style)
1. Replace remaining DAL entity usage in schedule/container/home WPF viewmodels with BLL contracts.
2. Move additional `ScheduleMatrixEngine` pure methods from WPF to BLL and leave only UI shaping in WPF.
3. Move export context types (`ScheduleExportContext`, `ScheduleSqlExportContext`) toward BLL-owned contracts to remove DAL leakage in Presentation export orchestration.
4. Add dedicated mapper classes in BLL per feature to reduce mapping duplication.
5. Add focused architecture tests (or Roslyn analyzers) checking that refactored WPF folders do not reference `DataAccessLayer.Models`.

---

## Architectural Rule (explicit)
For refactored features (Shop, Employee):
- **Presentation MUST NOT directly depend on DAL entity models.**
- Presentation consumes **BLL DTO/request contracts** and invokes **BLL facade interfaces**.
