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
        private bool isEdit;
        private bool isSuccessful;
        private string message;

        public AvailabilityView()
        {
            InitializeComponent();
            AssociateAndRaiseViewEvents();
            ConfigureGrid();
            WireNewControls();
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
            dataGrid.AutoGenerateColumns = false;
            dataGrid.Columns.Clear();

            dataGrid.ReadOnly = true;
            dataGrid.RowHeadersVisible = false;
            dataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGrid.MultiSelect = false;
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAvaialbilityMonthName",
                HeaderText = "Availability Name",
                DataPropertyName = nameof(DataAccessLayer.Models.AvailabilityMonthModel.Name),
                FillWeight = 50
            });

            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAvailabilityEmployeeId",
                HeaderText = "Employee FullName",
                DataPropertyName = nameof(AvailabilityMonthModel.EmployeeFullName),
                FillWeight = 50
            });

            dataGrid.RowTemplate.DividerHeight = 6;
            dataGrid.RowTemplate.Height = 36;
            dataGrid.ThemeStyle.RowsStyle.Height = 36;

            dataGrid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dataGrid.ColumnHeadersHeight = 36;
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
            };

            timepickerYear.ValueChanged += (_, __) =>
            {
                Year = timepickerYear.Value.Year;
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
    }
}
