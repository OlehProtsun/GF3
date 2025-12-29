using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private void ConfigureBindsGrid()
        {
            dataGridBinds.AutoGenerateColumns = false;
            dataGridBinds.Columns.Clear();

            dataGridBinds.ReadOnly = false;
            dataGridBinds.RowHeadersVisible = false;
            dataGridBinds.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridBinds.MultiSelect = false;
            dataGridBinds.AllowUserToAddRows = false;
            dataGridBinds.AllowUserToDeleteRows = false;

            dataGridBinds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // VALUE
            var colValue = new DataGridViewTextBoxColumn
            {
                Name = ColBindValue,
                HeaderText = "Value",
                DataPropertyName = nameof(BindModel.Value),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 60
            };

            // KEY (fixed)
            var colKey = new DataGridViewTextBoxColumn
            {
                Name = ColBindKey,
                HeaderText = "Key",
                DataPropertyName = nameof(BindModel.Key),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 140
            };

            // IS ACTIVE (fixed)
            var colActive = new DataGridViewCheckBoxColumn
            {
                Name = ColBindIsActive,
                HeaderText = "Active",
                DataPropertyName = nameof(BindModel.IsActive),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 70
            };

            dataGridBinds.Columns.AddRange(colValue, colKey, colActive);

            dataGridBinds.RowTemplate.Height = 34;
            dataGridBinds.ColumnHeadersHeight = 34;
            dataGridBinds.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridBinds.AllowUserToResizeRows = false;

            dataGridBinds.EnableHeadersVisualStyles = false;
            dataGridBinds.DefaultCellStyle.ForeColor = Color.Black;
            dataGridBinds.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dataGridBinds.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridBinds.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dataGridBinds.CurrentCellDirtyStateChanged -= DataGridBinds_CurrentCellDirtyStateChanged;
            dataGridBinds.CellValueChanged -= DataGridBinds_CellValueChanged;
            dataGridBinds.RowValidated -= DataGridBinds_RowValidated;
            dataGridBinds.EditingControlShowing -= DataGridBinds_EditingControlShowing;

            dataGridBinds.CurrentCellDirtyStateChanged += DataGridBinds_CurrentCellDirtyStateChanged;
            dataGridBinds.CellValueChanged += DataGridBinds_CellValueChanged;
            dataGridBinds.RowValidated += DataGridBinds_RowValidated;
            dataGridBinds.EditingControlShowing += DataGridBinds_EditingControlShowing;

        }

        public void SetBindsBindingSource(BindingSource binds)
        {
            bindsBindingSource.ListChanged -= BindsBindingSource_ListChanged;

            bindsBindingSource = binds ?? new BindingSource();

            bindsBindingSource.ListChanged += BindsBindingSource_ListChanged;

            dataGridBinds.AutoGenerateColumns = false;
            dataGridBinds.DataSource = bindsBindingSource;

            RebuildActiveBindsCache();
        }

        private void BindsBindingSource_ListChanged(object? sender, ListChangedEventArgs e)
        {
            // простий варіант: перебудовуємо кеш після будь-якої зміни
            RebuildActiveBindsCache();
        }

        private void RebuildActiveBindsCache()
        {
            _activeBinds.Clear();

            foreach (var b in bindsBindingSource.List.OfType<BindModel>())
            {
                if (!b.IsActive) continue;

                var key = NormalizeKey(b.Key);
                if (string.IsNullOrWhiteSpace(key)) continue;

                _activeBinds[key] = b.Value ?? string.Empty;
            }
        }

        private void WireBindsToAvailabilityDays()
        {
            dataGridAvailabilityDays.EditingControlShowing -= DataGridAvailabilityDays_EditingControlShowing;
            dataGridAvailabilityDays.EditingControlShowing += DataGridAvailabilityDays_EditingControlShowing;

            dataGridAvailabilityDays.KeyDown -= DataGridAvailabilityDays_KeyDown;
            dataGridAvailabilityDays.KeyDown += DataGridAvailabilityDays_KeyDown;
        }

        private void DataGridAvailabilityDays_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                tb.KeyDown -= AvailabilityDaysTextBox_KeyDown;
                tb.KeyDown += AvailabilityDaysTextBox_KeyDown;
            }
        }

        private void DataGridAvailabilityDays_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!dataGridAvailabilityDays.IsCurrentCellInEditMode)
                TryApplyBindToDays(e.KeyData, e);
        }

        private void AvailabilityDaysTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            TryApplyBindToDays(e.KeyData, e);
        }

        private void TryApplyBindToDays(Keys keyData, KeyEventArgs e)
        {
            var cell = dataGridAvailabilityDays.CurrentCell;
            if (cell is null) return;

            var colName = cell.OwningColumn?.Name;
            if (string.IsNullOrWhiteSpace(colName)) return;

            // не дозволяємо вставляти в Day
            if (colName == DayCol)
                return;

            if (keyData is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
                return;

            var keyText = KeysConv.ConvertToString(keyData);
            if (string.IsNullOrWhiteSpace(keyText)) return;

            var normalized = NormalizeKey(keyText);
            if (!_activeBinds.TryGetValue(normalized, out var value))
                return;

            // вставляємо значення
            if (dataGridAvailabilityDays.EditingControl is TextBox tb)
                tb.Text = value;
            else
                cell.Value = value;


            dataGridAvailabilityDays.CommitEdit(DataGridViewDataErrorContexts.Commit);
            dataGridAvailabilityDays.EndEdit();

            // перейти на наступний день у тій самій колонці
            var row = cell.RowIndex;
            var nextRow = row + 1;

            if (nextRow < dataGridAvailabilityDays.Rows.Count)
            {
                dataGridAvailabilityDays.CurrentCell = dataGridAvailabilityDays.Rows[nextRow].Cells[colName];
                dataGridAvailabilityDays.BeginEdit(true);
            }

            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        private static string NormalizeKey(string raw)
        {
            raw = (raw ?? string.Empty).Trim();
            if (raw.Length == 0) return raw;

            try
            {
                // ConvertFromString повертає object; зазвичай це Keys
                var obj = KeysConv.ConvertFromString(raw);
                if (obj is Keys keys)
                    return KeysConv.ConvertToString(keys) ?? raw;

                return raw;
            }
            catch
            {
                return raw;
            }
        }

        private void DataGridBinds_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (!dataGridBinds.IsCurrentCellDirty) return;
            if (dataGridBinds.CurrentCell?.OwningColumn?.Name == ColBindIsActive)
                dataGridBinds.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DataGridBinds_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) dirtyBindRows.Add(e.RowIndex);
        }

        private async void DataGridBinds_RowValidated(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dirtyBindRows.Remove(e.RowIndex)) return;

            var model = dataGridBinds.Rows[e.RowIndex].DataBoundItem as BindModel;
            if (model is null) return;

            await RaiseSafeAsync(UpsertBindEvent, model);
        }

        private void DataGridBinds_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is not TextBox tb) return;

            tb.KeyDown -= BindKeyTextBox_KeyDown;
            tb.ReadOnly = false;

            if (dataGridBinds.CurrentCell?.OwningColumn?.Name == ColBindKey)
            {
                tb.ReadOnly = true;
                tb.KeyDown += BindKeyTextBox_KeyDown;
            }
        }

        private void BindKeyTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // ігноруємо "голі" модифікатори
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu) return;

            var text = KeysConv.ConvertToString(e.KeyData);
            if (string.IsNullOrWhiteSpace(text)) return;

            if (sender is TextBox tb)
                tb.Text = text;

            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }
}
