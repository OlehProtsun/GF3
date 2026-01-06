using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.ViewModel;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Shop
{
    public interface IShopView : IBusyView
    {
        ShopViewModel Mode { get; set; }
        ShopViewModel CancelTarget { get; set; }
        int Id { get; set; }
        string Name { get; set; }
        string Address { get; set; }
        string? Description { get; set; }

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

        void SetShopListBindingSource(BindingSource shopList);
        void SwitchToEditMode();
        void SwitchToListMode();
        void ClearInputs();
        void ClearValidationErrors();
        void SetValidationErrors(IReadOnlyDictionary<string, string> errors);
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SwitchToProfileMode();
        void SetProfile(ShopModel model);
    }
}
