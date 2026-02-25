using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.Applications.Notifications;

namespace WPFApp.ViewModel.Availability.Main
{
    public sealed partial class AvailabilityViewModel
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
                if (Mode == AvailabilitySection.Edit)
                {
                                        return;
                }

                var selectedId = Mode == AvailabilitySection.Profile ? ProfileVm.AvailabilityId : ListVm.SelectedItem?.Id;

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadAllGroupsAsync(CancellationToken.None);
                    await LoadEmployeesAsync(CancellationToken.None);
                    await LoadBindsAsync(CancellationToken.None);

                    if (Mode == AvailabilitySection.Profile && selectedId.HasValue)
                    {
                        var reloaded = await _availabilityService.LoadFullAsync(selectedId.Value, CancellationToken.None);
                        if (reloaded.group != null)
                            ProfileVm.SetProfile(reloaded.group, reloaded.members, reloaded.days);
                    }
                }).Task.Unwrap();

                            }
            catch (Exception ex)
            {
                            }
            finally
            {
                Interlocked.Exchange(ref _databaseReloadInProgress, 0);
            }
        }
    }
}
