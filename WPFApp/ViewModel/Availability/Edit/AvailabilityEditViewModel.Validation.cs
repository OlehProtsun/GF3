/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Validation у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using WPFApp.Applications.Matrix.Availability;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel : INotifyDataErrorInfo` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel : INotifyDataErrorInfo
    {
        
        
        

        /// <summary>
        /// Визначає публічний елемент `public bool HasErrors => _validation.HasErrors;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool HasErrors => _validation.HasErrors;

        /// <summary>
        /// Визначає публічний елемент `public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            
            add => _validation.ErrorsChanged += value;
            remove => _validation.ErrorsChanged -= value;
        }

        /// <summary>
        /// Визначає публічний елемент `public IEnumerable GetErrors(string? propertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public IEnumerable GetErrors(string? propertyName)
            
            => _validation.GetErrors(propertyName);

        
        
        

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            
            ClearValidationErrors();

            
            var rawErrors = AvailabilityValidationRules.ValidateAll(
                name: AvailabilityName,
                year: AvailabilityYear,
                month: AvailabilityMonth);

            
            SetValidationErrors(rawErrors);

            
            if (showDialog && HasErrors)
            {
                CustomMessageBox.Show(
                    "Validation",
                    BuildValidationSummary(rawErrors),
                    CustomMessageBoxIcon.Error,
                    okText: "OK");
            }

            return !HasErrors;
        }

        private void ValidateProperty(string propertyName)
        {
            
            var msg = AvailabilityValidationRules.ValidateProperty(
                name: AvailabilityName,
                year: AvailabilityYear,
                month: AvailabilityMonth,
                vmPropertyName: propertyName);

            
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ClearValidationErrors(string propertyName)
        {
            
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            
            _validation.Clear(propertyName);

            
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            
            if (Application.Current?.Dispatcher is not null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => SetValidationErrors(errors));
                return;
            }

            
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            
            var mapped = ValidationDictionaryHelper.RemapFirstErrors(errors, MapValidationKeyToVm);

            if (mapped.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            
            _validation.SetMany(mapped);

            
            OnPropertyChanged(nameof(HasErrors));

            
            
            foreach (var key in mapped.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    OnPropertyChanged(key);
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public void ClearValidationErrors()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ClearValidationErrors()
        {
            
            _validation.ClearAll();

            
            OnPropertyChanged(nameof(HasErrors));
        }

        private static string MapValidationKeyToVm(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = ValidationDictionaryHelper.NormalizeLastSegment(key);

            return key switch
            {
                
                "AvailabilityName" => nameof(AvailabilityName),
                "AvailabilityMonth" => nameof(AvailabilityMonth),
                "AvailabilityYear" => nameof(AvailabilityYear),

                
                "Name" => nameof(AvailabilityName),
                "Month" => nameof(AvailabilityMonth),
                "Year" => nameof(AvailabilityYear),

                _ => key
            };
        }

        private static string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)
        {
            if (errors is null || errors.Count == 0)
                return "Please check the input values.";

            var sb = new StringBuilder();

            foreach (var msg in errors.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                sb.AppendLine(msg);

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? "Please check the input values." : text;
        }

        
        internal void ShowValidationErrorsDialog(IReadOnlyDictionary<string, string> errors)
        {
            CustomMessageBox.Show(
                "Validation",
                BuildValidationSummary(errors),
                CustomMessageBoxIcon.Error,
                okText: "OK");
        }

        
        
        

        private void GroupTable_ColumnChanged(object? sender, DataColumnChangeEventArgs e)
        {
            
            if (_suppressColumnChangedHandler)
                return;

            
            if (e.Column.ColumnName == DayColumnName)
                return;

            
            var raw = Convert.ToString(e.ProposedValue) ?? string.Empty;

            
            if (!AvailabilityMatrixEngine.TryNormalizeCell(raw, out var normalized, out var error))
            {
                
                e.Row.SetColumnError(e.Column, error ?? "Invalid value.");

                
                return;
            }

            
            e.Row.SetColumnError(e.Column, string.Empty);

            
            var current = Convert.ToString(e.Row[e.Column]) ?? string.Empty;

            
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                _suppressColumnChangedHandler = true;
                try
                {
                    e.Row[e.Column] = normalized;
                }
                finally
                {
                    _suppressColumnChangedHandler = false;
                }
            }
        }

        private void NormalizeAndValidateAllMatrixCells()
        {
            
            _suppressColumnChangedHandler = true;

            try
            {
                
                AvailabilityMatrixEngine.NormalizeAndValidateAllCells(_groupTable);
            }
            finally
            {
                
                _suppressColumnChangedHandler = false;
            }
        }
    }
}
