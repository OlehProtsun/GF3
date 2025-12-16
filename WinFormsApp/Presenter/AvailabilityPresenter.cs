using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
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
        private readonly IBindService _bindService;
        private readonly BindingSource _bindsSource = new();
        private readonly BindingSource _bindingSource = new();

        public AvailabilityPresenter
            (
                IAvailabilityView view,
                IAvailabilityMonthService service,
                IEmployeeService employeeService,
                IBindService bindService 
            )
        {
            _view = view;
            _service = service;
            _employeeService = employeeService;
            _bindService = bindService;

            _view.SearchEvent += OnSearchEventAsync;
            _view.AddEvent += OnAddEventAsync;
            _view.EditEvent += OnEditEventAsync;
            _view.DeleteEvent += OnDeleteEventAsync;
            _view.SaveEvent += OnSaveEventAsync;
            _view.CancelEvent += OnCancelEventAsync;
            _view.OpenProfileEvent += OnOpenProfileAsync;
            _view.AddBindEvent += OnAddBindAsync;
            _view.UpsertBindEvent += OnUpsertBindAsync;
            _view.DeleteBindEvent += OnDeleteBindAsync;

            _view.SetListBindingSource(_bindingSource);
            _view.SetBindsBindingSource(_bindsSource);
        }

        private Task OnAddBindAsync(CancellationToken ct)
        {
            _bindsSource.Add(new BindModel { IsActive = true });
            return Task.CompletedTask;
        }

        private async Task OnDeleteBindAsync(BindModel bind, CancellationToken ct)
        {
            if (bind is null) return;

            if (bind.Id == 0)
            {
                _bindsSource.Remove(bind);
                return;
            }

            await _bindService.DeleteAsync(bind.Id, ct);
            await LoadBinds(ct);
        }

        private async Task OnUpsertBindAsync(BindModel bind, CancellationToken ct)
        {
            if (bind is null) return;

            // якщо рядок порожній - не чіпаємо
            if (string.IsNullOrWhiteSpace(bind.Key) && string.IsNullOrWhiteSpace(bind.Value))
                return;

            if (string.IsNullOrWhiteSpace(bind.Key) || string.IsNullOrWhiteSpace(bind.Value))
            {
                _view.ShowError("Bind має містити і Key, і Value.");
                return;
            }

            if (!TryNormalizeKey(bind.Key, out var normalizedKey))
            {
                _view.ShowError("Невірний формат хоткею (спробуй натиснути комбінацію в полі Key).");
                return;
            }

            bind.Key = normalizedKey;

            try
            {
                if (bind.Id == 0)
                    await _bindService.CreateAsync(bind, ct);
                else
                    await _bindService.UpdateAsync(bind, ct);

                // щоб одразу бачити відсортовано + з нормалізованим ключем
                await LoadBinds(ct);
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.GetBaseException().Message);
            }
        }

        private static bool TryNormalizeKey(string raw, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var conv = new KeysConverter();
                var keys = (Keys)conv.ConvertFromString(raw.Trim())!;
                normalized = conv.ConvertToString(keys) ?? raw.Trim();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task InitializeAsync() {

            await LoadAllAvailabilityMonthList();
            await LoadEmployees();
            await LoadBinds();
        }

        private async Task LoadBinds(CancellationToken ct = default)
        {
            _bindsSource.DataSource = await _bindService.GetAllAsync(ct);
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

        private async Task OnEditEventAsync(CancellationToken ct)
        {
            var availabilityMonth = (AvailabilityMonthModel?)_bindingSource.Current;
            if (availabilityMonth is null) return;

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

            // Нове: дістаємо дні з сервісу
            var days = await _service.GetDaysForMonthAsync(availabilityMonth.Id, ct);

            var daysDict = days.ToDictionary(d => d.DayOfMonth);
            var rows = new List<AvailabilityDayRow>();

            int daysInMonth = DateTime.DaysInMonth(availabilityMonth.Year, availabilityMonth.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                string value = string.Empty;

                if (daysDict.TryGetValue(day, out var d))
                {
                    value = d.Kind switch
                    {
                        AvailabilityKind.ANY => "+",
                        AvailabilityKind.NONE => "-",
                        AvailabilityKind.INT => d.IntervalStr ?? string.Empty,
                        _ => string.Empty
                    };
                }

                rows.Add(new AvailabilityDayRow
                {
                    DayOfMonth = day,
                    Value = value
                });
            }

            _view.AvailabilityDays = rows;

            _view.SwitchToEditMode();
        }

        private async Task OnSaveEventAsync(CancellationToken ct)
        {
            try
            {
                var month = new AvailabilityMonthModel
                {
                    Id = _view.AvailabilityMonthId,
                    Year = _view.Year,
                    Month = _view.Month,
                    EmployeeId = _view.EmployeeId,
                    Name = _view.AvailabilityMonthName,
                };

                // 1) Валідація місяця
                var errors = Validate(month);
                if (errors.Count > 0)
                {
                    _view.SetValidationErrors(errors);
                    _view.IsSuccessful = false;
                    _view.Message = "Please fix the highlighted fields.";
                    return;
                }

                // 2) Читаємо дні з гріда і парсимо код (+ / - / HH:mm - HH:mm)
                var dayRows = _view.AvailabilityDays;
                var dayEntities = new List<AvailabilityDayModel>();

                foreach (var row in dayRows)
                {
                    if (!TryParseAvailabilityCode(row.Value, out var kind, out var intervalStr))
                    {
                        _view.ShowError($"Невірний інтервал у дні {row.DayOfMonth}.");
                        return;
                    }

                    dayEntities.Add(new AvailabilityDayModel
                    {
                        // Id не чіпаємо – EF заповнить сам
                        AvailabilityMonthId = month.Id, // у сервісі все одно буде перезаписано
                        DayOfMonth = row.DayOfMonth,
                        Kind = kind,
                        IntervalStr = intervalStr
                    });
                }

                // 3) Один виклик у BLL, який всередині зробить create/update + заміну днів
                await _service.SaveWithDaysAsync(month, dayEntities, ct);

                _view.IsSuccessful = true;
                _view.ShowInfo(_view.IsEdit
                    ? "Availability Month updated successfully."
                    : "Availability Month added successfully.");

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

        private async Task OnOpenProfileAsync(CancellationToken ct)
        {
            var availabilityMonth = (AvailabilityMonthModel?)_bindingSource.Current;
            if (availabilityMonth is null)
                return;

            // Заголовки профілю (назва, працівник, Id)
            _view.SetProfile(availabilityMonth);

            // Дні – копія логіки з OnEditEventAsync
            var days = await _service.GetDaysForMonthAsync(availabilityMonth.Id, ct);

            var daysDict = days.ToDictionary(d => d.DayOfMonth);
            var rows = new List<AvailabilityDayRow>();

            int daysInMonth = DateTime.DaysInMonth(availabilityMonth.Year, availabilityMonth.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                string value = string.Empty;

                if (daysDict.TryGetValue(day, out var d))
                {
                    value = d.Kind switch
                    {
                        AvailabilityKind.ANY => "+",
                        AvailabilityKind.NONE => "-",
                        AvailabilityKind.INT => d.IntervalStr ?? string.Empty,
                        _ => string.Empty
                    };
                }

                rows.Add(new AvailabilityDayRow
                {
                    DayOfMonth = day,
                    Value = value
                });
            }

            // Це оновить і edit-грід, і profile-грід
            _view.AvailabilityDays = rows;

            _view.CancelTarget = AvailabilityViewModel.List;
            _view.SwitchToProfileMode();
        }


        //HELPERS

        private bool TryParseAvailabilityCode(
            string code,
            out AvailabilityKind kind,
            out string? intervalStr)
            {
                code = (code ?? string.Empty).Trim();
                intervalStr = null;

                if (string.IsNullOrEmpty(code) || code == "-")
                {
                    kind = AvailabilityKind.NONE;
                    return true;
                }

                if (code == "+")
                {
                    kind = AvailabilityKind.ANY;
                    return true;
                }

                // решта – пробуємо трактувати як інтервал
                if (!TryNormalizeInterval(code, out var normalized))
                {
                    kind = default;
                    return false;
                }

                kind = AvailabilityKind.INT;
                intervalStr = normalized;
                return true;
            }

        private bool TryNormalizeInterval(string input, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return false;

            if (!TimeSpan.TryParse(parts[0], out var start) ||
                !TimeSpan.TryParse(parts[1], out var end))
                return false;

            if (end <= start)
                return false;

            normalized = $"{start:hh\\:mm} - {end:hh\\:mm}";
            return true;
        }

}
}
