using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Container
{
    public sealed class ContainerEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ContainerViewModel _owner;
        private readonly Dictionary<string, List<string>> _errors = new();

        private int _containerId;
        public int ContainerId
        {
            get => _containerId;
            set => SetProperty(ref _containerId, value);
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

        private string? _note;
        public string? Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                    ClearValidationErrors(nameof(Note));
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

        public string FormTitle => IsEdit ? "Edit Container" : "Add Container";

        public string FormSubtitle => IsEdit
            ? "Update the container information and press Save."
            : "Fill the form and press Save.";

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ContainerEditViewModel(ContainerViewModel owner)
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
            ContainerId = 0;
            Name = string.Empty;
            Note = string.Empty;
            IsEdit = false;
            ClearValidationErrors();
        }

        public void SetContainer(ContainerModel model)
        {
            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;
            IsEdit = true;
            ClearValidationErrors();
        }

        public ContainerModel ToModel()
        {
            return new ContainerModel
            {
                Id = ContainerId,
                Name = Name?.Trim() ?? string.Empty,
                Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
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
