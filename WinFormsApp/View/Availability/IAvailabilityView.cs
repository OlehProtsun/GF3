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
        AvailabilityViewModel Mode { get; set; }
        AvailabilityViewModel CancelTarget { get; set; }

        int EmployeeId { get; set; }                 // combobox вибір працівника
        int AvailabilityMonthId { get; set; }        // використовуємо як GroupId (щоб не ламати UI)
        string AvailabilityMonthName { get; set; }   // використовуємо як GroupName
        int Year { get; set; }
        int Month { get; set; }
        string SearchValue { get; set; }

        bool IsEdit { get; set; }
        bool IsSuccessful { get; set; }
        string Message { get; set; }

        // EVENTS
        event Func<CancellationToken, Task>? SearchEvent;
        event Func<CancellationToken, Task>? AddEvent;
        event Func<CancellationToken, Task>? EditEvent;
        event Func<CancellationToken, Task>? DeleteEvent;
        event Func<CancellationToken, Task>? SaveEvent;
        event Func<CancellationToken, Task>? CancelEvent;
        event Func<CancellationToken, Task>? OpenProfileEvent;

        event Func<CancellationToken, Task>? AddBindEvent;
        event Func<BindModel, CancellationToken, Task>? UpsertBindEvent;
        event Func<BindModel, CancellationToken, Task>? DeleteBindEvent;

        event Func<CancellationToken, Task>? AddEmployeeToGroupEvent;
        event Func<CancellationToken, Task>? RemoveEmployeeFromGroupEvent;

        // LIST BINDINGS
        void SetListBindingSource(BindingSource availabilityList);
        void SetBindsBindingSource(BindingSource binds);

        // UI MODE
        void SwitchToEditMode();
        void SwitchToListMode();
        void SwitchToProfileMode();

        // COMMON UI
        void ClearInputs();
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SetValidationErrors(IReadOnlyDictionary<string, string> errors);
        void ClearValidationErrors();

        void SetEmployeeList(IEnumerable<EmployeeModel> employees);

        // ✅ GROUP MATRIX API
        void ResetGroupMatrix();
        bool TryAddEmployeeColumn(int employeeId, string header);
        bool RemoveEmployeeColumn(int employeeId);
        IReadOnlyList<int> GetSelectedEmployeeIds();
        IList<(int employeeId, IList<(int dayOfMonth, string code)> codes)> ReadGroupCodes();
        void SetEmployeeCodes(int employeeId, IList<(int dayOfMonth, string code)> codes);

        // PROFILE (readonly matrix)
        void SetProfile(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days);
    }
}
