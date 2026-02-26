/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.AvailabilityPreview у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.Applications.Preview;




namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        private static readonly List<ScheduleSlotModel> EmptySlots = new();
        private static readonly List<ScheduleEmployeeModel> EmptyEmployees = new();

        
        private CancellationTokenSource? _availabilityPreviewCts;

        
        private int _availabilityPreviewVersion;

        
        private string? _availabilityPreviewRequestKey;

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal async Task UpdateAvailabilityPreviewAsync(CancellationToken ct = default)
        {
            if (ScheduleEditVm.SelectedBlock is null)
                return;

            var year = ScheduleEditVm.ScheduleYear;
            var month = ScheduleEditVm.ScheduleMonth;

            
            if (year < 1 || month < 1 || month > 12)
            {
                await ClearAvailabilityPreviewAsync(year, month, ct);
                return;
            }

            var selectedGroupId = ScheduleEditVm.SelectedBlock.SelectedAvailabilityGroupId;

            
            if (selectedGroupId <= 0)
            {
                await ClearAvailabilityPreviewAsync(year, month, ct);
                return;
            }

            
            
            static string CanonShift(string s)
            {
                s = (s ?? "").Trim();
                return s.Replace(" - ", "-").Replace(" -", "-").Replace("- ", "-");
            }

            var previewKey =
                $"{selectedGroupId}|{year}|{month}|{CanonShift(ScheduleEditVm.ScheduleShift1)}|{CanonShift(ScheduleEditVm.ScheduleShift2)}";

            
            if (ScheduleEditVm.IsAvailabilityPreviewCurrent(previewKey))
            {
                _availabilityPreviewRequestKey = previewKey;
                return;
            }

            
            if (previewKey == _availabilityPreviewRequestKey
                && _availabilityPreviewCts != null
                && !_availabilityPreviewCts.IsCancellationRequested)
                return;

            _availabilityPreviewRequestKey = previewKey;

            
            CancelAvailabilityPreviewPipeline();
            var localCts = _availabilityPreviewCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var version = ++_availabilityPreviewVersion;

            try
            {
                
                var loaded = await _availabilityGroupService
                    .LoadFullAsync(selectedGroupId, localCts.Token)
                    .ConfigureAwait(false);

                
                var group = loaded.Item1;
                var members = loaded.Item2; 
                var days = loaded.Item3;

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                
                if (group.Year != year || group.Month != month)
                {
                    await ClearAvailabilityPreviewAsync(year, month, ct);
                    return;
                }

                
                (string from, string to)? shift1 = TrySplitShift(ScheduleEditVm.ScheduleShift1);
                (string from, string to)? shift2 = TrySplitShift(ScheduleEditVm.ScheduleShift2);

                
                var result = await Task.Run(() =>
                    AvailabilityPreviewBuilder.Build(members, days, shift1, shift2, localCts.Token),
                    localCts.Token).ConfigureAwait(false);

                if (localCts.IsCancellationRequested || version != _availabilityPreviewVersion)
                    return;

                
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

            
            (string from, string to)? TrySplitShift(string rawShift)
            {
                
                
                
                if (!TryNormalizeShiftRange(rawShift, out var normalized, out _))
                    return null;

                var parts = normalized.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Length == 2 ? (parts[0], parts[1]) : null;
            }
        }

        
        
        
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

        
        
        
        
        internal void CancelScheduleEditWork()
        {
            CancelAvailabilityPreviewPipeline();

            
            _availabilityPreviewVersion++;

            _availabilityPreviewRequestKey = null;
        }

        
        
        
        private void CancelAvailabilityPreviewPipeline()
        {
            _availabilityPreviewCts?.Cancel();
            _availabilityPreviewCts?.Dispose();
            _availabilityPreviewCts = null;
        }
    }
}
