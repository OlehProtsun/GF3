/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleMatrixEngine у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
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
    /// Визначає публічний елемент `public static class ScheduleMatrixEngine` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ScheduleMatrixEngine
    {
        
        private static string DayCol => ScheduleMatrixConstants.DayColumnName;
        private static string ConflictCol => ScheduleMatrixConstants.ConflictColumnName;
        private static string WeekendCol => ScheduleMatrixConstants.WeekendColumnName;
        private static string Empty => ScheduleMatrixConstants.EmptyMark;

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryParseTime(string? s, out TimeSpan t)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            return BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(s, out t);
        }

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static DataTable BuildScheduleTable(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static DataTable BuildScheduleTable(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId,
            CancellationToken ct = default)
        {
            
            colNameToEmpId = new Dictionary<string, int>();
            var table = new DataTable();

            
            table.Columns.Add(DayCol, typeof(int));
            table.Columns.Add(ConflictCol, typeof(bool));
            table.Columns.Add(WeekendCol, typeof(bool)); 

            
            
            
            
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
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    var d = s.DayOfMonth;

                    
                    if ((uint)d > (uint)daysInMonth || d <= 0)
                        continue;

                    
                    var list = slotsByDay[d];
                    if (list == null)
                    {
                        list = new List<ScheduleSlotModel>(8);
                        slotsByDay[d] = list;
                    }

                    
                    list.Add(s);
                }
            }

            
            
            var empCols = colNameToEmpId.ToArray();

            
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

            
            for (int day = 1; day <= daysInMonth; day++)
            {
                
                ct.ThrowIfCancellationRequested();

                var daySlots = slotsByDay[day];

                
                var row = table.NewRow();

                
                row[DayCol] = day;

                
                var dow = new DateTime(year, month, day).DayOfWeek;
                row[WeekendCol] = (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday);

                
                if (daySlots == null || daySlots.Count == 0)
                {
                    row[ConflictCol] = false;

                    for (int i = 0; i < empCols.Length; i++)
                        row[empCols[i].Key] = Empty;

                    table.Rows.Add(row);
                    continue;
                }

                
                
                bool conflict = false;
                var byEmp = new Dictionary<int, List<ScheduleSlotModel>>(capacity: Math.Min(orderedEmployees.Count, 32));

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var s = daySlots[i];

                    
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

                
                for (int i = 0; i < empCols.Length; i++)
                {
                    var colName = empCols[i].Key;
                    var empId = empCols[i].Value;

                    
                    if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                    {
                        row[colName] = Empty;
                        continue;
                    }

                    
                    var merged = MergeIntervalsForDisplay(empSlots);

                    
                    row[colName] = FormatMerged(merged);
                }

                
                table.Rows.Add(row);
            }

            return table;

            
            
            
            
            static bool HasOverlap(List<ScheduleSlotModel> empSlots)
            {
                
                var intervals = new List<(TimeSpan from, TimeSpan to)>(empSlots.Count);

                for (int i = 0; i < empSlots.Count; i++)
                {
                    var s = empSlots[i];

                    
                    if (!TryParseTime(s.FromTime, out var from)) continue;
                    if (!TryParseTime(s.ToTime, out var to)) continue;

                    
                    if (to < from) to += TimeSpan.FromHours(24);

                    intervals.Add((from, to));
                }

                
                if (intervals.Count <= 1)
                    return false;

                
                intervals.Sort((a, b) =>
                {
                    var c = a.from.CompareTo(b.from);
                    return c != 0 ? c : a.to.CompareTo(b.to);
                });

                
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
        /// Визначає публічний елемент `public static bool ComputeConflictForDay(IList<ScheduleSlotModel> slots, int day)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool ComputeConflictForDay(IList<ScheduleSlotModel> slots, int day)
        {
            
            var byEmp = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.DayOfMonth != day)
                    continue;

                
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
        /// Визначає публічний елемент `public static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
        {
            
            var unique = new HashSet<(int fromMin, int toMin)>();
            var list = new List<(int fromMin, int toMin)>();

            foreach (var s in slots)
            {
                if (!TryParseTime(s.FromTime, out var f)) continue;
                if (!TryParseTime(s.ToTime, out var t)) continue;

                var fromMin = (int)f.TotalMinutes;
                var toMin = (int)t.TotalMinutes;

                
                if (toMin < fromMin)
                    toMin += 24 * 60;

                
                if (unique.Add((fromMin, toMin)))
                    list.Add((fromMin, toMin));
            }

            if (list.Count == 0)
                return new List<(string from, string to)>();

            
            list.Sort((a, b) =>
            {
                var c = a.fromMin.CompareTo(b.fromMin);
                return c != 0 ? c : a.toMin.CompareTo(b.toMin);
            });

            
            var merged = new List<(int fromMin, int toMin)>(capacity: list.Count);

            var curFrom = list[0].fromMin;
            var curTo = list[0].toMin;

            for (int i = 1; i < list.Count; i++)
            {
                var it = list[i];

                
                
                if (it.fromMin <= curTo)
                {
                    if (it.toMin > curTo)
                        curTo = it.toMin;
                }
                else
                {
                    
                    merged.Add((curFrom, curTo));

                    
                    curFrom = it.fromMin;
                    curTo = it.toMin;
                }
            }

            
            merged.Add((curFrom, curTo));

            
            var result = new List<(string from, string to)>(merged.Count);

            for (int i = 0; i < merged.Count; i++)
            {
                var m = merged[i];

                
                var from = TimeSpan.FromMinutes(m.fromMin % (24 * 60));
                var to = TimeSpan.FromMinutes(m.toMin % (24 * 60));

                result.Add((from.ToString(@"hh\:mm"), to.ToString(@"hh\:mm")));
            }

            return result;
        }

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryParseIntervals(string? text, out List<(string from, string to)> intervals, out string? error)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryParseIntervals(string? text, out List<(string from, string to)> intervals, out string? error)
        {
            intervals = new List<(string from, string to)>();
            error = null;

            
            text = (text ?? string.Empty).Trim();

            
            if (string.IsNullOrWhiteSpace(text) || text == Empty)
                return true;

            
            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            
            var unique = new HashSet<(TimeSpan from, TimeSpan to)>();

            foreach (var part in parts)
            {
                
                var dash = part.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (dash.Length != 2)
                {
                    error = "Format: HH:mm - HH:mm (comma separated allowed).";
                    return false;
                }

                
                if (!TryParseTime(dash[0], out var from) || !TryParseTime(dash[1], out var to))
                {
                    error = "Time must be HH:mm (e.g. 09:00 - 14:30).";
                    return false;
                }

                
                if (from >= to)
                {
                    error = "From must be earlier than To";
                    return false;
                }

                unique.Add((from, to));
            }

            
            intervals = unique
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();

            return true;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void ApplyIntervalsToSlots(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void ApplyIntervalsToSlots(
            int scheduleId,
            IList<ScheduleSlotModel> slots,
            int day,
            int empId,
            List<(string from, string to)> intervals)
        {
            
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

            
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var s = slots[i];
                if (s.DayOfMonth == day && s.EmployeeId == empId)
                    slots.RemoveAt(i);
            }

            
            if (intervals.Count == 0)
                return;

            
            
            
            var intervalsToAdd = new List<(string from, string to)>();

            for (int k = 0; k < intervals.Count; k++)
            {
                var (fromStr, toStr) = intervals[k];

                
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
                        continue; 

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
                    
                    vacant.EmployeeId = empId;
                    
                    
                }
                else
                {
                    intervalsToAdd.Add((fromStr, toStr));
                }
            }

            
            if (intervalsToAdd.Count == 0)
                return;

            
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

        /// <summary>
        /// Визначає публічний елемент `public static bool ComputeConflictForDayWithStaffing(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool ComputeConflictForDayWithStaffing(
    IList<ScheduleSlotModel> slots,
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
                    .Replace('–', '-')  
                    .Replace('—', '-');

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

    }
}
