using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using System.Windows;


namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Schedules.Open — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за “відкриття/перехід” у сценаріях schedule:
    ///
    /// 1) Пошук schedule для контейнера (SearchScheduleAsync)
    /// 2) Старт створення нового schedule (StartScheduleAddAsync)
    /// 3) Відкриття редагування одного schedule (EditSelectedScheduleAsync)
    /// 4) Мультивідкриття (MultiOpenSchedulesAsync) — кілька schedule у табах
    /// 5) Відкриття профілю schedule (OpenScheduleProfileAsync)
    /// 6) Видалення schedule (DeleteSelectedScheduleAsync)
    ///
    /// Тут немає генерації і немає save — це буде окремий файл.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        /// <summary>
        /// Завантажити schedules для поточного контейнера з фільтром (якщо є).
        /// Викликається з Profile екрану (кнопка Search).
        /// </summary>
        internal async Task SearchScheduleAsync(CancellationToken ct = default)
        {
            var containerId = GetCurrentContainerId();
            if (containerId <= 0)
            {
                ShowError("Select a container first.");
                return;
            }

            await LoadSchedulesAsync(containerId, ProfileVm.ScheduleListVm.SearchText, ct);
        }

        /// <summary>
        /// Старт створення нового schedule:
        /// 1) перевіряємо containerId
        /// 2) вантажимо lookups (shops/groups/employees)
        /// 3) скидаємо фільтри
        /// 4) готуємо ScheduleEditVm під Add режим
        /// 5) створюємо 1 дефолтний блок і робимо його SelectedBlock
        /// 6) refresh матриці
        /// 7) очищаємо preview
        /// 8) переходимо в ScheduleEdit
        /// </summary>
        internal async Task StartScheduleAddAsync(CancellationToken ct = default)
        {
            var containerId = GetCurrentContainerId();
            if (containerId <= 0)
            {
                ShowError("Select a container first.");
                return;
            }

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                ScheduleEditVm.CancelBackgroundWork();
                CancelScheduleEditWork();

                var safeCt = ct.IsCancellationRequested ? CancellationToken.None : uiToken;

                await LoadLookupsAsync(safeCt);
                ResetScheduleFilters();

                ScheduleEditVm.ClearValidationErrors();
                ScheduleEditVm.ResetForNew();
                ScheduleEditVm.IsEdit = false;
                ScheduleEditVm.Blocks.Clear();

                var block = CreateDefaultBlock(containerId);
                ScheduleEditVm.Blocks.Add(block);
                ScheduleEditVm.SelectedBlock = block;

                ScheduleCancelTarget = ContainerSection.Profile;

                await SwitchToScheduleEditAsync();

                await ScheduleEditVm.RefreshScheduleMatrixAsync(CancellationToken.None);

                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    block.Model.Year, block.Model.Month,
                    new List<ScheduleSlotModel>(),
                    new List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                    CancellationToken.None);

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

        /// <summary>
        /// Відкрити редагування одного schedule (SelectedItem у ProfileVm.ScheduleListVm).
        ///
        /// Логіка:
        /// 1) вантажимо lookups
        /// 2) тягнемо detailed schedule (employees/slots/styles)
        /// 3) перевіряємо що schedule має згенеровані дані (інакше редагувати нема що)
        /// 4) будуємо block і кладемо в ScheduleEditVm
        /// 5) refresh матриці + preview
        /// 6) переходимо в ScheduleEdit
        /// </summary>
        internal async Task EditSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();

            // дати WPF відрендерити "Working" ДО важких await
            await Application.Current.Dispatcher.InvokeAsync(
                () => { },
                DispatcherPriority.ApplicationIdle
            );

            try
            {
                // щоб старі білди/preview не "вистрілили" під час переходу
                ScheduleEditVm.CancelBackgroundWork();
                CancelScheduleEditWork();

                await LoadLookupsAsync(uiToken);
                ResetScheduleFilters();

                var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken);
                if (detailed is null)
                {
                    await HideNavStatusAsync();
                    return;
                }

                if (!HasGeneratedContent(detailed))
                {
                    await HideNavStatusAsync();
                    ShowError("This schedule doesn’t contain generated data and can’t be edited. Please run generation first.");
                    return;
                }

                ScheduleEditVm.ClearValidationErrors();
                ScheduleEditVm.ResetForNew();
                ScheduleEditVm.IsEdit = true;
                ScheduleEditVm.Blocks.Clear();

                var block = CreateBlockFromSchedule(
                    detailed,
                    detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                    detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>(),
                    detailed.CellStyles?.ToList());

                block.SelectedAvailabilityGroupId = detailed.AvailabilityGroupId!.Value;

                ScheduleEditVm.Blocks.Add(block);
                ScheduleEditVm.SelectedBlock = block;

                await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken);

                ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                    ? ContainerSection.ScheduleProfile
                    : ContainerSection.Profile;

                await UpdateAvailabilityPreviewAsync(uiToken);

                // навігація на Edit view
                await SwitchToScheduleEditAsync();

                // дати новому view реально піднятись/відрендеритись
                await Application.Current.Dispatcher.InvokeAsync(
                    () => { },
                    DispatcherPriority.ApplicationIdle
                );

                // тепер Success (і авто-hide)
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

        /// <summary>
        /// Відкрити кілька schedules у табах редактора.
        ///
        /// Поведінка:
        /// - якщо schedule вже відкритий — не дублюємо, а додаємо як openedBlocks (щоб можна було активувати)
        /// - якщо перевищили ліміт MaxOpenedSchedules — пропускаємо і показуємо повідомлення
        /// - schedules без generated data — пропускаємо і показуємо повідомлення
        /// </summary>
        internal async Task MultiOpenSchedulesAsync(IReadOnlyList<ScheduleModel> schedules, CancellationToken ct = default)
        {
            if (schedules is null || schedules.Count == 0)
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await LoadLookupsAsync(uiToken);
                ResetScheduleFilters();

                ScheduleEditVm.ClearValidationErrors();

                var keepExisting = Mode == ContainerSection.ScheduleEdit && ScheduleEditVm.IsEdit;
                if (!keepExisting)
                    ScheduleEditVm.ResetForNew();

                ScheduleEditVm.IsEdit = true;

                var openedBlocks = new List<ScheduleBlockViewModel>();
                var invalidSchedules = new List<string>();
                var limitSkipped = new List<string>();

                foreach (var schedule in schedules)
                {
                    uiToken.ThrowIfCancellationRequested();

                    // якщо вже відкритий — просто активуємо
                    var existing = ScheduleEditVm.Blocks.FirstOrDefault(b => b.Model.Id == schedule.Id);
                    if (existing != null)
                    {
                        openedBlocks.Add(existing);
                        continue;
                    }

                    // ліміт табів
                    if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
                    {
                        limitSkipped.Add(string.IsNullOrWhiteSpace(schedule.Name) ? $"Schedule {schedule.Id}" : schedule.Name);
                        continue;
                    }

                    var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken);
                    if (detailed is null)
                        continue;

                    if (!HasGeneratedContent(detailed))
                    {
                        invalidSchedules.Add(string.IsNullOrWhiteSpace(detailed.Name) ? $"Schedule {detailed.Id}" : detailed.Name);
                        continue;
                    }

                    var block = CreateBlockFromSchedule(
                        detailed,
                        detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                        detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>(),
                        detailed.CellStyles?.ToList());

                    block.SelectedAvailabilityGroupId = detailed.AvailabilityGroupId!.Value;

                    ScheduleEditVm.Blocks.Add(block);
                    openedBlocks.Add(block);
                }

                if (openedBlocks.Count == 0)
                {
                    await HideNavStatusAsync();

                    if (limitSkipped.Count > 0)
                    {
                        ShowError($"Max open schedules limit is {MaxOpenedSchedules}. Close some tabs first.");
                        return;
                    }

                    ShowError("Selected schedules could not be loaded.");
                    return;
                }

                if (invalidSchedules.Count > 0)
                    ShowError($"Skipped schedules without generated data:{Environment.NewLine}{string.Join(Environment.NewLine, invalidSchedules)}");

                if (limitSkipped.Count > 0)
                    ShowInfo($"Opened only first {MaxOpenedSchedules}. Skipped due to limit:{Environment.NewLine}{string.Join(Environment.NewLine, limitSkipped)}");

                ScheduleEditVm.SelectedBlock = openedBlocks.First();
                await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken);

                ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                    ? ContainerSection.ScheduleProfile
                    : ContainerSection.Profile;

                await UpdateAvailabilityPreviewAsync(uiToken);

                await SwitchToScheduleEditAsync();

                // дати view відрендеритися
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

        /// <summary>
        /// Відкрити профіль schedule (read-only перегляд).
        /// </summary>
        internal async Task OpenScheduleProfileAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync(); // показали Working
                                         // дати WPF відрендерити оверлей ДО важких await
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => { },
                DispatcherPriority.ApplicationIdle
            );

            try
            {
                var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken).ConfigureAwait(false);
                if (detailed is null)
                {
                    await HideNavStatusAsync().ConfigureAwait(false);
                    return;
                }

                var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

                await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, uiToken)
                                       .ConfigureAwait(false);

                // синхронізуємо selection у списку
                await RunOnUiThreadAsync(() =>
                {
                    ProfileVm.ScheduleListVm.SelectedItem =
                        ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);
                });

                ScheduleCancelTarget = ContainerSection.Profile;

                // навігація
                await SwitchToScheduleProfileAsync().ConfigureAwait(false);

                // важливо: дати новому view завантажитись/відрендеритись
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                    () => { },
                    DispatcherPriority.ApplicationIdle
                );

                // тепер Success (і авто-hide)
                await ShowNavSuccessThenAutoHideAsync(uiToken, 900).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync().ConfigureAwait(false);
                ShowError(ex);
            }
        }


        /// <summary>
        /// Видалити вибраний schedule з контейнера.
        /// </summary>
        internal async Task DeleteSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            if (!Confirm($"Delete schedule {schedule.Name}?"))
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await _scheduleService.DeleteAsync(schedule.Id, uiToken);
                _databaseChangeNotifier.NotifyDatabaseChanged("Container.ScheduleDelete");
                await LoadSchedulesAsync(schedule.ContainerId, search: null, uiToken);
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
    }
}
