/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.Binds у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFApp.UI.Hotkeys;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal async Task AddBindAsync(CancellationToken ct = default)
        {
            
            var draftKey = $"__draft__{Guid.NewGuid():N}";

            var draft = new BindModel
            {
                Key = draftKey,
                Value = string.Empty,
                IsActive = false
            };

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                
                await _bindService.CreateAsync(draft, uiToken);

                
                await LoadBindsAsync(uiToken);

                
                var row = EditVm.Binds.FirstOrDefault(b => b.Key == draftKey);
                if (row != null)
                {
                    row.Key = string.Empty;
                    row.Value = string.Empty;
                    row.IsActive = true;
                    EditVm.SelectedBind = row;
                }

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }
        internal async Task FlashNavWorkingSuccessAsync(CancellationToken ct = default, int successMs = 550)
        {
            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                
                await Task.Delay(120, uiToken);
                await ShowNavSuccessThenAutoHideAsync(uiToken, successMs);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
        }

        internal async Task DeleteBindAsync(CancellationToken ct = default)
        {
            
            var bind = EditVm.SelectedBind;
            if (bind is null)
                return;

            
            if (!Confirm($"Delete bind '{bind.Key}'?", "Confirm"))
                return;

            
            if (bind.Id == 0)
            {
                EditVm.Binds.Remove(bind);
                EditVm.SelectedBind = null;

                await FlashNavWorkingSuccessAsync(ct, successMs: 500);
                return;
            }

            var uiToken = ResetNavUiCts(ct);

            await ShowNavWorkingAsync();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                
                await _bindService.DeleteAsync(bind.Id, uiToken);

                
                await LoadBindsAsync(uiToken);

                
                EditVm.SelectedBind = null;

                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await ShowNavSuccessThenAutoHideAsync(uiToken, 700);
            }
            catch (OperationCanceledException)
            {
                await HideNavStatusAsync();
            }
            catch (Exception ex)
            {
                await HideNavStatusAsync();
                ShowError(ex);
            }
        }
        internal async Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
        {
            
            if (bind is null)
                return;

            
            if (string.IsNullOrWhiteSpace(bind.Key) && string.IsNullOrWhiteSpace(bind.Value))
                return;

            
            if (string.IsNullOrWhiteSpace(bind.Key) || string.IsNullOrWhiteSpace(bind.Value))
                return;

            
            if (!KeyGestureTextHelper.TryNormalizeKey(bind.Key, out var normalizedKey))
            {
                ShowError("Invalid hotkey format.");
                return;
            }

            
            bind.Key = normalizedKey;

            
            var model = bind.ToModel();

            
            var selectId = bind.Id;            
            var selectKey = normalizedKey;     

            try
            {
                
                if (bind.Id == 0)
                    await _bindService.CreateAsync(model, ct);
                else
                    await _bindService.UpdateAsync(model, ct);

                
                await LoadBindsAsync(ct);

                
                
                
                var restored = selectId > 0
                    ? EditVm.Binds.FirstOrDefault(b => b.Id == selectId)
                    : EditVm.Binds.FirstOrDefault(b => string.Equals(b.Key, selectKey, StringComparison.OrdinalIgnoreCase));

                if (restored != null)
                    EditVm.SelectedBind = restored;
            }
            catch (Exception ex)
            {
                
                ShowError(ex);
            }
        }

        internal string? FormatKeyGesture(Key key, ModifierKeys modifiers)
        {
            
            return KeyGestureTextHelper.FormatKeyGesture(key, modifiers);
        }

        internal bool TryNormalizeKey(string raw, out string normalized)
        {
            
            return KeyGestureTextHelper.TryNormalizeKey(raw, out normalized);
        }
    }
}
