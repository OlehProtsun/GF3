using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ScheduleViewModel ScheduleMode { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ScheduleViewModel ScheduleCancelTarget { get; set; } = ScheduleViewModel.List;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ScheduleId
        {
            get => (int)numberScheduleId.Value;
            set
            {
                numberScheduleId.Value = value;
                var block = GetSelectedScheduleBlock();
                if (block != null)
                    block.ScheduleId = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ScheduleContainerId { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ScheduleShopId
        {
            get => comboScheduleShop.SelectedValue is int id ? id : 0;
            set
            {
                if (value > 0)
                    comboScheduleShop.SelectedValue = value;
                else
                    comboScheduleShop.SelectedIndex = -1;

                UpdateShopIdLabel();
            }
        }

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
        public string ScheduleNote { get => inputScheduleNote.Text; set => inputScheduleNote.Text = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ScheduleSearch { get => inputScheduleSearch.Text; set => inputScheduleSearch.Text = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ScheduleShopSearchValue
        {
            get => textBoxSearchValueFromScheduleEdit.Text;
            set => textBoxSearchValueFromScheduleEdit.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ScheduleAvailabilitySearchValue
        {
            get => textBoxSearchValue2FromScheduleEdit.Text;
            set => textBoxSearchValue2FromScheduleEdit.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ScheduleEmployeeSearchValue
        {
            get => textBoxSearchValue3FromScheduleEdit.Text;
            set => textBoxSearchValue3FromScheduleEdit.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SelectedAvailabilityGroupId
        {
            get => comboScheduleAvailability.SelectedValue is int id ? id : 0;
            set
            {
                if (value > 0)
                    comboScheduleAvailability.SelectedValue = value;
                else
                    comboScheduleAvailability.SelectedIndex = -1;

                UpdateAvailabilityIdLabel();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ScheduleEmployeeId
        {
            get => int.TryParse(lblEmployeeId.Text, out var id) ? id : 0;
            set => lblEmployeeId.Text = value > 0 ? value.ToString() : string.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleSlotModel> ScheduleSlots
        {
            get => GetSelectedScheduleBlock()?.Slots ?? _slots;
            set
            {
                _slots.Clear();
                if (value != null) _slots.AddRange(value);
                var block = GetSelectedScheduleBlock();
                if (block != null)
                {
                    block.Slots.Clear();
                    if (value != null) block.Slots.AddRange(value);
                }

                RequestScheduleGridRefresh(block);
                RefreshScheduleProfileIfOpened();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleEmployeeModel> ScheduleEmployees
        {
            get => GetSelectedScheduleBlock()?.Employees ?? _employees;
            set
            {
                _employees.Clear();
                if (value != null) _employees.AddRange(value);
                var block = GetSelectedScheduleBlock();
                if (block != null)
                {
                    block.Employees.Clear();
                    if (value != null) block.Employees.AddRange(value);
                }

                RequestScheduleGridRefresh(block);
                RefreshScheduleProfileIfOpened();
            }
        }

        private bool _suppressAvailabilitySelectionChanged;

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
        public event Func<CancellationToken, Task>? ScheduleSearchShopEvent;
        public event Func<CancellationToken, Task>? ScheduleSearchAvailabilityEvent;
        public event Func<CancellationToken, Task>? ScheduleSearchEmployeeEvent;
        public event Func<CancellationToken, Task>? ScheduleAddEmployeeToGroupEvent;
        public event Func<CancellationToken, Task>? ScheduleRemoveEmployeeFromGroupEvent;
        public event Func<CancellationToken, Task>? ScheduleAddNewBlockEvent;
        public event Func<Guid, CancellationToken, Task>? ScheduleBlockSelectEvent;
        public event Func<Guid, CancellationToken, Task>? ScheduleBlockCloseEvent;

        public event Func<CancellationToken, Task>? AvailabilitySelectionChangedEvent;


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
            ScheduleSlots = slots?.List?.OfType<ScheduleSlotModel>().ToList()
                           ?? new List<ScheduleSlotModel>();
        }

        public void SetAvailabilityGroupList(IEnumerable<AvailabilityGroupModel> groups)
        {
            var list = (groups ?? Array.Empty<AvailabilityGroupModel>()).ToList();
            var selectedId = SelectedAvailabilityGroupId;

            comboScheduleAvailability.BeginUpdate();
            try
            {
                comboScheduleAvailability.DataSource = null;
                comboScheduleAvailability.DisplayMember = nameof(AvailabilityGroupModel.Name);
                comboScheduleAvailability.ValueMember = nameof(AvailabilityGroupModel.Id);
                comboScheduleAvailability.DataSource = list;

                if (selectedId > 0 && list.Any(g => g.Id == selectedId))
                    comboScheduleAvailability.SelectedValue = selectedId;
                else
                    comboScheduleAvailability.SelectedIndex = list.Count > 0 ? 0 : -1;
            }
            finally
            {
                comboScheduleAvailability.EndUpdate();
            }

            UpdateAvailabilityIdLabel();
        }

        public void SetShopList(IEnumerable<ShopModel> shops)
        {
            var selectedId = ScheduleShopId;

            comboScheduleShop.BeginUpdate();
            try
            {
                comboScheduleShop.DataSource = null;
                comboScheduleShop.DisplayMember = nameof(ShopModel.Name);
                comboScheduleShop.ValueMember = nameof(ShopModel.Id);
                comboScheduleShop.DataSource = (shops ?? Array.Empty<ShopModel>()).ToList();

                if (selectedId > 0 && comboScheduleShop.Items.Count > 0)
                    comboScheduleShop.SelectedValue = selectedId;
                else
                    comboScheduleShop.SelectedIndex = comboScheduleShop.Items.Count > 0 ? 0 : -1;
            }
            finally
            {
                comboScheduleShop.EndUpdate();
            }

            UpdateShopIdLabel();
        }

        public void SetEmployeeList(IEnumerable<EmployeeModel> employees)
        {
            var list = new List<EmployeeListItem>();
            foreach (var employee in employees ?? Array.Empty<EmployeeModel>())
            {
                list.Add(new EmployeeListItem
                {
                    Id = employee.Id,
                    FullName = $"{employee.FirstName} {employee.LastName}"
                });
            }

            var selectedId = ScheduleEmployeeId;

            comboboxEmployee.BeginUpdate();
            try
            {
                comboboxEmployee.DataSource = null;
                comboboxEmployee.DisplayMember = nameof(EmployeeListItem.FullName);
                comboboxEmployee.ValueMember = nameof(EmployeeListItem.Id);
                comboboxEmployee.DataSource = list;

                if (selectedId > 0 && list.Any(e => e.Id == selectedId))
                    comboboxEmployee.SelectedValue = selectedId;
                else
                    comboboxEmployee.SelectedIndex = list.Count > 0 ? 0 : -1;
            }
            finally
            {
                comboboxEmployee.EndUpdate();
            }

            UpdateEmployeeIdLabel();
        }

        public void SetSelectedAvailabilityGroupId(int groupId, bool fireEvent = true)
        {
            SelectedAvailabilityGroupId = groupId;

            if (fireEvent)
                BeginInvoke(new Action(async () =>
                    await SafeRaiseAsync(AvailabilitySelectionChangedEvent)));
        }

        private void UpdateShopIdLabel()
        {
            lbShopId.Text = ScheduleShopId > 0 ? ScheduleShopId.ToString() : string.Empty;
        }

        private void UpdateAvailabilityIdLabel()
        {
            lblAvailabilityID.Text = SelectedAvailabilityGroupId > 0
                ? SelectedAvailabilityGroupId.ToString()
                : string.Empty;
        }

        private void UpdateEmployeeIdLabel()
        {
            var id = comboboxEmployee.SelectedValue is int value ? value : 0;
            ScheduleEmployeeId = id;
        }

    }
}
