using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView : Form, IContainerView
    {
        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;

        public ContainerView()
        {
            InitializeComponent();
            ConfigureContainerGrid();
            ConfigureScheduleGrid();
            ConfigureSlotGrid();
            AssociateAndRaiseEvents();
            comboStatus.DataSource = Enum.GetValues(typeof(ScheduleStatus));
        }

        #region Container properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ContainerViewModel Mode { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ContainerViewModel CancelTarget { get; set; } = ContainerViewModel.List;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ContainerId { get => (int)numberContainerId.Value; set => numberContainerId.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ContainerName { get => inputContainerName.Text; set => inputContainerName.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? ContainerNote { get => inputContainerNote.Text; set => inputContainerNote.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string SearchValue { get => inputSearch.Text; set => inputSearch.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public bool IsEdit { get => isEdit; set => isEdit = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public bool IsSuccessful { get => isSuccessful; set => isSuccessful = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string Message { get => message; set => message = value; }
        #endregion

        #region Schedule properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleViewModel ScheduleMode { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleViewModel ScheduleCancelTarget { get; set; } = ScheduleViewModel.List;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleId { get => (int)numberScheduleId.Value; set => numberScheduleId.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleContainerId { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleShopId { get => comboShop.SelectedValue is int v ? v : 0; set => comboShop.SelectedValue = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleName { get => inputScheduleName.Text; set => inputScheduleName.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleYear { get => (int)inputYear.Value; set => inputYear.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMonth { get => (int)inputMonth.Value; set => inputMonth.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int SchedulePeoplePerShift { get => (int)inputPeoplePerShift.Value; set => inputPeoplePerShift.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleShift1 { get => inputShift1.Text; set => inputShift1.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleShift2 { get => inputShift2.Text; set => inputShift2.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxHoursPerEmp { get => (int)inputMaxHours.Value; set => inputMaxHours.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxConsecutiveDays { get => (int)inputMaxConsecutiveDays.Value; set => inputMaxConsecutiveDays.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxConsecutiveFull { get => (int)inputMaxConsecutiveFull.Value; set => inputMaxConsecutiveFull.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxFullPerMonth { get => (int)inputMaxFull.Value; set => inputMaxFull.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? ScheduleComment { get => inputScheduleComment.Text; set => inputScheduleComment.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleStatus ScheduleStatus { get => (ScheduleStatus)comboStatus.SelectedItem!; set => comboStatus.SelectedItem = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleSearch { get => inputScheduleSearch.Text; set => inputScheduleSearch.Text = value; }
        public IList<int> SelectedAvailabilityIds => checkedAvailabilities.CheckedItems
            .OfType<AvailabilityMonthModel>()
            .Select(a => a.Id)
            .ToList();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public IList<ScheduleSlotModel> ScheduleSlots
        {
            get => (slotGrid.DataSource as BindingSource)?.List.Cast<ScheduleSlotModel>().ToList() ?? new List<ScheduleSlotModel>();
            set => slotGrid.DataSource = new BindingSource { DataSource = value };
        }
        #endregion

        #region Events
        public event Func<CancellationToken, Task>? SearchEvent;
        public event Func<CancellationToken, Task>? AddEvent;
        public event Func<CancellationToken, Task>? EditEvent;
        public event Func<CancellationToken, Task>? DeleteEvent;
        public event Func<CancellationToken, Task>? SaveEvent;
        public event Func<CancellationToken, Task>? CancelEvent;
        public event Func<CancellationToken, Task>? OpenProfileEvent;

        public event Func<CancellationToken, Task>? ScheduleSearchEvent;
        public event Func<CancellationToken, Task>? ScheduleAddEvent;
        public event Func<CancellationToken, Task>? ScheduleEditEvent;
        public event Func<CancellationToken, Task>? ScheduleDeleteEvent;
        public event Func<CancellationToken, Task>? ScheduleSaveEvent;
        public event Func<CancellationToken, Task>? ScheduleCancelEvent;
        public event Func<CancellationToken, Task>? ScheduleOpenProfileEvent;
        public event Func<CancellationToken, Task>? ScheduleGenerateEvent;
        #endregion

        private void AssociateAndRaiseEvents()
        {
            btnSearch.Click += async (_, __) => { if (SearchEvent != null) await SearchEvent(CancellationToken.None); };
            btnAdd.Click += async (_, __) => { if (AddEvent != null) await AddEvent(CancellationToken.None); };
            btnEdit.Click += async (_, __) => { if (EditEvent != null) await EditEvent(CancellationToken.None); };
            btnDelete.Click += async (_, __) => { if (DeleteEvent != null) await DeleteEvent(CancellationToken.None); };
            btnSave.Click += async (_, __) => { if (SaveEvent != null) await SaveEvent(CancellationToken.None); };
            btnCancel.Click += async (_, __) => { if (CancelEvent != null) await CancelEvent(CancellationToken.None); };
            containerGrid.CellDoubleClick += async (_, __) => { if (OpenProfileEvent != null) await OpenProfileEvent(CancellationToken.None); };
            inputSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && SearchEvent != null) await SearchEvent(CancellationToken.None); };

            btnScheduleSearch.Click += async (_, __) => { if (ScheduleSearchEvent != null) await ScheduleSearchEvent(CancellationToken.None); };
            btnScheduleAdd.Click += async (_, __) => { if (ScheduleAddEvent != null) await ScheduleAddEvent(CancellationToken.None); };
            btnScheduleEdit.Click += async (_, __) => { if (ScheduleEditEvent != null) await ScheduleEditEvent(CancellationToken.None); };
            btnScheduleDelete.Click += async (_, __) => { if (ScheduleDeleteEvent != null) await ScheduleDeleteEvent(CancellationToken.None); };
            btnScheduleSave.Click += async (_, __) => { if (ScheduleSaveEvent != null) await ScheduleSaveEvent(CancellationToken.None); };
            btnScheduleCancel.Click += async (_, __) => { if (ScheduleCancelEvent != null) await ScheduleCancelEvent(CancellationToken.None); };
            btnGenerate.Click += async (_, __) => { if (ScheduleGenerateEvent != null) await ScheduleGenerateEvent(CancellationToken.None); };
            btnOpenScheduleProfile.Click += async (_, __) => { if (ScheduleOpenProfileEvent != null) await ScheduleOpenProfileEvent(CancellationToken.None); };
            scheduleGrid.CellDoubleClick += async (_, __) => { if (ScheduleOpenProfileEvent != null) await ScheduleOpenProfileEvent(CancellationToken.None); };
        }

        public void SetContainerBindingSource(BindingSource containers)
        {
            containerGrid.AutoGenerateColumns = false;
            containerGrid.DataSource = containers;
        }

        public void SetScheduleBindingSource(BindingSource schedules)
        {
            scheduleGrid.AutoGenerateColumns = false;
            scheduleGrid.DataSource = schedules;
        }

        public void SetSlotBindingSource(BindingSource slots)
        {
            slotGrid.AutoGenerateColumns = false;
            slotGrid.DataSource = slots;
        }

        public void SetShopList(IEnumerable<ShopModel> shops)
        {
            comboShop.DataSource = shops.ToList();
            comboShop.DisplayMember = nameof(ShopModel.Name);
            comboShop.ValueMember = nameof(ShopModel.Id);
        }

        public void SetAvailabilityList(IEnumerable<AvailabilityMonthModel> availabilities)
        {
            checkedAvailabilities.Items.Clear();
            foreach (var a in availabilities)
                checkedAvailabilities.Items.Add(a, false);

            checkedAvailabilities.DisplayMember = nameof(AvailabilityMonthModel.Name);
        }

        private void ConfigureContainerGrid()
        {
            containerGrid.AutoGenerateColumns = false;
            containerGrid.Columns.Clear();
            containerGrid.ReadOnly = true;
            containerGrid.AllowUserToAddRows = false;
            containerGrid.AllowUserToDeleteRows = false;
            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ContainerModel.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Note",
                DataPropertyName = nameof(ContainerModel.Note),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void ConfigureScheduleGrid()
        {
            scheduleGrid.AutoGenerateColumns = false;
            scheduleGrid.Columns.Clear();
            scheduleGrid.ReadOnly = true;
            scheduleGrid.AllowUserToAddRows = false;
            scheduleGrid.AllowUserToDeleteRows = false;
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ScheduleModel.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Shop",
                DataPropertyName = nameof(ScheduleModel.ShopId),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Year",
                DataPropertyName = nameof(ScheduleModel.Year)
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Month",
                DataPropertyName = nameof(ScheduleModel.Month)
            });
        }

        private void ConfigureSlotGrid()
        {
            slotGrid.AutoGenerateColumns = false;
            slotGrid.Columns.Clear();
            slotGrid.AllowUserToAddRows = false;
            slotGrid.AllowUserToDeleteRows = false;
            slotGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Day", DataPropertyName = nameof(ScheduleSlotModel.DayOfMonth) });
            slotGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Shift", DataPropertyName = nameof(ScheduleSlotModel.ShiftNo) });
            slotGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Slot", DataPropertyName = nameof(ScheduleSlotModel.SlotNo) });
            slotGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Employee", DataPropertyName = nameof(ScheduleSlotModel.EmployeeId) });

            scheduleSlotProfileGrid.AutoGenerateColumns = false;
            scheduleSlotProfileGrid.Columns.Clear();
            scheduleSlotProfileGrid.Columns.AddRange(slotGrid.Columns.Cast<DataGridViewColumn>().Select(c => (DataGridViewColumn)c.Clone()).ToArray());
        }

        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEdit;
            Mode = ContainerViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = ContainerViewModel.List;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = ContainerViewModel.Profile;
        }

        public void SwitchToScheduleEditMode()
        {
            tabControl.SelectedTab = tabScheduleEdit;
            ScheduleMode = ScheduleViewModel.Edit;
        }

        public void SwitchToScheduleListMode()
        {
            tabControl.SelectedTab = tabProfile;
            ScheduleMode = ScheduleViewModel.List;
        }

        public void SwitchToScheduleProfileMode()
        {
            tabControl.SelectedTab = tabScheduleProfile;
            ScheduleMode = ScheduleViewModel.Profile;
        }

        public void ClearInputs()
        {
            ContainerId = 0;
            ContainerName = string.Empty;
            ContainerNote = string.Empty;
        }

        public void ClearScheduleInputs()
        {
            ScheduleId = 0;
            ScheduleName = string.Empty;
            ScheduleYear = DateTime.Now.Year;
            ScheduleMonth = DateTime.Now.Month;
            SchedulePeoplePerShift = 1;
            ScheduleShift1 = string.Empty;
            ScheduleShift2 = string.Empty;
            ScheduleMaxHoursPerEmp = 1;
            ScheduleMaxConsecutiveDays = 1;
            ScheduleMaxConsecutiveFull = 1;
            ScheduleMaxFullPerMonth = 1;
            ScheduleComment = string.Empty;
            foreach (int i in checkedAvailabilities.CheckedIndices)
                checkedAvailabilities.SetItemChecked(i, false);
            ScheduleSlots = new List<ScheduleSlotModel>();
        }

        public void ShowInfo(string text) => MessageBox.Show(text, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public void ShowError(string text) => MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        public bool Confirm(string text, string? caption = null)
        {
            return MessageBox.Show(text, caption ?? "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(ContainerName): errorProviderContainer.SetError(inputContainerName, kv.Value); break;
                    case nameof(ContainerNote): errorProviderContainer.SetError(inputContainerNote, kv.Value); break;
                }
            }
        }

        public void SetScheduleValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearScheduleValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(ScheduleName): errorProviderSchedule.SetError(inputScheduleName, kv.Value); break;
                    case nameof(ScheduleContainerId): errorProviderSchedule.SetError(comboShop, kv.Value); break;
                    case nameof(ScheduleShopId): errorProviderSchedule.SetError(comboShop, kv.Value); break;
                    case nameof(ScheduleYear): errorProviderSchedule.SetError(inputYear, kv.Value); break;
                    case nameof(ScheduleMonth): errorProviderSchedule.SetError(inputMonth, kv.Value); break;
                    case nameof(SchedulePeoplePerShift): errorProviderSchedule.SetError(inputPeoplePerShift, kv.Value); break;
                    case nameof(ScheduleShift1): errorProviderSchedule.SetError(inputShift1, kv.Value); break;
                    case nameof(ScheduleShift2): errorProviderSchedule.SetError(inputShift2, kv.Value); break;
                    case nameof(ScheduleMaxHoursPerEmp): errorProviderSchedule.SetError(inputMaxHours, kv.Value); break;
                }
            }
        }

        public void ClearValidationErrors()
        {
            errorProviderContainer.Clear();
        }

        public void ClearScheduleValidationErrors()
        {
            errorProviderSchedule.Clear();
        }

        public void SetProfile(ContainerModel model)
        {
            lblContainerName.Text = $"Name: {model.Name}";
            lblContainerNote.Text = $"Note: {model.Note}";
        }

        public void SetScheduleProfile(ScheduleModel model)
        {
            lblScheduleSummary.Text = $"{model.Name} ({model.Year}/{model.Month}) - {model.Shop?.Name}";
            scheduleSlotProfileGrid.DataSource = new BindingSource { DataSource = model.Slots.ToList() };
        }

        private void inputMaxFull_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
