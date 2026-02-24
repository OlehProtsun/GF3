using DataAccessLayer.Models;
using System;
using System.Collections.Generic;


namespace WPFApp.Applications.Matrix.Schedule
{
    /// <summary>
    /// ScheduleTotalsCalculator — “двигун” підрахунку підсумків по розкладу.
    ///
    /// НАВІЩО окремий файл/клас:
    /// - ViewModel не повинен містити складну логіку підрахунків.
    /// - Підрахунки потрібні не лише для UI (їх можна перевикористати в сервісі/експорті/логіці).
    /// - Такий клас легко тестувати: дав список слотів/працівників -> отримав результат.
    ///
    /// ЩО рахуємо:
    /// 1) TotalEmployees — скільки унікальних працівників реально присутні в блоці
    /// 2) TotalDuration — сумарний час всіх слотів
    /// 3) PerEmployeeDuration — сумарний час по кожному працівнику (EmployeeId -> TimeSpan)
    ///
    /// Важливий нюанс:
    /// - Ми рахуємо тільки ті слоти, де:
    ///   a) є EmployeeId
    ///   b) EmployeeId входить у список Employees блоку
    ///   (бо може лишитись “висячий” слот після видалення працівника)
    /// </summary>
    public static class ScheduleTotalsCalculator
    {
        /// <summary>
        /// Результат підрахунку.
        /// Це компактна структура даних, яку зручно передавати з engine в VM.
        /// </summary>
        public sealed class TotalsResult
        {
            /// <summary>
            /// Кількість унікальних працівників у блоці.
            /// </summary>
            public int TotalEmployees { get; init; }

            /// <summary>
            /// Сумарна тривалість всіх слотів (Total hours).
            /// </summary>
            public TimeSpan TotalDuration { get; init; }

            /// <summary>
            /// Сумарна тривалість по кожному працівнику: EmployeeId -> TimeSpan.
            /// Якщо працівник не має слотів — він може бути відсутній у словнику.
            /// </summary>
            public IReadOnlyDictionary<int, TimeSpan> PerEmployeeDuration { get; init; }
                = new Dictionary<int, TimeSpan>();
        }

        /// <summary>
        /// Порахувати totals для блока.
        ///
        /// Вхід:
        /// - employees: список ScheduleEmployeeModel (містить EmployeeId)
        /// - slots: список ScheduleSlotModel (містить EmployeeId + FromTime/ToTime)
        ///
        /// Вихід:
        /// - TotalsResult з TotalEmployees, TotalDuration, PerEmployeeDuration
        ///
        /// Алгоритм:
        /// 1) Збираємо HashSet employeeIds з employees (унікальні id)
        /// 2) Проходимо по slots:
        ///    - якщо слот без EmployeeId -> пропускаємо
        ///    - якщо EmployeeId не в списку employees -> пропускаємо
        ///    - парсимо часи через ScheduleMatrixEngine.TryParseTime
        ///    - вираховуємо duration, враховуючи перехід через 00:00
        ///    - додаємо до total і до per-employee
        /// </summary>
        public static TotalsResult Calculate(
            IReadOnlyList<ScheduleEmployeeModel> employees,
            IReadOnlyList<ScheduleSlotModel> slots)
        {
            // 1) Унікальні employeeId
            var empIds = new HashSet<int>();
            for (int i = 0; i < employees.Count; i++)
                empIds.Add(employees[i].EmployeeId);

            // 2) Якщо працівників нема — все одразу нуль
            if (empIds.Count == 0 || slots.Count == 0)
            {
                return new TotalsResult
                {
                    TotalEmployees = empIds.Count,
                    TotalDuration = TimeSpan.Zero,
                    PerEmployeeDuration = new Dictionary<int, TimeSpan>()
                };
            }

            // 3) Підсумки
            var total = TimeSpan.Zero;

            // capacity підбираємо “приблизно”, щоб менше реалокацій
            var perEmp = new Dictionary<int, TimeSpan>(capacity: Math.Max(8, empIds.Count));

            // 4) Проходимо слоти
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];

                // 4.1) Без працівника — не рахуємо
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    continue;

                var empId = s.EmployeeId.Value;

                // 4.2) Якщо працівника вже прибрали з employees — пропускаємо “висячий” слот
                if (!empIds.Contains(empId))
                    continue;

                // 4.3) Парсимо часи
                if (!ScheduleMatrixEngine.TryParseTime(s.FromTime, out var from) ||
                    !ScheduleMatrixEngine.TryParseTime(s.ToTime, out var to))
                    continue;

                // 4.4) Duration
                var dur = to - from;

                // якщо час закінчення менший — значить слот перейшов через 00:00
                if (dur < TimeSpan.Zero)
                    dur += TimeSpan.FromHours(24);

                // 4.5) Додаємо до total
                total += dur;

                // 4.6) Додаємо до perEmp
                if (perEmp.TryGetValue(empId, out var cur))
                    perEmp[empId] = cur + dur;
                else
                    perEmp[empId] = dur;
            }

            return new TotalsResult
            {
                TotalEmployees = empIds.Count,
                TotalDuration = total,
                PerEmployeeDuration = perEmp
            };
        }

        /// <summary>
        /// Зручно: форматування TimeSpan в “Xh Ym”.
        /// Це корисно, щоб VM не дублював формат в 2-3 місцях.
        /// </summary>
        public static string FormatHoursMinutes(TimeSpan t)
            => $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
