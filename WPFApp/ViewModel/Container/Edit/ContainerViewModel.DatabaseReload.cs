using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Container.Edit
{
    public sealed partial class ContainerViewModel
    {
        private int _databaseReloadInProgress;

        private void OnDatabaseChanged(object? sender, Service.DatabaseChangedEventArgs e)
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
                    // 1) reload containers (UI-safe: метод нижче теж зробимо UI-safe)
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

                    // 2) profile + selection
                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == latest.Id);

                    // 3) reload schedules (UI-safe: метод нижче теж зробимо UI-safe)
                    await LoadSchedulesAsync(latest.Id, search: null, CancellationToken.None);

                    if (selectedScheduleId.HasValue)
                    {
                        ProfileVm.ScheduleListVm.SelectedItem = ProfileVm.ScheduleListVm.Items
                            .FirstOrDefault(x => x.Model.Id == selectedScheduleId.Value);
                    }

                    // 4) КРИТИЧНО: якщо ти зараз у ScheduleProfile — треба перевідкрити профіль,
                    // інакше графіки можуть лишитися пустими після перезавантаження списку/кешів.
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
