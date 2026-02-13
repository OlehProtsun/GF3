using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.ViewModel.Container.Edit.Helpers;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

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

            try
            {
                // Важливо: зупинити старі білди/preview, щоб вони не “стріляли” під час переходу
                ScheduleEditVm.CancelBackgroundWork();
                CancelScheduleEditWork(); // preview pipeline в ContainerViewModel (AvailabilityPreview.cs)

                // Якщо токен випадково вже скасований — не даємо цьому зірвати Add
                var safeCt = ct.IsCancellationRequested ? CancellationToken.None : ct;

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

                // Можна переключитись одразу, щоб UI не “виглядав як завис”
                await SwitchToScheduleEditAsync();

                // Для нового schedule slots порожні — Refresh просто очистить матрицю й вийде
                await ScheduleEditVm.RefreshScheduleMatrixAsync(CancellationToken.None);

                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    block.Model.Year, block.Model.Month,
                    new List<ScheduleSlotModel>(),
                    new List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                    CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Cancellation тут не має ламати Add
            }
            catch (Exception ex)
            {
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

            await LoadLookupsAsync(ct);
            ResetScheduleFilters();

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;

            if (!HasGeneratedContent(detailed))
            {
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

            // група, з якої був згенерований графік
            block.SelectedAvailabilityGroupId = detailed.AvailabilityGroupId!.Value;

            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

            ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                ? ContainerSection.ScheduleProfile
                : ContainerSection.Profile;

            await UpdateAvailabilityPreviewAsync(ct);
            await SwitchToScheduleEditAsync();
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
            if (schedules.Count == 0)
                return;

            await LoadLookupsAsync(ct);
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

                var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
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
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

            ScheduleCancelTarget = Mode == ContainerSection.ScheduleProfile
                ? ContainerSection.ScheduleProfile
                : ContainerSection.Profile;

            await UpdateAvailabilityPreviewAsync(ct);
            await SwitchToScheduleEditAsync();
        }

        /// <summary>
        /// Відкрити профіль schedule (read-only перегляд).
        /// </summary>
        internal async Task OpenScheduleProfileAsync(CancellationToken ct = default)
        {
            var schedule = ProfileVm.ScheduleListVm.SelectedItem?.Model;
            if (schedule is null) return;

            var detailed = await _scheduleService.GetDetailedAsync(schedule.Id, ct);
            if (detailed is null) return;

            var employees = detailed.Employees?.ToList() ?? new List<ScheduleEmployeeModel>();
            var slots = detailed.Slots?.ToList() ?? new List<ScheduleSlotModel>();
            var cellStyles = detailed.CellStyles?.ToList() ?? new List<ScheduleCellStyleModel>();

            await ScheduleProfileVm.SetProfileAsync(detailed, employees, slots, cellStyles, ct);

            // синхронізуємо selection у списку
            ProfileVm.ScheduleListVm.SelectedItem =
                ProfileVm.ScheduleListVm.Items.FirstOrDefault(x => x.Model.Id == detailed.Id);

            ScheduleCancelTarget = ContainerSection.Profile;
            await SwitchToScheduleProfileAsync();
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

            await _scheduleService.DeleteAsync(schedule.Id, ct);
            _databaseChangeNotifier.NotifyDatabaseChanged("Container.ScheduleDelete");
            await LoadSchedulesAsync(schedule.ContainerId, search: null, ct);

            ShowInfo("Schedule deleted successfully.");
            await SwitchToProfileAsync();
        }
    }
}
