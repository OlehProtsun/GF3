using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Employee
{
    public sealed class EmployeeEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        internal static readonly Regex EmailRegex =
            new(@"^\S+@\S+\.\S+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static readonly Regex PhoneRegex =
            new(@"^[0-9+\-\s()]{5,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly EmployeeViewModel _owner;
        private readonly Dictionary<string, List<string>> _errors = new();

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
                if (SetProperty(ref _firstName, value))
                    ClearValidationErrors(nameof(FirstName));
            }
        }

        private string _lastName = string.Empty;
        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                    ClearValidationErrors(nameof(LastName));
            }
        }

        private string? _email;
        public string? Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                    ClearValidationErrors(nameof(Email));
            }
        }

        private string? _phone;
        public string? Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                    ClearValidationErrors(nameof(Phone));
            }
        }

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

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public EmployeeEditViewModel(EmployeeViewModel owner)
        {
            _owner = owner;

            SaveCommand = new AsyncRelayCommand(() => _owner.SaveAsync());
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName is null || !_errors.TryGetValue(propertyName, out var list))
                return Array.Empty<string>();

            return list;
        }

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
            return new EmployeeModel
            {
                Id = EmployeeId,
                FirstName = FirstName?.Trim() ?? string.Empty,
                LastName = LastName?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim()
            };
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
                AddError(kv.Key, kv.Value);
        }

        public void ClearValidationErrors()
        {
            var keys = _errors.Keys.ToList();
            _errors.Clear();

            foreach (var key in keys)
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));

            OnPropertyChanged(nameof(HasErrors));
        }

        private void AddError(string propertyName, string message)
        {
            if (!_errors.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                _errors[propertyName] = list;
            }

            if (!list.Contains(message))
            {
                list.Add(message);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void ClearValidationErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }
    }
}
