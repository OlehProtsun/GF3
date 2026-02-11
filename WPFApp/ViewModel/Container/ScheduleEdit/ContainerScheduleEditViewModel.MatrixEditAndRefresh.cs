using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Infrastructure.ScheduleMatrix;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Цей файл — “все про матрицю розкладу”.
    ///
    /// Важливо:
    /// - Це НЕ новий клас, а частина (partial) того самого ViewModel.
    /// - Тобто він має доступ до всіх полів/властивостей основного файлу:
    ///   SelectedBlock, ScheduleYear, ScheduleMonth, _owner, _colNameToEmpId,
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
        /// </summary>
        private int _scheduleBuildVersion;

        /// <summary>
        /// CTS для побудови основної матриці.
        /// </summary>
        private CancellationTokenSource? _scheduleMatrixCts;

        /// <summary>
        /// CTS для preview-матриці доступності (Availability preview).
        /// </summary>
        private CancellationTokenSource? _availabilityPreviewCts;

        /// <summary>
        /// Ключ “актуальності” preview матриці.
        /// </summary>
        private string? _availabilityPreviewKey;

        /// <summary>
        /// Версія побудови preview матриці.
        /// </summary>
        private int _availabilityPreviewBuildVersion;


        // =========================
        // 2) ПУБЛІЧНИЙ “ТРИГЕР” REFRESH (зазвичай викликає UI)
        // =========================

        /// <summary>
        /// Простий синхронний метод, який “запускає” асинхронний refresh і спеціально НЕ чекає його.
        /// </summary>
        public void RefreshScheduleMatrix()
        {
            SafeForget(RefreshScheduleMatrixAsync());
        }


        // =========================
        // 3) SAFE FORGET — щоб не ловити UnobservedTaskException
        // =========================

        /// <summary>
        /// Якщо ми запускаємо Task “в нікуди”, то виняток у Task може стати Unobserved.
        /// </summary>
        private static void SafeForget(Task task)
        {
            _ = task.ContinueWith(t =>
            {
                // робимо exception observed
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
        /// </summary>
        internal async Task RefreshScheduleMatrixAsync(CancellationToken ct = default)
        {
            int buildVer = Interlocked.Increment(ref _scheduleBuildVersion);

            // Скасовуємо попередній refresh (thread-safe)
            var prev = Interlocked.Exchange(ref _scheduleMatrixCts, null);
            if (prev != null)
            {
                try { prev.Cancel(); } catch { }
                try { prev.Dispose(); } catch { }
            }

            // Фіксуємо selected block на момент старту (щоб не читати SelectedBlock багато разів)
            var block = SelectedBlock;

            // Якщо нема блоку / некоректний місяць — очищаємо матрицю
            if (block is null ||
                ScheduleYear < 1 ||
                ScheduleMonth < 1 || ScheduleMonth > 12)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    ScheduleMatrix = new DataView();
                    RecalculateTotals();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);

                return;
            }

            // Якщо слотів 0 — теж очищаємо (твоя початкова логіка)
            if (block.Slots.Count == 0)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    ScheduleMatrix = new DataView();
                    RecalculateTotals();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);
                }).ConfigureAwait(false);

                return;
            }

            var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _scheduleMatrixCts = localCts;
            var token = localCts.Token;

            // Фіксуємо year/month
            int year = ScheduleYear;
            int month = ScheduleMonth;

            // Snapshot (щоб не чіпати ObservableCollection з background)
            var slotsSnapshot = block.Slots.ToList();
            var employeesSnapshot = block.Employees.ToList();

            // Для перевірки “той самий блок” під час apply
            var scheduleIdSnapshot = block.Model.Id;

            try
            {
                var result = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var table = ScheduleMatrixEngine.BuildScheduleTable(
                        year, month,
                        slotsSnapshot, employeesSnapshot,
                        out var colMap,
                        token);

                    token.ThrowIfCancellationRequested();

                    return (View: table.DefaultView, ColMap: colMap);
                }, token).ConfigureAwait(false);

                // Якщо стало неактуально — виходимо
                if (buildVer != Volatile.Read(ref _scheduleBuildVersion) || token.IsCancellationRequested)
                    return;

                await _owner.RunOnUiThreadAsync(() =>
                {
                    // Перевірка: чи SelectedBlock все ще той самий schedule
                    var currentBlock = SelectedBlock;
                    if (currentBlock is null || currentBlock.Model.Id != scheduleIdSnapshot)
                        return;

                    // Оновлюємо мапу "колонка -> employeeId"
                    _colNameToEmpId.Clear();
                    foreach (var pair in result.ColMap)
                        _colNameToEmpId[pair.Key] = pair.Value;

                    // Ставимо DataView в UI
                    ScheduleMatrix = result.View;

                    // Перерахунок Conflict на старті (щоб крапки були правильні одразу після build)
                    var m = currentBlock.Model;
                    foreach (DataRowView rv in ScheduleMatrix)
                    {
                        int day = 0;
                        try { day = Convert.ToInt32(rv[DayColumnName]); }
                        catch { continue; }

                        rv[ConflictColumnName] = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
                            currentBlock.Slots,
                            day,
                            m.PeoplePerShift,
                            m.Shift1Time,
                            m.Shift2Time);
                    }

                    RecalculateTotals();
                    MatrixChanged?.Invoke(this, EventArgs.Empty);

                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // нормальна ситуація
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshScheduleMatrixAsync failed: {ex}");
            }
            finally
            {
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
        /// </summary>
        internal async Task RefreshAvailabilityPreviewMatrixAsync(
            int year,
            int month,
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            string? previewKey = null,
            CancellationToken ct = default)
        {
            var effectiveKey = previewKey ?? $"CLEAR|{year}|{month}";

            if (effectiveKey == _availabilityPreviewKey)
                return;

            var buildVer = Interlocked.Increment(ref _availabilityPreviewBuildVersion);

            // Скасовуємо попередню побудову preview (thread-safe)
            var prev = Interlocked.Exchange(ref _availabilityPreviewCts, null);
            if (prev != null)
            {
                try { prev.Cancel(); } catch { }
                try { prev.Dispose(); } catch { }
            }

            var localCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _availabilityPreviewCts = localCts;
            var token = localCts.Token;

            try
            {
                var view = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var table = ScheduleMatrixEngine.BuildScheduleTable(year, month, slots, employees, out _, token);
                    return table.DefaultView;

                }, token).ConfigureAwait(false);

                if (token.IsCancellationRequested || buildVer != Volatile.Read(ref _availabilityPreviewBuildVersion))
                    return;

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
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshAvailabilityPreviewMatrixAsync failed: {ex}");
            }
            finally
            {
                if (ReferenceEquals(_availabilityPreviewCts, localCts))
                {
                    _availabilityPreviewCts = null;
                    try { localCts.Dispose(); } catch { }
                }
            }
        }


        // =========================
        // 6) EDIT: застосування введення з клітинки до слотів
        // =========================

        /// <summary>
        /// Спроба застосувати редагування однієї клітинки матриці.
        /// </summary>
        public bool TryApplyMatrixEdit(string columnName, int day, string rawInput, out string normalizedValue, out string? error)
        {
            normalizedValue = rawInput;
            error = null;

            var block = SelectedBlock;
            if (block is null)
                return false;

            if (!_colNameToEmpId.TryGetValue(columnName, out var empId))
                return false;

            if (!ScheduleMatrixEngine.TryParseIntervals(rawInput, out var intervals, out error))
                return false;

            ScheduleMatrixEngine.ApplyIntervalsToSlots(
                scheduleId: block.Model.Id,
                slots: block.Slots,
                day: day,
                empId: empId,
                intervals: intervals);

            normalizedValue = intervals.Count == 0
                ? EmptyMark
                : string.Join(", ", intervals.Select(i => $"{i.from} - {i.to}"));

            UpdateMatrixCellInPlace(columnName, day, normalizedValue);

            RecalculateTotals();
            return true;
        }


        // =========================
        // 7) EDIT SUPPORT: оновити 1 клітинку і conflict в DataTable
        // =========================

        /// <summary>
        /// Оновлює конкретну клітинку в DataTable + перераховує Conflict для цього дня.
        /// </summary>
        private void UpdateMatrixCellInPlace(string columnName, int day, string normalizedValue)
        {
            var view = ScheduleMatrix; // DataView
            if (view == null || view.Count == 0)
                return;

            DataRowView? rv = null;

            // fast path: day-1 (якщо рядки відсортовані по днях)
            if (day >= 1 && day <= view.Count)
            {
                var candidate = view[day - 1];
                try
                {
                    if (Convert.ToInt32(candidate[DayColumnName]) == day)
                        rv = candidate;
                }
                catch
                {
                    // ignore
                }
            }

            // fallback scan
            if (rv == null)
            {
                foreach (DataRowView r in view)
                {
                    try
                    {
                        if (Convert.ToInt32(r[DayColumnName]) == day)
                        {
                            rv = r;
                            break;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (rv == null)
                return;

            rv.BeginEdit();
            try
            {
                rv[columnName] = normalizedValue;

                var block = SelectedBlock;
                if (block != null)
                {
                    var m = block.Model;

                    rv[ConflictColumnName] = ScheduleMatrixEngine.ComputeConflictForDayWithStaffing(
                        block.Slots,
                        day,
                        m.PeoplePerShift,
                        m.Shift1Time,
                        m.Shift2Time);
                }
            }
            finally
            {
                rv.EndEdit();
            }
        }


        // =========================
        // 8) ДРІБНІ МЕТОДИ ПІДТРИМКИ REFRESH-ЛОГІКИ
        // =========================

        /// <summary>
        /// Скасувати все background, що стосується матриці.
        /// </summary>
        internal void CancelBackgroundWork()
        {
            var prevPreview = Interlocked.Exchange(ref _availabilityPreviewCts, null);
            if (prevPreview != null)
            {
                try { prevPreview.Cancel(); } catch { }
                try { prevPreview.Dispose(); } catch { }
            }

            var prevMain = Interlocked.Exchange(ref _scheduleMatrixCts, null);
            if (prevMain != null)
            {
                try { prevMain.Cancel(); } catch { }
                try { prevMain.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Перевірка: чи preview матриця відповідає конкретному ключу.
        /// </summary>
        internal bool IsAvailabilityPreviewCurrent(string? previewKey)
        {
            if (string.IsNullOrWhiteSpace(previewKey))
                return false;

            return string.Equals(_availabilityPreviewKey, previewKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Коли вибір блоку змінився — ми очищаємо обидві матриці і totals.
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
