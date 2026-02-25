using System;
using System.Collections.Generic;
using System.Data;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.Applications.Matrix.Availability
{
    /// <summary>
    /// AvailabilityMatrixEngine — “двигун” матриці availability.
    ///
    /// Принцип:
    /// - Тут живе ВСЯ логіка роботи з DataTable:
    ///   * створення/підтримка структури (Day column, employee columns)
    ///   * регенерація рядків 1..N (дні місяця)
    ///   * читання/запис кодів у клітинки
    ///   * нормалізація та валідація коду клітинки (через AvailabilityCellCodeParser)
    ///
    /// - ViewModel:
    ///   * НЕ містить алгоритмів таблиці
    ///   * лише викликає engine і піднімає UI-події (MatrixChanged/PropertyChanged)
    ///
    /// Важливо:
    /// - Engine НЕ піднімає PropertyChanged/MatrixChanged — це відповідальність VM.
    /// - Engine НЕ знає про WPF/owner/команди — лише “чиста” логіка даних.
    /// </summary>
    public static class AvailabilityMatrixEngine
    {
        /// <summary>
        /// Єдиний “центр правди” для назви колонки дня.
        /// Ми навмисно використовуємо ScheduleMatrixConstants.DayColumnName,
        /// бо у проекті вже прийнята конвенція "DayOfMonth".
        /// </summary>
        public static readonly string DayColumnName = ScheduleMatrixConstants.DayColumnName;

        /// <summary>
        /// Побудувати технічну назву колонки працівника за його EmployeeId.
        /// </summary>
        /// <param name="employeeId">Id працівника.</param>
        /// <returns>Технічне ім’я колонки, напр. "emp_12".</returns>
        public static string GetEmployeeColumnName(int employeeId)
        {
            // Формуємо стабільний ключ колонки.
            // Це важливо для:
            // - збереження/відновлення
            // - пошуку колонки
            // - биндів/гарячих клавіш
            return $"emp_{employeeId}";
        }

        /// <summary>
        /// Переконатися, що у таблиці існує Day column (DayOfMonth).
        /// Якщо нема — додати і (опційно) виставити PrimaryKey.
        /// </summary>
        /// <param name="table">DataTable матриці availability.</param>
        public static void EnsureDayColumn(DataTable table)
        {
            // Захист від null, щоб помилка була відразу зрозуміла.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Якщо колонка вже існує — нічого не робимо.
            if (table.Columns.Contains(DayColumnName))
                return;

            // Створюємо колонку дня:
            // - тип int, бо день місяця це число 1..31
            // - ReadOnly, бо користувач не має редагувати день
            var dayColumn = new DataColumn(DayColumnName, typeof(int))
            {
                Caption = "Day",     // Caption — це заголовок, який WPF DataGrid може показувати.
                ReadOnly = true      // Блокуємо редагування.
            };

            // Додаємо колонку у таблицю.
            table.Columns.Add(dayColumn);

            // (Опційно) Виставляємо PrimaryKey, щоб у майбутньому можна було робити table.Rows.Find(day).
            // Тут це безпечно, бо DayColumn — унікальний для рядків (1..N).
            table.PrimaryKey = new[] { dayColumn };
        }

        /// <summary>
        /// Зрівняти кількість рядків таблиці з кількістю днів у вказаному (year, month).
        ///
        /// Логіка:
        /// - рядки йдуть строго як day=1..N у тому ж порядку
        /// - якщо треба більше рядків — додаємо в кінець
        /// - якщо треба менше — обрізаємо з кінця
        ///
        /// Це зберігає значення для “спільних” днів і мінімізує алокації.
        /// </summary>
        /// <param name="table">Таблиця матриці.</param>
        /// <param name="year">Рік.</param>
        /// <param name="month">Місяць (1..12).</param>
        public static void EnsureDayRowsForMonth(DataTable table, int year, int month)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Некоректні параметри — просто нічого не робимо.
            // Валідація year/month виконується у VM через rules,
            // але engine не має падати через “проміжний стан” UI.
            if (year <= 0 || month < 1 || month > 12)
                return;

            // Переконуємось, що Day column існує.
            EnsureDayColumn(table);

            // Визначаємо, скільки днів у цьому місяці.
            int desiredRowCount = DateTime.DaysInMonth(year, month);

            // Поточна кількість рядків.
            int currentRowCount = table.Rows.Count;

            // Якщо рядків не вистачає — додаємо.
            if (currentRowCount < desiredRowCount)
            {
                // Додаємо від (current+1) до desired включно.
                for (int day = currentRowCount + 1; day <= desiredRowCount; day++)
                {
                    // Створюємо новий рядок.
                    var row = table.NewRow();

                    // Записуємо номер дня у Day column.
                    row[DayColumnName] = day;

                    // Для всіх employee колонок ставимо пустий рядок,
                    // щоб клітинки були однорідні і не містили DBNull.
                    foreach (DataColumn col in table.Columns)
                    {
                        // DayColumn не чіпаємо (вже встановили).
                        if (col.ColumnName == DayColumnName)
                            continue;

                        // У availability значення в клітинці — string (код).
                        row[col.ColumnName] = string.Empty;
                    }

                    // Додаємо рядок в таблицю.
                    table.Rows.Add(row);
                }
            }
            // Якщо рядків забагато — обрізаємо.
            else if (currentRowCount > desiredRowCount)
            {
                // Видаляємо з кінця, щоб зберегти перші desired днів.
                for (int i = currentRowCount - 1; i >= desiredRowCount; i--)
                    table.Rows.RemoveAt(i);
            }

            // Якщо current == desired — нічого не робимо.
        }

        /// <summary>
        /// Додати колонку працівника (string) у матрицю.
        /// </summary>
        /// <param name="table">Таблиця матриці.</param>
        /// <param name="employeeId">Id працівника.</param>
        /// <param name="header">Caption колонки (ім’я працівника) для UI.</param>
        /// <param name="columnName">Out: технічне ім’я створеної колонки ("emp_{id}").</param>
        /// <returns>true якщо колонку додано, false якщо не можна/вже існує.</returns>
        public static bool TryAddEmployeeColumn(DataTable table, int employeeId, string header, out string columnName)
        {
            // Готуємо out значення за замовчуванням.
            columnName = string.Empty;

            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Некоректний employeeId — не додаємо.
            if (employeeId <= 0)
                return false;

            // Гарантуємо Day column.
            EnsureDayColumn(table);

            // Рахуємо технічну назву колонки.
            columnName = GetEmployeeColumnName(employeeId);

            // Якщо така колонка вже є — нічого не робимо.
            if (table.Columns.Contains(columnName))
                return false;

            // Створюємо нову колонку типу string:
            // - ColumnName = технічний ключ
            // - Caption = те, що бачить користувач
            // - DefaultValue = пусто (а не DBNull)
            var col = new DataColumn(columnName, typeof(string))
            {
                Caption = header ?? string.Empty,
                DefaultValue = string.Empty
            };

            // Додаємо колонку в таблицю.
            table.Columns.Add(col);

            // Для вже існуючих рядків явно проставляємо пустий рядок,
            // щоб прибрати будь-які "null/DBNull" артефакти.
            foreach (DataRow r in table.Rows)
                r[columnName] = string.Empty;

            // Сигнал успіху.
            return true;
        }

        /// <summary>
        /// Видалити колонку працівника за ім’ям колонки.
        /// </summary>
        /// <param name="table">Таблиця.</param>
        /// <param name="columnName">Технічне ім’я колонки (наприклад "emp_12").</param>
        /// <returns>true якщо видалено, false якщо колонки не існувало або columnName некоректне.</returns>
        public static bool RemoveEmployeeColumn(DataTable table, string columnName)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Порожній ключ — не валідно.
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            // Day column видаляти не дозволяємо.
            if (columnName == DayColumnName)
                return false;

            // Якщо колонки нема — нічого не робимо.
            if (!table.Columns.Contains(columnName))
                return false;

            // Видаляємо колонку.
            table.Columns.Remove(columnName);

            // Сигнал успіху.
            return true;
        }

        /// <summary>
        /// Видалити всі employee колонки, залишивши тільки Day column.
        /// </summary>
        /// <param name="table">Таблиця.</param>
        public static void RemoveAllEmployeeColumns(DataTable table)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Гарантуємо Day column.
            EnsureDayColumn(table);

            // Збираємо список колонок для видалення,
            // бо не можна модифікувати collection під час foreach по ній.
            var toRemove = new List<string>();

            foreach (DataColumn c in table.Columns)
            {
                // Лишаємо тільки Day column.
                if (c.ColumnName == DayColumnName)
                    continue;

                // Все інше — employee колонки.
                toRemove.Add(c.ColumnName);
            }

            // Видаляємо.
            for (int i = 0; i < toRemove.Count; i++)
                table.Columns.Remove(toRemove[i]);
        }

        /// <summary>
        /// Повністю очистити таблицю:
        /// - прибрати employee колонки
        /// - прибрати всі рядки
        /// - (опційно) знову створити рядки 1..N для поточного (year,month)
        /// </summary>
        public static void Reset(DataTable table, bool regenerateDays, int year, int month)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // 1) Day column має існувати (ми його залишаємо).
            EnsureDayColumn(table);

            // 2) Прибираємо всі employee колонки.
            RemoveAllEmployeeColumns(table);

            // 3) Чистимо всі рядки.
            table.Rows.Clear();

            // 4) Якщо треба — генеруємо рядки під місяць.
            if (regenerateDays)
                EnsureDayRowsForMonth(table, year, month);
        }

        /// <summary>
        /// Записати набір кодів (day -> code) у конкретну employee колонку.
        /// </summary>
        public static void SetEmployeeCodes(DataTable table, string employeeColumnName, IEnumerable<(int dayOfMonth, string code)> codes)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Без колонки — нічого робити.
            if (string.IsNullOrWhiteSpace(employeeColumnName))
                return;

            // Якщо колонки нема — нічого робити.
            if (!table.Columns.Contains(employeeColumnName))
                return;

            // Проходимо коди.
            foreach (var (day, raw) in codes)
            {
                // В availability рядки зберігаються як day=1..N в index day-1.
                // Тому відразу робимо fast-path.
                if (day <= 0 || day > table.Rows.Count)
                    continue;

                // Беремо потрібний рядок.
                var row = table.Rows[day - 1];

                // Пишемо значення; null замінюємо на пустий рядок.
                row[employeeColumnName] = raw ?? string.Empty;
            }
        }

        /// <summary>
        /// Прочитати всі коди (1..N) з конкретної employee колонки.
        /// </summary>
        public static List<(int dayOfMonth, string code)> ReadEmployeeCodes(DataTable table, string employeeColumnName)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Готуємо результат.
            var result = new List<(int dayOfMonth, string code)>();

            // Якщо колонки нема — повертаємо пусто.
            if (string.IsNullOrWhiteSpace(employeeColumnName) || !table.Columns.Contains(employeeColumnName))
                return result;

            // Кількість рядків = кількість днів.
            int rowCount = table.Rows.Count;

            // Резервуємо capacity, щоб зменшити реалокації.
            result = new List<(int dayOfMonth, string code)>(capacity: rowCount);

            // Проходимо всі рядки.
            for (int i = 0; i < rowCount; i++)
            {
                // Day по конвенції = index+1.
                int day = i + 1;

                // Значення клітинки як string.
                var code = Convert.ToString(table.Rows[i][employeeColumnName]) ?? string.Empty;

                // Додаємо.
                result.Add((day, code));
            }

            return result;
        }

        /// <summary>
        /// Нормалізувати та перевірити одиночне значення клітинки availability.
        /// Це thin-wrapper навколо AvailabilityCellCodeParser.
        /// </summary>
        public static bool TryNormalizeCell(string? raw, out string normalized, out string? error)
        {
            // Делегуємо у parser:
            // - приймає "", "+", "-", "HH:mm-HH:mm", "HH:mm - HH:mm"
            // - повертає нормалізований вигляд
            return AvailabilityCellCodeParser.TryNormalize(raw, out normalized, out error);
        }

        /// <summary>
        /// Нормалізувати і проставити ColumnError для всієї таблиці.
        ///
        /// Застосування:
        /// - після LoadGroup (масове заповнення), щоб:
        ///   * привести формат інтервалів до канонічного
        ///   * підсвітити невалідні значення (якщо вони якимось чином потрапили)
        /// </summary>
        public static void NormalizeAndValidateAllCells(DataTable table)
        {
            // Захист від null.
            if (table is null)
                throw new ArgumentNullException(nameof(table));

            // Проходимо всі рядки.
            foreach (DataRow row in table.Rows)
            {
                // Проходимо всі колонки.
                foreach (DataColumn col in table.Columns)
                {
                    // Day column не валідимо як “код”.
                    if (col.ColumnName == DayColumnName)
                        continue;

                    // Дістаємо поточне значення.
                    var raw = Convert.ToString(row[col]) ?? string.Empty;

                    // Пробуємо нормалізувати/перевірити.
                    if (!TryNormalizeCell(raw, out var normalized, out var error))
                    {
                        // Якщо невалідно — ставимо помилку на колонку (WPF DataGrid її бачить).
                        row.SetColumnError(col, error ?? "Invalid value.");
                        continue;
                    }

                    // Якщо валідно — чистимо помилку.
                    row.SetColumnError(col, string.Empty);

                    // Якщо нормалізація змінила вигляд — переписуємо.
                    // (Наприклад "9:00 - 18:00" -> "09:00-18:00")
                    if (!string.Equals(raw, normalized, StringComparison.Ordinal))
                        row[col] = normalized;
                }
            }
        }
    }
}
