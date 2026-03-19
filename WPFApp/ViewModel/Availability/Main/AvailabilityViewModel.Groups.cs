/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.Groups у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal async Task SearchAsync(CancellationToken ct = default)
        {
            
            var term = ListVm.SearchText;

            
            var list = string.IsNullOrWhiteSpace(term)
                ? await _availabilityService.GetAllAsync(ct)
                : await _availabilityService.GetByValueAsync(term, ct);

            
            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
            => UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await LoadEmployeesAsync(uiToken);

                    await RunOnUiThreadAsync(() =>
                    {
                        ResetEmployeeSearch();
                        EditVm.ResetForNew();
                        CancelTarget = AvailabilitySection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await LoadEmployeesAsync(uiToken);

                    var (group, members, days) = await _availabilityService.LoadFullAsync(selected.Id, uiToken);

                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.LoadGroup(group, members, days, _employeeNames);

                        CancelTarget = Mode == AvailabilitySection.Profile
                            ? AvailabilitySection.Profile
                            : AvailabilitySection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var rawName = (EditVm.AvailabilityName ?? string.Empty).Trim();

            
            var isNew = EditVm.AvailabilityId == 0;

            
            var suffix = $"{EditVm.AvailabilityMonth:D2}.{EditVm.AvailabilityYear}";

            
            var finalName = isNew
                ? $"{rawName} : {suffix}"
                : rawName;

            
            var group = new AvailabilityGroupModel
            {
                Id = EditVm.AvailabilityId,
                Name = finalName,
                Year = EditVm.AvailabilityYear,
                Month = EditVm.AvailabilityMonth
            };

            
            var errors = AvailabilityGroupValidator.Validate(group);

            
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                EditVm.ShowValidationErrorsDialog(errors); 
                return;
            }

            
            var selectedEmployees = EditVm.GetSelectedEmployeeIds();
            if (selectedEmployees.Count == 0)
            {
                ShowError("Add at least 1 employee to the group.");
                return;
            }

            
            var selectedSet = selectedEmployees.ToHashSet();

            
            var raw = EditVm.ReadGroupCodes()
                .Where(x => selectedSet.Contains(x.employeeId));

            
            if (!AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var err))
            {
                ShowError(err ?? "Invalid availability codes.");
                return;
            }

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _availabilityService.SaveGroupAsync(group, payload, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Availability.Save");

                    await LoadAllGroupsAsync(uiToken);

                    if (CancelTarget == AvailabilitySection.Profile)
                    {
                        var profileId = _openedProfileGroupId ?? group.Id;

                        if (profileId > 0)
                        {
                            var (g, members, days) = await _availabilityService.LoadFullAsync(profileId, uiToken);
                            await RunOnUiThreadAsync(() => ProfileVm.SetProfile(g, members, days));
                        }

                        await SwitchToProfileAsync();
                    }
                    else
                    {
                        await SwitchToListAsync();
                    }
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            
            if (!Confirm($"Delete '{current.Name}' ?"))
                return;

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _availabilityService.DeleteAsync(current.Id, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Availability.Delete");
                    await LoadAllGroupsAsync(uiToken);
                    await SwitchToListAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var (group, members, days) = await _availabilityService.LoadFullAsync(current.Id, uiToken);

                    await RunOnUiThreadAsync(() =>
                    {
                        _openedProfileGroupId = current.Id;
                        ProfileVm.SetProfile(group, members, days);
                        CancelTarget = AvailabilitySection.List;
                    });

                    await SwitchToProfileAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        internal Task CancelAsync()
        {
            
            EditVm.ClearValidationErrors();

            
            ResetEmployeeSearch();

            
            return Mode switch
            {
                AvailabilitySection.Edit => CancelTarget == AvailabilitySection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),

                _ => SwitchToListAsync()
            };
        }
    }
}
