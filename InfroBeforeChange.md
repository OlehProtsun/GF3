# InfroBeforeChange

## 0. Executive Summary
- Проєкт — десктопний WPF застосунок (net10.0-windows) для ведення довідників співробітників/магазинів/контейнерів, груп доступності (availability) і побудови/редагування місячних графіків змін.
- Архітектура фактично 3-шарова: `WPFApp -> BusinessLogicLayer -> DataAccessLayer` з DI через `Microsoft.Extensions.Hosting`.
- Стартова точка UI: `WPFApp/App.xaml` + `App.xaml.cs`; під час старту створюється LocalAppData каталог `GF3`, формується SQLite connection string і реєструються всі сервіси.
- DAL реалізований на EF Core SQLite (`AppDbContext`, репозиторії, міграції), плюс окремий ADO.NET admin-сервіс для raw SQL.
- BLL містить контракти моделей, мапінг DAL↔BLL, CRUD-сервіси, фасади для Shop/Employee, агрегований сервіс графіків, валідатори availability і генератор графіків (`ScheduleGenerator`).
- UI реалізований у стилі MVVM: ViewModels для кожного доменного модуля (Home/Employee/Shop/Availability/Container/Database/Information), окремі validation rules, commands, converters.
- Ключові потоки: користувач взаємодіє з View -> відповідний ViewModel -> BLL service/facade -> DAL repository -> SQLite DB; результати повертаються у ViewModel для оновлення bindable state.
- Експорт: є потужний `ScheduleExportService` (Excel/SQL), використовує шаблони `Resources/Excel/*.xlsx` та BLL builder для SQL payload.
- Інфраструктурно важливе: відсутні appsettings/config; connection string генерується в коді; секретів/ключів не знайдено.
- Для міграції в backend+React: майже весь BLL/DAL підходить як основа HTTP API; WPFApp містить UI-специфіку, яку треба перенести в React сторінки/компоненти.

## 1. Repository Map
- Solution: `GF3.sln`
- Projects: `DataAccessLayer`, `BusinessLogicLayer`, `WPFApp`, `ArchitectureTests`.

### Directory Tree (скорочено, DAL/BLL/WPFApp повністю)
```text
DataAccessLayer
  DataAccessLayer.csproj
  Models/
    AvailabilityGroupDayModel.cs
    AvailabilityGroupMemberModel.cs
    AvailabilityGroupModel.cs
    BindModel.cs
    ContainerModel.cs
    EmployeeModel.cs
    ScheduleCellStyleModel.cs
    ScheduleEmployeeModel.cs
    ScheduleModel.cs
    ScheduleSlotModel.cs
    ShopModel.cs
    DataBaseContext/
      AppDbContext.cs
      AppDbContextFactory.cs
      Extensions.cs
    Enums/
      AvailabilityKind.cs
      SlotStatus.cs
  Repositories/
    AvailabilityGroupDayRepository.cs
    AvailabilityGroupMemberRepository.cs
    AvailabilityGroupRepository.cs
    BindRepository.cs
    ContainerRepository.cs
    EmployeeRepository.cs
    GenericRepository.cs
    ScheduleCellStyleRepository.cs
    ScheduleEmployeeRepository.cs
    ScheduleRepository.cs
    ScheduleSlotRepository.cs
    ShopRepository.cs
    Abstractions/
      IAvailabilityGroupDayRepository.cs
      IAvailabilityGroupMemberRepository.cs
      IAvailabilityGroupRepository.cs
      IBaseRepository.cs
      IBindRepository.cs
      IContainerRepository.cs
      IEmployeeRepository.cs
      IScheduleCellStyleRepository.cs
      IScheduleEmployeeRepository.cs
      IScheduleRepository.cs
      IScheduleSlotRepository.cs
      IShopRepository.cs
  Administration/
    SqliteAdminService.cs
  Migrations/
    20251114184904_AddNameToAvailabilityMonth.Designer.cs
    20251114184904_AddNameToAvailabilityMonth.cs
    20251114185421_AddNameToAvailabilityMonth2.Designer.cs
    20251114185421_AddNameToAvailabilityMonth2.cs
    20251206194754_SlotTimes.Designer.cs
    20251206194754_SlotTimes.cs
    20251216172827_AddBind.Designer.cs
    20251216172827_AddBind.cs
    20251219185609_DropScheduleStatusAndComment.Designer.cs
    20251219185609_DropScheduleStatusAndComment.cs
    20251220111829_AddAvailabilityGroups.Designer.cs
    20251220111829_AddAvailabilityGroups.cs
    20251229184256_RemoveShopAndAvailabilityLegacy.Designer.cs
    20251229184256_RemoveShopAndAvailabilityLegacy.cs
    20260103192437_addNoteToSchedule.Designer.cs
    20260103192437_addNoteToSchedule.cs
    20260106132614_AddShopLogic.Designer.cs
    20260106132614_AddShopLogic.cs
    20260106140227_ShopLogicFix.Designer.cs
    20260106140227_ShopLogicFix.cs
    20260107120000_AddScheduleCellStyles.cs
    20260113190024_AddScheduleAvailabilityGroupId.Designer.cs
    20260113190024_AddScheduleAvailabilityGroupId.cs
    20260113191835_Init2.Designer.cs
    20260113191835_Init2.cs
    20260115120000_AddUniqueIndexes.Designer.cs
    20260115120000_AddUniqueIndexes.cs
    20260115125626_databaseFix.Designer.cs
    20260115125626_databaseFix.cs
    20260116183224_cellPaintAdded.Designer.cs
    20260116183224_cellPaintAdded.cs
    20260220191954_FixMissingScheduleCellStyleTable.Designer.cs
    20260220191954_FixMissingScheduleCellStyleTable.cs
    AppDbContextModelSnapshot.cs
BusinessLogicLayer
  BusinessLogicLayer.csproj
  Extensions.cs
  Schedule/
    ScheduleMatrixConstants.cs
    ScheduleMatrixEngine.cs
    ScheduleTotalsCalculator.cs
  Contracts/
    Models/
      Models.cs
    Shops/
      SaveShopRequest.cs
      ShopDto.cs
    Export/
      ScheduleSqlExportData.cs
    Database/
      SqliteAdminDtos.cs
    Employees/
      EmployeeDto.cs
      SaveEmployeeRequest.cs
    Enums/
      AvailabilityKind.cs
      SlotStatus.cs
  Common/
    ValidationException.cs
  Availability/
    AvailabilityCodeParser.cs
    AvailabilityGroupValidator.cs
    AvailabilityPayloadBuilder.cs
  Mappers/
    ModelMapper.cs
  Generators/
    IScheduleGenerator.cs
    ScheduleGenerator.cs
  Services/
    AvailabilityGroupService.cs
    BindService.cs
    ContainerService.cs
    EmployeeFacade.cs
    EmployeeService.cs
    GenericService.cs
    ScheduleEmployeeService.cs
    ScheduleService.cs
    ScheduleSlotService.cs
    ServiceMappingHelper.cs
    ShopFacade.cs
    ShopService.cs
    SqliteAdminFacade.cs
    Export/
      ScheduleExportDataBuilder.cs
    Abstractions/
      IAvailabilityGroupService.cs
      IBaseService.cs
      IBindService.cs
      IContainerService.cs
      IEmployeeFacade.cs
      IEmployeeService.cs
      IScheduleEmployeeService.cs
      IScheduleExportDataBuilder.cs
      IScheduleService.cs
      IScheduleSlotService.cs
      IShopFacade.cs
      IShopService.cs
      ISqliteAdminFacade.cs
WPFApp
  App.xaml
  App.xaml.cs
  AssemblyInfo.cs
  WPFApp.csproj
  UI/
    Hotkeys/
      KeyGestureTextHelper.cs
    Helpers/
      ColorHelpers.cs
    Dialogs/
      ColorPickerService.cs
      CustomMessageBox.cs
      IColorPickerService.cs
    Matrix/
      Schedule/
        ScheduleMatrixColumnBuilder.cs
  View/
    DatabaseView.xaml
    DatabaseView.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Home/
      HomeView.xaml
      HomeView.xaml.cs
    Helpers/
      AvailabilityMatrixGridBuilder.cs
      AvailabilityViewInputHelper.cs
    Dialogs/
      ColorPickerDialog.xaml
      ColorPickerDialog.xaml.cs
      CustomMessageBoxView.xaml
      CustomMessageBoxView.xaml.cs
      UIStatus.xaml
      UIStatus.xaml.cs
    Information/
      InformationView.xaml
      InformationView.xaml.cs
    Availability/
      AvailabilityEditView.xaml
      AvailabilityEditView.xaml.cs
      AvailabilityListView.xaml
      AvailabilityListView.xaml.cs
      AvailabilityProfileView.xaml
      AvailabilityProfileView.xaml.cs
      AvailabilityView.xaml
      AvailabilityView.xaml.cs
    Shop/
      ShopEditView.xaml
      ShopEditView.xaml.cs
      ShopListView.xaml
      ShopListView.xaml.cs
      ShopProfileView.xaml
      ShopProfileView.xaml.cs
      ShopView.xaml
      ShopView.xaml.cs
    Employee/
      EmployeeEditView.xaml
      EmployeeEditView.xaml.cs
      EmployeeListView.xaml
      EmployeeListView.xaml.cs
      EmployeeProfileView.xaml
      EmployeeProfileView.xaml.cs
      EmployeeView.xaml
      EmployeeView.xaml.cs
    Container/
      ContainerEditView.xaml
      ContainerEditView.xaml.cs
      ContainerListView.xaml
      ContainerListView.xaml.cs
      ContainerProfileView.xaml
      ContainerProfileView.xaml.cs
      ContainerScheduleEditView.xaml
      ContainerScheduleEditView.xaml.cs
      ContainerScheduleProfileView.xaml
      ContainerScheduleProfileView.xaml.cs
      ContainerView.xaml
      ContainerView.xaml.cs
  ViewModel/
    Main/
      MainViewModel.cs
      NavPage.cs
    Database/
      DatabaseViewModel.cs
    Home/
      HomeViewModel.cs
    Dialogs/
      CustomMessageBoxViewModel.cs
    Information/
      InformationViewModel.cs
    Availability/
      Main/
        AvailabilityViewModel.Binds.cs
        AvailabilityViewModel.DatabaseReload.cs
        AvailabilityViewModel.Employees.cs
        AvailabilityViewModel.Groups.cs
        AvailabilityViewModel.Initialization.cs
        AvailabilityViewModel.NavStatus.cs
        AvailabilityViewModel.Navigation.cs
        AvailabilityViewModel.Ui.cs
        AvailabilityViewModel.cs
      Edit/
        AvailabilityEditViewModel.Batching.cs
        AvailabilityEditViewModel.Binds.cs
        AvailabilityEditViewModel.Commands.cs
        AvailabilityEditViewModel.Fields.cs
        AvailabilityEditViewModel.Matrix.cs
        AvailabilityEditViewModel.Validation.cs
        AvailabilityEditViewModel.cs
      Profile/
        AvailabilityProfileViewModel.cs
      Helpers/
        BindRow.cs
        EmployeeListItem.cs
      List/
        AvailabilityListViewModel.cs
    Shared/
      UiOperationRunner.cs
      ValidationDictionaryHelper.cs
    Shop/
      ShopEditViewModel.cs
      ShopListViewModel.cs
      ShopProfileViewModel.cs
      ShopViewModel.cs
      Helpers/
        ShopDisplayHelper.cs
    Employee/
      EmployeeEditViewModel.cs
      EmployeeListViewModel.cs
      EmployeeProfileViewModel.cs
      EmployeeViewModel.cs
      Helpers/
        EmployeeDisplayHelper.cs
    Container/
      Edit/
        ContainerEditViewModel.cs
        ContainerViewModel.AvailabilityPreview.cs
        ContainerViewModel.Containers.cs
        ContainerViewModel.DatabaseReload.cs
        ContainerViewModel.Lookups.cs
        ContainerViewModel.Navigation.cs
        ContainerViewModel.ScheduleEditor.cs
        ContainerViewModel.Schedules.BlockFactory.cs
        ContainerViewModel.Schedules.Open.cs
        ContainerViewModel.Schedules.SaveGenerate.cs
        ContainerViewModel.cs
        Helpers/
          ContainerSection.cs
          ContainerViewModel.Export.cs
          ContainerViewModel.Ui.cs
      Profile/
        ContainerProfileViewModel.cs
      ScheduleProfile/
        ContainerScheduleProfileViewModel.cs
      ScheduleEdit/
        ContainerScheduleEditViewModel.CellStyling.cs
        ContainerScheduleEditViewModel.Lookups.cs
        ContainerScheduleEditViewModel.MatrixBinding.cs
        ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs
        ContainerScheduleEditViewModel.ModelBinding.cs
        ContainerScheduleEditViewModel.Selection.cs
        ContainerScheduleEditViewModel.Totals.cs
        ContainerScheduleEditViewModel.Validation.cs
        ContainerScheduleEditViewModel.cs
        Helpers/
          IScheduleMatrixStyleProvider.cs
          ScheduleBlockViewModel.cs
          ScheduleCellStyleStore.cs
          ScheduleMatrixCellRef.cs
      ScheduleList/
        ContainerScheduleListViewModel.cs
      List/
        ContainerListViewModel.cs
  MVVM/
    Validation/
      ValidationErrors.cs
      WpfRules/
        MinHoursAtLeastOneRule.cs
      Rules/
        AvailabilityValidationRules.cs
        ContainerValidationRules.cs
        EmployeeValidationRules.cs
        ScheduleValidationRules.cs
        ShopValidationRules.cs
    Core/
      ObservableObject.cs
      ViewModelBase.cs
    Threading/
      UiDebouncedAction.cs
    Commands/
      AsyncRelayCommand.cs
      RelayCommand.cs
  DesignTime/
    AvailabilityEditViewDesignVM.cs
    AvailabilityListDesignViewModel.cs
    AvailabilityProfileViewDesignVM.cs
    ContainerProfileViewDesignVm.cs
    ContainerScheduleEditDesignVM.cs
    HomeViewDesignTime.cs
  Applications/
    Preview/
      AvailabilityPreviewBuilder.cs
    Export/
      IScheduleExportService.cs
      ScheduleExportService.cs
    Diagnostics/
      ExceptionMessageBuilder.cs
    Matrix/
      Schedule/
        ScheduleMatrixConstants.cs
        ScheduleMatrixEngine.cs
        ScheduleTotalsCalculator.cs
      Availability/
        AvailabilityCellCodeParser.cs
        AvailabilityMatrixEngine.cs
    Configuration/
      DatabasePathProvider.cs
    Notifications/
      DatabaseChangeNotifier.cs
  Converters/
    DataRowViewBoolColumnConverter.cs
    EmployeeTotalHoursHeaderConverter.cs
    ScheduleMatrixCellBrushConverter.cs
    ScheduleMatrixCellReferenceConverter.cs
    ScheduleMatrixEmployeeTotalHoursConverter.cs
    Validation/
      FirstValidationErrorConverter.cs
  Resources/
    ViewTemplates.xaml
    Excel/
      ContainerTemplate.xlsx
      ScheduleTemplate.xlsx
    Styles/
      Buttons.xaml
      DataGrids.xaml
      Icons.xaml
      ScrollBars.xaml
      TextBoxes.xaml
      Theme.xaml
    Availability/
      AvailabilityResources.xaml
    Container/
      ContainerScheduleResources.xaml
    Controls/
      NumericUpDown.xaml
      NumericUpDown.xaml.cs
```

