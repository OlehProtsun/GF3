using BusinessLogicLayer.Contracts.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.ScheduleEditor — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за дії всередині ScheduleEditVm:
    ///
    /// 1) Робота з працівниками в поточному блоці:
    ///    - додати працівника
    ///    - прибрати працівника
    ///
    /// 2) Робота з “табами/блоками” (кілька відкритих schedule одночасно):
    ///    - додати новий блок
    ///    - вибрати блок
    ///    - закрити блок
    ///
    /// Чому винесено:
    /// - це не CRUD контейнера і не навігація
    /// - це “логіка редактора schedule”
    /// - так головний ContainerViewModel стає читабельніший
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        /// <summary>
        /// Додати працівника в поточний блок schedule.
        ///
        /// Потік логіки:
        /// 1) Перевірити, що є SelectedBlock
        /// 2) Перевірити, що користувач вибрав Employee
        /// 3) Перевірити, що такого працівника ще немає в block.Employees
        /// 4) Додати ScheduleEmployeeModel
        /// 5) Перерахувати матрицю і preview
        /// </summary>
        internal async Task AddScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;

            var employee = ScheduleEditVm.SelectedEmployee;
            if (employee is null)
            {
                ShowError("Select employee first.");
                return;
            }

            // Захист від дублювання
            if (block.Employees.Any(e => e.EmployeeId == employee.Id))
            {
                ShowInfo("This employee is already added.");
                return;
            }

            // Додаємо в колекцію блоку
            block.Employees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                Employee = employee
            });

            // Оновлюємо UI-матрицю
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

            // Оновлюємо preview availability, бо список employees впливає на preview відображення
            await UpdateAvailabilityPreviewAsync(ct);
        }

        /// <summary>
        /// Прибрати працівника з поточного блока schedule.
        ///
        /// Потік логіки:
        /// 1) Перевірити SelectedBlock
        /// 2) Перевірити, що вибрано SelectedScheduleEmployee
        /// 3) Знайти відповідний ScheduleEmployeeModel у block.Employees і видалити
        /// 4) Видалити всі slots цього працівника (щоб матриця не показувала “висячі” дані)
        /// 5) Видалити стилі клітинок для цього працівника (фон/текст)
        /// 6) Refresh матриці + preview
        /// </summary>
        internal async Task RemoveScheduleEmployeeAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var block = ScheduleEditVm.SelectedBlock;

            var selected = ScheduleEditVm.SelectedScheduleEmployee;
            if (selected is null)
            {
                ShowError("Select employee first.");
                return;
            }

            var toRemove = block.Employees.FirstOrDefault(e => e.EmployeeId == selected.EmployeeId);
            if (toRemove is null)
            {
                ShowInfo("This employee is not in the group.");
                return;
            }

            // 1) Видаляємо працівника з блоку
            block.Employees.Remove(toRemove);

            // 2) Видаляємо всі слоти цього працівника
            var slotsToRemove = block.Slots
                .Where(s => s.EmployeeId == selected.EmployeeId)
                .ToList();

            foreach (var slot in slotsToRemove)
                block.Slots.Remove(slot);

            // 3) Видаляємо стилі клітинок працівника (щоб не лишалось “мертвих” стилів)
            ScheduleEditVm.RemoveCellStylesForEmployee(selected.EmployeeId);

            // 4) Refresh матриці і preview
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        /// <summary>
        /// Додати новий “блок” schedule у режимі створення (IsEdit == false).
        ///
        /// Обмеження:
        /// - у Edit режимі (коли редагуємо існуючі schedule) блоки додаються через MultiOpen,
        ///   а не через “Add block”.
        ///
        /// Потік:
        /// 1) перевірити режим і SelectedBlock
        /// 2) перевірити ліміт MaxOpenedSchedules
        /// 3) створити блок через CreateDefaultBlock(containerId)
        /// 4) додати його в ScheduleEditVm.Blocks і зробити SelectedBlock
        /// 5) refresh матриці + preview
        /// </summary>
        internal async Task AddScheduleBlockAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.IsEdit)
                return;

            if (ScheduleEditVm.SelectedBlock is null)
                return;

            if (ScheduleEditVm.Blocks.Count >= MaxOpenedSchedules)
            {
                ShowInfo($"You can open max {MaxOpenedSchedules} schedules.");
                return;
            }

            // Беремо containerId з поточного блока (він точно правильний)
            var containerId = ScheduleEditVm.SelectedBlock.Model.ContainerId;

            var block = CreateDefaultBlock(containerId);
            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        /// <summary>
        /// Перемкнутися на інший блок у ScheduleEditVm.Blocks.
        ///
        /// Потік:
        /// 1) перевірити, що блок існує в колекції Blocks
        /// 2) зробити його SelectedBlock
        /// 3) очистити/поставити помилки (якщо ти їх зберігаєш по блоках)
        /// 4) refresh матриці і preview
        /// </summary>
        internal async Task SelectScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (block is null)
                return;

            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            // якщо той самий блок — нічого не робимо
            if (ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
                return;

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();

            // щоб Working гарантовано відрендерився
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                // зафіксувати selection + прибрати старі errors (UI thread)
                await RunOnUiThreadAsync(() =>
                {
                    ScheduleEditVm.SelectedBlock = block;
                    ScheduleEditVm.ClearValidationErrors();
                }).ConfigureAwait(false);

                // важкі refresh'і під токеном, щоб швидкі кліки скасовували попереднє
                await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken).ConfigureAwait(false);
                await UpdateAvailabilityPreviewAsync(uiToken).ConfigureAwait(false);

                // дати UI відрендерити матрицю для нового табу
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

                // Success (можеш зробити 400–600мс, щоб не дратувало при частих кліках)
                await ShowNavSuccessThenAutoHideAsync(uiToken, 500).ConfigureAwait(false);
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
        /// Закрити блок (таб) schedule у редакторі.
        ///
        /// Потік:
        /// 1) перевірити, що блок існує
        /// 2) confirm
        /// 3) видалити блок з Blocks
        /// 4) якщо блоків не лишилось:
        ///    - SelectedBlock = null
        ///    - очистити матрицю і preview
        /// 5) інакше:
        ///    - вибрати “сусідній” блок (по індексу)
        ///    - оновити preview
        /// </summary>
        internal async Task CloseScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            if (!Confirm("Are you sure you want to close this schedule?"))
                return;

            var index = ScheduleEditVm.Blocks.IndexOf(block);
            ScheduleEditVm.Blocks.Remove(block);

            // Якщо блоків не лишилось — очищаємо UI
            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ScheduleEditVm.SelectedBlock = null;

                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

                // Очищаємо preview: передаємо empty списки + CLEAR ключ
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    block.Model.Year, block.Model.Month,
                    new System.Collections.Generic.List<ScheduleSlotModel>(),
                    new System.Collections.Generic.List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                    ct);

                return;
            }

            // Вибираємо наступний блок: той самий індекс (або останній, якщо видалили останній)
            var nextIndex = Math.Max(0, Math.Min(index, ScheduleEditVm.Blocks.Count - 1));
            var next = ScheduleEditVm.Blocks[nextIndex];

            await SelectScheduleBlockAsync(next, ct);
        }
    }
}
