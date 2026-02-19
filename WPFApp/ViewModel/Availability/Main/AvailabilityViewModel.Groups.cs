using BusinessLogicLayer.Availability;
using DataAccessLayer.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Groups — CRUD і “бізнес-потоки” для AvailabilityGroup:
    /// - Search
    /// - StartAdd
    /// - EditSelected
    /// - Save
    /// - DeleteSelected
    /// - OpenProfile
    /// - Cancel
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal async Task SearchAsync(CancellationToken ct = default)
        {
            // 1) Беремо search term з ListVm.
            var term = ListVm.SearchText;

            // 2) Якщо пусто — беремо все, інакше — пошук по значенню.
            var list = string.IsNullOrWhiteSpace(term)
                ? await _availabilityService.GetAllAsync(ct)
                : await _availabilityService.GetByValueAsync(term, ct);

            // 3) Кладемо результат у ListVm.
            ListVm.SetItems(list);
        }

        internal async Task StartAddAsync(CancellationToken ct = default)
        {
            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await LoadEmployeesAsync(uiToken);

                await RunOnUiThreadAsync(() =>
                {
                    ResetEmployeeSearch();
                    EditVm.ResetForNew();
                    CancelTarget = AvailabilitySection.List;
                });

                await SwitchToEditAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null)
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
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

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var rawName = (EditVm.AvailabilityName ?? string.Empty).Trim();

            // 3) Визначаємо, чи це create.
            var isNew = EditVm.AvailabilityId == 0;

            // 4) Сuffix для нової групи (MM.YYYY).
            var suffix = $"{EditVm.AvailabilityMonth:D2}.{EditVm.AvailabilityYear}";

            // 5) Для “нових” — додаємо suffix в назву, щоб відрізняти місяці.
            var finalName = isNew
                ? $"{rawName} : {suffix}"
                : rawName;

            // 6) Формуємо модель групи.
            var group = new AvailabilityGroupModel
            {
                Id = EditVm.AvailabilityId,
                Name = finalName,
                Year = EditVm.AvailabilityYear,
                Month = EditVm.AvailabilityMonth
            };

            // 7) Валідатор домену (BLL): повертає словник помилок (property -> message).
            var errors = AvailabilityGroupValidator.Validate(group);

            // 8) Якщо помилки є — показуємо в EditVm (INotifyDataErrorInfo) і виходимо.
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            // 9) Перевіряємо, що вибрано хоча б 1 працівника.
            var selectedEmployees = EditVm.GetSelectedEmployeeIds();
            if (selectedEmployees.Count == 0)
            {
                ShowError("Add at least 1 employee to the group.");
                return;
            }

            // 10) Для швидкого contains — робимо HashSet.
            var selectedSet = selectedEmployees.ToHashSet();

            // 11) Беремо коди з матриці і лишаємо тільки для вибраних employeeId.
            var raw = EditVm.ReadGroupCodes()
                .Where(x => selectedSet.Contains(x.employeeId));

            // 12) Будуємо payload для BLL (перетворення "+/-/interval" -> доменні структури).
            if (!AvailabilityPayloadBuilder.TryBuild(raw, out var payload, out var err))
            {
                ShowError(err ?? "Invalid availability codes.");
                return;
            }

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
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

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
                return;
            }
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            // 1) Беремо поточний вибір.
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            // 2) Confirm.
            if (!Confirm($"Delete '{current.Name}' ?"))
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await _availabilityService.DeleteAsync(current.Id, uiToken);

                _databaseChangeNotifier.NotifyDatabaseChanged("Availability.Delete");
                await LoadAllGroupsAsync(uiToken);
                await SwitchToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var current = ListVm.SelectedItem;
            if (current is null)
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                var (group, members, days) = await _availabilityService.LoadFullAsync(current.Id, uiToken);

                await RunOnUiThreadAsync(() =>
                {
                    _openedProfileGroupId = current.Id;
                    ProfileVm.SetProfile(group, members, days);
                    CancelTarget = AvailabilitySection.List;
                });

                await SwitchToProfileAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }

        internal Task CancelAsync()
        {
            // 1) При Cancel з Edit — чистимо validation, щоб не “прилипало”.
            EditVm.ClearValidationErrors();

            // 2) Скидаємо пошук employees (щоб при наступному відкритті був повний список).
            ResetEmployeeSearch();

            // 3) Навігація залежить від Mode і CancelTarget.
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