## 2. Build & Run (As-Is)
- Запуск зараз: стартується `WPFApp` (WinExe), показує `MainWindow`.
- Runtime-залежності: .NET SDK/Runtime 10.0, Windows (через WPF), SQLite файл у `%LocalAppData%/GF3/SQLite.db`.
- Типові команди:
  - `dotnet restore GF3.sln`
  - `dotnet build GF3.sln -c Debug`
  - `dotnet run --project WPFApp/WPFApp.csproj`
- Міграції БД: присутні EF Core migration-файли у `DataAccessLayer/Migrations`; design-time factory задає той самий шлях БД у LocalAppData.

## 3. Projects & Dependencies

| Project | TargetFramework | References | Purpose |
|---|---|---|---|
| DataAccessLayer | net10.0 | EF Core + SQLite packages | Entity models, DbContext, repositories, migrations, sqlite admin service. |
| BusinessLogicLayer | net10.0 | ProjectReference -> DataAccessLayer | BLL models/contracts, mappers, services/facades, schedule generation/export prep. |
| WPFApp | net10.0-windows | ProjectReference -> BusinessLogicLayer, ClosedXML, Hosting/DI | WPF UI, MVVM, navigation, export UI, database tools. |
| ArchitectureTests | net10.0 | xUnit | Architecture guard: WPF не має напряму посилатись на DAL namespace/project. |

Dependency graph (text):
```text
WPFApp --> BusinessLogicLayer --> DataAccessLayer --> SQLite
ArchitectureTests --(reads project/source files)--> WPFApp
```

## 4. Layer-by-Layer Documentation

