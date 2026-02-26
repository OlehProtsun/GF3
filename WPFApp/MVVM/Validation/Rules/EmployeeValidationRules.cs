/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeValidationRules у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Contracts.Employees;

namespace WPFApp.MVVM.Validation.Rules
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class EmployeeValidationRules` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class EmployeeValidationRules
    {
        
        
        

        /// <summary>
        /// Визначає публічний елемент `public const string K_FirstName = "FirstName";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_FirstName = "FirstName";
        /// <summary>
        /// Визначає публічний елемент `public const string K_LastName = "LastName";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_LastName = "LastName";
        /// <summary>
        /// Визначає публічний елемент `public const string K_Email = "Email";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Email = "Email";
        /// <summary>
        /// Визначає публічний елемент `public const string K_Phone = "Phone";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Phone = "Phone";

        
        
        

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static readonly Regex EmailRegex =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly Regex EmailRegex =
            new(@"^\S+@\S+\.\S+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static readonly Regex PhoneRegex =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly Regex PhoneRegex =
            new(@"^[0-9+\-\s()]{5,}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        
        
        

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static IReadOnlyDictionary<string, string> ValidateAll(SaveEmployeeRequest? model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(SaveEmployeeRequest? model)
        {
            
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            
            if (model is null)
                return errors;

            
            AddIfError(errors, K_FirstName, ValidateFirstName(model.FirstName));

            
            AddIfError(errors, K_LastName, ValidateLastName(model.LastName));

            
            AddIfError(errors, K_Email, ValidateEmail(model.Email));

            
            AddIfError(errors, K_Phone, ValidatePhone(model.Phone));

            
            return errors;
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string? ValidateProperty(SaveEmployeeRequest? model, string vmPropertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string? ValidateProperty(SaveEmployeeRequest? model, string vmPropertyName)
        {
            
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            
            return vmPropertyName switch
            {
                K_FirstName => ValidateFirstName(model.FirstName),
                K_LastName => ValidateLastName(model.LastName),
                K_Email => ValidateEmail(model.Email),
                K_Phone => ValidatePhone(model.Phone),
                _ => null
            };
        }

        
        
        

        private static string? ValidateFirstName(string? firstName)
        {
            
            firstName = (firstName ?? string.Empty).Trim();

            
            if (firstName.Length == 0)
                return "First name is required.";

            
            if (firstName.Length < 2)
                return "First name is too short (min 2 chars).";

            
            if (firstName.Length > 100)
                return "First name is too long (max 100 chars).";

            
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
            
            if (string.IsNullOrWhiteSpace(email))
                return null;

            
            email = email.Trim();

            
            if (!EmailRegex.IsMatch(email))
                return "Invalid email format.";

            return null;
        }

        private static string? ValidatePhone(string? phone)
        {
            
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            
            phone = phone.Trim();

            
            if (!PhoneRegex.IsMatch(phone))
                return "Invalid phone number.";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            
            if (string.IsNullOrWhiteSpace(message))
                return;

            
            errors[key] = message;
        }
    }
}
