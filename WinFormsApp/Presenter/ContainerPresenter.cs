using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.View.Container;
using WinFormsApp.ViewModel;
using System.Globalization;


namespace WinFormsApp.Presenter
{
    public class ContainerPresenter
    {
        private readonly IContainerView _view;
        private readonly IContainerService _containerService;
        private readonly IScheduleService _scheduleService;
        private readonly IShopService _shopService;
        private readonly IAvailabilityMonthService _availabilityService;
        private readonly IScheduleGenerator _generator;

        private readonly BindingSource _containerBinding = new();
        private readonly BindingSource _scheduleBinding = new();
        private readonly BindingSource _slotBinding = new();

        public ContainerPresenter(
            IContainerView view,
            IContainerService containerService,
            IScheduleService scheduleService,
            IShopService shopService,
            IAvailabilityMonthService availabilityService,
            IScheduleGenerator generator)
        {
            _view = view;
            _containerService = containerService;
            _scheduleService = scheduleService;
            _shopService = shopService;
            _availabilityService = availabilityService;
            _generator = generator;

            _view.SearchEvent += OnSearchAsync;
            _view.AddEvent += OnAddAsync;
            _view.EditEvent += OnEditAsync;
            _view.DeleteEvent += OnDeleteAsync;
            _view.SaveEvent += OnSaveAsync;
            _view.CancelEvent += OnCancelAsync;
            _view.OpenProfileEvent += OnOpenProfileAsync;

            _view.ScheduleSearchEvent += OnScheduleSearchAsync;
            _view.ScheduleAddEvent += OnScheduleAddAsync;
            _view.ScheduleEditEvent += OnScheduleEditAsync;
            _view.ScheduleDeleteEvent += OnScheduleDeleteAsync;
            _view.ScheduleSaveEvent += OnScheduleSaveAsync;
            _view.ScheduleCancelEvent += OnScheduleCancelAsync;
            _view.ScheduleOpenProfileEvent += OnScheduleOpenProfileAsync;
            _view.ScheduleGenerateEvent += OnScheduleGenerateAsync;

            _view.SetContainerBindingSource(_containerBinding);
            _view.SetScheduleBindingSource(_scheduleBinding);
            _view.SetSlotBindingSource(_slotBinding);
        }

        public async Task InitializeAsync()
        {
            await LoadContainers();
            await LoadLookups();
        }

        private async Task LoadContainers()
        {
            _containerBinding.DataSource = await _containerService.GetAllAsync();
        }

        private async Task LoadLookups()
        {
            var shops = await _shopService.GetAllAsync();
            _view.SetShopList(shops);

            var availabilities = await _availabilityService.GetAllAsync();
            _view.SetAvailabilityList(availabilities);
        }

        private async Task OnSearchAsync(CancellationToken ct)
        {
            var term = _view.SearchValue;
            _containerBinding.DataSource = string.IsNullOrWhiteSpace(term)
                ? await _containerService.GetAllAsync(ct)
                : await _containerService.GetByValueAsync(term, ct);
        }

        private Task OnAddAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            _view.ClearInputs();
            _view.IsEdit = false;
            _view.Message = "Fill the form and press Save.";
            _view.CancelTarget = ContainerViewModel.List;
            _view.SwitchToEditMode();
            return Task.CompletedTask;
        }