### 4.1 DataAccessLayer
| File | Role | Key types/methods | Interactions | Data | Notes |
|---|---|---|---|---|---|
| `DataAccessLayer/DataAccessLayer.csproj` | Supporting source/resource file | -; - | - | - | - |
| `DataAccessLayer/Models/AvailabilityGroupDayModel.cs` | Entity/DTO/domain model definition | AvailabilityGroupDayModel; - | Microsoft.EntityFrameworkCore, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/AvailabilityGroupMemberModel.cs` | Entity/DTO/domain model definition | AvailabilityGroupMemberModel; - | System, System.Collections.Generic, System.ComponentModel.DataAnnotations | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/AvailabilityGroupModel.cs` | Entity/DTO/domain model definition | AvailabilityGroupModel; - | System, System.Collections.Generic, System.ComponentModel.DataAnnotations | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/BindModel.cs` | Entity/DTO/domain model definition | BindModel; - | System.Collections.Generic, System.Text | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ContainerModel.cs` | Entity/DTO/domain model definition | ContainerModel; - | Microsoft.EntityFrameworkCore, System.Collections.Generic, System.ComponentModel.DataAnnot | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/EmployeeModel.cs` | Entity/DTO/domain model definition | EmployeeModel; - | System.Collections.Generic, System.ComponentModel.DataAnnotations, System.ComponentModel.D | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ScheduleCellStyleModel.cs` | Entity/DTO/domain model definition | ScheduleCellStyleModel; - | Microsoft.EntityFrameworkCore, System.ComponentModel.DataAnnotations, System.ComponentMode | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ScheduleEmployeeModel.cs` | Entity/DTO/domain model definition | ScheduleEmployeeModel; - | System, System.Collections.Generic, System.ComponentModel.DataAnnotations | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ScheduleModel.cs` | Entity/DTO/domain model definition | ScheduleModel; - | DataAccessLayer.Models.Enums, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ScheduleSlotModel.cs` | Entity/DTO/domain model definition | ScheduleSlotModel; - | DataAccessLayer.Models.Enums, System.ComponentModel.DataAnnotations, System.ComponentModel | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/ShopModel.cs` | Entity/DTO/domain model definition | ShopModel; - | System.Collections.Generic, System.ComponentModel.DataAnnotations, System.ComponentModel.D | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs` | Entity/DTO/domain model definition | AppDbContext; AppDbContext | Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Storage.ValueConversion, Syst | Models/DTOs/ViewState | Core file |
| `DataAccessLayer/Models/DataBaseContext/AppDbContextFactory.cs` | Entity/DTO/domain model definition | AppDbContextFactory; CreateDbContext | Microsoft.EntityFrameworkCore.Design, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/DataBaseContext/Extensions.cs` | Entity/DTO/domain model definition | Extensions; AddDataAccess | DataAccessLayer.Repositories, DataAccessLayer.Repositories.Abstractions, Microsoft.EntityF | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/Enums/AvailabilityKind.cs` | Entity/DTO/domain model definition | AvailabilityKind; - | System.Collections.Generic, System.Linq, System.Text | Models/DTOs/ViewState | - |
| `DataAccessLayer/Models/Enums/SlotStatus.cs` | Entity/DTO/domain model definition | SlotStatus; - | System.Collections.Generic, System.Linq, System.Text | Models/DTOs/ViewState | - |
| `DataAccessLayer/Repositories/AvailabilityGroupDayRepository.cs` | Data-access repository implementation | AvailabilityGroupDayRepository; GetByMemberIdAsync, DeleteByMemberIdAsync, GetByGroupIdAsync, AddRangeAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/AvailabilityGroupMemberRepository.cs` | Data-access repository implementation | AvailabilityGroupMemberRepository; GetAllAsync, GetByGroupIdAsync, GetByGroupAndEmployeeAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/AvailabilityGroupRepository.cs` | Data-access repository implementation | AvailabilityGroupRepository; GetAllAsync, GetByValueAsync, GetFullByIdAsync, ExistsByNameAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/BindRepository.cs` | Data-access repository implementation | BindRepository; GetAllAsync, GetByKeyAsync, GetActiveAsync, UpsertByKeyAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/ContainerRepository.cs` | Data-access repository implementation | ContainerRepository; GetAllAsync, GetByValueAsync | DataAccessLayer.Models, DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositori | - | - |
| `DataAccessLayer/Repositories/EmployeeRepository.cs` | Data-access repository implementation | EmployeeRepository; GetByValueAsync, ExistsByNameAsync, HasAvailabilityReferencesAsync, HasScheduleReferencesAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/GenericRepository.cs` | Data-access repository implementation | GenericRepository; GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync | DataAccessLayer.Repositories.Abstractions, Microsoft.EntityFrameworkCore, System | - | - |
| `DataAccessLayer/Repositories/ScheduleCellStyleRepository.cs` | Data-access repository implementation | ScheduleCellStyleRepository; GetByScheduleAsync | DataAccessLayer.Models, DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositori | - | - |
| `DataAccessLayer/Repositories/ScheduleEmployeeRepository.cs` | Data-access repository implementation | ScheduleEmployeeRepository; GetByScheduleAsync | DataAccessLayer.Models, DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositori | - | - |
| `DataAccessLayer/Repositories/ScheduleRepository.cs` | Data-access repository implementation | ScheduleRepository; GetAllAsync, GetByContainerAsync, GetByValueAsync, GetDetailedAsync | DataAccessLayer.Models, DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositori | - | - |
| `DataAccessLayer/Repositories/ScheduleSlotRepository.cs` | Data-access repository implementation | ScheduleSlotRepository; GetByScheduleAsync | DataAccessLayer.Models, DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositori | - | - |
| `DataAccessLayer/Repositories/ShopRepository.cs` | Data-access repository implementation | ShopRepository; GetByValueAsync, ExistsByNameAsync, HasScheduleReferencesAsync | DataAccessLayer.Models.DataBaseContext, DataAccessLayer.Repositories.Abstractions, Microso | - | - |
| `DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupDayRepository.cs` | Service/repository contract interface | IAvailabilityGroupDayRepository; - | System, System.Collections.Generic, System.Text | - | - |
| `DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupMemberRepository.cs` | Service/repository contract interface | IAvailabilityGroupMemberRepository; - | System, System.Collections.Generic, System.Text | - | - |
| `DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupRepository.cs` | Service/repository contract interface | IAvailabilityGroupRepository; - | System, System.Collections.Generic, System.Text | - | - |
| `DataAccessLayer/Repositories/Abstractions/IBaseRepository.cs` | Service/repository contract interface | IBaseRepository; - | System.Collections.Generic, System.Linq, System.Text | - | - |
| `DataAccessLayer/Repositories/Abstractions/IBindRepository.cs` | Service/repository contract interface | IBindRepository; - | System, System.Collections.Generic, System.Text | - | - |
| `DataAccessLayer/Repositories/Abstractions/IContainerRepository.cs` | Service/repository contract interface | IContainerRepository; - | DataAccessLayer.Models, System.Collections.Generic, System.Threading | - | - |
| `DataAccessLayer/Repositories/Abstractions/IEmployeeRepository.cs` | Service/repository contract interface | IEmployeeRepository; - | System, System.Collections.Generic, System.Linq | - | - |
| `DataAccessLayer/Repositories/Abstractions/IScheduleCellStyleRepository.cs` | Service/repository contract interface | IScheduleCellStyleRepository; - | DataAccessLayer.Models, System.Collections.Generic, System.Threading | - | - |
| `DataAccessLayer/Repositories/Abstractions/IScheduleEmployeeRepository.cs` | Service/repository contract interface | IScheduleEmployeeRepository; - | DataAccessLayer.Models, System.Collections.Generic, System.Threading | - | - |
| `DataAccessLayer/Repositories/Abstractions/IScheduleRepository.cs` | Service/repository contract interface | IScheduleRepository; - | DataAccessLayer.Models, System.Collections.Generic, System.Threading | - | - |
| `DataAccessLayer/Repositories/Abstractions/IScheduleSlotRepository.cs` | Service/repository contract interface | IScheduleSlotRepository; - | DataAccessLayer.Models, System.Collections.Generic, System.Threading | - | - |
| `DataAccessLayer/Repositories/Abstractions/IShopRepository.cs` | Service/repository contract interface | IShopRepository; - | System, System.Collections.Generic, System.Linq | - | - |
| `DataAccessLayer/Administration/SqliteAdminService.cs` | Supporting source/resource file | SqlExecutionResult, DatabaseInfo, ISqliteAdminService; ExecuteSqlAsync, ImportSqlScriptAsync, GetDatabaseInfoAsync, Comp | Microsoft.Data.Sqlite, System, System.Collections.Generic | - | - |
| `DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.Designer.cs` | EF Core migration snapshot/step | AddNameToAvailabilityMonth; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.cs` | EF Core migration snapshot/step | AddNameToAvailabilityMonth; - | - | - | - |
| `DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.Designer.cs` | EF Core migration snapshot/step | AddNameToAvailabilityMonth2; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.cs` | EF Core migration snapshot/step | AddNameToAvailabilityMonth2; - | - | - | - |
| `DataAccessLayer/Migrations/20251206194754_SlotTimes.Designer.cs` | EF Core migration snapshot/step | SlotTimes; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251206194754_SlotTimes.cs` | EF Core migration snapshot/step | SlotTimes; - | - | - | - |
| `DataAccessLayer/Migrations/20251216172827_AddBind.Designer.cs` | EF Core migration snapshot/step | AddBind; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251216172827_AddBind.cs` | EF Core migration snapshot/step | AddBind; - | - | - | - |
| `DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.Designer.cs` | EF Core migration snapshot/step | DropScheduleStatusAndComment; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.cs` | EF Core migration snapshot/step | DropScheduleStatusAndComment; - | - | - | - |
| `DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.Designer.cs` | EF Core migration snapshot/step | AddAvailabilityGroups; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.cs` | EF Core migration snapshot/step | AddAvailabilityGroups; - | - | - | - |
| `DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.Designer.cs` | EF Core migration snapshot/step | RemoveShopAndAvailabilityLegacy; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.cs` | EF Core migration snapshot/step | RemoveShopAndAvailabilityLegacy; - | - | - | - |
| `DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.Designer.cs` | EF Core migration snapshot/step | addNoteToSchedule; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.cs` | EF Core migration snapshot/step | addNoteToSchedule; - | - | - | - |
| `DataAccessLayer/Migrations/20260106132614_AddShopLogic.Designer.cs` | EF Core migration snapshot/step | AddShopLogic; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260106132614_AddShopLogic.cs` | EF Core migration snapshot/step | AddShopLogic; - | - | - | - |
| `DataAccessLayer/Migrations/20260106140227_ShopLogicFix.Designer.cs` | EF Core migration snapshot/step | ShopLogicFix; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260106140227_ShopLogicFix.cs` | EF Core migration snapshot/step | ShopLogicFix; - | - | - | - |
| `DataAccessLayer/Migrations/20260107120000_AddScheduleCellStyles.cs` | EF Core migration snapshot/step | AddScheduleCellStyles; - | - | - | - |
| `DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.Designer.cs` | EF Core migration snapshot/step | AddScheduleAvailabilityGroupId; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.cs` | EF Core migration snapshot/step | AddScheduleAvailabilityGroupId; - | - | - | - |
| `DataAccessLayer/Migrations/20260113191835_Init2.Designer.cs` | EF Core migration snapshot/step | Init2; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260113191835_Init2.cs` | EF Core migration snapshot/step | Init2; - | - | - | - |
| `DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.Designer.cs` | EF Core migration snapshot/step | AddUniqueIndexes; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.cs` | EF Core migration snapshot/step | AddUniqueIndexes; - | Microsoft.EntityFrameworkCore.Migrations | - | - |
| `DataAccessLayer/Migrations/20260115125626_databaseFix.Designer.cs` | EF Core migration snapshot/step | databaseFix; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260115125626_databaseFix.cs` | EF Core migration snapshot/step | databaseFix; - | - | - | - |
| `DataAccessLayer/Migrations/20260116183224_cellPaintAdded.Designer.cs` | EF Core migration snapshot/step | cellPaintAdded; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260116183224_cellPaintAdded.cs` | EF Core migration snapshot/step | cellPaintAdded; - | - | - | - |
| `DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.Designer.cs` | EF Core migration snapshot/step | FixMissingScheduleCellStyleTable; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | - | - |
| `DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.cs` | EF Core migration snapshot/step | FixMissingScheduleCellStyleTable; - | - | - | - |
| `DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs` | EF Core migration snapshot/step | AppDbContextModelSnapshot; - | DataAccessLayer.Models.DataBaseContext, Microsoft.EntityFrameworkCore, Microsoft.EntityFra | Models/DTOs/ViewState | - |

### 4.2 BusinessLogicLayer
| File | Role | Key types/methods | Interactions | Data | Notes |
|---|---|---|---|---|---|
| `BusinessLogicLayer/BusinessLogicLayer.csproj` | Supporting source/resource file | -; - | - | - | - |
| `BusinessLogicLayer/Extensions.cs` | Supporting source/resource file | Extensions; AddBusinessLogicLayer, AddBusinessLogicStack | BusinessLogicLayer.Services.Abstractions, DataAccessLayer.Administration, DataAccessLayer. | - | - |
| `BusinessLogicLayer/Schedule/ScheduleMatrixConstants.cs` | Supporting source/resource file | ScheduleMatrixConstants; - | - | - | - |
| `BusinessLogicLayer/Schedule/ScheduleMatrixEngine.cs` | Supporting source/resource file | ScheduleMatrixEngine; TryParseTime | System.Globalization | - | - |
| `BusinessLogicLayer/Schedule/ScheduleTotalsCalculator.cs` | Supporting source/resource file | ScheduleTotalsCalculator, TotalsResult; Calculate, FormatHoursMinutes | BusinessLogicLayer.Contracts.Models | - | - |
| `BusinessLogicLayer/Contracts/Models/Models.cs` | Entity/DTO/domain model definition | BindModel, ContainerModel, EmployeeModel; - | BusinessLogicLayer.Contracts.Enums | Models/DTOs/ViewState | - |
| `BusinessLogicLayer/Contracts/Shops/SaveShopRequest.cs` | Supporting source/resource file | SaveShopRequest; - | - | - | - |
| `BusinessLogicLayer/Contracts/Shops/ShopDto.cs` | Supporting source/resource file | ShopDto; - | - | - | - |
| `BusinessLogicLayer/Contracts/Export/ScheduleSqlExportData.cs` | Supporting source/resource file | ScheduleSqlExportData, ScheduleSqlDto, ScheduleEmployeeSqlDto; - | - | - | - |
| `BusinessLogicLayer/Contracts/Database/SqliteAdminDtos.cs` | Supporting source/resource file | SqlExecutionResultDto, DatabaseInfoDto; - | System.Data | - | - |
| `BusinessLogicLayer/Contracts/Employees/EmployeeDto.cs` | Supporting source/resource file | EmployeeDto; - | - | - | - |
| `BusinessLogicLayer/Contracts/Employees/SaveEmployeeRequest.cs` | Supporting source/resource file | SaveEmployeeRequest; - | - | - | - |
| `BusinessLogicLayer/Contracts/Enums/AvailabilityKind.cs` | Supporting source/resource file | AvailabilityKind; - | - | - | - |
| `BusinessLogicLayer/Contracts/Enums/SlotStatus.cs` | Supporting source/resource file | SlotStatus; - | - | - | - |
| `BusinessLogicLayer/Common/ValidationException.cs` | Supporting source/resource file | ValidationException; - | System.Collections.Generic, System.Linq, System.Text | - | - |
| `BusinessLogicLayer/Availability/AvailabilityCodeParser.cs` | Supporting source/resource file | AvailabilityCodeParser; TryParse, TryNormalizeInterval | System.Globalization | - | - |
| `BusinessLogicLayer/Availability/AvailabilityGroupValidator.cs` | Supporting source/resource file | AvailabilityGroupValidator; Validate | - | - | - |
| `BusinessLogicLayer/Availability/AvailabilityPayloadBuilder.cs` | Supporting source/resource file | AvailabilityPayloadBuilder; TryBuild | - | - | - |
| `BusinessLogicLayer/Mappers/ModelMapper.cs` | Supporting source/resource file | ModelMapper; - | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, Dal | Models/DTOs/ViewState | - |
| `BusinessLogicLayer/Generators/IScheduleGenerator.cs` | Supporting source/resource file | IScheduleGenerator; - | BusinessLogicLayer.Contracts.Models, System.Threading, System.Threading.Tasks | - | Core file |
| `BusinessLogicLayer/Generators/ScheduleGenerator.cs` | Supporting source/resource file | ScheduleGenerator, ShiftTemplate; GenerateAsync | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, System.Collection | - | Core file |
| `BusinessLogicLayer/Services/AvailabilityGroupService.cs` | BLL service/facade implementation | AvailabilityGroupService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, BusinessLogicLaye | - | - |
| `BusinessLogicLayer/Services/BindService.cs` | BLL service/facade implementation | BindService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/ContainerService.cs` | BLL service/facade implementation | ContainerService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/EmployeeFacade.cs` | BLL service/facade implementation | EmployeeFacade; GetAllAsync, GetByValueAsync, GetAsync, CreateAsync | BusinessLogicLayer.Contracts.Employees, BusinessLogicLayer.Contracts.Models, BusinessLogic | - | - |
| `BusinessLogicLayer/Services/EmployeeService.cs` | BLL service/facade implementation | EmployeeService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/GenericService.cs` | BLL service/facade implementation | GenericService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | DataAccessLayer.Repositories.Abstractions, System, System.Collections.Generic | - | - |
| `BusinessLogicLayer/Services/ScheduleEmployeeService.cs` | BLL service/facade implementation | ScheduleEmployeeService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/ScheduleService.cs` | BLL service/facade implementation | ScheduleService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Common, BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers | - | - |
| `BusinessLogicLayer/Services/ScheduleSlotService.cs` | BLL service/facade implementation | ScheduleSlotService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/ServiceMappingHelper.cs` | BLL service/facade implementation | ServiceMappingHelper, where; - | - | - | - |
| `BusinessLogicLayer/Services/ShopFacade.cs` | BLL service/facade implementation | ShopFacade; GetAllAsync, GetByValueAsync, GetAsync, CreateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Contracts.Shops, BusinessLogicLaye | - | - |
| `BusinessLogicLayer/Services/ShopService.cs` | BLL service/facade implementation | ShopService; GetAsync, GetAllAsync, CreateAsync, UpdateAsync | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Mappers, BusinessLogicLayer.Servic | - | - |
| `BusinessLogicLayer/Services/SqliteAdminFacade.cs` | BLL service/facade implementation | SqliteAdminFacade; ExecuteSqlAsync, ImportSqlScriptAsync, GetDatabaseInfoAsync, ComputeFileHashAsync | BusinessLogicLayer.Contracts.Database, BusinessLogicLayer.Services.Abstractions, DataAcces | - | - |
| `BusinessLogicLayer/Services/Export/ScheduleExportDataBuilder.cs` | BLL service/facade implementation | ScheduleExportDataBuilder; BuildSqlData | BusinessLogicLayer.Contracts.Export, BusinessLogicLayer.Contracts.Models, BusinessLogicLay | - | - |
| `BusinessLogicLayer/Services/Abstractions/IAvailabilityGroupService.cs` | Service/repository contract interface | IAvailabilityGroupService; - | System, System.Collections.Generic, System.Text | - | - |
| `BusinessLogicLayer/Services/Abstractions/IBaseService.cs` | Service/repository contract interface | IBaseService; - | System.Collections.Generic, System.Linq, System.Text | - | - |
| `BusinessLogicLayer/Services/Abstractions/IBindService.cs` | Service/repository contract interface | IBindService; - | System, System.Collections.Generic, System.Text | - | - |
| `BusinessLogicLayer/Services/Abstractions/IContainerService.cs` | Service/repository contract interface | IContainerService; - | BusinessLogicLayer.Contracts.Models, System.Collections.Generic, System.Threading | - | - |
| `BusinessLogicLayer/Services/Abstractions/IEmployeeFacade.cs` | Service/repository contract interface | IEmployeeFacade; - | BusinessLogicLayer.Contracts.Employees | - | - |
| `BusinessLogicLayer/Services/Abstractions/IEmployeeService.cs` | Service/repository contract interface | IEmployeeService; - | System, System.Collections.Generic, System.Linq | - | - |
| `BusinessLogicLayer/Services/Abstractions/IScheduleEmployeeService.cs` | Service/repository contract interface | IScheduleEmployeeService; - | System, System.Collections.Generic, System.Linq | - | - |
| `BusinessLogicLayer/Services/Abstractions/IScheduleExportDataBuilder.cs` | Service/repository contract interface | IScheduleExportDataBuilder; - | BusinessLogicLayer.Contracts.Export, BusinessLogicLayer.Contracts.Models | - | - |
| `BusinessLogicLayer/Services/Abstractions/IScheduleService.cs` | Service/repository contract interface | IScheduleService; - | BusinessLogicLayer.Contracts.Models, System.Collections.Generic, System.Threading | - | - |
| `BusinessLogicLayer/Services/Abstractions/IScheduleSlotService.cs` | Service/repository contract interface | IScheduleSlotService; - | System, System.Collections.Generic, System.Linq | - | - |
| `BusinessLogicLayer/Services/Abstractions/IShopFacade.cs` | Service/repository contract interface | IShopFacade; - | - | - | - |
| `BusinessLogicLayer/Services/Abstractions/IShopService.cs` | Service/repository contract interface | IShopService; - | System, System.Collections.Generic, System.Linq | - | - |
| `BusinessLogicLayer/Services/Abstractions/ISqliteAdminFacade.cs` | Service/repository contract interface | ISqliteAdminFacade; - | BusinessLogicLayer.Contracts.Database | - | - |

