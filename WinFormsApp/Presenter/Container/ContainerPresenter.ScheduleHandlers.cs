using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinFormsApp.Presenter.Availability;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private async Task<IList<AvailabilityGroupModel>> LoadLookupsAsync(CancellationToken ct)
        {
            var groups = (await _availabilityGroupService.GetAllAsync(ct)).ToList();
            var shops = await _shopService.GetAllAsync(ct);
            var employees = await _employeeService.GetAllAsync(ct);

            _allAvailabilityGroups.Clear();
            _allAvailabilityGroups.AddRange(groups);

            _allShops.Clear();
            _allShops.AddRange(shops);

            _allEmployees.Clear();
            _allEmployees.AddRange(employees);

            _view.SetAvailabilityGroupList(groups);
            _view.SetShopList(shops);
            _view.SetEmployeeList(employees);

            return groups;
        }

        private async Task LoadSchedulesAsync(int containerId, string? search, CancellationToken ct)
        {
            _view.ScheduleContainerId = containerId;
            _scheduleBinding.DataSource = await _scheduleService.GetByContainerAsync(containerId, search, ct);
        }

        private async Task OnScheduleSearchCoreAsync(CancellationToken ct)
        {
            var container = CurrentContainerOrError();
            if (container is null) return;

            await LoadSchedulesAsync(container.Id, _view.ScheduleSearch, ct);
        }

        private async Task OnScheduleAddCoreAsync(CancellationToken ct)
        {
            var container = CurrentContainerOrError();
            if (container is null) return;

            await LoadLookupsAsync(ct);
            ResetScheduleEditFilters();

            _view.ClearScheduleValidationErrors();
            _view.ClearScheduleInputs();

            _view.IsEdit = false;
            _view.ScheduleContainerId = container.Id;
            _view.ScheduleCancelTarget = ScheduleViewModel.List;
            _view.SwitchToScheduleEditMode();
            _view.SetAddNewScheduleEnabled(true);
            ResetScheduleBlocks();
            _view.InitializeScheduleBlocks();

            var block = CreateDefaultBlock(container.Id);
            _scheduleBlocks.Add(block);
            _selectedScheduleBlockId = block.Id;
            _view.AddScheduleBlock(block.Id);
            _view.SetSelectedScheduleBlock(block.Id);
            ApplyBlockToView(block);
            _view.ClearAvailabilityPreviewMatrix();
            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task OnScheduleEditCoreAsync(CancellationToken ct)
        {
            var schedule = CurrentSchedule;
            var groups = await LoadLookupsAsync(ct);

            if (schedule is null) return;

            await LoadLookupsAsync(ct);
            ResetScheduleEditFilters();

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);

            _view.ClearScheduleValidationErrors();

            _view.IsEdit = true;
            _view.ScheduleCancelTarget = (_view.ScheduleMode == ScheduleViewModel.Profile)
                ? ScheduleViewModel.Profile
                : ScheduleViewModel.List;

            _view.SwitchToScheduleEditMode();
            _view.SetAddNewScheduleEnabled(false);
            ResetScheduleBlocks();
            _view.InitializeScheduleBlocks();

            var block = CreateBlockFromSchedule(schedule,
                detailed?.Employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                detailed?.Slots?.ToList() ?? new List<ScheduleSlotModel>());

            var availabilityId = groups
                .Where(g => g.Year == block.Model.Year && g.Month == block.Model.Month)
                .Select(g => g.Id)
                .FirstOrDefault();

            block.SelectedAvailabilityGroupId = availabilityId;

            _scheduleBlocks.Add(block);
            _selectedScheduleBlockId = block.Id;
            _view.AddScheduleBlock(block.Id);
            _view.SetSelectedScheduleBlock(block.Id);
            ApplyBlockToView(block);

            _view.ClearAvailabilityPreviewMatrix();
            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task OnScheduleSaveCoreAsync(CancellationToken ct)
        {
            CaptureSelectedBlockFromView();
            _view.ClearScheduleValidationErrors();
            if (_scheduleBlocks.Count == 0)
            {
                _view.ShowError("Add at least one schedule first.");
                return;
            }
            var invalidBlocks = new List<ScheduleBlockState>();

            foreach (var block in _scheduleBlocks)
            {
                var errors = ValidateAndNormalizeSchedule(block.Model, out var normalizedShift1, out var normalizedShift2);
                block.ValidationErrors = errors;

                if (errors.Count > 0)
                {
                    invalidBlocks.Add(block);
                    continue;
                }

                block.Model.Shift1Time = normalizedShift1!;
                block.Model.Shift2Time = normalizedShift2!;
            }

            if (invalidBlocks.Count > 0)
            {
                var first = invalidBlocks.First();
                _selectedScheduleBlockId = first.Id;
                _view.SetSelectedScheduleBlock(first.Id);
                ApplyBlockToView(first);
                _view.SetScheduleValidationErrors(first.ValidationErrors);
                _view.IsSuccessful = false;
                _view.Message = "Please fix the highlighted fields.";
                return;
            }

            var names = _scheduleBlocks
                .Select((block, index) =>
                {
                    var name = string.IsNullOrWhiteSpace(block.Model.Name)
                        ? $"Schedule {index + 1}"
                        : block.Model.Name;
                    return $"- {name}";
                })
                .ToList();

            var confirmMessage = $"Do you want to save these schedules?{Environment.NewLine}{string.Join(Environment.NewLine, names)}";
            if (!_view.Confirm(confirmMessage))
                return;

            foreach (var block in _scheduleBlocks)
            {
                var employees = (block.Employees ?? new List<ScheduleEmployeeModel>())
                    .GroupBy(e => e.EmployeeId)
                    .Select(g => new ScheduleEmployeeModel
                    {
                        EmployeeId = g.Key,
                        MinHoursMonth = g.First().MinHoursMonth
                    })
                    .ToList();

                var slots = block.Slots ?? new List<ScheduleSlotModel>();

                await _scheduleService.SaveWithDetailsAsync(block.Model, employees, slots, ct);
            }

            var containerId = _scheduleBlocks.FirstOrDefault()?.Model.ContainerId ?? _view.ScheduleContainerId;
            await LoadSchedulesAsync(containerId, search: null, ct);

            _view.ShowInfo("Schedules saved successfully.");
            _view.IsSuccessful = true;
        }

        private async Task OnScheduleDeleteCoreAsync(CancellationToken ct)
        {
            var schedule = CurrentSchedule;
            if (schedule is null) return;

            if (!_view.Confirm($"Delete schedule {schedule.Name}?"))
                return;

            await _scheduleService.DeleteAsync(schedule.Id, ct);

            await LoadSchedulesAsync(schedule.ContainerId, search: null, ct);

            _view.ShowInfo("Schedule deleted successfully.");
            _view.SwitchToScheduleListMode();
        }

        private Task OnScheduleCancelCoreAsync(CancellationToken ct)
        {
            ResetScheduleEditFilters();
            ResetScheduleBlocks();
            SwitchBackFromScheduleEdit();
            return Task.CompletedTask;
        }

        private Task OnScheduleShopSearchCoreAsync(CancellationToken ct)
        {
            ApplyShopFilter(_view.ScheduleShopSearchValue);
            return Task.CompletedTask;
        }

        private Task OnScheduleAvailabilitySearchCoreAsync(CancellationToken ct)
        {
            ApplyAvailabilityFilter(_view.ScheduleAvailabilitySearchValue);
            return Task.CompletedTask;
        }

        private Task OnScheduleEmployeeSearchCoreAsync(CancellationToken ct)
        {
            ApplyEmployeeFilter(_view.ScheduleEmployeeSearchValue);
            return Task.CompletedTask;
        }

        private Task OnScheduleAddEmployeeToGroupCoreAsync(CancellationToken ct)
        {
            var empId = _view.ScheduleEmployeeId;
            if (empId <= 0)
            {
                _view.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            var employee = _allEmployees.FirstOrDefault(e => e.Id == empId);
            if (employee is null)
            {
                _view.ShowError("Selected employee not found.");
                return Task.CompletedTask;
            }

            var employees = _view.ScheduleEmployees?.ToList() ?? new List<ScheduleEmployeeModel>();
            if (employees.Any(e => e.EmployeeId == empId))
            {
                _view.ShowInfo("This employee is already added.");
                return Task.CompletedTask;
            }

            employees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = empId,
                Employee = employee
            });

            _view.ScheduleEmployees = employees;
            CaptureSelectedBlockFromView();
            return Task.CompletedTask;
        }

        private Task OnScheduleRemoveEmployeeFromGroupCoreAsync(CancellationToken ct)
        {
            var empId = _view.ScheduleEmployeeId;
            if (empId <= 0)
            {
                _view.ShowError("Select employee first.");
                return Task.CompletedTask;
            }

            var employees = _view.ScheduleEmployees?.ToList() ?? new List<ScheduleEmployeeModel>();
            if (employees.RemoveAll(e => e.EmployeeId == empId) == 0)
            {
                _view.ShowInfo("This employee is not in the group.");
                return Task.CompletedTask;
            }

            var slots = _view.ScheduleSlots?.ToList() ?? new List<ScheduleSlotModel>();
            slots.RemoveAll(s => s.EmployeeId == empId);

            _view.ScheduleEmployees = employees;
            _view.ScheduleSlots = slots;
            CaptureSelectedBlockFromView();
            return Task.CompletedTask;
        }

        private async Task OnScheduleOpenProfileCoreAsync(CancellationToken ct)
        {
            var schedule = CurrentSchedule;
            if (schedule is null) return;

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;

            _view.ScheduleEmployees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            _view.ScheduleSlots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();

            _view.SetScheduleProfile(detailed);
            _view.ScheduleCancelTarget = ScheduleViewModel.List;
            _view.SwitchToScheduleProfileMode();
        }

        private async Task OnScheduleGenerateCoreAsync(CancellationToken ct)
        {
            var model = BuildScheduleFromView();

            var errors = ValidateAndNormalizeSchedule(model, out var normalizedShift1, out var normalizedShift2);
            if (errors.Count > 0)
            {
                _view.SetScheduleValidationErrors(errors);
                _view.ShowError("Please fix the highlighted fields.");
                return;
            }

            model.Shift1Time = normalizedShift1!;
            model.Shift2Time = normalizedShift2!;

            var selectedGroupId = _view.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                _view.ShowError("Select an availability group.");
                return;
            }

            await LoadLookupsAsync(ct);

            var fullGroups = new List<AvailabilityGroupModel>(capacity: 1);
            var employees = new List<ScheduleEmployeeModel>();
            var seenEmp = new HashSet<int>();

            foreach (var groupId in new[] { selectedGroupId })
            {
                ct.ThrowIfCancellationRequested();

                var (group, members, days) = await _availabilityGroupService.LoadFullAsync(groupId, ct);

                if (group.Year != model.Year || group.Month != model.Month)
                    continue;

                var daysByMember = days
                    .GroupBy(d => d.AvailabilityGroupMemberId)
                    .ToDictionary(
                        g => g.Key,
                        g => (ICollection<AvailabilityGroupDayModel>)g.ToList());

                foreach (var m in members)
                {
                    m.Days = daysByMember.TryGetValue(m.Id, out var list)
                        ? list
                        : new List<AvailabilityGroupDayModel>();

                    if (seenEmp.Add(m.EmployeeId))
                    {
                        employees.Add(new ScheduleEmployeeModel
                        {
                            EmployeeId = m.EmployeeId,
                            Employee = m.Employee
                        });
                    }
                }

                group.Members = members;
                fullGroups.Add(group);
            }

            if (fullGroups.Count == 0 || employees.Count == 0)
            {
                _view.ShowError("No employees/availability found for selected groups for this month.");
                return;
            }

            var slots = await _generator.GenerateAsync(model, fullGroups, employees, ct);

            _view.ScheduleShift1 = model.Shift1Time;
            _view.ScheduleShift2 = model.Shift2Time;
            _view.ScheduleEmployees = employees;
            _view.ScheduleSlots = slots.ToList();
            CaptureSelectedBlockFromView();

            _view.ShowInfo("Slots generated. Review before saving.");
        }

        private Task OnAvailabilitySelectionChangedCoreAsync(CancellationToken ct)
        {
            CaptureSelectedBlockFromView();
            return UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task UpdateAvailabilityPreviewCoreAsync(CancellationToken ct)
        {
            var year = _view.ScheduleYear;
            var month = _view.ScheduleMonth;

            var selectedGroupId = _view.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                _view.ClearAvailabilityPreviewMatrix();
                return;
            }

            // 1) Підготуємо shift1/shift2 як (from,to), щоб показувати ANY
            (string from, string to)? shift1 = TrySplitShift(_view.ScheduleShift1);
            (string from, string to)? shift2 = TrySplitShift(_view.ScheduleShift2);

            var employees = new List<ScheduleEmployeeModel>();
            var slots = new List<ScheduleSlotModel>();
            var seenEmp = new HashSet<int>();
            var seenSlot = new HashSet<string>(); // щоб не дублювати інтервали між групами

            foreach (var groupId in new[] { selectedGroupId })
            {
                ct.ThrowIfCancellationRequested();

                var (group, members, days) = await _availabilityGroupService.LoadFullAsync(groupId, ct);

                // тільки потрібний місяць/рік
                if (group.Year != year || group.Month != month)
                    continue;

                var daysByMember = days
                    .GroupBy(d => d.AvailabilityGroupMemberId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var m in members)
                {
                    // employees для заголовків колонок (важливо щоб Employee був підвантажений, бо BuildScheduleTable бере FirstName/LastName)
                    if (seenEmp.Add(m.EmployeeId))
                    {
                        employees.Add(new ScheduleEmployeeModel
                        {
                            EmployeeId = m.EmployeeId,
                            Employee = m.Employee
                        });
                    }

                    if (!daysByMember.TryGetValue(m.Id, out var mdays) || mdays.Count == 0)
                        continue;

                    foreach (var d in mdays)
                    {
                        // NONE -> пусто
                        if (d.Kind == AvailabilityKind.NONE)
                            continue;

                        // INT -> 1 інтервал з IntervalStr
                        if (d.Kind == AvailabilityKind.INT)
                        {
                            if (string.IsNullOrWhiteSpace(d.IntervalStr))
                                continue;

                            // на всякий випадок проганяємо нормалізацію
                            if (!AvailabilityCodeParser.TryNormalizeInterval(d.IntervalStr, out var normalized))
                                continue;

                            if (TrySplitInterval(normalized, out var from, out var to))
                                AddSlotUnique(m.EmployeeId, d.DayOfMonth, from, to);

                            continue;
                        }

                        // ANY -> показуємо shift1 + shift2 (якщо вони валідні)
                        if (d.Kind == AvailabilityKind.ANY)
                        {
                            if (shift1 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift1.Value.from, shift1.Value.to);
                            if (shift2 != null) AddSlotUnique(m.EmployeeId, d.DayOfMonth, shift2.Value.from, shift2.Value.to);
                        }
                    }
                }
            }

            _view.SetAvailabilityPreviewMatrix(year, month, slots, employees);

            // ---------- local helpers ----------
            void AddSlotUnique(int empId, int day, string from, string to)
            {
                var key = $"{empId}|{day}|{from}|{to}";
                if (!seenSlot.Add(key)) return;

                slots.Add(new ScheduleSlotModel
                {
                    EmployeeId = empId,
                    DayOfMonth = day,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = 1,
                    Status = SlotStatus.UNFURNISHED
                });
            }

            static bool TrySplitInterval(string normalized, out string from, out string to)
            {
                from = to = "";
                var parts = normalized.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2) return false;
                from = parts[0];
                to = parts[1];
                return true;
            }

            // Використовує твою ж логіку нормалізації shift-ів з ContainerPresenter.Validation :contentReference[oaicite:8]{index=8}
            (string from, string to)? TrySplitShift(string rawShift)
            {
                if (!TryNormalizeShiftRange(rawShift, out var normalized, out _))
                    return null;

                return TrySplitInterval(normalized, out var f, out var t) ? (f, t) : null;
            }
        }

        private void ApplyShopFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                _view.SetShopList(_allShops);
                return;
            }

            var filtered = _allShops
                .Where(s => ContainsIgnoreCase(s.Name, term))
                .ToList();

            _view.SetShopList(filtered);
        }

        private void ApplyAvailabilityFilter(string? raw)
        {
            var term = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(term))
            {
                _view.SetAvailabilityGroupList(_allAvailabilityGroups);
                return;
            }

            var filtered = _allAvailabilityGroups
                .Where(g => ContainsIgnoreCase(g.Name, term))
                .ToList();

            _view.SetAvailabilityGroupList(filtered);
        }

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

        private void ResetScheduleEditFilters()
        {
            _view.ScheduleShopSearchValue = string.Empty;
            _view.ScheduleAvailabilitySearchValue = string.Empty;
            _view.ScheduleEmployeeSearchValue = string.Empty;

            _view.SetShopList(_allShops);
            _view.SetAvailabilityGroupList(_allAvailabilityGroups);
            _view.SetEmployeeList(_allEmployees);
        }

        private static bool ContainsIgnoreCase(string? source, string value)
            => (source ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase);

    }
}
