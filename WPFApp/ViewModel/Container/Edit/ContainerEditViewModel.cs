/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerEditViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections;
using System.ComponentModel;
using BusinessLogicLayer.Contracts.Models;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.MVVM.Validation.Rules;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class ContainerEditViewModel : ViewModelBase, INotifyDataErrorInfo` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ContainerEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        
        
        
        
        
        
        private readonly ContainerViewModel _owner;

        
        
        
        
        
        
        
        private readonly ValidationErrors _validation = new();

        
        
        

        private int _containerId;
        /// <summary>
        /// Визначає публічний елемент `public int ContainerId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ContainerId
        {
            get => _containerId;
            set => SetProperty(ref _containerId, value);
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
                if (SetProperty(ref _name, value))
                {
                    
                    
                    ClearValidationErrors(nameof(Name));

                    
                    ValidateProperty(nameof(Name));
                }
            }
        }

        private string? _note;
        /// <summary>
        /// Визначає публічний елемент `public string? Note` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                {
                    ClearValidationErrors(nameof(Note));
                    ValidateProperty(nameof(Note));
                }
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
        /// Визначає публічний елемент `public string FormTitle => IsEdit ? "Edit Container" : "Add Container";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormTitle => IsEdit ? "Edit Container" : "Add Container";

        /// <summary>
        /// Визначає публічний елемент `public string FormSubtitle => IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormSubtitle => IsEdit
            ? "Update the container information and press Save."
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
        /// Визначає публічний елемент `public ContainerEditViewModel(ContainerViewModel owner)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerEditViewModel(ContainerViewModel owner)
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
            ContainerId = 0;
            Name = string.Empty;
            Note = string.Empty;
            IsEdit = false;

            ClearValidationErrors();
        }

        /// <summary>
        /// Визначає публічний елемент `public void SetContainer(ContainerModel model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetContainer(ContainerModel model)
        {
            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;
            IsEdit = true;

            ClearValidationErrors();
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerModel ToModel()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerModel ToModel()
        {
            return new ContainerModel
            {
                Id = ContainerId,
                Name = Name?.Trim() ?? string.Empty,
                Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
            };
        }

        
        
        

        
        
        
        
        
        
        
        private bool ValidateBeforeSave()
        {
            var model = ToModel();
            var errors = ContainerValidationRules.ValidateAll(model);

            SetValidationErrors(errors);
            return !HasErrors;
        }

        
        
        
        private void ValidateProperty(string propertyName)
        {
            var model = ToModel();
            var msg = ContainerValidationRules.ValidateProperty(model, propertyName);

            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            _validation.SetMany(errors);
            OnPropertyChanged(nameof(HasErrors));
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
            _validation.Clear(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }

        
        
        

        
        
        
        
        
        private Task SaveWithValidationAsync()
        {
            if (!ValidateBeforeSave())
                return Task.CompletedTask;

            return _owner.SaveAsync();
        }
    }
}