### 4.3 WPFApp
| File | Role | Key types/methods | Interactions | Data | Notes |
|---|---|---|---|---|---|
| `WPFApp/App.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/App.xaml.cs` | WPF code-behind for UI behavior/events | App, App; - | BusinessLogicLayer, BusinessLogicLayer.Services.Abstractions, Microsoft.Extensions.Depende | - | Core file |
| `WPFApp/AssemblyInfo.cs` | Supporting source/resource file | -; - | System.Windows | - | - |
| `WPFApp/WPFApp.csproj` | Supporting source/resource file | -; - | - | - | - |
| `WPFApp/UI/Hotkeys/KeyGestureTextHelper.cs` | Supporting source/resource file | KeyGestureTextHelper, KeyGestureTextHelper; FormatKeyGesture, FormatKeyGesture, TryNormalizeKey, TryNormalizeKey | System, System.Globalization, System.Linq | - | - |
| `WPFApp/UI/Helpers/ColorHelpers.cs` | Supporting source/resource file | ColorHelpers, ColorHelpers; ToArgb, ToArgb, FromArgb, FromArgb | System, System.Collections.Concurrent, System.Globalization | - | - |
| `WPFApp/UI/Dialogs/ColorPickerService.cs` | Supporting source/resource file | ColorPickerService, ColorPickerService; TryPickColor, TryPickColor | System.Windows, System.Windows.Media, WPFApp.View.Dialogs | - | - |
| `WPFApp/UI/Dialogs/CustomMessageBox.cs` | Supporting source/resource file | CustomMessageBox, CustomMessageBox; Show, Show | System, System.Collections.Generic, System.Text | - | - |
| `WPFApp/UI/Dialogs/IColorPickerService.cs` | Supporting source/resource file | IColorPickerService, IColorPickerService; - | System.Windows.Media | - | - |
| `WPFApp/UI/Matrix/Schedule/ScheduleMatrixColumnBuilder.cs` | Supporting source/resource file | ScheduleMatrixColumnBuilder, ScheduleMatrixColumnBuilder; BuildScheduleMatrixColumns, BuildScheduleMatrixColumns | System.Data, System.Windows, System.Windows.Controls | - | - |
| `WPFApp/View/DatabaseView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/DatabaseView.xaml.cs` | WPF code-behind for UI behavior/events | DatabaseView, DatabaseView; - | System.Windows.Controls | - | - |
| `WPFApp/View/MainWindow.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/MainWindow.xaml.cs` | WPF code-behind for UI behavior/events | MainWindow, MainWindow; - | System.Text, System.Windows, System.Windows.Controls | - | - |
| `WPFApp/View/Home/HomeView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Home/HomeView.xaml.cs` | WPF code-behind for UI behavior/events | HomeView, HomeView; - | System, System.Data, System.Linq | - | - |
| `WPFApp/View/Helpers/AvailabilityMatrixGridBuilder.cs` | WPF View / window / user-control | AvailabilityMatrixGridBuilder, AvailabilityMatrixGridBuilder; BuildEditable, BuildEditable, BuildReadOnly, BuildReadOnly | System, System.Data, System.Windows | - | - |
| `WPFApp/View/Helpers/AvailabilityViewInputHelper.cs` | WPF View / window / user-control | AvailabilityViewInputHelper, AvailabilityViewInputHelper; KeyToBindToken, KeyToBindToken, IsAllDigits, IsAllDigits | System, System.Linq, System.Windows.Input | - | - |
| `WPFApp/View/Dialogs/ColorPickerDialog.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Dialogs/ColorPickerDialog.xaml.cs` | WPF code-behind for UI behavior/events | ColorPickerDialog, ColorPickerDialog, ColorSwatch; - | System.Collections.ObjectModel, System.Windows, System.Windows.Controls | - | - |
| `WPFApp/View/Dialogs/CustomMessageBoxView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Dialogs/CustomMessageBoxView.xaml.cs` | WPF code-behind for UI behavior/events | CustomMessageBoxView, CustomMessageBoxView; - | System, System.Windows, WPFApp.ViewModel.Dialogs | - | - |
| `WPFApp/View/Dialogs/UIStatus.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Dialogs/UIStatus.xaml.cs` | WPF code-behind for UI behavior/events | UIStatusKind, UIStatusKind, UIStatus; - | System.Windows, System.Windows.Controls, System.Windows.Media | - | - |
| `WPFApp/View/Information/InformationView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Information/InformationView.xaml.cs` | WPF code-behind for UI behavior/events | InformationView, InformationView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Availability/AvailabilityEditView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Availability/AvailabilityEditView.xaml.cs` | WPF code-behind for UI behavior/events | AvailabilityEditView, AvailabilityEditView; - | System, System.Data, System.Windows | - | - |
| `WPFApp/View/Availability/AvailabilityListView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Availability/AvailabilityListView.xaml.cs` | WPF code-behind for UI behavior/events | AvailabilityListView, AvailabilityListView; - | System.Windows, System.Windows.Controls, System.Windows.Controls.Primitives | - | - |
| `WPFApp/View/Availability/AvailabilityProfileView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Availability/AvailabilityProfileView.xaml.cs` | WPF code-behind for UI behavior/events | AvailabilityProfileView, AvailabilityProfileView; - | System, System.Windows, System.Windows.Controls | - | - |
| `WPFApp/View/Availability/AvailabilityView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Availability/AvailabilityView.xaml.cs` | WPF code-behind for UI behavior/events | AvailabilityView, AvailabilityView; - | System, System.Collections.Generic, System.Text | - | - |
| `WPFApp/View/Shop/ShopEditView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Shop/ShopEditView.xaml.cs` | WPF code-behind for UI behavior/events | ShopEditView, ShopEditView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Shop/ShopListView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Shop/ShopListView.xaml.cs` | WPF code-behind for UI behavior/events | ShopListView, ShopListView; - | System.Windows, System.Windows.Controls, System.Windows.Input | - | - |
| `WPFApp/View/Shop/ShopProfileView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Shop/ShopProfileView.xaml.cs` | WPF code-behind for UI behavior/events | ShopProfileView, ShopProfileView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Shop/ShopView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Shop/ShopView.xaml.cs` | WPF code-behind for UI behavior/events | ShopView, ShopView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Employee/EmployeeEditView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Employee/EmployeeEditView.xaml.cs` | WPF code-behind for UI behavior/events | EmployeeEditView, EmployeeEditView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Employee/EmployeeListView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Employee/EmployeeListView.xaml.cs` | WPF code-behind for UI behavior/events | EmployeeListView, EmployeeListView; - | System.Windows, System.Windows.Controls, System.Windows.Input | - | - |
| `WPFApp/View/Employee/EmployeeProfileView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Employee/EmployeeProfileView.xaml.cs` | WPF code-behind for UI behavior/events | EmployeeProfileView, EmployeeProfileView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Employee/EmployeeView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Employee/EmployeeView.xaml.cs` | WPF code-behind for UI behavior/events | EmployeeView, EmployeeView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Container/ContainerEditView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerEditView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerEditView, ContainerEditView; - | System.Windows.Controls | - | - |
| `WPFApp/View/Container/ContainerListView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerListView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerListView, ContainerListView; - | System.Windows, System.Windows.Controls, System.Windows.Input | - | - |
| `WPFApp/View/Container/ContainerProfileView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerProfileView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerProfileView, ContainerProfileView; - | System, System.Windows, System.Windows.Controls | - | - |
| `WPFApp/View/Container/ContainerScheduleEditView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerScheduleEditView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerScheduleEditView, ContainerScheduleEditView; - | System, System.Collections, System.Collections.Generic | - | - |
| `WPFApp/View/Container/ContainerScheduleProfileView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerScheduleProfileView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerScheduleProfileView, ContainerScheduleProfileView; - | System, System.Data, System.Linq | - | - |
| `WPFApp/View/Container/ContainerView.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/View/Container/ContainerView.xaml.cs` | WPF code-behind for UI behavior/events | ContainerView, ContainerView; - | System, System.Collections.Generic, System.Text | - | - |
| `WPFApp/ViewModel/Main/MainViewModel.cs` | MVVM ViewModel logic/state/commands | MainViewModel, MainViewModel; Dispose, Dispose | Microsoft.Extensions.DependencyInjection, System, System.Threading | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Main/NavPage.cs` | MVVM ViewModel logic/state/commands | NavPage, NavPage; - | System, System.Collections.Generic, System.Text | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Database/DatabaseViewModel.cs` | MVVM ViewModel logic/state/commands | DatabaseViewModel, DatabaseViewModel; - | BusinessLogicLayer.Services.Abstractions, Microsoft.Win32, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Home/HomeViewModel.cs` | MVVM ViewModel logic/state/commands | HomeViewModel, HomeViewModel, WhoWorksTodayRowViewModel; EnsureInitializedAsync, EnsureInitializedAsync, ApplyBuild, App | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Services.Abstractions, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Dialogs/CustomMessageBoxViewModel.cs` | MVVM ViewModel logic/state/commands | CustomMessageBoxIcon, CustomMessageBoxIcon, CustomMessageBoxViewModel; CanExecute, CanExecute, Execute, Execute | System, System.Windows, System.Windows.Input | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Information/InformationViewModel.cs` | MVVM ViewModel logic/state/commands | InformationViewModel, InformationViewModel; - | WPFApp.MVVM.Core, WPFApp.ViewModel | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Linq | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | System, System.Threading, System.Threading.Tasks | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Models, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; EnsureInitializedAsync, EnsureInitializedAsync | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | System.Threading, System.Threading.Tasks, System.Windows | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | System.Threading.Tasks | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs` | MVVM ViewModel logic/state/commands | AvailabilityViewModel, AvailabilityViewModel; - | System, System.Threading.Tasks, System.Windows | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs` | MVVM ViewModel logic/state/commands | AvailabilitySection, AvailabilitySection, AvailabilityViewModel; - | BusinessLogicLayer.Services.Abstractions, WPFApp.Applications.Notifications, WPFApp.MVVM.C | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel, MatrixUpdateScope; Dispose, Dispose, Dispose, Dispose | System, System.Threading | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; SetBinds, SetBinds, TryGetBindValue, TryGetBindValue | BusinessLogicLayer.Contracts.Models, System.Collections.Specialized, System.ComponentModel | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; - | System.Threading.Tasks, System.Windows, WPFApp.MVVM.Commands | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; SetEmployees, SetEmployees, ResetForNew, ResetForNew | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; GetErrors, GetErrors, SetValidationErrors, SetValidationErrors | System, System.Collections, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs` | MVVM ViewModel logic/state/commands | AvailabilityEditViewModel, AvailabilityEditViewModel; - | System, System.Collections.Specialized, System.Data | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Profile/AvailabilityProfileViewModel.cs` | MVVM ViewModel logic/state/commands | AvailabilityProfileViewModel, AvailabilityProfileViewModel; SetProfile, SetProfile | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Helpers/BindRow.cs` | MVVM ViewModel logic/state/commands | BindRow, BindRow; ToModel, ToModel, UpdateFromModel, UpdateFromModel | BusinessLogicLayer.Contracts.Models, System, WPFApp.MVVM.Core | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/Helpers/EmployeeListItem.cs` | MVVM ViewModel logic/state/commands | EmployeeListItem, EmployeeListItem; ToString, ToString | - | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Availability/List/AvailabilityListViewModel.cs` | MVVM ViewModel logic/state/commands | AvailabilityListViewModel, AvailabilityListViewModel; SetItems, SetItems | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shared/UiOperationRunner.cs` | MVVM ViewModel logic/state/commands | UiOperationRunner; - | System, System.Threading, System.Threading.Tasks | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shared/ValidationDictionaryHelper.cs` | MVVM ViewModel logic/state/commands | ValidationDictionaryHelper; - | System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shop/ShopEditViewModel.cs` | MVVM ViewModel logic/state/commands | ShopEditViewModel, ShopEditViewModel; GetErrors, GetErrors, ResetForNew, ResetForNew | BusinessLogicLayer.Contracts.Shops, System, System.Collections | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shop/ShopListViewModel.cs` | MVVM ViewModel logic/state/commands | ShopListViewModel, ShopListViewModel; SetItems, SetItems | BusinessLogicLayer.Contracts.Shops, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shop/ShopProfileViewModel.cs` | MVVM ViewModel logic/state/commands | ShopProfileViewModel, ShopProfileViewModel; SetProfile, SetProfile | BusinessLogicLayer.Contracts.Shops, WPFApp.MVVM.Commands, WPFApp.MVVM.Core | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shop/ShopViewModel.cs` | MVVM ViewModel logic/state/commands | ShopSection, ShopSection, ShopViewModel; EnsureInitializedAsync, EnsureInitializedAsync | BusinessLogicLayer.Contracts.Shops, BusinessLogicLayer.Services.Abstractions, System.Windo | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Shop/Helpers/ShopDisplayHelper.cs` | MVVM ViewModel logic/state/commands | ShopDisplayHelper, ShopDisplayHelper; TextOrDash, TextOrDash, NameOrEmpty, NameOrEmpty | BusinessLogicLayer.Contracts.Shops | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Employee/EmployeeEditViewModel.cs` | MVVM ViewModel logic/state/commands | EmployeeEditViewModel, EmployeeEditViewModel; GetErrors, GetErrors, ResetForNew, ResetForNew | BusinessLogicLayer.Contracts.Employees, System, System.Collections | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Employee/EmployeeListViewModel.cs` | MVVM ViewModel logic/state/commands | EmployeeListViewModel, EmployeeListViewModel; SetItems, SetItems | BusinessLogicLayer.Contracts.Employees, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Employee/EmployeeProfileViewModel.cs` | MVVM ViewModel logic/state/commands | EmployeeProfileViewModel, EmployeeProfileViewModel; SetProfile, SetProfile | BusinessLogicLayer.Contracts.Employees, WPFApp.MVVM.Commands, WPFApp.MVVM.Core | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Employee/EmployeeViewModel.cs` | MVVM ViewModel logic/state/commands | EmployeeSection, EmployeeSection, EmployeeViewModel; EnsureInitializedAsync, EnsureInitializedAsync | BusinessLogicLayer.Contracts.Employees, BusinessLogicLayer.Services.Abstractions, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Employee/Helpers/EmployeeDisplayHelper.cs` | MVVM ViewModel logic/state/commands | EmployeeDisplayHelper, EmployeeDisplayHelper; GetFullName, GetFullName, TextOrDash, TextOrDash | BusinessLogicLayer.Contracts.Employees, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerEditViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerEditViewModel, ContainerEditViewModel; GetErrors, GetErrors, ResetForNew, ResetForNew | BusinessLogicLayer.Contracts.Models, System, System.Collections | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Models, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; EnsureInitializedAsync, EnsureInitializedAsync | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | System, System.Linq, System.Threading | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System.Linq, System.Threading.Tasks | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Linq | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Co | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, BusinessLogicLayer.Generators, BusinessLogicLayer.Ser | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/Helpers/ContainerSection.cs` | MVVM ViewModel logic/state/commands | ContainerSection, ContainerSection; - | System, System.Collections.Generic, System.Text | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/Helpers/ContainerViewModel.Export.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | BusinessLogicLayer.Contracts.Models, System.Threading, System.Threading.Tasks | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Edit/Helpers/ContainerViewModel.Ui.cs` | MVVM ViewModel logic/state/commands | ContainerViewModel, ContainerViewModel; - | System, System.Threading.Tasks, System.Windows | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/Profile/ContainerProfileViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerProfileViewModel, ContainerProfileViewModel, ShopHeader; SetProfile, SetProfile, SetStatisticsAsync, SetStatist | BusinessLogicLayer.Contracts.Models, Microsoft.Win32, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleProfile/ContainerScheduleProfileViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleProfileViewModel, ContainerScheduleProfileViewModel, EmployeeWorkFreeStatRow; SetProfileAsync, SetProfi | BusinessLogicLayer.Contracts.Models, Microsoft.Win32, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; GetCellBackgroundBrush, GetCellBackgroundBrush, GetCellF | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; Dispose, Dispose, SetLookups, SetLookups | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; TryBuildCellReference, TryBuildCellReference | System, System.Data, System.Globalization | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; RefreshScheduleMatrix, RefreshScheduleMatrix, TryApplyMa | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; - | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Models, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; GetEmployeeTotalHoursText, GetEmployeeTotalHoursText | System.Collections.Generic, WPFApp.Applications.Matrix.Schedule | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel; ClearValidationErrors, ClearValidationErrors, SetValidat | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerScheduleEditViewModel, ContainerScheduleEditViewModel, SchedulePaintMode; GetErrors, GetErrors, SelectBlockAsyn | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Co | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/Helpers/IScheduleMatrixStyleProvider.cs` | MVVM ViewModel logic/state/commands | IScheduleMatrixStyleProvider, IScheduleMatrixStyleProvider; - | BusinessLogicLayer.Contracts.Models, System.Windows.Media | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleBlockViewModel.cs` | MVVM ViewModel logic/state/commands | ScheduleBlockViewModel, ScheduleBlockViewModel; - | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleCellStyleStore.cs` | MVVM ViewModel logic/state/commands | ScheduleCellStyleStore, ScheduleCellStyleStore; Load, Load, TryGetStyle, TryGetStyle | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleMatrixCellRef.cs` | MVVM ViewModel logic/state/commands | struct, struct; ScheduleMatrixCellRef, ScheduleMatrixCellRef | - | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/ScheduleList/ContainerScheduleListViewModel.cs` | MVVM ViewModel logic/state/commands | ScheduleRowVm, ScheduleRowVm, ContainerScheduleListViewModel; SetItems, SetItems, ToggleRowSelection, ToggleRowSelection | BusinessLogicLayer.Contracts.Models, RelayCommand, System | Models/DTOs/ViewState | - |
| `WPFApp/ViewModel/Container/List/ContainerListViewModel.cs` | MVVM ViewModel logic/state/commands | ContainerListViewModel, ContainerListViewModel; SetItems, SetItems | BusinessLogicLayer.Contracts.Models, System.Collections.ObjectModel, WPFApp.MVVM.Commands | Models/DTOs/ViewState | - |
| `WPFApp/MVVM/Validation/ValidationErrors.cs` | MVVM infrastructure (commands/base/validation) | ValidationErrors, ValidationErrors; GetErrors, GetErrors, ClearAll, ClearAll | System, System.Collections, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Validation/WpfRules/MinHoursAtLeastOneRule.cs` | MVVM infrastructure (commands/base/validation) | MinHoursAtLeastOneRule, MinHoursAtLeastOneRule; Validate, Validate | System.Globalization, System.Windows.Controls | - | - |
| `WPFApp/MVVM/Validation/Rules/AvailabilityValidationRules.cs` | MVVM infrastructure (commands/base/validation) | AvailabilityValidationRules, AvailabilityValidationRules; ValidateAll, ValidateAll, ValidateProperty, ValidateProperty | System, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Validation/Rules/ContainerValidationRules.cs` | MVVM infrastructure (commands/base/validation) | ContainerValidationRules, ContainerValidationRules; ValidateAll, ValidateAll, ValidateProperty, ValidateProperty | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Validation/Rules/EmployeeValidationRules.cs` | MVVM infrastructure (commands/base/validation) | EmployeeValidationRules, EmployeeValidationRules; ValidateAll, ValidateAll, ValidateProperty, ValidateProperty | BusinessLogicLayer.Contracts.Employees, System, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Validation/Rules/ScheduleValidationRules.cs` | MVVM infrastructure (commands/base/validation) | ScheduleValidationRules, ScheduleValidationRules; ValidateAll, ValidateAll, ValidateProperty, ValidateProperty | BusinessLogicLayer.Contracts.Models, System, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Validation/Rules/ShopValidationRules.cs` | MVVM infrastructure (commands/base/validation) | ShopValidationRules, ShopValidationRules; ValidateAll, ValidateAll, ValidateProperty, ValidateProperty | BusinessLogicLayer.Contracts.Shops, System, System.Collections.Generic | - | - |
| `WPFApp/MVVM/Core/ObservableObject.cs` | MVVM infrastructure (commands/base/validation) | ObservableObject, ObservableObject; - | System.Runtime.CompilerServices | - | - |
| `WPFApp/MVVM/Core/ViewModelBase.cs` | MVVM infrastructure (commands/base/validation) | ViewModelBase, ViewModelBase; - | System, System.Collections.Concurrent, System.Collections.Generic | Models/DTOs/ViewState | - |
| `WPFApp/MVVM/Threading/UiDebouncedAction.cs` | MVVM infrastructure (commands/base/validation) | UiDebouncedAction, UiDebouncedAction; Schedule, Schedule, Cancel, Cancel | System, System.Threading, System.Threading.Tasks | - | - |
| `WPFApp/MVVM/Commands/AsyncRelayCommand.cs` | MVVM infrastructure (commands/base/validation) | AsyncRelayCommand, AsyncRelayCommand; CanExecute, CanExecute, Execute, Execute | System, System.Threading, System.Threading.Tasks | - | - |
| `WPFApp/MVVM/Commands/RelayCommand.cs` | MVVM infrastructure (commands/base/validation) | RelayCommand, RelayCommand, RelayCommand; CanExecute, CanExecute, Execute, Execute | System, System.Windows, System.Windows.Input | - | - |
| `WPFApp/DesignTime/AvailabilityEditViewDesignVM.cs` | Supporting source/resource file | AvailabilityEditViewDesignVM, AvailabilityEditViewDesignVM; - | System.Collections.ObjectModel, System.Data, System.Threading.Tasks | - | - |
| `WPFApp/DesignTime/AvailabilityListDesignViewModel.cs` | Supporting source/resource file | AvailabilityListDesignRow, AvailabilityListDesignRow, AvailabilityListDesignViewModel; - | System.Collections.ObjectModel | Models/DTOs/ViewState | - |
| `WPFApp/DesignTime/AvailabilityProfileViewDesignVM.cs` | Supporting source/resource file | AvailabilityProfileViewDesignVM, AvailabilityProfileViewDesignVM, AvailabilityProfileRow; CanExecute, CanExecute, Execut | System, System.Collections.ObjectModel, System.ComponentModel | - | - |
| `WPFApp/DesignTime/ContainerProfileViewDesignVm.cs` | Supporting source/resource file | ContainerProfileViewDesignVm, ContainerProfileViewDesignVm, ContainerScheduleListDesignVm; - | System, System.Collections.Generic, System.Collections.ObjectModel | - | - |
| `WPFApp/DesignTime/ContainerScheduleEditDesignVM.cs` | Supporting source/resource file | NotifyBase, NotifyBase, ContainerScheduleEditDesignVM; - | System, System.Collections.ObjectModel, System.ComponentModel | - | - |
| `WPFApp/DesignTime/HomeViewDesignTime.cs` | Supporting source/resource file | HomeViewDesignTime, HomeViewDesignTime, WhoWorksTodayRow; - | System, System.Collections.ObjectModel | - | - |
| `WPFApp/Applications/Preview/AvailabilityPreviewBuilder.cs` | Application-level helper/service | AvailabilityPreviewBuilder, AvailabilityPreviewBuilder; - | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Co | - | - |
| `WPFApp/Applications/Export/IScheduleExportService.cs` | Application-level helper/service | IScheduleExportService, IScheduleExportService; - | System.Threading, System.Threading.Tasks | - | - |
| `WPFApp/Applications/Export/ScheduleExportService.cs` | Application-level helper/service | ScheduleExportContext, ScheduleExportContext, AvailabilityGroupExportData; ExportToExcelAsync, ExportToExcelAsync, Expor | BusinessLogicLayer.Contracts.Models, ClosedXML.Excel, System | - | - |
| `WPFApp/Applications/Diagnostics/ExceptionMessageBuilder.cs` | Application-level helper/service | ExceptionMessageBuilder, ExceptionMessageBuilder; - | System, System.Text | - | - |
| `WPFApp/Applications/Matrix/Schedule/ScheduleMatrixConstants.cs` | Application-level helper/service | ScheduleMatrixConstants, ScheduleMatrixConstants; - | - | - | - |
| `WPFApp/Applications/Matrix/Schedule/ScheduleMatrixEngine.cs` | Application-level helper/service | ScheduleMatrixEngine, ScheduleMatrixEngine; TryParseTime, TryParseTime, BuildScheduleTable, BuildScheduleTable | BusinessLogicLayer.Contracts.Enums, BusinessLogicLayer.Contracts.Models, System | - | - |
| `WPFApp/Applications/Matrix/Schedule/ScheduleTotalsCalculator.cs` | Application-level helper/service | ScheduleTotalsCalculator, ScheduleTotalsCalculator, TotalsResult; Calculate, Calculate, FormatHoursMinutes, FormatHoursM | BusinessLogicLayer.Contracts.Models | - | - |
| `WPFApp/Applications/Matrix/Availability/AvailabilityCellCodeParser.cs` | Application-level helper/service | AvailabilityCellCodeParser, AvailabilityCellCodeParser; TryNormalize, TryNormalize | BusinessLogicLayer.Availability, BusinessLogicLayer.Contracts.Enums, System | - | - |
| `WPFApp/Applications/Matrix/Availability/AvailabilityMatrixEngine.cs` | Application-level helper/service | AvailabilityMatrixEngine, AvailabilityMatrixEngine; GetEmployeeColumnName, GetEmployeeColumnName, EnsureDayColumn, Ensur | System, System.Collections.Generic, System.Data | - | - |
| `WPFApp/Applications/Configuration/DatabasePathProvider.cs` | Application-level helper/service | IDatabasePathProvider, IDatabasePathProvider, DatabasePathProvider; - | System, System.IO | - | - |
| `WPFApp/Applications/Notifications/DatabaseChangeNotifier.cs` | Application-level helper/service | DatabaseChangedEventArgs, DatabaseChangedEventArgs, IDatabaseChangeNotifier; NotifyDatabaseChanged, NotifyDatabaseChange | System, System.Threading, System.Threading.Tasks | - | - |
| `WPFApp/Converters/DataRowViewBoolColumnConverter.cs` | WPF binding converter | DataRowViewBoolColumnConverter, DataRowViewBoolColumnConverter; Convert, Convert, ConvertBack, ConvertBack | System, System.Data, System.Globalization | - | - |
| `WPFApp/Converters/EmployeeTotalHoursHeaderConverter.cs` | WPF binding converter | EmployeeTotalHoursHeaderConverter, EmployeeTotalHoursHeaderConverter; Convert, Convert, ConvertBack, ConvertBack | System, System.Collections.Concurrent, System.Globalization | - | - |
| `WPFApp/Converters/ScheduleMatrixCellBrushConverter.cs` | WPF binding converter | ScheduleMatrixBrushKind, ScheduleMatrixBrushKind, ScheduleMatrixCellBrushConverter; Convert, Convert, ConvertBack, Conve | System, System.Globalization, System.Windows | - | - |
| `WPFApp/Converters/ScheduleMatrixCellReferenceConverter.cs` | WPF binding converter | ScheduleMatrixCellReferenceConverter, ScheduleMatrixCellReferenceConverter; Convert, Convert, ConvertBack, ConvertBack | System, System.Globalization, System.Windows.Data | - | - |
| `WPFApp/Converters/ScheduleMatrixEmployeeTotalHoursConverter.cs` | WPF binding converter | ScheduleMatrixEmployeeTotalHoursConverter, ScheduleMatrixEmployeeTotalHoursConverter; Convert, Convert, ConvertBack, Con | System, System.Globalization, System.Windows.Data | - | - |
| `WPFApp/Converters/Validation/FirstValidationErrorConverter.cs` | WPF binding converter | FirstValidationErrorConverter, FirstValidationErrorConverter; Convert, Convert, ConvertBack, ConvertBack | System, System.Globalization, System.Linq | - | - |
| `WPFApp/Resources/ViewTemplates.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Excel/ContainerTemplate.xlsx` | Excel export template artifact | artifact | used by export service | Excel template | - |
| `WPFApp/Resources/Excel/ScheduleTemplate.xlsx` | Excel export template artifact | artifact | used by export service | Excel template | - |
| `WPFApp/Resources/Styles/Buttons.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Styles/DataGrids.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Styles/Icons.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Styles/ScrollBars.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Styles/TextBoxes.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Styles/Theme.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Availability/AvailabilityResources.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Container/ContainerScheduleResources.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Controls/NumericUpDown.xaml` | WPF markup (view/resource/style/control template) | -; - | - | - | - |
| `WPFApp/Resources/Controls/NumericUpDown.xaml.cs` | WPF code-behind for UI behavior/events | NumericUpDown, NumericUpDown; - | System, System.Globalization, System.Windows | - | - |

