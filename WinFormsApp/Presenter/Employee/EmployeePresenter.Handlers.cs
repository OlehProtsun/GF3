using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Employee
{
    public partial class EmployeePresenter
    {
        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();
            _view.IsEdit = false;
            _view.IsSuccessful = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = EmployeeViewModel.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
        {
            var employee = CurrentEmployee();
            if (employee is null) return Task.CompletedTask;

            _view.ClearValidationErrors();

            _view.Id = employee.Id;
            _view.FirstName = employee.FirstName;
            _view.LastName = employee.LastName;
            _view.Email = employee.Email;
            _view.Phone = employee.Phone;

            _view.IsEdit = true;
            _view.IsSuccessful = false;
            _view.Message = "Edit the data and press Save.";

            _view.CancelTarget = (_view.Mode == EmployeeViewModel.Profile)
                ? EmployeeViewModel.Profile
                : EmployeeViewModel.List;

            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnSaveEventAsync(CancellationToken ct) =>
            RunSafeAsync(async () =>
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
                    await _service.UpdateAsync(model, ct);
                    _view.ShowInfo("Employee updated successfully.");
                }
                else
                {
                    await _service.CreateAsync(model, ct);
                    _view.ShowInfo("Employee added successfully.");
                }

                _view.IsSuccessful = true;

                await LoadEmployeesAsync(ct2 => _service.GetAllAsync(ct2), ct, selectId: model.Id);
                SwitchToTargetAfterSaveOrCancel();
            });

        private Task OnDeleteEventAsync(CancellationToken ct) =>
            RunSafeAsync(async () =>
            {
                var employee = CurrentEmployee();
                if (employee is null) return;

                if (!_view.Confirm($"Delete {employee.FirstName} {employee.LastName}?"))
                    return;

                await _service.DeleteAsync(employee.Id, ct);

                _view.IsSuccessful = true;
                _view.ShowInfo("Employee deleted successfully.");

                await LoadEmployeesAsync(ct2 => _service.GetAllAsync(ct2), ct, selectId: null);
                _view.SwitchToListMode();
            });

        private Task OnCancelEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            SwitchToTargetAfterSaveOrCancel();
            return Task.CompletedTask;
        }

        private void SwitchToTargetAfterSaveOrCancel()
        {
            if (_view.Mode == EmployeeViewModel.Edit)
            {
                if (_view.CancelTarget == EmployeeViewModel.Profile)
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
            RunSafeAsync(async () =>
            {
                var term = _view.SearchValue;

                if (string.IsNullOrWhiteSpace(term))
                {
                    await LoadEmployeesAsync(ct2 => _service.GetAllAsync(ct2), ct, selectId: null);
                    return;
                }

                await LoadEmployeesAsync(ct2 => _service.GetByValueAsync(term, ct2), ct, selectId: null);
            });

        private Task OnOpenProfileAsync(CancellationToken ct)
        {
            var employee = CurrentEmployee();
            if (employee is null) return Task.CompletedTask;

            _view.SetProfile(employee);

            _view.CancelTarget = EmployeeViewModel.List;
            _view.SwitchToProfileMode();
            return Task.CompletedTask;
        }
    }
}
