using System;
using System.Data;
using System.Globalization;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// MatrixBinding.cs — частина (partial) ViewModel, яка відповідає ТІЛЬКИ за:
    /// - перетворення “того, що приходить з UI DataGrid” (рядок + колонка)
    ///   у структурований і однозначний ідентифікатор клітинки ScheduleMatrixCellRef.
    ///
    /// Навіщо це винесено:
    /// - у головному VM залишаються властивості/команди/загальна композиція
    /// - а "binding glue" (UI -> модель) живе окремо і читається як модуль
    ///
    /// Типовий сценарій:
    /// - користувач клікає/редагує клітинку DataGrid
    /// - UI віддає:
    ///   1) rowData (об’єкт рядка; для DataView зазвичай це DataRowView)
    ///   2) columnName (технічне ім’я колонки, напр. "emp_12")
    /// - Ми маємо визначити:
    ///   day (з колонки DayColumnName в рядку)
    ///   employeeId (через map _colNameToEmpId по columnName)
    /// - і сформувати ScheduleMatrixCellRef(day, employeeId, columnName)
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        /// <summary>
        /// Перевіряє, чи колонка “технічна” (не працівник).
        ///
        /// Технічні колонки — це:
        /// - DayColumnName (номер дня)
        /// - ConflictColumnName (прапорець конфлікту)
        /// - WeekendColumnName (службова колонка для стилю/вихідного)
        ///
        /// Для таких колонок ScheduleMatrixCellRef НЕ будується,
        /// бо вони не представляють редаговану “клітинку працівника”.
        /// </summary>
        private static bool IsTechnicalMatrixColumn(string columnName)
        {
            return columnName == DayColumnName
                || columnName == ConflictColumnName
                || columnName == WeekendColumnName;
        }

        /// <summary>
        /// Побудувати посилання на клітинку матриці (ScheduleMatrixCellRef) за даними з UI.
        ///
        /// Параметри:
        /// - rowData: те, що DataGrid передає як “дані рядка”.
        ///           Для DataView це зазвичай DataRowView.
        /// - columnName: технічне ім’я колонки DataTable (напр. "emp_12").
        ///
        /// Повертає:
        /// - true  => cellRef заповнений коректно (це клітинка працівника)
        /// - false => не вдалося ідентифікувати клітинку (технічна колонка, нема даних, тощо)
        ///
        /// Важливий принцип:
        /// - Ми робимо багато захисних перевірок, щоб не кидати exception через UI-edge cases.
        /// - UI може викликати це, коли рядок/колонка ще не повністю ініціалізовані.
        /// </summary>
        public bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef)
        {
            // 0) Вихідний out-параметр завжди ініціалізуємо.
            //    Це “правильна” практика для Try... методів.
            cellRef = default;

            // 1) Без SelectedBlock нема контексту (нема матриці/нема mapping колонок).
            if (SelectedBlock is null)
                return false;

            // 2) Перевіряємо columnName:
            //    - null/empty => це точно не колонка працівника
            //    - технічні колонки також не мають cellRef
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            if (IsTechnicalMatrixColumn(columnName))
                return false;

            // 3) Перевіряємо тип rowData.
            //    У твоєму випадку ScheduleMatrix — DataView, тож рядок найчастіше DataRowView.
            //    Якщо прийшов інший тип — не ризикуємо, просто false.
            if (rowData is not DataRowView rowView)
                return false;

            // 4) Дістаємо “день” з колонки DayColumnName.
            //    Вона зберігається у DataTable як int, але в DataRowView це object.
            var dayObj = rowView[DayColumnName];

            //    DBNull.Value означає “значення відсутнє” в DataTable.
            if (dayObj is null || dayObj == DBNull.Value)
                return false;

            // 5) Конвертуємо dayObj в int максимально безпечно.
            //    Convert.ToInt32 може кинути виняток (наприклад якщо там string "abc").
            int day;
            try
            {
                day = Convert.ToInt32(dayObj, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }

            // 6) День має бути позитивним (1..31).
            //    Для некоректних даних — false.
            if (day <= 0)
                return false;

            // 7) Знаходимо employeeId за технічною назвою колонки.
            //    _colNameToEmpId формується під час build матриці (ScheduleMatrixEngine.BuildScheduleTable).
            //    Якщо колонки нема в мапі — значить це не колонка працівника.
            if (!_colNameToEmpId.TryGetValue(columnName, out var employeeId))
                return false;

            // 8) Будуємо structured reference:
            //    - day: який рядок
            //    - employeeId: який працівник/колонка
            //    - columnName: технічне ім’я колонки (зручно для зворотних операцій/логів)
            cellRef = new ScheduleMatrixCellRef(day, employeeId, columnName);

            return true;
        }
    }
}
