using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace WPFApp.Applications.Matrix.Schedule
{
    /// <summary>
    /// “Двигун” матриці розкладу.
    ///
    /// Ідея: тут живе вся логіка, яка НЕ залежить від UI і WPF.
    /// Тобто:
    ///  - побудова DataTable (матриці) з слотів/працівників
    ///  - парсинг інтервалів з тексту
    ///  - мердж інтервалів для показу
    ///  - перевірка конфліктів (перетини, незаповнені слоти)
    ///  - застосування інтервалів до колекції слотів
    ///
    /// А ViewModel має тільки:
    ///  - викликати ці методи
    ///  - оновлювати DataView/властивості/події на UI-потоці
    /// </summary>
    public static class ScheduleMatrixEngine
    {
        // Щоб не тягнути всюди довгі назви – беремо константи коротко.
        private static string DayCol => ScheduleMatrixConstants.DayColumnName;
        private static string ConflictCol => ScheduleMatrixConstants.ConflictColumnName;
        private static string WeekendCol => ScheduleMatrixConstants.WeekendColumnName;
        private static string Empty => ScheduleMatrixConstants.EmptyMark;

        /// <summary>
        /// Безпечний парсер часу (09:00 або 9:00).
        /// Повертає true/false і віддає результат в out.
        /// </summary>
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            return BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(s, out t);
        }

        /// <summary>
        /// Будує DataTable для відображення в гріді:
        ///  - рядки: дні місяця
        ///  - колонки: працівники (плюс службові Day/Conflict/Weekend)
        ///  - значення в клітинці: "HH:mm - HH:mm, HH:mm - HH:mm" або "-"
        ///
        /// Також повертає map: columnName -> employeeId, щоб VM знав,
        /// який працівник відповідає за яку колонку.
        /// </summary>
        public static DataTable BuildScheduleTable(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId,
            CancellationToken ct = default)
        {
            // 1) Готуємо структуру таблиці
            colNameToEmpId = new Dictionary<string, int>();
            var table = new DataTable();

            // 2) Додаємо службові колонки
            table.Columns.Add(DayCol, typeof(int));
            table.Columns.Add(ConflictCol, typeof(bool));
            table.Columns.Add(WeekendCol, typeof(bool)); // службова під RowStyle у WPF

            // 3) Колонки по працівниках (по одному на employeeId)
            // HashSet потрібен, щоб не додати одну людину двічі, якщо дані з дублікатами
            // 3) Колонки по працівниках (по одному на employeeId)
            // ✅ Єдиний канонічний порядок: LastName -> FirstName -> EmployeeId
            var orderedEmployees = (employees ?? Array.Empty<ScheduleEmployeeModel>())
                .Where(e => e != null)
                .OrderBy(e => (e.Employee?.LastName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => (e.Employee?.FirstName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => e.EmployeeId)
                .ToList();

            // HashSet потрібен, щоб не додати одну людину двічі, якщо дані з дублікатами
            var seenEmpIds = new HashSet<int>();

            foreach (var emp in orderedEmployees)
            {
                // якщо id вже був — пропускаємо
                if (!seenEmpIds.Add(emp.EmployeeId))
                    continue;

                var displayName = $"{emp.Employee?.FirstName} {emp.Employee?.LastName}".Trim();
                var baseName = string.IsNullOrWhiteSpace(displayName)
                    ? $"Employee {emp.EmployeeId}"
                    : displayName;

                var columnName = $"emp_{emp.EmployeeId}";

                var suffix = 1;
                while (table.Columns.Contains(columnName))
                    columnName = $"emp_{emp.EmployeeId}_{++suffix}";

                var col = table.Columns.Add(columnName, typeof(string));
                col.Caption = baseName;

                colNameToEmpId[columnName] = emp.EmployeeId;
            }

            // 4) Скільки днів у цьому місяці (28..31)
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // 5) Для швидкості індексуємо слоти по днях
            // slotsByDay[1] = список слотів для 1-го числа
            var slotsByDay = new List<ScheduleSlotModel>?[daysInMonth + 1]; // 1..daysInMonth

            if (slots != null && slots.Count > 0)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    var d = s.DayOfMonth;

                    // захист від кривих даних: день поза межами місяця
                    if ((uint)d > (uint)daysInMonth || d <= 0)
                        continue;

                    // якщо список для цього дня ще не створений — створюємо
                    var list = slotsByDay[d];
                    if (list == null)
                    {
                        list = new List<ScheduleSlotModel>(8);
                        slotsByDay[d] = list;
                    }

                    // додаємо слот у “кошик” дня
                    list.Add(s);
                }
            }

            // 6) Щоб швидко пробігати по колонках працівників (без Dictionary iterator overhead)
            // empCols: масив пар (columnName, employeeId)
            var empCols = colNameToEmpId.ToArray();

            // 7) Локальна функція: швидко зібрати текст "HH:mm - HH:mm, ..."
            static string FormatMerged(List<(string from, string to)> merged)
            {
                if (merged == null || merged.Count == 0)
                    return ScheduleMatrixConstants.EmptyMark;

                var sb = new StringBuilder(capacity: merged.Count * 14);

                for (int i = 0; i < merged.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(merged[i].from).Append(" - ").Append(merged[i].to);
                }

                return sb.ToString();
            }

            // 8) Основний цикл по днях місяця
            for (int day = 1; day <= daysInMonth; day++)
            {
                // якщо VM скасував побудову (новий build стартанув) — виходимо
                ct.ThrowIfCancellationRequested();

                var daySlots = slotsByDay[day];

                // створюємо новий рядок
                var row = table.NewRow();

                // день
                row[DayCol] = day;

                // вихідний чи ні (Saturday/Sunday)
                var dow = new DateTime(year, month, day).DayOfWeek;
                row[WeekendCol] = (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday);

                // якщо слотів у день нема — заповнюємо "-" і конфлікт = false
                if (daySlots == null || daySlots.Count == 0)
                {
                    row[ConflictCol] = false;

                    for (int i = 0; i < empCols.Length; i++)
                        row[empCols[i].Key] = Empty;

                    table.Rows.Add(row);
                    continue;
                }

                // 8.1) Групуємо слоти по працівнику в цей день
                // byEmp[employeeId] = список слотів цієї людини в цей день
                bool conflict = false;
                var byEmp = new Dictionary<int, List<ScheduleSlotModel>>(capacity: Math.Min(orderedEmployees.Count, 32));

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var s = daySlots[i];

                    // якщо слот не прив’язаний до працівника — це вже конфлікт
                    if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    {
                        conflict = true;
                        continue;
                    }

                    int empId = s.EmployeeId.Value;

                    if (!byEmp.TryGetValue(empId, out var list))
                        byEmp[empId] = list = new List<ScheduleSlotModel>(4);

                    list.Add(s);
                }

                // 8.2) Якщо ще не конфлікт — перевіряємо перетини інтервалів
                if (!conflict)
                {
                    foreach (var kv in byEmp)
                    {
                        if (HasOverlap(kv.Value))
                        {
                            conflict = true;
                            break;
                        }
                    }
                }

                row[ConflictCol] = conflict;

                // 8.3) Заповнюємо клітинки для кожної колонки працівника
                for (int i = 0; i < empCols.Length; i++)
                {
                    var colName = empCols[i].Key;
                    var empId = empCols[i].Value;

                    // якщо в цього працівника нема слотів цього дня — "-"
                    if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                    {
                        row[colName] = Empty;
                        continue;
                    }

                    // мерджимо інтервали, щоб красиво показати
                    var merged = MergeIntervalsForDisplay(empSlots);

                    // записуємо рядок типу "09:00 - 12:00, 13:00 - 15:00"
                    row[colName] = FormatMerged(merged);
                }

                // додаємо рядок в таблицю
                table.Rows.Add(row);
            }

            return table;

            // -----------------------------
            // Локальна функція:
            // перевірка “чи є overlap” у слотів однієї людини в один день
            // -----------------------------
            static bool HasOverlap(List<ScheduleSlotModel> empSlots)
            {
                // збираємо інтервали (from-to) в TimeSpan
                var intervals = new List<(TimeSpan from, TimeSpan to)>(empSlots.Count);

                for (int i = 0; i < empSlots.Count; i++)
                {
                    var s = empSlots[i];

                    // якщо час не парситься — пропускаємо цей слот
                    if (!TryParseTime(s.FromTime, out var from)) continue;
                    if (!TryParseTime(s.ToTime, out var to)) continue;

                    // якщо перехід через 00:00 — робимо to “після” from
                    if (to < from) to += TimeSpan.FromHours(24);

                    intervals.Add((from, to));
                }

                // 0 або 1 інтервал — перетинів бути не може
                if (intervals.Count <= 1)
                    return false;

                // сортуємо по старту
                intervals.Sort((a, b) =>
                {
                    var c = a.from.CompareTo(b.from);
                    return c != 0 ? c : a.to.CompareTo(b.to);
                });

                // дивимось, чи наступний старт починається до того, як закінчився попередній
                var lastEnd = intervals[0].to;

                for (int i = 1; i < intervals.Count; i++)
                {
                    var cur = intervals[i];

                    if (cur.from < lastEnd)
                        return true;

                    if (cur.to > lastEnd)
                        lastEnd = cur.to;
                }

                return false;
            }
        }

        /// <summary>
        /// Обчислює conflict для конкретного дня:
        ///  - якщо є слот без працівника => conflict = true
        ///  - якщо в одного працівника перетинаються інтервали => conflict = true
        /// </summary>
        public static bool ComputeConflictForDay(IList<ScheduleSlotModel> slots, int day)
        {
            // byEmp[empId] = список (from,to) для цього empId в цей день
            var byEmp = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth != day)
                    continue;

                // слот без працівника — одразу конфлікт
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    return true;

                if (!TryParseTime(s.FromTime, out var from))
                    continue;
                if (!TryParseTime(s.ToTime, out var to))
                    continue;

                if (to < from)
                    to += TimeSpan.FromHours(24);

                var empId = s.EmployeeId.Value;

                if (!byEmp.TryGetValue(empId, out var list))
                    byEmp[empId] = list = new List<(TimeSpan, TimeSpan)>();

                list.Add((from, to));
            }

            // перевірка перетинів для кожного працівника
            foreach (var kv in byEmp)
            {
                var list = kv.Value;
                if (list.Count <= 1)
                    continue;

                list.Sort((a, b) => a.from.CompareTo(b.from));

                var lastEnd = list[0].to;
                for (int j = 1; j < list.Count; j++)
                {
                    var cur = list[j];
                    if (cur.from < lastEnd)
                        return true;

                    if (cur.to > lastEnd)
                        lastEnd = cur.to;
                }
            }

            return false;
        }

        /// <summary>
        /// Об’єднує (merge) інтервали для красивого показу.
        /// Напр:
        ///  09:00-12:00 + 11:30-13:00 => 09:00-13:00
        ///
        /// Повертає список рядків (from,to) вже у форматі "HH:mm".
        /// </summary>
        public static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
        {
            // робимо “unique + sort + merge” без LINQ-ланцюжка для контролю логіки
            var unique = new HashSet<(int fromMin, int toMin)>();
            var list = new List<(int fromMin, int toMin)>();

            foreach (var s in slots)
            {
                if (!TryParseTime(s.FromTime, out var f)) continue;
                if (!TryParseTime(s.ToTime, out var t)) continue;

                var fromMin = (int)f.TotalMinutes;
                var toMin = (int)t.TotalMinutes;

                // якщо перехід через 00:00 — додаємо +24h
                if (toMin < fromMin)
                    toMin += 24 * 60;

                // додаємо тільки унікальні інтервали
                if (unique.Add((fromMin, toMin)))
                    list.Add((fromMin, toMin));
            }

            if (list.Count == 0)
                return new List<(string from, string to)>();

            // сортуємо по старту
            list.Sort((a, b) =>
            {
                var c = a.fromMin.CompareTo(b.fromMin);
                return c != 0 ? c : a.toMin.CompareTo(b.toMin);
            });

            // мерджимо пересічні інтервали
            var merged = new List<(int fromMin, int toMin)>(capacity: list.Count);

            var curFrom = list[0].fromMin;
            var curTo = list[0].toMin;

            for (int i = 1; i < list.Count; i++)
            {
                var it = list[i];

                // якщо наступний інтервал починається ДО або В момент закінчення поточного
                // значить вони “зливаються”
                if (it.fromMin <= curTo)
                {
                    if (it.toMin > curTo)
                        curTo = it.toMin;
                }
                else
                {
                    // поточний інтервал завершився — фіксуємо його
                    merged.Add((curFrom, curTo));

                    // починаємо новий
                    curFrom = it.fromMin;
                    curTo = it.toMin;
                }
            }

            // не забуваємо додати останній
            merged.Add((curFrom, curTo));

            // перетворюємо хвилини назад у "HH:mm"
            var result = new List<(string from, string to)>(merged.Count);

            for (int i = 0; i < merged.Count; i++)
            {
                var m = merged[i];

                // % (24*60) — щоб з 1500 хвилин знову зробити час в межах доби
                var from = TimeSpan.FromMinutes(m.fromMin % (24 * 60));
                var to = TimeSpan.FromMinutes(m.toMin % (24 * 60));

                result.Add((from.ToString(@"hh\:mm"), to.ToString(@"hh\:mm")));
            }

            return result;
        }

        /// <summary>
        /// Парсить введення з клітинки.
        /// Підтримка:
        ///  - "-" або пусто => 0 інтервалів (тобто очистка)
        ///  - "09:00 - 12:00"
        ///  - "09:00 - 12:00, 13:00 - 15:00"
        ///
        /// Якщо помилка — повертає false і заповнює error.
        /// </summary>
        public static bool TryParseIntervals(string? text, out List<(string from, string to)> intervals, out string? error)
        {
            intervals = new List<(string from, string to)>();
            error = null;

            // готуємо текст
            text = (text ?? string.Empty).Trim();

            // якщо нічого нема або "-" — це легальна “очистка”
            if (string.IsNullOrWhiteSpace(text) || text == Empty)
                return true;

            // ділимо по комі: "a, b, c"
            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // unique — щоб не додати однаковий інтервал 2 рази
            var unique = new HashSet<(TimeSpan from, TimeSpan to)>();

            foreach (var part in parts)
            {
                // ділимо один інтервал по "-" (тільки 2 частини)
                var dash = part.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (dash.Length != 2)
                {
                    error = "Format: HH:mm - HH:mm (comma separated allowed).";
                    return false;
                }

                // парсимо часи
                if (!TryParseTime(dash[0], out var from) || !TryParseTime(dash[1], out var to))
                {
                    error = "Time must be HH:mm (e.g. 09:00 - 14:30).";
                    return false;
                }

                // правило: старт має бути менший за кінець (без переходу через 00:00 у введенні)
                if (from >= to)
                {
                    error = "From must be earlier than To";
                    return false;
                }

                unique.Add((from, to));
            }

            // сортуємо і перетворюємо в "HH:mm"
            intervals = unique
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();

            return true;
        }

        /// <summary>
        /// Застосовує новий список інтервалів до колекції слотів.
        ///
        /// Важливо: метод НЕ знає про ViewModel, він працює тільки з даними.
        /// Тому приймає:
        ///  - scheduleId (щоб правильно створити нові ScheduleSlotModel)
        ///  - slots (будь-яка IList: ObservableCollection теж підходить)
        ///  - day, empId, intervals
        ///
        /// Логіка:
        /// 1) Запам’ятати старий Status (якщо був) для (day,empId)
        /// 2) Видалити старі слоти (day,empId)
        /// 3) Якщо intervals порожній — все, клітинка очищена
        /// 4) Інакше додати нові слоти і правильно виставити SlotNo
        /// </summary>
        public static void ApplyIntervalsToSlots(
            int scheduleId,
            IList<ScheduleSlotModel> slots,
            int day,
            int empId,
            List<(string from, string to)> intervals)
        {
            // 1) preserve status з першого слоту цього працівника (як було)
            var preservedStatus = SlotStatus.UNFURNISHED;

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth == day && s.EmployeeId == empId)
                {
                    preservedStatus = s.Status;
                    break;
                }
            }

            // 2) Видаляємо старі слоти для (day, empId) (як було)
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var s = slots[i];
                if (s.DayOfMonth == day && s.EmployeeId == empId)
                    slots.RemoveAt(i);
            }

            // 3) Якщо нових інтервалів нема — очистка
            if (intervals.Count == 0)
                return;

            // =========================
            // NEW: спочатку заповнюємо "порожні" слоти (EmployeeId=null) з тим самим from/to
            // =========================
            var intervalsToAdd = new List<(string from, string to)>();

            for (int k = 0; k < intervals.Count; k++)
            {
                var (fromStr, toStr) = intervals[k];

                // нормалізуємо до TimeSpan для коректного матчінгу 9:00 vs 09:00
                if (!TryParseTime(fromStr, out var fromTs) || !TryParseTime(toStr, out var toTs))
                {
                    intervalsToAdd.Add((fromStr, toStr));
                    continue;
                }

                ScheduleSlotModel? vacant = null;

                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.DayOfMonth != day) continue;

                    if (s.EmployeeId.HasValue && s.EmployeeId.Value > 0)
                        continue; // не вакантний

                    if (!TryParseTime(s.FromTime, out var sf) || !TryParseTime(s.ToTime, out var st))
                        continue;

                    if (sf == fromTs && st == toTs)
                    {
                        vacant = s;
                        break;
                    }
                }

                if (vacant != null)
                {
                    // "займаємо" вакантний слот, замість створення нового
                    vacant.EmployeeId = empId;
                    // статус краще не ламати; але якщо треба — можна присвоїти preservedStatus
                    // vacant.Status = preservedStatus;
                }
                else
                {
                    intervalsToAdd.Add((fromStr, toStr));
                }
            }

            // Якщо всі інтервали лягли у вакантні слоти — нічого не додаємо
            if (intervalsToAdd.Count == 0)
                return;

            // 4) usedByKey як було
            var usedByKey = new Dictionary<(int day, string from, string to), HashSet<int>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth != day) continue;
                if (string.IsNullOrWhiteSpace(s.FromTime) || string.IsNullOrWhiteSpace(s.ToTime)) continue;

                var key = (day, s.FromTime, s.ToTime);

                if (!usedByKey.TryGetValue(key, out var set))
                    usedByKey[key] = set = new HashSet<int>();

                set.Add(s.SlotNo);
            }

            // 5) Додаємо тільки те, що не вдалося покласти у vacancy
            foreach (var (from, to) in intervalsToAdd)
            {
                var key = (day, from, to);

                if (!usedByKey.TryGetValue(key, out var used))
                    usedByKey[key] = used = new HashSet<int>();

                var slotNo = 1;
                while (used.Contains(slotNo)) slotNo++;

                used.Add(slotNo);

                slots.Add(new ScheduleSlotModel
                {
                    ScheduleId = scheduleId,
                    DayOfMonth = day,
                    EmployeeId = empId,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = slotNo,
                    Status = preservedStatus
                });
            }
        }

        public static bool ComputeConflictForDayWithStaffing(
    IList<ScheduleSlotModel> slots,
    int day,
    int peoplePerShift,
    string? shift1Range,
    string? shift2Range)
        {
            // 0) захист
            if (peoplePerShift <= 0)
                peoplePerShift = 1;

            // 1) парсимо shift ranges (приймаємо "09:00 - 15:00" і "09:00-15:00")
            var shifts = new List<(TimeSpan from, TimeSpan to)>(2);

            if (TryParseShiftRange(shift1Range, out var sh1)) shifts.Add(sh1);
            if (TryParseShiftRange(shift2Range, out var sh2)) shifts.Add(sh2);

            // якщо shift-и не парсяться — fallback на стару логіку
            if (shifts.Count == 0)
                return ComputeConflictForDay(slots, day);

            // 2) конфлікт по overlap для ОДНОГО працівника (залишаємо твою ідею)
            //    (не рахуємо vacancy як конфлікт тут — бо staffing це покриє)
            var byEmp = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth != day) continue;
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0) continue;

                if (!TryParseTime(s.FromTime, out var from)) continue;
                if (!TryParseTime(s.ToTime, out var to)) continue;

                if (to < from) to += TimeSpan.FromHours(24);

                var empId = s.EmployeeId.Value;
                if (!byEmp.TryGetValue(empId, out var list))
                    byEmp[empId] = list = new List<(TimeSpan, TimeSpan)>();

                list.Add((from, to));
            }

            foreach (var kv in byEmp)
            {
                var list = kv.Value;
                if (list.Count <= 1) continue;

                list.Sort((a, b) => a.from.CompareTo(b.from));
                var lastEnd = list[0].to;

                for (int j = 1; j < list.Count; j++)
                {
                    var cur = list[j];
                    if (cur.from < lastEnd)
                        return true;

                    if (cur.to > lastEnd)
                        lastEnd = cur.to;
                }
            }

            // 3) staffing-check: для кожної зміни рахуємо людей, які ПОВНІСТЮ покривають зміну
            foreach (var (shiftFrom, shiftTo) in shifts)
            {
                var covered = new HashSet<int>();

                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.DayOfMonth != day) continue;
                    if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0) continue;

                    if (!TryParseTime(s.FromTime, out var from)) continue;
                    if (!TryParseTime(s.ToTime, out var to)) continue;

                    if (to < from) to += TimeSpan.FromHours(24);

                    // shiftTo вже нормалізований в TryParseShiftRange (може бути +24h)
                    if (from <= shiftFrom && to >= shiftTo)
                        covered.Add(s.EmployeeId.Value);
                }

                if (covered.Count < peoplePerShift)
                    return true;
            }

            return false;

            static bool TryParseShiftRange(string? s, out (TimeSpan from, TimeSpan to) shift)
            {
                shift = default;

                s = (s ?? string.Empty).Trim()
                    .Replace('–', '-')  // на всякий випадок
                    .Replace('—', '-');

                if (s.Length == 0) return false;

                var parts = s.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return false;

                if (!TryParseTime(parts[0], out var from)) return false;
                if (!TryParseTime(parts[1], out var to)) return false;

                if (to < from) to += TimeSpan.FromHours(24); // дозволяємо нічні зміни
                if (to == from) return false;

                shift = (from, to);
                return true;
            }
        }

    }
}
