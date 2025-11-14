using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp.View.Availability;
using WinFormsApp.View.Employee;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter
{
    public class AvailabilityPresenter
    {
        private readonly IAvailabilityView _view;
        private readonly IAvailabilityMonthService _service;
        private readonly IEmployeeService _employeeService; 
        private readonly BindingSource _bindingSource = new();

        public AvailabilityPresenter
            (
                IAvailabilityView view,
                IAvailabilityMonthService service,
                IEmployeeService employeeService
            )
        {
            _view = view;
            _service = service;
            _employeeService = employeeService;

            _view.SearchEvent += OnSearchEventAsync;
            _view.AddEvent += OnAddEventAsync;
            _view.EditEvent += OnEditEventAsync;
            _view.DeleteEvent += OnDeleteEventAsync;
            _view.SaveEvent += OnSaveEventAsync;
            _view.CancelEvent += OnCancelEventAsync;
            _view.OpenProfileEvent += OnOpenProfileAsync;

            _view.SetListBindingSource(_bindingSource);
        }

        public async Task InitializeAsync() {

            await LoadAllAvailabilityMonthList();
            await LoadEmployees();
        }

        private async Task LoadAllAvailabilityMonthList()
        {
            _bindingSource.DataSource = await _service.GetAllAsync();
        }

        private async Task LoadEmployees()
        {
            var employees = await _employeeService.GetAllAsync();
            _view.SetEmployeeList(employees);
        }

        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearInputs();
            _view.ClearValidationErrors();
            _view.IsEdit = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = AvailabilityViewModel.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
        {

            var availabilityMonth = (AvailabilityMonthModel?)_bindingSource.Current;
            if (availabilityMonth is null) return Task.CompletedTask;

            _view.ClearValidationErrors();
            _view.AvailabilityMonthId = availabilityMonth.Id;
            _view.EmployeeId = availabilityMonth.EmployeeId;
            _view.Year = availabilityMonth.Year;
            _view.Month = availabilityMonth.Month;
            _view.AvailabilityMonthName = availabilityMonth.Name;
            _view.IsEdit = true;
            _view.Message = "Edit the data and press Save.";

            _view.CancelTarget = (_view.Mode == AvailabilityViewModel.Profile)
                ? AvailabilityViewModel.Profile
                : AvailabilityViewModel.List;



            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private async Task OnSaveEventAsync(CancellationToken ct)
        {
            try
            {
                var model = new AvailabilityMonthModel
                {
                    Id = _view.AvailabilityMonthId,
                    Year = _view.Year,
                    Month = _view.Month,
                    EmployeeId = _view.EmployeeId,
                    Name = _view.AvailabilityMonthName,

                };

                // VALIDATION
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
                    await _service.UpdateAsync(model);
                    _view.ShowInfo("Availability Month updated successfully.");
                }
                else
                {
                    await _service.CreateAsync(model);
                    _view.ShowInfo("Availability Month added successfully.");
                }

                _view.IsSuccessful = true;

                await LoadAllAvailabilityMonthList();

                if (_view.CancelTarget == AvailabilityViewModel.Profile)
                    _view.SwitchToProfileMode();
                else
                    _view.SwitchToListMode();

            }
            catch (Exception ex)
            {
                _view.ShowError(ex.GetBaseException().Message);
            }
        }

        private async Task OnDeleteEventAsync(CancellationToken ct)
        {
            try
            {
                var model = (AvailabilityMonthModel?)_bindingSource.Current;
                if (model == null) return;

                if (!_view.Confirm($"Delete {model.Name} ?"))
                    return;

                await _service.DeleteAsync(model.Id);
                _view.IsSuccessful = true;
                _view.ShowInfo("Availability Month deleted successfully.");
                await LoadAllAvailabilityMonthList();
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

            if (_view.Mode == AvailabilityViewModel.Edit)
            {

                if (_view.CancelTarget == AvailabilityViewModel.Profile)
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
                    ? await _service.GetAllAsync()
                    : await _service.GetByValueAsync(term);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private Dictionary<string, string> Validate(AvailabilityMonthModel model)
        {
            var errors = new Dictionary<string, string>();

            if (model.EmployeeId <= 0)
                errors[nameof(model.EmployeeId)] = "Select an employee.";

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(model.Name)] = "Indicate the name of the month of availability.";

            if (model.Month < 1 || model.Month > 12)
                errors[nameof(model.Month)] = "The month must be between 1 and 12.";

            if (model.Year < DateTime.Today.Year - 1 || model.Year > DateTime.Today.Year + 5)
                errors[nameof(model.Year)] = "Invalid year.";

            return errors;
        }

        private Task OnOpenProfileAsync(CancellationToken ct)
        {
            var employee = (AvailabilityMonthModel?)_bindingSource.Current;
            if (employee is null)
                return Task.CompletedTask;

            _view.SetProfile(employee);

            _view.CancelTarget = AvailabilityViewModel.List;
            _view.SwitchToProfileMode();
            return Task.CompletedTask;
        }
    }
}
