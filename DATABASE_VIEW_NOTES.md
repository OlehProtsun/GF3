# Database View Notes

## Architecture + UI Style Summary

- **App startup / composition root**: `WPFApp/App.xaml.cs` builds a Generic Host, registers DAL/BLL/services/ViewModels/Views, then resolves `MainWindow` from DI. SQLite DB path is under `%LocalAppData%/GF3/SQLite.db` and connection string is passed to DAL registration.
- **Navigation shell**: `MainViewModel` drives module navigation via `CurrentViewModel` and async commands (`ShowEmployeeCommand`, etc.). `MainWindow.xaml` binds nav buttons to these commands and hosts views through a `ContentControl`.
- **View resolution**: `Resources/ViewTemplates.xaml` maps ViewModel types to corresponding UserControls with `DataTemplate`.
- **MVVM conventions**:
  - Base classes: `ObservableObject`, `ViewModelBase`.
  - Commands: custom `RelayCommand` + `AsyncRelayCommand` with cancellation and re-entrancy guard.
  - Most logic lives in ViewModels and services; XAML binds to properties/commands.
- **Styling system**:
  - Shared dictionaries merged in `App.xaml`: `Theme.xaml`, `Buttons.xaml`, `TextBoxes.xaml`, `DataGrids.xaml`, etc.
  - Core resources: `SurfaceBrush`, `PageBackgroundBrush`, `AccentBrush`, radii (`Cr10`, `Cr12`, `Cr20`), font size tokens, card styles (`TopCardStyle`, `CardStyle`).
  - Data grids commonly use card container borders plus styles like `EmployeeListCardGridStyle` / `CardListCardGridStyleBase`.

## Importer behavior choice

Implemented **Option A**: **Import SQL file/script contents into the current application database**.

- User picks a `.sql` file.
- File metadata is shown (path, size, modified timestamp, SHA-256 hash).
- File contents are loaded into editable multiline script box.
- Clicking **Execute Import Script** runs that SQL script against the app DB (`%LocalAppData%/GF3/SQLite.db`).

This is the safest and most consistent behavior with the current single-database app model because it does not replace or attach external DB files.

## Quick usage

1. Open **Database** from left navigation.
2. **Executor**: type SQL, click **Execute**.
   - `SELECT/PRAGMA/WITH` shows results in a styled DataGrid.
   - Non-query shows affected rows message.
   - Errors are shown in red-styled output.
3. **Importer**: choose `.sql` file, review script, execute import.
4. **Database Information**: review file metadata + table list and click **Refresh** anytime.
