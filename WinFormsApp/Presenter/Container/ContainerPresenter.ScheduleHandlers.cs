using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.Presenter.Availability;
using WinFormsApp.ViewModel;
using WinFormsApp.Presenter.Availability;
using DataAccessLayer.Models.Enums;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private async Task<IList<AvailabilityGroupModel>> LoadLookupsAsync(CancellationToken ct)
        {
            var groups = (await _availabilityGroupService.GetAllAsync(ct)).ToList();
            _view.SetAvailabilityGroupList(groups);
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

            _view.ClearScheduleValidationErrors();
            _view.ClearScheduleInputs();

            _view.IsEdit = false;
            _view.ScheduleContainerId = container.Id;
            _view.ScheduleCancelTarget = ScheduleViewModel.List;
            _view.SwitchToScheduleEditMode();
            _view.ClearAvailabilityPreviewMatrix();
            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task OnScheduleEditCoreAsync(CancellationToken ct)
        {
            var schedule = CurrentSchedule;
            var groups = await LoadLookupsAsync(ct);

            if (schedule is null) return;

            await LoadLookupsAsync(ct);

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);

            _view.ScheduleEmployees = detailed?.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            _view.ScheduleSlots = detailed?.Slots?.ToList() ?? new List<ScheduleSlotModel>();

            _view.ClearScheduleValidationErrors();

            _view.ScheduleId = schedule.Id;
            _view.ScheduleContainerId = schedule.ContainerId;
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
            _view.ScheduleNote = schedule.Note ?? string.Empty;

            _view.IsEdit = true;
            _view.ScheduleCancelTarget = (_view.ScheduleMode == ScheduleViewModel.Profile)
                ? ScheduleViewModel.Profile
                : ScheduleViewModel.List;

            var idsToCheck = groups
                .Where(g => g.Year == _view.ScheduleYear && g.Month == _view.ScheduleMonth)
                .Select(g => g.Id)
                .ToList();

            _view.SetCheckedAvailabilityGroupIds(idsToCheck);

            _view.SwitchToScheduleEditMode();

            _view.ClearAvailabilityPreviewMatrix();
            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task OnScheduleSaveCoreAsync(CancellationToken ct)
        {
            _view.ClearScheduleValidationErrors();
            var model = BuildScheduleFromView();

            var errors = ValidateAndNormalizeSchedule(model, out var normalizedShift1, out var normalizedShift2);
            if (errors.Count > 0)
            {
                _view.SetScheduleValidationErrors(errors);
                _view.IsSuccessful = false;
                _view.Message = "Please fix the highlighted fields.";
                return;
            }

            model.Shift1Time = normalizedShift1!;
            model.Shift2Time = normalizedShift2!;

            var employees = (_view.ScheduleEmployees ?? new List<ScheduleEmployeeModel>())
                .GroupBy(e => e.EmployeeId)
                .Select(g => new ScheduleEmployeeModel
                {
                    EmployeeId = g.Key,
                    MinHoursMonth = g.First().MinHoursMonth
                })
                .ToList();

            var slots = _view.ScheduleSlots ?? new List<ScheduleSlotModel>();

            await _scheduleService.SaveWithDetailsAsync(model, employees, slots, ct);

            _view.ShowInfo(_view.IsEdit ? "Schedule updated successfully." : "Schedule added successfully.");
            _view.IsSuccessful = true;

            await LoadSchedulesAsync(model.ContainerId, search: null, ct);
            SwitchBackFromScheduleEdit();
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
            SwitchBackFromScheduleEdit();
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

            var selectedGroupIds = _view.SelectedAvailabilityGroupIds;
            if (selectedGroupIds is null || selectedGroupIds.Count == 0)
            {
                _view.ShowError("Select at least one availability group.");
                return;
            }

            await LoadLookupsAsync(ct);

            var fullGroups = new List<AvailabilityGroupModel>(selectedGroupIds.Count);
            var employees = new List<ScheduleEmployeeModel>();
            var seenEmp = new HashSet<int>();

            foreach (var groupId in selectedGroupIds)
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

            _view.ShowInfo("Slots generated. Review before saving.");
        }

        private Task OnAvailabilitySelectionChangedCoreAsync(CancellationToken ct)
            => UpdateAvailabilityPreviewCoreAsync(ct);

        private async Task UpdateAvailabilityPreviewCoreAsync(CancellationToken ct)
        {
            var year = _view.ScheduleYear;
            var month = _view.ScheduleMonth;

            var selectedGroupIds = _view.SelectedAvailabilityGroupIds;
            if (selectedGroupIds is null || selectedGroupIds.Count == 0)
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

            foreach (var groupId in selectedGroupIds.Distinct())
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

    }
}