### UI Flow (WPFApp)
- `MainWindow` + `MainViewModel` керують навігацією між Home/Employee/Shop/Availability/Container/Information/Database.
- Кожний модуль має набір Views (List/Edit/Profile/Root) і відповідний ViewModel з командами CRUD/refresh/filter.
- Для складних матриць (availability/schedule) використані окремі engines у `WPFApp/Applications/Matrix/*` та часткові ViewModel-файли.

## 5. End-to-End Flows (Use Cases)
1. Навігація в модуль: Main menu click -> `MainViewModel` command -> resolve child VM через DI -> показ відповідного View.
2. CRUD Employee: EmployeeView/List/Edit -> `Employee*ViewModel` -> `IEmployeeFacade`/`IEmployeeService` -> `EmployeeRepository` -> `Employees` table.
3. CRUD Shop: ShopView/List/Edit -> `Shop*ViewModel` -> `IShopFacade`/`IShopService` -> `ShopRepository` -> `Shops` table.
4. CRUD Container: ContainerView/List/Edit -> `ContainerViewModel` -> `IContainerService` -> `ContainerRepository` -> `Containers` table.
5. Створення availability group: AvailabilityEdit -> parse/validate codes (`AvailabilityCellCodeParser`, BLL `AvailabilityCodeParser`) -> `IAvailabilityGroupService.SaveGroupAsync` -> група/учасники/дні в БД.
6. Пошук/фільтрація availability: AvailabilityList/Profile -> `AvailabilityViewModel` -> `IAvailabilityGroupService.GetAll/GetByValue` -> repositories.
7. Створення/редагування schedule: Container schedule editor -> `ContainerScheduleEditViewModel` -> `IScheduleService.SaveWithDetailsAsync` -> `Schedules` + `ScheduleEmployees` + `ScheduleSlots` + `ScheduleCellStyles`.
8. Генерація графіка: UI action Generate -> `IScheduleGenerator.GenerateAsync` -> обчислення слотів з обмеженнями -> збереження через schedule service.
9. Експорт в Excel: UI export command -> `ScheduleExportService.ExportToExcelAsync` -> ClosedXML + templates -> файл на диску.
10. Експорт в SQL: UI export command -> `ScheduleExportService.ExportToSqlAsync` + BLL `IScheduleExportDataBuilder` -> sql script/output.
11. DB Admin: DatabaseView -> `DatabaseViewModel` -> `ISqliteAdminFacade` -> DAL `SqliteAdminService` (raw SQL/PRAGMA/hash).
12. Оновлення стану після змін БД: operation -> `DatabaseChangeNotifier` -> підписані VM роблять reload.

