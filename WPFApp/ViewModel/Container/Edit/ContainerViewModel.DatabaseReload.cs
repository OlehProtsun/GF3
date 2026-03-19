/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.DatabaseReload у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Notifications;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        private int _databaseReloadInProgress;

        private void OnDatabaseChanged(object? sender, DatabaseChangedEventArgs e)
        {
            _ = ReloadAfterDatabaseChangeAsync(e.Source);
        }

        private async Task ReloadAfterDatabaseChangeAsync(string source)
        {
            if (!_initialized)
                return;

            if (Interlocked.Exchange(ref _databaseReloadInProgress, 1) == 1)
                return;

            try
            {
                if (Mode == Helpers.ContainerSection.Edit || Mode == Helpers.ContainerSection.ScheduleEdit)
                    return;

                var selectedContainerId = ListVm.SelectedItem?.Id ?? _openedProfileContainerId;
                var selectedScheduleId = ProfileVm.ScheduleListVm.SelectedItem?.Model.Id;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    
                    await LoadContainersAsync(CancellationToken.None, selectedContainerId);

                    var activeContainerId = selectedContainerId ?? GetCurrentContainerId();
                    if (activeContainerId <= 0)
                        return;

                    var latest = await _containerService.GetAsync(activeContainerId, CancellationToken.None);
                    if (latest == null)
                    {
                        if (Mode != Helpers.ContainerSection.List)
                            await SwitchToListAsync();
                        return;
                    }

                    _openedProfileContainerId = latest.Id;

                    
                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == latest.Id);

                    
                    await LoadSchedulesAsync(latest.Id, search: null, CancellationToken.None);

                    if (selectedScheduleId.HasValue)
                    {
                        ProfileVm.ScheduleListVm.SelectedItem = ProfileVm.ScheduleListVm.Items
                            .FirstOrDefault(x => x.Model.Id == selectedScheduleId.Value);
                    }

                    
                    
                    if (Mode == Helpers.ContainerSection.ScheduleProfile && selectedScheduleId.HasValue)
                    {
                        var detailed = await _scheduleService.GetDetailedAsync(selectedScheduleId.Value, CancellationToken.None);
                        if (detailed != null)
                        {
                            await ScheduleProfileVm.SetProfileAsync(
                                detailed,
                                detailed.Employees?.ToList() ?? new(),
                                detailed.Slots?.ToList() ?? new(),
                                detailed.CellStyles?.ToList() ?? new(),
                                CancellationToken.None);
                        }
                    }

                }).Task.Unwrap();
            }
            finally
            {
                Interlocked.Exchange(ref _databaseReloadInProgress, 0);
            }
        }
    }
}
