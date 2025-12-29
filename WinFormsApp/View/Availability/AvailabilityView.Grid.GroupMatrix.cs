using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private void ConfigureAvailabilityGroupGrid()
        {
            dataGridAvailabilityDays.AutoGenerateColumns = true;
            dataGridAvailabilityDays.DataSource = _groupTable;

            dataGridAvailabilityDays.RowHeadersVisible = false;
            dataGridAvailabilityDays.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridAvailabilityDays.MultiSelect = false;
            dataGridAvailabilityDays.AllowUserToAddRows = false;
            dataGridAvailabilityDays.AllowUserToDeleteRows = false;

            dataGridAvailabilityDays.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridAvailabilityDays.ColumnHeadersHeight = 36;
            dataGridAvailabilityDays.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridAvailabilityDays.AllowUserToResizeRows = false;

            dataGridAvailabilityDays.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridAvailabilityDays.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // ✅ як slotGrid
            dataGridAvailabilityDays.CellBorderStyle = DataGridViewCellBorderStyle.None;
            ApplyAvailabilityGridLook(dataGridAvailabilityDays);

            dataGridAvailabilityDays.CellPainting -= MatrixGrid_CellPainting;
            dataGridAvailabilityDays.CellPainting += MatrixGrid_CellPainting;

            dataGridAvailabilityDays.DataBindingComplete -= DataGridAvailabilityDays_DataBindingComplete;
            dataGridAvailabilityDays.DataBindingComplete += DataGridAvailabilityDays_DataBindingComplete;

            // edit за замовчуванням (для таба Create/Edit)
            dataGridAvailabilityDays.ReadOnly = false;
            dataGridAvailabilityDays.ThemeStyle.ReadOnly = false;
            dataGridAvailabilityDays.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        }

        private void DataGridAvailabilityDays_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not DataGridView grid) return;

            ApplyMatrixHeadersFromDataTable(grid, grid.DataSource as DataTable);

            if (grid.Columns.Contains(DayCol))
            {
                var col = grid.Columns[DayCol];
                col.HeaderText = "Day";
                col.ReadOnly = true;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = 60;
                col.MinimumWidth = 60;
                col.Resizable = DataGridViewTriState.False;
                col.Frozen = true;
            }

            bool isProfile = ReferenceEquals(grid, dataGridAvailabilityMonthProfile);

            if (grid is Guna.UI2.WinForms.Guna2DataGridView g2)
            {
                g2.ReadOnly = isProfile;
                g2.ThemeStyle.ReadOnly = isProfile;
                g2.EditMode = isProfile
                    ? DataGridViewEditMode.EditProgrammatically
                    : DataGridViewEditMode.EditOnKeystrokeOrF2;
            }
            else
            {
                grid.ReadOnly = isProfile;
                grid.EditMode = isProfile
                    ? DataGridViewEditMode.EditProgrammatically
                    : DataGridViewEditMode.EditOnKeystrokeOrF2;
            }

            foreach (DataGridViewColumn c in grid.Columns)
            {
                if (c.Name == DayCol) continue;
                c.ReadOnly = isProfile;
            }
        }

        private void RegenerateGroupDays()
        {
            var year = Year;
            var month = Month;
            if (year <= 0 || month <= 0) return;

            int daysInMonth = DateTime.DaysInMonth(year, month);

            // 1) якщо перший раз — створюємо Day колонку
            if (!_groupTable.Columns.Contains(DayCol))
            {
                _groupTable.Columns.Add(new DataColumn(DayCol, typeof(int)));
            }

            // 2) кешуємо старі значення: (day, colName) -> value
            var old = new Dictionary<(int day, string col), string>();

            foreach (DataRow r in _groupTable.Rows)
            {
                var d = Convert.ToInt32(r[DayCol]);
                foreach (DataColumn c in _groupTable.Columns)
                {
                    if (c.ColumnName == DayCol) continue;
                    old[(d, c.ColumnName)] = Convert.ToString(r[c]) ?? string.Empty;
                }
            }

            // 3) пересоздаємо рядки
            _groupTable.Rows.Clear();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var row = _groupTable.NewRow();
                row[DayCol] = day;

                foreach (DataColumn c in _groupTable.Columns)
                {
                    if (c.ColumnName == DayCol) continue;
                    row[c.ColumnName] = old.TryGetValue((day, c.ColumnName), out var v) ? v : string.Empty;
                }

                _groupTable.Rows.Add(row);
            }
        }

        public bool TryAddEmployeeColumn(int employeeId, string header)
        {
            if (employeeId <= 0) return false;
            if (_employeeIdToColumn.ContainsKey(employeeId)) return false;

            var colName = $"emp_{employeeId}";

            if (_groupTable.Columns.Contains(colName))
                return false;

            var col = new DataColumn(colName, typeof(string))
            {
                Caption = header
            };

            _groupTable.Columns.Add(col);
            _employeeIdToColumn[employeeId] = colName;

            foreach (DataRow r in _groupTable.Rows)
                r[colName] = string.Empty;

            return true;
        }

        public bool RemoveEmployeeColumn(int employeeId)
        {
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return false;

            _employeeIdToColumn.Remove(employeeId);

            // 1) прибираємо з DataTable (це головне)
            if (_groupTable.Columns.Contains(colName))
                _groupTable.Columns.Remove(colName);

            // 2) на всякий випадок прибираємо з грида (інколи UI може “тримати” колонку)
            if (dataGridAvailabilityDays.Columns.Contains(colName))
                dataGridAvailabilityDays.Columns.Remove(colName);

            return true;
        }

        public IReadOnlyList<int> GetSelectedEmployeeIds()
            => _employeeIdToColumn.Keys.ToList();

        public IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes()
        {
            var result = new List<(int employeeId, IList<(int day, string code)> codes)>();

            foreach (var kv in _employeeIdToColumn)
            {
                var employeeId = kv.Key;
                var colName = kv.Value;

                var list = new List<(int day, string code)>();

                foreach (DataRow r in _groupTable.Rows)
                {
                    var day = Convert.ToInt32(r[DayCol]);
                    var code = Convert.ToString(r[colName]) ?? string.Empty;
                    list.Add((day, code));
                }

                result.Add((employeeId, list));
            }

            return result;
        }

        public void ResetGroupMatrix()
        {
            // прибираємо всі employee колонки
            var toRemove = _employeeIdToColumn.Keys.ToList();
            foreach (var empId in toRemove)
                RemoveEmployeeColumn(empId);

            // чистимо рядки та генеруємо дні під поточний місяць
            _groupTable.Rows.Clear();
            RegenerateGroupDays();
        }

        public void SetEmployeeCodes(int employeeId, IList<(int dayOfMonth, string code)> codes)
        {
            if (!_employeeIdToColumn.TryGetValue(employeeId, out var colName))
                return;

            var map = codes.ToDictionary(x => x.dayOfMonth, x => x.code ?? string.Empty);

            foreach (DataRow r in _groupTable.Rows)
            {
                var day = Convert.ToInt32(r[DayCol]);
                r[colName] = map.TryGetValue(day, out var v) ? v : string.Empty;
            }
        }

        private static void ApplyAvailabilityGridLook(Guna2DataGridView grid)
        {
            // як у ContainerView.ApplyAvailabilityGridLook
            grid.BackgroundColor = Color.LightGray;
            grid.GridColor = Color.Gainsboro;

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SystemColors.Control;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            grid.DefaultCellStyle.ForeColor = Color.Black;
            grid.DefaultCellStyle.SelectionBackColor = Color.Gray;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            // ✅ Центрування тексту всюди
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // IMPORTANT: дублюємо в ThemeStyle (Guna любить перебивати)
            grid.ThemeStyle.BackColor = Color.LightGray;
            grid.ThemeStyle.GridColor = Color.Gainsboro;

            grid.ThemeStyle.HeaderStyle.BackColor = Color.White;
            grid.ThemeStyle.HeaderStyle.ForeColor = Color.Black;
            grid.ThemeStyle.HeaderStyle.Height = 36;

            grid.ThemeStyle.RowsStyle.SelectionBackColor = Color.Gray;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
        }

        private void MatrixGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView grid) return;
            if (e.ColumnIndex < 0) return;

            // малюємо все, але без стандартних бордерів
            var parts = e.PaintParts & ~DataGridViewPaintParts.Border;
            e.Paint(e.CellBounds, parts);

            // вертикальні лінії між колонками
            if (e.ColumnIndex < grid.ColumnCount - 1)
            {
                int x = e.CellBounds.Right - 1;
                e.Graphics.DrawLine(_matrixVPen, x, e.CellBounds.Top, x, e.CellBounds.Bottom - 1);
            }

            // м’які горизонтальні (не малюємо “останню” — щоб не було зовнішньої рамки)
            bool isLastRow = (e.RowIndex >= 0 && e.RowIndex == grid.Rows.Count - 1);

            if (!isLastRow)
            {
                int y = e.CellBounds.Bottom - 1;
                e.Graphics.DrawLine(_matrixHPen, e.CellBounds.Left, y, e.CellBounds.Right - 1, y);
            }

            e.Handled = true;
        }

        private static void ApplyMatrixHeadersFromDataTable(DataGridView grid, DataTable? dt)
        {
            // Employee колонки: берем Caption (ти його ставиш у TryAddEmployeeColumn)
            if (dt is null) return;

            foreach (DataGridViewColumn c in grid.Columns)
            {
                if (!dt.Columns.Contains(c.Name)) continue;
                var cap = dt.Columns[c.Name].Caption;
                if (!string.IsNullOrWhiteSpace(cap))
                    c.HeaderText = cap;
            }
        }
    }
}