using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Shop
{
    public partial class ShopPresenter
    {
        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();
            _view.IsEdit = false;
            _view.IsSuccessful = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = ShopViewModel.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
        {
            var shop = CurrentShop();
            if (shop is null) return Task.CompletedTask;

            _view.ClearValidationErrors();

            _view.Id = shop.Id;
            _view.Name = shop.Name;
            _view.Address = shop.Address;
            _view.Description = shop.Description;

            _view.IsEdit = true;
            _view.IsSuccessful = false;
            _view.Message = "Edit the data and press Save.";

            _view.CancelTarget = (_view.Mode == ShopViewModel.Profile)
                ? ShopViewModel.Profile
                : ShopViewModel.List;

            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnSaveEventAsync(CancellationToken ct) =>
            RunBusySafeAsync(async innerCt =>
            {
                _view.ClearValidationErrors();
                var model = BuildModelFromView();

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
                    await _service.UpdateAsync(model, innerCt);
                    _view.ShowInfo("Shop updated successfully.");
                }
                else
                {
                    await _service.CreateAsync(model, innerCt);
                    _view.ShowInfo("Shop added successfully.");
                }

                _view.IsSuccessful = true;

                await LoadShopsAsync(ct2 => _service.GetAllAsync(ct2), innerCt, selectId: model.Id);
                SwitchToTargetAfterSaveOrCancel();
            }, ct, "Saving shop...");

        private Task OnDeleteEventAsync(CancellationToken ct) =>
            RunBusySafeAsync(async innerCt =>
            {
                var shop = CurrentShop();
                if (shop is null) return;

                if (!_view.Confirm($"Delete {shop.Name}?"))
                    return;

                await _service.DeleteAsync(shop.Id, innerCt);

                _view.IsSuccessful = true;
                _view.ShowInfo("Shop deleted successfully.");

                await LoadShopsAsync(ct2 => _service.GetAllAsync(ct2), innerCt, selectId: null);
                _view.SwitchToListMode();
            }, ct, "Deleting shop...");

        private Task OnCancelEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            SwitchToTargetAfterSaveOrCancel();
            return Task.CompletedTask;
        }

        private void SwitchToTargetAfterSaveOrCancel()
        {
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
        }

        private Task OnSearchEventAsync(CancellationToken ct) =>
            RunBusySafeAsync(async innerCt =>
            {
                var term = _view.SearchValue;

                if (string.IsNullOrWhiteSpace(term))
                {
                    await LoadShopsAsync(ct2 => _service.GetAllAsync(ct2), innerCt, selectId: null);
                    return;
                }

                await LoadShopsAsync(ct2 => _service.GetByValueAsync(term, ct2), innerCt, selectId: null);
            }, ct, "Searching shops...");

        private Task OnOpenProfileAsync(CancellationToken ct)
        {
            var shop = CurrentShop();
            if (shop is null) return Task.CompletedTask;

            _view.SetProfile(shop);

            _view.CancelTarget = ShopViewModel.List;
            _view.SwitchToProfileMode();
            return Task.CompletedTask;
        }
    }
}
