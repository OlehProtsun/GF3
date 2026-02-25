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
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

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

        public SaveEmployeeRequest ToRequest()
        {
            // Важливо:
            // - Trim для імен
            // - Email/Phone: null якщо пусто (зручніше для БД/сервісу)
            return new SaveEmployeeRequest
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
            // ВАЖЛИВО: WPF validation visuals краще оновлювати з UI thread
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

            // HasErrors — computed
            OnPropertyChanged(nameof(HasErrors));

            // КЛЮЧОВЕ: примусово “штовхаємо” WPF, щоб він одразу перемалював Validation.HasError
            // навіть якщо користувач не змінював поле (не було PropertyChanged від binding’а)
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
            // 1) Беремо модель з поточного стану VM.
            var model = ToRequest();

            // 2) Питаємо rules: чи є помилка саме для цього поля.
            var msg = EmployeeValidationRules.ValidateProperty(model, propertyName);

            // 3) Якщо є — додаємо.
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            // 4) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }

        // ----------------------------
        // Full-form validation (Save gate) — як у ContainerScheduleEdit
        // ----------------------------

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            // 1) Будь-які старі помилки прибираємо, щоб не змішувались зі свіжими.
            ClearValidationErrors();

            // 2) Валідимо всю модель одним проходом — єдине джерело правил.
            var model = ToRequest();
            var raw = EmployeeValidationRules.ValidateAll(model);

            // приводимо ключі до тих, що реально біндяться в XAML (FirstName/LastName/Email/Phone)
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in raw)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (string.IsNullOrWhiteSpace(vmKey))
                    continue;

                // не даємо дублювати ключі
                if (!errors.ContainsKey(vmKey))
                    errors[vmKey] = kv.Value;
            }

            SetValidationErrors(errors);


            // 4) Якщо треба — показуємо діалог із summary.
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
            // як у ContainerScheduleEdit: валідацію виконуємо на UI thread
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

            // Якщо правила повертають "Employee.FirstName" або "Model.FirstName"
            var dot = key.LastIndexOf('.');
            if (dot >= 0 && dot < key.Length - 1)
                key = key[(dot + 1)..];

            // Нормалізуємо під VM property names
            // (додай сюди всі нестиковки, які реально може повертати EmployeeValidationRules)
            return key switch
            {
                "FirstName" => nameof(FirstName),
                "LastName" => nameof(LastName),
                "Email" => nameof(Email),
                "Phone" => nameof(Phone),

                // приклади на випадок інших назв з rules:
                "EmailAddress" => nameof(Email),
                "PhoneNumber" => nameof(Phone),

                _ => key
            };
        }


    }
}
