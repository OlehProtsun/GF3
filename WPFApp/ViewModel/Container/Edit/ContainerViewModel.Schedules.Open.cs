using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Shared;

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
        internal Task StartScheduleAddAsync(CancellationToken ct = default)
        {
            var containerId = GetCurrentContainerId();
            if (containerId <= 0)
            {
                ShowError("Select a container first.");
                return Task.CompletedTask;
            }

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await PrepareScheduleEditContextAsync(uiToken).ConfigureAwait(false);

                    ScheduleEditVm.ResetForNew();
                    ScheduleEditVm.IsEdit = false;
                    ScheduleEditVm.Blocks.Clear();

                    var block = CreateDefaultBlock(containerId);
                    ScheduleEditVm.Blocks.Add(block);
                    ScheduleEditVm.SelectedBlock = block;

                    ScheduleCancelTarget = ContainerSection.Profile;

                    await SwitchToScheduleEditAsync().ConfigureAwait(false);
                    await ScheduleEditVm.RefreshScheduleMatrixAsync(CancellationToken.None).ConfigureAwait(false);
                    await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                        block.Model.Year,
                        block.Model.Month,
                        new List<ScheduleSlotModel>(),
                        new List<ScheduleEmployeeModel>(),
                        previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                        CancellationToken.None).ConfigureAwait(false);
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 700);
        }


        private async Task PrepareScheduleEditContextAsync(CancellationToken uiToken)
        {
            ScheduleEditVm.CancelBackgroundWork();
            CancelScheduleEditWork();

            await LoadLookupsAsync(uiToken).ConfigureAwait(false);
            ResetScheduleFilters();
            ScheduleEditVm.ClearValidationErrors();
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
        internal Task EditSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await PrepareScheduleEditContextAsync(uiToken).ConfigureAwait(false);

                    var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken).ConfigureAwait(false);
                    if (detailed is null)
                        return false;

                    if (!HasGeneratedContent(detailed))
                    {
                        ShowError("This schedule doesn’t contain generated data and can’t be edited. Please run generation first.");
                        return false;
                    }

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

                    await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken).ConfigureAwait(false);
                    ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                        ? ContainerSection.ScheduleProfile
                        : ContainerSection.Profile;

                    await UpdateAvailabilityPreviewAsync(uiToken).ConfigureAwait(false);
                    await SwitchToScheduleEditAsync().ConfigureAwait(false);

                    return true;
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        /// <summary>
        /// Відкрити кілька schedules у табах редактора.
        ///
        /// Поведінка:
        /// - якщо schedule вже відкритий — не дублюємо, а додаємо як openedBlocks (щоб можна було активувати)
        /// - якщо перевищили ліміт MaxOpenedSchedules — пропускаємо і показуємо повідомлення
        /// - schedules без generated data — пропускаємо і показуємо повідомлення
        /// </summary>
        internal Task MultiOpenSchedulesAsync(IReadOnlyList<ScheduleModel> schedules, CancellationToken ct = default)
        {
            if (schedules is null || schedules.Count == 0)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await PrepareScheduleEditContextAsync(uiToken).ConfigureAwait(false);

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

                        var existing = ScheduleEditVm.Blocks.FirstOrDefault(b => b.Model.Id == schedule.Id);
                        if (existing != null)
                        {
                            openedBlocks.Add(existing);
                            continue;
                        }

                        if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
                        {
                            limitSkipped.Add(string.IsNullOrWhiteSpace(schedule.Name) ? $"Schedule {schedule.Id}" : schedule.Name);
                            continue;
                        }

                        var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken).ConfigureAwait(false);
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
                        if (limitSkipped.Count > 0)
                            ShowError($"Max open schedules limit is {MaxOpenedSchedules}. Close some tabs first.");
                        else
                            ShowError("Selected schedules could not be loaded.");

                        return false;
                    }

                    if (invalidSchedules.Count > 0)
                        ShowError($"Skipped schedules without generated data:{Environment.NewLine}{string.Join(Environment.NewLine, invalidSchedules)}");

                    if (limitSkipped.Count > 0)
                        ShowInfo($"Opened only first {MaxOpenedSchedules}. Skipped due to limit:{Environment.NewLine}{string.Join(Environment.NewLine, limitSkipped)}");

                    ScheduleEditVm.SelectedBlock = openedBlocks.First();
                    await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken).ConfigureAwait(false);

                    ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                        ? ContainerSection.ScheduleProfile
                        : ContainerSection.Profile;

                    await UpdateAvailabilityPreviewAsync(uiToken).ConfigureAwait(false);
                    await SwitchToScheduleEditAsync().ConfigureAwait(false);
                    return true;
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }

        /// <summary>
        /// Відкрити профіль schedule (read-only перегляд).
        /// </summary>
        internal Task OpenScheduleProfileAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null)
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, uiToken).ConfigureAwait(false);
                    if (detailed is null)
                        return false;

                    var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
                    var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
                    var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

                    await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, uiToken)
                        .ConfigureAwait(false);

                    await RunOnUiThreadAsync(() =>
                    {
                        ProfileVm.ScheduleListVm.SelectedItem =
                            ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);
                    }).ConfigureAwait(false);

                    ScheduleCancelTarget = ContainerSection.Profile;
                    await SwitchToScheduleProfileAsync().ConfigureAwait(false);
                    return true;
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }


        /// <summary>
        /// Видалити вибраний schedule з контейнера.
        /// </summary>
        internal Task DeleteSelectedScheduleAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null)
                return Task.CompletedTask;

            if (!Confirm($"Delete schedule {schedule.Name}?"))
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await _scheduleService.DeleteAsync(schedule.Id, uiToken).ConfigureAwait(false);
                    _databaseChangeNotifier.NotifyDatabaseChanged("Container.ScheduleDelete");
                    await LoadSchedulesAsync(schedule.ContainerId, search: null, uiToken).ConfigureAwait(false);
                    await SwitchToProfileAsync().ConfigureAwait(false);
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 900);
        }
    }
}
