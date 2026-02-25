using BusinessLogicLayer.Contracts.Models;
using System.Windows.Media;

namespace WPFApp.ViewModel.Container.ScheduleEdit.Helpers
{
    /// <summary>
    /// IScheduleMatrixStyleProvider — інтерфейс, який описує:
    /// “як UI (DataGrid) може отримати стилі для конкретної клітинки матриці”.
    ///
    /// Навіщо інтерфейс:
    /// - XAML/Converters/Behaviors можуть працювати з абстракцією, а не з конкретним VM.
    /// - Ти можеш мати іншу реалізацію для preview-матриці або для тестів.
    ///
    /// Типовий сценарій в UI:
    /// 1) UI знає rowData (DataRowView) і columnName (рядок DataGridColumn)
    /// 2) UI викликає TryBuildCellReference(...) -> отримує ScheduleMatrixCellRef
    /// 3) UI питає GetCellBackgroundBrush / GetCellForegroundBrush -> отримує Brush
    /// 4) UI (опційно) може попросити сам style object через TryGetCellStyle
    /// 5) UI відстежує CellStyleRevision — коли число змінюється, треба перерендерити стилі
    /// </summary>
    public interface IScheduleMatrixStyleProvider
    {
        /// <summary>
        /// Ревізія стилів.
        ///
        /// UI може прив’язатись до цього int:
        /// - коли він змінюється, значить стилі змінились (фарбування, очистка, тощо)
        /// - і треба змусити DataGrid оновити cell style/brush.
        /// </summary>
        int CellStyleRevision { get; }

        /// <summary>
        /// “Місток” між UI DataGrid і бізнес-логікою:
        /// будує ScheduleMatrixCellRef (day + employeeId + columnName),
        /// якщо row/column відповідають “працівничій” клітинці.
        ///
        /// Повертає false для:
        /// - службових колонок (Day/Conflict/Weekend)
        /// - невалідного rowData
        /// - ситуацій, коли не можемо визначити employeeId
        /// </summary>
        bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef);

        /// <summary>
        /// Повертає Brush фону клітинки.
        /// null означає: “немає кастомного фону, використовуй стандартний стиль”.
        /// </summary>
        Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef);

        /// <summary>
        /// Повертає Brush тексту (foreground) клітинки.
        /// null означає: “використовуй стандартний колір тексту”.
        /// </summary>
        Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef);

        /// <summary>
        /// Дати доступ до сирого об’єкта стилю (ScheduleCellStyleModel),
        /// якщо він існує.
        ///
        /// Це корисно, якщо UI хоче показати “деталі стилю”
        /// або визначати інші параметри стилю не тільки з Brush (наприклад прапорці).
        /// </summary>
        bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style);
    }
}
