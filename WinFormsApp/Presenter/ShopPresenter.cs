using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System.Windows.Forms;
using WinFormsApp.View.Shop;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter
{
    public class ShopPresenter
    {
        private readonly IShopView _view;
        private readonly IShopService _service;
        private readonly BindingSource _bindingSource = new();

        public ShopPresenter(IShopView view, IShopService service)
        {
            _view = view;
            _service = service;

            _view.SearchEvent += OnSearchEventAsync;
            _view.AddEvent += OnAddEventAsync;
            _view.EditEvent += OnEditEventAsync;
            _view.DeleteEvent += OnDeleteEventAsync;
            _view.SaveEvent += OnSaveEventAsync;
            _view.CancelEvent += OnCancelEventAsync;
            _view.OpenProfileEvent += OnOpenProfileAsync;

            _view.SetListBindingSource(_bindingSource);
        }

        public async Task InitializeAsync() => await LoadAllAsync();

        private async Task LoadAllAsync()
        {
            _bindingSource.DataSource = await _service.GetAllAsync();
        }

        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();
            _view.IsEdit = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = ShopViewModel.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
        {
            var shop = (ShopModel?)_bindingSource.Current;
            if (shop is null) return Task.CompletedTask;

            _view.ClearValidationErrors();
            _view.ShopId = shop.Id;
            _view.ShopName = shop.Name;
            _view.ShopDescription = shop.Description;
            _view.IsEdit = true;
            _view.Message = "Edit the data and press Save.";

            _view.CancelTarget = (_view.Mode == ShopViewModel.Profile)
                ? ShopViewModel.Profile
                : ShopViewModel.List;

            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private async Task OnSaveEventAsync(CancellationToken ct)
        {
            try
            {
                var model = new ShopModel
                {
                    Id = _view.ShopId,
                    Name = _view.ShopName,
                    Description = _view.ShopDescription
                };

                var errors = Validate(model);
                if (errors.Count > 0)
                {
                    _view.SetValidationErrors(errors);
                    _view.IsSuccessful = false;
                    _view.Message = "Please fix the highlighted fields.";
                    return;
                }

                if (_view.IsEdit)
                {
                    await _service.UpdateAsync(model, ct);
                    _view.ShowInfo("Shop updated successfully.");
                }
                else
                {
                    await _service.CreateAsync(model, ct);
                    _view.ShowInfo("Shop added successfully.");
                }

                _view.IsSuccessful = true;

                await LoadAllAsync();
                if (_view.CancelTarget == ShopViewModel.Profile)
                    _view.SwitchToProfileMode();
                else
                    _view.SwitchToListMode();
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private async Task OnDeleteEventAsync(CancellationToken ct)
        {
            try
            {
                var shop = (ShopModel?)_bindingSource.Current;
                if (shop is null) return;

                if (!_view.Confirm($"Delete {shop.Name} ?"))
                    return;

                await _service.DeleteAsync(shop.Id, ct);
                _view.IsSuccessful = true;
                _view.ShowInfo("Shop deleted successfully.");
                await LoadAllAsync();
                _view.SwitchToListMode();
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private Task OnCancelEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();

            if (_view.Mode == ShopViewModel.Edit)
            {
                if (_view.CancelTarget == ShopViewModel.Profile)
                    _view.SwitchToProfileMode();
                else
                    _view.SwitchToListMode();
            }
            else
            {
                _view.SwitchToListMode();
            }

            return Task.CompletedTask;
        }

        private async Task OnSearchEventAsync(CancellationToken ct)
        {
            try
            {
                var term = _view.SearchValue;
                _bindingSource.DataSource = string.IsNullOrWhiteSpace(term)
                    ? await _service.GetAllAsync(ct)
                    : await _service.GetByValueAsync(term, ct);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private Task OnOpenProfileAsync(CancellationToken ct)
        {
            var shop = (ShopModel?)_bindingSource.Current;
            if (shop is null)
                return Task.CompletedTask;

            _view.SetProfile(shop);
            _view.CancelTarget = ShopViewModel.List;
            _view.SwitchToProfileMode();
            return Task.CompletedTask;
        }

        private static Dictionary<string, string> Validate(ShopModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(IShopView.ShopName)] = "Name is required.";

            return errors;
        }
    }
}
