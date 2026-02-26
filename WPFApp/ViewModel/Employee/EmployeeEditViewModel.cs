/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeEditViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Employees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
using WPFApp.ViewModel.Dialogs;
using WPFApp.ViewModel.Shared;

namespace WPFApp.ViewModel.Employee
{
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeEditViewModel : ViewModelBase, INotifyDataErrorInfo` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly EmployeeViewModel _owner;

        
        private readonly ValidationErrors _validation = new();

        
        
        

        private int _employeeId;
        /// <summary>
        /// Визначає публічний елемент `public int EmployeeId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int EmployeeId
        {
            get => _employeeId;
            set => SetProperty(ref _employeeId, value);
        }

        private string _firstName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string FirstName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (!SetProperty(ref _firstName, value))
                    return;

                
                ClearValidationErrors(nameof(FirstName));

                
                ValidateProperty(nameof(FirstName));
            }
        }

        private string _lastName = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string LastName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string LastName
        {
            get => _lastName;
            set
            {
                if (!SetProperty(ref _lastName, value))
                    return;

                ClearValidationErrors(nameof(LastName));
                ValidateProperty(nameof(LastName));
            }
        }

        private string? _email;
        /// <summary>
        /// Визначає публічний елемент `public string? Email` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? Email
        {
            get => _email;
            set
            {
                if (!SetProperty(ref _email, value))
                    return;

                ClearValidationErrors(nameof(Email));
                ValidateProperty(nameof(Email));
            }
        }

        private string? _phone;
        /// <summary>
        /// Визначає публічний елемент `public string? Phone` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? Phone
        {
            get => _phone;
            set
            {
                if (!SetProperty(ref _phone, value))
                    return;

                ClearValidationErrors(nameof(Phone));
                ValidateProperty(nameof(Phone));
            }
        }

        
        
        

        private bool _isEdit;
        /// <summary>
        /// Визначає публічний елемент `public bool IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsEdit
        {
            get => _isEdit;
            private set
            {
                if (SetProperty(ref _isEdit, value))
                    OnPropertiesChanged(nameof(FormTitle), nameof(FormSubtitle));
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public string FormTitle => IsEdit ? "Edit Employee" : "Add Employee";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormTitle => IsEdit ? "Edit Employee" : "Add Employee";

        /// <summary>
        /// Визначає публічний елемент `public string FormSubtitle => IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormSubtitle => IsEdit
            ? "Update the employee information and press Save."
            : "Fill the form and press Save.";

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SaveCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelCommand { get; }

        /// <summary>
        /// Визначає публічний елемент `public EmployeeEditViewModel(EmployeeViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeEditViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
        }

        
        
        

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

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public void ResetForNew()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ResetForNew()
        {
            EmployeeId = 0;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            IsEdit = false;

            ClearValidationErrors();
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetEmployee(EmployeeDto model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetEmployee(EmployeeDto model)
        {
            EmployeeId = model.Id;
            FirstName = model.FirstName;
            LastName = model.LastName;
            Email = model.Email;
            Phone = model.Phone;
            IsEdit = true;

            ClearValidationErrors();
        }

        /// <summary>
        /// Визначає публічний елемент `public SaveEmployeeRequest ToRequest()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public SaveEmployeeRequest ToRequest()
        {
            
            
            
            return new SaveEmployeeRequest
            {
                Id = EmployeeId,
                FirstName = FirstName?.Trim() ?? string.Empty,
                LastName = LastName?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim()
            };
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

            _validation.SetMany(errors);

            
            OnPropertyChanged(nameof(HasErrors));

            
            
            foreach (var key in errors.Keys)
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

        private void ClearValidationErrors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            _validation.Clear(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ValidateProperty(string propertyName)
        {
            
            var model = ToRequest();

            
            var msg = EmployeeValidationRules.ValidateProperty(model, propertyName);

            
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            
            ClearValidationErrors();

            
            var model = ToRequest();
            var raw = EmployeeValidationRules.ValidateAll(model);

            
            var errors = ValidationDictionaryHelper.RemapFirstErrors(raw, MapValidationKeyToVm);
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

        private static string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)
        {
            var sb = new StringBuilder();

            foreach (var msg in errors.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                sb.AppendLine(msg);

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? "Please check the input values." : text;
        }

        private async Task SaveWithValidationAsync()
        {
            
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(showDialog: true));

            if (!ok)
                return;

            await _owner.SaveAsync().ConfigureAwait(false);
        }

        internal static string MapValidationKeyToVm(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = ValidationDictionaryHelper.NormalizeLastSegment(key);

            
            
            return key switch
            {
                "FirstName" => nameof(FirstName),
                "LastName" => nameof(LastName),
                "Email" => nameof(Email),
                "Phone" => nameof(Phone),

                
                "EmailAddress" => nameof(Email),
                "PhoneNumber" => nameof(Phone),

                _ => key
            };
        }


    }
}
