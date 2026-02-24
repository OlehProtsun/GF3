using DataAccessLayer.Models;
using System;
using System.Collections.Generic;

namespace WPFApp.MVVM.Validation.Rules
{
    /// <summary>
    /// ContainerValidationRules — єдине місце з правилами валідації для Container (контейнера).
    ///
    /// Навіщо окремий файл:
    /// - ViewModel не роздувається правилами (“що дозволено/що ні”).
    /// - Правила можна використовувати повторно (наприклад, і в Save-сервісі, і в UI).
    /// - Легко тестувати незалежно від WPF.
    ///
    /// Повертаємо помилки як:
    /// - key: назва властивості ViewModel (наприклад nameof(ContainerEditViewModel.Name) => "Name")
    /// - value: текст помилки
    /// </summary>
    public static class ContainerValidationRules
    {
        // Ключі — як імена властивостей VM.
        // Це важливо для INotifyDataErrorInfo: WPF показує помилку біля того поля,
        // чия binding-властивість має таку назву.
        public const string K_Name = "Name";
        public const string K_Note = "Note";

        /// <summary>
        /// Повна валідація всієї моделі контейнера.
        ///
        /// Використання:
        /// - перед Save
        /// - при отриманні моделі ззовні, якщо треба прогнати правила
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(ContainerModel? model)
        {
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            if (model is null)
                return errors;

            AddIfError(errors, K_Name, ValidateName(model.Name));
            AddIfError(errors, K_Note, ValidateNote(model.Note));

            return errors;
        }

        /// <summary>
        /// Валідація одного поля (для inline-валідації при зміні властивості).
        ///
        /// vmPropertyName — ім’я властивості у VM (наприклад "Name" або "Note").
        /// Повертає null, якщо все ок.
        /// </summary>
        public static string? ValidateProperty(ContainerModel? model, string vmPropertyName)
        {
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            return vmPropertyName switch
            {
                K_Name => ValidateName(model.Name),
                K_Note => ValidateNote(model.Note),
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

            // Підбери ліміт під ваш продукт
            if (name.Length > 100)
                return "Name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateNote(string? note)
        {
            // Note може бути null/порожній — це ок
            if (string.IsNullOrWhiteSpace(note))
                return null;

            // Ліміт під ваш продукт
            if (note.Length > 1000)
                return "Note is too long (max 1000 chars).";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            // Не перезаписуємо, якщо вже є помилка для цього поля
            if (!errors.ContainsKey(key))
                errors[key] = message;
        }
    }
}
