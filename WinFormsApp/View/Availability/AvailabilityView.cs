using DataAccessLayer.Models;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView : Form, IAvailabilityView
    {
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
            dataGridAvailabilityMonthProfile.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridAvailabilityMonthProfile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDayProfile",
                HeaderText = "Day",
                DataPropertyName = nameof(AvailabilityDayRow.DayOfMonth),
                FillWeight = 30,
                ReadOnly = true
            });

            dataGridAvailabilityMonthProfile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colValueProfile",
                HeaderText = "Availability (+ / - / HH:mm - HH:mm)",
                DataPropertyName = nameof(AvailabilityDayRow.Value),
                FillWeight = 70,
                ReadOnly = true
            });

            // ВАЖЛИВО: той самий BindingSource, що й для Edit-гріда
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
            dataGridAvailabilityDays.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridAvailabilityDays.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDay",
                HeaderText = "Day",
                DataPropertyName = nameof(AvailabilityDayRow.DayOfMonth),
                ReadOnly = true,
                FillWeight = 30
            });

            dataGridAvailabilityDays.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colValue",
                HeaderText = "Availability (+ / - / HH:mm - HH:mm)",
                DataPropertyName = nameof(AvailabilityDayRow.Value),
                FillWeight = 70
            });

            dataGridAvailabilityDays.DataSource = availabilityDaysBindingSource;
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


    }
}
