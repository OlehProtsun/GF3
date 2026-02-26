/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityValidationRules у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;

namespace WPFApp.MVVM.Validation.Rules
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityValidationRules` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityValidationRules
    {
        
        /// <summary>
        /// Визначає публічний елемент `public const string K_AvailabilityName = "AvailabilityName";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_AvailabilityName = "AvailabilityName";
        /// <summary>
        /// Визначає публічний елемент `public const string K_AvailabilityMonth = "AvailabilityMonth";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_AvailabilityMonth = "AvailabilityMonth";
        /// <summary>
        /// Визначає публічний елемент `public const string K_AvailabilityYear = "AvailabilityYear";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_AvailabilityYear = "AvailabilityYear";

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static IReadOnlyDictionary<string, string> ValidateAll(string? name, int year, int month)` та контракт його використання у шарі WPFApp.
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
        /// Визначає публічний елемент `public static string? ValidateProperty(string? name, int year, int month, string vmPropertyName)` та контракт його використання у шарі WPFApp.
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

        
        
        

        private static string? ValidateName(string? name)
        {
            name = (name ?? string.Empty).Trim();

            if (name.Length == 0)
                return "Name is required.";

            if (name.Length < 2)
                return "Name is too short (min 2 chars).";

            
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
