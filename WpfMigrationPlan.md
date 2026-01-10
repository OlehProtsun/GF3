# WPF Migration Analysis & Plan

## Phase 1 — WinForms Presentation Analysis

### 1) Structure & Pattern
- **Pattern:** MVP-like with passive views. Views expose state + events via interfaces (`IEmployeeView`, `IShopView`, `IAvailabilityView`, `IContainerView`, `IMainView`) and presenters perform orchestration, validation, and service calls. Presenters own workflow logic and update the view or binding sources.【F:WinFormsApp/View/Employee/IEmployeeView.cs†L1-L44】【F:WinFormsApp/View/Container/IContainerView.cs†L1-L90】【F:WinFormsApp/Presenter/Employee/EmployeePresenter.cs†L1-L40】
- **View responsibilities:** expose UI state (mode, fields, selected IDs), raise events for user actions, provide UI helpers (switch modes, show dialogs, set validation errors, update binding sources). Example: Employee view defines CRUD events and binding source setup for list data.【F:WinFormsApp/View/Employee/IEmployeeView.cs†L1-L44】
- **Presenter responsibilities:** subscribe to view events, run validations, call BLL services, update binding sources or view fields, manage mode transitions, and handle busy overlays/errors. Example: `ContainerPresenter` wires the view events, uses BLL services, and toggles list/edit/profile modes for container & schedule flows.【F:WinFormsApp/Presenter/Container/ContainerPresenter.cs†L15-L120】
- **Dependency creation / DI:** WinForms `Program.cs` builds a `ServiceCollection`, registers BLL/DAL + presenters, and creates views with presenters injected. Each view is constructed with its presenter and `InitializeAsync()` called to load data, showing a consistent DI setup that can be mirrored in WPF.【F:WinFormsApp/Program.cs†L1-L79】
- **Navigation:** `MainPresenter` embeds child forms into a content panel and uses `IMdiViewFactory` to create forms on demand. It sets active page styles and toggles which form is shown in the host panel (single-container navigation).【F:WinFormsApp/Presenter/MainPresenter.cs†L15-L80】

### 2) UI/UX Design System (WinForms)
- **Left navigation bar:** fixed width panel with white background and a rounded group box (border color `#E0E0E0`, radius 15). Nav buttons are 39x39 with rounded corners and shadow. Active nav button fill is dark gray; inactive fill is white. Fonts include `Segoe UI` and `Corbel` for captions; label text uses gray/black. These styles are consistent in the Main view designer code.【F:WinFormsApp/View/Main/MainView.Designer.cs†L35-L200】
- **Guna2 usage:** uses `Guna2Button`, `Guna2GroupBox`, `Guna2Shapes` and shadow decoration to create modern, rounded, card-like UI. Button fills and disabled-state colors are customized, consistent with active/inactive navigation state changes.【F:WinFormsApp/View/Main/MainView.Designer.cs†L120-L210】
- **Repeated layout patterns:** list + edit/profile panels appear across Employee, Shop, Availability, and Container views (via `Mode` enums and SwitchTo* methods). This suggests reusable card/panel components in WPF for consistent list/detail layout and validation displays.【F:WinFormsApp/View/Employee/IEmployeeView.cs†L10-L41】【F:WinFormsApp/View/Container/IContainerView.cs†L10-L88】
- **Interaction cues:** selection highlighting via active nav styles, busy overlay display via `IBusyView`, and consistent confirmation/alert patterns (`ShowInfo`, `ShowError`, `Confirm`).【F:WinFormsApp/View/Shared/IBusyView.cs†L1-L15】【F:WinFormsApp/View/Employee/IEmployeeView.cs†L26-L41】

### 3) State Management
- **Modes:** Each screen uses an explicit mode enum (`List`, `Edit`, `Profile`) with `CancelTarget` to return to the previous view state. Container has additional schedule modes (`ScheduleEdit`, `ScheduleProfile`) and separate schedule cancellation logic.【F:WinFormsApp/ViewModel/EmployeeViewModel.cs†L1-L10】【F:WinFormsApp/ViewModel/ShopViewModel.cs†L1-L10】【F:WinFormsApp/ViewModel/AvailabilityViewModel.cs†L1-L10】【F:WinFormsApp/ViewModel/ContainerViewModel.cs†L1-L5】【F:WinFormsApp/View/Container/IContainerView.cs†L10-L75】
- **Binding & selection:** WinForms uses `BindingSource` for list data, with presenters reading `BindingSource.Current` to determine selection (`CurrentEmployee`, `CurrentContainer`, `CurrentSchedule`). This defines the selection flow and should be mapped to `ObservableCollection` + `SelectedItem` in WPF.【F:WinFormsApp/Presenter/Container/ContainerPresenter.cs†L68-L96】【F:WinFormsApp/Presenter/Employee/EmployeePresenter.Mapping.cs†L8-L21】
- **Validation:** Presenters validate models and pass a dictionary of field errors to the view (`SetValidationErrors`). E.g., Employee validation enforces required names and email/phone format, which should map to `INotifyDataErrorInfo` in WPF for parity.【F:WinFormsApp/Presenter/Employee/EmployeePresenter.Validation.cs†L9-L29】【F:WinFormsApp/View/Employee/IEmployeeView.cs†L24-L41】
- **Confirmation/message box patterns:** Views expose `Confirm`, `ShowInfo`, and `ShowError` methods; presenters call them during CRUD operations to confirm deletions or report errors.【F:WinFormsApp/View/Employee/IEmployeeView.cs†L26-L41】