        private Task OnEditAsync(CancellationToken ct)
        {
            var container = (ContainerModel?)_containerBinding.Current;
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

        private async Task OnSaveAsync(CancellationToken ct)
        {
            try
            {
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
                await LoadContainers();
                if (_view.CancelTarget == ContainerViewModel.Profile)
                    _view.SwitchToProfileMode();
                else
                    _view.SwitchToListMode();
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private async Task OnDeleteAsync(CancellationToken ct)
        {
            try
            {
                var container = (ContainerModel?)_containerBinding.Current;
                if (container is null) return;

                if (!_view.Confirm($"Delete {container.Name}?"))
                    return;

                await _containerService.DeleteAsync(container.Id, ct);
                _view.ShowInfo("Container deleted successfully.");
                await LoadContainers();
                _view.SwitchToListMode();
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private Task OnCancelAsync(CancellationToken ct)
        {
            _view.ClearValidationErrors();
            if (_view.CancelTarget == ContainerViewModel.Profile)
                _view.SwitchToProfileMode();
            else
                _view.SwitchToListMode();
            return Task.CompletedTask;
        }

        private async Task OnOpenProfileAsync(CancellationToken ct)
        {
            var container = (ContainerModel?)_containerBinding.Current;
            if (container is null) return;

            _view.SetProfile(container);
            await LoadSchedules(container.Id);
            _view.CancelTarget = ContainerViewModel.List;
            _view.SwitchToProfileMode();
        }

        private async Task LoadSchedules(int containerId, string? search = null, CancellationToken ct = default)
        {
            _view.ScheduleContainerId = containerId;
            _scheduleBinding.DataSource = await _scheduleService.GetByContainerAsync(containerId, search, ct);
        }

        private async Task OnScheduleSearchAsync(CancellationToken ct)
        {
            var container = (ContainerModel?)_containerBinding.Current;
            if (container is null) return;
            await LoadSchedules(container.Id, _view.ScheduleSearch, ct);
        }

        private async Task OnScheduleAddAsync(CancellationToken ct)
        {
            var container = (ContainerModel?)_containerBinding.Current;
            if (container is null)
            {
                _view.ShowError("Select a container first.");
                return;
            }

            // перезавантажити магазини та availability з БЛ/БД
            await LoadLookups();

            _view.ClearScheduleValidationErrors();
            _view.ClearScheduleInputs();
            _view.IsEdit = false;
            _view.ScheduleContainerId = container.Id;
            _view.ScheduleCancelTarget = ScheduleViewModel.List;
            _view.SwitchToScheduleEditMode();
        }

        private async Task OnScheduleEditAsync(CancellationToken ct)
        {
            var schedule = (ScheduleModel?)_scheduleBinding.Current;
            if (schedule is null) return;

            await LoadLookups();

            // Підтягуємо повний графік з БД: працівники + слоти
            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed != null)
            {
                _view.ScheduleEmployees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                _view.ScheduleSlots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
            }
            else
            {
                _view.ScheduleEmployees = new List<ScheduleEmployeeModel>();
                _view.ScheduleSlots = new List<ScheduleSlotModel>();
            }

            // Заповнюємо хедер форми, як було
            _view.ClearScheduleValidationErrors();
            _view.ScheduleId = schedule.Id;
            _view.ScheduleContainerId = schedule.ContainerId;
            _view.ScheduleShopId = schedule.ShopId;
            _view.ScheduleName = schedule.Name;
            _view.ScheduleYear = schedule.Year;
            _view.ScheduleMonth = schedule.Month;
            _view.SchedulePeoplePerShift = schedule.PeoplePerShift;
            _view.ScheduleShift1 = schedule.Shift1Time;
            _view.ScheduleShift2 = schedule.Shift2Time;
            _view.ScheduleMaxHoursPerEmp = schedule.MaxHoursPerEmpMonth;
            _view.ScheduleMaxConsecutiveDays = schedule.MaxConsecutiveDays;
            _view.ScheduleMaxConsecutiveFull = schedule.MaxConsecutiveFull;
            _view.ScheduleMaxFullPerMonth = schedule.MaxFullPerMonth;

            _view.IsEdit = true;
            _view.ScheduleCancelTarget = (_view.ScheduleMode == ScheduleViewModel.Profile)
                ? ScheduleViewModel.Profile
                : ScheduleViewModel.List;

            _view.SwitchToScheduleEditMode();

        }

        private async Task OnScheduleSaveAsync(CancellationToken ct)
        {
            try
            {
                // 1. Беремо існуючу сутність при редагуванні, або нову при створенні
                ScheduleModel model;
                if (_view.IsEdit && _view.ScheduleId != 0)
                {
                    // EF вже трекає цей об’єкт, ми просто міняємо йому поля
                    model = (ScheduleModel?)_scheduleBinding.Current
                            ?? new ScheduleModel { Id = _view.ScheduleId };
                }
                else
                {
                    model = new ScheduleModel();
                }

                // 2. Копіюємо значення з форми в модель
                model.ContainerId = _view.ScheduleContainerId;
                model.ShopId = _view.ScheduleShopId;
                model.Name = _view.ScheduleName;
                model.Year = _view.ScheduleYear;
                model.Month = _view.ScheduleMonth;
                model.PeoplePerShift = _view.SchedulePeoplePerShift;
                model.Shift1Time = _view.ScheduleShift1;
                model.Shift2Time = _view.ScheduleShift2;
                model.MaxHoursPerEmpMonth = _view.ScheduleMaxHoursPerEmp;
                model.MaxConsecutiveDays = _view.ScheduleMaxConsecutiveDays;
                model.MaxConsecutiveFull = _view.ScheduleMaxConsecutiveFull;
                model.MaxFullPerMonth = _view.ScheduleMaxFullPerMonth;

                var errors = ValidateSchedule(model);
                if (errors.Count > 0)
                {
                    _view.SetScheduleValidationErrors(errors);
                    _view.IsSuccessful = false;
                    _view.Message = "Please fix the highlighted fields.";
                    return;
                }

                // 3. Працівники – беремо з того, що в гріді / після генерації,
                //    без навігації Employee (щоб EF не намагався інсертити employee ще раз)
                var employees = _view.ScheduleEmployees
                    .GroupBy(e => e.EmployeeId)
                    .Select(g => new ScheduleEmployeeModel
                    {
                        EmployeeId = g.Key,
                        MinHoursMonth = g.First().MinHoursMonth
                    })
                    .ToList();

                // 4. Слоти – просто зі view
                var slots = _view.ScheduleSlots;

                await _scheduleService.SaveWithDetailsAsync(model, employees, slots, ct);

                _view.ShowInfo(_view.IsEdit ? "Schedule updated successfully." : "Schedule added successfully.");
                _view.IsSuccessful = true;

                await LoadSchedules(model.ContainerId, null, ct);

                if (_view.ScheduleCancelTarget == ScheduleViewModel.Profile)
                    _view.SwitchToScheduleProfileMode();
                else
                    _view.SwitchToScheduleListMode();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                _view.ShowError(msg);
            }
        }

        private async Task OnScheduleDeleteAsync(CancellationToken ct)
        {
            try
            {
                var schedule = (ScheduleModel?)_scheduleBinding.Current;
                if (schedule is null) return;

                if (!_view.Confirm($"Delete schedule {schedule.Name}?"))
                    return;

                await _scheduleService.DeleteAsync(schedule.Id, ct);
                await LoadSchedules(schedule.ContainerId, null, ct);
                _view.ShowInfo("Schedule deleted successfully.");
                _view.SwitchToScheduleListMode();
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        private Task OnScheduleCancelAsync(CancellationToken ct)
        {
            _view.ClearScheduleValidationErrors();
            if (_view.ScheduleCancelTarget == ScheduleViewModel.Profile)
                _view.SwitchToScheduleProfileMode();
            else
                _view.SwitchToScheduleListMode();
            return Task.CompletedTask;
        }

        private async Task OnScheduleOpenProfileAsync(CancellationToken ct)
        {
            var schedule = (ScheduleModel?)_scheduleBinding.Current;
            if (schedule is null) return;

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;

            // Заповнюємо internal-списки у View (щоб при переході в Edit вони вже були)
            _view.ScheduleEmployees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            _view.ScheduleSlots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();

            _view.SetScheduleProfile(detailed);
            _view.ScheduleCancelTarget = ScheduleViewModel.List;
            _view.SwitchToScheduleProfileMode();
        }

        private async Task OnScheduleGenerateAsync(CancellationToken ct)
        {
            var model = new ScheduleModel
            {
                Id = _view.ScheduleId,
                ContainerId = _view.ScheduleContainerId,
                ShopId = _view.ScheduleShopId,
                Name = _view.ScheduleName,
                Year = _view.ScheduleYear,
                Month = _view.ScheduleMonth,
                PeoplePerShift = _view.SchedulePeoplePerShift,
                Shift1Time = _view.ScheduleShift1,
                Shift2Time = _view.ScheduleShift2,
                MaxHoursPerEmpMonth = _view.ScheduleMaxHoursPerEmp,
                MaxConsecutiveDays = _view.ScheduleMaxConsecutiveDays,
                MaxConsecutiveFull = _view.ScheduleMaxConsecutiveFull,
                MaxFullPerMonth = _view.ScheduleMaxFullPerMonth,
            };

            var errors = ValidateSchedule(model);
            if (errors.Count > 0)
            {
                _view.SetScheduleValidationErrors(errors);
                _view.ShowError("Please fix the highlighted fields.");
                return;
            }
            var selectedAvailabilities = await _availabilityService.GetAllAsync(ct);
            var selectedIds = _view.SelectedAvailabilityIds;
            var employees = selectedAvailabilities
                .Where(a => selectedIds.Contains(a.Id))
                .Select(a => new ScheduleEmployeeModel { EmployeeId = a.EmployeeId, Employee = a.Employee })
                .GroupBy(e => e.EmployeeId)
                .Select(g => g.First())
                .ToList();

            var slots = await _generator.GenerateAsync(model, selectedAvailabilities.Where(a => selectedIds.Contains(a.Id)), employees, ct);
            _view.ScheduleShift1 = model.Shift1Time;
            _view.ScheduleShift2 = model.Shift2Time;
            _view.ScheduleEmployees = employees;
            _view.ScheduleSlots = slots.ToList();
            _view.ShowInfo("Slots generated. Review before saving.");
        }

        private static Dictionary<string, string> ValidateContainer(ContainerModel model)
        {
            var errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(IContainerView.ContainerName)] = "Name is required.";
            return errors;
        }

        private static Dictionary<string, string> ValidateSchedule(ScheduleModel model)
        {
            var errors = new Dictionary<string, string>();
            if (model.ContainerId <= 0)
                errors[nameof(IContainerView.ScheduleContainerId)] = "Select a container.";
            if (model.ShopId <= 0)
                errors[nameof(IContainerView.ScheduleShopId)] = "Select a shop.";
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(IContainerView.ScheduleName)] = "Name is required.";
            if (model.Year < 1900)
                errors[nameof(IContainerView.ScheduleYear)] = "Year is invalid.";
            if (model.Month < 1 || model.Month > 12)
                errors[nameof(IContainerView.ScheduleMonth)] = "Month must be 1-12.";
            if (model.PeoplePerShift <= 0)
                errors[nameof(IContainerView.SchedulePeoplePerShift)] = "People per shift must be greater than zero.";
            if (string.IsNullOrWhiteSpace(model.Shift1Time))
            {
                errors[nameof(IContainerView.ScheduleShift1)] = "Shift1 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift1Time, out var s1, out var err1))
            {
                errors[nameof(IContainerView.ScheduleShift1)] = err1 ?? "Invalid shift1 format.";
            }
            else
            {
                model.Shift1Time = s1; // 👈 нормалізували
            }

            if (string.IsNullOrWhiteSpace(model.Shift2Time))
            {
                errors[nameof(IContainerView.ScheduleShift2)] = "Shift2 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift2Time, out var s2, out var err2))
            {
                errors[nameof(IContainerView.ScheduleShift2)] = err2 ?? "Invalid shift2 format.";
            }
            else
            {
                model.Shift2Time = s2; // 👈 нормалізували
            }
            if (model.MaxHoursPerEmpMonth <= 0)
                errors[nameof(IContainerView.ScheduleMaxHoursPerEmp)] = "Max hours per employee must be greater than zero.";
            return errors;
        }

        private static bool TryParseTime(string s, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(
                (s ?? "").Trim(),
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                out t);
        }

        private static bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)
        {
            normalized = "";
            error = null;

            input = (input ?? "").Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Shift is required.";
                return false;
            }

            // дозволяє: "09:00-15:00", "9:00-15:00", "09:00 - 15:00" і т.д.
            var parts = input.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                error = "Format: HH:mm-HH:mm (пробіли не важливі)";
                return false;
            }

            if (!TryParseTime(parts[0], out var from) || !TryParseTime(parts[1], out var to))
            {
                error = "Time must be H:mm або HH:mm (наприклад 9:00-15:00)";
                return false;
            }

            if (from >= to)
            {
                error = "From must be earlier than To";
                return false;
            }

            normalized = $"{from:hh\\:mm} - {to:hh\\:mm}";
            return true;
        }

    }
}
