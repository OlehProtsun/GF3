using System;
using System.Collections;
using System.ComponentModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.Validation;

namespace WPFApp.ViewModel.Shop
{
    /// <summary>
    /// ShopEditViewModel — форма Add/Edit Shop.
    ///
    /// Оптимізації:
    /// 1) Власний Dictionary<string,List<string>> замінено на спільний ValidationErrors.
    ///    Це уніфікує поведінку з Availability/Employee/Container.
    /// 2) Inline validation:
    ///    - при зміні поля чистимо помилки цього поля
    ///    - одразу валідимо через ShopValidationRules
    /// 3) Публічні методи SetValidationErrors/ClearValidationErrors залишені, але тепер працюють через ValidationErrors.
    /// </summary>
    public sealed class ShopEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ShopViewModel _owner;

        // Єдине сховище помилок.
        private readonly ValidationErrors _validation = new();

        // ----------------------------
        // Поля форми
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
                // 1) Якщо значення не змінилось — виходимо.
                if (!SetProperty(ref _name, value))
                    return;

                // 2) Чистимо попередню помилку по цьому полю.
                ClearValidationErrors(nameof(Name));

                // 3) Валідимо новий стан.
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

        private string? _description;
        public string? Description
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
        // Режим форми (Add/Edit)
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
        // Команди
        // ----------------------------

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ShopEditViewModel(ShopViewModel owner)
        {
            _owner = owner;

            // Save/Cancel делегуються owner’у (owner знає потік навігації і service calls).
            SaveCommand = new AsyncRelayCommand(() => _owner.SaveAsync());
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
        }

        // ----------------------------
        // INotifyDataErrorInfo (проксі на ValidationErrors)
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
        // Life-cycle
        // ----------------------------

        public void ResetForNew()
        {
            // 1) Скидаємо поля.
            ShopId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Description = string.Empty;

            // 2) Режим add.
            IsEdit = false;

            // 3) Чистимо помилки.
            ClearValidationErrors();
        }

        public void SetShop(ShopModel model)
        {
            // 1) Заповнюємо поля з моделі.
            ShopId = model.Id;
            Name = model.Name;
            Address = model.Address;
            Description = model.Description;

            // 2) Режим edit.
            IsEdit = true;

            // 3) Чистимо старі помилки (на випадок попереднього редагування).
            ClearValidationErrors();
        }

        public ShopModel ToModel()
        {
            // 1) Trim важливий, щоб:
            //    - у БД не летіли " Shop " з пробілами
            //    - валідація була стабільна
            return new ShopModel
            {
                Id = ShopId,
                Name = Name?.Trim() ?? string.Empty,
                Address = Address?.Trim() ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };
        }

        // ----------------------------
        // Validation operations
        // ----------------------------

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            // 1) Якщо errors пустий — очищаємо.
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            // 2) Масово ставимо помилки.
            _validation.SetMany(errors);

            // 3) HasErrors — computed, тож явно нотифікуємо.
            OnPropertyChanged(nameof(HasErrors));
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
            // 1) Збираємо модель з поточного стану.
            var model = ToModel();

            // 2) Питаємо rules: чи є помилка саме для цього поля.
            var msg = ShopValidationRules.ValidateProperty(model, propertyName);

            // 3) Якщо є — додаємо.
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            // 4) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
