using DataAccessLayer.Models;
using Guna.UI2.WinForms;
using System.ComponentModel;
using System.Data;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView : Form, IAvailabilityView
    {
        public AvailabilityView()
        {
            InitializeComponent();
            AssociateAndRaiseViewEvents();
            ConfigureGrid();
            ConfigureAvailabilityGroupGrid();
            WireNewControls();
            RegenerateGroupDays();
            ConfigureBindsGrid();
            WireBindsToAvailabilityDays();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int EmployeeId
        {
            get => int.TryParse(lblEmployeeId.Text, out var id) ? id : 0;
            set => lblEmployeeId.Text = value > 0 ? value.ToString() : string.Empty;

        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int AvailabilityMonthId
        {
            get => (int)numberAvailabilityMonthId.Value;
            set => SetNumericValue(numberAvailabilityMonthId, value);
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
            get => (int)NumbAvailabilityYear.Value;
            set => SetNumericValue(NumbAvailabilityYear, value);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Month
        {
            get => (int)NumbAvailabilityMonth.Value;
            set => SetNumericValue(NumbAvailabilityMonth, value);
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
        public event Func<CancellationToken, Task>? AddBindEvent;
        public event Func<BindModel, CancellationToken, Task>? UpsertBindEvent;
        public event Func<BindModel, CancellationToken, Task>? DeleteBindEvent;
        public event Func<CancellationToken, Task>? AddEmployeeToGroupEvent;
        public event Func<CancellationToken, Task>? RemoveEmployeeFromGroupEvent; 

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
            Month = DateTime.Today.Month;
            Year = DateTime.Today.Year;
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

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    // якщо ти валідуш AvailabilityGroupModel у презентері — ключі будуть ці:
                    case nameof(AvailabilityGroupModel.Name):
                        errorProvider.SetError(inputAvailabilityMonthName, kv.Value);
                        break;

                    case nameof(AvailabilityGroupModel.Month):
                        errorProvider.SetError(NumbAvailabilityMonth, kv.Value);
                        break;

                    case nameof(AvailabilityGroupModel.Year):
                        errorProvider.SetError(NumbAvailabilityYear, kv.Value);
                        break;


                        // опціонально: якщо десь ще залишиться валідатор з EmployeeId
                        // case nameof(AvailabilityMonthModel.EmployeeId):
                        //     errorProvider.SetError(comboboxEmployee, kv.Value);
                        //     break;
                }
            }
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = AvailabilityViewModel.Profile;
        }

        private static void SetNumericValue(Guna.UI2.WinForms.Guna2NumericUpDown nud, int value)
        {
            var min = (int)nud.Minimum;
            var max = (int)nud.Maximum;
            if (value < min) value = min;
            if (value > max) value = max;
            nud.Value = value;
        }
    }
}
