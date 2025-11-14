using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Availability
{
    public interface IAvailabilityView
    {
        int AvailabilityMonthId { get; set; }
        int EmployeeId { get; set; }
        string AvailabilityMonthName { get; set; }
        int Year { get; set; }
        int Month { get; set; } 

        AvailabilityViewModel Mode { get; set; }
        AvailabilityViewModel CancelTarget { get; set; }
        bool IsEdit { get; set; }
        bool IsSuccessful { get; set; }
        string Message { get; set; }
        string SearchValue { get; set; }

        event Func<CancellationToken, Task>? SearchEvent;
        event Func<CancellationToken, Task>? AddEvent;
        event Func<CancellationToken, Task>? EditEvent;
        event Func<CancellationToken, Task>? DeleteEvent;
        event Func<CancellationToken, Task>? SaveEvent;
        event Func<CancellationToken, Task>? CancelEvent;
        event Func<CancellationToken, Task>? OpenProfileEvent;

        void SetListBindingSource(BindingSource days);
        void SwitchToEditMode();
        void SwitchToListMode();
        void ClearInputs();
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SetEmployeeList(IEnumerable<EmployeeModel> employees);
        void SetValidationErrors(IDictionary<string, string> errors);
        void ClearValidationErrors();
        void SetProfile(AvailabilityMonthModel model);
        void SwitchToProfileMode();

    }
}
