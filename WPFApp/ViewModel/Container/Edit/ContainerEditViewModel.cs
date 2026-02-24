using System;
using System.Collections;
using System.ComponentModel;
using DataAccessLayer.Models;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.MVVM.Validation.Rules;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerEditViewModel — VM форми “Add/Edit Container”.
    ///
    /// Основні задачі:
    /// 1) Тримати поля контейнера (ContainerId, Name, Note)
    /// 2) Тримати режим форми (IsEdit) і текстові заголовки (FormTitle/FormSubtitle)
    /// 3) Давати команди Save/Cancel, які делегуються owner’у
    /// 4) Показувати валідаційні помилки через INotifyDataErrorInfo
    ///
    /// Важливий принцип:
    /// - ViewModel НЕ зберігає помилки вручну (Dictionary),
    ///   а використовує спільний інфраструктурний клас ValidationErrors.
    /// </summary>
    public sealed class ContainerEditViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        /// <summary>
        /// Owner (ContainerViewModel) — керує реальними діями:
        /// - SaveAsync()
        /// - CancelAsync()
        /// VM лише викликає ці методи командами.
        /// </summary>
        private readonly ContainerViewModel _owner;

        /// <summary>
        /// Єдине сховище помилок валідації.
        /// Воно:
        /// - зберігає помилки по propertyName
        /// - піднімає ErrorsChanged
        /// - дає HasErrors/GetErrors
        /// </summary>
        private readonly ValidationErrors _validation = new();

        // ------------------------
        // 1) Поля контейнера
        // ------------------------

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
                {
                    // Inline-валидація:
                    // 1) прибираємо стару помилку
                    ClearValidationErrors(nameof(Name));

                    // 2) перевіряємо нове значення через правила
                    ValidateProperty(nameof(Name));
                }
            }
        }

        private string? _note;
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

        // ------------------------
        // 2) Режим форми (Add/Edit)
        // ------------------------

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

        // ------------------------
        // 3) Команди
        // ------------------------

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ContainerEditViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            // Save обгортаємо повною валідацією:
            // - якщо є помилки => не викликаємо owner.SaveAsync()
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            CancelCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
        }

        // ------------------------
        // 4) INotifyDataErrorInfo (проксі на ValidationErrors)
        // ------------------------

        public bool HasErrors => _validation.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            add => _validation.ErrorsChanged += value;
            remove => _validation.ErrorsChanged -= value;
        }

        public IEnumerable GetErrors(string? propertyName)
            => _validation.GetErrors(propertyName);

        // ------------------------
        // 5) Життєвий цикл / заповнення
        // ------------------------

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

        /// <summary>
        /// Перетворити VM-стан у модель для збереження.
        /// Тут ми також нормалізуємо значення:
        /// - Name trim
        /// - Note trim або null якщо порожній
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

        // ------------------------
        // 6) Валідація (операції)
        // ------------------------

        /// <summary>
        /// Повна перевірка перед Save.
        ///
        /// Навіщо:
        /// - inline-валидація працює тільки коли користувач змінює поля
        /// - але перед Save треба бути впевненим, що всі правила виконані
        /// </summary>
        private bool ValidateBeforeSave()
        {
            var model = ToModel();
            var errors = ContainerValidationRules.ValidateAll(model);

            SetValidationErrors(errors);
            return !HasErrors;
        }

        /// <summary>
        /// Валідація одного поля (inline) за поточним станом VM.
        /// </summary>
        private void ValidateProperty(string propertyName)
        {
            var model = ToModel();
            var msg = ContainerValidationRules.ValidateProperty(model, propertyName);

            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);
        }

        /// <summary>
        /// Встановити багато помилок “пакетом”.
        /// Наприклад, коли сервер повернув помилки.
        /// </summary>
        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            _validation.SetMany(errors);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Очистити всі помилки.
        /// </summary>
        public void ClearValidationErrors()
        {
            _validation.ClearAll();
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Очистити помилки для конкретної властивості.
        /// </summary>
        private void ClearValidationErrors(string propertyName)
        {
            _validation.Clear(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }

        // ------------------------
        // 7) Save wrapper
        // ------------------------

        /// <summary>
        /// Обгортка над Save:
        /// - спочатку валідимо
        /// - якщо ок — викликаємо owner.SaveAsync()
        /// </summary>
        private Task SaveWithValidationAsync()
        {
            if (!ValidateBeforeSave())
                return Task.CompletedTask;

            return _owner.SaveAsync();
        }
    }
}
