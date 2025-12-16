using DataAccessLayer.Models;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView : Form, IAvailabilityView
    {
        private BindingSource bindsBindingSource = new();
        private readonly HashSet<int> dirtyBindRows = new();
        private readonly BindingSource availabilityDaysBindingSource = new();
        private bool isEdit;
        private bool isSuccessful;
        private string message;

        public AvailabilityView()
        {
            InitializeComponent();
            AssociateAndRaiseViewEvents();
            ConfigureGrid();
            ConfigureAvailabilityDaysGrid();
            WireNewControls();
            RegenerateDays();
            ConfigureBindsGrid();
            WireBindsToAvailabilityDays();

        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int EmployeeId
        {
            get => comboboxEmployee.SelectedValue is int id ? id : 0;
            set
            {
                comboboxEmployee.SelectedValue = value;
                if (value >= numberEmployeeId.Minimum && value <= numberEmployeeId.Maximum)
                    numberEmployeeId.Value = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int AvailabilityMonthId
        {
            get => (int)numberAvailabilityMonthId.Value;
            set => numberAvailabilityMonthId.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string AvailabilityMonthName
        {
            get => inputAvailabilityMonthName.Text;
            set => inputAvailabilityMonthName.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Year
        {
            get => timepickerYear.Value.Year;
            set
            {
                var dt = timepickerYear.Value;
                timepickerYear.Value = new DateTime(value, dt.Month, 1);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Month
        {
            get => timepickerMonth.Value.Month;
            set
            {
                var dt = timepickerMonth.Value;
                timepickerMonth.Value = new DateTime(dt.Year, value, 1);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public AvailabilityViewModel Mode { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public AvailabilityViewModel CancelTarget { get; set; } = AvailabilityViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsEdit
        {
            get => isEdit;
            set => isEdit = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsSuccessful
        {
            get => isSuccessful;
            set => isSuccessful = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Message
        {
            get => message;
            set => message = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SearchValue { get => inputSearch.Text; set => inputSearch.Text = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<AvailabilityDayRow> AvailabilityDays
        {
            get => availabilityDaysBindingSource.List
                .Cast<AvailabilityDayRow>()
                .ToList();

            set => availabilityDaysBindingSource.DataSource =
                value ?? new List<AvailabilityDayRow>();
        }

        public event Func<CancellationToken, Task>? SearchEvent;
        public event Func<CancellationToken, Task>? AddEvent;
        public event Func<CancellationToken, Task>? EditEvent;
        public event Func<CancellationToken, Task>? DeleteEvent;
        public event Func<CancellationToken, Task>? SaveEvent;
        public event Func<CancellationToken, Task>? CancelEvent;
        public event Func<CancellationToken, Task>? OpenProfileEvent;
        public event Func<CancellationToken, Task>? AddBindEvent;
        public event Func<BindModel, CancellationToken, Task>? UpsertBindEvent;
        public event Func<BindModel, CancellationToken, Task>? DeleteBindEvent;

        public void SetListBindingSource(BindingSource availabilityList)
        {
            dataGrid.AutoGenerateColumns = false;
            dataGrid.DataSource = availabilityList;
        }

        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEditAdnCreate;
            Mode = AvailabilityViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = AvailabilityViewModel.List;
        }

        public void ClearInputs()
        {
            EmployeeId = 0;
            AvailabilityMonthId = 0;
            AvailabilityMonthName = "";
            EmployeeId = 0;
            Month = DateTime.Today.Month;
            Year = DateTime.Today.Year;
        }

        public void ShowInfo(string text)
        {
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Information;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public void ShowError(string text)
        {
            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Error;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.OK;
            MessageDialog.Text = text;
            MessageDialog.Show();
        }

        public bool Confirm(string text, string? caption = null)
        {
            if (!string.IsNullOrWhiteSpace(caption))
                MessageDialog.Caption = caption;

            MessageDialog.Icon = Guna.UI2.WinForms.MessageDialogIcon.Question;
            MessageDialog.Buttons = Guna.UI2.WinForms.MessageDialogButtons.YesNo;
            MessageDialog.Text = text;

            var result = MessageDialog.Show(); // DialogResult.Yes / No
            return result == DialogResult.Yes;
        }

        private void AssociateAndRaiseViewEvents()
        {
            btnSearch.Click += async (_, __) => { if (SearchEvent != null) await SearchEvent(CancellationToken.None); };
            btnAdd.Click += async (_, __) => { if (AddEvent != null) await AddEvent(CancellationToken.None); };
            btnEdit.Click += async (_, __) => { if (EditEvent != null) await EditEvent(CancellationToken.None); };
            btnDelete.Click += async (_, __) => { if (DeleteEvent != null) await DeleteEvent(CancellationToken.None); };
            btnSave.Click += async (_, __) => { if (SaveEvent != null) await SaveEvent(CancellationToken.None); };
            btnCancel.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCancelProfile.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            inputSearch.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && SearchEvent != null)
                    await SearchEvent(CancellationToken.None);
            };

            dataGrid.CellDoubleClick += async (_, __) =>
            {
                if (OpenProfileEvent is not null)
                    await OpenProfileEvent(CancellationToken.None);
            };

            btnBackToAvailabilityList.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCacnelAvailabilityEdit2.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCacnelAvailabilityEdit2.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnAddNewBind.Click += async (_, __) =>
            {
                if (AddBindEvent != null)
                    await AddBindEvent(CancellationToken.None);

                // фокус на новий рядок
                if (dataGridBinds.Rows.Count > 0)
                {
                    dataGridBinds.CurrentCell = dataGridBinds.Rows[^1].Cells["colBindValue"];
                    dataGridBinds.BeginEdit(true);
                }
            };

            btnDeleteBind.Click += async (_, __) =>
            {
                var bind = dataGridBinds.CurrentRow?.DataBoundItem as BindModel;
                if (bind is null) return;

                if (!Confirm($"Delete bind '{bind.Key}'?", "Confirm"))
                    return;

                if (DeleteBindEvent != null)
                    await DeleteBindEvent(bind, CancellationToken.None);
            };

            btnBackToAvailabilityListFromProfile.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

            btnCancelProfile2.Click += async (_, __) =>
            {
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };

        }

        private void ConfigureGrid()
        {
            ConfigureMonthGrid(dataGrid);                // вкладка List – список місяців
            ConfigureProfileAvailabilityDaysGrid();      // вкладка Profile – дні місяця
        }

        private void ConfigureMonthGrid(DataGridView grid)
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

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAvaialbilityMonthName",
                HeaderText = "Availability Name",
                DataPropertyName = nameof(AvailabilityMonthModel.Name),
                FillWeight = 50
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAvailabilityEmployeeId",
                HeaderText = "Employee FullName",
                DataPropertyName = nameof(AvailabilityMonthModel.EmployeeFullName),
                FillWeight = 50
            });

            grid.RowTemplate.DividerHeight = 6;
            grid.RowTemplate.Height = 36;

            grid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            grid.ColumnHeadersHeight = 36;
        }

        private void ConfigureProfileAvailabilityDaysGrid()
        {
            dataGridAvailabilityMonthProfile.AutoGenerateColumns = false;
            dataGridAvailabilityMonthProfile.Columns.Clear();

            dataGridAvailabilityMonthProfile.ReadOnly = true;
            dataGridAvailabilityMonthProfile.RowHeadersVisible = false;
            dataGridAvailabilityMonthProfile.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridAvailabilityMonthProfile.MultiSelect = false;
            dataGridAvailabilityMonthProfile.AllowUserToAddRows = false;
            dataGridAvailabilityMonthProfile.AllowUserToDeleteRows = false;

            // Загалом Fill, але перша колонка буде fixed
            dataGridAvailabilityMonthProfile.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // --- Фіксована висота хедера ---
            dataGridAvailabilityMonthProfile.ColumnHeadersHeight = 36;
            dataGridAvailabilityMonthProfile.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // --- (Опційно) фіксована висота рядків ---
            dataGridAvailabilityMonthProfile.RowTemplate.Height = 36;
            dataGridAvailabilityMonthProfile.AllowUserToResizeRows = false;

            // --- Чорний текст всюди ---
            dataGridAvailabilityMonthProfile.DefaultCellStyle.ForeColor = Color.Black;

            dataGridAvailabilityMonthProfile.EnableHeadersVisualStyles = false; // важливо!
            dataGridAvailabilityMonthProfile.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;

            dataGridAvailabilityMonthProfile.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridAvailabilityMonthProfile.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;


            // --- Колонки ---
            var colDay = new DataGridViewTextBoxColumn
            {
                Name = "colDay",
                HeaderText = "Day",
                DataPropertyName = nameof(AvailabilityDayRow.DayOfMonth),
                ReadOnly = true,

                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 30,                 // <- фіксована ширина
                MinimumWidth = 30,
                Resizable = DataGridViewTriState.False
            };

            var colValue = new DataGridViewTextBoxColumn
            {
                Name = "colValue",
                HeaderText = "Availability (+ / - / HH:mm - HH:mm)",
                DataPropertyName = nameof(AvailabilityDayRow.Value),

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridAvailabilityMonthProfile.Columns.Add(colDay);
            dataGridAvailabilityMonthProfile.Columns.Add(colValue);
            dataGridAvailabilityMonthProfile.CellPainting -= DataGridAvailabilityDays_CellPainting;
            dataGridAvailabilityMonthProfile.CellPainting += DataGridAvailabilityDays_CellPainting;
            dataGridAvailabilityMonthProfile.DataSource = availabilityDaysBindingSource;
        }



        private void WireNewControls()
        {
            comboboxEmployee.SelectedIndexChanged += (_, __) =>
            {
                if (comboboxEmployee.SelectedValue is int id &&
                    id >= numberEmployeeId.Minimum &&
                    id <= numberEmployeeId.Maximum)
                {
                    numberEmployeeId.Value = id;
                }
            };

            timepickerMonth.ValueChanged += (_, __) =>
            {
                Month = timepickerMonth.Value.Month;
                RegenerateDays();
            };

            timepickerYear.ValueChanged += (_, __) =>
            {
                Year = timepickerYear.Value.Year;
                RegenerateDays();
            };
        }

        public void SetEmployeeList(IEnumerable<EmployeeModel> employees)
        {
            var list = employees
                .Select(e => new
                {
                    e.Id,
                    FullName = $"{e.FirstName} {e.LastName}"
                })
                .ToList();

            comboboxEmployee.DisplayMember = "FullName";
            comboboxEmployee.ValueMember = "Id";
            comboboxEmployee.DataSource = list;
        }

        public void ClearValidationErrors()
        {
            errorProvider.Clear();
        }

        private void WireBindsToAvailabilityDays()
        {
            dataGridAvailabilityDays.EditingControlShowing += (_, e) =>
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyDown -= AvailabilityDaysTextBox_KeyDown;
                    tb.KeyDown += AvailabilityDaysTextBox_KeyDown;
                }
            };

            dataGridAvailabilityDays.KeyDown += (_, e) =>
            {
                // коли не в режимі редагування
                if (!dataGridAvailabilityDays.IsCurrentCellInEditMode)
                    TryApplyBindToDays(e.KeyData, e);
            };
        }

        private void AvailabilityDaysTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            TryApplyBindToDays(e.KeyData, e);
        }

        private void TryApplyBindToDays(Keys keyData, KeyEventArgs e)
        {
            if (dataGridAvailabilityDays.CurrentCell?.OwningColumn?.Name != "colValue")
                return;

            if (keyData is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
                return;

            var keyText = new KeysConverter().ConvertToString(keyData);
            if (string.IsNullOrWhiteSpace(keyText)) return;

            var bind = bindsBindingSource.List
                .Cast<BindModel>()
                .Where(b => b.IsActive)
                .FirstOrDefault(b => string.Equals(NormalizeKey(b.Key), NormalizeKey(keyText), StringComparison.OrdinalIgnoreCase));

            if (bind is null) return;

            // вставляємо значення
            if (dataGridAvailabilityDays.EditingControl is TextBox tb)
                tb.Text = bind.Value;
            else
                dataGridAvailabilityDays.CurrentCell.Value = bind.Value;

            dataGridAvailabilityDays.CommitEdit(DataGridViewDataErrorContexts.Commit);
            dataGridAvailabilityDays.EndEdit();
            availabilityDaysBindingSource.EndEdit();

            // перейти на наступний день (та сама колонка)
            var row = dataGridAvailabilityDays.CurrentCell.RowIndex;
            var nextRow = row + 1;

            if (nextRow < dataGridAvailabilityDays.Rows.Count)
            {
                dataGridAvailabilityDays.CurrentCell = dataGridAvailabilityDays.Rows[nextRow].Cells["colValue"];
                dataGridAvailabilityDays.BeginEdit(true);
            }

            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        private static string NormalizeKey(string raw)
        {
            raw = (raw ?? "").Trim();
            try
            {
                var conv = new KeysConverter();
                var keys = (Keys)conv.ConvertFromString(raw)!;
                return conv.ConvertToString(keys) ?? raw;
            }
            catch
            {
                return raw;
            }
        }


        public void SetValidationErrors(IDictionary<string, string> errors)
        {
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(AvailabilityMonthModel.EmployeeId):
                        errorProvider.SetError(comboboxEmployee, kv.Value);
                        break;

                    case nameof(AvailabilityMonthModel.Name):
                        errorProvider.SetError(inputAvailabilityMonthName, kv.Value);
                        break;

                    case nameof(AvailabilityMonthModel.Month):
                        errorProvider.SetError(timepickerMonth, kv.Value);
                        break;

                    case nameof(AvailabilityMonthModel.Year):
                        errorProvider.SetError(timepickerYear, kv.Value);
                        break;
                }
            }
        }

        public void SetProfile(AvailabilityMonthModel m)
        {
            labelAvailabilityMonthName.Text = m.Name;
            labelEmployeeFullName.Text = m.EmployeeFullName;
            labelId.Text = m.Id.ToString();
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = AvailabilityViewModel.Profile;
        }

        private void ConfigureAvailabilityDaysGrid()
        {
            dataGridAvailabilityDays.AutoGenerateColumns = false;
            dataGridAvailabilityDays.Columns.Clear();

            dataGridAvailabilityDays.ReadOnly = false;
            dataGridAvailabilityDays.RowHeadersVisible = false;
            dataGridAvailabilityDays.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridAvailabilityDays.MultiSelect = false;
            dataGridAvailabilityDays.AllowUserToAddRows = false;
            dataGridAvailabilityDays.AllowUserToDeleteRows = false;

            // Загалом Fill, але перша колонка буде fixed
            dataGridAvailabilityDays.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // --- Фіксована висота хедера ---
            dataGridAvailabilityDays.ColumnHeadersHeight = 36;
            dataGridAvailabilityDays.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // --- (Опційно) фіксована висота рядків ---
            dataGridAvailabilityDays.RowTemplate.Height = 36;
            dataGridAvailabilityDays.AllowUserToResizeRows = false;

            // --- Чорний текст всюди ---
            dataGridAvailabilityDays.DefaultCellStyle.ForeColor = Color.Black;

            dataGridAvailabilityDays.EnableHeadersVisualStyles = false; // важливо!
            dataGridAvailabilityDays.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;

            dataGridAvailabilityDays.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridAvailabilityDays.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;


            // --- Колонки ---
            var colDay = new DataGridViewTextBoxColumn
            {
                Name = "colDay",
                HeaderText = "Day",
                DataPropertyName = nameof(AvailabilityDayRow.DayOfMonth),
                ReadOnly = true,

                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 30,                 // <- фіксована ширина
                MinimumWidth = 30,
                Resizable = DataGridViewTriState.False
            };

            var colValue = new DataGridViewTextBoxColumn
            {
                Name = "colValue",
                HeaderText = "Availability (+ / - / HH:mm - HH:mm)",
                DataPropertyName = nameof(AvailabilityDayRow.Value),

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridAvailabilityDays.Columns.Add(colDay);
            dataGridAvailabilityDays.Columns.Add(colValue);
            dataGridAvailabilityDays.CellPainting -= DataGridAvailabilityDays_CellPainting;
            dataGridAvailabilityDays.CellPainting += DataGridAvailabilityDays_CellPainting;
            dataGridAvailabilityDays.DataSource = availabilityDaysBindingSource;
        }

        private void DataGridAvailabilityDays_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView grid) return;
            if (e.ColumnIndex < 0) return; // не дані

            // Малюємо все стандартно (включно з текстом/фоном/горизонтальними лініями)
            e.Paint(e.CellBounds, DataGridViewPaintParts.All);

            // Для хедера теж можна (RowIndex == -1), тому НЕ фільтруємо по RowIndex
            int colDayIdx = grid.Columns["colDay"].Index;
            int colValueIdx = grid.Columns["colValue"].Index;

            using var pen = new Pen(grid.GridColor, 1);

            // Day -> лінія справа
            if (e.ColumnIndex == colDayIdx)
            {
                int x = e.CellBounds.Right - 1;
                e.Graphics.DrawLine(pen, x, e.CellBounds.Top, x, e.CellBounds.Bottom - 1);
            }
            // Value -> лінія зліва
            else if (e.ColumnIndex == colValueIdx)
            {
                int x = e.CellBounds.Left;
                e.Graphics.DrawLine(pen, x, e.CellBounds.Top, x, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }


        private void RegenerateDays()
        {
            var year = Year;
            var month = Month;

            if (year <= 0 || month <= 0)
                return;

            int daysInMonth = DateTime.DaysInMonth(year, month);

            // Зберігаємо те, що вже ввів користувач, щоб не губити
            var old = availabilityDaysBindingSource.List
                .Cast<AvailabilityDayRow>()
                .ToDictionary(r => r.DayOfMonth, r => r.Value);

            var rows = new List<AvailabilityDayRow>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                old.TryGetValue(day, out var val);

                rows.Add(new AvailabilityDayRow
                {
                    DayOfMonth = day,
                    Value = val ?? string.Empty
                });
            }

            availabilityDaysBindingSource.DataSource = rows;
        }

        public void SetBindsBindingSource(BindingSource binds)
        {
            bindsBindingSource = binds;
            dataGridBinds.AutoGenerateColumns = false;
            dataGridBinds.DataSource = bindsBindingSource;
        }

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
                Name = "colBindValue",
                HeaderText = "Value",
                DataPropertyName = nameof(BindModel.Value),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 60
            };

            // KEY (fixed)
            var colKey = new DataGridViewTextBoxColumn
            {
                Name = "colBindKey",
                HeaderText = "Key",
                DataPropertyName = nameof(BindModel.Key),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 140
            };

            // IS ACTIVE (fixed)
            var colActive = new DataGridViewCheckBoxColumn
            {
                Name = "colBindIsActive",
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

            // dirty tracking
            dataGridBinds.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (!dataGridBinds.IsCurrentCellDirty) return;

                // Комітимо тільки чекбокс
                if (dataGridBinds.CurrentCell?.OwningColumn?.Name == "colBindIsActive")
                    dataGridBinds.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            dataGridBinds.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0) dirtyBindRows.Add(e.RowIndex);
            };

            // автозбереження при виході з рядка
            dataGridBinds.RowValidated += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (!dirtyBindRows.Remove(e.RowIndex)) return;

                var model = dataGridBinds.Rows[e.RowIndex].DataBoundItem as BindModel;
                if (model is null) return;

                if (UpsertBindEvent != null)
                    await UpsertBindEvent(model, CancellationToken.None);
            };

            // запис хоткею натисканням в колонці Key
            dataGridBinds.EditingControlShowing += (_, e) =>
            {
                if (e.Control is not TextBox tb) return;

                // DataGridView reuse-ить один і той самий TextBox для різних колонок,
                // тому відписуємось ЗАВЖДИ.
                tb.KeyDown -= BindKeyTextBox_KeyDown;
                tb.ReadOnly = false;

                // Підписка лише для колонки Key
                if (dataGridBinds.CurrentCell?.OwningColumn?.Name == "colBindKey")
                {
                    tb.ReadOnly = true; // опціонально, щоб руками не вводили текст
                    tb.KeyDown += BindKeyTextBox_KeyDown;
                }
            };

        }

        private void BindKeyTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // ігноруємо "голі" модифікатори
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu) return;

            var text = new KeysConverter().ConvertToString(e.KeyData);
            if (string.IsNullOrWhiteSpace(text)) return;

            if (sender is TextBox tb)
                tb.Text = text;

            e.SuppressKeyPress = true;
            e.Handled = true;
        }


        private void dataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
