using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using WPFApp.Infrastructure.AvailabilityMatrix;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Логіка “матриці”:
    /// - SetEmployees (оновлення captions)
    /// - ResetForNew/LoadGroup/ResetGroupMatrix
    /// - ReadGroupCodes / SetEmployeeCodes
    /// - Додавання/видалення колонок працівників
    /// - Регенерація day rows під month/year
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        public void SetEmployees(IEnumerable<EmployeeModel> employees, IReadOnlyDictionary<int, string> nameLookup)
        {
            // 1) Очищаємо UI список працівників.
            Employees.Clear();

            // 2) Очищаємо локальний lookup імен.
            _employeeNames.Clear();

            // 3) Заповнюємо списки.
            foreach (var employee in employees)
            {
                // 3.1) Беремо ім’я з nameLookup (якщо воно “вже красиве”).
                var name = nameLookup.TryGetValue(employee.Id, out var fullName)
                    ? fullName
                    // 3.2) Інакше — формуємо з моделі.
                    : $"{employee.FirstName} {employee.LastName}";

                // 3.3) Додаємо елемент в UI-список.
                Employees.Add(new EmployeeListItem { Id = employee.Id, FullName = name });

                // 3.4) Зберігаємо в локальний словник.
                _employeeNames[employee.Id] = name;
            }

            // 4) Якщо матриця вже має employee колонки — оновимо Caption у DataTable.
            bool captionsChanged = false;

            foreach (var kv in _employeeIdToColumn)
            {
                // kv.Key = employeeId, kv.Value = columnName
                if (!_employeeNames.TryGetValue(kv.Key, out var displayName))
                    continue;

                // Якщо колонка є в таблиці — оновлюємо caption.
                if (_groupTable.Columns.Contains(kv.Value))
                {
                    var col = _groupTable.Columns[kv.Value];

                    // Оновлюємо лише коли реально змінилось.
                    if (!string.Equals(col.Caption, displayName, StringComparison.Ordinal))
                    {
                        col.Caption = displayName;
                        captionsChanged = true;
                    }
                }
            }

            // 5) Якщо змінювали captions — просимо UI оновитись.
            if (captionsChanged)
                NotifyMatrixChanged();
        }

        public void ResetForNew()
        {
            // 1) Входимо у батч матриці: обмежуємо кількість UI нотифікацій.
            using var _ = EnterMatrixUpdate();

            // 2) Входимо у батч дати: Year+Month ставимо разом.
            using var __ = EnterDateSync();

            // 3) Скидаємо поля групи.
            AvailabilityId = 0;
            AvailabilityName = string.Empty;

            // 4) Ставимо дефолтну дату.
            AvailabilityMonth = DateTime.Today.Month;
            AvailabilityYear = DateTime.Today.Year;

            // 5) Чистимо валідаційні помилки.
            ClearValidationErrors();

            // 6) Скидаємо матрицю і одразу генеруємо day rows.
            ResetGroupMatrixCore(regenerateDays: true);
        }

        public void LoadGroup(
            AvailabilityGroupModel group,
            List<AvailabilityGroupMemberModel> members,
            List<AvailabilityGroupDayModel> days,
            IReadOnlyDictionary<int, string> nameLookup)
        {
            // 1) Батчимо матрицю, щоб під час load не було “дерганини” UI.
            using var _ = EnterMatrixUpdate();

            // 2) Батчимо дату, щоб Month+Year не робили regen двічі.
            using var __ = EnterDateSync();

            // 3) Заповнюємо інформаційні поля.
            AvailabilityId = group.Id;
            AvailabilityName = group.Name ?? string.Empty;
            AvailabilityMonth = group.Month;
            AvailabilityYear = group.Year;

            // 4) Чистимо помилки (екран щойно завантажився — не показуємо старе).
            ClearValidationErrors();

            // 5) Скидаємо таблицю і будуємо day rows під group.Year/group.Month.
            ResetGroupMatrixCore(regenerateDays: true);

            // 6) Додаємо колонки працівників.
            foreach (var m in members)
            {
                // 6.1) Header: якщо Employee не завантажений — беремо з nameLookup або fallback.
                var header = m.Employee is null
                    ? (nameLookup.TryGetValue(m.EmployeeId, out var n) ? n : $"Employee #{m.EmployeeId}")
                    // 6.2) Інакше беремо з моделі.
                    : $"{m.Employee.FirstName} {m.Employee.LastName}";

                // 6.3) Додаємо колонку.
                TryAddEmployeeColumn(m.EmployeeId, header);
            }

            // 7) Рахуємо кількість днів у місяці (dim).
            int dim = DateTime.DaysInMonth(group.Year, group.Month);

            // 8) Будуємо lookup (memberId, day) -> record.
            //    Якщо в джерелі є дублікати — беремо останній.
            var dayLookup = days
                .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                .ToDictionary(g => g.Key, g => g.Last());

            // 9) Заповнюємо коди для кожного member.
            foreach (var mb in members)
            {
                // 9.1) Готуємо масив (day, code) для швидкого SetEmployeeCodes.
                var codes = new (int day, string code)[dim];

                // 9.2) Для кожного дня формуємо код.
                for (int day = 1; day <= dim; day++)
                {
                    // Якщо запису немає — ставимо default "-".
                    if (!dayLookup.TryGetValue((mb.Id, day), out var d))
                    {
                        codes[day - 1] = (day, AvailabilityCellCodeParser.NoneMark);
                        continue;
                    }

                    // Інакше формуємо код залежно від AvailabilityKind.
                    var code = d.Kind switch
                    {
                        AvailabilityKind.ANY => AvailabilityCellCodeParser.AnyMark,
                        AvailabilityKind.NONE => AvailabilityCellCodeParser.NoneMark,
                        AvailabilityKind.INT => d.IntervalStr ?? string.Empty,
                        _ => AvailabilityCellCodeParser.NoneMark
                    };

                    // Запис у масив.
                    codes[day - 1] = (day, code);
                }

                // 9.3) Масово записуємо коди в колонку працівника.
                SetEmployeeCodes(mb.EmployeeId, codes);
            }

            // 10) Після масового запису:
            // - нормалізуємо формат інтервалів
            // - ставимо column errors при невалідних значеннях
            NormalizeAndValidateAllMatrixCells();
        }

        public void ResetGroupMatrix()
        {
            // 1) Батчимо.
            using var _ = EnterMatrixUpdate();

            // 2) Скидаємо.
            ResetGroupMatrixCore(regenerateDays: true);
        }

        public IReadOnlyList<int> GetSelectedEmployeeIds()
            // Повертаємо employeeId, які зараз присутні як колонки.
            => _employeeIdToColumn.Keys.ToList();

        public IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes()
        {
            // 1) Ініціалізуємо результат з capacity (оптимізація алокацій).
            var result = new List<(int employeeId, IList<(int dayOfMonth, string code)> codes)>(
                capacity: _employeeIdToColumn.Count);

            // 2) Для кожного employeeId читаємо його колонку.
            foreach (var kv in _employeeIdToColumn)
            {
                // 2.1) Читаємо коди через engine.
                var list = AvailabilityMatrixEngine.ReadEmployeeCodes(_groupTable, kv.Value);

                // 2.2) Додаємо пару (employeeId, codes).
                result.Add((kv.Key, list));
            }

            return result;
        }

        public void SetEmployeeCodes(int employeeId, IEnumerable<(int dayOfMonth, string code)> codes)
        {
            // 1) Якщо employeeId не має колонки — виходимо.
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return;

            // 2) Делегуємо масовий запис кодів у engine.
            AvailabilityMatrixEngine.SetEmployeeCodes(_groupTable, colName, codes);
        }

        private void RegenerateGroupDays()
        {
            // 1) Зчитуємо параметри.
            int year = AvailabilityYear;
            int month = AvailabilityMonth;

            // 2) Делегуємо синхронізацію рядків під (year, month).
            AvailabilityMatrixEngine.EnsureDayRowsForMonth(_groupTable, year, month);

            // 3) Сигналимо UI.
            NotifyMatrixChanged();
        }

        private bool TryAddEmployeeColumn(int employeeId, string header)
        {
            // 1) Якщо employee вже доданий у мапу — не дублюємо.
            if (_employeeIdToColumn.ContainsKey(employeeId))
                return false;

            // 2) Пробуємо додати колонку через engine.
            if (!AvailabilityMatrixEngine.TryAddEmployeeColumn(_groupTable, employeeId, header, out var colName))
                return false;

            // 3) Фіксуємо зв’язок employeeId -> columnName.
            _employeeIdToColumn[employeeId] = colName;

            // 4) Сигналимо UI.
            NotifyMatrixChanged();

            return true;
        }

        private bool RemoveEmployeeColumn(int employeeId)
        {
            // 1) Якщо колонки нема — нічого видаляти.
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return false;

            // 2) Видаляємо з мапи.
            _employeeIdToColumn.Remove(employeeId);

            // 3) Видаляємо колонку з DataTable.
            AvailabilityMatrixEngine.RemoveEmployeeColumn(_groupTable, colName);

            // 4) Сигналимо UI.
            NotifyMatrixChanged();

            return true;
        }

        private void ResetGroupMatrixCore(bool regenerateDays)
        {
            // 1) Чистимо мапу employeeId -> columnName (структура буде збудована з нуля).
            _employeeIdToColumn.Clear();

            // 2) Скидаємо таблицю через engine.
            //    engine:
            //    - прибере employee колонки
            //    - очистить rows
            //    - якщо regenerateDays==true — створить day rows під поточний Year/Month
            AvailabilityMatrixEngine.Reset(_groupTable, regenerateDays, AvailabilityYear, AvailabilityMonth);

            // 3) Оновлення UI.
            NotifyMatrixChanged();
        }
    }
}
