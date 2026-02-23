using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using WPFApp.Infrastructure.AvailabilityMatrix;
using WPFApp.Infrastructure.Validation;
using WPFApp.Service;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;

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

        private bool ValidateBeforeSave(bool showDialog = true)
        {
            // 1) Чистимо старі помилки перед новим проходом.
            ClearValidationErrors();

            // 2) Валідимо всі поля форми.
            var rawErrors = AvailabilityValidationRules.ValidateAll(
                name: AvailabilityName,
                year: AvailabilityYear,
                month: AvailabilityMonth);

            // 3) Проставляємо помилки (всередині буде мапінг ключів).
            SetValidationErrors(rawErrors);

            // 4) Показуємо діалог як в Employee.
            if (showDialog && HasErrors)
            {
                CustomMessageBox.Show(
                    "Validation",
                    BuildValidationSummary(rawErrors),
                    CustomMessageBoxIcon.Error,
                    okText: "OK");
            }

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
            // Важливо: WPF validation visuals краще оновлювати в UI thread
            if (Application.Current?.Dispatcher is not null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => SetValidationErrors(errors));
                return;
            }

            // 1) Якщо помилок нема — чистимо все.
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            // 2) Мапимо ключі валідатора -> властивості VM (Name -> AvailabilityName і т.д.)
            var mapped = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in errors)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (string.IsNullOrWhiteSpace(vmKey))
                    continue;

                if (!mapped.ContainsKey(vmKey))
                    mapped[vmKey] = kv.Value;
            }

            if (mapped.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            // 3) Проставляємо помилки
            _validation.SetMany(mapped);

            // 4) Оновлюємо HasErrors
            OnPropertyChanged(nameof(HasErrors));

            // 5) КЛЮЧОВЕ: пушимо PropertyChanged для конкретних полів,
            //    щоб WPF одразу намалював Validation.HasError після Save
            foreach (var key in mapped.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    OnPropertyChanged(key);
            }
        }

        public void ClearValidationErrors()
        {
            // 1) Чистимо все.
            _validation.ClearAll();

            // 2) Оновлюємо HasErrors.
            OnPropertyChanged(nameof(HasErrors));
        }

        private static string MapValidationKeyToVm(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = key.Trim();

            // Якщо валідатор повертає "Group.Name" / "Model.Year" — беремо останню частину
            var dot = key.LastIndexOf('.');
            if (dot >= 0 && dot < key.Length - 1)
                key = key[(dot + 1)..];

            return key switch
            {
                // VM names
                "AvailabilityName" => nameof(AvailabilityName),
                "AvailabilityMonth" => nameof(AvailabilityMonth),
                "AvailabilityYear" => nameof(AvailabilityYear),

                // Domain/BLL names (дуже важливо)
                "Name" => nameof(AvailabilityName),
                "Month" => nameof(AvailabilityMonth),
                "Year" => nameof(AvailabilityYear),

                _ => key
            };
        }

        private static string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)
        {
            if (errors is null || errors.Count == 0)
                return "Please check the input values.";

            var sb = new StringBuilder();

            foreach (var msg in errors.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                sb.AppendLine(msg);

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? "Please check the input values." : text;
        }

        // (Опціонально, але дуже зручно — щоб показувати діалог і з owner-а)
        internal void ShowValidationErrorsDialog(IReadOnlyDictionary<string, string> errors)
        {
            CustomMessageBox.Show(
                "Validation",
                BuildValidationSummary(errors),
                CustomMessageBoxIcon.Error,
                okText: "OK");
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
