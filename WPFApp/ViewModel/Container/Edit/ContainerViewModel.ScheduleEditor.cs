/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.ScheduleEditor у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        
        
        
        
        
        
        
        
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

            
            if (block.Employees.Any(e => e.EmployeeId == employee.Id))
            {
                ShowInfo("This employee is already added.");
                return;
            }

            
            block.Employees.Add(new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                Employee = employee
            });

            
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

            
            await UpdateAvailabilityPreviewAsync(ct);
        }

        
        
        
        
        
        
        
        
        
        
        
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

            
            block.Employees.Remove(toRemove);

            
            var slotsToRemove = block.Slots
                .Where(s => s.EmployeeId == selected.EmployeeId)
                .ToList();

            foreach (var slot in slotsToRemove)
                block.Slots.Remove(slot);

            
            ScheduleEditVm.RemoveCellStylesForEmployee(selected.EmployeeId);

            
            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
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

            
            var containerId = ScheduleEditVm.SelectedBlock.Model.ContainerId;

            var block = CreateDefaultBlock(containerId);
            ScheduleEditVm.Blocks.Add(block);
            ScheduleEditVm.SelectedBlock = block;

            await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);
            await UpdateAvailabilityPreviewAsync(ct);
        }

        
        
        
        
        
        
        
        
        
        internal Task SelectScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (block is null || !ScheduleEditVm.Blocks.Contains(block))
                return Task.CompletedTask;

            if (ReferenceEquals(ScheduleEditVm.SelectedBlock, block))
                return Task.CompletedTask;

            return UiOperationRunner.RunNavStatusFlowAsync(
                ct,
                ResetNavUiCts,
                ShowNavWorkingAsync,
                WaitForUiIdleAsync,
                async uiToken =>
                {
                    await RunOnUiThreadAsync(() =>
                    {
                        ScheduleEditVm.SelectedBlock = block;
                        ScheduleEditVm.ClearValidationErrors();
                    }).ConfigureAwait(false);

                    await ScheduleEditVm.RefreshScheduleMatrixAsync(uiToken).ConfigureAwait(false);
                    await UpdateAvailabilityPreviewAsync(uiToken).ConfigureAwait(false);
                },
                ShowNavSuccessThenAutoHideAsync,
                HideNavStatusAsync,
                ShowError,
                successDelayMs: 500);
        }


        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal async Task CloseScheduleBlockAsync(ScheduleBlockViewModel block, CancellationToken ct = default)
        {
            if (!ScheduleEditVm.Blocks.Contains(block))
                return;

            if (!Confirm("Are you sure you want to close this schedule?"))
                return;

            var index = ScheduleEditVm.Blocks.IndexOf(block);
            ScheduleEditVm.Blocks.Remove(block);

            
            if (ScheduleEditVm.Blocks.Count == 0)
            {
                ScheduleEditVm.SelectedBlock = null;

                await ScheduleEditVm.RefreshScheduleMatrixAsync(ct);

                
                await ScheduleEditVm.RefreshAvailabilityPreviewMatrixAsync(
                    block.Model.Year, block.Model.Month,
                    new System.Collections.Generic.List<ScheduleSlotModel>(),
                    new System.Collections.Generic.List<ScheduleEmployeeModel>(),
                    previewKey: $"CLEAR|{block.Model.Year}|{block.Model.Month}",
                    ct);

                return;
            }

            
            var nextIndex = Math.Max(0, Math.Min(index, ScheduleEditVm.Blocks.Count - 1));
            var next = ScheduleEditVm.Blocks[nextIndex];

            await SelectScheduleBlockAsync(next, ct);
        }
    }
}
