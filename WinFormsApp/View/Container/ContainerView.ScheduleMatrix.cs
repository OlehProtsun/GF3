using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private bool _availabilityPreviewGridConfigured;
        private void RequestScheduleGridRefresh() 
        {
            if (IsDisposed) return;

            // ✅ future-proof: якщо прилетить з background thread
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RequestScheduleGridRefresh));
                return;
            }

            if (!IsHandleCreated)
            {
                RefreshScheduleGridCore();
                return;
            }

            if (_scheduleRefreshPending) return;
            _scheduleRefreshPending = true;

            BeginInvoke(new Action(() =>
            {
                _scheduleRefreshPending = false;
                RefreshScheduleGridCore();
            }));

        }
        private void RefreshScheduleGridCore() 
        {
            // 1) спробувати вийти з редагування
            CancelGridEditSafely(slotGrid);

            // 2) якщо все ще не можемо вийти — відкласти (1-2 рази)
            if (slotGrid.IsCurrentCellInEditMode || slotGrid.IsCurrentRowDirty)
            {
                if (_rebindRetry++ < 2)
                {
                    BeginInvoke(new Action(RefreshScheduleGridCore));
                    return;
                }

                _rebindRetry = 0;
                return;
            }

            _rebindRetry = 0;

            // ✅ один-єдиний binding
            _scheduleTable = BindScheduleMatrix(
                grid: slotGrid,
                year: ScheduleYear,
                month: ScheduleMonth,
                slots: _slots,
                employees: _employees,
                readOnly: false,
                configureGrid: false
            );

        }

        private DataTable BindScheduleMatrix(
            Guna2DataGridView grid,
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            bool readOnly,
            bool configureGrid)
        {
            var table = BuildScheduleTable(year, month, slots, employees, out var map);

            // map потрібний тільки для editable grid (slotGrid)
            if (!readOnly)
                _colNameToEmpId = map;

            grid.SuspendLayout();
            try
            {
                if (configureGrid)
                    ConfigureMatrixGrid(grid, readOnly);

                grid.DataSource = null;
                grid.AutoGenerateColumns = true;
                grid.DataSource = table;

                ApplyDayAndConflictColumnsStyle(grid);
            }
            finally
            {
                grid.ResumeLayout();
            }

            return table;

        }

        private DataTable BuildScheduleTable(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId)
        {
            colNameToEmpId = new Dictionary<string, int>();
            var table = new DataTable();

            // Day + Conflict (ОДИН раз)
            table.Columns.Add(DayCol, typeof(int));
            table.Columns.Add(ConflictCol, typeof(bool));

            // employee columns
            foreach (var emp in employees)
            {
                var displayName = $"{emp.Employee.FirstName} {emp.Employee.LastName}";
                var columnName = displayName;
                var suffix = 1;

                while (table.Columns.Contains(columnName))
                    columnName = $"{displayName} ({++suffix})";

                table.Columns.Add(columnName, typeof(string));
                colNameToEmpId[columnName] = emp.EmployeeId;
            }

            // ✅ завжди будуємо всі дні місяця
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // day -> slots list (може бути пусто)
            var byDay = (slots ?? Array.Empty<ScheduleSlotModel>())
                .GroupBy(s => s.DayOfMonth)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (int day = 1; day <= daysInMonth; day++)
            {
                byDay.TryGetValue(day, out var daySlots);

                var row = table.NewRow();
                row[DayCol] = day;

                if (daySlots == null || daySlots.Count == 0)
                {
                    row[ConflictCol] = false;

                    // якщо колонок працівників нема — просто додаємо Day
                    // якщо колонки є — заповнюємо "-"
                    foreach (var (colName, _) in colNameToEmpId)
                        row[colName] = EmptyMark;

                    table.Rows.Add(row);
                    continue;
                }

                row[ConflictCol] = daySlots.Any(s => s.EmployeeId == null);

                if (colNameToEmpId.Count > 0)
                {
                    var byEmp = daySlots
                        .Where(s => s.EmployeeId != null)
                        .GroupBy(s => s.EmployeeId!.Value)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var (colName, empId) in colNameToEmpId)
                    {
                        if (!byEmp.TryGetValue(empId, out var empSlots) || empSlots.Count == 0)
                        {
                            row[colName] = EmptyMark;
                            continue;
                        }

                        var merged = MergeIntervalsForDisplay(empSlots);
                        row[colName] = merged.Count == 0
                            ? EmptyMark
                            : string.Join(", ", merged.Select(i => $"{i.from} - {i.to}"));
                    }
                }

                table.Rows.Add(row);
            }

            return table;

        }

        private static void CancelGridEditSafely(DataGridView grid)
        {
            if (grid == null) return;

            // 0) Якщо є dirty cell — спробувати закомітити/скинути
            try { grid.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch { }
            try { grid.EndEdit(); } catch { }

            // 1) Скасувати редактор комірки
            try { grid.CancelEdit(); } catch { }

            // 2) CurrencyManager для DataTable/DataView
            try
            {
                if (grid.BindingContext != null && grid.DataSource != null)
                {
                    if (grid.BindingContext[grid.DataSource] is CurrencyManager cm)
                    {
                        try { cm.EndCurrentEdit(); } catch { }
                        try { cm.CancelCurrentEdit(); } catch { }
                    }
                }
            }
            catch { }

        }
        private static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots) 
        {
            var list = new List<(TimeSpan from, TimeSpan to)>();

            foreach (var s in slots)
            {
                if (!TryParseTime(s.FromTime, out var f)) continue;
                if (!TryParseTime(s.ToTime, out var t)) continue;
                list.Add((f, t));
            }

            list = list
                .Distinct()
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .ToList();

            if (list.Count == 0) return new();

            var merged = new List<(TimeSpan from, TimeSpan to)>();
            foreach (var cur in list)
            {
                if (merged.Count == 0)
                {
                    merged.Add(cur);
                    continue;
                }

                var last = merged[^1];

                if (cur.from <= last.to)
                {
                    merged[^1] = (last.from, cur.to > last.to ? cur.to : last.to);
                }
                else
                {
                    merged.Add(cur);
                }
            }

            return merged
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();

        }

        private void SlotGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e) 
        {
            _oldCellValue = slotGrid[e.ColumnIndex, e.RowIndex].Value;
        }
        private void SlotGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e) 
        {
            // Ми не робимо ніякого Cancel тут взагалі.
            // Вся логіка парсингу/відкату — в CellEndEdit (як у тебе і задумано).
            return;
        }
        private void SlotGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e) 
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (slotGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView rowView)
                return;

            var colName = slotGrid.Columns[e.ColumnIndex].Name;
            if (colName == DayCol || colName == ConflictCol) return;

            if (!_colNameToEmpId.TryGetValue(colName, out var empId))
                return;

            var day = (int)rowView[DayCol];
            var raw = (rowView[colName]?.ToString() ?? EmptyMark).Trim();

            if (!TryParseIntervals(raw, out var intervals, out var error))
            {
                rowView[colName] = (_oldCellValue?.ToString() ?? EmptyMark).Trim();
                ShowError(error ?? "Invalid format.");
                return;
            }

            ApplyIntervalsToSlots(day, empId, intervals);

            rowView[colName] = intervals.Count == 0
                ? EmptyMark
                : string.Join(", ", intervals.Select(i => $"{i.from} - {i.to}"));


        }

        private static bool TryParseIntervals(string? text, out List<(string from, string to)> intervals, out string? error)
        {
            intervals = new();
            error = null;

            text = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text) || text == EmptyMark)
                return true;

            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var parsed = new List<(TimeSpan from, TimeSpan to)>();
            foreach (var p in parts)
            {
                var dash = p.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dash.Length != 2)
                {
                    error = "Format: HH:mm - HH:mm (можна кілька через кому)";
                    return false;
                }

                if (!TryParseTime(dash[0], out var from) || !TryParseTime(dash[1], out var to))
                {
                    error = "Time must be HH:mm (наприклад 09:00 - 14:30)";
                    return false;
                }

                if (from >= to)
                {
                    error = "From must be earlier than To";
                    return false;
                }

                parsed.Add((from, to));
            }

            // ✅ distinct + сорт без повторного ParseExact
            var unique = parsed
                .Distinct()
                .OrderBy(x => x.from)
                .ThenBy(x => x.to)
                .ToList();

            intervals = unique
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();

            return true;

        }

        private static bool TryParseTime(string s, out TimeSpan t) 
        {
            return TimeSpan.TryParseExact(
            s.Trim(),
            new[] { @"h\:mm", @"hh\:mm" },
            CultureInfo.InvariantCulture,
            out t);
        }

        private void ApplyIntervalsToSlots(int day, int empId, List<(string from, string to)> intervals)
        {
            var preservedStatus =
            _slots.FirstOrDefault(s => s.DayOfMonth == day && s.EmployeeId == empId)?.Status
            ?? SlotStatus.UNFURNISHED;

            _slots.RemoveAll(s => s.DayOfMonth == day && s.EmployeeId == empId);

            foreach (var (from, to) in intervals)
            {
                _slots.Add(new ScheduleSlotModel
                {
                    ScheduleId = ScheduleId,
                    DayOfMonth = day,
                    EmployeeId = empId,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = NextFreeSlotNo(_slots, day, from, to),
                    Status = preservedStatus
                });
            }

        }

        private static int NextFreeSlotNo(List<ScheduleSlotModel> list, int day, string from, string to)
        {
            var used = list
            .Where(s => s.DayOfMonth == day && s.FromTime == from && s.ToTime == to)
            .Select(s => s.SlotNo)
            .ToHashSet();

            var n = 1;
            while (used.Contains(n)) n++;
            return n;

        }

        public void SetAvailabilityPreviewMatrix(int year, int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees)
        {
            // bind у dataGridAvailabilityOnScheduleEdit (readonly)
            BindScheduleMatrix(
                grid: dataGridAvailabilityOnScheduleEdit,
                year: year,
                month: month,
                slots: slots ?? new List<ScheduleSlotModel>(),
                employees: employees ?? new List<ScheduleEmployeeModel>(),
                readOnly: true,
                configureGrid: !_availabilityPreviewGridConfigured
            );

            _availabilityPreviewGridConfigured = true;
        }

        public void ClearAvailabilityPreviewMatrix()
        {
            SetAvailabilityPreviewMatrix(ScheduleYear, ScheduleMonth,
                new List<ScheduleSlotModel>(),
                new List<ScheduleEmployeeModel>());
        }
    }
}
