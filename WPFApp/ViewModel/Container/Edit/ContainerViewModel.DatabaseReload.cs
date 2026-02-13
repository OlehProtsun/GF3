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
                {
                    _logger.Log($"[DB-CHANGE] Container reload skipped in {Mode} mode (unsaved editor safety). Source={source}.");
                    return;
                }

                var selectedContainerId = ListVm.SelectedItem?.Id ?? _openedProfileContainerId;
                var selectedScheduleId = ProfileVm.ScheduleListVm.SelectedItem?.Model.Id;

                await RunOnUiThreadAsync(() => { }).ConfigureAwait(false);
                await LoadContainersAsync(CancellationToken.None, selectedContainerId).ConfigureAwait(false);

                var activeContainerId = selectedContainerId ?? GetCurrentContainerId();
                if (activeContainerId > 0)
                {
                    var latest = await _containerService.GetAsync(activeContainerId, CancellationToken.None).ConfigureAwait(false);
                    if (latest != null)
                    {
                        _openedProfileContainerId = latest.Id;

                        await RunOnUiThreadAsync(() =>
                        {
                            ProfileVm.SetProfile(latest);
                            ListVm.SelectedItem = ListVm.Items.FirstOrDefault(x => x.Id == latest.Id);
                        }).ConfigureAwait(false);

                        await LoadSchedulesAsync(latest.Id, search: null, CancellationToken.None).ConfigureAwait(false);

                        if (selectedScheduleId.HasValue)
                        {
                            await RunOnUiThreadAsync(() =>
                            {
                                ProfileVm.ScheduleListVm.SelectedItem = ProfileVm.ScheduleListVm.Items
                                    .FirstOrDefault(x => x.Model.Id == selectedScheduleId.Value);
                            }).ConfigureAwait(false);
                        }
                    }
                    else if (Mode != Helpers.ContainerSection.List)
                    {
                        await SwitchToListAsync().ConfigureAwait(false);
                    }
                }

                _logger.Log($"[DB-CHANGE] Container module reloaded. Source={source}.");
            }
            catch (Exception ex)
            {
                _logger.Log($"[DB-CHANGE] Container reload failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _databaseReloadInProgress, 0);
            }
        }
    }
}