### 4) Data Flow (WinForms → BLL → DAL)
- **Services per screen:**
  - Employee → `IEmployeeService` for CRUD operations.【F:WinFormsApp/Presenter/Employee/EmployeePresenter.cs†L10-L31】
  - Shop → `IShopService` for CRUD operations.【F:WinFormsApp/Presenter/Shop/ShopPresenter.cs†L10-L31】
  - Availability → `IAvailabilityGroupService`, `IEmployeeService`, `IBindService` for group and bind management.【F:WinFormsApp/Presenter/Availability/AvailabilityPresenter.cs†L10-L40】
  - Container → `IContainerService`, `IScheduleService`, `IAvailabilityGroupService`, `IShopService`, `IEmployeeService`, `IScheduleGenerator` for container/schedule operations and generation.【F:WinFormsApp/Presenter/Container/ContainerPresenter.cs†L15-L55】
- **Async patterns:** Presenters run load and save logic via `RunBusyAsync` and `SafeAsync` patterns for cancellation and error handling, indicating a consistent busy overlay workflow to replicate in WPF.【F:WinFormsApp/Presenter/Container/ContainerPresenter.cs†L62-L120】

### 5) Entity Framework & DAL
- **EF Context:** `AppDbContext` is registered in DAL extensions with SQLite via `AddDbContext`, and repositories are injected through service interfaces. WPF must reuse the same BLL/DAL registrations, not duplicate logic.【F:DataAccessLayer/Models/DataBaseContext/Extensions.cs†L10-L36】

## Phase 1 Deliverable — Migration Plan + Screen Map + Style Guide

### Screen/Module Map
- **Main shell:** `MainView` (left nav + content host) embeds views inside `PanelContentMain` via `MainPresenter`.
- **Employee:** `EmployeeView` with list/edit/profile modes.
- **Shop:** `ShopView` with list/edit/profile modes.
- **Availability:** `AvailabilityView` with list/edit/profile modes and group/employee binding management.
- **Container:** `ContainerView` with list/edit/profile and schedule list/edit/profile, plus schedule block management and availability preview matrix.

### Style Guide Mapping (WinForms → WPF)
- **Surface/background:** white panels with light gray borders (#E0E0E0).
- **Navigation:** active tab/button fill dark gray; inactive white. Buttons are square (39x39) with rounded corners and shadowed cards.
- **Typography:** `Segoe UI` for body, `Corbel` for caption text, gray label color for secondary labels.
- **Cards:** rounded, shadowed group boxes with consistent padding.

### Migration Plan (Iterative)
1. **Shell + theme:** implement WPF `MainWindow` with left nav, busy overlay, and global styles that mirror WinForms (white surfaces, rounded cards, dark gray active nav).
2. **Port one screen:** migrate Employee list/edit/profile end-to-end with validation and CRUD using existing BLL services.
3. **Port remaining screens:** Shop, Availability, Container + Schedule flows, reusing shared components and validation patterns.
4. **Parity checks:** replicate confirmation dialogs, enable/disable logic, navigation state, and highlight behaviors.

## Phase 2 — WPF Architecture Decision
- **Chosen pattern:** MVVM (ViewModels + Commands + DataBinding). This aligns with WPF strengths while still consuming the same BLL services as presenters. Presenter logic is not duplicated; instead, ViewModels call the same BLL APIs used in WinForms. This keeps business logic centralized and maintains layered architecture consistency.

## Phase 3 — Implementation Requirements Mapping
- **Project:** New WPF project `GF3.Presentation.Wpf` added to solution; WinForms remains unchanged.
- **DI composition root:** `App.xaml.cs` mirrors WinForms DI registration for DAL/BLL and registers ViewModels + dialog services.
- **Navigation/shell:** `MainWindow` provides left navigation and a content host for page ViewModels.
- **Reusable components:** WPF `UserControl` card components (`InfoCardControl`, `EmployeeCardControl`, `NoteCardControl`) to standardize UI blocks.
- **Binding/validation:** ViewModels use `ObservableCollection`, `ICommand`, and `INotifyDataErrorInfo` for parity with WinForms validation behavior.
- **Mapping doc:** See next section for component mapping.

## Component Mapping (WinForms → WPF)
- **MainView → MainWindow** (left nav + content host)
- **EmployeeView + EmployeePresenter → EmployeePage + EmployeePageViewModel**
- **ShopView + ShopPresenter → ShopPage + ShopPageViewModel**
- **AvailabilityView + AvailabilityPresenter → AvailabilityPage + AvailabilityPageViewModel**
- **ContainerView + ContainerPresenter → ContainerPage + ContainerPageViewModel**
- **Busy overlay (BusyOverlayController) → WPF busy overlay bound to `IsBusy`/`BusyText`**
- **Dialogs (ShowInfo/ShowError/Confirm) → `IMessageDialogService`**

## Phase 4 — Parity Checklist
- Navigation flows match `MainPresenter` behavior (active page highlights, only one view visible).
- CRUD actions across Employee/Shop/Availability/Container behave identically.
- Validation rules mirror presenter validation dictionaries.
- Confirmation dialogs for delete/cancel show same conditions.
- Search/filter flows mirror WinForms search events.
- Schedule generation, schedule block management, and availability preview behavior match ContainerView.
