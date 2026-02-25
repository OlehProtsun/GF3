# PROJECT OVERVIEW
- Solution name: `GF3` (from `GF3.sln`).
- High-level purpose: Desktop WPF application for managing employees, availability groups, containers, shops, binds, and schedule generation/export backed by SQLite via Entity Framework Core.
- Main technologies: .NET 10, WPF, MVVM-style ViewModels, EF Core + SQLite, Microsoft.Extensions.Hosting/DI, ClosedXML export.
- Architectural layers/projects: `WPFApp` (UI + app services), `BusinessLogicLayer` (domain services/generators/validators), `DataAccessLayer` (entities/repositories/DbContext/migrations).
- Layer interaction: WPFApp references BLL and DAL directly; BLL references DAL; DAL is persistence foundation. UI uses services/viewmodels and reaches database through BLL service abstractions plus some DAL model coupling.
- Architectural style: Layered architecture with repository/service patterns and MVVM, but partially mixed boundaries (UI knows DAL models; BLL mostly thin wrappers over repositories).

# SOLUTION STRUCTURE TREE
```text
├── ARCHITECTURE_NOTES.md
├── BusinessLogicLayer
│   ├── Availability
│   │   ├── AvailabilityCodeParser.cs
│   │   ├── AvailabilityGroupValidator.cs
│   │   └── AvailabilityPayloadBuilder.cs
│   ├── BusinessLogicLayer.csproj
│   ├── Common
│   │   └── ValidationException.cs
│   ├── Extensions.cs
│   ├── Generators
│   │   ├── IScheduleGenerator.cs
│   │   └── ScheduleGenerator.cs
│   └── Services
│       ├── Abstractions
│       │   ├── IAvailabilityGroupService.cs
│       │   ├── IBaseService.cs
│       │   ├── IBindService.cs
│       │   ├── IContainerService.cs
│       │   ├── IEmployeeService.cs
│       │   ├── IScheduleEmployeeService.cs
│       │   ├── IScheduleService.cs
│       │   ├── IScheduleSlotService.cs
│       │   └── IShopService.cs
│       ├── AvailabilityGroupService.cs
│       ├── BindService.cs
│       ├── ContainerService.cs
│       ├── EmployeeService.cs
│       ├── GenericService.cs
│       ├── ScheduleEmployeeService.cs
│       ├── ScheduleService.cs
│       ├── ScheduleSlotService.cs
│       └── ShopService.cs
├── DATABASE_VIEW_NOTES.md
├── DataAccessLayer
│   ├── DataAccessLayer.csproj
│   ├── Migrations
│   │   ├── 20251114184904_AddNameToAvailabilityMonth.Designer.cs
│   │   ├── 20251114184904_AddNameToAvailabilityMonth.cs
│   │   ├── 20251114185421_AddNameToAvailabilityMonth2.Designer.cs
│   │   ├── 20251114185421_AddNameToAvailabilityMonth2.cs
│   │   ├── 20251206194754_SlotTimes.Designer.cs
│   │   ├── 20251206194754_SlotTimes.cs
│   │   ├── 20251216172827_AddBind.Designer.cs
│   │   ├── 20251216172827_AddBind.cs
│   │   ├── 20251219185609_DropScheduleStatusAndComment.Designer.cs
│   │   ├── 20251219185609_DropScheduleStatusAndComment.cs
│   │   ├── 20251220111829_AddAvailabilityGroups.Designer.cs
│   │   ├── 20251220111829_AddAvailabilityGroups.cs
│   │   ├── 20251229184256_RemoveShopAndAvailabilityLegacy.Designer.cs
│   │   ├── 20251229184256_RemoveShopAndAvailabilityLegacy.cs
│   │   ├── 20260103192437_addNoteToSchedule.Designer.cs
│   │   ├── 20260103192437_addNoteToSchedule.cs
│   │   ├── 20260106132614_AddShopLogic.Designer.cs
│   │   ├── 20260106132614_AddShopLogic.cs
│   │   ├── 20260106140227_ShopLogicFix.Designer.cs
│   │   ├── 20260106140227_ShopLogicFix.cs
│   │   ├── 20260107120000_AddScheduleCellStyles.cs
│   │   ├── 20260113190024_AddScheduleAvailabilityGroupId.Designer.cs
│   │   ├── 20260113190024_AddScheduleAvailabilityGroupId.cs
│   │   ├── 20260113191835_Init2.Designer.cs
│   │   ├── 20260113191835_Init2.cs
│   │   ├── 20260115120000_AddUniqueIndexes.Designer.cs
│   │   ├── 20260115120000_AddUniqueIndexes.cs
│   │   ├── 20260115125626_databaseFix.Designer.cs
│   │   ├── 20260115125626_databaseFix.cs
│   │   ├── 20260116183224_cellPaintAdded.Designer.cs
│   │   ├── 20260116183224_cellPaintAdded.cs
│   │   ├── 20260220191954_FixMissingScheduleCellStyleTable.Designer.cs
│   │   ├── 20260220191954_FixMissingScheduleCellStyleTable.cs
│   │   └── AppDbContextModelSnapshot.cs
│   ├── Models
│   │   ├── AvailabilityGroupDayModel.cs
│   │   ├── AvailabilityGroupMemberModel.cs
│   │   ├── AvailabilityGroupModel.cs
│   │   ├── BindModel.cs
│   │   ├── ContainerModel.cs
│   │   ├── DataBaseContext
│   │   │   ├── AppDbContext.cs
│   │   │   ├── AppDbContextFactory.cs
│   │   │   └── Extensions.cs
│   │   ├── EmployeeModel.cs
│   │   ├── Enums
│   │   │   ├── AvailabilityKind.cs
│   │   │   └── SlotStatus.cs
│   │   ├── ScheduleCellStyleModel.cs
│   │   ├── ScheduleEmployeeModel.cs
│   │   ├── ScheduleModel.cs
│   │   ├── ScheduleSlotModel.cs
│   │   └── ShopModel.cs
│   └── Repositories
│       ├── Abstractions
│       │   ├── IAvailabilityGroupDayRepository.cs
│       │   ├── IAvailabilityGroupMemberRepository.cs
│       │   ├── IAvailabilityGroupRepository.cs
│       │   ├── IBaseRepository.cs
│       │   ├── IBindRepository.cs
│       │   ├── IContainerRepository.cs
│       │   ├── IEmployeeRepository.cs
│       │   ├── IScheduleCellStyleRepository.cs
│       │   ├── IScheduleEmployeeRepository.cs
│       │   ├── IScheduleRepository.cs
│       │   ├── IScheduleSlotRepository.cs
│       │   └── IShopRepository.cs
│       ├── AvailabilityGroupDayRepository.cs
│       ├── AvailabilityGroupMemberRepository.cs
│       ├── AvailabilityGroupRepository.cs
│       ├── BindRepository.cs
│       ├── ContainerRepository.cs
│       ├── EmployeeRepository.cs
│       ├── GenericRepository.cs
│       ├── ScheduleCellStyleRepository.cs
│       ├── ScheduleEmployeeRepository.cs
│       ├── ScheduleRepository.cs
│       ├── ScheduleSlotRepository.cs
│       └── ShopRepository.cs
├── GF3.sln
├── README.md
├── SCHEDULE_GENERATION_FIX.md
├── WPFApp
│   ├── ASYNC_OVERLAY_PATTERN.md
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── AssemblyInfo.cs
│   ├── Controls
│   │   ├── NumericUpDown.xaml
│   │   └── NumericUpDown.xaml.cs
│   ├── Converters
│   │   ├── DataRowViewBoolColumnConverter.cs
│   │   ├── EmployeeTotalHoursHeaderConverter.cs
│   │   ├── FirstValidationErrorConverter.cs
│   │   ├── ScheduleMatrixCellBrushConverter.cs
│   │   ├── ScheduleMatrixCellReferenceConverter.cs
│   │   └── ScheduleMatrixEmployeeTotalHoursConverter.cs
│   ├── DesignTime
│   │   ├── AvailabilityEditViewDesignVM.cs
│   │   ├── AvailabilityListDesignViewModel.cs
│   │   ├── AvailabilityProfileViewDesignVM.cs
│   │   ├── ContainerProfileViewDesignVm.cs
│   │   ├── ContainerScheduleEditDesignVM.cs
│   │   └── HomeViewDesignTime.cs
│   ├── Infrastructure
│   │   ├── AsyncRelayCommand.cs
│   │   ├── AvailabilityMatrix
│   │   │   ├── AvailabilityCellCodeParser.cs
│   │   │   └── AvailabilityMatrixEngine.cs
│   │   ├── AvailabilityPreview
│   │   │   └── AvailabilityPreviewBuilder.cs
│   │   ├── ColorHelpers.cs
│   │   ├── ExceptionMessageBuilder.cs
│   │   ├── Hotkeys
│   │   │   └── KeyGestureTextHelper.cs
│   │   ├── ObservableObject.cs
│   │   ├── RelayCommand.cs
│   │   ├── ScheduleMatrix
│   │   │   ├── ScheduleMatrixColumnBuilder.cs
│   │   │   ├── ScheduleMatrixConstants.cs
│   │   │   ├── ScheduleMatrixEngine.cs
│   │   │   └── ScheduleTotalsCalculator.cs
│   │   ├── Threading
│   │   │   └── UiDebouncedAction.cs
│   │   ├── Validation
│   │   │   ├── AvailabilityValidationRules.cs
│   │   │   ├── ContainerValidationRules.cs
│   │   │   ├── EmployeeValidationRules.cs
│   │   │   ├── FirstValidationErrorConverter.cs
│   │   │   ├── MinHoursAtLeastOneRule.cs
│   │   │   ├── ScheduleValidationRules.cs
│   │   │   ├── ShopValidationRules.cs
│   │   │   └── ValidationErrors.cs
│   │   └── ViewModelBase.cs
│   ├── Resources
│   │   ├── Availability
│   │   │   └── AvailabilityResources.xaml
│   │   ├── Container
│   │   │   └── ContainerScheduleResources.xaml
│   │   ├── Excel
│   │   │   ├── ContainerTemplate.xlsx
│   │   │   └── ScheduleTemplate.xlsx
│   │   ├── Styles
│   │   │   ├── Buttons.xaml
│   │   │   ├── DataGrids.xaml
│   │   │   ├── Icons.xaml
│   │   │   ├── ScrollBars.xaml
│   │   │   ├── TextBoxes.xaml
│   │   │   └── Theme.xaml
│   │   └── ViewTemplates.xaml
│   ├── Service
│   │   ├── ColorPickerService.cs
│   │   ├── CustomMessageBox.cs
│   │   ├── DatabaseChangeNotifier.cs
│   │   ├── DatabasePathProvider.cs
│   │   ├── IColorPickerService.cs
│   │   ├── IScheduleExportService.cs
│   │   ├── DiagnosticsServiceImpl.cs
│   │   ├── ScheduleExportService.cs
│   │   └── SqliteAdminService.cs
│   ├── View
│   │   ├── Availability
│   │   │   ├── AvailabilityEditView.xaml
│   │   │   ├── AvailabilityEditView.xaml.cs
│   │   │   ├── AvailabilityListView.xaml
│   │   │   ├── AvailabilityListView.xaml.cs
│   │   │   ├── AvailabilityProfileView.xaml
│   │   │   ├── AvailabilityProfileView.xaml.cs
│   │   │   ├── AvailabilityView.xaml
│   │   │   └── AvailabilityView.xaml.cs
│   │   ├── Container
│   │   │   ├── ContainerEditView.xaml
│   │   │   ├── ContainerEditView.xaml.cs
│   │   │   ├── ContainerListView.xaml
│   │   │   ├── ContainerListView.xaml.cs
│   │   │   ├── ContainerProfileView.xaml
│   │   │   ├── ContainerProfileView.xaml.cs
│   │   │   ├── ContainerScheduleEditView.xaml
│   │   │   ├── ContainerScheduleEditView.xaml.cs
│   │   │   ├── ContainerScheduleProfileView.xaml
│   │   │   ├── ContainerScheduleProfileView.xaml.cs
│   │   │   ├── ContainerView.xaml
│   │   │   └── ContainerView.xaml.cs
│   │   ├── DatabaseView.xaml
│   │   ├── DatabaseView.xaml.cs
│   │   ├── Dialogs
│   │   │   ├── ColorPickerDialog.xaml
│   │   │   ├── ColorPickerDialog.xaml.cs
│   │   │   ├── CustomMessageBoxView.xaml
│   │   │   ├── CustomMessageBoxView.xaml.cs
│   │   │   ├── UIStatus.xaml
│   │   │   └── UIStatus.xaml.cs
│   │   ├── Employee
│   │   │   ├── EmployeeEditView.xaml
│   │   │   ├── EmployeeEditView.xaml.cs
│   │   │   ├── EmployeeListView.xaml
│   │   │   ├── EmployeeListView.xaml.cs
│   │   │   ├── EmployeeProfileView.xaml
│   │   │   ├── EmployeeProfileView.xaml.cs
│   │   │   ├── EmployeeView.xaml
│   │   │   └── EmployeeView.xaml.cs
│   │   ├── Helpers
│   │   │   ├── AvailabilityMatrixGridBuilder.cs
│   │   │   └── AvailabilityViewInputHelper.cs
│   │   ├── Home
│   │   │   ├── HomeView.xaml
│   │   │   └── HomeView.xaml.cs
│   │   ├── Information
│   │   │   ├── InformationView.xaml
│   │   │   └── InformationView.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   └── Shop
│   │       ├── ShopEditView.xaml
│   │       ├── ShopEditView.xaml.cs
│   │       ├── ShopListView.xaml
│   │       ├── ShopListView.xaml.cs
│   │       ├── ShopProfileView.xaml
│   │       ├── ShopProfileView.xaml.cs
│   │       ├── ShopView.xaml
│   │       └── ShopView.xaml.cs
│   ├── ViewModel
│   │   ├── Availability
│   │   │   ├── Edit
│   │   │   │   ├── AvailabilityEditViewModel.Batching.cs
│   │   │   │   ├── AvailabilityEditViewModel.Binds.cs
│   │   │   │   ├── AvailabilityEditViewModel.Commands.cs
│   │   │   │   ├── AvailabilityEditViewModel.Fields.cs
│   │   │   │   ├── AvailabilityEditViewModel.Matrix.cs
│   │   │   │   ├── AvailabilityEditViewModel.Validation.cs
│   │   │   │   └── AvailabilityEditViewModel.cs
│   │   │   ├── Helpers
│   │   │   │   ├── BindRow.cs
│   │   │   │   └── EmployeeListItem.cs
│   │   │   ├── List
│   │   │   │   └── AvailabilityListViewModel.cs
│   │   │   ├── Main
│   │   │   │   ├── AvailabilityViewModel.Binds.cs
│   │   │   │   ├── AvailabilityViewModel.DatabaseReload.cs
│   │   │   │   ├── AvailabilityViewModel.Employees.cs
│   │   │   │   ├── AvailabilityViewModel.Groups.cs
│   │   │   │   ├── AvailabilityViewModel.Initialization.cs
│   │   │   │   ├── AvailabilityViewModel.NavStatus.cs
│   │   │   │   ├── AvailabilityViewModel.Navigation.cs
│   │   │   │   ├── AvailabilityViewModel.Ui.cs
│   │   │   │   └── AvailabilityViewModel.cs
│   │   │   └── Profile
│   │   │       └── AvailabilityProfileViewModel.cs
│   │   ├── Container
│   │   │   ├── Edit
│   │   │   │   ├── ContainerEditViewModel.cs
│   │   │   │   ├── ContainerViewModel.AvailabilityPreview.cs
│   │   │   │   ├── ContainerViewModel.Containers.cs
│   │   │   │   ├── ContainerViewModel.DatabaseReload.cs
│   │   │   │   ├── ContainerViewModel.Lookups.cs
│   │   │   │   ├── ContainerViewModel.Navigation.cs
│   │   │   │   ├── ContainerViewModel.ScheduleEditor.cs
│   │   │   │   ├── ContainerViewModel.Schedules.BlockFactory.cs
│   │   │   │   ├── ContainerViewModel.Schedules.Open.cs
│   │   │   │   ├── ContainerViewModel.Schedules.SaveGenerate.cs
│   │   │   │   ├── ContainerViewModel.cs
│   │   │   │   └── Helpers
│   │   │   │       ├── ContainerSection.cs
│   │   │   │       ├── ContainerViewModel.Export.cs
│   │   │   │       └── ContainerViewModel.Ui.cs
│   │   │   ├── List
│   │   │   │   └── ContainerListViewModel.cs
│   │   │   ├── Profile
│   │   │   │   └── ContainerProfileViewModel.cs
│   │   │   ├── ScheduleEdit
│   │   │   │   ├── ContainerScheduleEditViewModel.CellStyling.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.Lookups.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.MatrixBinding.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.ModelBinding.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.Selection.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.Totals.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.Validation.cs
│   │   │   │   ├── ContainerScheduleEditViewModel.cs
│   │   │   │   └── Helpers
│   │   │   │       ├── IScheduleMatrixStyleProvider.cs
│   │   │   │       ├── ScheduleBlockViewModel.cs
│   │   │   │       ├── ScheduleCellStyleStore.cs
│   │   │   │       └── ScheduleMatrixCellRef.cs
│   │   │   ├── ScheduleList
│   │   │   │   └── ContainerScheduleListViewModel.cs
│   │   │   └── ScheduleProfile
│   │   │       └── ContainerScheduleProfileViewModel.cs
│   │   ├── Database
│   │   │   └── DatabaseViewModel.cs
│   │   ├── Dialogs
│   │   │   └── CustomMessageBoxViewModel.cs
│   │   ├── Employee
│   │   │   ├── EmployeeEditViewModel.cs
│   │   │   ├── EmployeeListViewModel.cs
│   │   │   ├── EmployeeProfileViewModel.cs
│   │   │   ├── EmployeeViewModel.cs
│   │   │   └── Helpers
│   │   │       └── EmployeeDisplayHelper.cs
│   │   ├── Home
│   │   │   └── HomeViewModel.cs
│   │   ├── Information
│   │   │   └── InformationViewModel.cs
│   │   ├── Main
│   │   │   ├── MainViewModel.cs
│   │   │   └── NavPage.cs
│   │   └── Shop
│   │       ├── Helpers
│   │       │   └── ShopDisplayHelper.cs
│   │       ├── ShopEditViewModel.cs
│   │       ├── ShopListViewModel.cs
│   │       ├── ShopProfileViewModel.cs
│   │       └── ShopViewModel.cs
│   └── WPFApp.csproj
└── WinFormsApp
    └── View
        ├── Container
        │   └── ContainerView.ScheduleStyles.cs
        └── Shared
            ├── BusyForm.cs
            ├── ColorHexConverter.cs
            └── ScheduleCellStyleResolver.cs
```

