using System;
using System.Collections;
using System.ComponentModel;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Infrastructure.Validation;

namespace WPFApp.ViewModel.Employee
{
    /// <summary>
    /// EmployeeEditViewModel — форма Add/Edit Employee.
    ///
    /// Покращення:
    /// - не тримаємо власний Dictionary<string,List<string>>:
    ///   використовуємо спільний ValidationErrors (як у Container/Availability/Schedule) :contentReference[oaicite:11]{index=11}
    /// - inline validation: при зміні поля:
    ///   * чистимо помилку цього поля
    ///   * валідимо нове значення через EmployeeValidationRules
    /// - не зберігаємо Regex тут: Regex винесені у EmployeeValidationRules
    /// </summary>
    public sealed class EmployeeEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly EmployeeViewModel _owner;

        // Єдине сховище помилок.
        private readonly ValidationErrors _validation = new();

        // ----------------------------
        // Поля форми
        // ----------------------------

        private int _employeeId;
        public int EmployeeId
        {
            get => _employeeId;
            set => SetProperty(ref _employeeId, value);
        }

        private string _firstName = string.Empty;
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (!SetProperty(ref _firstName, value))
                    return;

                // 1) При зміні поля — прибираємо стару помилку FirstName.
                ClearValidationErrors(nameof(FirstName));

                // 2) Перевіряємо новий стан (inline).
                ValidateProperty(nameof(FirstName));
            }
        }

        private string _lastName = string.Empty;
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

        public string FormTitle => IsEdit ? "Edit Employee" : "Add Employee";

        public string FormSubtitle => IsEdit
            ? "Update the employee information and press Save."
            : "Fill the form and press Save.";

        // ----------------------------
        // Команди
        // ----------------------------

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public EmployeeEditViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            // Save/Cancel делегуються owner’у.
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
        // Life-cycle / binding
        // ----------------------------

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

        public void SetEmployee(EmployeeModel model)
        {
            EmployeeId = model.Id;
            FirstName = model.FirstName;
            LastName = model.LastName;
            Email = model.Email;
            Phone = model.Phone;
            IsEdit = true;

            ClearValidationErrors();
        }

        public EmployeeModel ToModel()
        {
            // Важливо:
            // - Trim для імен
            // - Email/Phone: null якщо пусто (зручніше для БД/сервісу)
            return new EmployeeModel
            {
                Id = EmployeeId,
                FirstName = FirstName?.Trim() ?? string.Empty,
                LastName = LastName?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim()
            };
        }

        // ----------------------------
        // Validation operations (set/clear)
        // ----------------------------

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            // SetMany сам:
            // - очистить старі
            // - додасть нові
            // - підніме ErrorsChanged на кожну властивість
            _validation.SetMany(errors);

            // HasErrors — computed, тож явно нотифікуємо UI.
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
            // 1) Беремо модель з поточного стану VM.
            var model = ToModel();

            // 2) Питаємо rules: чи є помилка саме для цього поля.
            var msg = EmployeeValidationRules.ValidateProperty(model, propertyName);

            // 3) Якщо є — додаємо.
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            // 4) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
