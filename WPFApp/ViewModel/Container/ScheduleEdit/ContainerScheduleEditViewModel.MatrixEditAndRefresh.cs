using DataAccessLayer.Models;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using WPFApp.Infrastructure.ScheduleMatrix;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Цей файл — “все про матрицю розкладу”.
    ///
    /// Важливо:
    /// - Це НЕ новий клас, а частина (partial) того самого ViewModel.
    /// - Тобто він має доступ до всіх полів/властивостей основного файлу:
    ///   SelectedBlock, ScheduleYear, ScheduleMonth, _owner, _logger, _colNameToEmpId,
    ///   ScheduleMatrix, AvailabilityPreviewMatrix, RecalculateTotals(), MatrixChanged і т.д.
    ///
    /// Навіщо це робити:
    /// - щоб основний файл VM не був “монстром”
    /// - щоб код матриці був в одному місці
    /// - легше підтримувати та дебажити
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        // =========================
        // 1) ПОЛЯ, ЯКІ ПОТРІБНІ ТІЛЬКИ ДЛЯ REFRESH/BUILD
        // =========================

        /// <summary>
        /// Номер “версії” побудови матриці.
        /// Кожен новий refresh збільшує це число.
        ///
        /// Навіщо:
        /// - Ми будуємо матрицю у background Task.
        /// - Поки вона будується, користувач може клікнути інший блок/місяць.
        /// - Якщо старий Task завершиться пізніше, він НЕ має права перезаписати нові дані.
        ///
        /// Тому: на старті refresh запам’ятали version -> в кінці перевірили,
        /// що version все ще актуальна. Якщо ні — просто ігноруємо результат.
        /// </summary>
        private int _scheduleBuildVersion;

        /// <summary>
        /// CTS для побудови основної матриці.
        /// Якщо користувач робить refresh знову — попередній CTS ми скасовуємо,
        /// щоб не витрачати CPU на непотрібну побудову.
        /// </summary>
        private CancellationTokenSource? _scheduleMatrixCts;

        /// <summary>
        /// CTS для preview-матриці доступності (Availability preview).
        /// Окремий токен, бо preview може оновлюватися іншим сценарієм.
        /// </summary>
        private CancellationTokenSource? _availabilityPreviewCts;

        /// <summary>
        /// Ключ “актуальності” preview матриці.
        /// Якщо ключ не змінився — ми можемо не перебудовувати preview,
        /// бо вона вже актуальна.
        /// </summary>
        private string? _availabilityPreviewKey;

        /// <summary>
        /// Версія побудови preview матриці — аналогічно до _scheduleBuildVersion,
        /// тільки для AvailabilityPreviewMatrix.
        /// </summary>
        private int _availabilityPreviewBuildVersion;


        // =========================
        // 2) ПУБЛІЧНИЙ “ТРИГЕР” REFRESH (зазвичай викликає UI)
        // =========================

        /// <summary>
        /// Простий синхронний метод, який “запускає” асинхронний refresh
        /// і спеціально НЕ чекає його.
        ///
        /// Це зручно, коли:
        /// - виклик йде з UI (клік/зміна selection)
        /// - нам не потрібно блокувати потік
        /// </summary>
        public void RefreshScheduleMatrix()
        {
            SafeForget(RefreshScheduleMatrixAsync());
        }


        // =========================
        // 3) SAFE FORGET — щоб не ловити UnobservedTaskException
        // =========================

        /// <summary>
        /// Якщо ми запускаємо Task “в нікуди”, то виняток у Task
        /// може стати Unobserved (особливо в Debug).
        ///
        /// Цей helper підписується на faulted і “зчитує” Exception,
        /// щоб середовище не вважало його “необробленим”.
        /// </summary>
        private static void SafeForget(Task task)
        {
            task.ContinueWith(t =>
            {
                // Просто торкаємось Exception, щоб вона стала observed.
                _ = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }


        // =========================
        // 4) ОСНОВНИЙ REFRESH МАТРИЦІ (ScheduleMatrix)
        // =========================

        /// <summary>
        /// Повна логіка оновлення (refresh) основної матриці:
        /// - скасувати попередню побудову
        /// - зняти “знімок” даних (slots + employees)
        /// - побудувати DataTable у background потоці
        /// - повернутись в UI thread і застосувати результат
        ///
        /// Чому знімок (ToList):
        /// - SelectedBlock.Slots та Employees — ObservableCollection
        /// - Їх не можна безпечно ітерувати у background, бо UI може змінити їх в цей момент
        /// - Тому беремо копію (snapshot) і працюємо з нею в Task.Run.
        /// </summary>
        internal async Task RefreshScheduleMatrixAsync(CancellationToken ct = default)
        {
            // 1) Піднімаємо версію побудови — це “id” цього refresh.
            int buildVer = Interlocked.Increment(ref _scheduleBuildVersion);

            // 3) Якщо попередній refresh ще будувався — скасовуємо його
            CancellationTokenSource? prev = Interlocked.Exchange(ref _scheduleMatrixCts, null);
            if (prev != null)
            {
                try { prev.Cancel(); } catch { }
                try { prev.Dispose(); } catch { }
            }

            // 4) Створюємо новий CTS, який “підв’язаний” до зовнішнього ct
            //    Тобто: якщо ct скасують ззовні — скасується і наш локальний.
            var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _scheduleMatrixCts = localCts;
            var token = localCts.Token;

            try
            {
                // 5) Якщо нема вибраного блоку або некоректні дані — просто очищаємо матрицю
                if (SelectedBlock is null ||
                    ScheduleYear < 1 || ScheduleMonth < 1 || ScheduleMonth > 12 ||
                    SelectedBlock.Slots.Count == 0)
                {
                    // Важливо: ScheduleMatrix — властивість, яка прив’язана до UI,
                    // тому встановлюємо її в UI thread.
                    await _owner.RunOnUiThreadAsync(() =>
                    {
                        ScheduleMatrix = new DataView();
                        RecalculateTotals();
                        MatrixChanged?.Invoke(this, EventArgs.Empty);
                    }).ConfigureAwait(false);

                    return;
                }

                // 6) Фіксуємо year/month, щоб вони “не поїхали” під час background роботи
                int year = ScheduleYear;
                int month = ScheduleMonth;

                // 7) Робимо snapshot даних (щоб не чіпати ObservableCollection з background)
                var slotsSnapshot = SelectedBlock.Slots.ToList();
                var employeesSnapshot = SelectedBlock.Employees.ToList();

                // 8) Виносимо важку побудову в background
                var result = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    // Тут вся “логіка побудови DataTable” сидить у ScheduleMatrixEngine.
                    // VM лише викликає engine і забирає готову таблицю.
                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year, month,
                        slotsSnapshot, employeesSnapshot,
                        out var colMap,
                        token);

                    token.ThrowIfCancellationRequested();

                    // Повертаємо:
                    // - DataView (зручно одразу для WPF DataGrid)
                    // - Map колонка -> employeeId (потрібно для редагування клітинок)
                    return (View: table.DefaultView, ColMap: colMap);
                }, token).ConfigureAwait(false);

                // 9) Якщо поки будували — стартував новий refresh або токен скасований, просто виходимо
                if (buildVer != Volatile.Read(ref _scheduleBuildVersion) || token.IsCancellationRequested)
                    return;

                // 10) Застосування результату робимо в UI thread
                await _owner.RunOnUiThreadAsync(() =>
                {

                    // 10.1) Оновлюємо мапу "колонка -> employeeId"
                    _colNameToEmpId.Clear();
                    foreach (var pair in result.ColMap)
                        _colNameToEmpId[pair.Key] = pair.Value;

                    // 10.2) Встановлюємо DataView у властивість, прив’язану до UI
                    ScheduleMatrix = result.View;

                    // 10.3) Перерахунок тоталів (години/працівники)
                    RecalculateTotals();

                    // 10.4) Нотифікація UI/слухачів
                    MatrixChanged?.Invoke(this, EventArgs.Empty);

                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Тут ти свідомо ігноруєш всі інші винятки.
                // Якщо захочеш — можна додати логування Exception.
            }
            finally
            {
                // 11) Прибираємо CTS, але ТІЛЬКИ якщо поле все ще посилається на localCts
                // (бо могло статися, що запустили новий refresh і _scheduleMatrixCts уже інший)
                if (ReferenceEquals(_scheduleMatrixCts, localCts))
                {
                    _scheduleMatrixCts = null;
                    try { localCts.Dispose(); } catch { }
                }
            }
        }


        // =========================
        // 5) REFRESH PREVIEW МАТРИЦІ (AvailabilityPreviewMatrix)
        // =========================

        /// <summary>
        /// Оновлює матрицю попереднього перегляду (preview) доступності.
        ///
        /// Вхідні параметри тут вже приходять готові (slots + employees + year/month),
        /// тому цей метод НЕ бере дані з SelectedBlock напряму.
        ///
        /// previewKey — “ключ актуальності”:
        /// якщо ключ збігається з _availabilityPreviewKey, значить preview і так актуальний.
        /// </summary>
        internal async Task RefreshAvailabilityPreviewMatrixAsync(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            string? previewKey = null,
            CancellationToken ct = default)
        {
            // Якщо ключ не передали — робимо базовий.
            var effectiveKey = previewKey ?? $"CLEAR|{year}|{month}";

            // Якщо preview вже для цього ключа — нічого не робимо
            if (effectiveKey == _availabilityPreviewKey)
                return;

            // Версія білду preview
            var buildVer = Interlocked.Increment(ref _availabilityPreviewBuildVersion);

            // Скасовуємо попередню побудову preview
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();

            var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _availabilityPreviewCts = localCts;
            var token = localCts.Token;

            try
            {
                // Побудову DataTable теж робимо у background
                var view = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    // Для preview нам не потрібен colMap
                    var table = ScheduleMatrixEngine.BuildScheduleTable(year, month, slots, employees, out _, token);
                    return table.DefaultView;
                }, token).ConfigureAwait(false);

                // Якщо стало неактуально — виходимо
                if (token.IsCancellationRequested || buildVer != Volatile.Read(ref _availabilityPreviewBuildVersion))
                    return;

                // Ставимо результат в UI thread
                await _owner.RunOnUiThreadAsync(() =>
                {
                    if (token.IsCancellationRequested || buildVer != _availabilityPreviewBuildVersion)
                        return;

                    AvailabilityPreviewMatrix = view;
                    _availabilityPreviewKey = effectiveKey;

                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // нормальна ситуація — ігноруємо
            }
            finally
            {
                if (ReferenceEquals(_availabilityPreviewCts, localCts))
                {
                    _availabilityPreviewCts = null;
                    localCts.Dispose();
                }
            }
        }


        // =========================
        // 6) EDIT: застосування введення з клітинки до слотів
        // =========================

        /// <summary>
        /// Спроба застосувати редагування однієї клітинки матриці.
        ///
        /// Вхід:
        /// - columnName: технічна назва колонки (наприклад "emp_12")
        /// - day: день місяця (1..31)
        /// - rawInput: текст, який ввів користувач (наприклад "09:00 - 12:00, 13:00 - 15:00")
        ///
        /// Вихід:
        /// - normalizedValue: текст, який ми хочемо показати після нормалізації
        /// - error: пояснення помилки (якщо введення невалідне)
        ///
        /// Що робиться всередині:
        /// 1) з colNameToEmpId знаходимо employeeId, кому належить колонка
        /// 2) парсимо інтервали через ScheduleMatrixEngine.TryParseIntervals
        /// 3) застосовуємо до SelectedBlock.Slots через ScheduleMatrixEngine.ApplyIntervalsToSlots
        /// 4) оновлюємо DataTable “на місці”, щоб не перебудовувати всю матрицю
        /// 5) оновлюємо totals (години)
        /// </summary>
        public bool TryApplyMatrixEdit(string columnName, int day, string rawInput, out string normalizedValue, out string? error)
        {
            normalizedValue = rawInput;
            error = null;

            // Без вибраного блоку — редагувати нікуди
            if (SelectedBlock is null)
                return false;

            // Колонка -> employeeId
            // Якщо немає в мапі — значить це не “колонка працівника”
            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return false;

            // Парсимо введення користувача
            // Якщо формат неправильний — повертаємо error
            if (!ScheduleMatrixEngine.TryParseIntervals(rawInput, out var intervals, out error))
                return false;

            // Застосовуємо список інтервалів до слотів блоку.
            // Engine сам видалить старі слоти (day+empId) і поставить нові.
            ScheduleMatrixEngine.ApplyIntervalsToSlots(
                scheduleId: SelectedBlock.Model.Id,
                slots: SelectedBlock.Slots,
                day: day,
                empId: empId,
                intervals: intervals);

            // Нормалізуємо відображення (що показувати в клітинці)
            normalizedValue = intervals.Count == 0
                ? EmptyMark
                : string.Join(", ", intervals.Select(i => $"{i.from} - {i.to}"));

            // Оновлюємо конкретну клітинку в DataTable (без повного rebuild)
            UpdateMatrixCellInPlace(columnName, day, normalizedValue);

            // Перераховуємо totals (години)
            RecalculateTotals();

            return true;
        }


        // =========================
        // 7) EDIT SUPPORT: оновити 1 клітинку і conflict в DataTable
        // =========================

        /// <summary>
        /// Оновлює конкретну клітинку в DataTable, яка вже показується у UI.
        ///
        /// Навіщо:
        /// - після редагування ми не хочемо перебудовувати всю матрицю (це дорого)
        /// - простіше змінити 1 значення у DataRow
        ///
        /// Додатково:
        /// - перераховуємо Conflict для цього дня, щоб UI одразу показав “проблему”
        /// </summary>
        private void UpdateMatrixCellInPlace(string columnName, int day, string normalizedValue)
        {
            // Беремо DataTable з поточного DataView
            var table = ScheduleMatrix?.Table;
            if (table == null || table.Rows.Count == 0)
                return;

            DataRow? row = null;

            // Оптимізація:
            // у твоїй таблиці рядки йдуть як правило “1 день => row[0]”, “2 день => row[1]”
            // тому пробуємо fast path: day-1 індекс.
            if (day >= 1 && day <= table.Rows.Count)
            {
                var r = table.Rows[day - 1];
                try
                {
                    if (Convert.ToInt32(r[DayColumnName], CultureInfo.InvariantCulture) == day)
                        row = r;
                }
                catch { /* якщо дані криві — просто перейдемо на fallback */ }
            }

            // Fallback:
            // якщо з якоїсь причини fast path не попав у потрібний рядок,
            // шукаємо рядок перебором.
            if (row == null)
            {
                foreach (DataRow r in table.Rows)
                {
                    try
                    {
                        if (Convert.ToInt32(r[DayColumnName], CultureInfo.InvariantCulture) == day)
                        {
                            row = r;
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (row == null)
                return;

            // Власне оновлення клітинки
            row[columnName] = normalizedValue;

            // Перерахунок Conflict для цього дня:
            // - слот без працівника
            // - перетини інтервалів у працівника
            if (SelectedBlock != null)
                row[ConflictColumnName] = ScheduleMatrixEngine.ComputeConflictForDay(SelectedBlock.Slots, day);
        }


        // =========================
        // 8) ДРІБНІ МЕТОДИ ПІДТРИМКИ REFRESH-ЛОГІКИ
        // =========================

        /// <summary>
        /// Скасувати все background, що стосується матриці.
        /// Зручно викликати, коли закриваємо форму або міняємо контекст.
        /// </summary>
        internal void CancelBackgroundWork()
        {
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            _availabilityPreviewCts = null;

            _scheduleMatrixCts?.Cancel();
            _scheduleMatrixCts?.Dispose();
            _scheduleMatrixCts = null;
        }

        /// <summary>
        /// Перевірка: чи preview матриця відповідає конкретному ключу.
        /// Наприклад, інший блок/інший місяць — ключ буде інший => preview неактуальний.
        /// </summary>
        internal bool IsAvailabilityPreviewCurrent(string? previewKey)
        {
            if (string.IsNullOrWhiteSpace(previewKey))
                return false;

            return string.Equals(_availabilityPreviewKey, previewKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Коли вибір блоку змінився — ми очищаємо обидві матриці і totals.
        /// Це “скидання UI стану” перед новими даними.
        /// </summary>
        private void RestoreMatricesForSelection()
        {
            AvailabilityPreviewMatrix = new DataView();
            ScheduleMatrix = new DataView();
            _availabilityPreviewKey = null;

            RecalculateTotals();
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