## 6. Data Model
- Основні сутності: Container, Employee, Shop, Schedule, ScheduleEmployee, ScheduleSlot, ScheduleCellStyle, Bind, AvailabilityGroup, AvailabilityGroupMember, AvailabilityGroupDay.
- Визначення в DAL: `DataAccessLayer/Models/*.cs`; BLL контракти/копії: `BusinessLogicLayer/Contracts/Models/Models.cs`; мапінг між шарами: `BusinessLogicLayer/Mappers/ModelMapper.cs`.
- DbSets у `AppDbContext`: `Containers`, `Employees`, `Shops`, `Schedules`, `ScheduleEmployees`, `ScheduleSlots`, `ScheduleCellStyles`, `AvailabilityBinds`, `AvailabilityGroups`, `AvailabilityGroupMembers`, `AvailabilityGroupDays`.
- Ключові обмеження: унікальні індекси на імена/композити, check constraints для month/day, форматів shift time і availability interval; зв’язки FK з Cascade/Restrict/SetNull (див. `AppDbContext`).

## 7. Configuration & Environment
- Конфіг-файли `appsettings*.json`/`*.config`: **Not found** у репозиторії.
- Connection string формується в коді: `Data Source=%LocalAppData%/GF3/SQLite.db` (`WPFApp/Applications/Configuration/DatabasePathProvider.cs`).
- Та сама логіка для design-time міграцій у `AppDbContextFactory`.
- Секрети/API keys: **Not found**.
- Важливі runtime залежності: Windows desktop (WPF), доступ до файлової системи LocalAppData, SQLite engine.

