/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Containers у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BusinessLogicLayer.Contracts.Models;
using System.Windows;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public async Task EnsureInitializedAsync(CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadContainersAsync(ct);
        }

        
        
        
        
        
        
        
        
        
        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;

            var list = string.IsNullOrWhiteSpace(term)
                ? await _containerService.GetAllAsync(ct)
                : await _containerService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        
        
        
        
        
        
        
        
        internal Task StartAddAsync(CancellationToken ct = default)
            => UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async _ =>
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.ResetForNew();
                        CancelTarget = ContainerSection.List;
                    });

                    await SwitchToEditAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);


        
        
        
        
        
        
        
        
        
        
        
        
        internal Task EditSelectedAsync(CancellationToken ct = default)
        {
            var id = GetCurrentContainerId();
            if (id <= 0)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _containerService.GetAsync(id, uiToken).ConfigureAwait(false);
                    if (latest is null)
                        return false;

                    await RunOnUiThreadAsync(() =>
                    {
                        EditVm.SetContainer(latest);

                        CancelTarget = Mode == ContainerSection.Profile
                            ? ContainerSection.Profile
                            : ContainerSection.List;
                    }).ConfigureAwait(false);

                    await SwitchToEditAsync().ConfigureAwait(false);
                    return true;
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);
        }


        
        
        
        
        
        
        
        
        
        
        
        internal Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = ValidateContainer(model);

            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return Task.CompletedTask;
            }

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    if (EditVm.IsEdit)
                    {
                        await _containerService.UpdateAsync(model, uiToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var created = await _containerService.CreateAsync(model, uiToken).ConfigureAwait(false);
                        await RunOnUiThreadAsync(() => EditVm.ContainerId = created.Id).ConfigureAwait(false);
                        model = created;
                    }

                    _databaseChangeNotifier.NotifyDatabaseChanged("Container.Save");
                    await LoadContainersAsync(uiToken, selectId: model.Id).ConfigureAwait(false);

                    if (CancelTarget == ContainerSection.Profile)
                    {
                        var profileId = _openedProfileContainerId ?? model.Id;
                        if (profileId > 0)
                        {
                            var latest = await _containerService.GetAsync(profileId, uiToken).ConfigureAwait(false) ?? model;
                            await SyncProfileAndSelectionAsync(latest).ConfigureAwait(false);
                        }

                        await SwitchToProfileAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await SwitchToListAsync().ConfigureAwait(false);
                    }
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }


        
        
        
        
        
        
        
        
        
        
        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            var currentId = GetCurrentContainerId();
            if (currentId <= 0) return;

            var currentName = Mode == ContainerSection.Profile
                ? ProfileVm.Name
                : ListVm.SelectedItem?.Name ?? string.Empty;

            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                ? "Delete container?"
                : $"Delete {currentName}?"))
            {
                return;
            }

            await UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _containerService.DeleteAsync(currentId, uiToken);

                    _databaseChangeNotifier.NotifyDatabaseChanged("Container.Delete");
                    await LoadContainersAsync(uiToken, selectId: null);
                    await SwitchToListAsync();
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        
        
        
        
        
        
        
        
        
        
        internal Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var latest = await _containerService.GetAsync(selected.Id, uiToken).ConfigureAwait(false) ?? selected;

                    _openedProfileContainerId = latest.Id;
                    await SyncProfileAndSelectionAsync(latest).ConfigureAwait(false);
                    await LoadSchedulesAsync(latest.Id, search: null, uiToken).ConfigureAwait(false);

                    CancelTarget = ContainerSection.List;
                    await SwitchToProfileAsync().ConfigureAwait(false);
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }




        
        
        

        
        
        
        
        private async Task LoadContainersAsync(CancellationToken ct, int? selectId = null)
        {
            var list = await _containerService.GetAllAsync(ct).ConfigureAwait(false);

            var disp = System.Windows.Application.Current?.Dispatcher;
            if (disp != null && !disp.CheckAccess())
            {
                await disp.InvokeAsync(() =>
                {
                    ListVm.SetItems(list);
                    if (selectId.HasValue)
                        ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == selectId.Value);
                });
            }
            else
            {
                ListVm.SetItems(list);
                if (selectId.HasValue)
                    ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == selectId.Value);
            }
        }

        
        
        
        
        private async Task LoadSchedulesAsync(int containerId, string? search, CancellationToken ct)
        {
            ClearScheduleDetailsCache();

            var schedules = await _scheduleService
                .GetByContainerAsync(containerId, search, ct)
                .ConfigureAwait(false);

            var disp = Application.Current?.Dispatcher;
            if (disp != null && !disp.CheckAccess())
            {
                await disp.InvokeAsync(() => ProfileVm.ScheduleListVm.SetItems(schedules));
            }
            else
            {
                ProfileVm.ScheduleListVm.SetItems(schedules);
            }
        }


        
        
        

        
        
        
        
        
        
        
        private static Dictionary<string, string> ValidateContainer(ContainerModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(ContainerEditViewModel.Name)] = "Name is required.";

            return errors;
        }
    }
}
