using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.View.Container;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private async Task LoadContainersAsync(CancellationToken ct)
            => _containerBinding.DataSource = await _containerService.GetAllAsync(ct);

        private async Task OnSearchCoreAsync(CancellationToken ct)
        {
            var term = _view.SearchValue;

            _containerBinding.DataSource = string.IsNullOrWhiteSpace(term)
                ? await _containerService.GetAllAsync(ct)
                : await _containerService.GetByValueAsync(term, ct);
        }

        private Task OnAddCoreAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();

            _view.IsEdit = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = ContainerViewModel.List;
            _view.SwitchToEditMode();

            return Task.CompletedTask;
        }

        private Task OnEditCoreAsync(CancellationToken ct)
        {
            var container = CurrentContainer;
            if (container is null) return Task.CompletedTask;

            _view.ClearValidationErrors();

            _view.ContainerId = container.Id;
            _view.ContainerName = container.Name;
            _view.ContainerNote = container.Note;

            _view.IsEdit = true;
            _view.Message = "Edit and press Save.";
            _view.CancelTarget = (_view.Mode == ContainerViewModel.Profile)
                ? ContainerViewModel.Profile
                : ContainerViewModel.List;

            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private async Task OnSaveCoreAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            var model = new ContainerModel
            {
                Id = _view.ContainerId,
                Name = _view.ContainerName,
                Note = _view.ContainerNote
            };

            var errors = ValidateContainer(model);
            if (errors.Count > 0)
            {
                _view.SetValidationErrors(errors);
                _view.IsSuccessful = false;
                _view.Message = "Please fix the highlighted fields.";
                return;
            }

            if (_view.IsEdit)
            {
                await _containerService.UpdateAsync(model, ct);
                _view.ShowInfo("Container updated successfully.");
            }
            else
            {
                await _containerService.CreateAsync(model, ct);
                _view.ShowInfo("Container added successfully.");
            }

            _view.IsSuccessful = true;
            await LoadContainersAsync(ct);
            SwitchBackFromContainerEdit();
        }

        private async Task OnDeleteCoreAsync(CancellationToken ct)
        {
            var container = CurrentContainer;
            if (container is null) return;

            if (!_view.Confirm($"Delete {container.Name}?"))
                return;

            await _containerService.DeleteAsync(container.Id, ct);
            _view.ShowInfo("Container deleted successfully.");

            await LoadContainersAsync(ct);
            _view.SwitchToListMode();
        }

        private Task OnCancelCoreAsync(CancellationToken ct)
        {
            SwitchBackFromContainerEdit();
            return Task.CompletedTask;
        }

        private async Task OnOpenProfileCoreAsync(CancellationToken ct)
        {
            var container = CurrentContainer;
            if (container is null) return;

            _view.SetProfile(container);

            await LoadSchedulesAsync(container.Id, search: null, ct);

            _view.CancelTarget = ContainerViewModel.List;
            _view.SwitchToProfileMode();
        }

        private static Dictionary<string, string> ValidateContainer(ContainerModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(IContainerView.ContainerName)] = "Name is required.";

            return errors;
        }
    }
}
