using DataAccessLayer.Models;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private void ConfigureContainerGrid() 
        {
            ApplyListGridDefaults(containerGrid);

            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ContainerModel.Name),
                FillWeight = 50
            });

            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Note",
                DataPropertyName = nameof(ContainerModel.Note),
                FillWeight = 50
            });

        }
        private void ConfigureScheduleGrid() 
        {
            ApplyListGridDefaults(scheduleGrid);

            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ScheduleModel.Name),
                FillWeight = 50
            });

            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Year",
                DataPropertyName = nameof(ScheduleModel.Year),
                FillWeight = 50
            });

            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Month",
                DataPropertyName = nameof(ScheduleModel.Month),
                FillWeight = 50
            });

        }
        private void ConfigureSlotGrid() 
        {
            ConfigureMatrixGrid(slotGrid, readOnly: false);
            ConfigureMatrixGrid(scheduleSlotProfileGrid, readOnly: true);
        }

        private void ConfigureMatrixGrid(Guna2DataGridView grid, bool readOnly) 
        {

            grid.AutoGenerateColumns = true;
            grid.Columns.Clear();

            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;

            grid.ReadOnly = readOnly;
            grid.ThemeStyle.ReadOnly = readOnly; // важливо для Guna
            grid.EditMode = readOnly ? DataGridViewEditMode.EditProgrammatically : DataGridViewEditMode.EditOnKeystrokeOrF2;

            // кастомна сітка
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.GridColor = Color.Gainsboro;

            // щоб не плодити підписки на кожен виклик
            grid.CellPainting -= SlotGrid_CellPainting;
            grid.CellPainting += SlotGrid_CellPainting;

            grid.DataError -= SlotGrid_DataError;
            grid.DataError += SlotGrid_DataError;

            if (!readOnly)
            {
                grid.CellBeginEdit -= SlotGrid_CellBeginEdit;
                grid.CellEndEdit -= SlotGrid_CellEndEdit;

                grid.CellBeginEdit += SlotGrid_CellBeginEdit;
                grid.CellEndEdit += SlotGrid_CellEndEdit;
            }

            ApplyAvailabilityGridLook(grid);

        }

        private static void ApplyAvailabilityGridLook(Guna2DataGridView grid) 
        {
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

            // ✅ центрування один раз тут
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.ThemeStyle.BackColor = Color.LightGray;
            grid.ThemeStyle.GridColor = Color.Gainsboro;

            grid.ThemeStyle.HeaderStyle.BackColor = Color.White;
            grid.ThemeStyle.HeaderStyle.ForeColor = Color.Black;
            grid.ThemeStyle.HeaderStyle.Height = 36;

            grid.ThemeStyle.RowsStyle.SelectionBackColor = Color.Gray;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;

        }
        private static void ApplyListGridDefaults(DataGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.RowTemplate.DividerHeight = 6;
            grid.RowTemplate.Height = 36;

            // для Guna2DataGridView теж ок:
            if (grid is Guna.UI2.WinForms.Guna2DataGridView g)
                g.ThemeStyle.RowsStyle.Height = 36;

            grid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            grid.ColumnHeadersHeight = 36;

        }

        private void SlotGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e) 
        {
            // Не даємо гріду падати через DataError
            e.ThrowException = false;
        }
        private void SlotGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e) 
        {
            if (sender is not DataGridView grid) return;
            if (e.ColumnIndex < 0) return;

            var parts = e.PaintParts & ~DataGridViewPaintParts.Border;
            e.Paint(e.CellBounds, parts);

            // вертикальні внутрішні
            if (e.ColumnIndex < grid.ColumnCount - 1)
            {
                int x = e.CellBounds.Right - 1;
                e.Graphics.DrawLine(_gridVPen, x, e.CellBounds.Top, x, e.CellBounds.Bottom - 1);
            }

            // горизонтальні внутрішні (без зовнішньої рамки)
            bool isLastRow = (e.RowIndex >= 0 && e.RowIndex == grid.Rows.Count - 1);
            if (!isLastRow)
            {
                int y = e.CellBounds.Bottom - 1;
                e.Graphics.DrawLine(_gridHPen, e.CellBounds.Left, y, e.CellBounds.Right - 1, y);
            }

            // маленьке червоне коло для конфлікту в Day
            if (e.RowIndex >= 0 &&
                grid.Columns.Contains(DayCol) &&
                e.ColumnIndex == grid.Columns[DayCol].Index &&
                grid.Rows[e.RowIndex].DataBoundItem is DataRowView rowView &&
                rowView[ConflictCol] is bool hasConflict &&
                hasConflict)
            {
                const int diameter = 3;
                const int leftPadding = 6;

                var rect = new Rectangle(
                    e.CellBounds.Left + leftPadding,
                    e.CellBounds.Top + (e.CellBounds.Height - diameter) / 2,
                    diameter,
                    diameter);

                e.Graphics.DrawEllipse(_conflictPen, rect);
            }

            e.Handled = true;

        }

        private void ApplyDayAndConflictColumnsStyle(DataGridView grid) 
        {
            if (grid.Columns.Contains(ConflictCol))
                grid.Columns[ConflictCol].Visible = false;

            if (grid.Columns.Contains(DayCol))
            {
                var colDay = grid.Columns[DayCol];
                colDay.ReadOnly = true;

                colDay.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colDay.Width = 40;
                colDay.MinimumWidth = 40;
                colDay.Resizable = DataGridViewTriState.False;
                colDay.Frozen = true; // при горизонтальному скролі Day завжди зліва
            }

        }
    }
}
