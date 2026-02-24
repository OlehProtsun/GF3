using System;
using System.Collections.Generic;
using DataAccessLayer.Models;

namespace WPFApp.MVVM.Validation.Rules
{
    /// <summary>
    /// ShopValidationRules — єдине місце з правилами валідації Shop.
    ///
    /// Навіщо:
    /// - ShopViewModel (owner) не повинен містити правила валідації напряму.
    /// - ShopEditViewModel може робити inline-валидацію використовуючи ті самі правила.
    /// - Менше дублювання і менше шансів, що правила “роз’їдуться”.
    ///
    /// Формат помилок:
    /// - key: назва властивості у ViewModel (Name/Address/Description)
    /// - value: повідомлення про помилку
    /// </summary>
    public static class ShopValidationRules
    {
        // Ключі мають відповідати назвам властивостей у ShopEditViewModel
        public const string K_Name = "Name";
        public const string K_Address = "Address";
        public const string K_Description = "Description";

        /// <summary>
        /// Повна валідація (перед Save).
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(ShopModel? model)
        {
            // 1) Готуємо результуючий словник.
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            // 2) Null-safe: якщо model == null — повертаємо пустий словник.
            if (model is null)
                return errors;

            // 3) Перевіряємо Name.
            AddIfError(errors, K_Name, ValidateName(model.Name));

            // 4) Перевіряємо Address.
            AddIfError(errors, K_Address, ValidateAddress(model.Address));

            // 5) Description — як правило optional (у твоєму коді воно optional),
            //    але можемо накласти лише ліміт довжини, щоб не було “дуже довго”.
            AddIfError(errors, K_Description, ValidateDescription(model.Description));

            return errors;
        }

        /// <summary>
        /// Валідація конкретної властивості (inline-валидація у setter'і).
        /// </summary>
        public static string? ValidateProperty(ShopModel? model, string vmPropertyName)
        {
            // 1) Захист.
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            // 2) Switch по ключу.
            return vmPropertyName switch
            {
                K_Name => ValidateName(model.Name),
                K_Address => ValidateAddress(model.Address),
                K_Description => ValidateDescription(model.Description),
                _ => null
            };
        }

        // ----------------------------
        // Конкретні правила
        // ----------------------------

        private static string? ValidateName(string? name)
        {
            // 1) Null-safe + Trim.
            name = (name ?? string.Empty).Trim();

            // 2) Required.
            if (name.Length == 0)
                return "Name is required.";

            // 3) Мін/макс довжина (консервативні межі).
            if (name.Length < 2)
                return "Name is too short (min 2 chars).";

            if (name.Length > 120)
                return "Name is too long (max 120 chars).";

            return null;
        }

        private static string? ValidateAddress(string? address)
        {
            // 1) Null-safe + Trim.
            address = (address ?? string.Empty).Trim();

            // 2) Required (це саме те, що робив твій Validate у ShopViewModel).
            if (address.Length == 0)
                return "Address is required.";

            // 3) Мін/макс довжина.
            if (address.Length < 3)
                return "Address is too short (min 3 chars).";

            if (address.Length > 200)
                return "Address is too long (max 200 chars).";

            return null;
        }

        private static string? ValidateDescription(string? description)
        {
            // Description optional.
            if (string.IsNullOrWhiteSpace(description))
                return null;

            // 1) Trim.
            description = description.Trim();

            // 2) Ліміт довжини — щоб не було “вставили роман”.
            if (description.Length > 2000)
                return "Description is too long (max 2000 chars).";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            // 1) Якщо повідомлення пусте — помилки немає.
            if (string.IsNullOrWhiteSpace(message))
                return;

            // 2) Записуємо помилку.
            errors[key] = message;
        }
    }
}
