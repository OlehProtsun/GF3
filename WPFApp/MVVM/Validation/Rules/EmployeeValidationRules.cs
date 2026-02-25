using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Contracts.Employees;

namespace WPFApp.MVVM.Validation.Rules
{
    /// <summary>
    /// EmployeeValidationRules — “єдине місце”, де зібрані правила валідації Employee.
    ///
    /// Навіщо окремий файл:
    /// 1) EmployeeViewModel і EmployeeEditViewModel не повинні містити Regex/правила (щоб не дублювати).
    /// 2) Owner (EmployeeViewModel) і форма (EmployeeEditViewModel) використовують ОДНАКОВІ правила.
    /// 3) Легко тестувати без WPF.
    ///
    /// Формат помилок:
    /// - key   = ім'я властивості у ViewModel (FirstName/LastName/Email/Phone)
    /// - value = повідомлення про помилку
    /// </summary>
    public static class EmployeeValidationRules
    {
        // ------------------------------------------------------------
        // 1) Ключі (повинні відповідати назвам властивостей EmployeeEditViewModel)
        // ------------------------------------------------------------

        public const string K_FirstName = "FirstName";
        public const string K_LastName = "LastName";
        public const string K_Email = "Email";
        public const string K_Phone = "Phone";

        // ------------------------------------------------------------
        // 2) Regex (винесли з EmployeeEditViewModel, щоб owner не залежав від EditVM)
        // ------------------------------------------------------------

        /// <summary>
        /// Простий, практичний email regex (такий самий, як у твоєму EditVM).
        /// Примітка: це НЕ RFC-perfect, але для UI валідації часто достатньо.
        /// </summary>
        public static readonly Regex EmailRegex =
            new(@"^\S+@\S+\.\S+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Телефон: цифри + символи + - ( ) пробіли, мінімум 5 символів.
        /// </summary>
        public static readonly Regex PhoneRegex =
            new(@"^[0-9+\-\s()]{5,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // ------------------------------------------------------------
        // 3) Публічні API
        // ------------------------------------------------------------

        /// <summary>
        /// Повна валідація всієї моделі (перед Save).
        /// Повертає словник: propertyName -> errorMessage.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(SaveEmployeeRequest? model)
        {
            // 1) Готуємо результуючий словник.
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            // 2) Якщо model == null — повертаємо пусто (owner може вирішити, що робити).
            if (model is null)
                return errors;

            // 3) FirstName.
            AddIfError(errors, K_FirstName, ValidateFirstName(model.FirstName));

            // 4) LastName.
            AddIfError(errors, K_LastName, ValidateLastName(model.LastName));

            // 5) Email (не обов’язковий, але якщо заданий — має бути валідний).
            AddIfError(errors, K_Email, ValidateEmail(model.Email));

            // 6) Phone (не обов’язковий, але якщо заданий — має бути валідний).
            AddIfError(errors, K_Phone, ValidatePhone(model.Phone));

            // 7) Повертаємо.
            return errors;
        }

        /// <summary>
        /// Валідація одного поля (inline validation у setter'і ViewModel).
        /// vmPropertyName — назва властивості ViewModel (FirstName/LastName/Email/Phone).
        /// </summary>
        public static string? ValidateProperty(SaveEmployeeRequest? model, string vmPropertyName)
        {
            // 1) Захист.
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            // 2) Switch по ключу.
            return vmPropertyName switch
            {
                K_FirstName => ValidateFirstName(model.FirstName),
                K_LastName => ValidateLastName(model.LastName),
                K_Email => ValidateEmail(model.Email),
                K_Phone => ValidatePhone(model.Phone),
                _ => null
            };
        }

        // ------------------------------------------------------------
        // 4) Конкретні правила
        // ------------------------------------------------------------

        private static string? ValidateFirstName(string? firstName)
        {
            // 1) Trim, null-safe.
            firstName = (firstName ?? string.Empty).Trim();

            // 2) Required.
            if (firstName.Length == 0)
                return "First name is required.";

            // 3) Мінімальна довжина (за бажанням можеш змінити).
            if (firstName.Length < 2)
                return "First name is too short (min 2 chars).";

            // 4) Максимальна довжина (узгоджено з іншими формами, де часто 100).
            if (firstName.Length > 100)
                return "First name is too long (max 100 chars).";

            // 5) Ok.
            return null;
        }

        private static string? ValidateLastName(string? lastName)
        {
            lastName = (lastName ?? string.Empty).Trim();

            if (lastName.Length == 0)
                return "Last name is required.";

            if (lastName.Length < 2)
                return "Last name is too short (min 2 chars).";

            if (lastName.Length > 100)
                return "Last name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateEmail(string? email)
        {
            // Email необов’язковий: якщо пустий — ок.
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Trim.
            email = email.Trim();

            // Перевірка формату.
            if (!EmailRegex.IsMatch(email))
                return "Invalid email format.";

            return null;
        }

        private static string? ValidatePhone(string? phone)
        {
            // Phone необов’язковий: якщо пустий — ок.
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Trim.
            phone = phone.Trim();

            // Перевірка формату.
            if (!PhoneRegex.IsMatch(phone))
                return "Invalid phone number.";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            // 1) Якщо повідомлення пусте — помилки немає.
            if (string.IsNullOrWhiteSpace(message))
                return;

            // 2) Записуємо.
            errors[key] = message;
        }
    }
}
