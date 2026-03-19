/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.MatrixEditAndRefresh у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        

        
        
        
        
        private int _scheduleBuildVersion;

        
        
        
        private CancellationTokenSource? _scheduleMatrixCts;

        
        
        
        private CancellationTokenSource? _availabilityPreviewCts;

        
        
        
        private string? _availabilityPreviewKey;

        
        
        
        private int _availabilityPreviewBuildVersion;


        
        
        

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void RefreshScheduleMatrix()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void RefreshScheduleMatrix()
        {
            SafeForget(RefreshScheduleMatrixAsync());
        }


        
        
        

        
        
        
        private static void SafeForget(Task task)
        {
            _ = task.ContinueWith(t =>
            {
                
                _ = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }


        
        
        

        
        
        
        
        
        
        
        internal async Task RefreshScheduleMatrixAsync(CancellationToken ct = default)
        {
            int buildVer = Interlocked.Increment(ref _scheduleBuildVersion);

            
            var prev = Interlocked.Exchange(ref _scheduleMatrixCts, null);
            if (prev != null)
            {
                try { prev.Cancel(); } catch { }
                try { prev.Dispose(); } catch { }
            }

            
            var block = SelectedBlock;

            
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

            
            int year = ScheduleYear;
            int month = ScheduleMonth;

            
            var slotsSnapshot = block.Slots.ToList();
            var employeesSnapshot = block.Employees.ToList();

            
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

                
                if (buildVer != Volatile.Read(ref _scheduleBuildVersion) || token.IsCancellationRequested)
                    return;

                await _owner.RunOnUiThreadAsync(() =>
                {
                    
                    var currentBlock = SelectedBlock;
                    if (currentBlock is null || currentBlock.Model.Id != scheduleIdSnapshot)
                        return;

                    
                    _colNameToEmpId.Clear();
                    foreach (var pair in result.ColMap)
                        _colNameToEmpId[pair.Key] = pair.Value;

                    
                    ScheduleMatrix = result.View;

                    
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
                
            }
            catch (Exception ex)
            {
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
                    if (token.IsCancellationRequested)
                        return (System.Data.DataView?)null;

                    try
                    {
                        var table = ScheduleMatrixEngine.BuildScheduleTable(
                            year, month, slots, employees, out _, token);

                        return token.IsCancellationRequested ? null : table.DefaultView;
                    }
                    catch (OperationCanceledException)
                    {
                        
                        return null;
                    }

                }, CancellationToken.None).ConfigureAwait(false);

                
                if (view == null)
                    return;

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
            catch (Exception ex)
            {
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


        
        
        

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public bool TryApplyMatrixEdit(string columnName, int day, string rawInput, out string normalizedValue, out string? erro` та контракт його використання у шарі WPFApp.
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


        
        
        

        
        
        
        private void UpdateMatrixCellInPlace(string columnName, int day, string normalizedValue)
        {
            var view = ScheduleMatrix; 
            if (view == null || view.Count == 0)
                return;

            DataRowView? rv = null;

            
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
                    
                }
            }

            
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

        
        
        
        internal bool IsAvailabilityPreviewCurrent(string? previewKey)
        {
            if (string.IsNullOrWhiteSpace(previewKey))
                return false;

            return string.Equals(_availabilityPreviewKey, previewKey, StringComparison.Ordinal);
        }

        
        
        
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