## 8. Migration Readiness Notes (для майбутнього .NET backend + React)
### API candidates (BLL -> HTTP)
- `IEmployeeFacade/IEmployeeService`: list/get/search/create/update/delete employees.
- `IShopFacade/IShopService`: list/get/search/create/update/delete shops.
- `IContainerService`: CRUD containers + search.
- `IAvailabilityGroupService`: list/get/search/save-full-group/load-full-group/delete.
- `IScheduleService`: list/get/detail/by-container/save-with-details/delete.
- `IScheduleSlotService`, `IScheduleEmployeeService`: granular endpoints при потребі.
- `ISqliteAdminFacade`: тільки для локального адмін-режиму (обережно; високий ризик безпеки для remote API).
- `IScheduleExportDataBuilder`/generator/export services: або backend jobs, або окремий локальний сервіс.

### WPF screen/function -> React mapping (draft)
| WPF module/screen | React target | Notes |
|---|---|---|
| HomeView | Dashboard page | KPI/cards/current month stats. |
| EmployeeView + List/Edit/Profile | Employees pages | CRUD + validation. |
| ShopView + List/Edit/Profile | Shops pages | CRUD + validation. |
| AvailabilityView + List/Edit/Profile | Availability pages | Matrix editor + code normalization. |
| ContainerView + Edit/Profile + ScheduleList/Edit/Profile | Containers & Schedules pages | Найскладніша частина: matrix editing, block selection, styling. |
| DatabaseView | Admin/maintenance page | Можливо лишити тільки в desktop-admin режимі. |
| InformationView | About/help page | Просте перенесення. |

