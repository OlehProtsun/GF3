using BusinessLogicLayer.Availability;
using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure.AvailabilityPreview;




namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.AvailabilityPreview — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за preview-матрицю доступності в ScheduleEdit.
    ///
    /// Сценарій:
    /// - користувач змінює AvailabilityGroup / Year / Month / Shift1 / Shift2
    /// - потрібно показати “preview availability matrix”
    /// - але робити це треба обережно:
    ///   1) debounce вже є в ScheduleEditVm на selection
    ///   2) тут додатково є pipeline з CTS + version + requestKey
    ///      щоб не запускати зайві запити і не застосовувати застарілий результат.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        // Порожні списки для “очистки preview”, щоб не алокувати нові List кожен раз
        private static readonly List<ScheduleSlotModel> EmptySlots = new();
        private static readonly List<ScheduleEmployeeModel> EmptyEmployees = new();

        // CTS для поточного запиту preview pipeline
        private CancellationTokenSource? _availabilityPreviewCts;

        // Версія pipeline (stale guard)
        private int _availabilityPreviewVersion;

        // Ключ останнього запиту, який ми стартанули або який актуальний
        private string? _availabilityPreviewRequestKey;

        /// <summary>
        /// Основний метод: оновити preview availability matrix.
        ///
        /// Логіка (дуже коротко):
        /// 1) перевіряємо чи є SelectedBlock
        /// 2) перевіряємо валідність year/month
        /// 3) перевіряємо що вибрана група
        /// 4) будуємо previewKey (groupId|year|month|shift1|shift2)
        /// 5) якщо preview вже актуальний — виходимо
        /// 6) якщо такий самий запит вже “в польоті” — виходимо
        /// 7) скасовуємо попередній pipeline і запускаємо новий:
        ///    - LoadFullAsync(group)
        ///    - перевірка month/year
        ///    - build preview employees+slots у background
        ///    - передаємо їх у ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(...)
        /// </summary>
        internal async Task UpdateAvailabilityPreviewAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var year = ScheduleEditVm.ScheduleYear;
            var month = ScheduleEditVm.ScheduleMonth;

            // Некоректний місяць/рік => очищаємо preview
            if (year < 1 || month < 1 || month > 12)
            {
                await ClearAvailabilityPreviewAsync(year, month, ct);
                return;
            }

            var selectedGroupId = ScheduleEditVm.SelectedBlock.SelectedAvailabilityGroupId;

            // Якщо група не вибрана => очищаємо preview
            if (selectedGroupId <= 0)
            {
                await ClearAvailabilityPreviewAsync(year, month, ct);
                return;
            }

            // CanonShift — робить стабільний ключ без різниці у пробілах навколо '-'
            // (щоб "09:00 - 18:00" і "09:00-18:00" вважалися одним і тим самим)
            static string CanonShift(string s)
            {
                s = (s ?? "").Trim();
                return s.Replace(" - ", "-").Replace(" -", "-").Replace("- ", "-");
            }

            var previewKey =
                $"{selectedGroupId}|{year}|{month}|{CanonShift(ScheduleEditVm.ScheduleShift1)}|{CanonShift(ScheduleEditVm.ScheduleShift2)}";

            // Якщо ScheduleEditVm вже має актуальний preview для цього ключа — просто зафіксуємо key і вийдемо
            if (ScheduleEditVm.IsAvailabilityPreviewCurrent(previewKey))
            {
                _availabilityPreviewRequestKey = previewKey;
                return;
            }

            // Якщо точно такий самий запит уже запущений і не скасований — не стартуємо ще один
            if (previewKey == _availabilityPreviewRequestKey
                && _availabilityPreviewCts != null
                && !_availabilityPreviewCts.IsCancellationRequested)
                return;

            _availabilityPreviewRequestKey = previewKey;

            // Скасовуємо попередній pipeline і стартуємо новий
            CancelAvailabilityPreviewPipeline();
            var localCts = _availabilityPreviewCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var version = ++_availabilityPreviewVersion;

            try
            {
                // 1) Витягуємо повні дані групи
                var loaded = await _availabilityGroupService
                    .LoadFullAsync(selectedGroupId, localCts.Token)
                    .ConfigureAwait(false);

                // Беремо елементи через Item1/Item2/Item3 — це стабільно працює незалежно від назв tuple-елементів
                var group = loaded.Item1;
                var members = loaded.Item2; // якщо сервіс гарантує non-null — залиш так
                var days = loaded.Item3;

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                // 2) Перевіряємо що група відповідає year/month schedule
                if (group.Year != year || group.Month != month)
                {
                    await ClearAvailabilityPreviewAsync(year, month, ct);
                    return;
                }

                // 3) shift ranges (як у твоєму поточному коді)
                (string from, string to)? shift1 = TrySplitShift(ScheduleEditVm.ScheduleShift1);
                (string from, string to)? shift2 = TrySplitShift(ScheduleEditVm.ScheduleShift2);

                // 4) Побудова preview даних — у background
                var result = await Task.Run(() =>
                    AvailabilityPreviewBuilder.Build(members, days, shift1, shift2, localCts.Token),
                    localCts.Token).ConfigureAwait(false);

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                // 5) Передаємо результат у ScheduleEditVm (він сам зробить DataTable build всередині)
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    year, month,
                    result.Slots,
                    result.Employees,
                    previewKey,
                    localCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            // Локальний helper: парсинг shift рядка в (from,to)
            (string from, string to)? TrySplitShift(string rawShift)
            {
                // Тут ми зберігаємо поведінку 1-в-1 як у твоєму поточному коді:
                // - якщо shift невалідний => повертаємо null
                // - якщо валідний => нормалізуємо і віддаємо (from,to)
                if (!TryNormalizeShiftRange(rawShift, out var normalized, out _))
                    return null;

                var parts = normalized.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Length == 2 ? (parts[0], parts[1]) : null;
            }
        }

        /// <summary>
        /// Очистити preview матрицю (показати порожню) і скинути requestKey/pipeline.
        /// </summary>
        private Task ClearAvailabilityPreviewAsync(int year, int month, CancellationToken ct)
        {
            CancelAvailabilityPreviewPipeline();
            _availabilityPreviewRequestKey = null;

            return ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                year, month,
                EmptySlots,
                EmptyEmployees,
                previewKey: $"CLEAR|{year}|{month}",
                ct);
        }

        /// <summary>
        /// Викликається, коли ми виходимо із schedule edit/profile або міняємо контекст так,
        /// що старий preview pipeline більше не має працювати.
        /// </summary>
        internal void CancelScheduleEditWork()
        {
            CancelAvailabilityPreviewPipeline();

            // Збільшуємо версію, щоб будь-які “старі” await-и вважалися застарілими
            _availabilityPreviewVersion++;

            _availabilityPreviewRequestKey = null;
        }

        /// <summary>
        /// Скасувати поточний CTS preview pipeline і звільнити ресурси.
        /// </summary>
        private void CancelAvailabilityPreviewPipeline()
        {
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            _availabilityPreviewCts = null;
        }
    }
}
