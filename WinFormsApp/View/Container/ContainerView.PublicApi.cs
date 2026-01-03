using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
        public int ScheduleId { get => (int)numberScheduleId.Value; set => numberScheduleId.Value = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int ScheduleContainerId { get; set; }

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

        public IList<int> SelectedAvailabilityGroupIds => checkedAvailabilities.CheckedItems
            .OfType<AvailabilityGroupModel>()
            .Select(g => g.Id)
            .ToList();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleSlotModel> ScheduleSlots
        {
            get => _slots;
            set
            {
                _slots.Clear();
                if (value != null) _slots.AddRange(value);
                RequestScheduleGridRefresh();
                RefreshScheduleProfileIfOpened();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleEmployeeModel> ScheduleEmployees
        {
            get => _employees;
            set
            {
                _employees.Clear();
                if (value != null) _employees.AddRange(value);
                RequestScheduleGridRefresh();
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
            checkedAvailabilities.BeginUpdate();
            try
            {
                checkedAvailabilities.Items.Clear();
                checkedAvailabilities.DisplayMember = nameof(AvailabilityGroupModel.Name);

                foreach (var g in groups)
                    checkedAvailabilities.Items.Add(g, false);
            }
            finally
            {
                checkedAvailabilities.EndUpdate();
            }
        }

        public void SetCheckedAvailabilityGroupIds(IEnumerable<int> groupIds, bool fireEvent = true)
        {
            var set = new HashSet<int>(groupIds ?? Array.Empty<int>());

            checkedAvailabilities.BeginUpdate();
            try
            {
                for (int i = 0; i < checkedAvailabilities.Items.Count; i++)
                {
                    if (checkedAvailabilities.Items[i] is AvailabilityGroupModel g)
                        checkedAvailabilities.SetItemChecked(i, set.Contains(g.Id));
                }
            }
            finally
            {
                checkedAvailabilities.EndUpdate();
            }

            if (fireEvent)
                BeginInvoke(new Action(async () =>
                    await SafeRaiseAsync(AvailabilitySelectionChangedEvent)));
        }

    }
}
