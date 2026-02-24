using DataAccessLayer.Models;
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

namespace WPFApp.ViewModel.Shop
{
    /// <summary>
    /// ShopEditViewModel — форма Add/Edit Shop.
    /// Логіка валідації: як у ContainerScheduleEdit/EmployeeEdit
    /// - INotifyDataErrorInfo через ValidationErrors
    /// - Save -> ValidateAll -> SetMany(errors) -> MessageBox -> підсвітка полів
    /// - ВАЖЛИВО: після SetMany робимо OnPropertyChanged(key), щоб WPF одразу намалював red-border
    /// </summary>
    public sealed class ShopEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ShopViewModel _owner; // має мати SaveAsync/CancelAsync як у інших

        private readonly ValidationErrors _validation = new();

        // ----------------------------
        // Form fields
        // ----------------------------

        private int _shopId;
        public int ShopId
        {
            get => _shopId;
            set => SetProperty(ref _shopId, value);
        }

        private string _name = string.Empty;
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

        // ----------------------------
        // Mode (Add/Edit)
        // ----------------------------

        private bool _isEdit;
        public bool IsEdit
        {
            get => _isEdit;
            private set
            {
                if (SetProperty(ref _isEdit, value))
                    OnPropertiesChanged(nameof(FormTitle), nameof(FormSubtitle));
            }
        }

        public string FormTitle => IsEdit ? "Edit Shop" : "Add Shop";
        public string FormSubtitle => IsEdit
            ? "Update the shop information and press Save."
            : "Fill the form and press Save.";

        // ----------------------------
        // Commands
        // ----------------------------

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ShopEditViewModel(ShopViewModel owner)
        {
            _owner = owner;

            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
        }

        // ----------------------------
        // INotifyDataErrorInfo
        // ----------------------------

        public bool HasErrors => _validation.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            add => _validation.ErrorsChanged += value;
            remove => _validation.ErrorsChanged -= value;
        }

        public IEnumerable GetErrors(string? propertyName)
            => _validation.GetErrors(propertyName);

        // ----------------------------
        // Lifecycle / binding helpers
        // ----------------------------

        public void ResetForNew()
        {
            ShopId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;
            IsEdit = false;

            ClearValidationErrors();
        }

        public void SetShop(ShopModel model)
        {
            ShopId = model.Id;
            Name = model.Name ?? string.Empty;
            Address = model.Address ?? string.Empty;
            Description = model.Description ?? string.Empty;
            IsEdit = true;

            ClearValidationErrors();
        }

        public ShopModel ToModel()
        {
            return new ShopModel
            {
                Id = ShopId,
                Name = Name?.Trim() ?? string.Empty,
                Address = Address?.Trim() ?? string.Empty,
                Description = Description?.Trim() ?? string.Empty
            };
        }

        // ----------------------------
        // Validation operations (set/clear)
        // ----------------------------

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            // важливо: UI thread
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

            // КЛЮЧ: примусово “штовхаємо” WPF, щоб red-border з’явився одразу після Save
            foreach (var key in errors.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    OnPropertyChanged(key);
            }
        }

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
            var model = ToModel();

            // має існувати у тебе (аналог EmployeeValidationRules)
            var msg = ShopValidationRules.ValidateProperty(model, propertyName);

            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            OnPropertyChanged(nameof(HasErrors));
        }

        // ----------------------------
        // Full-form validation (Save gate)
        // ----------------------------

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            ClearValidationErrors();

            var model = ToModel();

            // має існувати у тебе (аналог EmployeeValidationRules.ValidateAll)
            var raw = ShopValidationRules.ValidateAll(model);

            // нормалізуємо ключі під VM property names (Name/Address/Description)
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in raw)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (string.IsNullOrWhiteSpace(vmKey))
                    continue;

                if (!errors.ContainsKey(vmKey))
                    errors[vmKey] = kv.Value;
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

            key = key.Trim();

            // якщо правила повертають "Shop.Name" / "Model.Name"
            var dot = key.LastIndexOf('.');
            if (dot >= 0 && dot < key.Length - 1)
                key = key[(dot + 1)..];

            return key switch
            {
                "Name" => nameof(Name),
                "Address" => nameof(Address),
                "Description" => nameof(Description),

                // можливі альтернативи з rules (якщо є)
                "ShopName" => nameof(Name),

                _ => key
            };
        }
    }
}
