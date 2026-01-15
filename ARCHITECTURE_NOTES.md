# Repository Architecture Notes

## Solution layout
- **WPFApp**: WPF UI using MVVM. Views are XAML/Code-behind, view models handle list/edit/profile navigation, and UI dialogs are implemented via `CustomMessageBox`. Data grids are wired with WPF events such as `CellEditEnding` and `PreviewKeyDown` for data-entry workflows.
- **BusinessLogicLayer**: Application services (e.g., Employee, Shop, Availability, Schedule) extend a generic service layer and enforce domain-level validation.
- **DataAccessLayer**: Entity Framework Core (SQLite) with `AppDbContext`, entity models, migrations, and repository implementations used by the services.
- **WinFormsApp**: Legacy/parallel UI with presenters; tests live in `WinFormsApp.Presentation.Tests`.

## Key architectural patterns
- **MVVM** in WPF: `EmployeeViewModel`, `ShopViewModel`, `AvailabilityViewModel`, `ContainerViewModel` each manage List/Edit/Profile sections, using `AsyncRelayCommand` for operations.
- **Repository + Service pattern**: Repositories abstract EF Core queries; services perform normalization/validation and coordinate business logic.
- **Validation**: Simple UI validation lives in WPF view models (required fields, formatting). Services perform additional normalization and guardrails (e.g., duplicate checks).
- **DataGrid editing**: Availability and schedule grids are built from `DataTable`/`DataView` sources; editing events are handled in code-behind (`CellEditEnding`, `PreviewKeyDown`).

## Planned fix touchpoints
- **Delete safeguards + uniqueness**: Service-layer checks in `EmployeeService`, `ShopService`, and `AvailabilityGroupService`, backed by repository queries.
- **Post-failure recovery**: Repository save operations reset EF change tracking after failures to avoid invalid state leaks.
- **Time normalization**: WPF Availability and schedule grid edits normalize `hh:mm-hh:mm` into `hh:mm - hh:mm` in the appropriate edit-commit handlers.
- **Error visibility**: Enhanced `CustomMessageBox` dialog with optional “Details” expander for exception chains.
