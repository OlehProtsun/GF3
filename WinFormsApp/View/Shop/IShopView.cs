using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Shedule
{
    public interface IShopView
    {
        int ShopId { get; set; }
        string ShopName { get; set; }
        string? ShopDescription { get; set; }
        ICollection<ScheduleModel> Schedules { get; set; }

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
        void SetValidationErrors(IDictionary<string, string> errors);
        void ClearValidationErrors();
        void SetProfile(ShopModel model);
        void SwitchToProfileMode();
    }
}
