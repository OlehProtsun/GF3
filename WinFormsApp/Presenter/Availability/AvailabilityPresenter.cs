using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.Windows.Forms;
using WinFormsApp.View.Availability;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Availability
{
    public class AvailabilityPresenter
    {
        private readonly IAvailabilityView _view;
        private readonly IAvailabilityGroupService _service;
        private readonly IEmployeeService _employeeService;
        private readonly IBindService _bindService;
        private static readonly KeysConverter _keysConverter = new();
        private readonly BindingSource _bindsSource = new();
        private readonly BindingSource _bindingSource = new();
        private int? _openedProfileGroupId;
        private readonly Dictionary<int, string> _employeeNames = new();
        private readonly List<EmployeeModel> _allEmployees = new();

        public AvailabilityPresenter(
            IAvailabilityView view,
            IAvailabilityGroupService service,
            IEmployeeService employeeService,
            IBindService bindService)
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

            _view.AddEmployeeToGroupEvent += OnAddEmployeeToGroupAsync;
            _view.RemoveEmployeeFromGroupEvent += OnRemoveEmployeeFromGroupAsync;
            _view.SearchEmployeeEvent += OnSearchEmployeeAsync;

            _view.SetListBindingSource(_bindingSource);
            _view.SetBindsBindingSource(_bindsSource);
        }

        public async Task InitializeAsync()
        {
            await _view.RunBusyAsync(async ct =>
            {
                await LoadAllGroups(ct);
                await LoadEmployees(ct);
                await LoadBinds(ct);
            }, _view.LifetimeToken, "Loading availability...");
        }

        private async Task LoadAllGroups(CancellationToken ct = default)
            => _bindingSource.DataSource = await _service.GetAllAsync(ct);

        private async Task LoadEmployees(CancellationToken ct = default)
        {
            var employees = await _employeeService.GetAllAsync(ct);
            _employeeNames.Clear();
            _allEmployees.Clear();
            _allEmployees.AddRange(employees);

            foreach (var e in employees)
                _employeeNames[e.Id] = $"{e.FirstName} {e.LastName}";

            _view.SetEmployeeList(_allEmployees);
        }

        private async Task LoadBinds(CancellationToken ct = default)
            => _bindsSource.DataSource = await _bindService.GetAllAsync(ct);

        private Task OnAddEmployeeToGroupAsync(CancellationToken ct)
        {
            var empId = _view.EmployeeId;
            if (empId <= 0)
            {
                _view.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            var header = _employeeNames.TryGetValue(empId, out var n) ? n : $"Employee #{empId}";
            if (!_view.TryAddEmployeeColumn(empId, header))
                _view.ShowInfo("This employee is already added.");

            return Task.CompletedTask;
        }

        private Task OnRemoveEmployeeFromGroupAsync(CancellationToken ct)
        {
            var empId = _view.EmployeeId;
            if (empId <= 0)
            {
                _view.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            if (!_view.RemoveEmployeeColumn(empId))
                _view.ShowInfo("This employee is not in the group.");

            return Task.CompletedTask;
        }

        private Task OnAddEventAsync(CancellationToken ct)
        {
            _view.ClearInputs();
            _view.ClearValidationErrors();
            _view.ResetGroupMatrix();

            _view.IsEdit = false;
            _view.Message = "Fill the form, add employees, set codes and press Save.";
            _view.CancelTarget = AvailabilityViewModel.List;
            _view.SwitchToEditMode();

            return Task.CompletedTask;
        }

        private Task OnEditEventAsync(CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                var current = _bindingSource.Current as AvailabilityGroupModel;
                if (current is null) return;

                _view.ClearValidationErrors();
                _view.ResetGroupMatrix();

                var (group, members, days) = await _service.LoadFullAsync(current.Id, innerCt);

                _view.AvailabilityMonthId = group.Id;
                _view.AvailabilityMonthName = group.Name;
                _view.Year = group.Year;
                _view.Month = group.Month;

                // 1) add columns
                foreach (var m in members)
                {
                    var empId = m.EmployeeId;
                    var header = m.Employee is null
                        ? (_employeeNames.TryGetValue(empId, out var n) ? n : $"Employee #{empId}")
                        : $"{m.Employee.FirstName} {m.Employee.LastName}";

                    _view.TryAddEmployeeColumn(empId, header);
                }

                // 2) fill codes per employee
                int dim = DateTime.DaysInMonth(group.Year, group.Month);

                // один lookup на всі days
                var dayLookup = days
                    .GroupBy(d => (d.AvailabilityGroupMemberId, d.DayOfMonth))
                    .ToDictionary(g => g.Key, g => g.Last()); // або LastOrDefault/OrderBy якщо треба

                foreach (var mb in members)
                {
                    var codes = new List<(int day, string code)>(capacity: dim);

                    for (int day = 1; day <= dim; day++)
                    {
                        if (!dayLookup.TryGetValue((mb.Id, day), out var d))
                        {
                            codes.Add((day, "-"));
                            continue;
                        }

                        var code = d.Kind switch
                        {
                            AvailabilityKind.ANY => "+",
                            AvailabilityKind.NONE => "-",
                            AvailabilityKind.INT => d.IntervalStr ?? "",
                            _ => "-"
                        };

                        codes.Add((day, code));
                    }

                    _view.SetEmployeeCodes(mb.EmployeeId, codes);
                }


                _view.IsEdit = true;
                _view.Message = "Edit data and press Save.";
                _view.CancelTarget = (_view.Mode == AvailabilityViewModel.Profile)
                    ? AvailabilityViewModel.Profile
                    : AvailabilityViewModel.List;

                _view.SwitchToEditMode();
            }, ct, "Loading availability...");

        private Task OnSaveEventAsync(CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                _view.ClearValidationErrors();
                var group = new AvailabilityGroupModel
                {
                    Id = _view.AvailabilityMonthId,
                    Name = _view.AvailabilityMonthName?.Trim() ?? "",
                    Year = _view.Year,
                    Month = _view.Month
                };

                var errors = AvailabilityGroupValidator.Validate(group);
                if (errors.Count > 0)
                {
                    _view.SetValidationErrors(errors);
                    return;
                }

                // один раз беремо selected
                var selected = _view.GetSelectedEmployeeIds();
                if (selected.Count == 0)
                {
                    _view.ShowError("Add at least 1 employee to the group.");
                    return;
                }

                // читаємо матрицю тільки для вибраних
                var selectedSet = selected.ToHashSet();
                var raw = _view.ReadGroupCodes()
                               .Where(x => selectedSet.Contains(x.employeeId));

                if (!AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var err))
                {
                    _view.ShowError(err!);
                    return;
                }

                var isNew = group.Id == 0;

                await _service.SaveGroupAsync(group, payload, innerCt);

                _view.IsSuccessful = true;
                _view.ShowInfo(isNew
                    ? "Availability Group added successfully."
                    : "Availability Group updated successfully.");


                await LoadAllGroups(innerCt);

                if (_view.CancelTarget == AvailabilityViewModel.Profile)
                {
                    var profileId = _openedProfileGroupId ?? group.Id;
                    if (profileId > 0)
                    {
                        var (g, members, days) = await _service.LoadFullAsync(profileId, innerCt);
                        _view.SetProfile(g, members, days);
                    }

                    _view.SwitchToProfileMode();
                }
                else
                {
                    _view.SwitchToListMode();
                }
            }, ct, "Saving availability...");

        private Task OnDeleteEventAsync(CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                var current = _bindingSource.Current as AvailabilityGroupModel;
                if (current is null) return;

                if (!_view.Confirm($"Delete '{current.Name}' ?"))
                    return;

                await _service.DeleteAsync(current.Id, innerCt);
                _view.ShowInfo("Availability Group deleted successfully.");
                await LoadAllGroups(innerCt);
                _view.SwitchToListMode();
            }, ct, "Deleting availability...");

        private Task OnCancelEventAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            ResetEmployeeSearch();

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

        private Task OnSearchEmployeeAsync(CancellationToken ct)
        {
            ApplyEmployeeFilter(_view.EmployeeSearchValue);
            return Task.CompletedTask;
        }

        private Task OnSearchEventAsync(CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                var term = _view.SearchValue;
                _bindingSource.DataSource = string.IsNullOrWhiteSpace(term)
                    ? await _service.GetAllAsync(innerCt)
                    : await _service.GetByValueAsync(term, innerCt);
            }, ct, "Searching availability...");

        private Task OnOpenProfileAsync(CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                var current = _bindingSource.Current as AvailabilityGroupModel;
                if (current is null) return;

                _openedProfileGroupId = current.Id;

                var (group, members, days) = await _service.LoadFullAsync(current.Id, innerCt);

                _view.SetProfile(group, members, days);
                _view.CancelTarget = AvailabilityViewModel.List;
                _view.SwitchToProfileMode();
            }, ct, "Loading profile...");

        private Task OnAddBindAsync(CancellationToken ct)
        {
            _bindsSource.Add(new BindModel { IsActive = true });
            return Task.CompletedTask;
        }

        private Task OnDeleteBindAsync(BindModel bind, CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                if (bind is null) return;

                if (bind.Id == 0)
                {
                    _bindsSource.Remove(bind);
                    return;
                }

                await _bindService.DeleteAsync(bind.Id, innerCt);
                await LoadBinds(innerCt);
            }, ct, "Deleting bind...");

        private Task OnUpsertBindAsync(BindModel bind, CancellationToken ct)
            => RunBusySafeAsync(async innerCt =>
            {
                if (bind is null) return;

                if (string.IsNullOrWhiteSpace(bind.Key) && string.IsNullOrWhiteSpace(bind.Value))
                    return;

                if (string.IsNullOrWhiteSpace(bind.Key) || string.IsNullOrWhiteSpace(bind.Value))
                {
                    _view.ShowError("Bind must contain both Key and Value.");
                    return;
                }

                if (!TryNormalizeKey(bind.Key, out var normalizedKey))
                {
                    _view.ShowError("Invalid hotkey format.");
                    return;
                }

                bind.Key = normalizedKey;

                if (bind.Id == 0)
                    await _bindService.CreateAsync(bind, innerCt);
                else
                    await _bindService.UpdateAsync(bind, innerCt);

                await LoadBinds(innerCt);
            }, ct, "Saving bind...");

        private static bool TryNormalizeKey(string raw, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var keys = (Keys)_keysConverter.ConvertFromString(raw.Trim())!;
                normalized = _keysConverter.ConvertToString(keys) ?? raw.Trim();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Task RunBusySafeAsync(Func<CancellationToken, Task> action, CancellationToken ct, string? busyText)
            => _view.RunBusyAsync(async innerCt =>
            {
                try
                {
                    await action(innerCt);
                }
                catch (OperationCanceledException)
                {
                    // опційно ігноруємо
                }
                catch (Exception ex)
                {
                    _view.ShowError(ex.GetBaseException().Message);
                }
            }, ct, busyText);

        private void ApplyEmployeeFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                _view.SetEmployeeList(_allEmployees);
                return;
            }

            var filtered = _allEmployees
                .Where(e => ContainsIgnoreCase(e.FirstName, term) || ContainsIgnoreCase(e.LastName, term))
                .ToList();

            _view.SetEmployeeList(filtered);
        }

        private void ResetEmployeeSearch()
        {
            _view.EmployeeSearchValue = string.Empty;
            _view.SetEmployeeList(_allEmployees);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
