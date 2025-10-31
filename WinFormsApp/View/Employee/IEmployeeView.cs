using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp.View.Employee
{
    public interface IEmployeeView
    {
        EmployeeViewMode Mode { get; set; }
        EmployeeViewMode CancelTarget { get; set; }
        int Id { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string? Phone { get; set; }
        string? Email { get; set; }

        string SearchValue { get; set; }
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

        void SetEmployeeListBindingSource(BindingSource employeeList);
        void SwitchToEditMode();
        void SwitchToListMode();
        void ClearInputs();
        void ClearValidationErrors();
        void SetValidationErrors(IReadOnlyDictionary<string, string> errors);
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SwitchToProfileMode();
        void SetProfile(EmployeeModel model);
    }
}