### DAL what to keep
- EF Core моделі, `AppDbContext`, репозиторії і міграції слід зберегти як data-access шар backend API.
- `SqliteAdminService` — локальний infrastructure/admin компонент; не відкривати назовні без authz.

### Risks / complex points
- Важкі класи (`ScheduleGenerator`, `ScheduleExportService`, matrix engines, великі partial ViewModels) — потребують поетапного винесення логіки з UI в backend/domain services.
- Частина логіки дублюється між BLL та WPF `Applications/Matrix/*` (потрібна уніфікація).
- Локальні файлові операції/шляхи (`LocalAppData`, Excel templates) — адаптувати до server-hosted сценарію.
- Raw SQL admin endpoint потенційно небезпечний.
- Tight coupling на WPF state/dispatcher/INotifyDataErrorInfo в VM для validation/UX.

### Must-know configs/integrations
- `%LocalAppData%/GF3/SQLite.db` (головне джерело даних).
- Excel templates: `WPFApp/Resources/Excel/ContainerTemplate.xlsx`, `WPFApp/Resources/Excel/ScheduleTemplate.xlsx`.
- DI bootstrap: `WPFApp/App.xaml.cs` + `BusinessLogicLayer/Extensions.cs` + `DataAccessLayer/Models/DataBaseContext/Extensions.cs`.

## 9. Appendix
### External libraries
- EF Core (`Microsoft.EntityFrameworkCore`, `Sqlite`, `Tools`, `Design`) in DAL.
- `ClosedXML` для Excel export у WPFApp.
- `Microsoft.Extensions.Hosting` + `Microsoft.Extensions.DependencyInjection` для DI/Host у WPFApp/BLL.
- `CommunityToolkit.Mvvm` referenced in WPFApp.
- xUnit stack in ArchitectureTests.

### Entry points
- `WPFApp/App.xaml` + `WPFApp/App.xaml.cs` (application startup/shutdown).
- `WPFApp/View/MainWindow.xaml` + code-behind (main shell).
- `BusinessLogicLayer.Extensions.AddBusinessLogicStack` (BLL+DAL composition).
- `DataAccessLayer.Models.DataBaseContext.Extensions.AddDataAccess` (DAL composition).

### Important comments / TODO/FIXME
- TODO/FIXME markers: **Not found** (scanned via ripgrep).
- Some files have auto-added descriptive header comments (Ukrainian) but no pending-action markers.
