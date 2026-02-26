/*
  Опис файлу: цей модуль містить реалізацію компонента ShopEditViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Shops;
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

namespace WPFApp.ViewModel.Shop
{
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ShopEditViewModel : ViewModelBase, INotifyDataErrorInfo` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ShopEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ShopViewModel _owner; 

        private readonly ValidationErrors _validation = new();

        
        
        

        private int _shopId;
        /// <summary>
        /// Визначає публічний елемент `public int ShopId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ShopId
        {
            get => _shopId;
            set => SetProperty(ref _shopId, value);
        }

        private string _name = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Name` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (!SetProperty(ref _name, value))
                    return;

                ClearValidationErrors(nameof(Name));
                ValidateProperty(nameof(Name));
            }
        }

        private string _address = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Address` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Address
        {
            get => _address;
            set
            {
                if (!SetProperty(ref _address, value))
                    return;

                ClearValidationErrors(nameof(Address));
                ValidateProperty(nameof(Address));
            }
        }

        private string _description = string.Empty;
        /// <summary>
        /// Визначає публічний елемент `public string Description` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (!SetProperty(ref _description, value))
                    return;

                ClearValidationErrors(nameof(Description));
                ValidateProperty(nameof(Description));
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
        /// Визначає публічний елемент `public string FormTitle => IsEdit ? "Edit Shop" : "Add Shop";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormTitle => IsEdit ? "Edit Shop" : "Add Shop";
        /// <summary>
        /// Визначає публічний елемент `public string FormSubtitle => IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormSubtitle => IsEdit
            ? "Update the shop information and press Save."
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
        /// Визначає публічний елемент `public ShopEditViewModel(ShopViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopEditViewModel(ShopViewModel owner)
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
            ShopId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
            IsEdit = false;

            ClearValidationErrors();
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetShop(ShopDto model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetShop(ShopDto model)
        {
            ShopId = model.Id;
            Name = model.Name ?? string.Empty;
            Address = model.Address ?? string.Empty;
            Description = model.Description ?? string.Empty;
            IsEdit = true;

            ClearValidationErrors();
        }

        /// <summary>
        /// Визначає публічний елемент `public SaveShopRequest ToRequest()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public SaveShopRequest ToRequest()
        {
            return new SaveShopRequest
            {
                Id = ShopId,
                Name = Name?.Trim() ?? string.Empty,
                Address = Address?.Trim() ?? string.Empty,
                Description = Description?.Trim() ?? string.Empty
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

            
            var msg = ShopValidationRules.ValidateProperty(model, propertyName);

            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            ClearValidationErrors();

            var model = ToRequest();

            
            var raw = ShopValidationRules.ValidateAll(model);

            
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

        private static string MapValidationKeyToVm(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = ValidationDictionaryHelper.NormalizeLastSegment(key);

            return key switch
            {
                "Name" => nameof(Name),
                "Address" => nameof(Address),
                "Description" => nameof(Description),

                
                "ShopName" => nameof(Name),

                _ => key
            };
        }
    }
}
