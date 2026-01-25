namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    /// <summary>
    /// ScheduleMatrixCellRef — легкий структурований ідентифікатор клітинки матриці.
    ///
    /// Містить:
    /// - DayOfMonth: номер дня (1..31) — рядок матриці
    /// - EmployeeId: ідентифікатор працівника — “працівнича” колонка
    /// - ColumnName: технічне ім’я колонки DataTable (наприклад "emp_12")
    ///
    /// Навіщо тут ColumnName, якщо є EmployeeId:
    /// - У DataGrid/WPF ти часто працюєш з columnName як з ключем
    /// - ColumnName допомагає швидко зв’язати UI-колонку і логику (tooltips, edit)
    /// - При цьому для “store” стилів достатньо (DayOfMonth, EmployeeId),
    ///   але ColumnName корисний для зворотного зв’язку з DataTable/DataGrid.
    ///
    /// record struct:
    /// - immutable (readonly)
    /// - швидке порівняння за значеннями
    /// - добре підходить для ключів/колекцій/Distinct()
    /// </summary>
    public readonly record struct ScheduleMatrixCellRef(int DayOfMonth, int EmployeeId, string ColumnName);
}
