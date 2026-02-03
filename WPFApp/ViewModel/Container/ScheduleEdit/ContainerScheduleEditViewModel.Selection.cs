using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WPFApp.Infrastructure.Threading;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Selection.cs — частина (partial) ViewModel, яка відповідає ТІЛЬКИ за:
    /// 1) “реакції” на зміну вибраних значень (SelectedShop / SelectedAvailabilityGroup)
    /// 2) debounce (щоб не виконувати важку логіку на кожен швидкий клік)
    /// 3) інвалідацію (скидання) згенерованого розкладу, коли змінився контекст
    ///
    /// Чому це винесено:
    /// - у головному файлі VM повинні лишатися в основному властивості/команди,
    ///   а не поведінкові механізми “коли що перераховувати/скидати”.
    /// - ця логіка добре читається як окремий модуль.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        // ============================================================
        // 1) DEBOUNCE-НАЛАШТУВАННЯ ТА СТАН
        // ============================================================

        /// <summary>
        /// Скільки чекати перед “застосуванням” зміни selection.
        ///
        /// Навіщо:
        /// - користувач може швидко клацати по списку магазинів/груп,
        ///   і ми НЕ хочемо запускати логіку на кожен клік
        /// - ми хочемо виконати тільки останній вибір після короткої паузи
        ///
        /// 200ms — типове значення, яке:
        /// - майже не відчувається людиною
        /// - але сильно зменшує навантаження на UI/обчислення
        /// </summary>
        private static readonly TimeSpan SelectionDebounceDelay = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Дебаунсер для зміни магазину (SelectedShop).
        /// Після затримки він застосує ScheduleShopId, якщо вибір не змінився.
        /// </summary>
        private readonly UiDebouncedAction _shopSelectionDebounce;

        /// <summary>
        /// Дебаунсер для зміни AvailabilityGroup (SelectedAvailabilityGroup).
        /// Після затримки він:
        /// - запише SelectedBlock.SelectedAvailabilityGroupId
        /// - і скине згенеровані слоти (InvalidateGeneratedSchedule)
        /// </summary>
        private readonly UiDebouncedAction _availabilitySelectionDebounce;

        /// <summary>
        /// Скасувати “відкладені” застосування selection.
        /// Корисно викликати при ResetForNew() або закритті форми.
        /// </summary>
        private void CancelSelectionDebounce()
        {
            _shopSelectionDebounce.Cancel();
            _availabilitySelectionDebounce.Cancel();
        }


        // ============================================================
        // 2) РЕАКЦІЯ НА ЗМІНУ SHOP (SelectedShop)
        // ============================================================

        /// <summary>
        /// Викликається із setter'а SelectedShop тоді, коли:
        /// - value реально змінилося (oldId != newId)
        /// - ми НЕ в режимі selection-sync (_selectionSyncDepth == 0)
        ///
        /// Що робить:
        /// - запускає debounce: чекаємо SelectionDebounceDelay
        /// - якщо за цей час користувач вибрав інший shop — нічого не робимо
        /// - якщо вибір все ще актуальний — записуємо ScheduleShopId = newId
        ///
        /// Важливо:
        /// - сам setter SelectedShop НЕ повинен одразу писати в модель,
        ///   бо це може викликати каскад оновлень (матриця, preview, тощо).
        /// - через debounce ми застосовуємо тільки “остаточний” вибір користувача.
        /// </summary>
        private void ScheduleShopSelectionChange(int newId)
        {
            _shopSelectionDebounce.Schedule(() =>
            {
                // 1) Перевірка актуальності.
                // Якщо користувач за час очікування вже вибрав інший shop,
                // то SelectedShop.Id буде іншим, і ми просто виходимо.
                if (SelectedShop?.Id != newId)
                    return;

                // 2) Записуємо в модель через властивість ScheduleShopId.
                // Це запускає SetScheduleValue(...) та пов'язані реакції/валідацію.
                ScheduleShopId = newId;

                if (newId > 0)
                    ClearShopSelectionErrors();
            });
        }


        // ============================================================
        // 3) РЕАКЦІЯ НА ЗМІНУ AVAILABILITY GROUP (SelectedAvailabilityGroup)
        // ============================================================

        /// <summary>
        /// Викликається із setter'а SelectedAvailabilityGroup тоді, коли:
        /// - value реально змінилося (oldId != newId)
        /// - SelectedBlock != null
        /// - ми НЕ в режимі selection-sync (_selectionSyncDepth == 0)
        /// - і НЕ активний suppress-флаг (_suppressAvailabilityGroupUpdate == false)
        ///
        /// Що робить після debounce:
        /// 1) ще раз перевіряє, що користувач не вибрав іншу групу за цей час
        /// 2) записує groupId у SelectedBlock.SelectedAvailabilityGroupId
        /// 3) скидає згенеровані слоти, бо вони залежать від групи доступності
        /// </summary>
        private void ScheduleAvailabilitySelectionChange(int newId)
        {
            _availabilitySelectionDebounce.Schedule(() =>
            {
                // 1) Перевірка актуальності.
                // Якщо вибір змінився знову — цей запуск застарів.
                if (SelectedAvailabilityGroup?.Id != newId)
                    return;

                // 2) Без SelectedBlock нема куди записувати.
                if (SelectedBlock is null)
                    return;

                // 3) Записуємо вибрану групу у блок (це “джерело правди”).
                SelectedBlock.SelectedAvailabilityGroupId = newId;

                // 4) Дуже важливо:
                // Зміна групи доступності змінює “правила”, за якими генерується розклад.
                // Тому старі згенеровані слоти стають недійсними.
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);

                if (newId > 0)
                    ClearAvailabilitySelectionErrors();

                SafeForget(HandleAvailabilitySelectionChangedAsync(newId));
            });
        }

        private async Task HandleAvailabilitySelectionChangedAsync(int availabilityGroupId)
        {
            if (availabilityGroupId <= 0)
                return;

            var block = SelectedBlock;
            if (block is null)
                return;

            var year = ScheduleYear;
            var month = ScheduleMonth;

            var preview = await _owner.GetAvailabilityPreviewAsync(availabilityGroupId, year, month).ConfigureAwait(false);

            if (!ReferenceEquals(SelectedBlock, block) || block.SelectedAvailabilityGroupId != availabilityGroupId)
                return;

            var minHoursByEmpId = block.Employees
                .GroupBy(e => e.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First().MinHoursMonth);

            List<ScheduleEmployeeModel> previewEmployees = new();

            await _owner.RunOnUiThreadAsync(() =>
            {
                if (!ReferenceEquals(SelectedBlock, block) || block.SelectedAvailabilityGroupId != availabilityGroupId)
                    return;

                block.Employees.Clear();

                foreach (var employee in preview.employees)
                {
                    minHoursByEmpId.TryGetValue(employee.Id, out var minHours);

                    block.Employees.Add(new ScheduleEmployeeModel
                    {
                        EmployeeId = employee.Id,
                        Employee = employee,
                        MinHoursMonth = minHours
                    });
                }

                previewEmployees = block.Employees.ToList();
            }).ConfigureAwait(false);

            if (!ReferenceEquals(SelectedBlock, block) || block.SelectedAvailabilityGroupId != availabilityGroupId)
                return;

            var previewKey = BuildAvailabilityPreviewKey(availabilityGroupId, year, month);
            var availabilitySlots = preview.availabilitySlots.ToList();

            await RefreshAvailabilityPreviewMatrixAsync(
                year,
                month,
                availabilitySlots,
                previewEmployees,
                previewKey).ConfigureAwait(false);
        }

        private string BuildAvailabilityPreviewKey(int availabilityGroupId, int year, int month)
        {
            static string CanonShift(string? value)
            {
                value = (value ?? string.Empty).Trim();
                return value.Replace(" - ", "-").Replace(" -", "-").Replace("- ", "-");
            }

            return $"{availabilityGroupId}|{year}|{month}|{CanonShift(ScheduleShift1)}|{CanonShift(ScheduleShift2)}";
        }


        // ============================================================
        // 4) ІНВАЛІДАЦІЯ ЗГЕНЕРОВАНОГО РОЗКЛАДУ
        // ============================================================

        /// <summary>
        /// Скидає (інвалідить) вже згенерований розклад/матриці, коли змінилася база для генерації:
        /// - змінили рік/місяць
        /// - змінили availability group
        /// - інші параметри, які впливають на структуру слотів
        ///
        /// Параметр clearPreviewMatrix:
        /// - true  => очищаємо також AvailabilityPreviewMatrix та _availabilityPreviewKey
        /// - false => залишаємо preview як є (якщо сценарій дозволяє)
        ///
        /// Що робимо по кроках:
        /// 1) очищаємо SelectedBlock.Slots (бо вони більше не валідні)
        /// 2) очищаємо ScheduleMatrix (UI одразу стане “порожнім”)
        /// 3) за потреби очищаємо Preview матрицю та ключ актуальності preview
        /// 4) перераховуємо totals
        /// 5) піднімаємо MatrixChanged, щоб UI/слухачі оновилися
        /// </summary>
        internal void InvalidateGeneratedSchedule(bool clearPreviewMatrix = true)
        {
            if (SelectedBlock is null)
                return;

            // 1) Скидаємо вже згенеровані слоти
            if (SelectedBlock.Slots.Count > 0)
                SelectedBlock.Slots.Clear();

            // 2) Скидаємо основну матрицю (UI таблиця)
            ScheduleMatrix = new DataView();

            // 3) Опційно: скидаємо preview матрицю і її ключ актуальності
            if (clearPreviewMatrix)
            {
                AvailabilityPreviewMatrix = new DataView();
                _availabilityPreviewKey = null; // _availabilityPreviewKey живе в Matrix partial
            }

            // 4) Оновлюємо totals (години/кількість працівників)
            RecalculateTotals();

            // 5) Нотифікація, що матриця/стан змінився
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
