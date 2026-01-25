using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private void InitializeScheduleStyleMenu()
        {
            _scheduleStyleMenu = new ContextMenuStrip();

            var setBackground = new ToolStripMenuItem("Set background color...");
            var setForeground = new ToolStripMenuItem("Set text color...");
            var clearStyle = new ToolStripMenuItem("Clear style");
            var clearAllStyles = new ToolStripMenuItem("Clear all styles");

            setBackground.Click += (_, __) => ApplyColorToSelection(isBackground: true);
            setForeground.Click += (_, __) => ApplyColorToSelection(isBackground: false);
            clearStyle.Click += (_, __) => ClearStylesForSelection();
            clearAllStyles.Click += (_, __) => ClearAllStyles();

            _scheduleStyleMenu.Items.AddRange(new ToolStripItem[]
            {
                setBackground,
                setForeground,
                new ToolStripSeparator(),
                clearStyle,
                clearAllStyles
            });

            slotGrid.ContextMenuStrip = _scheduleStyleMenu;
            slotGrid.MouseDown -= SlotGrid_MouseDown;
            slotGrid.MouseDown += SlotGrid_MouseDown;
        }

        private void SlotGrid_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (sender is not DataGridView grid) return;

            var hit = grid.HitTest(e.X, e.Y);
            if (hit.Type != DataGridViewHitTestType.Cell) return;

            if (hit.RowIndex < 0 || hit.ColumnIndex < 0) return;

            var cell = grid[hit.ColumnIndex, hit.RowIndex];
            if (!cell.Selected)
            {
                grid.ClearSelection();
                cell.Selected = true;
            }

            grid.CurrentCell = cell;
        }

        private void ApplyColorToSelection(bool isBackground)
        {
            var targets = GetStyleTargetCells();
            if (targets.Count == 0) return;

            using var dialog = new ColorDialog();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var hex = ColorHexConverter.ToHex(dialog.Color);
            foreach (var (day, empId) in targets)
            {
                if (isBackground)
                    UpsertCellStyle(day, empId, backgroundHex: hex, foregroundHex: null, replaceNulls: false);
                else
                    UpsertCellStyle(day, empId, backgroundHex: null, foregroundHex: hex, replaceNulls: false);
            }

            RefreshScheduleStyles();
        }

        private void ClearStylesForSelection()
        {
            var targets = GetStyleTargetCells();
            if (targets.Count == 0) return;

            foreach (var (day, empId) in targets)
                UpsertCellStyle(day, empId, backgroundHex: null, foregroundHex: null, replaceNulls: true);

            RefreshScheduleStyles();
        }

        private void ClearAllStyles()
        {
            _cellStyles.Clear();
            _styleLookup.Clear();
            RefreshScheduleStyles();
        }

        private void RefreshScheduleStyles()
        {
            RequestScheduleGridRefresh();
            RefreshScheduleProfileIfOpened();
        }

        private List<(int day, int empId)> GetStyleTargetCells()
        {
            var cells = slotGrid.SelectedCells.Cast<DataGridViewCell>().ToList();
            if (cells.Count == 0 && slotGrid.CurrentCell != null)
                cells.Add(slotGrid.CurrentCell);

            var targets = new List<(int day, int empId)>();
            foreach (var cell in cells)
            {
                if (TryGetCellStyleKey(cell, out var day, out var empId))
                    targets.Add((day, empId));
            }

            return targets.Distinct().ToList();
        }

        private bool TryGetCellStyleKey(DataGridViewCell cell, out int day, out int empId)
        {
            day = 0;
            empId = 0;

            var colName = cell.OwningColumn?.Name;
            if (string.IsNullOrWhiteSpace(colName) || colName == DayCol || colName == ConflictCol)
                return false;

            if (!TryGetColumnMap(slotGrid, out var map) || !map.TryGetValue(colName, out empId))
                return false;

            var rawDay = cell.OwningRow?.Cells[DayCol]?.Value?.ToString();
            if (!int.TryParse(rawDay, out day))
                return false;

            return true;
        }

        private void UpsertCellStyle(
            int day,
            int empId,
            string? backgroundHex,
            string? foregroundHex,
            bool replaceNulls)
        {
            var key = (day, empId);
            if (!_styleLookup.TryGetValue(key, out var style))
            {
                style = new ScheduleCellStyleModel
                {
                    DayOfMonth = day,
                    EmployeeId = empId,
                    ScheduleId = ScheduleId
                };

                _styleLookup[key] = style;
                _cellStyles.Add(style);
            }

            if (replaceNulls || backgroundHex != null)
                style.BackgroundHex = backgroundHex;

            if (replaceNulls || foregroundHex != null)
                style.ForegroundHex = foregroundHex;

            if (string.IsNullOrWhiteSpace(style.BackgroundHex) &&
                string.IsNullOrWhiteSpace(style.ForegroundHex))
            {
                _styleLookup.Remove(key);
                _cellStyles.Remove(style);
            }
        }

        private void RebuildCellStyleLookup()
        {
            _styleLookup.Clear();
            foreach (var style in _cellStyles)
            {
                if (string.IsNullOrWhiteSpace(style.BackgroundHex) &&
                    string.IsNullOrWhiteSpace(style.ForegroundHex))
                {
                    continue;
                }

                _styleLookup[(style.DayOfMonth, style.EmployeeId)] = style;
            }
        }

        private ScheduleCellStyleModel? GetOverrideStyle(int day, int empId)
            => _styleLookup.TryGetValue((day, empId), out var style) ? style : null;

        private void ScheduleGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView grid) return;
            ApplyScheduleCellStyle(grid, e.RowIndex, e.ColumnIndex, e.CellStyle);
        }

        private void ApplyScheduleCellStyle(
            DataGridView grid,
            int rowIndex,
            int columnIndex,
            DataGridViewCellStyle cellStyle)
        {
            if (rowIndex < 0 || columnIndex < 0) return;

            if (grid.Rows[rowIndex].DataBoundItem is not DataRowView rowView)
                return;

            if (rowView[DayCol] is not int day || day <= 0)
                return;

            if (!TryGetGridYearMonth(grid, out var year, out var month))
                return;

            var isWeekend = IsWeekendCached(year, month, day);
            var columnName = grid.Columns[columnIndex].Name;

            ScheduleCellStyleModel? overrideStyle = null;
            if (!string.IsNullOrWhiteSpace(columnName) && columnName != DayCol && columnName != ConflictCol)
            {
                if (TryGetColumnMap(grid, out var map) && map.TryGetValue(columnName, out var empId))
                    overrideStyle = GetOverrideStyle(day, empId);
            }

            cellStyle.BackColor = _styleResolver.ResolveBackground(overrideStyle, isWeekend);
            cellStyle.ForeColor = _styleResolver.ResolveForeground(overrideStyle);

#if DEBUG
            if (isWeekend && _weekendStyleLogCount < 3)
            {
                _weekendStyleLogCount++;
                Debug.WriteLine(
                    $"[ScheduleStyle] {year:D4}-{month:D2}-{day:D2} col={columnName} " +
                    $"bg={cellStyle.BackColor} fg={cellStyle.ForeColor} " +
                    $"overrideBg={overrideStyle?.BackgroundHex ?? "none"} overrideFg={overrideStyle?.ForegroundHex ?? "none"}");
            }
#endif
        }
    }
}
