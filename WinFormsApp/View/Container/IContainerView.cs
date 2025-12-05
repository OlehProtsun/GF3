using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.Collections.Generic;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public interface IContainerView
    {
        ContainerViewModel Mode { get; set; }
        ContainerViewModel CancelTarget { get; set; }

        int ContainerId { get; set; }
        string ContainerName { get; set; }
        string? ContainerNote { get; set; }
        string SearchValue { get; set; }

        // schedule fields
        ScheduleViewModel ScheduleMode { get; set; }
        ScheduleViewModel ScheduleCancelTarget { get; set; }
        int ScheduleId { get; set; }
        int ScheduleContainerId { get; set; }
        int ScheduleShopId { get; set; }
        string ScheduleName { get; set; }
        int ScheduleYear { get; set; }
        int ScheduleMonth { get; set; }
        int SchedulePeoplePerShift { get; set; }
        string ScheduleShift1 { get; set; }
        string ScheduleShift2 { get; set; }
        int ScheduleMaxHoursPerEmp { get; set; }
        int ScheduleMaxConsecutiveDays { get; set; }
        int ScheduleMaxConsecutiveFull { get; set; }
        int ScheduleMaxFullPerMonth { get; set; }
        string? ScheduleComment { get; set; }
        ScheduleStatus ScheduleStatus { get; set; }
        string ScheduleSearch { get; set; }
        IList<int> SelectedAvailabilityIds { get; }
        IList<ScheduleSlotModel> ScheduleSlots { get; set; }

        bool IsEdit { get; set; }
        bool IsSuccessful { get; set; }
        string Message { get; set; }

        event Func<CancellationToken, Task>? SearchEvent;
        event Func<CancellationToken, Task>? AddEvent;
        event Func<CancellationToken, Task>? EditEvent;
        event Func<CancellationToken, Task>? DeleteEvent;
        event Func<CancellationToken, Task>? SaveEvent;
        event Func<CancellationToken, Task>? CancelEvent;
        event Func<CancellationToken, Task>? OpenProfileEvent;

        // schedule events
        event Func<CancellationToken, Task>? ScheduleSearchEvent;
        event Func<CancellationToken, Task>? ScheduleAddEvent;
        event Func<CancellationToken, Task>? ScheduleEditEvent;
        event Func<CancellationToken, Task>? ScheduleDeleteEvent;
        event Func<CancellationToken, Task>? ScheduleSaveEvent;
        event Func<CancellationToken, Task>? ScheduleCancelEvent;
        event Func<CancellationToken, Task>? ScheduleOpenProfileEvent;
        event Func<CancellationToken, Task>? ScheduleGenerateEvent;

        void SetContainerBindingSource(BindingSource containers);
        void SetScheduleBindingSource(BindingSource schedules);
        void SetSlotBindingSource(BindingSource slots);
        void SetShopList(IEnumerable<ShopModel> shops);
        void SetAvailabilityList(IEnumerable<AvailabilityMonthModel> availabilities);

        void SwitchToEditMode();
        void SwitchToListMode();
        void SwitchToProfileMode();

        void SwitchToScheduleEditMode();
        void SwitchToScheduleListMode();
        void SwitchToScheduleProfileMode();

        void ClearInputs();
        void ClearScheduleInputs();
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SetValidationErrors(IReadOnlyDictionary<string, string> errors);
        void SetScheduleValidationErrors(IReadOnlyDictionary<string, string> errors);
        void ClearValidationErrors();
        void ClearScheduleValidationErrors();
        void SetProfile(ContainerModel model);
        void SetScheduleProfile(ScheduleModel model);
    }
}
