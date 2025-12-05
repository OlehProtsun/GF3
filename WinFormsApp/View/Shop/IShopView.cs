using DataAccessLayer.Models;
using System.Windows.Forms;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Shop
{
    public interface IShopView
    {
        ShopViewModel Mode { get; set; }
        ShopViewModel CancelTarget { get; set; }

        int ShopId { get; set; }
        string ShopName { get; set; }
        string? ShopDescription { get; set; }
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

        void SetListBindingSource(BindingSource shops);
        void SwitchToEditMode();
        void SwitchToListMode();
        void ClearInputs();
        void ShowInfo(string text);
        void ShowError(string text);
        bool Confirm(string text, string? caption = null);
        void SetValidationErrors(IReadOnlyDictionary<string, string> errors);
        void ClearValidationErrors();
        void SetProfile(ShopModel model);
        void SwitchToProfileMode();
    }
}
