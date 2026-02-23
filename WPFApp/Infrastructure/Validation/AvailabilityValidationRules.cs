using System;
using System.Collections.Generic;

namespace WPFApp.Infrastructure.Validation
{
    /// <summary>
    /// AvailabilityValidationRules — єдине місце з правилами валідації для форми редагування AvailabilityGroup.
    ///
    /// Чому окремий файл:
    /// - ViewModel не роздувається правилами.
    /// - Правила можна повторно використати (owner/service/tests).
    /// - Легко тестувати без WPF.
    ///
    /// Формат помилок:
    /// key   = ім'я властивості ViewModel (щоб INotifyDataErrorInfo підсвітив правильне поле)
    /// value = текст помилки
    /// </summary>
    public static class AvailabilityValidationRules
    {
        // Ключі мають збігатися з назвами властивостей AvailabilityEditViewModel
        public const string K_AvailabilityName = "AvailabilityName";
        public const string K_AvailabilityMonth = "AvailabilityMonth";
        public const string K_AvailabilityYear = "AvailabilityYear";

        /// <summary>
        /// Повна валідація (перед Save).
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(string? name, int year, int month)
        {
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            AddIfError(errors, K_AvailabilityName, ValidateName(name));
            AddIfError(errors, K_AvailabilityYear, ValidateYear(year));
            AddIfError(errors, K_AvailabilityMonth, ValidateMonth(month));

            return errors;
        }

        /// <summary>
        /// Валідація одного поля (inline-валидація у setter'і).
        /// </summary>
        public static string? ValidateProperty(string? name, int year, int month, string vmPropertyName)
        {
            if (string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            return vmPropertyName switch
            {
                K_AvailabilityName => ValidateName(name),
                K_AvailabilityYear => ValidateYear(year),
                K_AvailabilityMonth => ValidateMonth(month),
                _ => null
            };
        }

        // -----------------------
        // Конкретні правила
        // -----------------------

        private static string? ValidateName(string? name)
        {
            name = (name ?? string.Empty).Trim();

            if (name.Length == 0)
                return "Name is required.";

            if (name.Length < 2)
                return "Name is too short (min 2 chars).";

            // Ліміт зробив таким самим як у container/schedule правилах (практичний дефолт)
            if (name.Length > 100)
                return "Name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateMonth(int month)
        {
            if (month < 1 || month > 12)
                return "Month must be between 1 and 12.";

            return null;
        }

        private static string? ValidateYear(int year)
        {
            // Залишив твій діапазон (2000..3000), бо так було в VM.
            if (year < 2000 || year > 3000)
                return "Year must be in range 2000..3000.";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors[key] = message;
        }
    }
}
