/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.Validation у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void ClearValidationErrors()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ClearValidationErrors()
        {
            _validation.ClearAll();

            
            
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        
        
        
        
        
        
        
        
        private void ClearValidationErrors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            _validation.Clear(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        
        
        
        
        
        
        
        
        private void AddValidationError(string propertyName, string message)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            _validation.Add(propertyName, message);
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            _validation.SetMany(errors);
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        
        
        
        
        
        
        
        
        
        
        

        
        


        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private bool ValidateBeforeSave(
            bool showDialog = true,
            bool requireShift2 = false,
            bool requireAvailabilityGroup = false)
        {
            ApplyPendingSelectionsForValidation();

            
            if (SelectedBlock?.Model is not ScheduleModel model)
            {
                ClearValidationErrors();

                if (showDialog)
                {
                    CustomMessageBox.Show(
                        "Validation",
                        "Please add or select a schedule block first.",
                        CustomMessageBoxIcon.Error,
                        okText: "OK");
                }

                return false;
            }

            
            var raw = ScheduleValidationRules.ValidateAll(model);

            
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in raw)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (!errors.ContainsKey(vmKey))
                    errors[vmKey] = kv.Value;
            }

            
            
            if (requireAvailabilityGroup)
            {
                if (SelectedAvailabilityGroup is null || SelectedAvailabilityGroup.Id <= 0)
                {
                    errors[nameof(SelectedAvailabilityGroup)] = "Please select an availability group.";
                }
            }

            if (requireShift2)
            {
                
                if (string.IsNullOrWhiteSpace(model.Shift2Time))
                {
                    errors[nameof(ScheduleShift2)] = "Shift 2 time is required (example: 09:00 - 18:00).";
                }
            }

            
            if (SelectedAvailabilityGroup is not null
                && TryGetAvailabilityGroupPeriod(SelectedAvailabilityGroup, out var gy, out var gm)
                && model.Year > 0 && model.Month is >= 1 and <= 12
                && (gy != model.Year || gm != model.Month))
            {
                errors[nameof(SelectedAvailabilityGroup)] =
                    $"Selected availability group is for {gy:D4}-{gm:D2}, but schedule is {model.Year:D4}-{model.Month:D2}.";
            }

            
            if (SelectedBlock.Employees
                    .Where(e => e != null && IsAvailabilityEmployee(e.EmployeeId))
                    .Any(e => e.MinHoursMonth < 1))
            {
                errors[nameof(ScheduleEmployees)] = "Min hours per employee must be at least 1.";
            }


            SetValidationErrors(errors);

            if (showDialog && HasErrors)
            {
                CustomMessageBox.Show(
                    "Validation",
                    BuildValidationSummary(errors),
                    CustomMessageBoxIcon.Error,
                    okText: "OK");
            }

            return !HasErrors;
        }
        
        
        
        
        
        private void ApplyPendingSelectionsForValidation()
        {
            if (SelectedBlock is null)
                return;

            
            var shopId = SelectedShop?.Id ?? 0;
            if (ScheduleShopId != shopId)
                ScheduleShopId = shopId;

            var groupId = SelectedAvailabilityGroup?.Id ?? 0;
            if (SelectedBlock.SelectedAvailabilityGroupId != groupId)
            {
                SelectedBlock.SelectedAvailabilityGroupId = groupId;
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);
            }
        }

        
        
        
        
        private async Task SaveWithValidationAsync()
        {
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(showDialog: true));

            if (!ok)
                return;

            await _owner.SaveScheduleAsync().ConfigureAwait(false);
        }               
                        
                        
        private async Task GenerateWithValidationAsync()
        {
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(
                    showDialog: true,
                    requireShift2: true,
                    requireAvailabilityGroup: true));


            if (!ok)
                return;

            await _owner.GenerateScheduleAsync().ConfigureAwait(false);

            await Application.Current.Dispatcher
                .InvokeAsync(async () => await RefreshScheduleMatrixAsync());
        }


        private static string MapValidationKeyToVm(string key)
        {
            return key switch
            {
                
                ScheduleValidationRules.K_ScheduleShopId => nameof(SelectedShop),

                _ => key
            };
        }


        private static string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)
        {
            var sb = new StringBuilder();

            foreach (var msg in errors.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                sb.AppendLine(msg);

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? "Please check the input values." : text;
        }



    }
}
