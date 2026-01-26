using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using WPFApp.Infrastructure.AvailabilityMatrix;
using WPFApp.Infrastructure.Validation;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Валідація:
    /// - INotifyDataErrorInfo проксі на ValidationErrors
    /// - Валідація полів (через AvailabilityValidationRules)
    /// - Валідація/нормалізація клітинок DataTable (ColumnChanged + масова нормалізація)
    /// </summary>
    public sealed partial class AvailabilityEditViewModel : INotifyDataErrorInfo
    {
        // ----------------------------
        // INotifyDataErrorInfo
        // ----------------------------

        public bool HasErrors => _validation.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            // Перепідписуємо зовнішніх слухачів на внутрішній контейнер.
            add => _validation.ErrorsChanged += value;
            remove => _validation.ErrorsChanged -= value;
        }

        public IEnumerable GetErrors(string? propertyName)
            // Делегуємо отримання помилок.
            => _validation.GetErrors(propertyName);

        // ----------------------------
        // Field validation (form)
        // ----------------------------

        private bool ValidateBeforeSave()
        {
            // 1) Отримуємо помилки для всіх полів одразу.
            var errors = AvailabilityValidationRules.ValidateAll(
                name: AvailabilityName,
                year: AvailabilityYear,
                month: AvailabilityMonth);

            // 2) Проставляємо помилки у контейнер.
            SetValidationErrors(errors);

            // 3) Якщо помилок немає — можна зберігати.
            return !HasErrors;
        }

        private void ValidateProperty(string propertyName)
        {
            // 1) Питаємо rules: чи є помилка для конкретної властивості.
            var msg = AvailabilityValidationRules.ValidateProperty(
                name: AvailabilityName,
                year: AvailabilityYear,
                month: AvailabilityMonth,
                vmPropertyName: propertyName);

            // 2) Якщо є повідомлення — додаємо його в контейнер.
            if (!string.IsNullOrWhiteSpace(msg))
                _validation.Add(propertyName, msg);

            // 3) Оновлюємо HasErrors для UI.
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ClearValidationErrors(string propertyName)
        {
            // 1) Якщо ім’я пусте — нічого чистити.
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            // 2) Чистимо помилки по конкретній властивості.
            _validation.Clear(propertyName);

            // 3) Повідомляємо UI.
            OnPropertyChanged(nameof(HasErrors));
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            // 1) Якщо errors пустий — очищаємо все.
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            // 2) Масово ставимо помилки.
            _validation.SetMany(errors);

            // 3) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }

        public void ClearValidationErrors()
        {
            // 1) Чистимо все.
            _validation.ClearAll();

            // 2) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }

        // ----------------------------
        // Matrix cell validation (DataTable)
        // ----------------------------

        private void GroupTable_ColumnChanged(object? sender, DataColumnChangeEventArgs e)
        {
            // 1) Якщо ми самі зараз переприсвоюємо нормалізоване значення — виходимо (anti-recursion).
            if (_suppressColumnChangedHandler)
                return;

            // 2) Day column (DayOfMonth) не є “кодом” — не валідимо.
            if (e.Column.ColumnName == DayColumnName)
                return;

            // 3) Беремо значення, яке пропонується до запису (може відрізнятися від поточного).
            var raw = Convert.ToString(e.ProposedValue) ?? string.Empty;

            // 4) Пробуємо нормалізувати та перевірити через engine/parser.
            if (!AvailabilityMatrixEngine.TryNormalizeCell(raw, out var normalized, out var error))
            {
                // 4.1) Якщо невалідно — ставимо column error (WPF DataGrid покаже).
                e.Row.SetColumnError(e.Column, error ?? "Invalid value.");

                // 4.2) Виходимо, бо запис у клітинку вважаємо некоректним.
                return;
            }

            // 5) Якщо валідно — чистимо попередні помилки.
            e.Row.SetColumnError(e.Column, string.Empty);

            // 6) Якщо normalized відрізняється (наприклад прибрали пробіли) — перепишемо значення.
            var current = Convert.ToString(e.Row[e.Column]) ?? string.Empty;

            // 7) Якщо різні — переприсвоюємо, але з suppress flag, щоб не викликати рекурсію ColumnChanged.
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                _suppressColumnChangedHandler = true;
                try
                {
                    e.Row[e.Column] = normalized;
                }
                finally
                {
                    _suppressColumnChangedHandler = false;
                }
            }
        }

        private void NormalizeAndValidateAllMatrixCells()
        {
            // 1) Вмикаємо suppress, щоб масова нормалізація не породила лавину ColumnChanged.
            _suppressColumnChangedHandler = true;

            try
            {
                // 2) Делегуємо масову нормалізацію в engine.
                AvailabilityMatrixEngine.NormalizeAndValidateAllCells(_groupTable);
            }
            finally
            {
                // 3) Завжди вимикаємо suppress, навіть якщо щось впало.
                _suppressColumnChangedHandler = false;
            }
        }
    }
}
