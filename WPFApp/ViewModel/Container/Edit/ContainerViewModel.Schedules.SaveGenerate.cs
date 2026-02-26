/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Schedules.SaveGenerate у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;


using ScheduleBlockVm = WPFApp.ViewModel.Container.ScheduleEdit.Helpers.ScheduleBlockViewModel;


namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal async Task SaveScheduleAsync(CancellationToken ct = default)
        {
            ScheduleEditVm.ClearValidationErrors();

            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ShowError("Add at least one schedule first.");
                return;
            }

            
            var invalidBlocks = new List<(ScheduleBlockVm Block, Dictionary<string, string> Errors)>();

            foreach (ScheduleBlockVm block in ScheduleEditVm.Blocks)
            {
                var errors = ValidateAndNormalizeSchedule(block.Model, out var normalizedShift1, out var normalizedShift2);

                if (errors.Count > 0)
                {
                    invalidBlocks.Add((Block: block, Errors: errors));
                    continue;
                }

                block.Model.Shift1Time = normalizedShift1!;
                block.Model.Shift2Time = normalizedShift2!;
            }

            
            if (invalidBlocks.Count > 0)
            {
                var first = invalidBlocks[0];

                ScheduleEditVm.SelectedBlock = first.Block;
                ScheduleEditVm.SetValidationErrors(first.Errors);

                ShowError(BuildScheduleValidationSummary(invalidBlocks));
                return;
            }

            
            var missingGenerated = ScheduleEditVm.Blocks.Where(block => !HasGeneratedContent(block)).ToList();
            if (missingGenerated.Count > 0)
            {
                var first = missingGenerated.First();
                ScheduleEditVm.SelectedBlock = first;
                ShowError("You can’t save a schedule until something has been generated. Please run generation first.");
                return;
            }

            
            var names = ScheduleEditVm.Blocks
                .Select((block, index) =>
                {
                    var name = string.IsNullOrWhiteSpace(block.Model.Name)
                        ? $"Schedule {index + 1}"
                        : block.Model.Name;
                    return $"- {name}";
                })
                .ToList();

            var confirmMessage =
                $"Do you want to save these schedules?{Environment.NewLine}{string.Join(Environment.NewLine, names)}";

            if (!Confirm(confirmMessage))
                return;

            
            var uiToken = ResetSaveUiCts(ct);
            await ShowSaveWorkingAsync(); 

            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { },
                System.Windows.Threading.DispatcherPriority.ApplicationIdle
            );


            try
            {
                
                foreach (var block in ScheduleEditVm.Blocks)
                {
                    
                    var employees = block.Employees
                        .GroupBy(e => e.EmployeeId)
                        .Select(g => new ScheduleEmployeeModel
                        {
                            EmployeeId = g.Key,
                            MinHoursMonth = g.First().MinHoursMonth
                        })
                        .ToList();

                    var slots = block.Slots.ToList();

                    foreach (var s in slots)
                    {
                        
                        if (s.EmployeeId is int id && id == 0)
                            s.EmployeeId = null;

                        
                        
                        
                        s.Status = s.EmployeeId == null ? SlotStatus.UNFURNISHED : SlotStatus.ASSIGNED;

                        
                        s.FromTime = NormalizeHHmm(s.FromTime);
                        s.ToTime = NormalizeHHmm(s.ToTime);

                        
                        if (string.CompareOrdinal(s.FromTime, s.ToTime) >= 0)
                            throw new InvalidOperationException(
                                $"Invalid time range: day={s.DayOfMonth}, slot={s.SlotNo}, {s.FromTime}-{s.ToTime}");
                    }

                    
                    var cellStyles = block.CellStyles
                        .Where(cs => cs.BackgroundColorArgb.HasValue || cs.TextColorArgb.HasValue)
                        .ToList();

                    
                    block.Model.AvailabilityGroupId = block.SelectedAvailabilityGroupId > 0
                        ? block.SelectedAvailabilityGroupId
                        : null;

                    
                    await _scheduleService
                        .SaveWithDetailsAsync(block.Model, employees, slots, cellStyles, uiToken)
                        .ConfigureAwait(false);
                }

                
                var containerId = ScheduleEditVm.Blocks.FirstOrDefault()?.Model.ContainerId ?? GetCurrentContainerId();
                if (containerId > 0)
                    await LoadSchedulesAsync(containerId, search: null, uiToken).ConfigureAwait(false);

                _databaseChangeNotifier.NotifyDatabaseChanged("Container.ScheduleSave");

                
                await ShowSaveSuccessThenAutoHideAsync(uiToken, 1400).ConfigureAwait(false);

                
                

                
                if (ScheduleEditVm.IsEdit)
                {
                    var savedBlock = ScheduleEditVm.Blocks.FirstOrDefault();
                    if (savedBlock != null)
                    {
                        var detailed = await _scheduleService.GetDetailedAsync(savedBlock.Model.Id, uiToken)
                                                             .ConfigureAwait(false);
                        if (detailed != null)
                        {
                            var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                            var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                            var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

                            await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, uiToken)
                                                   .ConfigureAwait(false);

                            ProfileVm.ScheduleListVm.SelectedItem =
                                ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);
                        }
                    }

                    ScheduleCancelTarget = ContainerSection.Profile;
                    await SwitchToScheduleProfileAsync().ConfigureAwait(false);
                    return;
                }

                
                await SwitchToProfileAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await HideSaveStatusAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                await HideSaveStatusAsync().ConfigureAwait(false);
                ShowError(ex);
                return;
            }

            
            static string NormalizeHHmm(string value)
            {
                if (TimeSpan.TryParse(value, out var ts))
                    return ts.ToString(@"hh\:mm");
                return value;
            }
        }

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal async Task GenerateScheduleAsync(CancellationToken ct = default)
        {
            await RunOnUiThreadAsync(() => Keyboard.ClearFocus());

            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;
            var model = block.Model;

            var errors = ValidateAndNormalizeSchedule(model, out var normalizedShift1, out var normalizedShift2);
            if (errors.Count > 0)
            {
                ScheduleEditVm.SetValidationErrors(errors);
                ShowError("Please fix the highlighted fields.");
                return;
            }

            model.Shift1Time = normalizedShift1!;
            model.Shift2Time = normalizedShift2!;

            var selectedGroupId = block.SelectedAvailabilityGroupId;
            if (selectedGroupId <= 0)
            {
                ShowError("Select an availability group.");
                return;
            }

            
            model.AvailabilityGroupId = selectedGroupId;

            
            await SyncEmployeesFromAvailabilityGroupAsync(selectedGroupId, ct);

            
            
            var loaded = await _availabilityGroupService.LoadFullAsync(selectedGroupId, ct).ConfigureAwait(false);
            var group = loaded.Item1;
            var members = loaded.Item2 ?? new List<AvailabilityGroupMemberModel>();
            var days = loaded.Item3 ?? new List<AvailabilityGroupDayModel>();

            if (group.Year != model.Year || group.Month != model.Month)
            {
                ShowError("Selected availability group is for a different month/year.");
                return;
            }

            
            var daysByMember = days
                .GroupBy(d => d.AvailabilityGroupMemberId)
                .ToDictionary(g => g.Key, g => (ICollection<AvailabilityGroupDayModel>)g.ToList());

            foreach (var m in members)
            {
                m.Days = daysByMember.TryGetValue(m.Id, out var list)
                    ? list
                    : new List<AvailabilityGroupDayModel>();
            }

            group.Members = members;

            
            
            var memberEmpIds = members.Select(m => m.EmployeeId).Distinct().ToHashSet();

            var employees = block.Employees
                .Where(e => memberEmpIds.Contains(e.EmployeeId))
                .GroupBy(e => e.EmployeeId)
                .Select(g =>
                {
                    var first = g.First();
                    return new ScheduleEmployeeModel
                    {
                        EmployeeId = first.EmployeeId,
                        Employee = first.Employee,          
                        MinHoursMonth = first.MinHoursMonth 
                    };
                })
                .ToList();

            if (employees.Count == 0)
            {
                ShowError("No employees found for selected availability group.");
                return;
            }

            var fullGroups = new List<AvailabilityGroupModel>(capacity: 1) { group };

            
            var slots = await _generator.GenerateAsync(model, fullGroups, employees, progress: null, ct: ct)
                       ?? new List<ScheduleSlotModel>();

            
            block.Slots.Clear();
            foreach (var slot in slots)
                block.Slots.Add(slot);

            
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            ShowInfo("Slots generated. Review before saving.");
        }

        
        
        

        
        
        
        
        
        
        
        
        internal async Task SyncEmployeesFromAvailabilityGroupAsync(int groupId, CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;

            var loaded = await _availabilityGroupService.LoadFullAsync(groupId, ct).ConfigureAwait(false);
            var members = loaded.Item2 ?? new List<AvailabilityGroupMemberModel>();

            if (ct.IsCancellationRequested)
                return;

            
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            
            var memberByEmpId = new Dictionary<int, AvailabilityGroupMemberModel>(capacity: Math.Max(16, members.Count));
            foreach (var m in members)
            {
                var empId = m.EmployeeId;

                if (memberByEmpId.TryGetValue(empId, out var existing))
                {
                    if (existing.Employee == null && m.Employee != null)
                        memberByEmpId[empId] = m;
                }
                else
                {
                    memberByEmpId[empId] = m;
                }
            }

            bool changed = false;

            await RunOnUiThreadAsync(() =>
            {
                
                if (!ScheduleEditVm.Blocks.Contains(block))
                    return;

                
                var oldMin = new Dictionary<int, int?>();
                foreach (var e in block.Employees)
                {
                    if (!oldMin.ContainsKey(e.EmployeeId))
                        oldMin[e.EmployeeId] = e.MinHoursMonth;
                }

                
                var existingById = new Dictionary<int, ScheduleEmployeeModel>();
                foreach (var e in block.Employees)
                {
                    if (!existingById.ContainsKey(e.EmployeeId))
                        existingById[e.EmployeeId] = e;
                }

                
                for (int i = block.Employees.Count - 1; i >= 0; i--)
                {
                    var e = block.Employees[i];
                    if (!memberByEmpId.ContainsKey(e.EmployeeId))
                    {
                        block.Employees.RemoveAt(i);
                        changed = true;
                    }
                }

                
                foreach (var kv in memberByEmpId)
                {
                    var empId = kv.Key;
                    var m = kv.Value;

                    if (existingById.TryGetValue(empId, out var existing))
                    {
                        
                        if (existing.Employee == null && m.Employee != null)
                        {
                            existing.Employee = m.Employee;
                            changed = true;
                        }
                        continue;
                    }

                    oldMin.TryGetValue(empId, out var min);

                    block.Employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = empId,
                        Employee = m.Employee,
                        MinHoursMonth = min
                    });

                    changed = true;
                }
            }).ConfigureAwait(false);

            
            if (changed && !ct.IsCancellationRequested && ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
            {
                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct).ConfigureAwait(false);
            }
        }

        

        private CancellationTokenSource? _saveUiCts;

        private CancellationToken ResetSaveUiCts(CancellationToken outer)
        {
            _saveUiCts?.Cancel();
            _saveUiCts?.Dispose();
            _saveUiCts = CancellationTokenSource.CreateLinkedTokenSource(outer);
            return _saveUiCts.Token;
        }

        private Task ShowSaveWorkingAsync()
            => RunOnUiThreadAsync(() =>
            {
                ScheduleEditVm.SaveStatus = UIStatusKind.Working;
                ScheduleEditVm.IsSaveStatusVisible = true;
            });

        private Task HideSaveStatusAsync()
            => RunOnUiThreadAsync(() => ScheduleEditVm.IsSaveStatusVisible = false);

        private async Task ShowSaveSuccessThenAutoHideAsync(CancellationToken ct, int ms = 1400)
        {
            await RunOnUiThreadAsync(() =>
            {
                ScheduleEditVm.SaveStatus = UIStatusKind.Success;
                ScheduleEditVm.IsSaveStatusVisible = true;
            }).ConfigureAwait(false);

            try { await Task.Delay(ms, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }

            await HideSaveStatusAsync().ConfigureAwait(false);
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        private static Dictionary<string, string> ValidateAndNormalizeSchedule(
            ScheduleModel model,
            out string? normalizedShift1,
            out string? normalizedShift2)
        {
            var errors = new Dictionary<string, string>();
            normalizedShift1 = null;
            normalizedShift2 = null;

            if (model.ContainerId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleContainerId)] = "Select a container.";
            if (model.ShopId <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShopId)] = "Select a shop.";
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerScheduleEditViewModel.ScheduleName)] = "Name is required.";
            if (model.Year < 1900)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleYear)] = "Year is invalid.";
            if (model.Month < 1 || model.Month > 12)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMonth)] = "Month must be 1-12.";
            if (model.PeoplePerShift <= 0)
                errors[nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift)] = "People per shift must be greater than zero.";
            if (model.MaxHoursPerEmpMonth <= 0)
                errors[nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp)] = "Max hours per employee must be greater than zero.";

            
            if (string.IsNullOrWhiteSpace(model.Shift1Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = "Shift1 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift1Time, out normalizedShift1, out var err1))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift1)] = err1 ?? "Invalid shift1 format.";
            }

            
            if (string.IsNullOrWhiteSpace(model.Shift2Time))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = "Shift2 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift2Time, out normalizedShift2, out var err2))
            {
                errors[nameof(ContainerScheduleEditViewModel.ScheduleShift2)] = err2 ?? "Invalid shift2 format.";
            }

            
            model.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

            return errors;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        private static bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)
        {
            normalized = string.Empty;
            error = null;

            input = (input ?? string.Empty).Trim();
            if (input.Length == 0)
            {
                error = "Shift is required.";
                return false;
            }

            
            if (!AvailabilityCodeParser.TryNormalizeInterval(input, out var normalizedCandidate))
            {
                error = "Shift format must be: HH:mm-HH:mm.";
                return false;
            }

            var parts = normalizedCandidate.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "Shift format must be: HH:mm-HH:mm.";
                return false;
            }

            
            if (!TimeSpan.TryParseExact(parts[0], @"hh\:mm", CultureInfo.InvariantCulture, out var from) ||
                !TimeSpan.TryParseExact(parts[1], @"hh\:mm", CultureInfo.InvariantCulture, out var to))
            {
                error = "Shift time must be HH:mm.";
                return false;
            }

            
            if (from < TimeSpan.Zero || from >= TimeSpan.FromHours(24) ||
                to < TimeSpan.Zero || to >= TimeSpan.FromHours(24))
            {
                error = "Shift time must be within 00:00..23:59 (24:00 is not allowed).";
                return false;
            }

            if (to <= from)
            {
                error = "Shift end must be later than shift start.";
                return false;
            }

            normalized = $"{from:hh\\:mm} - {to:hh\\:mm}";
            return true;

        }

        
        
        
        
        
        private string BuildScheduleValidationSummary(
            IReadOnlyCollection<(ScheduleBlockVm Block, Dictionary<string, string> Errors)> invalidBlocks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fix the following schedule errors before saving:");

            foreach (var item in invalidBlocks)
            {
                var block = item.Block;
                var errors = item.Errors;

                var index = ScheduleEditVm.Blocks.IndexOf(block);
                var displayIndex = index >= 0 ? index + 1 : 0;

                var header = displayIndex > 0 ? $"Schedule #{displayIndex}" : "Schedule";

                var name = string.IsNullOrWhiteSpace(block.Model.Name) ? null : block.Model.Name.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    header = $"{header} \"{name}\"";

                foreach (var kv in errors)
                {
                    var label = GetScheduleFieldLabel(kv.Key);
                    sb.AppendLine($"- {header}: {label} — {kv.Value}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        
        
        
        
        private static string GetScheduleFieldLabel(string propertyName)
        {
            return propertyName switch
            {
                nameof(ContainerScheduleEditViewModel.ScheduleContainerId) => "Container",
                nameof(ContainerScheduleEditViewModel.ScheduleShopId) => "Shop",
                nameof(ContainerScheduleEditViewModel.ScheduleName) => "Name",
                nameof(ContainerScheduleEditViewModel.ScheduleYear) => "Year",
                nameof(ContainerScheduleEditViewModel.ScheduleMonth) => "Month",
                nameof(ContainerScheduleEditViewModel.SchedulePeoplePerShift) => "People per shift",
                nameof(ContainerScheduleEditViewModel.ScheduleMaxHoursPerEmp) => "Max hours per employee",
                nameof(ContainerScheduleEditViewModel.ScheduleShift1) => "Shift 1",
                nameof(ContainerScheduleEditViewModel.ScheduleShift2) => "Shift 2",
                _ => propertyName
            };
        }
    }
}
