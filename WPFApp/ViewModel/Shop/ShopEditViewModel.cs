using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Shop
{
    public sealed class ShopEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ShopViewModel _owner;
        private readonly Dictionary<string, List<string>> _errors = new();

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
                if (SetProperty(ref _name, value))
                    ClearValidationErrors(nameof(Name));
            }
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                    ClearValidationErrors(nameof(Address));
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                    ClearValidationErrors(nameof(Description));
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

        public string FormTitle => IsEdit ? "Edit Shop" : "Add Shop";

        public string FormSubtitle => IsEdit
            ? "Update the shop information and press Save."
            : "Fill the form and press Save.";

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ShopEditViewModel(ShopViewModel owner)
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
            Name = model.Name;
            Address = model.Address;
            Description = model.Description;
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
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
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
