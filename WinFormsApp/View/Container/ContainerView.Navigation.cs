using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
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
            Mode = ContainerViewModel.Profile;     // ✅ щоб стан відповідав табу
            ScheduleMode = ScheduleViewModel.List;
        }

        public void SwitchToScheduleProfileMode()
        {
            tabControl.SelectedTab = tabScheduleProfile;
            ScheduleMode = ScheduleViewModel.Profile;
            ScheduleCancelTarget = ScheduleViewModel.List;

            RefreshScheduleProfileIfOpened();
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
            ScheduleShopId = 0;
            ScheduleYear = DateTime.Today.Year;
            ScheduleMonth = DateTime.Today.Month;
            SchedulePeoplePerShift = 1;
            ScheduleShift1 = string.Empty;
            ScheduleShift2 = string.Empty;
            ScheduleMaxHoursPerEmp = 1;
            ScheduleMaxConsecutiveDays = 1;
            ScheduleMaxConsecutiveFull = 1;
            ScheduleMaxFullPerMonth = 1;
            ScheduleNote = string.Empty;

            SelectedAvailabilityGroupId = 0;

            // ✅ ГОЛОВНЕ: щоб новий графік був "пустий"
            ScheduleEmployees = new List<ScheduleEmployeeModel>();

            // ✅ теж логічно скидати, щоб не висів старий вибір
            ScheduleEmployeeId = 0;

            // slots теж чистимо
            ScheduleSlots = new List<ScheduleSlotModel>();
        }

        public void SetProfile(ContainerModel model)
        {
            lblContainerName.Text = model.Name;
            lblContainerNote.Text = model.Note;
            labelId.Text = $"{model.Id}";
        }

        public void SetScheduleProfile(ScheduleModel model)
        {
            _scheduleProfileModel = model;

            ScheduleId = model.Id;
            ScheduleYear = model.Year;
            ScheduleMonth = model.Month;

            lblScheduleId.Text = $"{model.Id}";
            labelName.Text = model.Name;
            lblScheduleFromContainer.Text = model.Container?.Name;
            lblScheduleYear.Text = $"{model.Year}";
            lblScheduleMonth.Text = $"{model.Month}";
            lblScheduleNote.Text = string.IsNullOrWhiteSpace(model.Note) ? "---" : model.Note;

            IList<ScheduleSlotModel> slots =
                _slots.Count > 0 ? _slots :
                (model.Slots?.ToList() ?? new List<ScheduleSlotModel>());

            BindScheduleMatrix(
                block: null,
                grid: scheduleSlotProfileGrid,
                year: model.Year,
                month: model.Month,
                slots: slots,
                employees: _employees,
                readOnly: true,
                configureGrid: false
            );
        }

        private void RefreshScheduleProfileIfOpened()
        {
            if (tabControl.SelectedTab != tabScheduleProfile) return;
            RefreshScheduleProfileMatrix();
        }

        private void RefreshScheduleProfileMatrix()
        {
            if (ScheduleId <= 0) return;

            var year = _scheduleProfileModel?.Year ?? ScheduleYear;
            var month = _scheduleProfileModel?.Month ?? ScheduleMonth;

            IList<ScheduleSlotModel> slots =
                _slots.Count > 0 ? _slots :
                (_scheduleProfileModel?.Slots?.ToList() ?? new List<ScheduleSlotModel>());

            BindScheduleMatrix(
                block: null,
                grid: scheduleSlotProfileGrid,
                year: year,
                month: month,
                slots: slots,
                employees: _employees,
                readOnly: true,
                configureGrid: false
            );
        }
    }
}
