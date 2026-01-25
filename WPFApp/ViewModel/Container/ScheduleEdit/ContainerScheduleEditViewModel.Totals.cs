using System.Collections.Generic;
using WPFApp.Infrastructure.ScheduleMatrix;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Totals.cs — частина ViewModel, яка відповідає тільки за підсумки (totals):
    /// - загальна кількість працівників
    /// - загальна кількість годин
    /// - текст “Total hours …” по кожному працівнику (для заголовків/tooltip’ів)
    ///
    /// Чому це окремим файлом:
    /// - totals часто перераховуються (після refresh матриці, після edit клітинки, після інвалідації)
    /// - це відносно незалежна логіка
    /// - головний VM стає меншим і читабельнішим
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        /// <summary>
        /// Кеш текстів "Total hours: Xh Ym" для кожного працівника.
        ///
        /// Ключ: EmployeeId
        /// Значення: вже готовий рядок, який зручно показувати в UI.
        ///
        /// Навіщо кеш:
        /// - UI може часто запитувати tooltip / заголовок колонки
        /// - формувати рядок щоразу — зайва робота
        /// - ми перераховуємо цей словник тільки коли змінюються слоти/працівники
        /// </summary>
        private readonly Dictionary<int, string> _employeeTotalHoursText = new();

        /// <summary>
        /// Повертає текст загального часу для конкретної колонки працівника.
        ///
        /// columnName — це технічна назва колонки DataTable (наприклад "emp_12").
        /// Ми через _colNameToEmpId дізнаємось, якому EmployeeId відповідає ця колонка,
        /// і дістаємо готовий текст з _employeeTotalHoursText.
        ///
        /// Якщо:
        /// - SelectedBlock == null => нічого не показуємо
        /// - columnName не є колонкою працівника => нічого не показуємо
        /// - у кеші немає значення => повертаємо "Total hours: 0h 0m"
        /// </summary>
        public string GetEmployeeTotalHoursText(string columnName)
        {
            if (SelectedBlock is null)
                return string.Empty;

            // Якщо це не колонка працівника — _colNameToEmpId не знайде її
            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return string.Empty;

            return _employeeTotalHoursText.TryGetValue(empId, out var text)
                ? text
                : "Total hours: 0h 0m";
        }

        /// <summary>
        /// Перерахувати totals.
        ///
        /// Викликати, коли:
        /// - refresh матриці завершився
        /// - користувач відредагував клітинку (інтервали змінені -> слоти змінені)
        /// - інвалідували генерацію (слоти очищені)
        /// - додали/видалили працівника
        ///
        /// Алгоритм:
        /// 1) очистити кеш текстів
        /// 2) якщо SelectedBlock null => totals = 0
        /// 3) інакше використати engine ScheduleTotalsCalculator:
        ///    - він рахує TotalEmployees, TotalDuration, PerEmployeeDuration
        /// 4) записати TotalEmployees і TotalHoursText
        /// 5) заповнити _employeeTotalHoursText для кожного працівника
        /// </summary>
        private void RecalculateTotals()
        {
            _employeeTotalHoursText.Clear();

            if (SelectedBlock is null)
            {
                TotalEmployees = 0;
                TotalHoursText = "0h 0m";
                return;
            }

            // 1) Рахуємо totals через винесений engine-клас
            var result = ScheduleTotalsCalculator.Calculate(SelectedBlock.Employees, SelectedBlock.Slots);

            // 2) Кількість працівників
            TotalEmployees = result.TotalEmployees;

            // 3) Загальна тривалість по всіх слотах
            TotalHoursText = ScheduleTotalsCalculator.FormatHoursMinutes(result.TotalDuration);

            // 4) Кеш текстів для кожного працівника
            foreach (var emp in SelectedBlock.Employees)
            {
                var empId = emp.EmployeeId;

                // Якщо працівник не має слотів — у словнику може не бути ключа, тоді empTotal буде 00:00
                result.PerEmployeeDuration.TryGetValue(empId, out var empTotal);

                _employeeTotalHoursText[empId] =
                    $"Total hours: {ScheduleTotalsCalculator.FormatHoursMinutes(empTotal)}";
            }
        }
    }
}