# PROJECTS SUMMARY
## WPFApp
- Purpose: WPF desktop presentation layer with views/viewmodels, commands, validation helpers, and app-level services.
- Target framework: `net10.0-windows`.
- Dependencies/references: Project refs=['..\\BusinessLogicLayer\\BusinessLogicLayer.csproj', '..\\DataAccessLayer\\DataAccessLayer.csproj']; NuGet packages=['ClosedXML', 'CommunityToolkit.Mvvm', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Design', 'Microsoft.Extensions.DependencyInjection', 'Microsoft.Extensions.Hosting'].
- Used by layers/projects: ['solution entry point only'].
- Main namespaces/folders: inferred from directory structure and namespace declarations.
- Cohesion notes: folders generally grouped by feature (Availability/Container/Shop/Schedule) but some cross-cutting concerns are spread across partial classes/helpers.
- Risks/smells: direct DAL model usage in UI/BLL contracts, large ViewModels split across many partial files, and many migrations indicating evolving schema.
## DataAccessLayer
- Purpose: Persistence layer containing EF Core DbContext, entity models, repositories, and migrations.
- Target framework: `net10.0`.
- Dependencies/references: Project refs=none; NuGet packages=['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Design', 'Microsoft.EntityFrameworkCore.Sqlite', 'Microsoft.EntityFrameworkCore.Tools'].
- Used by layers/projects: ['solution entry point only'].
- Main namespaces/folders: inferred from directory structure and namespace declarations.
- Cohesion notes: folders generally grouped by feature (Availability/Container/Shop/Schedule) but some cross-cutting concerns are spread across partial classes/helpers.
- Risks/smells: direct DAL model usage in UI/BLL contracts, large ViewModels split across many partial files, and many migrations indicating evolving schema.
## BusinessLogicLayer
- Purpose: Business/service layer exposing CRUD/services, schedule generation, and availability validation/parsing utilities.
- Target framework: `net10.0`.
- Dependencies/references: Project refs=['..\\DataAccessLayer\\DataAccessLayer.csproj']; NuGet packages=none.
- Used by layers/projects: ['solution entry point only'].
- Main namespaces/folders: inferred from directory structure and namespace declarations.
- Cohesion notes: folders generally grouped by feature (Availability/Container/Shop/Schedule) but some cross-cutting concerns are spread across partial classes/helpers.
- Risks/smells: direct DAL model usage in UI/BLL contracts, large ViewModels split across many partial files, and many migrations indicating evolving schema.

# DETAILED FILE-BY-FILE DOCUMENTATION

## ARCHITECTURE_NOTES.md
### Purpose
- Project documentation/note file capturing design decisions or usage notes.
- Supports: Solution/documentation.

### Type Information
- File type: `.md`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily n/a.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Availability/AvailabilityCodeParser.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Availability'].
- Main declared type(s): ['class AvailabilityCodeParser'].

### Dependencies
- Key imports/usings: ['System.Globalization'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Globalization'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool TryParse(string code, out AvailabilityKind kind, out string? intervalStr)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryNormalizeInterval(string input, out string normalized)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseTime(string s, out TimeSpan ts)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Availability/AvailabilityGroupValidator.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Availability'].
- Main declared type(s): ['class AvailabilityGroupValidator'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Availability/AvailabilityPayloadBuilder.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Availability'].
- Main declared type(s): ['class AvailabilityPayloadBuilder'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool TryBuild(IEnumerable<(int employeeId, IList<(int day, string code)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/BusinessLogicLayer.csproj
### Purpose
- Build/solution configuration file declaring projects, references, packages, and target frameworks.
- Supports: Business logic.

### Type Information
- File type: `.csproj`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily business/domain.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Common/ValidationException.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Common'].
- Main declared type(s): ['class ValidationException'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Extensions.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer'].
- Main declared type(s): ['class Extensions'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Services.Abstractions', 'Microsoft.Extensions.DependencyInjection', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions'].
- External dependencies: ['Microsoft.Extensions.DependencyInjection', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public IServiceCollection AddBusinessLogicLayer(this IServiceCollection serviceCollection)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Generators/IScheduleGenerator.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Generators'].
- Main declared type(s): ['interface IScheduleGenerator'].

### Dependencies
- Key imports/usings: ['System.Threading', 'System.Threading.Tasks', 'DataAccessLayer.Models'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Public/visible properties (detected):
  - `public interface IScheduleGenerator`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Generators/ScheduleGenerator.cs
### Purpose
- C# source file containing application logic.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Generators'].
- Main declared type(s): ['class ScheduleGenerator', 'class ShiftTemplate'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Globalization', 'System.Reflection', 'System.Threading', 'System.Threading.Tasks', 'DataAccessLayer.Models', 'DataAccessLayer.Models.Enums'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums'].
- External dependencies: ['System.Collections.Generic', 'System.Globalization', 'System.Reflection', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Public/visible properties (detected):
  - `public int Index`
  - `public string From`
  - `public string To`
  - `public int StartMin`
  - `public int EndMin`
  - `public double Hours`
- Methods/constructors (detected signatures):
  - `public Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private IList<ScheduleSlotModel> GenerateCore(ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress,
            CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void FillAllUnfurnishedOrderIndependent(ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            ref int rrCursor,
            CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private int FindBestOrderIndependent(int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool restrictSecondShift,
            ScheduleModel schedule,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            List<ShiftTemplate> shifts,
            int n,
            int[] assignedStamp,
            int stamp,
            int rrCursor)` — implementation in file; inspect branches/side-effects in source.
  - `private void StrictMinHoursRepairMinConflicts(ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            double[] minHours,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private bool CanAddShiftOrderIndependent(ScheduleModel schedule,
            int emp,
            int day,
            double addedHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int stride)` — implementation in file; inspect branches/side-effects in source.
  - `private int CountLeft(int emp, int day, int[] shiftsPerDay, int stride, Func<int, bool> predicate)` — implementation in file; inspect branches/side-effects in source.
  - `private int CountRight(int emp, int day, int daysInMonth, int[] shiftsPerDay, int stride, Func<int, bool> predicate)` — implementation in file; inspect branches/side-effects in source.
  - `private int PickCandidatePhase1Fast(int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool preferNoSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            int[] scarcityDays,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int[] assignedStamp,
            int stamp,
            int rrCursor)` — implementation in file; inspect branches/side-effects in source.
  - `private int FindBestPhase1Fast(int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool restrictSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            int[] scarcityDays,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int[] assignedStamp,
            int stamp,
            int rrCursor)` — implementation in file; inspect branches/side-effects in source.
  - `private bool CanAssignPhase1Fast(int emp,
            int day,
            double addedHours,
            int shiftsAlreadyToday,
            ScheduleModel schedule,
            double[] totalHours,
            int[] fullDaysCount,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount)` — implementation in file; inspect branches/side-effects in source.
  - `private void AssignPhase1Fast(int day,
            int shiftIdx,
            int slotIdx,
            int emp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int stride,
            int pps,
            int n,
            int[] assignedStamp,
            int stamp)` — implementation in file; inspect branches/side-effects in source.
  - `private void AssignToEmptySlot(int day,
            int shiftIdx,
            int slotIdx,
            int emp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int shiftCount,
            int stride,
            int pps,
            int n)` — implementation in file; inspect branches/side-effects in source.
  - `private void SwapSlot(int day,
            int shiftIdx,
            int slotIdx,
            int donor,
            int receiver,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int pps,
            int shiftCount,
            int stride,
            int n)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryPredictAssignmentImpact(int day,
            int shiftIdx,
            int slotIdx,
            int candidateEmp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int shiftCount,
            out double candidateHours,
            out int pairedEmp,
            out double pairedDelta)` — implementation in file; inspect branches/side-effects in source.
  - `private void RecomputeAfterChange(int day,
            int shiftIdx,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int n,
            int pps,
            int shiftCount)` — implementation in file; inspect branches/side-effects in source.
  - `private void RecomputeSingleShiftSlot(int day,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            ShiftTemplate sh,
            double[] totalHours,
            int n,
            int pps)` — implementation in file; inspect branches/side-effects in source.
  - `private void RecomputeTwoShiftPair(int day,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            ShiftTemplate sh1,
            ShiftTemplate sh2,
            double[] totalHours,
            int n,
            int pps,
            int shiftCount)` — implementation in file; inspect branches/side-effects in source.
  - `private int NextStamp(int[] stampArr, int stamp)` — implementation in file; inspect branches/side-effects in source.
  - `private void PremarkAssignedInShift(int[] assigned,
            int day,
            int shiftIdx,
            int pps,
            int shiftCount,
            int[] assignedStamp,
            int stamp)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsEmployeeInShift(int[] assigned, int day, int shiftIdx, int pps, int shiftCount, int emp)` — implementation in file; inspect branches/side-effects in source.
  - `private int SlotBase(int day, int shiftIdx, int pps, int shiftCount)` — implementation in file; inspect branches/side-effects in source.
  - `private int SlotIndex(int day, int shiftIdx, int slotIdx, int pps, int shiftCount)` — implementation in file; inspect branches/side-effects in source.
  - `private void InitSlotBaseTimes(int daysInMonth,
            int shiftCount,
            int pps,
            List<ShiftTemplate> shifts,
            int[] slotFromMin,
            int[] slotToMin)` — implementation in file; inspect branches/side-effects in source.
  - `private double ReadMinHours(ScheduleEmployeeModel e)` — implementation in file; inspect branches/side-effects in source.
  - `private List<ShiftTemplate> GetShiftTemplates(ScheduleModel schedule)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryCreateShiftTemplate(string? shiftText, int index, out ShiftTemplate template)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryReadAvailabilityWindow(object dayModel, out int fromMin, out int toMin)` — implementation in file; inspect branches/side-effects in source.
  - `private bool AvailabilityKindRequiresWindow(object? kind)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryReadIntervalText(object obj, string[] names, out int fromMin, out int toMin)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseIntervalRange(string text, out int fromMin, out int toMin)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryReadTime(object obj, string[] names, out int minutes)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryReadHourMinute(object obj, string[] hourNames, string[] minuteNames, out int minutes)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryConvertToMinutes(object value, out int minutes)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryToInt(object value, out int i)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 36 private methods (regex-detected).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['BusinessLogicLayer/Generators/IScheduleGenerator.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IAvailabilityGroupService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IAvailabilityGroupService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IBaseService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IBaseService'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IBindService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IBindService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IContainerService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IContainerService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IEmployeeService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IEmployeeService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IScheduleEmployeeService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IScheduleEmployeeService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IScheduleService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IScheduleService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IScheduleSlotService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IScheduleSlotService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/Abstractions/IShopService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services.Abstractions'].
- Main declared type(s): ['interface IShopService'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/AvailabilityGroupService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class AvailabilityGroupService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<AvailabilityGroupModel>> GetByValueAsync(string value,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task SaveGroupAsync(AvailabilityGroupModel group,
            IList<(int employeeId, IList<AvailabilityGroupDayModel> days)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/BindService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class BindService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/ContainerService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class ContainerService'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/EmployeeService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class EmployeeService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['BusinessLogicLayer/Services/ScheduleEmployeeService.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/GenericService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class GenericService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<TEntity?> GetAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task UpdateAsync(TEntity entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task DeleteAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/ScheduleEmployeeService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class ScheduleEmployeeService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/ScheduleService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class ScheduleService'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System', 'System.Globalization', 'System.Text.RegularExpressions'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System', 'System.Globalization', 'System.Text.RegularExpressions'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<ScheduleModel> CreateAsync(ScheduleModel entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task UpdateAsync(ScheduleModel entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task SaveWithDetailsAsync(ScheduleModel schedule,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            IEnumerable<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private void NormalizeSchedule(ScheduleModel schedule)` — implementation in file; inspect branches/side-effects in source.
  - `private string NormalizeShift(string? value, string label)` — implementation in file; inspect branches/side-effects in source.
  - `private void EnsureGenerated(ScheduleModel schedule, IEnumerable<ScheduleSlotModel> slots)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/ScheduleSlotService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class ScheduleSlotService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## BusinessLogicLayer/Services/ShopService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: Business logic.

### Type Information
- File type: `.cs`.
- Namespace(s): ['BusinessLogicLayer.Services'].
- Main declared type(s): ['class ShopService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily business/domain.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: business/domain.
- BLL depends on DAL entities directly (thin domain abstraction).
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DATABASE_VIEW_NOTES.md
### Purpose
- Project documentation/note file capturing design decisions or usage notes.
- Supports: Solution/documentation.

### Type Information
- File type: `.md`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily n/a.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/DataAccessLayer.csproj
### Purpose
- Build/solution configuration file declaring projects, references, packages, and target frameworks.
- Supports: Data access.

### Type Information
- File type: `.csproj`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddNameToAvailabilityMonth'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddNameToAvailabilityMonth'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251114184904_AddNameToAvailabilityMonth.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddNameToAvailabilityMonth2'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddNameToAvailabilityMonth2'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251114185421_AddNameToAvailabilityMonth2.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251206194754_SlotTimes.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class SlotTimes'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251206194754_SlotTimes.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251206194754_SlotTimes.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class SlotTimes'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251206194754_SlotTimes.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251216172827_AddBind.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddBind'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251216172827_AddBind.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251216172827_AddBind.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddBind'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251216172827_AddBind.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class DropScheduleStatusAndComment'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class DropScheduleStatusAndComment'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251219185609_DropScheduleStatusAndComment.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddAvailabilityGroups'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddAvailabilityGroups'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251220111829_AddAvailabilityGroups.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class RemoveShopAndAvailabilityLegacy'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class RemoveShopAndAvailabilityLegacy'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20251229184256_RemoveShopAndAvailabilityLegacy.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class addNoteToSchedule'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class addNoteToSchedule'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260103192437_addNoteToSchedule.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260106132614_AddShopLogic.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddShopLogic'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260106132614_AddShopLogic.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260106132614_AddShopLogic.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddShopLogic'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260106132614_AddShopLogic.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260106140227_ShopLogicFix.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class ShopLogicFix'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260106140227_ShopLogicFix.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260106140227_ShopLogicFix.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class ShopLogicFix'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260106140227_ShopLogicFix.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260107120000_AddScheduleCellStyles.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddScheduleCellStyles'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddScheduleAvailabilityGroupId'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddScheduleAvailabilityGroupId'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260113190024_AddScheduleAvailabilityGroupId.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260113191835_Init2.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class Init2'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260113191835_Init2.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260113191835_Init2.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class Init2'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260113191835_Init2.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddUniqueIndexes'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AddUniqueIndexes'].

### Dependencies
- Key imports/usings: ['Microsoft.EntityFrameworkCore.Migrations'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.EntityFrameworkCore.Migrations'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260115120000_AddUniqueIndexes.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260115125626_databaseFix.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class databaseFix'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260115125626_databaseFix.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260115125626_databaseFix.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class databaseFix'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260115125626_databaseFix.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260116183224_cellPaintAdded.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class cellPaintAdded'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260116183224_cellPaintAdded.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260116183224_cellPaintAdded.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class cellPaintAdded'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260116183224_cellPaintAdded.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.Designer.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class FixMissingScheduleCellStyleTable'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Migrations', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildTargetModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class FixMissingScheduleCellStyleTable'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Up(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Down(MigrationBuilder migrationBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Migrations/20260220191954_FixMissingScheduleCellStyleTable.Designer.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs
### Purpose
- EF Core migration/snapshot file defining schema evolution operations and model metadata.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Migrations'].
- Main declared type(s): ['class AppDbContextModelSnapshot'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Infrastructure', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void BuildModel(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/AvailabilityGroupDayModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class AvailabilityGroupDayModel'].

### Dependencies
- Key imports/usings: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class AvailabilityGroupDayModel`
  - `public int Id`
  - `public int AvailabilityGroupMemberId`
  - `public AvailabilityGroupMemberModel AvailabilityGroupMember`
  - `public int DayOfMonth`
  - `public AvailabilityKind Kind`
  - `public string? IntervalStr`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/AvailabilityGroupMemberModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class AvailabilityGroupMemberModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class AvailabilityGroupMemberModel`
  - `public int Id`
  - `public int AvailabilityGroupId`
  - `public AvailabilityGroupModel AvailabilityGroup`
  - `public int EmployeeId`
  - `public EmployeeModel Employee`
  - `public ICollection<AvailabilityGroupDayModel> Days`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/AvailabilityGroupModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class AvailabilityGroupModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Text', 'System.Xml.Linq'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class AvailabilityGroupModel`
  - `public int Id`
  - `public string Name`
  - `public int Year`
  - `public int Month`
  - `public ICollection<AvailabilityGroupMemberModel> Members`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/BindModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class BindModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class BindModel`
  - `public int Id`
  - `public string Value`
  - `public string Key`
  - `public bool IsActive`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ContainerModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ContainerModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'Microsoft.EntityFrameworkCore'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'Microsoft.EntityFrameworkCore'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ContainerModel`
  - `public int Id`
  - `public string Name`
  - `public string? Note`
  - `public ICollection<ScheduleModel> Schedules`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/DataBaseContext/AppDbContext.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models.DataBaseContext'].
- Main declared type(s): ['class AppDbContext'].

### Dependencies
- Key imports/usings: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion', 'System', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.EntityFrameworkCore.Storage.ValueConversion', 'System', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public class AppDbContext(DbContextOptions<AppDbContext> options)` — implementation in file; inspect branches/side-effects in source.
  - `protected void OnModelCreating(ModelBuilder modelBuilder)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Models/DataBaseContext/AppDbContextFactory.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/DataBaseContext/AppDbContextFactory.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models.DataBaseContext'].
- Main declared type(s): ['class AppDbContextFactory'].

### Dependencies
- Key imports/usings: ['Microsoft.EntityFrameworkCore.Design', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.EntityFrameworkCore.Design', 'System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public AppDbContext CreateDbContext(string[] args)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/DataBaseContext/Extensions.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models.DataBaseContext'].
- Main declared type(s): ['class Extensions'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Repositories', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'Microsoft.Extensions.DependencyInjection', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Repositories', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'Microsoft.Extensions.DependencyInjection', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public IServiceCollection AddDataAccess(this IServiceCollection serviceCollection, string connectionString)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/EmployeeModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class EmployeeModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class EmployeeModel`
  - `public int Id`
  - `public string FirstName`
  - `public string LastName`
  - `public string? Phone`
  - `public string? Email`
  - `public ICollection<ScheduleEmployeeModel> ScheduleEmployees`
  - `public ICollection<ScheduleSlotModel> ScheduleSlots`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Models/ScheduleEmployeeModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/Enums/AvailabilityKind.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models.Enums'].
- Main declared type(s): ['enum AvailabilityKind'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public enum AvailabilityKind`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/Enums/SlotStatus.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models.Enums'].
- Main declared type(s): ['enum SlotStatus'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public enum SlotStatus`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ScheduleCellStyleModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ScheduleCellStyleModel'].

### Dependencies
- Key imports/usings: ['Microsoft.EntityFrameworkCore', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ScheduleCellStyleModel`
  - `public int Id`
  - `public int ScheduleId`
  - `public ScheduleModel Schedule`
  - `public int DayOfMonth`
  - `public int EmployeeId`
  - `public EmployeeModel Employee`
  - `public int? BackgroundColorArgb`
  - `public int? TextColorArgb`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ScheduleEmployeeModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ScheduleEmployeeModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ScheduleEmployeeModel`
  - `public int Id`
  - `public int ScheduleId`
  - `public ScheduleModel Schedule`
  - `public int EmployeeId`
  - `public EmployeeModel Employee`
  - `public int? MinHoursMonth`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ScheduleModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ScheduleModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Xml.Linq', 'DataAccessLayer.Models.Enums'].
- Internal dependencies: ['DataAccessLayer.Models.Enums'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.ComponentModel.DataAnnotations.Schema', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Xml.Linq'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ScheduleModel`
  - `public int Id`
  - `public int ContainerId`
  - `public ContainerModel Container`
  - `public int ShopId`
  - `public ShopModel Shop`
  - `public string Name`
  - `public int Year`
  - `public int Month`
  - `public int PeoplePerShift`
  - `public string Shift1Time`
  - `public string Shift2Time`
  - `public int MaxHoursPerEmpMonth`
  - `public int MaxConsecutiveDays`
  - `public int MaxConsecutiveFull`
  - `public int MaxFullPerMonth`
  - `public string? Note`
  - `public int? AvailabilityGroupId`
  - `public AvailabilityGroupModel? AvailabilityGroup`
  - `public ICollection<ScheduleEmployeeModel> Employees`
  - `public ICollection<ScheduleSlotModel> Slots`
  - `public ICollection<ScheduleCellStyleModel> CellStyles`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ScheduleSlotModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ScheduleSlotModel'].

### Dependencies
- Key imports/usings: ['System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'DataAccessLayer.Models.Enums'].
- Internal dependencies: ['DataAccessLayer.Models.Enums'].
- External dependencies: ['System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ScheduleSlotModel`
  - `public int Id`
  - `public int ScheduleId`
  - `public ScheduleModel Schedule`
  - `public int DayOfMonth`
  - `public int SlotNo`
  - `public int? EmployeeId`
  - `public EmployeeModel? Employee`
  - `public SlotStatus Status`
  - `public string FromTime`
  - `public string ToTime`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Models/ShopModel.cs
### Purpose
- Data/entity model used for persistence and transfer between layers.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Models'].
- Main declared type(s): ['class ShopModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.ComponentModel.DataAnnotations', 'System.ComponentModel.DataAnnotations.Schema', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Public/visible properties (detected):
  - `public class ShopModel`
  - `public int Id`
  - `public string Name`
  - `public string Address`
  - `public string? Description`
  - `public ICollection<ScheduleModel> Schedules`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs/outputs: structured domain/persistence data containers.
- Next stage: EF change tracking, repository queries, service DTO-like transfer.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupDayRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IAvailabilityGroupDayRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupMemberRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IAvailabilityGroupMemberRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IAvailabilityGroupRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IAvailabilityGroupRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IBaseRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IBaseRepository'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IBindRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IBindRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IContainerRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IContainerRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IEmployeeRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IEmployeeRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IScheduleCellStyleRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IScheduleCellStyleRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IScheduleEmployeeRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IScheduleEmployeeRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IScheduleRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IScheduleRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IScheduleSlotRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IScheduleSlotRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/Abstractions/IShopRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories.Abstractions'].
- Main declared type(s): ['interface IShopRepository'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/AvailabilityGroupDayRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class AvailabilityGroupDayRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<AvailabilityGroupDayModel>> GetByMemberIdAsync(int memberId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task DeleteByMemberIdAsync(int memberId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<List<AvailabilityGroupDayModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task AddRangeAsync(IEnumerable<AvailabilityGroupDayModel> days, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/AvailabilityGroupMemberRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class AvailabilityGroupMemberRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<AvailabilityGroupMemberModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<AvailabilityGroupMemberModel?> GetByGroupAndEmployeeAsync(int groupId, int employeeId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/AvailabilityGroupRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class AvailabilityGroupRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> ExistsByNameAsync(string name, int year, int month, int? excludeId = null, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/BindRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class BindRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ContainerRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ContainerRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/EmployeeRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class EmployeeRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> ExistsByNameAsync(string firstName,
            string lastName,
            int? excludeId = null,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> HasAvailabilityReferencesAsync(int employeeId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> HasScheduleReferencesAsync(int employeeId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['DataAccessLayer/Repositories/ScheduleEmployeeRepository.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/GenericRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class GenericRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Text', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task UpdateAsync(TEntity entity, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task DeleteAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SaveChangesOrResetAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ScheduleCellStyleRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ScheduleCellStyleRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ScheduleEmployeeRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ScheduleEmployeeRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ScheduleEmployeeModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ScheduleRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ScheduleRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ScheduleSlotRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ScheduleSlotRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ScheduleSlotModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## DataAccessLayer/Repositories/ShopRepository.cs
### Purpose
- Data access repository implementation/abstraction for CRUD and query logic.
- Supports: Data access.

### Type Information
- File type: `.cs`.
- Namespace(s): ['DataAccessLayer.Repositories'].
- Main declared type(s): ['class ShopRepository'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions', 'Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'DataAccessLayer.Repositories.Abstractions'].
- External dependencies: ['Microsoft.EntityFrameworkCore', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily DB/persistence.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<bool> HasScheduleReferencesAsync(int shopId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: repository method parameters and DbContext queryables.
- Outputs: entity lists/single models persisted or retrieved from SQLite.
- Next stage: consumed by BLL services and indirectly by UI ViewModels.

### Coupling / Layer Notes
- Layer classification: DB/persistence.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## GF3.sln
### Purpose
- Build/solution configuration file declaring projects, references, packages, and target frameworks.
- Supports: Solution/documentation.

### Type Information
- File type: `.sln`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily n/a.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## README.md
### Purpose
- Project documentation/note file capturing design decisions or usage notes.
- Supports: Solution/documentation.

### Type Information
- File type: `.md`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily n/a.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## SCHEDULE_GENERATION_FIX.md
### Purpose
- Project documentation/note file capturing design decisions or usage notes.
- Supports: Solution/documentation.

### Type Information
- File type: `.md`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily n/a.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ASYNC_OVERLAY_PATTERN.md
### Purpose
- Project documentation/note file capturing design decisions or usage notes.
- Supports: UI.

### Type Information
- File type: `.md`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/App.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/App.xaml.cs', 'WPFApp/WPFApp.csproj'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/App.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp'].
- Main declared type(s): ['class App'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.DataBaseContext', 'Microsoft.Extensions.DependencyInjection', 'Microsoft.Extensions.Hosting', 'System', 'System.IO', 'System.Windows', 'WPFApp.View', 'WPFApp.View.Availability', 'WPFApp.View.Employee', 'WPFApp.View.Shop', 'WPFApp.View.Home', 'WPFApp.View.Information'].
- Internal dependencies: ['DataAccessLayer.Models.DataBaseContext', 'WPFApp.View', 'WPFApp.View.Availability', 'WPFApp.View.Employee', 'WPFApp.View.Shop', 'WPFApp.View.Home', 'WPFApp.View.Information', 'WPFApp.ViewModel.Availability.Main', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Employee', 'WPFApp.ViewModel.Main', 'WPFApp.ViewModel.Shop'].
- External dependencies: ['Microsoft.Extensions.DependencyInjection', 'Microsoft.Extensions.Hosting', 'System', 'System.IO', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void ConfigureServices(IServiceCollection services)` — implementation in file; inspect branches/side-effects in source.
  - `protected void OnStartup(StartupEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `protected void OnExit(ExitEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/App.xaml', 'WPFApp/WPFApp.csproj'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/AssemblyInfo.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['System.Windows'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Controls/NumericUpDown.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['Value, ElementName=root, Mode=OneWay', 'IsReadOnly, ElementName=root'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/Controls/NumericUpDown.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Controls/NumericUpDown.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Controls'].
- Main declared type(s): ['class NumericUpDown'].

### Dependencies
- Key imports/usings: ['System.Globalization', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Data'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Globalization', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int Value`
  - `public int Minimum`
  - `public int Maximum`
  - `public int Step`
  - `public bool IsReadOnly`
- Methods/constructors (detected signatures):
  - `private void Up_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void Down_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBox_KeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void SyncTextWithValue()` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBox_LostFocus(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void CommitText()` — implementation in file; inspect branches/side-effects in source.
  - `private string GetProposedText(TextBox tb, string newText)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsTextAllowed(string text)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private object CoerceStep(DependencyObject d, object baseValue)` — implementation in file; inspect branches/side-effects in source.
  - `private object CoerceValue(DependencyObject d, object baseValue)` — implementation in file; inspect branches/side-effects in source.
  - `private int CoerceToRange(int value)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateButtonState()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 18 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/Controls/NumericUpDown.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/DataRowViewBoolColumnConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['class DataRowViewBoolColumnConverter'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Globalization', 'System.Windows.Data'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Data', 'System.Globalization', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/EmployeeTotalHoursHeaderConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['class EmployeeTotalHoursHeaderConverter'].

### Dependencies
- Key imports/usings: ['System.Collections.Concurrent', 'System.Globalization', 'System.Reflection', 'System.Text.RegularExpressions', 'System.Windows.Data'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Concurrent', 'System.Globalization', 'System.Reflection', 'System.Text.RegularExpressions', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/FirstValidationErrorConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['class FirstValidationErrorConverter'].

### Dependencies
- Key imports/usings: ['System.Collections', 'System.Collections.Generic', 'System.Globalization', 'System.Text', 'System.Windows.Controls', 'System.Windows.Data'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections', 'System.Collections.Generic', 'System.Globalization', 'System.Text', 'System.Windows.Controls', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/ScheduleMatrixCellBrushConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['enum ScheduleMatrixBrushKind', 'class ScheduleMatrixCellBrushConverter'].

### Dependencies
- Key imports/usings: ['System.Globalization', 'System.Windows', 'System.Windows.Data', 'System.Windows.Media', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System.Globalization', 'System.Windows', 'System.Windows.Data', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum ScheduleMatrixBrushKind`
  - `public ScheduleMatrixBrushKind Kind`
- Methods/constructors (detected signatures):
  - `public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/ScheduleMatrixCellReferenceConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['class ScheduleMatrixCellReferenceConverter'].

### Dependencies
- Key imports/usings: ['System', 'System.Globalization', 'System.Windows.Data', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Globalization', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Converters/ScheduleMatrixEmployeeTotalHoursConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Converters'].
- Main declared type(s): ['class ScheduleMatrixEmployeeTotalHoursConverter'].

### Dependencies
- Key imports/usings: ['System.Globalization', 'System.Windows.Data', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleEdit', 'WPFApp.ViewModel.Container.ScheduleProfile'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleEdit', 'WPFApp.ViewModel.Container.ScheduleProfile'].
- External dependencies: ['System.Globalization', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/AvailabilityEditViewDesignVM.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability.DesignTime'].
- Main declared type(s): ['class AvailabilityEditViewDesignVM'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Threading.Tasks', 'System.Windows.Input', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability', 'WPFApp.ViewModel.Availability.Helpers'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability', 'WPFApp.ViewModel.Availability.Helpers'].
- External dependencies: ['System.Data', 'System.Threading.Tasks', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ICommand CancelCommand`
  - `public ICommand CancelInformationCommand`
  - `public ICommand CancelEmployeeCommand`
  - `public ICommand CancelBindCommand`
  - `public ICommand SaveCommand`
  - `public ICommand SearchEmployeeCommand`
  - `public ICommand AddEmployeeCommand`
  - `public ICommand RemoveEmployeeCommand`
  - `public ICommand AddBindCommand`
  - `public ICommand DeleteBindCommand`
  - `public int AvailabilityId`
  - `public int AvailabilityMonth`
  - `public int AvailabilityYear`
  - `public string AvailabilityName`
  - `public string EmployeeSearchText`
  - `public ObservableCollection<EmployeeListItem> Employees`
  - `public EmployeeListItem? SelectedEmployee`
  - `public ObservableCollection<BindRow> Binds`
  - `public BindRow? SelectedBind`
  - `public object? SelectedAvailabilityDay`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/AvailabilityListDesignViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.DesignTime'].
- Main declared type(s): ['class AvailabilityListDesignRow', 'class AvailabilityListDesignViewModel'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public string Name`
  - `public ObservableCollection<AvailabilityListDesignRow> Items`
  - `public AvailabilityListDesignRow? SelectedItem`
  - `public string SearchText`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/AvailabilityProfileViewDesignVM.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability.DesignTime'].
- Main declared type(s): ['class AvailabilityProfileViewDesignVM', 'class AvailabilityProfileRow', 'class DesignCommand'].

### Dependencies
- Key imports/usings: ['System.Collections.ObjectModel', 'System.ComponentModel', 'System.Runtime.CompilerServices', 'System.Windows.Input'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.ObjectModel', 'System.ComponentModel', 'System.Runtime.CompilerServices', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int AvailabilityId`
  - `public string AvailabilityName`
  - `public string AvailabilityMonthYear`
  - `public ObservableCollection<AvailabilityProfileRow> ProfileAvailabilityMonths`
  - `public AvailabilityProfileRow? SelectedProfileMonth`
  - `public ICommand BackCommand`
  - `public ICommand CancelProfileCommand`
  - `public ICommand DeleteCommand`
  - `public ICommand CancelTableCommand`
  - `public ICommand EditCommand`
  - `public string Month`
  - `public string Week1`
  - `public string Week2`
  - `public string Week3`
  - `public string Week4`
  - `public string Notes`
- Methods/constructors (detected signatures):
  - `private void OnPropertyChanged([CallerMemberName] string? prop = null)` — implementation in file; inspect branches/side-effects in source.
  - `public bool CanExecute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Execute(object? parameter)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/ContainerProfileViewDesignVm.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.DesignTime'].
- Main declared type(s): ['class ContainerProfileViewDesignVm', 'class ContainerScheduleListDesignVm', 'class ScheduleItemDesignVm', 'class ShopDesignVm'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Text'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public class ContainerProfileViewDesignVm`
  - `public string ContainerId`
  - `public string Name`
  - `public string Note`
  - `public ContainerScheduleListDesignVm ScheduleListVm`
  - `public class ContainerScheduleListDesignVm`
  - `public bool IsMultiOpenEnabled`
  - `public string SearchText`
  - `public ObservableCollection<ScheduleItemDesignVm> Items`
  - `public ScheduleItemDesignVm SelectedItem`
  - `public class ScheduleItemDesignVm`
  - `public bool IsChecked`
  - `public string Name`
  - `public int Year`
  - `public int Month`
  - `public ShopDesignVm Shop`
  - `public class ShopDesignVm`
  - `public string Name`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/ContainerScheduleEditDesignVM.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.DesignTime'].
- Main declared type(s): ['class NotifyBase', 'class ContainerScheduleEditDesignVM', 'class ScheduleBlockVM', 'class ScheduleBlockModel', 'class ShopVM', 'class AvailabilityGroupVM', 'class EmployeeVM', 'class ScheduleEmployeeVM', 'class MatrixRowVM'].

### Dependencies
- Key imports/usings: ['System.Collections.ObjectModel', 'System.ComponentModel', 'System.Runtime.CompilerServices'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.ObjectModel', 'System.ComponentModel', 'System.Runtime.CompilerServices'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public string FormTitle`
  - `public string FormSubtitle`
  - `public string ScheduleId`
  - `public string ScheduleName`
  - `public int ScheduleMonth`
  - `public int ScheduleYear`
  - `public int SchedulePeoplePerShift`
  - `public string ScheduleShift1`
  - `public string ScheduleShift2`
  - `public int ScheduleMaxHoursPerEmp`
  - `public int ScheduleMaxConsecutiveDays`
  - `public int ScheduleMaxConsecutiveFull`
  - `public int ScheduleMaxFullPerMonth`
  - `public string ScheduleNote`
  - `public bool CanAddBlock`
  - `public ObservableCollection<ScheduleBlockVM> OpenedSchedules`
  - `public ScheduleBlockVM? ActiveSchedule`
  - `public ObservableCollection<ScheduleBlockVM> Blocks`
  - `public ScheduleBlockVM? SelectedBlock`
  - `public ObservableCollection<ShopVM> Shops`
  - `public ShopVM? SelectedShop`
  - `public string ShopSearchText`
  - `public ObservableCollection<AvailabilityGroupVM> AvailabilityGroups`
  - `public AvailabilityGroupVM? SelectedAvailabilityGroup`
  - `public string AvailabilitySearchText`
- Methods/constructors (detected signatures):
  - `protected void Raise([CallerMemberName] string? name = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/DesignTime/HomeViewDesignTime.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.DesignTime'].
- Main declared type(s): ['class HomeViewDesignTime', 'class WhoWorksTodayRow', 'class ActiveScheduleCard', 'class ActiveScheduleItemRow'].

### Dependencies
- Key imports/usings: ['System.Collections.ObjectModel'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.ObjectModel'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public class HomeViewDesignTime`
  - `public string CurrentTimeText`
  - `public ObservableCollection<WhoWorksTodayRow> WhoWorksTodayItems`
  - `public int MonthSchedulesCount`
  - `public int TotalContainersCount`
  - `public int TodayAssignmentsCount`
  - `public int ActiveShopsCount`
  - `public string StatusText`
  - `public ObservableCollection<ActiveScheduleCard> ActiveSchedules`
  - `public class WhoWorksTodayRow`
  - `public string Employee`
  - `public string Shift`
  - `public string Shop`
  - `public class ActiveScheduleCard`
  - `public string Title`
  - `public string Subtitle`
  - `public ObservableCollection<ActiveScheduleItemRow> Items`
  - `public class ActiveScheduleItemRow`
  - `public string Day`
  - `public string Employee`
  - `public string Shift`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/AsyncRelayCommand.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class AsyncRelayCommand'].

### Dependencies
- Key imports/usings: ['System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Input'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool CanExecute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Execute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Cancel()` — implementation in file; inspect branches/side-effects in source.
  - `public void RaiseCanExecuteChanged()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/AvailabilityMatrix/AvailabilityCellCodeParser.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.AvailabilityMatrix'].
- Main declared type(s): ['class AvailabilityCellCodeParser'].

### Dependencies
- Key imports/usings: ['WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool TryNormalize(string? raw, out string normalized, out string? error, bool allowOvernight = false)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/AvailabilityMatrix/AvailabilityMatrixEngine.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.AvailabilityMatrix'].
- Main declared type(s): ['class AvailabilityMatrixEngine'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Data', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['System.Collections.Generic', 'System.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string GetEmployeeColumnName(int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `public void EnsureDayColumn(DataTable table)` — implementation in file; inspect branches/side-effects in source.
  - `public void EnsureDayRowsForMonth(DataTable table, int year, int month)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryAddEmployeeColumn(DataTable table, int employeeId, string header, out string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `public bool RemoveEmployeeColumn(DataTable table, string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `public void RemoveAllEmployeeColumns(DataTable table)` — implementation in file; inspect branches/side-effects in source.
  - `public void Reset(DataTable table, bool regenerateDays, int year, int month)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetEmployeeCodes(DataTable table, string employeeColumnName, IEnumerable<(int dayOfMonth, string code)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryNormalizeCell(string? raw, out string normalized, out string? error)` — implementation in file; inspect branches/side-effects in source.
  - `public void NormalizeAndValidateAllCells(DataTable table)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/AvailabilityPreview/AvailabilityPreviewBuilder.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.AvailabilityPreview'].
- Main declared type(s): ['class AvailabilityPreviewBuilder'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'System', 'System.Collections.Generic', 'System.Threading'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ColorHelpers.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class ColorHelpers'].

### Dependencies
- Key imports/usings: ['System.Collections.Concurrent', 'System.Globalization', 'System.Windows.Media'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Concurrent', 'System.Globalization', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public int ToArgb(Color color)` — implementation in file; inspect branches/side-effects in source.
  - `public Color FromArgb(int argb)` — implementation in file; inspect branches/side-effects in source.
  - `public SolidColorBrush ToBrush(int argb)` — implementation in file; inspect branches/side-effects in source.
  - `public SolidColorBrush ToBrush(Color color)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearBrushCache()` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryParseHexColor(string? hex, out Color color)` — implementation in file; inspect branches/side-effects in source.
  - `public string ToHex(Color color, bool includeAlpha = true)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ExceptionMessageBuilder.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class ExceptionMessageBuilder'].

### Dependencies
- Key imports/usings: ['System.Diagnostics', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Diagnostics', 'System.Text'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void AppendException(StringBuilder sb,
            Exception ex,
            int depth,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Hotkeys/KeyGestureTextHelper.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Hotkeys'].
- Main declared type(s): ['class KeyGestureTextHelper'].

### Dependencies
- Key imports/usings: ['System.Globalization', 'System.Linq', 'System.Windows.Input'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Globalization', 'System.Linq', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? FormatKeyGesture(Key key, ModifierKeys modifiers, CultureInfo? culture = null)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryNormalizeKey(string raw, out string normalized, CultureInfo? culture = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ObservableObject.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class ObservableObject'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void Raise([CallerMemberName] string? propName = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/RelayCommand.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class RelayCommand', 'class RelayCommand'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Input'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool CanExecute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Execute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void RaiseCanExecuteChanged()` — implementation in file; inspect branches/side-effects in source.
  - `public bool CanExecute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Execute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void RaiseCanExecuteChanged()` — implementation in file; inspect branches/side-effects in source.
  - `private T? Cast(object? parameter)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/Infrastructure/AsyncRelayCommand.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ScheduleMatrix/ScheduleMatrixColumnBuilder.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.ScheduleMatrix'].
- Main declared type(s): ['class ScheduleMatrixColumnBuilder'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Shapes', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.ScheduleEdit'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.ScheduleEdit'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Shapes'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void BuildScheduleMatrixColumns(DataTable? table, DataGrid grid, bool isReadOnly)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ScheduleMatrix/ScheduleMatrixConstants.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['class ScheduleMatrixConstants'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ScheduleMatrix/ScheduleMatrixEngine.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.ScheduleMatrix'].
- Main declared type(s): ['class ScheduleMatrixEngine'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.Enums', 'System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Text', 'System.Threading'].
- Internal dependencies: ['DataAccessLayer.Models.Enums'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Text', 'System.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool TryParseTime(string? s, out TimeSpan t)` — implementation in file; inspect branches/side-effects in source.
  - `public DataTable BuildScheduleTable(int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public bool ComputeConflictForDay(IList<ScheduleSlotModel> slots, int day)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryParseIntervals(string? text, out List<(string from, string to)` — implementation in file; inspect branches/side-effects in source.
  - `public void ApplyIntervalsToSlots(int scheduleId,
            IList<ScheduleSlotModel> slots,
            int day,
            int empId,
            List<(string from, string to)` — implementation in file; inspect branches/side-effects in source.
  - `public bool ComputeConflictForDayWithStaffing(IList<ScheduleSlotModel> slots,
    int day,
    int peoplePerShift,
    string? shift1Range,
    string? shift2Range)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ScheduleMatrix/ScheduleTotalsCalculator.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.ScheduleMatrix'].
- Main declared type(s): ['class ScheduleTotalsCalculator', 'class TotalsResult'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['System', 'System.Collections.Generic'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int TotalEmployees`
  - `public TimeSpan TotalDuration`
- Methods/constructors (detected signatures):
  - `public TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees,
            IReadOnlyList<ScheduleSlotModel> slots)` — implementation in file; inspect branches/side-effects in source.
  - `public string FormatHoursMinutes(TimeSpan t)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Threading/UiDebouncedAction.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Threading'].
- Main declared type(s): ['class UiDebouncedAction'].

### Dependencies
- Key imports/usings: ['System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void Schedule(Action action)` — implementation in file; inspect branches/side-effects in source.
  - `public void Cancel()` — implementation in file; inspect branches/side-effects in source.
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.
  - `private Task RunAsync(CancellationTokenSource localCts, Action action)` — implementation in file; inspect branches/side-effects in source.
  - `private void CancelAndDispose(CancellationTokenSource? cts)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/AvailabilityValidationRules.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class AvailabilityValidationRules'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? ValidateProperty(string? name, int year, int month, string vmPropertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateName(string? name)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateMonth(int month)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateYear(int year)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddIfError(Dictionary<string, string> errors, string key, string? message)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/ContainerValidationRules.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class ContainerValidationRules'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? ValidateProperty(ContainerModel? model, string vmPropertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateName(string? name)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateNote(string? note)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddIfError(Dictionary<string, string> errors, string key, string? message)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/EmployeeValidationRules.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class EmployeeValidationRules'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text.RegularExpressions', 'DataAccessLayer.Models'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Text.RegularExpressions'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? ValidateProperty(EmployeeModel? model, string vmPropertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateFirstName(string? firstName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateLastName(string? lastName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateEmail(string? email)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidatePhone(string? phone)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddIfError(Dictionary<string, string> errors, string key, string? message)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 5 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/FirstValidationErrorConverter.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class FirstValidationErrorConverter'].

### Dependencies
- Key imports/usings: ['System.Globalization', 'System.Linq', 'System.Windows.Controls', 'System.Windows.Data'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Globalization', 'System.Linq', 'System.Windows.Controls', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.
  - `public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.
- Contains validation-related logic or rules.
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/MinHoursAtLeastOneRule.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class MinHoursAtLeastOneRule'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public ValidationResult Validate(object value, CultureInfo cultureInfo)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/ScheduleValidationRules.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class ScheduleValidationRules'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Globalization', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Globalization'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? ValidateProperty(ScheduleModel? model, string vmPropertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateShopId(int shopId)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateName(string? name)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateYear(int year)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateMonth(int month)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidatePeoplePerShift(int peoplePerShift)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateShift(string? shiftText, bool required, out TimeSpan? from, out TimeSpan? to)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateNonNegative(int value, string messageIfInvalid)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateNote(string? note)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IntervalsOverlap(TimeSpan aFrom, TimeSpan aTo, TimeSpan bFrom, TimeSpan bTo)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddIfError(Dictionary<string, string> errors, string key, string? errorMessage)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 10 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/ShopValidationRules.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class ShopValidationRules'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'DataAccessLayer.Models'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string? ValidateProperty(ShopModel? model, string vmPropertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateName(string? name)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateAddress(string? address)` — implementation in file; inspect branches/side-effects in source.
  - `private string? ValidateDescription(string? description)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddIfError(Dictionary<string, string> errors, string key, string? message)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/Validation/ValidationErrors.cs
### Purpose
- Cross-cutting infrastructure helper (commands, validation, threading, matrix calculation, parsing, etc.).
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure.Validation'].
- Main declared type(s): ['class ValidationErrors'].

### Dependencies
- Key imports/usings: ['System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearAll()` — implementation in file; inspect branches/side-effects in source.
  - `public void Clear(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void Add(string propertyName, string message)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetMany(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Infrastructure/ViewModelBase.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Infrastructure'].
- Main declared type(s): ['class ViewModelBase'].

### Dependencies
- Key imports/usings: ['System.Collections.Concurrent', 'System.Collections.Generic', 'System.ComponentModel', 'System.Runtime.CompilerServices', 'System.Windows'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Concurrent', 'System.Collections.Generic', 'System.ComponentModel', 'System.Runtime.CompilerServices', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)` — implementation in file; inspect branches/side-effects in source.
  - `protected void OnPropertiesChanged(params string[] propertyNames)` — implementation in file; inspect branches/side-effects in source.
  - `private PropertyChangedEventArgs GetArgs(string? propertyName)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Availability/AvailabilityResources.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Container/ContainerScheduleResources.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['IsChecked, ElementName=DetailsToggle', 'IsChecked, ElementName=EmployeesToggle', 'IsChecked, ElementName=NoteToggle', 'IsChecked, ElementName=ScheduleInfoToggle', 'IsChecked, ElementName=ScheduleInfoToggle', 'IsChecked, ElementName=ScheduleInfoToggle', 'IsChecked, ElementName=ScheduleInfoToggle', 'ElementName=SummaryBodyScroll, Path=ComputedVerticalScrollBarVisibility', 'IsChecked, ElementName=DetailsToggle', 'IsChecked, ElementName=EmployeesToggle', 'IsChecked, ElementName=NoteToggle', 'IsChecked, ElementName=DetailsToggle', 'IsChecked, ElementName=EmployeesToggle', 'IsChecked, ElementName=NoteToggle'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Excel/ContainerTemplate.xlsx
### Purpose
- Binary Excel template resource copied to output for export features.
### Type Information
- File type: Excel workbook template (`.xlsx`).
- Namespace/Main types: N/A (binary asset).
### Dependencies
- Used by WPF export services via file paths in resources.
### Public API / Members
- N/A.
### Internal Logic Details
- N/A (not source code).
### Data Flow
- Template loaded by export routine, populated with schedule/container data, written to user-selected output files.
### Coupling / Layer Notes
- UI infrastructure resource; tightly coupled to export format.
### Related Files
- `WPFApp/Service/ScheduleExportService.cs`, `WPFApp/WPFApp.csproj`.
### Summary
- Static resource file.
- Required for Excel export layout.
- Versioning changes affect exported document format.

## WPFApp/Resources/Excel/ScheduleTemplate.xlsx
### Purpose
- Binary Excel template resource copied to output for export features.
### Type Information
- File type: Excel workbook template (`.xlsx`).
- Namespace/Main types: N/A (binary asset).
### Dependencies
- Used by WPF export services via file paths in resources.
### Public API / Members
- N/A.
### Internal Logic Details
- N/A (not source code).
### Data Flow
- Template loaded by export routine, populated with schedule/container data, written to user-selected output files.
### Coupling / Layer Notes
- UI infrastructure resource; tightly coupled to export format.
### Related Files
- `WPFApp/Service/ScheduleExportService.cs`, `WPFApp/WPFApp.csproj`.
### Summary
- Static resource file.
- Required for Excel export layout.
- Versioning changes affect exported document format.

## WPFApp/Resources/Styles/Buttons.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Styles/DataGrids.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource Self', 'IsChecked', 'RelativeSource={RelativeSource AncestorType=DataGridCell', 'RelativeSource={RelativeSource AncestorType=DataGridCell', 'RelativeSource={RelativeSource Self', 'IsChecked', 'RelativeSource={RelativeSource AncestorType=DataGridRow', 'ElementName=HoursLine, Path=Text'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Styles/Icons.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Styles/ScrollBars.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource TemplatedParent', 'RelativeSource={RelativeSource TemplatedParent', 'RelativeSource={RelativeSource TemplatedParent', 'RelativeSource={RelativeSource TemplatedParent'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Styles/TextBoxes.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource Self', 'IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent', 'RelativeSource={RelativeSource TemplatedParent', 'ActualWidth, RelativeSource={RelativeSource TemplatedParent', 'RelativeSource={RelativeSource Self', 'RelativeSource={RelativeSource Self', 'RelativeSource={RelativeSource Self'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/Styles/Theme.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Resources/ViewTemplates.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/ColorPickerService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['class ColorPickerService'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Media', 'WPFApp.View.Dialogs'].
- Internal dependencies: ['WPFApp.View.Dialogs'].
- External dependencies: ['System.Windows', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool TryPickColor(Color? initialColor, out Color color)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/Service/IColorPickerService.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/CustomMessageBox.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['class CustomMessageBox'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text', 'System.Windows', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Collections.Generic', 'System.Text', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public bool Show(string title, string message,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info,
            string okText = "OK", string cancelText = "", string? details = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/DatabaseChangeNotifier.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['class DatabaseChangedEventArgs', 'interface IDatabaseChangeNotifier', 'class DatabaseChangeNotifier'].

### Dependencies
- Key imports/usings: ['System', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public DateTime ChangedAtUtc`
  - `public string Source`
  - `public interface IDatabaseChangeNotifier`
- Methods/constructors (detected signatures):
  - `public void NotifyDatabaseChanged(string source)` — implementation in file; inspect branches/side-effects in source.
  - `private Task PublishDebouncedAsync(string source, CancellationTokenSource cts)` — implementation in file; inspect branches/side-effects in source.
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/DatabasePathProvider.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['interface IDatabasePathProvider', 'class DatabasePathProvider'].

### Dependencies
- Key imports/usings: ['System', 'System.IO'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.IO'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public interface IDatabasePathProvider`
  - `public string RootDirectory`
  - `public string DatabaseFilePath`
  - `public string ConnectionString`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/IColorPickerService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['interface IColorPickerService'].

### Dependencies
- Key imports/usings: ['System.Windows.Media'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public interface IColorPickerService`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/IScheduleExportService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['interface IScheduleExportService'].

### Dependencies
- Key imports/usings: ['System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public interface IScheduleExportService`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/DiagnosticsServiceImpl.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['interface DiagnosticsService', 'class DiagnosticsServiceImpl'].

### Dependencies
- Key imports/usings: ['System', 'System.Globalization', 'System.IO', 'System.Text', 'System.Threading'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Globalization', 'System.IO', 'System.Text', 'System.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public interface DiagnosticsService`
- Methods/constructors (detected signatures):
  - `public void Log(string message)` — implementation in file; inspect branches/side-effects in source.
  - `public void PerfRecord(string area, string message)` — implementation in file; inspect branches/side-effects in source.
  - `private void WriteLine(string level, string message)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/ScheduleExportService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['class ScheduleExportContext', 'class AvailabilityGroupExportData', 'class ContainerExcelExportChartContext', 'class ContainerExcelExportContext', 'class ContainerSqlExportScheduleContext', 'class ContainerSqlExportContext', 'class ScheduleSqlExportContext', 'class ScheduleExportService'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.IO', 'System.Linq', 'System.Text', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers', 'WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleProfile'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.IO', 'System.Linq', 'System.Text', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public string ScheduleName`
  - `public int ScheduleMonth`
  - `public int ScheduleYear`
  - `public string ShopName`
  - `public string ShopAddress`
  - `public string TotalHoursText`
  - `public int TotalEmployees`
  - `public int TotalDays`
  - `public string Shift1`
  - `public string Shift2`
  - `public string TotalEmployeesListText`
  - `public DataView ScheduleMatrix`
  - `public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryDayHeader> SummaryDayHeaders`
  - `public IReadOnlyList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> SummaryRows`
  - `public IReadOnlyList<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> EmployeeWorkFreeStats`
  - `public IScheduleMatrixStyleProvider StyleProvider`
  - `public AvailabilityGroupModel Group`
  - `public IReadOnlyList<AvailabilityGroupMemberModel> Members`
  - `public IReadOnlyList<AvailabilityGroupDayModel> Days`
  - `public string ChartName`
  - `public ScheduleExportContext ScheduleContext`
  - `public int ContainerId`
  - `public string ContainerName`
  - `public string ContainerNote`
  - `public int TotalEmployees`
- Methods/constructors (detected signatures):
  - `public Task ExportToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task ExportToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private string ResolveExcelTemplatePath()` — implementation in file; inspect branches/side-effects in source.
  - `private string ResolveContainerTemplatePath()` — implementation in file; inspect branches/side-effects in source.
  - `private IXLWorksheet? FindTemplateSheet(XLWorkbook wb, IEnumerable<string> names)` — implementation in file; inspect branches/side-effects in source.
  - `private string MakeUniqueSheetName(XLWorkbook wb, string baseName)` — implementation in file; inspect branches/side-effects in source.
  - `public string SanitizeFileName(string? name, string fallback)` — implementation in file; inspect branches/side-effects in source.
  - `private string SanitizeWorksheetName(string? name, string fallback)` — implementation in file; inspect branches/side-effects in source.
  - `private void FixGray125Fills(IXLWorksheet sheet, string? rangeAddress = null)` — implementation in file; inspect branches/side-effects in source.
  - `private void FillMatrixSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private void SetDashIfEmpty(IXLCell cell, object? value)` — implementation in file; inspect branches/side-effects in source.
  - `private DateTime? TryBuildDate(ScheduleExportContext context, DataColumn? dayCol, DataRowView rowView)` — implementation in file; inspect branches/side-effects in source.
  - `private DateTime? SafeDate(int year, int month, int day)` — implementation in file; inspect branches/side-effects in source.
  - `private void SetExcelValuePreservingFormat(IXLCell cell, object? value)` — implementation in file; inspect branches/side-effects in source.
  - `private void FillStatisticSheetFromTemplate(IXLWorksheet sheet, ScheduleExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private void FillContainerTemplateSheetFromTemplate(IXLWorksheet sheet, ContainerExcelExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildContainerSqlScript(ContainerSqlExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildSqlScript(ScheduleSqlExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private List<EmployeeModel> CollectEmployees(ScheduleSqlExportContext context)` — implementation in file; inspect branches/side-effects in source.
  - `private string SqlInsert(string table, params (string Column, object? Value)` — implementation in file; inspect branches/side-effects in source.
  - `private string ToSqlLiteral(object? value)` — implementation in file; inspect branches/side-effects in source.
  - `private string EscapeSqlString(string value)` — implementation in file; inspect branches/side-effects in source.
  - `private bool GetBoolValue(DataRowView rowView, string columnName)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 20 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/Service/IScheduleExportService.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/Service/SqliteAdminService.cs
### Purpose
- Service class/interface coordinating business operations or app infrastructure.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.Service'].
- Main declared type(s): ['class SqlExecutionResult', 'class DatabaseInfo', 'interface ISqliteAdminService', 'class SqliteAdminService'].

### Dependencies
- Key imports/usings: ['Microsoft.Data.Sqlite', 'System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.IO', 'System.Security.Cryptography', 'System.Text', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Microsoft.Data.Sqlite', 'System', 'System.Collections.Generic', 'System.Data', 'System.Globalization', 'System.IO', 'System.Security.Cryptography', 'System.Text', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public bool IsSelect`
  - `public DataTable? ResultTable`
  - `public int AffectedRows`
  - `public string Message`
  - `public string DatabasePath`
  - `public long FileSizeBytes`
  - `public DateTime LastModifiedUtc`
  - `public int UserVersion`
  - `public IReadOnlyList<string> Tables`
  - `public interface ISqliteAdminService`
- Methods/constructors (detected signatures):
  - `public Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `public Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `public Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityEditView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'IsChecked, ElementName=InfoToggle, Converter={StaticResource BoolToVis', 'AvailabilityId', 'AvailabilityMonth, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged', 'AvailabilityYear, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged', 'AvailabilityName, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True,  NotifyOnValidationError=True', 'CancelInformationCommand', 'IsChecked, ElementName=EmployeeToggle, Converter={StaticResource BoolToVis', 'EmployeeSearchText, UpdateSourceTrigger=PropertyChanged', 'SearchEmployeeCommand', 'SelectedEmployeeId', 'Employees', 'SelectedEmployee', 'CancelEmployeeCommand', 'RemoveEmployeeCommand', 'AddEmployeeCommand', 'IsChecked, ElementName=BindToggle, Converter={StaticResource BoolToVis', 'Binds'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityEditView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityEditView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability'].
- Main declared type(s): ['class AvailabilityEditView'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Input', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.View.Availability.Helpers', 'WPFApp.ViewModel.Availability.Edit', 'WPFApp.ViewModel.Availability.Helpers'].
- Internal dependencies: ['WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.View.Availability.Helpers', 'WPFApp.ViewModel.Availability.Edit', 'WPFApp.ViewModel.Availability.Helpers'].
- External dependencies: ['System.Data', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnLoaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnUnloaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AttachViewModel(AvailabilityEditViewModel? viewModel)` — implementation in file; inspect branches/side-effects in source.
  - `private void DetachViewModel()` — implementation in file; inspect branches/side-effects in source.
  - `private void VmOnMatrixChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TextBoxSearchValueFromAvailabilityEdit_KeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridBinds_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridBinds_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridBinds_PreviewKeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridAvailabilityDays_PreviewKeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsCurrentCellEditing(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void AvailabilityGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 13 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityEditView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityListView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['SearchCommand', 'SearchText, UpdateSourceTrigger=PropertyChanged', 'SearchCommand', 'AddNewCommand', 'Items.Count, StringFormat=Total: {0', 'Items', 'SelectedItem', 'Name'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityListView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityListView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability'].
- Main declared type(s): ['class AvailabilityListView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Controls.Primitives', 'WPFApp.ViewModel.Availability.List'].
- Internal dependencies: ['WPFApp.ViewModel.Availability.List'].
- External dependencies: ['System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Controls.Primitives'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void InputSearch_KeyDown(object sender, KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridAvailabilityList_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void TryOpenProfileFromUi()` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 5 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityListView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityProfileView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['BackCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'AvailabilityId', 'AvailabilityName', 'AvailabilityMonthYear', 'CancelProfileCommand', 'DeleteCommand', 'ProfileAvailabilityMonths', 'CancelTableCommand', 'EditCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityProfileView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityProfileView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability'].
- Main declared type(s): ['class AvailabilityProfileView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'WPFApp.View.Availability.Helpers', 'WPFApp.ViewModel.Availability.Profile'].
- Internal dependencies: ['WPFApp.View.Availability.Helpers', 'WPFApp.ViewModel.Availability.Profile'].
- External dependencies: ['System.Windows', 'System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnLoaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnUnloaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AttachViewModel(AvailabilityProfileViewModel? viewModel)` — implementation in file; inspect branches/side-effects in source.
  - `private void DetachViewModel()` — implementation in file; inspect branches/side-effects in source.
  - `private void VmOnMatrixChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 6 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityProfileView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CurrentSection', 'IsNavStatusVisible, Converter={StaticResource BoolToVis', 'NavStatus', 'IsNavStatusVisible, Converter={StaticResource BoolToVis'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Availability/AvailabilityView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability'].
- Main declared type(s): ['class AvailabilityView'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Text', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Availability/AvailabilityView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerEditView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'FormTitle', 'FormSubtitle', 'ContainerId', 'Name, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=NameTextBox', 'IsKeyboardFocused, ElementName=NameTextBox', 'Note, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=NoteTextBox', 'IsKeyboardFocused, ElementName=NoteTextBox', 'CancelCommand', 'SaveCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerEditView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerEditView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Container'].
- Main declared type(s): ['class ContainerEditView'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerEditView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerListView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['SearchCommand', 'SearchText, UpdateSourceTrigger=PropertyChanged', 'SearchCommand', 'AddNewCommand', 'Items.Count, StringFormat=Total: {0', 'Items', 'SelectedItem', 'Name', 'Note'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerListView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerListView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Container'].
- Main declared type(s): ['class ContainerListView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media', 'WPFApp.ViewModel.Container.List'].
- Internal dependencies: ['WPFApp.ViewModel.Container.List'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void DataGridContainerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerListView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerProfileView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'DataContext.IsMultiOpenEnabled, RelativeSource={RelativeSource AncestorType=Button', 'DataContext.IsMultiOpenEnabled, RelativeSource={RelativeSource Self', 'BackCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ExportToExcelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ExportToCodeCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ContainerId', 'Name', 'Note', 'CancelProfileCommand', 'EditCommand', 'DeleteCommand', 'ScheduleListVm', 'Items.Count, StringFormat=Total: {0'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerProfileView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerProfileView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Container'].
- Main declared type(s): ['class ContainerProfileView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Threading', 'WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleList'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleList'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void AttachVm(ContainerProfileViewModel? vm)` — implementation in file; inspect branches/side-effects in source.
  - `private void DetachVm()` — implementation in file; inspect branches/side-effects in source.
  - `private void VmOnStatisticsChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void QueueRebuildStatsColumns()` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Style BlankTextWhenTotalsRow()` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridSchedules_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void DataGridSchedules_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 10 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerProfileView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerScheduleEditView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'FormTitle', 'FormSubtitle', 'OpenedSchedules.Count, Mode=OneWay', 'SaveCommand', 'OpenedSchedules', 'ActiveSchedule, Mode=OneWay', 'Model.Name', 'IsSelected, RelativeSource={RelativeSource AncestorType=ListBoxItem', 'IsMouseOver, RelativeSource={RelativeSource AncestorType=ListBoxItem', 'AddBlockCommand', 'CanAddBlock', 'IsChecked, ElementName=DetailsToggle, Converter={StaticResource BoolToVis', 'ScheduleId', 'ScheduleName, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True, Mode=TwoWay', 'SchedulePeoplePerShift, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'ScheduleMonth, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'ScheduleYear, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerScheduleEditView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerScheduleEditView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Container'].
- Main declared type(s): ['class ContainerScheduleEditView'].

### Dependencies
- Key imports/usings: ['System.Collections', 'System.Collections.Generic', 'System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Controls.Primitives', 'System.Windows.Data', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Threading', 'WPFApp.Controls'].
- Internal dependencies: ['WPFApp.Controls', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.Service', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.Edit.Helpers', 'WPFApp.ViewModel.Container.ScheduleEdit', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Collections', 'System.Collections.Generic', 'System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Controls.Primitives', 'System.Windows.Data', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void ContainerScheduleEditView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ContainerScheduleEditView_Loaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ContainerScheduleEditView_Unloaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AttachViewModel(ContainerScheduleEditViewModel? viewModel)` — implementation in file; inspect branches/side-effects in source.
  - `private void DetachViewModel()` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetInteractionState(bool commitPendingSelections)` — implementation in file; inspect branches/side-effects in source.
  - `private void ConfigureGridPerformance(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetGridColumns(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void HardResetGrid(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void SwapItemsSource(DataGrid grid, IEnumerable? source)` — implementation in file; inspect branches/side-effects in source.
  - `private void VmOnMatrixChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RefreshMatricesSmart()` — implementation in file; inspect branches/side-effects in source.
  - `private int BuildSchemaHash(DataTable table)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_CurrentCellChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_MouseMove(object sender, MouseEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetRowIndexUnderMouse(MouseEventArgs e, out int rowIndex)` — implementation in file; inspect branches/side-effects in source.
  - `private void SelectWholeRowCells(object rowItem, bool keepExistingSelection)` — implementation in file; inspect branches/side-effects in source.
  - `private void SelectWholeRowsRange(int a, int b, bool keepExistingSelection)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetCellReference(DataGridCell cell, out ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void OpenedSchedulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleBlockSelect_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleBlockClose_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void LookupComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void EmployeeComboBox_DropDownOpened(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void EmployeeComboBox_DropDownClosed(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ShopComboBox_DropDownOpened(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ShopComboBox_DropDownClosed(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AvailabilityComboBox_DropDownOpened(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AvailabilityComboBox_DropDownClosed(object sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 39 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerScheduleEditView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerScheduleProfileView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['BackCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ExportToExcelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ExportToSQLiteCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'IsChecked, ElementName=ScheduleInfoToggle, Converter={StaticResource BoolToVis', 'ScheduleId', 'ScheduleName', 'ScheduleMonthYear', 'ShopName', 'Note', 'IsChecked, ElementName=ScheduleInfoToggle, Converter={StaticResource BoolToVis', 'CancelProfileCommand', 'EditCommand', 'DeleteCommand', 'TotalEmployees, Mode=OneWay', 'TotalHoursText, Mode=OneWay', 'ScheduleMatrix'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerScheduleProfileView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerScheduleProfileView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Container'].
- Main declared type(s): ['class ContainerScheduleProfileView'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Threading', 'WPFApp.ViewModel.Container.ScheduleProfile', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.ViewModel.Container.ScheduleProfile', 'WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void ConfigureGridPerformance(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void ContainerScheduleProfileView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ContainerScheduleProfileView_Loaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ContainerScheduleProfileView_Unloaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void AttachViewModel(ContainerScheduleProfileViewModel? viewModel)` — implementation in file; inspect branches/side-effects in source.
  - `private void DetachViewModel()` — implementation in file; inspect branches/side-effects in source.
  - `private void VmOnMatrixChanged(object? sender, EventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetGridColumns(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void RefreshGridSmart()` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryBodyScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryHeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryLeftScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryHScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSummaryHorizontalBar()` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryBodyScroll_Loaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void SummaryBodyScroll_SizeChanged(object sender, SizeChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ForwardWheelToSummaryBody(MouseWheelEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 17 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerScheduleProfileView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CurrentSection', 'IsNavStatusVisible, Converter={StaticResource BoolToVis', 'NavStatus', 'IsNavStatusVisible, Converter={StaticResource BoolToVis'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Container/ContainerView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View'].
- Main declared type(s): ['class ContainerView'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Text', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Container/ContainerView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/DatabaseView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['ExecuteSqlCommand', 'ExecutorSql, UpdateSourceTrigger=PropertyChanged', 'ClearExecutorOutputCommand', 'ExecutorOutput', 'HasQueryResult, Converter={StaticResource BoolToVis', 'QueryResult', 'ChooseImportFileCommand', 'ImportFilePath', 'ImportFileSize', 'ImportFileModified', 'ImportFileHash', 'ImportScript, UpdateSourceTrigger=PropertyChanged', 'ExecuteImportScriptCommand', 'ImportOutput', 'RefreshInfoCommand', 'DatabasePath', 'DatabaseSize', 'DatabaseLastUpdated'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/DatabaseView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/DatabaseView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View'].
- Main declared type(s): ['class DatabaseView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/DatabaseView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/ColorPickerDialog.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['Swatches', 'Brush', 'Color', 'Hex'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/ColorPickerDialog.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/ColorPickerDialog.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Dialogs'].
- Main declared type(s): ['class ColorPickerDialog', 'class ColorSwatch'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media', 'WPFApp.Infrastructure'].
- Internal dependencies: ['WPFApp.Infrastructure'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<ColorSwatch> Swatches`
  - `public Color SelectedColor`
  - `public Color Color`
  - `public SolidColorBrush Brush`
  - `public string Hex`
- Methods/constructors (detected signatures):
  - `private void InitializeSwatches()` — implementation in file; inspect branches/side-effects in source.
  - `private void SwatchButton_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void CustomButton_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void CancelButton_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/ColorPickerDialog.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/CustomMessageBoxView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['IconGeometry', 'Title', 'CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Message', 'HasDetails, Converter={StaticResource BoolToVis', 'Details', 'CancelText', 'CancelCommand', 'IsCancelVisible, Converter={StaticResource BoolToVis', 'OkText', 'OkCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/CustomMessageBoxView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/CustomMessageBoxView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Dialogs'].
- Main declared type(s): ['class CustomMessageBoxView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void CopyDetails_Click(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/CustomMessageBoxView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/UIStatus.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/UIStatus.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Dialogs/UIStatus.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Dialogs'].
- Main declared type(s): ['enum UIStatusKind', 'class UIStatus'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls', 'System.Windows.Media'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum UIStatusKind`
  - `public UIStatusKind Status`
  - `public string? Message`
- Methods/constructors (detected signatures):
  - `private void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyStatusVisuals()` — implementation in file; inspect branches/side-effects in source.
  - `private SolidColorBrush BrushFrom(string hex)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Dialogs/UIStatus.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeEditView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource Self', 'CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'FormTitle', 'FormSubtitle', 'EmployeeId', 'FirstName, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=FirstNameTextBox', 'IsKeyboardFocused, ElementName=FirstNameTextBox', 'LastName, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=LastNameTextBox', 'IsKeyboardFocused, ElementName=LastNameTextBox', 'Email, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=EmailTextBox', 'IsKeyboardFocused, ElementName=EmailTextBox', 'Phone, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=PhoneTextBox', 'IsKeyboardFocused, ElementName=PhoneTextBox', 'CancelCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeEditView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeEditView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Employee'].
- Main declared type(s): ['class EmployeeEditView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeEditView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeListView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['SearchCommand', 'SearchText, UpdateSourceTrigger=PropertyChanged', 'SearchCommand', 'AddNewCommand', 'Items.Count, StringFormat=Total: {0', 'Items', 'SelectedItem', 'FirstName', 'LastName'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeListView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeListView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Employee'].
- Main declared type(s): ['class EmployeeListView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media', 'WPFApp.ViewModel.Employee'].
- Internal dependencies: ['WPFApp.ViewModel.Employee'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void DataGridEmployeeList_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeListView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeProfileView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['BackCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'EmployeeId', 'FullName', 'Email', 'Phone', 'CancelProfileCommand', 'EditCommand', 'DeleteCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeProfileView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeProfileView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Employee'].
- Main declared type(s): ['class EmployeeProfileView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeProfileView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CurrentSection', 'IsNavStatusVisible, Converter={StaticResource BoolToVis', 'NavStatus', 'IsNavStatusVisible, Converter={StaticResource BoolToVis'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Employee/EmployeeView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Employee'].
- Main declared type(s): ['class EmployeeView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Employee/EmployeeView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Helpers/AvailabilityMatrixGridBuilder.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability.Helpers'].
- Main declared type(s): ['class AvailabilityMatrixGridBuilder'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'WPFApp.Infrastructure.AvailabilityMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.AvailabilityMatrix'].
- External dependencies: ['System.Data', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Data'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void BuildEditable(DataTable? table, DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `public void BuildReadOnly(DataTable? table, DataGrid grid)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Helpers/AvailabilityViewInputHelper.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Availability.Helpers'].
- Main declared type(s): ['class AvailabilityViewInputHelper'].

### Dependencies
- Key imports/usings: ['System.Linq', 'System.Windows.Input'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Linq', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string KeyToBindToken(Key key)` — implementation in file; inspect branches/side-effects in source.
  - `public bool IsAllDigits(string? text)` — implementation in file; inspect branches/side-effects in source.
  - `public bool IsCommonEditorShortcut(Key key, ModifierKeys modifiers)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Home/HomeView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource AncestorType=ItemsControl', 'CurrentTimeText', 'RefreshCommand', 'ElementName=RightInfoColumn, Path=ActualHeight', 'WhoWorksTodayItems', 'Date, StringFormat=dd.MM.yyyy', 'Employee', 'Shift', 'Shop', 'CurrentMonthLabel', 'CurrentMonthContainerName', 'CurrentMonthTotalHours', 'CurrentMonthTotalEmployees', 'CurrentMonthTotalSchedules', 'CurrentMonthTotalShops', 'CurrentMonthScheduleNames', 'CurrentMonthShopNames', 'CurrentMonthEmployeeNames', 'OverallTotalEmployees', 'OverallTotalContainers'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Home/HomeView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Home/HomeView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Home'].
- Main declared type(s): ['class HomeView'].

### Dependencies
- Key imports/usings: ['System', 'System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Controls.Primitives', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.ViewModel.Home'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.ViewModel.Home'].
- External dependencies: ['System', 'System.Data', 'System.Linq', 'System.Windows', 'System.Windows.Controls', 'System.Windows.Controls.Primitives'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void ConfigureGridPerformance(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.
  - `private void ActiveScheduleMatrixGrid_Loaded(object sender, RoutedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RefreshGridSmart(DataGrid grid)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Home/HomeView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Information/InformationView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['No direct Binding markup found'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Information/InformationView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Information/InformationView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Information'].
- Main declared type(s): ['class InformationView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Information/InformationView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/MainWindow.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['ShowHomeCommand', 'IsHomeEnabled', 'ShowEmployeeCommand', 'IsEmployeeEnabled', 'ShowShopCommand', 'IsShopEnabled', 'ShowAvailabilityCommand', 'IsAvailabilityEnabled', 'ShowContainerCommand', 'IsContainerEnabled', 'ShowInformationCommand', 'IsInformationEnabled', 'ShowDatabaseCommand', 'IsDatabaseEnabled', 'MinimizeCommand', 'CloseCommand', 'CurrentViewModel'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/MainWindow.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/MainWindow.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View'].
- Main declared type(s): ['class MainWindow'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes', 'WPFApp.ViewModel.Main'].
- Internal dependencies: ['WPFApp.ViewModel.Main'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Data', 'System.Windows.Documents', 'System.Windows.Input', 'System.Windows.Media', 'System.Windows.Media.Imaging', 'System.Windows.Navigation', 'System.Windows.Shapes'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/MainWindow.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopEditView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['RelativeSource={RelativeSource Self', 'CancelCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'FormTitle', 'FormSubtitle', 'ShopId', 'Name, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=NameTextBox', 'IsKeyboardFocused, ElementName=NameTextBox', 'Address, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=AddressTextBox', 'IsKeyboardFocused, ElementName=AddressTextBox', 'Description, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True', 'Text, ElementName=DescriptionTextBox', 'IsKeyboardFocused, ElementName=DescriptionTextBox', 'CancelCommand', 'SaveCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopEditView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopEditView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Shop'].
- Main declared type(s): ['class ShopEditView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopEditView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopListView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['SearchCommand', 'SearchText, UpdateSourceTrigger=PropertyChanged', 'SearchCommand', 'AddNewCommand', 'Items.Count, StringFormat=Total: {0', 'Items', 'SelectedItem', 'Name', 'Address'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopListView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopListView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Shop'].
- Main declared type(s): ['class ShopListView'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media', 'WPFApp.ViewModel.Shop'].
- Internal dependencies: ['WPFApp.ViewModel.Shop'].
- External dependencies: ['System.Windows', 'System.Windows.Controls', 'System.Windows.Input', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void DataGridShopList_MouseDoubleClick(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void RowHitArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopListView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopProfileView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['BackCommand', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'Foreground, RelativeSource={RelativeSource AncestorType=Button', 'ShopId', 'Name', 'Address', 'Description', 'CancelProfileCommand', 'EditCommand', 'DeleteCommand'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopProfileView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopProfileView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Shop'].
- Main declared type(s): ['class ShopProfileView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopProfileView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopView.xaml
### Purpose
- WPF XAML view/resource dictionary defining visual tree, bindings, styles, templates, or converters.
- Supports: UI.

### Type Information
- File type: `.xaml`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- XAML bindings (sample): ['CurrentSection', 'IsNavStatusVisible, Converter={StaticResource BoolToVis', 'NavStatus', 'IsNavStatusVisible, Converter={StaticResource BoolToVis'].
- Public API concept: visual components/resources exposed through XAML names, styles, and DataContext bindings.

### Internal Logic Details
- Defines visual hierarchy, resource keys, and binding expressions for runtime UI composition.

### Data Flow
- Inputs: DataContext properties/commands/converters/resources.
- Outputs: rendered controls and command invocations back into ViewModels.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopView.xaml.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/View/Shop/ShopView.xaml.cs
### Purpose
- C# source file containing application logic.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.View.Shop'].
- Main declared type(s): ['class ShopView'].

### Dependencies
- Key imports/usings: ['System.Windows.Controls'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Controls'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/View/Shop/ShopView.xaml'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel', 'class MatrixUpdateScope', 'class DateSyncScope'].

### Dependencies
- Key imports/usings: ['System.Threading'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private IDisposable EnterMatrixUpdate()` — implementation in file; inspect branches/side-effects in source.
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.
  - `private void NotifyMatrixChanged()` — implementation in file; inspect branches/side-effects in source.
  - `private void NotifyMatrixChangedCore()` — implementation in file; inspect branches/side-effects in source.
  - `private IDisposable EnterDateSync()` — implementation in file; inspect branches/side-effects in source.
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Specialized', 'System.ComponentModel', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Input', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Helpers', 'System.Windows', 'System.Windows.Threading'].
- Internal dependencies: ['WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Helpers'].
- External dependencies: ['System.Collections.Specialized', 'System.ComponentModel', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Input', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void SetBinds(IEnumerable<BindModel> binds)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetBindValue(string rawKey, out string value)` — implementation in file; inspect branches/side-effects in source.
  - `public Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public string? FormatKeyGesture(Key key, ModifierKeys modifiers)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryApplyBindToCell(string columnName, int rowIndex, string bindValue, out int? nextRowIndex)` — implementation in file; inspect branches/side-effects in source.
  - `private void Binds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void MarkBindCacheDirty()` — implementation in file; inspect branches/side-effects in source.
  - `private void EnsureBindCache()` — implementation in file; inspect branches/side-effects in source.
  - `private void HookAllBindRows()` — implementation in file; inspect branches/side-effects in source.
  - `private void UnhookAllBindRows()` — implementation in file; inspect branches/side-effects in source.
  - `private void HookBindRow(BindRow row)` — implementation in file; inspect branches/side-effects in source.
  - `private void UnhookBindRow(BindRow row)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Windows', 'WPFApp.Infrastructure'].
- Internal dependencies: ['WPFApp.Infrastructure'].
- External dependencies: ['System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public AsyncRelayCommand SaveCommand`
  - `public AsyncRelayCommand CancelCommand`
  - `public AsyncRelayCommand CancelInformationCommand`
  - `public AsyncRelayCommand CancelEmployeeCommand`
  - `public AsyncRelayCommand CancelBindCommand`
  - `public AsyncRelayCommand AddEmployeeCommand`
  - `public AsyncRelayCommand RemoveEmployeeCommand`
  - `public AsyncRelayCommand SearchEmployeeCommand`
  - `public AsyncRelayCommand AddBindCommand`
  - `public AsyncRelayCommand DeleteBindCommand`
- Methods/constructors (detected signatures):
  - `private Task SaveWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task AddEmployeeAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task RemoveEmployeeAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SearchEmployeeAsync()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.ComponentModel', 'System.Data', 'System.Linq', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.Infrastructure.Validation', 'WPFApp.ViewModel.Availability.Helpers', 'WPFApp.ViewModel.Availability.Main'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.Infrastructure.Validation', 'WPFApp.ViewModel.Availability.Helpers', 'WPFApp.ViewModel.Availability.Main'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.ComponentModel', 'System.Data', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<EmployeeListItem> Employees`
  - `public ObservableCollection<BindRow> Binds`
  - `public EmployeeListItem? SelectedEmployee`
  - `public BindRow? SelectedBind`
  - `public object? SelectedAvailabilityDay`
  - `public int AvailabilityId`
  - `public int AvailabilityMonth`
  - `public int AvailabilityYear`
  - `public string AvailabilityName`
  - `public string EmployeeSearchText`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.Enums', 'System', 'System.Collections.Generic', 'System.Linq', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Helpers'].
- Internal dependencies: ['DataAccessLayer.Models.Enums', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void SetEmployees(IEnumerable<EmployeeModel> employees, IReadOnlyDictionary<int, string> nameLookup)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetForNew()` — implementation in file; inspect branches/side-effects in source.
  - `public void LoadGroup(AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days,
            IReadOnlyDictionary<int, string> nameLookup)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetGroupMatrix()` — implementation in file; inspect branches/side-effects in source.
  - `public IReadOnlyList<int> GetSelectedEmployeeIds()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetEmployeeCodes(int employeeId, IEnumerable<(int dayOfMonth, string code)` — implementation in file; inspect branches/side-effects in source.
  - `private void RegenerateGroupDays()` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryAddEmployeeColumn(int employeeId, string header)` — implementation in file; inspect branches/side-effects in source.
  - `private bool RemoveEmployeeColumn(int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetGroupMatrixCore(bool regenerateDays)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Data', 'System.Linq', 'System.Text', 'System.Windows', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Data', 'System.Linq', 'System.Text', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool ValidateBeforeSave(bool showDialog = true)` — implementation in file; inspect branches/side-effects in source.
  - `private void ValidateProperty(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearValidationErrors(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearValidationErrors()` — implementation in file; inspect branches/side-effects in source.
  - `private string MapValidationKeyToVm(string key)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowValidationErrorsDialog(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `private void GroupTable_ColumnChanged(object? sender, DataColumnChangeEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void NormalizeAndValidateAllMatrixCells()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Edit'].
- Main declared type(s): ['class AvailabilityEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Specialized', 'System.Data', 'System.Reflection', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Main'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Main'].
- External dependencies: ['System.Collections.Specialized', 'System.Data', 'System.Reflection'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Batching.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Commands.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Fields.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Matrix.cs', 'WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Validation.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Helpers/BindRow.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Helpers'].
- Main declared type(s): ['class BindRow'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int Id`
  - `public string Key`
  - `public string Value`
  - `public bool IsActive`
  - `public bool IsBlank`
- Methods/constructors (detected signatures):
  - `public BindModel ToModel()` — implementation in file; inspect branches/side-effects in source.
  - `public void UpdateFromModel(BindModel model)` — implementation in file; inspect branches/side-effects in source.
  - `public BindRow FromModel(BindModel model)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Explicit exception throwing/propagation exists.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Helpers/EmployeeListItem.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['class EmployeeListItem'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int Id`
  - `public string FullName`
- Methods/constructors (detected signatures):
  - `public string ToString()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/List/AvailabilityListViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.List'].
- Main declared type(s): ['class AvailabilityListViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Main'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Main'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<AvailabilityGroupModel> Items`
  - `public AvailabilityGroupModel? SelectedItem`
  - `public string SearchText`
  - `public AsyncRelayCommand SearchCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand OpenProfileCommand`
- Methods/constructors (detected signatures):
  - `public void SetItems(IEnumerable<AvailabilityGroupModel> items)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsSameList(IList<AvailabilityGroupModel> current, IList<AvailabilityGroupModel> next)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSelectionCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Input', 'System.Windows.Threading', 'WPFApp.Infrastructure.Hotkeys', 'WPFApp.ViewModel.Availability.Helpers'].
- Internal dependencies: ['WPFApp.Infrastructure.Hotkeys', 'WPFApp.ViewModel.Availability.Helpers'].
- External dependencies: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Input', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task AddBindAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task FlashNavWorkingSuccessAsync(CancellationToken ct = default, int successMs = 550)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteBindAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal string? FormatKeyGesture(Key key, ModifierKeys modifiers)` — implementation in file; inspect branches/side-effects in source.
  - `internal bool TryNormalizeKey(string raw, out string normalized)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Threading', 'System.Threading.Tasks', 'System.Windows'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Threading', 'System.Threading.Tasks', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void OnDatabaseChanged(object? sender, Service.DatabaseChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ReloadAfterDatabaseChangeAsync(string source)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal void ApplyEmployeeFilter(string? raw)` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetEmployeeSearch()` — implementation in file; inspect branches/side-effects in source.
  - `private bool ContainsIgnoreCase(string? source, string value)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task SearchAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task StartAddAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task EditSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SaveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task OpenProfileAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CancelAsync()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task EnsureInitializedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task InitializeCoreAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadAllGroupsAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadEmployeesAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadBindsAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'WPFApp.View.Dialogs'].
- Internal dependencies: ['WPFApp.View.Dialogs'].
- External dependencies: ['System', 'System.Threading', 'System.Threading.Tasks', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public bool IsNavStatusVisible`
  - `public UIStatusKind NavStatus`
- Methods/constructors (detected signatures):
  - `private CancellationToken ResetNavUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideNavStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public object CurrentSection`
  - `public AvailabilitySection Mode`
  - `public AvailabilitySection CancelTarget`
- Methods/constructors (detected signatures):
  - `private Task SwitchToListAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToEditAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToProfileAsync()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['System.Threading.Tasks', 'System.Windows', 'WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Threading.Tasks', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task RunOnUiThreadAsync(Action action)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowInfo(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(Exception ex)` — implementation in file; inspect branches/side-effects in source.
  - `private bool Confirm(string text, string? caption = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Main'].
- Main declared type(s): ['enum AvailabilitySection', 'class AvailabilityViewModel'].

### Dependencies
- Key imports/usings: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Edit', 'WPFApp.ViewModel.Availability.List', 'WPFApp.ViewModel.Availability.Profile'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Edit', 'WPFApp.ViewModel.Availability.List', 'WPFApp.ViewModel.Availability.Profile'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum AvailabilitySection`
  - `public AvailabilityListViewModel ListVm`
  - `public AvailabilityEditViewModel EditVm`
  - `public AvailabilityProfileViewModel ProfileVm`
  - `public AsyncRelayCommand ShowListCommand`
  - `public AsyncRelayCommand ShowEditCommand`
  - `public AsyncRelayCommand ShowProfileCommand`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Binds.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Employees.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Groups.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Initialization.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.NavStatus.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Navigation.cs', 'WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.Ui.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Availability/Profile/AvailabilityProfileViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Availability.Profile'].
- Main declared type(s): ['class AvailabilityProfileViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models.Enums', 'System', 'System.Collections.Generic', 'System.Data', 'System.Linq', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Main'].
- Internal dependencies: ['DataAccessLayer.Models.Enums', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityMatrix', 'WPFApp.ViewModel.Availability.Main'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Data', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public object? SelectedProfileMonth`
  - `public int AvailabilityId`
  - `public string AvailabilityName`
  - `public string AvailabilityMonthYear`
  - `public AsyncRelayCommand BackCommand`
  - `public AsyncRelayCommand CancelProfileCommand`
  - `public AsyncRelayCommand CancelTableCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
- Methods/constructors (detected signatures):
  - `private void UpdateGroupCommands()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetProfile(AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days)` — implementation in file; inspect branches/side-effects in source.
  - `private string ToProfileCellCode(AvailabilityGroupDayModel d)` — implementation in file; inspect branches/side-effects in source.
  - `private string NormalizeInterval(string raw)` — implementation in file; inspect branches/side-effects in source.
  - `private void NotifyMatrixChanged()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerEditViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections', 'System.ComponentModel', 'DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation'].
- External dependencies: ['System.Collections', 'System.ComponentModel'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int ContainerId`
  - `public string Name`
  - `public string? Note`
  - `public bool IsEdit`
  - `public AsyncRelayCommand SaveCommand`
  - `public AsyncRelayCommand CancelCommand`
- Methods/constructors (detected signatures):
  - `public IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetForNew()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetContainer(ContainerModel model)` — implementation in file; inspect branches/side-effects in source.
  - `public ContainerModel ToModel()` — implementation in file; inspect branches/side-effects in source.
  - `private bool ValidateBeforeSave()` — implementation in file; inspect branches/side-effects in source.
  - `private void ValidateProperty(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearValidationErrors()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearValidationErrors(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SaveWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 4 private methods (regex-detected).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure.AvailabilityPreview'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure.AvailabilityPreview'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task UpdateAvailabilityPreviewAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ClearAvailabilityPreviewAsync(int year, int month, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `internal void CancelScheduleEditWork()` — implementation in file; inspect branches/side-effects in source.
  - `private void CancelAvailabilityPreviewPipeline()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading', 'WPFApp.ViewModel.Container.Edit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Edit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Task EnsureInitializedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task StartAddAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task EditSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SaveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task OpenProfileAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadContainersAsync(CancellationToken ct, int? selectId = null)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadSchedulesAsync(int containerId, string? search, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void OnDatabaseChanged(object? sender, Service.DatabaseChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ReloadAfterDatabaseChangeAsync(string source)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private Task LoadLookupsAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void ResetScheduleFilters()` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchScheduleShopsAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchScheduleAvailabilityAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchScheduleEmployeesAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyShopFilter(string? raw)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyAvailabilityFilter(string? raw)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyEmployeeFilter(string? raw)` — implementation in file; inspect branches/side-effects in source.
  - `private bool ContainsIgnoreCase(string? source, string value)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 6 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['WPFApp.ViewModel.Container.Edit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Edit.Helpers'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public object CurrentSection`
  - `public ContainerSection Mode`
  - `public ContainerSection CancelTarget`
  - `public ContainerSection ScheduleCancelTarget`
- Methods/constructors (detected signatures):
  - `private int GetCurrentContainerId()` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CancelAsync()` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CancelScheduleAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToListAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToEditAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToProfileAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToScheduleEditAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToScheduleProfileAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private void CleanupScheduleEdit()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading', 'WPFApp.Infrastructure.Validation', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.Infrastructure.Validation', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task AddScheduleEmployeeAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RemoveScheduleEmployeeAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task AddScheduleBlockAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SelectScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CloseScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private ScheduleBlockViewModel CreateDefaultBlock(int containerId)` — implementation in file; inspect branches/side-effects in source.
  - `private ScheduleBlockViewModel CreateBlockFromSchedule(ScheduleModel model,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel>? cellStyles = null)` — implementation in file; inspect branches/side-effects in source.
  - `private bool HasGeneratedContent(ScheduleModel schedule)` — implementation in file; inspect branches/side-effects in source.
  - `private bool HasGeneratedContent(ScheduleBlockViewModel block)` — implementation in file; inspect branches/side-effects in source.
  - `private int GetDefaultAvailabilityGroupId(int year, int month)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 5 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Threading', 'WPFApp.ViewModel.Container.Edit.Helpers', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers', 'System.Windows'].
- Internal dependencies: ['WPFApp.ViewModel.Container.Edit.Helpers', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Threading', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task SearchScheduleAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task StartScheduleAddAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task EditSelectedScheduleAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task MultiOpenSchedulesAsync(IReadOnlyList<ScheduleModel> schedules, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task OpenScheduleProfileAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteSelectedScheduleAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.SaveGenerate.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'System', 'System.Collections.Generic', 'System.Globalization', 'System.Linq', 'System.Text', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Input', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.View.Dialogs'].
- Internal dependencies: ['DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Container.Edit.Helpers', 'WPFApp.ViewModel.Container.ScheduleEdit', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Globalization', 'System.Linq', 'System.Text', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task SaveScheduleAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task GenerateScheduleAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SyncEmployeesFromAvailabilityGroupAsync(int groupId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private CancellationToken ResetSaveUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowSaveWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideSaveStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowSaveSuccessThenAutoHideAsync(CancellationToken ct, int ms = 1400)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildScheduleValidationSummary(IReadOnlyCollection<(ScheduleBlockVm Block, Dictionary<string, string> Errors)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetScheduleFieldLabel(string propertyName)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'System', 'System.Collections.Concurrent', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Container.List'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Container.List', 'WPFApp.ViewModel.Container.Profile', 'WPFApp.ViewModel.Container.ScheduleEdit', 'WPFApp.ViewModel.Container.ScheduleProfile'].
- External dependencies: ['System', 'System.Collections.Concurrent', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public bool IsNavStatusVisible`
  - `public UIStatusKind NavStatus`
  - `public ContainerListViewModel ListVm`
  - `public ContainerEditViewModel EditVm`
  - `public ContainerProfileViewModel ProfileVm`
  - `public ContainerScheduleEditViewModel ScheduleEditVm`
  - `public ContainerScheduleProfileViewModel ScheduleProfileVm`
- Methods/constructors (detected signatures):
  - `private CancellationToken ResetNavUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideNavStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)` — implementation in file; inspect branches/side-effects in source.
  - `private Task<IReadOnlyList<ScheduleEmployeeModel>> LoadScheduleEmployeesAsync(int scheduleId, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task<IReadOnlyList<ScheduleSlotModel>> LoadScheduleSlotsAsync(int scheduleId, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task<ScheduleModel?> GetScheduleDetailsCachedAsync(int scheduleId, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearScheduleDetailsCache()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 8 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/Helpers/ContainerSection.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit.Helpers'].
- Main declared type(s): ['enum ContainerSection'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum ContainerSection`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/Helpers/ContainerViewModel.Export.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Service'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Service'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal Task ExportScheduleToExcelAsync(ScheduleExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task ExportScheduleToSqlAsync(ScheduleSqlExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task ExportContainerToExcelAsync(ContainerExcelExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task ExportContainerToSqlAsync(ContainerSqlExportContext context, string filePath, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task<ScheduleModel?> LoadScheduleDetailsForExportAsync(int scheduleId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task<AvailabilityGroupExportData?> LoadAvailabilityGroupExportDataAsync(int? availabilityGroupId, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Edit/Helpers/ContainerViewModel.Ui.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Edit'].
- Main declared type(s): ['class ContainerViewModel'].

### Dependencies
- Key imports/usings: ['System.Threading.Tasks', 'System.Windows', 'System.Windows.Media', 'WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System.Threading.Tasks', 'System.Windows', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `internal bool TryPickScheduleCellColor(Color? initialColor, out Color color)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RunOnUiThreadAsync(Action action)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowInfo(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(Exception ex)` — implementation in file; inspect branches/side-effects in source.
  - `internal bool Confirm(string text, string? caption = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/Edit/ContainerViewModel.AvailabilityPreview.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Containers.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.DatabaseReload.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Navigation.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.ScheduleEditor.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.BlockFactory.cs', 'WPFApp/ViewModel/Container/Edit/ContainerViewModel.Schedules.Open.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/List/ContainerListViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.List'].
- Main declared type(s): ['class ContainerListViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.Edit'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.Edit'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<ContainerModel> Items`
  - `public ContainerModel? SelectedItem`
  - `public string SearchText`
  - `public AsyncRelayCommand SearchCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand OpenProfileCommand`
- Methods/constructors (detected signatures):
  - `public void SetItems(IEnumerable<ContainerModel> containers)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSelectionCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/Profile/ContainerProfileViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.Profile'].
- Main declared type(s): ['class ContainerProfileViewModel', 'class ShopHeader', 'class EmployeeShopHoursRow', 'class EmployeeWorkFreeStatRow', 'class ScheduleStatsSource', 'class BuildStatsResult', 'class NullScheduleMatrixStyleProvider'].

### Dependencies
- Key imports/usings: ['Microsoft.Win32', 'System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Collections.Specialized', 'System.Globalization', 'System.Linq', 'System.Reflection', 'System.Text.RegularExpressions', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.Service', 'WPFApp.ViewModel.Container.ScheduleProfile', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.List', 'WPFApp.ViewModel.Container.ScheduleList', 'WPFApp.View.Dialogs'].
- External dependencies: ['Microsoft.Win32', 'System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Collections.Specialized', 'System.Globalization', 'System.Linq', 'System.Reflection', 'System.Text.RegularExpressions', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public bool IsExportStatusVisible`
  - `public UIStatusKind ExportStatus`
  - `public int ContainerId`
  - `public string Name`
  - `public string? Note`
  - `public ContainerScheduleListViewModel ScheduleListVm`
  - `public AsyncRelayCommand BackCommand`
  - `public AsyncRelayCommand CancelProfileCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand ExportToExcelCommand`
  - `public AsyncRelayCommand ExportToCodeCommand`
  - `public int CellStyleRevision`
  - `public int TotalEmployees`
  - `public int TotalShops`
  - `public string TotalEmployeesListText`
  - `public string TotalShopsListText`
  - `public string TotalHoursText`
  - `public string Key`
  - `public string Name`
  - `public ObservableCollection<ShopHeader> ShopHeaders`
  - `public string Employee`
  - `public int WorkDays`
  - `public int FreeDays`
  - `public string HoursSum`
- Methods/constructors (detected signatures):
  - `private bool CanExport()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetProfile(ContainerModel model)` — implementation in file; inspect branches/side-effects in source.
  - `public Task SetStatisticsAsync(IList<ScheduleStatsSource>? sources, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private CancellationToken ResetExportUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowExportWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideExportStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowExportSuccessThenAutoHideAsync(CancellationToken ct, int ms = 1400)` — implementation in file; inspect branches/side-effects in source.
  - `public Task RefreshStatisticsFromContainerSchedulesAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal void CancelBackgroundWork()` — implementation in file; inspect branches/side-effects in source.
  - `private void HookScheduleListForAutoStatistics()` — implementation in file; inspect branches/side-effects in source.
  - `private Task QueueAutoStatsRebuildAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExportToExcelAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExportToCodeAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private List<ContainerScheduleProfileViewModel.EmployeeWorkFreeStatRow> BuildEmployeeWorkFreeStats(IList<ContainerScheduleProfileViewModel.SummaryEmployeeRow> summaryRows,
            int year,
            int month)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseSummaryHoursToMinutes(string? text, out int minutes)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetShopAddress(ShopModel? shop)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` — implementation in file; inspect branches/side-effects in source.
  - `private Task<List<ScheduleStatsSource>> BuildSourcesFromScheduleListAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryExtractScheduleModel(object row, out ScheduleModel schedule)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatHoursMinutes(TimeSpan ts)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatHoursCell(TimeSpan ts)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildPreviewList(IReadOnlyList<string> items, int previewCount = 8)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetSlotEmployeeId(ScheduleSlotModel slot, out int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetSlotDay(ScheduleSlotModel slot, out int day)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetFirstLast(object obj, out string fullName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? TryGetString(object obj, string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetEmployeeDisplayName(ScheduleEmployeeModel emp)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 23 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Windows', 'System.Windows.Media', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Linq', 'System.Windows', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private Brush ToBrushCached(int argb)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` — implementation in file; inspect branches/side-effects in source.
  - `private void RefreshCellStyleMap()` — implementation in file; inspect branches/side-effects in source.
  - `internal void RemoveCellStylesForEmployee(int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `public void ApplyPaintToCell(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void SetCellBackgroundColor(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void SetCellTextColor(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyLastFillColor(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyLastTextColor(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void PickFillColor()` — implementation in file; inspect branches/side-effects in source.
  - `private void PickTextColor()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearCellFormatting(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearSelectedCellStyles(ScheduleMatrixCellRef? cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearAllScheduleStyles()` — implementation in file; inspect branches/side-effects in source.
  - `private ScheduleCellStyleModel GetOrCreateCellStyle(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyCellBackgroundColor(IReadOnlyCollection<ScheduleMatrixCellRef> cellRefs, int argb)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyCellTextColor(IReadOnlyCollection<ScheduleMatrixCellRef> cellRefs, int argb)` — implementation in file; inspect branches/side-effects in source.
  - `private IReadOnlyCollection<ScheduleMatrixCellRef> GetTargetCells(ScheduleMatrixCellRef? fallback)` — implementation in file; inspect branches/side-effects in source.
  - `public void UpdateSelectedCellRefs(IEnumerable<ScheduleMatrixCellRef> cellRefs)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 15 private methods (regex-detected).
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.
  - `private SelectionSyncScope EnterSelectionSync()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetLookups(IEnumerable<ShopModel> shops,
                               IEnumerable<AvailabilityGroupModel> groups,
                               IEnumerable<EmployeeModel> employees)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetShops(IEnumerable<ShopModel> shops)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetAvailabilityGroups(IEnumerable<AvailabilityGroupModel> groups)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetEmployees(IEnumerable<EmployeeModel> employees)` — implementation in file; inspect branches/side-effects in source.
  - `public void CommitPendingShopSelection()` — implementation in file; inspect branches/side-effects in source.
  - `public void CommitPendingAvailabilitySelection()` — implementation in file; inspect branches/side-effects in source.
  - `public void SyncSelectionFromBlock()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System.Data', 'System.Globalization', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Internal dependencies: ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System.Data', 'System.Globalization'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private bool IsTechnicalMatrixColumn(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Data', 'System.Diagnostics', 'System.Globalization', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Data', 'System.Diagnostics', 'System.Globalization', 'System.Linq', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void RefreshScheduleMatrix()` — implementation in file; inspect branches/side-effects in source.
  - `private void SafeForget(Task task)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RefreshScheduleMatrixAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RefreshAvailabilityPreviewMatrixAsync(int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            string? previewKey = null,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryApplyMatrixEdit(string columnName, int day, string rawInput, out string normalizedValue, out string? error)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateMatrixCellInPlace(string columnName, int day, string normalizedValue)` — implementation in file; inspect branches/side-effects in source.
  - `internal void CancelBackgroundWork()` — implementation in file; inspect branches/side-effects in source.
  - `internal bool IsAvailabilityPreviewCurrent(string? previewKey)` — implementation in file; inspect branches/side-effects in source.
  - `private void RestoreMatricesForSelection()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Runtime.CompilerServices', 'WPFApp.Infrastructure.Validation'].
- Internal dependencies: ['WPFApp.Infrastructure.Validation'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Runtime.CompilerServices'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Data', 'WPFApp.Infrastructure.Threading', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs', 'System.Collections.Generic', 'System.Linq', 'System.Threading.Tasks', 'System.Threading', 'BusinessLogicLayer.Availability', 'WPFApp.Infrastructure.AvailabilityPreview'].
- Internal dependencies: ['WPFApp.Infrastructure.Threading', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs', 'BusinessLogicLayer.Availability', 'WPFApp.Infrastructure.AvailabilityPreview'].
- External dependencies: ['System', 'System.Data', 'System.Collections.Generic', 'System.Linq', 'System.Threading.Tasks', 'System.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void CancelSelectionDebounce()` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleShopSelectionChange(int newId)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleAvailabilitySelectionChange(int newId)` — implementation in file; inspect branches/side-effects in source.
  - `internal void InvalidateGeneratedSchedule(bool clearPreviewMatrix = true)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadAvailabilityContextAsync(int availabilityGroupId)` — implementation in file; inspect branches/side-effects in source.
  - `private void ReplaceScheduleEmployeesFromAvailability(IEnumerable<EmployeeModel> employees)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnSchedulePeriodChanged()` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetAvailabilityGroupPeriod(AvailabilityGroupModel? group, out int year, out int month)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsAvailabilityPeriodMismatch(AvailabilityGroupModel? group, int scheduleYear, int scheduleMonth,
            out int groupYear, out int groupMonth)` — implementation in file; inspect branches/side-effects in source.
  - `private Task<List<EmployeeModel>> ResolveEmployeesForMembersAsync(List<AvailabilityGroupMemberModel> members,
    CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 9 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['WPFApp.Infrastructure.ScheduleMatrix'].
- Internal dependencies: ['WPFApp.Infrastructure.ScheduleMatrix'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string GetEmployeeTotalHoursText(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `private void RecalculateTotals()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Globalization', 'System.Runtime.CompilerServices', 'System.Text', 'System.Windows', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Globalization', 'System.Runtime.CompilerServices', 'System.Text', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void ClearValidationErrors()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearValidationErrors(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private void AddValidationError(string propertyName, string message)` — implementation in file; inspect branches/side-effects in source.
  - `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `private bool ValidateBeforeSave(bool showDialog = true,
            bool requireShift2 = false,
            bool requireAvailabilityGroup = false)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyPendingSelectionsForValidation()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SaveWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task GenerateWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private string MapValidationKeyToVm(string key)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 8 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit'].
- Main declared type(s): ['class ContainerScheduleEditViewModel', 'enum SchedulePaintMode'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Availability', 'BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'System.Collections.ObjectModel', 'System.ComponentModel', 'System.Data', 'System.Diagnostics', 'System.Globalization', 'System.Runtime.CompilerServices', 'System.Text', 'System.Windows'].
- Internal dependencies: ['BusinessLogicLayer.Availability', 'BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'DataAccessLayer.Models.Enums', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.AvailabilityPreview', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.Infrastructure.Threading', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Container.Edit'].
- External dependencies: ['System.Collections.ObjectModel', 'System.ComponentModel', 'System.Data', 'System.Diagnostics', 'System.Globalization', 'System.Runtime.CompilerServices', 'System.Text', 'System.Windows', 'System.Windows.Data', 'System.Windows.Media', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum SchedulePaintMode`
  - `public bool IsSaveStatusVisible`
  - `public UIStatusKind SaveStatus`
  - `public ObservableCollection<ScheduleMatrixCellRef> SelectedCellRefs`
  - `public ObservableCollection<ScheduleBlockViewModel> Blocks`
  - `public SchedulePaintMode ActivePaintMode`
  - `public int? LastFillColorArgb`
  - `public int? LastTextColorArgb`
  - `public Brush? LastFillBrush`
  - `public Brush? LastTextBrush`
  - `public ScheduleMatrixCellRef? SelectedCellRef`
  - `public int TotalEmployees`
  - `public string TotalHoursText`
  - `public ScheduleBlockViewModel? SelectedBlock`
  - `public ScheduleBlockViewModel? ActiveSchedule`
  - `public bool IsEdit`
  - `public int ScheduleId`
  - `public int ScheduleContainerId`
  - `public int ScheduleShopId`
  - `public string ScheduleName`
  - `public int ScheduleYear`
  - `public int ScheduleMonth`
  - `public int SchedulePeoplePerShift`
  - `public string ScheduleShift1`
  - `public string ScheduleShift2`
- Methods/constructors (detected signatures):
  - `private void Validation_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `public System.Collections.IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsManualEmployeeId(int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `private void RebindMinHoursEmployeesView()` — implementation in file; inspect branches/side-effects in source.
  - `public Task SelectBlockAsync(ScheduleBlockViewModel block)` — implementation in file; inspect branches/side-effects in source.
  - `public Task CloseBlockAsync(ScheduleBlockViewModel block)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetForNew()` — implementation in file; inspect branches/side-effects in source.
  - `private void EnsureSelectedScheduleEmployee()` — implementation in file; inspect branches/side-effects in source.
  - `private CancellationToken ResetSaveUiPulseCts(CancellationToken outer = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowSaveWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideSaveStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowSaveSuccessThenAutoHideAsync(CancellationToken ct, int ms = 450)` — implementation in file; inspect branches/side-effects in source.
  - `private Task PulseSaveWorkingSuccessAsync(int ms = 350, CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task AddEmployeeAndKeepSelectionAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task RemoveEmployeeSmartAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task AddEmployeeManualAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task RemoveEmployeeManualAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsAvailabilityEmployee(int employeeId)` — implementation in file; inspect branches/side-effects in source.
  - `private void SetAvailabilityEmployees(IEnumerable<EmployeeModel> availabilityEmployees)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 15 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Lookups.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.MatrixEditAndRefresh.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.ModelBinding.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Selection.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Totals.cs', 'WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.Validation.cs'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/Helpers/IScheduleMatrixStyleProvider.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Main declared type(s): ['interface IScheduleMatrixStyleProvider'].

### Dependencies
- Key imports/usings: ['System.Windows.Media'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public interface IScheduleMatrixStyleProvider`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleBlockViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Main declared type(s): ['class ScheduleBlockViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- External dependencies: ['System.Collections.Generic', 'System.Collections.ObjectModel'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public Guid Id`
  - `public ScheduleModel Model`
  - `public ObservableCollection<ScheduleEmployeeModel> Employees`
  - `public ObservableCollection<ScheduleSlotModel> Slots`
  - `public ObservableCollection<ScheduleCellStyleModel> CellStyles`
  - `public int SelectedAvailabilityGroupId`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleCellStyleStore.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- Main declared type(s): ['class ScheduleCellStyleStore'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'DataAccessLayer.Models'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['System.Collections.Generic', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void Load(IEnumerable<ScheduleCellStyleModel> styles)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` — implementation in file; inspect branches/side-effects in source.
  - `public ScheduleCellStyleModel GetOrCreate(ScheduleMatrixCellRef cellRef,
            Func<ScheduleCellStyleModel> factory,
            ICollection<ScheduleCellStyleModel> storage)` — implementation in file; inspect branches/side-effects in source.
  - `public int RemoveStyles(IEnumerable<ScheduleMatrixCellRef> cellRefs, ICollection<ScheduleCellStyleModel> storage)` — implementation in file; inspect branches/side-effects in source.
  - `public int RemoveAll(ICollection<ScheduleCellStyleModel> storage)` — implementation in file; inspect branches/side-effects in source.
  - `public int RemoveByEmployee(int employeeId, ICollection<ScheduleCellStyleModel> storage)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleEdit/Helpers/ScheduleMatrixCellRef.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['record struct', 'record struct'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleList/ContainerScheduleListViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleList'].
- Main declared type(s): ['class ScheduleRowVm', 'class ContainerScheduleListViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.ObjectModel', 'System.Linq', 'System.Windows.Input', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System', 'System.Collections.ObjectModel', 'System.Linq', 'System.Windows.Input'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ScheduleModel Model`
  - `public bool IsChecked`
  - `public ObservableCollection<ScheduleRowVm> Items`
  - `public ScheduleRowVm? SelectedItem`
  - `public string SearchText`
  - `public bool IsMultiOpenEnabled`
  - `public ICommand ToggleMultiOpenCommand`
  - `public AsyncRelayCommand MultiOpenCommand`
  - `public AsyncRelayCommand SearchCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand OpenProfileCommand`
- Methods/constructors (detected signatures):
  - `public void SetItems(IEnumerable<ScheduleModel> schedules)` — implementation in file; inspect branches/side-effects in source.
  - `private void OnRowCheckedChanged()` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSelectionCommands()` — implementation in file; inspect branches/side-effects in source.
  - `public void ToggleRowSelection(ScheduleRowVm row)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatScheduleName(ScheduleModel model, int index)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 3 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Container/ScheduleProfile/ContainerScheduleProfileViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Container.ScheduleProfile'].
- Main declared type(s): ['class ContainerScheduleProfileViewModel', 'class EmployeeWorkFreeStatRow', 'class SummaryDayHeader', 'class SummaryDayCell', 'class SummaryEmployeeRow', 'class BuildResult'].

### Dependencies
- Key imports/usings: ['Microsoft.Win32', 'System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Text.RegularExpressions', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Media'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.Service', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers', 'WPFApp.View.Dialogs'].
- External dependencies: ['Microsoft.Win32', 'System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Text.RegularExpressions', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int ScheduleId`
  - `public string ScheduleName`
  - `public string ScheduleMonthYear`
  - `public string ShopName`
  - `public string Note`
  - `public int ScheduleMonth`
  - `public int ScheduleYear`
  - `public string ShopAddress`
  - `public int TotalDays`
  - `public string Shift1`
  - `public string Shift2`
  - `public DataView ScheduleMatrix`
  - `public int CellStyleRevision`
  - `public int TotalEmployees`
  - `public string TotalHoursText`
  - `public string TotalEmployeesListText`
  - `public bool IsExportStatusVisible`
  - `public UIStatusKind ExportStatus`
  - `public ObservableCollection<ScheduleEmployeeModel> Employees`
  - `public ObservableCollection<SummaryDayHeader> SummaryDayHeaders`
  - `public ObservableCollection<SummaryEmployeeRow> SummaryRows`
  - `public string Employee`
  - `public int WorkDays`
  - `public int FreeDays`
  - `public ObservableCollection<EmployeeWorkFreeStatRow> EmployeeWorkFreeStats`
- Methods/constructors (detected signatures):
  - `public Task SetProfileAsync(ScheduleModel schedule,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal void CancelBackgroundWork()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExportToExcelAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExportToSQLiteAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void RebuildEmployeeWorkFreeStats()` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseSummaryHoursToMinutes(string? text, out int minutes)` — implementation in file; inspect branches/side-effects in source.
  - `private int CountWorkDays(IEnumerable<SummaryDayCell>? days)` — implementation in file; inspect branches/side-effects in source.
  - `public string GetEmployeeTotalHoursText(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsTechnicalMatrixColumn(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` — implementation in file; inspect branches/side-effects in source.
  - `private void RebuildStyleMaps(Dictionary<string, int> colMap, IList<ScheduleCellStyleModel> cellStyles)` — implementation in file; inspect branches/side-effects in source.
  - `private Brush ToBrushCached(int argb)` — implementation in file; inspect branches/side-effects in source.
  - `private int SafeDaysInMonth(int year, int month)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetScheduleString(ScheduleModel schedule, params string[] propertyNames)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetShopAddress(ShopModel? shop)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatTimeSpanToSummary(TimeSpan ts)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseTimeRanges(string text, out string from, out string to, out TimeSpan duration)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatHoursCell(TimeSpan ts)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildPreviewList(IReadOnlyList<string> items, int previewCount = 8)` — implementation in file; inspect branches/side-effects in source.
  - `private CancellationToken ResetExportUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowExportWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideExportStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowExportSuccessThenAutoHideAsync(CancellationToken ct, int milliseconds = 1400)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetFirstLast(object obj, out string fullName)` — implementation in file; inspect branches/side-effects in source.
  - `private string? TryGetString(object obj, string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private string GetEmployeeDisplayName(ScheduleEmployeeModel emp)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 22 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Database/DatabaseViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Database'].
- Main declared type(s): ['class DatabaseViewModel'].

### Dependencies
- Key imports/usings: ['Microsoft.Win32', 'System', 'System.Data', 'System.Globalization', 'System.IO', 'System.Threading', 'System.Threading.Tasks', 'WPFApp.Infrastructure', 'WPFApp.Service'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Service'].
- External dependencies: ['Microsoft.Win32', 'System', 'System.Data', 'System.Globalization', 'System.IO', 'System.Threading', 'System.Threading.Tasks'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public string ExecutorSql`
  - `public DataView? QueryResult`
  - `public string ExecutorOutput`
  - `public bool HasQueryResult`
  - `public bool IsExecutorError`
  - `public string ImportFilePath`
  - `public string ImportFileSize`
  - `public string ImportFileModified`
  - `public string ImportFileHash`
  - `public string ImportScript`
  - `public string ImportOutput`
  - `public bool IsImportError`
  - `public string DatabasePath`
  - `public string DatabaseSize`
  - `public string DatabaseLastUpdated`
  - `public string SchemaVersion`
  - `public string TablesSummary`
  - `public AsyncRelayCommand ExecuteSqlCommand`
  - `public RelayCommand ClearExecutorOutputCommand`
  - `public AsyncRelayCommand ChooseImportFileCommand`
  - `public AsyncRelayCommand ExecuteImportScriptCommand`
  - `public AsyncRelayCommand RefreshInfoCommand`
- Methods/constructors (detected signatures):
  - `private Task InitializeOnEnterAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExecuteSqlAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearExecutorOutput()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ChooseImportFileAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ExecuteImportScriptAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task RefreshDatabaseInfoAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private string FormatBytes(long bytes)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 7 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Dialogs/CustomMessageBoxViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Dialogs'].
- Main declared type(s): ['enum CustomMessageBoxIcon', 'class CustomMessageBoxViewModel', 'class RelayCommand'].

### Dependencies
- Key imports/usings: ['System.Windows', 'System.Windows.Input', 'System.Windows.Media'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Windows', 'System.Windows.Input', 'System.Windows.Media'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum CustomMessageBoxIcon`
  - `public string Title`
  - `public string Message`
  - `public string Details`
  - `public string OkText`
  - `public string CancelText`
  - `public Geometry IconGeometry`
  - `public ICommand OkCommand`
  - `public ICommand CancelCommand`
- Methods/constructors (detected signatures):
  - `private Geometry TryGetGeometryResource(string resourceKey)` — implementation in file; inspect branches/side-effects in source.
  - `public bool CanExecute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void Execute(object? parameter)` — implementation in file; inspect branches/side-effects in source.
  - `public void RaiseCanExecuteChanged()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).
- Explicit exception throwing/propagation exists.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Employee/EmployeeEditViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Employee'].
- Main declared type(s): ['class EmployeeEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Windows', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System', 'System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int EmployeeId`
  - `public string FirstName`
  - `public string LastName`
  - `public string? Email`
  - `public string? Phone`
  - `public bool IsEdit`
  - `public AsyncRelayCommand SaveCommand`
  - `public AsyncRelayCommand CancelCommand`
- Methods/constructors (detected signatures):
  - `public IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetForNew()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetEmployee(EmployeeModel model)` — implementation in file; inspect branches/side-effects in source.
  - `public EmployeeModel ToModel()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearValidationErrors()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearValidationErrors(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private void ValidateProperty(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool ValidateBeforeSave(bool showDialog = true)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SaveWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private string MapValidationKeyToVm(string key)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 6 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Employee/EmployeeListViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Employee'].
- Main declared type(s): ['class EmployeeListViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq', 'DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- External dependencies: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<EmployeeModel> Items`
  - `public EmployeeModel? SelectedItem`
  - `public string SearchText`
  - `public AsyncRelayCommand SearchCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand OpenProfileCommand`
- Methods/constructors (detected signatures):
  - `public void SetItems(IEnumerable<EmployeeModel> employees)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsSameList(IList<EmployeeModel> current, IList<EmployeeModel> next)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSelectionCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Employee/EmployeeProfileViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Employee'].
- Main declared type(s): ['class EmployeeProfileViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Employee.Helpers'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Employee.Helpers'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int EmployeeId`
  - `public string FullName`
  - `public string Email`
  - `public string Phone`
  - `public AsyncRelayCommand BackCommand`
  - `public AsyncRelayCommand CancelProfileCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
- Methods/constructors (detected signatures):
  - `public void SetProfile(EmployeeModel model)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Employee/EmployeeViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Employee'].
- Main declared type(s): ['enum EmployeeSection', 'class EmployeeViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'DocumentFormat.OpenXml.Spreadsheet', 'DocumentFormat.OpenXml.Wordprocessing', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs', 'WPFApp.ViewModel.Employee.Helpers'].
- External dependencies: ['DocumentFormat.OpenXml.Spreadsheet', 'DocumentFormat.OpenXml.Wordprocessing', 'System', 'System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum EmployeeSection`
  - `public bool IsNavStatusVisible`
  - `public UIStatusKind NavStatus`
  - `public object CurrentSection`
  - `public EmployeeSection Mode`
  - `public EmployeeSection CancelTarget`
  - `public EmployeeListViewModel ListVm`
  - `public EmployeeEditViewModel EditVm`
  - `public EmployeeProfileViewModel ProfileVm`
- Methods/constructors (detected signatures):
  - `private CancellationToken ResetNavUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideNavStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)` — implementation in file; inspect branches/side-effects in source.
  - `public Task EnsureInitializedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task InitializeCoreAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task StartAddAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task EditSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SaveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task OpenProfileAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CancelAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadEmployeesAsync(CancellationToken ct, int? selectId)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToListAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToEditAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToProfileAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private int GetCurrentEmployeeId()` — implementation in file; inspect branches/side-effects in source.
  - `private string GetCurrentEmployeeName()` — implementation in file; inspect branches/side-effects in source.
  - `private void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ReloadAfterDatabaseChangeAsync(string source)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RunOnUiThreadAsync(Action action)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowInfo(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(Exception ex)` — implementation in file; inspect branches/side-effects in source.
  - `private bool Confirm(string text, string? caption = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 14 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Employee/Helpers/EmployeeDisplayHelper.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Employee.Helpers'].
- Main declared type(s): ['class EmployeeDisplayHelper'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models'].
- Internal dependencies: ['DataAccessLayer.Models'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string GetFullName(EmployeeModel? model)` — implementation in file; inspect branches/side-effects in source.
  - `public string TextOrDash(string? value)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Home/HomeViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Home'].
- Main declared type(s): ['class HomeViewModel', 'class WhoWorksTodayRowViewModel', 'class HomeScheduleCardViewModel', 'class HomeScheduleEntryViewModel'].

### Dependencies
- Key imports/usings: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Media'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.ScheduleMatrix', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Container.Edit.Helpers', 'WPFApp.ViewModel.Container.ScheduleEdit.Helpers'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Data', 'System.Globalization', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Media', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<WhoWorksTodayRowViewModel> WhoWorksTodayItems`
  - `public ObservableCollection<HomeScheduleCardViewModel> ActiveSchedules`
  - `public AsyncRelayCommand RefreshCommand`
  - `public string CurrentMonthContainerName`
  - `public string CurrentMonthLabel`
  - `public ObservableCollection<string> CurrentMonthScheduleNames`
  - `public int CurrentMonthTotalEmployees`
  - `public int CurrentMonthTotalSchedules`
  - `public double CurrentMonthTotalHours`
  - `public int CurrentMonthTotalShops`
  - `public int OverallTotalEmployees`
  - `public int OverallTotalContainers`
  - `public int OverallTotalShops`
  - `public string CurrentTimeText`
  - `public string StatusText`
  - `public ObservableCollection<string> CurrentMonthShopNames`
  - `public ObservableCollection<string> CurrentMonthEmployeeNames`
  - `public bool IsLoading`
  - `public int MonthSchedulesCount`
  - `public int TotalContainersCount`
  - `public int TodayAssignmentsCount`
  - `public int ActiveShopsCount`
  - `public bool IsNavStatusVisible`
  - `public UIStatusKind NavStatus`
  - `public DateTime Date`
- Methods/constructors (detected signatures):
  - `public Task EnsureInitializedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private CancellationToken ResetNavUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task RunOnUiThreadAsync(Action a)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideNavStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 700)` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadDataAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateCurrentTimeText()` — implementation in file; inspect branches/side-effects in source.
  - `private void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Task RefreshWithOverlayAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `public void ApplyBuild(DataView view,
            Dictionary<string, int> colMap,
            IList<ScheduleCellStyleModel> styles,
            Dictionary<int, string> perEmployeeTotals,
            string totalHoursText)` — implementation in file; inspect branches/side-effects in source.
  - `public string GetEmployeeTotalHoursText(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsTechnicalMatrixColumn(string columnName)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef)` — implementation in file; inspect branches/side-effects in source.
  - `public bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style)` — implementation in file; inspect branches/side-effects in source.
  - `private Brush ToBrushCached(int argb)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 11 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Information/InformationViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Information'].
- Main declared type(s): ['class InformationViewModel'].

### Dependencies
- Key imports/usings: ['WPFApp.Infrastructure', 'WPFApp.ViewModel'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- No public members detected or members are expression-bodied/partial-generated.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Main/MainViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Main'].
- Main declared type(s): ['class MainViewModel'].

### Dependencies
- Key imports/usings: ['System.Threading', 'System.Threading.Tasks', 'System.Windows', 'Microsoft.Extensions.DependencyInjection', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Main', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Employee', 'WPFApp.ViewModel.Shop', 'WPFApp.ViewModel.Database', 'WPFApp.ViewModel.Home', 'WPFApp.ViewModel.Information'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.ViewModel.Availability.Main', 'WPFApp.ViewModel.Container.Edit', 'WPFApp.ViewModel.Employee', 'WPFApp.ViewModel.Shop', 'WPFApp.ViewModel.Database', 'WPFApp.ViewModel.Home', 'WPFApp.ViewModel.Information'].
- External dependencies: ['System.Threading', 'System.Threading.Tasks', 'System.Windows', 'Microsoft.Extensions.DependencyInjection'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public NavPage ActivePage`
  - `public object? CurrentViewModel`
  - `public bool IsBusy`
  - `public string? BusyText`
  - `public AsyncRelayCommand ShowHomeCommand`
  - `public AsyncRelayCommand ShowEmployeeCommand`
  - `public AsyncRelayCommand ShowShopCommand`
  - `public AsyncRelayCommand ShowAvailabilityCommand`
  - `public AsyncRelayCommand ShowContainerCommand`
  - `public AsyncRelayCommand ShowInformationCommand`
  - `public AsyncRelayCommand ShowDatabaseCommand`
  - `public AsyncRelayCommand CloseCommand`
  - `public AsyncRelayCommand MinimizeCommand`
- Methods/constructors (detected signatures):
  - `private AsyncRelayCommand CreateNavCommand(NavPage page,
            string busyText,
            Func<object> getOrCreateVm,
            Func<bool> isEnabled)` — implementation in file; inspect branches/side-effects in source.
  - `private Task NavigateAsync(NavPage page, string busyText, Func<object> getOrCreateVm)` — implementation in file; inspect branches/side-effects in source.
  - `private Task EnsureInitializedIfNeededAsync(object viewModel, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private Task RunBusyAsync(Func<Task> action, string? text, CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `private void RaiseNavStateProperties()` — implementation in file; inspect branches/side-effects in source.
  - `private void RaiseNavCanExecute()` — implementation in file; inspect branches/side-effects in source.
  - `public void Dispose()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 6 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Main/NavPage.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Main'].
- Main declared type(s): ['enum NavPage'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Text'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Collections.Generic', 'System.Text'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum NavPage`

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Shop/Helpers/ShopDisplayHelper.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Shop.Helpers'].
- Main declared type(s): ['class ShopDisplayHelper'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string TextOrDash(string? value)` — implementation in file; inspect branches/side-effects in source.
  - `public string NameOrEmpty(ShopModel? model)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Shop/ShopEditViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Shop'].
- Main declared type(s): ['class ShopEditViewModel'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Windows', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- Internal dependencies: ['WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.ViewModel.Dialogs'].
- External dependencies: ['System', 'System.Collections', 'System.Collections.Generic', 'System.ComponentModel', 'System.Linq', 'System.Text', 'System.Threading.Tasks', 'System.Windows'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int ShopId`
  - `public string Name`
  - `public string Address`
  - `public string Description`
  - `public bool IsEdit`
  - `public AsyncRelayCommand SaveCommand`
  - `public AsyncRelayCommand CancelCommand`
- Methods/constructors (detected signatures):
  - `public IEnumerable GetErrors(string? propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `public void ResetForNew()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetShop(ShopModel model)` — implementation in file; inspect branches/side-effects in source.
  - `public ShopModel ToModel()` — implementation in file; inspect branches/side-effects in source.
  - `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `public void ClearValidationErrors()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearValidationErrors(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private void ValidateProperty(string propertyName)` — implementation in file; inspect branches/side-effects in source.
  - `private bool ValidateBeforeSave(bool showDialog = true)` — implementation in file; inspect branches/side-effects in source.
  - `private string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SaveWithValidationAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private string MapValidationKeyToVm(string key)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 6 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Shop/ShopListViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Shop'].
- Main declared type(s): ['class ShopListViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq', 'DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure'].
- External dependencies: ['System.Collections.Generic', 'System.Collections.ObjectModel', 'System.Linq'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public ObservableCollection<ShopModel> Items`
  - `public ShopModel? SelectedItem`
  - `public string SearchText`
  - `public AsyncRelayCommand SearchCommand`
  - `public AsyncRelayCommand AddNewCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
  - `public AsyncRelayCommand OpenProfileCommand`
- Methods/constructors (detected signatures):
  - `public void SetItems(IEnumerable<ShopModel> shops)` — implementation in file; inspect branches/side-effects in source.
  - `private bool IsSameList(IList<ShopModel> current, IList<ShopModel> next)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateSelectionCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 2 private methods (regex-detected).
- Manages observable UI state collections/properties.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Shop/ShopProfileViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Shop'].
- Main declared type(s): ['class ShopProfileViewModel'].

### Dependencies
- Key imports/usings: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Shop.Helpers'].
- Internal dependencies: ['DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.ViewModel.Shop.Helpers'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public int ShopId`
  - `public string Name`
  - `public string Address`
  - `public string Description`
  - `public AsyncRelayCommand BackCommand`
  - `public AsyncRelayCommand CancelProfileCommand`
  - `public AsyncRelayCommand EditCommand`
  - `public AsyncRelayCommand DeleteCommand`
- Methods/constructors (detected signatures):
  - `public void SetProfile(ShopModel model)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpdateCommands()` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/ViewModel/Shop/ShopViewModel.cs
### Purpose
- MVVM ViewModel orchestrating UI state, commands, validation, and service calls.
- Supports: UI.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WPFApp.ViewModel.Shop'].
- Main declared type(s): ['enum ShopSection', 'class ShopViewModel'].

### Dependencies
- Key imports/usings: ['System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading', 'BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs'].
- Internal dependencies: ['BusinessLogicLayer.Services.Abstractions', 'DataAccessLayer.Models', 'WPFApp.Infrastructure', 'WPFApp.Infrastructure.Validation', 'WPFApp.Service', 'WPFApp.View.Dialogs', 'WPFApp.ViewModel.Dialogs', 'WPFApp.ViewModel.Shop.Helpers'].
- External dependencies: ['System.Collections.Generic', 'System.Linq', 'System.Threading', 'System.Threading.Tasks', 'System.Windows', 'System.Windows.Threading'].
- Coupling focus: primarily UI.

### Public API / Members
- Public/visible properties (detected):
  - `public enum ShopSection`
  - `public bool IsNavStatusVisible`
  - `public UIStatusKind NavStatus`
  - `public object CurrentSection`
  - `public ShopSection Mode`
  - `public ShopSection CancelTarget`
  - `public ShopListViewModel ListVm`
  - `public ShopEditViewModel EditVm`
  - `public ShopProfileViewModel ProfileVm`
- Methods/constructors (detected signatures):
  - `private CancellationToken ResetNavUiCts(CancellationToken outer)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavWorkingAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task HideNavStatusAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task ShowNavSuccessThenAutoHideAsync(CancellationToken ct, int ms = 900)` — implementation in file; inspect branches/side-effects in source.
  - `public Task EnsureInitializedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `private Task InitializeCoreAsync(CancellationToken ct)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SearchAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task StartAddAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task EditSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task SaveAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task DeleteSelectedAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task OpenProfileAsync(CancellationToken ct = default)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task CancelAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task LoadShopsAsync(CancellationToken ct, int? selectId)` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToListAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToEditAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private Task SwitchToProfileAsync()` — implementation in file; inspect branches/side-effects in source.
  - `private int GetCurrentShopId()` — implementation in file; inspect branches/side-effects in source.
  - `private void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private Task ReloadAfterDatabaseChangeAsync(string source)` — implementation in file; inspect branches/side-effects in source.
  - `internal Task RunOnUiThreadAsync(Action action)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowInfo(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(string text)` — implementation in file; inspect branches/side-effects in source.
  - `internal void ShowError(Exception ex)` — implementation in file; inspect branches/side-effects in source.
  - `private bool Confirm(string text, string? caption = null)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 13 private methods (regex-detected).
- Async behavior present (`async/await` keywords found).
- Task-based asynchronous operations used.
- Explicit exception throwing/propagation exists.
- Contains validation-related logic or rules.

### Data Flow
- Inputs: UI commands/events, bound form values, injected services.
- Outputs: property updates, validation errors, service calls, and navigation/dialog triggers.
- Next stage: views re-render through bindings; data persisted via services.

### Coupling / Layer Notes
- Layer classification: UI.
- Observed layer leakage: UI references DAL models directly.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WPFApp/WPFApp.csproj
### Purpose
- Build/solution configuration file declaring projects, references, packages, and target frameworks.
- Supports: UI.

### Type Information
- File type: `.csproj`.
- Namespace(s): ['N/A'].
- Main declared type(s): ['No type declarations (config/XAML/doc or partial companion)'].

### Dependencies
- Key imports/usings: ['None/implicit'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['Framework base only/none'].
- Coupling focus: primarily UI.

### Public API / Members
- N/A for configuration/docs/binary assets.

### Internal Logic Details
- Internal logic is declarative configuration or narrative documentation.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: UI.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WinFormsApp/View/Container/ContainerView.ScheduleStyles.cs
### Purpose
- C# source file containing application logic.
- Supports: Solution/documentation.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WinFormsApp.View.Container'].
- Main declared type(s): ['class ContainerView'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Data', 'System.Diagnostics', 'System.Drawing', 'System.Linq', 'System.Windows.Forms', 'WinFormsApp.View.Shared'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Data', 'System.Diagnostics', 'System.Drawing', 'System.Linq', 'System.Windows.Forms', 'WinFormsApp.View.Shared'].
- Coupling focus: primarily n/a.

### Public API / Members
- Methods/constructors (detected signatures):
  - `private void InitializeScheduleStyleMenu()` — implementation in file; inspect branches/side-effects in source.
  - `private void SlotGrid_MouseDown(object? sender, MouseEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyColorToSelection(bool isBackground)` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearStylesForSelection()` — implementation in file; inspect branches/side-effects in source.
  - `private void ClearAllStyles()` — implementation in file; inspect branches/side-effects in source.
  - `private void RefreshScheduleStyles()` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryGetCellStyleKey(DataGridViewCell cell, out int day, out int empId)` — implementation in file; inspect branches/side-effects in source.
  - `private void UpsertCellStyle(int day,
            int empId,
            string? backgroundHex,
            string? foregroundHex,
            bool replaceNulls)` — implementation in file; inspect branches/side-effects in source.
  - `private void RebuildCellStyleLookup()` — implementation in file; inspect branches/side-effects in source.
  - `private ScheduleCellStyleModel? GetOverrideStyle(int day, int empId)` — implementation in file; inspect branches/side-effects in source.
  - `private void ScheduleGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)` — implementation in file; inspect branches/side-effects in source.
  - `private void ApplyScheduleCellStyle(DataGridView grid,
            int rowIndex,
            int columnIndex,
            DataGridViewCellStyle cellStyle)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 12 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WinFormsApp/View/Shared/BusyForm.cs
### Purpose
- C# source file containing application logic.
- Supports: Solution/documentation.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WinFormsApp.View.Shared'].
- Main declared type(s): ['class BusyForm'].

### Dependencies
- Key imports/usings: ['System', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Forms'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Collections.Generic', 'System.Threading', 'System.Threading.Tasks', 'System.Windows.Forms'].
- Coupling focus: primarily n/a.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public void ShowBusy(string? text = null)` — implementation in file; inspect branches/side-effects in source.
  - `public void HideBusy()` — implementation in file; inspect branches/side-effects in source.
  - `public Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? text = null)` — implementation in file; inspect branches/side-effects in source.
  - `public Task RunBusyAsync(Func<CancellationToken, IProgress<int>?, Task> action, CancellationToken ct, string? text = null)` — implementation in file; inspect branches/side-effects in source.
  - `protected void SetBusyState(bool enabled)` — implementation in file; inspect branches/side-effects in source.
  - `protected void Dispose(bool disposing)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).
- Task-based asynchronous operations used.

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WinFormsApp/View/Shared/ColorHexConverter.cs
### Purpose
- C# source file containing application logic.
- Supports: Solution/documentation.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WinFormsApp.View.Shared'].
- Main declared type(s): ['class ColorHexConverter'].

### Dependencies
- Key imports/usings: ['System.Globalization'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System.Globalization'].
- Coupling focus: primarily n/a.

### Public API / Members
- Methods/constructors (detected signatures):
  - `public string ToHex(Color color)` — implementation in file; inspect branches/side-effects in source.
  - `public Color? FromHex(string? hex)` — implementation in file; inspect branches/side-effects in source.
  - `private bool TryParseByte(ReadOnlySpan<char> value, out byte result)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 1 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

## WinFormsApp/View/Shared/ScheduleCellStyleResolver.cs
### Purpose
- C# source file containing application logic.
- Supports: Solution/documentation.

### Type Information
- File type: `.cs`.
- Namespace(s): ['WinFormsApp.View.Shared'].
- Main declared type(s): ['class ScheduleCellStyleResolver'].

### Dependencies
- Key imports/usings: ['System', 'System.Drawing'].
- Internal dependencies: ['Not explicit or same-namespace'].
- External dependencies: ['System', 'System.Drawing'].
- Coupling focus: primarily n/a.

### Public API / Members
- Public/visible properties (detected):
  - `public Color WeekdayBackground`
  - `public Color WeekendBackground`
  - `public Color DefaultForeground`
- Methods/constructors (detected signatures):
  - `public Color ResolveBackground(ScheduleCellStyleModel? overrideStyle, bool isWeekend)` — implementation in file; inspect branches/side-effects in source.
  - `public Color ResolveForeground(ScheduleCellStyleModel? overrideStyle)` — implementation in file; inspect branches/side-effects in source.
  - `public bool IsWeekend(int year, int month, int day)` — implementation in file; inspect branches/side-effects in source.

### Internal Logic Details
- Private/internal routines detected: 0 private methods (regex-detected).

### Data Flow
- Data flow is contextual to file purpose (config/docs).

### Coupling / Layer Notes
- Layer classification: n/a.
- Responsibility mix inferred from file name and detected members; partial classes may split responsibilities across companion files.

### Related Files
- Closely related files: ['Inferred by namespace/project (see sibling folders)'].

### Summary
- Captures current AS-IS behavior/structure in its layer context.
- Key declarations and dependencies extracted above.
- Requires companion files (partial classes/resources/interfaces) for full runtime behavior in many cases.

# XAML / UI STRUCTURE (if WPF present)
- Views are under `WPFApp/Views/**` and mapped to feature-centric ViewModels via DataContext assignment in code-behind, templates, or navigation host ViewModel.
- Resource dictionaries under `WPFApp/Resources/**` define theme, buttons, textboxes, datagrids, icons, container schedule resources, and view templates.
- Converters/validation helpers include first-error converters and validation rule classes in `WPFApp/Infrastructure/Validation/**`.
- Commands are exposed from ViewModels using `RelayCommand`/`AsyncRelayCommand` and bound in XAML through `{Binding ...}` command bindings.
- Navigation appears to be ViewModel-driven (`MainViewModel`, home/profile/list/edit VMs) with content control templates linking VM types to views.

# VIEWMODEL MAP (if MVVM is used)
| ViewModel | Associated View (inferred) | Responsibilities | Commands/Actions | Services/Models used | Notes |
|---|---|---|---|---|---|
| AvailabilityEditViewModel.Batching | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel.Binds | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel.Commands | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel.Fields | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel.Matrix | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel.Validation | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityEditViewModel | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityListDesignViewModel | Not directly explicit | UI state orchestration for DesignTime feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityListViewModel | Not directly explicit | UI state orchestration for List feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityProfileViewModel | Not directly explicit | UI state orchestration for Profile feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Binds | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.DatabaseReload | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Employees | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Groups | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Initialization | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.NavStatus | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Navigation | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel.Ui | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| AvailabilityViewModel | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerEditViewModel | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerListViewModel | Not directly explicit | UI state orchestration for List feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerProfileViewModel | Not directly explicit | UI state orchestration for Profile feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.CellStyling | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.Lookups | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.MatrixBinding | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.MatrixEditAndRefresh | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.ModelBinding | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.Selection | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.Totals | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel.Validation | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleEditViewModel | Not directly explicit | UI state orchestration for ScheduleEdit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleListViewModel | Not directly explicit | UI state orchestration for ScheduleList feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerScheduleProfileViewModel | Not directly explicit | UI state orchestration for ScheduleProfile feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.AvailabilityPreview | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Containers | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.DatabaseReload | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Export | Not directly explicit | UI state orchestration for Helpers feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Lookups | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Navigation | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.ScheduleEditor | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Schedules.BlockFactory | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Schedules.Open | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Schedules.SaveGenerate | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel.Ui | Not directly explicit | UI state orchestration for Helpers feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ContainerViewModel | Not directly explicit | UI state orchestration for Edit feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| CustomMessageBoxViewModel | Not directly explicit | UI state orchestration for Dialogs feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| DatabaseViewModel | Not directly explicit | UI state orchestration for Database feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| EmployeeEditViewModel | Not directly explicit | UI state orchestration for Employee feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| EmployeeListViewModel | Not directly explicit | UI state orchestration for Employee feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| EmployeeProfileViewModel | Not directly explicit | UI state orchestration for Employee feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| EmployeeViewModel | Not directly explicit | UI state orchestration for Employee feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| HomeViewModel | Not directly explicit | UI state orchestration for Home feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| InformationViewModel | Not directly explicit | UI state orchestration for Information feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| MainViewModel | Not directly explicit | UI state orchestration for Main feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ScheduleBlockViewModel | Not directly explicit | UI state orchestration for Helpers feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ShopEditViewModel | Not directly explicit | UI state orchestration for Shop feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ShopListViewModel | Not directly explicit | UI state orchestration for Shop feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ShopProfileViewModel | Not directly explicit | UI state orchestration for Shop feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ShopViewModel | Not directly explicit | UI state orchestration for Shop feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |
| ViewModelBase | Not directly explicit | UI state orchestration for Infrastructure feature | Relay/async commands in class/partials | BLL services + DAL models (inferred from usings) | Many VMs split via partial files; can be large responsibility surface |

# BUSINESS LOGIC MAP
- Core services in `BusinessLogicLayer/Services/**` implement abstractions for employee, schedule, slot, container, bind, availability group, and shop operations.
- `ScheduleGenerator` (under `Generators`) is central generation engine behind schedule creation workflows.
- Availability-specific domain logic lives in `BusinessLogicLayer/Availability/*` (`AvailabilityCodeParser`, `AvailabilityGroupValidator`, `AvailabilityPayloadBuilder`).
- `GenericService<T>` appears to provide reusable CRUD scaffolding over repository interfaces.
- BLL workflows generally delegate to DAL repositories, performing validation/mapping/aggregation where needed.

# DATA ACCESS MAP
- `AppDbContext` and `AppDbContextFactory` configure EF Core SQLite persistence and design-time context creation.
- Entity models in `DataAccessLayer/Models/**` include Employee, Schedule, Slot, Shop, Container, Bind, AvailabilityGroup(+Day/+Member), and cell style entities.
- Repositories implement per-aggregate query/update operations with base abstractions in `Repositories/Abstractions` and generic shared implementation in `GenericRepository`.
- Migrations folder contains chronological schema changes plus designer snapshots; `AppDbContextModelSnapshot` tracks current model state.
- Data flow: DbContext <-> repositories <-> BLL services <-> WPF ViewModels/UI.

# CROSS-CUTTING COMPONENTS
- Logging and notifications: `DiagnosticsServiceImpl`, `DatabaseChangeNotifier`, and custom message box service/viewmodel components.
- Commands/observability: `ObservableObject`, `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand`.
- Validation: rule sets for schedule/container/shop/employee/availability, plus error aggregation/converters.
- Threading/debounce: `UiDebouncedAction` to prevent excessive UI-triggered work.
- Export/import/configuration: `ScheduleExportService`, `DatabasePathProvider`, Excel template resources.
- Parsing/matrix engines: availability/schedule matrix engines and parser helpers for code-to-grid transformations.

# DEPENDENCY & INTERACTION ANALYSIS
- Project reference graph: `WPFApp -> BusinessLogicLayer -> DataAccessLayer`, and `WPFApp -> DataAccessLayer` directly.
- Central hubs: `AppDbContext` (persistence), repository abstractions/implementations, major feature ViewModels (`ContainerViewModel`, `ContainerScheduleEditViewModel`, `AvailabilityEditViewModel`), and `ScheduleGenerator`.
- Widely used services: CRUD services for entities, schedule slot/schedule employee services, export and validation utilities.
- Potential circularity risk is low at project level (one-direction references), but conceptual coupling is high because DAL models flow into upper layers.
- UI/business/data boundaries exist but are porous: UI often manipulates DAL entity types and business services may pass entities through without anti-corruption mapping.
- Hidden patterns: partial-class decomposition by feature scenario (e.g., `.Commands`, `.Validation`, `.MatrixBinding`) acts as internal modularization inside large ViewModels.

# CURRENT ARCHITECTURAL ISSUES (OBSERVATIONAL ONLY)
- **Layer leakage (high):** UI and BLL both frequently reference `DataAccessLayer.Models` directly, reducing separation and making data schema changes ripple upward.
- **Large ViewModel surfaces (high):** Several feature ViewModels are split across many partial files, indicating high responsibility concentration and complexity.
- **Repository/service thin abstraction (medium):** Many services appear CRUD-forwarding wrappers around repositories, giving limited domain encapsulation.
- **Schema churn (medium):** Numerous migrations suggest frequent model adjustments; risk of drift between intended domain and persistence concerns.
- **Mixed concerns in UI layer (medium):** Export, DB admin, and infrastructure logic coexist in WPF project alongside presentation concerns.
- **Naming/organization inconsistency (low-medium):** Mixed singular/plural and feature folder conventions; some helper/util classes not uniformly grouped.

# REFACTORING PREPARATION NOTES (NO REFACTOR PLAN YET)
- Well-organized areas: DAL repository abstractions vs implementations; WPF feature folders (`Availability`, `Container`, `Shop`).
- Mixed folders: WPF `Infrastructure` and `Service` include both pure UI helpers and broader application/infrastructure concerns.
- Candidate boundaries to evaluate later: UI (WPF views/viewmodels), Application services/orchestration, Domain logic/validation/generation, Infrastructure persistence/export/logging.
- Constraints inferred: WPF binding requires stable property names/INPC behavior; EF Core model/migrations tightly tied to current entities; export templates expect specific matrix layouts.
- Open questions: intended domain model independence from EF entities, navigation framework conventions, and whether repositories are meant for unit-of-work boundaries or simple data access wrappers.

# GLOSSARY OF IMPORTANT TYPES
## DataAccessLayer
- `AppDbContext`: EF Core database context exposing DbSet mappings and model configuration.
- `GenericRepository<T>`: Base repository with reusable CRUD/query operations.
- `ScheduleModel` / `ScheduleSlotModel` / `ScheduleEmployeeModel`: Core schedule persistence entities.
- `AvailabilityGroupModel`, `AvailabilityGroupDayModel`, `AvailabilityGroupMemberModel`: Availability grouping entities.
## BusinessLogicLayer
- `ScheduleGenerator`: Core schedule generation engine/service.
- `AvailabilityCodeParser`: Parses encoded availability expressions into structured forms.
- `AvailabilityGroupValidator`: Validates availability grouping constraints/business rules.
- `GenericService<T>`: Base service abstraction over repositories.
## WPFApp
- `MainViewModel`: Main shell/navigation state holder for the application UI.
- `ContainerViewModel`: Complex VM coordinating container editing, schedules, preview, export, and persistence.
- `ContainerScheduleEditViewModel`: Detailed schedule matrix editing VM with validation/totals/styling logic.
- `AvailabilityEditViewModel`: Availability matrix editing VM with binds/groups/validation batching logic.
- `RelayCommand` / `AsyncRelayCommand`: ICommand implementations used across ViewModels.
- `ScheduleMatrixEngine` / `AvailabilityMatrixEngine`: UI matrix computation components.