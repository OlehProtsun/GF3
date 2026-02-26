using System.Data;
using System.Globalization;
using System.Text;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Schedule
{
    public static class ScheduleMatrixEngine
    {
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            s = (s ?? string.Empty).Trim();
            return TimeSpan.TryParseExact(
                s,
                ScheduleMatrixConstants.TimeFormats,
                CultureInfo.InvariantCulture,
                out t);
        }

        public static DataTable BuildScheduleTable(
            int year,
            int month,
            IReadOnlyList<ScheduleSlotModel> slots,
            IReadOnlyList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId,
            CancellationToken ct = default)
        {
            colNameToEmpId = new Dictionary<string, int>();
            var table = new DataTable();

            table.Columns.Add(ScheduleMatrixConstants.DayColumnName, typeof(int));
            table.Columns.Add(ScheduleMatrixConstants.ConflictColumnName, typeof(bool));
            table.Columns.Add(ScheduleMatrixConstants.WeekendColumnName, typeof(bool));

            var orderedEmployees = (employees ?? Array.Empty<ScheduleEmployeeModel>())
                .Where(e => e != null)
                .OrderBy(e => (e.Employee?.LastName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => (e.Employee?.FirstName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => e.EmployeeId)
                .ToList();

            var seenEmpIds = new HashSet<int>();
            foreach (var emp in orderedEmployees)
            {
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

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var slotsByDay = new List<ScheduleSlotModel>?[daysInMonth + 1];

            if (slots != null && slots.Count > 0)
            {
                for (var i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    var day = s.DayOfMonth;
                    if ((uint)day > (uint)daysInMonth || day <= 0)
                        continue;

                    var list = slotsByDay[day];
                    if (list is null)
                    {
                        list = new List<ScheduleSlotModel>(8);
                        slotsByDay[day] = list;
                    }

                    list.Add(s);
                }
            }

            var empCols = colNameToEmpId.ToArray();

            static string FormatMerged(List<(string from, string to)> merged)
            {
                if (merged == null || merged.Count == 0)
                    return ScheduleMatrixConstants.EmptyMark;

                var sb = new StringBuilder(merged.Count * 14);
                for (var i = 0; i < merged.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(merged[i].from).Append(" - ").Append(merged[i].to);
                }

                return sb.ToString();
            }

            for (var day = 1; day <= daysInMonth; day++)
            {
                ct.ThrowIfCancellationRequested();
                var daySlots = slotsByDay[day];
                var row = table.NewRow();

                row[ScheduleMatrixConstants.DayColumnName] = day;
                var dow = new DateTime(year, month, day).DayOfWeek;
                row[ScheduleMatrixConstants.WeekendColumnName] = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;

                if (daySlots == null || daySlots.Count == 0)
                {
                    row[ScheduleMatrixConstants.ConflictColumnName] = false;
                    for (var i = 0; i < empCols.Length; i++)
                        row[empCols[i].Key] = ScheduleMatrixConstants.EmptyMark;

                    table.Rows.Add(row);
                    continue;
                }

                var conflict = false;
                var byEmp = new Dictionary<int, List<ScheduleSlotModel>>(Math.Min(orderedEmployees.Count, 32));

                for (var i = 0; i < daySlots.Count; i++)
                {
                    var s = daySlots[i];
                    if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0)
                    {
                        conflict = true;
                        continue;
                    }

                    var empId = s.EmployeeId.Value;
                    if (!byEmp.TryGetValue(empId, out var list))
                        byEmp[empId] = list = new List<ScheduleSlotModel>(4);
                    list.Add(s);
                }

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

                row[ScheduleMatrixConstants.ConflictColumnName] = conflict;

                for (var i = 0; i < empCols.Length; i++)
                {
                    var colName = empCols[i].Key;
                    var empId = empCols[i].Value;

                    if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                    {
                        row[colName] = ScheduleMatrixConstants.EmptyMark;
                        continue;
                    }

                    row[colName] = FormatMerged(MergeIntervalsForDisplay(empSlots));
                }

                table.Rows.Add(row);
            }

            return table;

            static bool HasOverlap(List<ScheduleSlotModel> empSlots)
            {
                var intervals = new List<(TimeSpan from, TimeSpan to)>(empSlots.Count);
                for (var i = 0; i < empSlots.Count; i++)
                {
                    var s = empSlots[i];
                    if (!TryParseTime(s.FromTime, out var from)) continue;
                    if (!TryParseTime(s.ToTime, out var to)) continue;
                    if (to < from) to += TimeSpan.FromHours(24);
                    intervals.Add((from, to));
                }

                if (intervals.Count <= 1) return false;

                intervals.Sort((a, b) =>
                {
                    var c = a.from.CompareTo(b.from);
                    return c != 0 ? c : a.to.CompareTo(b.to);
                });

                var lastEnd = intervals[0].to;
                for (var i = 1; i < intervals.Count; i++)
                {
                    var cur = intervals[i];
                    if (cur.from < lastEnd) return true;
                    if (cur.to > lastEnd) lastEnd = cur.to;
                }

                return false;
            }
        }

        public static bool ComputeConflictForDay(IReadOnlyList<ScheduleSlotModel> slots, int day)
        {
            var byEmp = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();

            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth != day) continue;
                if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0) return true;

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
                for (var j = 1; j < list.Count; j++)
                {
                    var cur = list[j];
                    if (cur.from < lastEnd) return true;
                    if (cur.to > lastEnd) lastEnd = cur.to;
                }
            }

            return false;
        }

        public static bool ComputeConflictForDayWithStaffing(
            IReadOnlyList<ScheduleSlotModel> slots,
            int day,
            int peoplePerShift,
            string? shift1Range,
            string? shift2Range)
        {
            if (peoplePerShift <= 0)
                peoplePerShift = 1;

            var shifts = new List<(TimeSpan from, TimeSpan to)>(2);
            if (TryParseShiftRange(shift1Range, out var sh1)) shifts.Add(sh1);
            if (TryParseShiftRange(shift2Range, out var sh2)) shifts.Add(sh2);

            if (shifts.Count == 0)
                return ComputeConflictForDay(slots, day);

            var byEmp = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();
            for (var i = 0; i < slots.Count; i++)
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
                for (var j = 1; j < list.Count; j++)
                {
                    var cur = list[j];
                    if (cur.from < lastEnd) return true;
                    if (cur.to > lastEnd) lastEnd = cur.to;
                }
            }

            foreach (var (shiftFrom, shiftTo) in shifts)
            {
                var covered = new HashSet<int>();
                for (var i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.DayOfMonth != day) continue;
                    if (!s.EmployeeId.HasValue || s.EmployeeId.Value <= 0) continue;
                    if (!TryParseTime(s.FromTime, out var from)) continue;
                    if (!TryParseTime(s.ToTime, out var to)) continue;

                    if (to < from) to += TimeSpan.FromHours(24);
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
                s = (s ?? string.Empty).Trim().Replace('–', '-').Replace('—', '-');
                if (s.Length == 0) return false;

                var parts = s.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return false;

                if (!TryParseTime(parts[0], out var from)) return false;
                if (!TryParseTime(parts[1], out var to)) return false;
                if (to < from) to += TimeSpan.FromHours(24);
                if (to == from) return false;

                shift = (from, to);
                return true;
            }
        }

        public static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
        {
            var unique = new HashSet<(int fromMin, int toMin)>();
            var list = new List<(int fromMin, int toMin)>();

            foreach (var s in slots)
            {
                if (!TryParseTime(s.FromTime, out var fromTs)) continue;
                if (!TryParseTime(s.ToTime, out var toTs)) continue;

                var fromMin = (int)fromTs.TotalMinutes;
                var toMin = (int)toTs.TotalMinutes;
                if (toMin < fromMin) toMin += 24 * 60;

                if (unique.Add((fromMin, toMin)))
                    list.Add((fromMin, toMin));
            }

            if (list.Count == 0)
                return [];

            list.Sort((a, b) =>
            {
                var c = a.fromMin.CompareTo(b.fromMin);
                return c != 0 ? c : a.toMin.CompareTo(b.toMin);
            });

            var merged = new List<(int fromMin, int toMin)>(list.Count);
            var curFrom = list[0].fromMin;
            var curTo = list[0].toMin;

            for (var i = 1; i < list.Count; i++)
            {
                var item = list[i];
                if (item.fromMin <= curTo)
                {
                    if (item.toMin > curTo)
                        curTo = item.toMin;
                }
                else
                {
                    merged.Add((curFrom, curTo));
                    curFrom = item.fromMin;
                    curTo = item.toMin;
                }
            }

            merged.Add((curFrom, curTo));

            var result = new List<(string from, string to)>(merged.Count);
            for (var i = 0; i < merged.Count; i++)
            {
                var item = merged[i];
                var from = TimeSpan.FromMinutes(item.fromMin % (24 * 60));
                var to = TimeSpan.FromMinutes(item.toMin % (24 * 60));
                result.Add((from.ToString(@"hh\:mm"), to.ToString(@"hh\:mm")));
            }

            return result;
        }
    }
}
