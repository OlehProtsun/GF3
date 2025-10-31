using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinFormsApp.View.Employee;

namespace WinFormsApp.Presenter
{
    public class EmployeePresenter
    {
        private readonly IEmployeeView _view;
        private readonly IEmployeeService _service;
        private readonly BindingSource _bindingSource = new();

        public EmployeePresenter(IEmployeeView view, IEmployeeService service)
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

            _view.SetEmployeeListBindingSource(_bindingSource);
        }

        public async Task InitializeAsync() => await LoadAllEmployeeList();

        private async Task LoadAllEmployeeList()
        {
            _bindingSource.DataSource = await _service.GetAllAsync();
        }

        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();
            _view.IsEdit = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = EmployeeViewMode.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
        {
            var employee = (EmployeeModel?)_bindingSource.Current;
            if (employee is null) return Task.CompletedTask;

            _view.ClearValidationErrors();
            _view.Id = employee.Id;
            _view.FirstName = employee.FirstName;
            _view.LastName = employee.LastName;
            _view.Email = employee.Email;
            _view.Phone = employee.Phone;
            _view.IsEdit = true;
            _view.Message = "Edit the data and press Save.";

            _view.CancelTarget = (_view.Mode == EmployeeViewMode.Profile)
                ? EmployeeViewMode.Profile
                : EmployeeViewMode.List;

            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private async Task OnSaveEventAsync(CancellationToken ct)
        {
            try
            {
                var model = new EmployeeModel
                {
                    Id = _view.Id,
                    FirstName = _view.FirstName,
                    LastName = _view.LastName,
                    Email = _view.Email,
                    Phone = _view.Phone
                };

                // VALIDATION
                var errors = Validate(model);
                if (errors.Count > 0)
                {
                    _view.SetValidationErrors(errors);
                    _view.IsSuccessful = false;
                    _view.Message = "Please fix the highlighted fields.";
                    return; // не зберігаємо
                }

                if (_view.IsEdit)
                {
                    await _service.UpdateAsync(model);
                    _view.ShowInfo("Employee updated successfully.");
                }
                else
                {
                    await _service.CreateAsync(model);
                    _view.ShowInfo("Employee added successfully.");
                }

                _view.IsSuccessful = true;

                await LoadAllEmployeeList();
                if (_view.CancelTarget == EmployeeViewMode.Profile)
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
                var employee = (EmployeeModel?)_bindingSource.Current;
                if (employee == null) return;

                if (!_view.Confirm($"Delete {employee.FirstName} {employee.LastName}?"))
                    return;

                await _service.DeleteAsync(employee.Id);
                _view.IsSuccessful = true;
                _view.ShowInfo("Employee deleted successfully.");
                await LoadAllEmployeeList();
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

            if (_view.Mode == EmployeeViewMode.Edit)
            {
                // В Edit повертаємось туди, звідки зайшли
                if (_view.CancelTarget == EmployeeViewMode.Profile)
                    _view.SwitchToProfileMode();
                else
                    _view.SwitchToListMode();
            }
            else
            {
                // З інших режимів — завжди в List (твоє правило для профілю)
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
                    ? await _service.GetAllAsync()
                    : await _service.GetByValueAsync(term);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        // Простий валідатор (можеш замінити на FluentValidation)
        private static Dictionary<string, string> Validate(EmployeeModel m)
        {
            var map = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(m.FirstName))
                map[nameof(m.FirstName)] = "First name is required.";

            if (string.IsNullOrWhiteSpace(m.LastName))
                map[nameof(m.LastName)] = "Last name is required.";

            if (!string.IsNullOrWhiteSpace(m.Email))
            {
                // Дуже простий патерн для прикладу
                var emailOk = System.Text.RegularExpressions.Regex.IsMatch(
                    m.Email, @"^\S+@\S+\.\S+$", RegexOptions.IgnoreCase);
                if (!emailOk)
                    map[nameof(m.Email)] = "Invalid email format.";
            }

            if (!string.IsNullOrWhiteSpace(m.Phone))
            {
                var phoneOk = System.Text.RegularExpressions.Regex.IsMatch(
                    m.Phone, @"^[0-9+\-\s()]{5,}$");
                if (!phoneOk)
                    map[nameof(m.Phone)] = "Invalid phone number.";
            }

            return map;
        }

        private Task OnOpenProfileAsync(CancellationToken ct)
        {
            var employee = (EmployeeModel?)_bindingSource.Current;
            if (employee is null)
                return Task.CompletedTask;

             _view.SetProfile(employee);

            _view.CancelTarget = EmployeeViewMode.List;
            _view.SwitchToProfileMode();
            return Task.CompletedTask;
        }
    }
}
