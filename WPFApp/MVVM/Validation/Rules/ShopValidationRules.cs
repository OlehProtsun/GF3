/*
  Опис файлу: цей модуль містить реалізацію компонента ShopValidationRules у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using BusinessLogicLayer.Contracts.Shops;

namespace WPFApp.MVVM.Validation.Rules
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ShopValidationRules` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ShopValidationRules
    {
        
        /// <summary>
        /// Визначає публічний елемент `public const string K_Name = "Name";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Name = "Name";
        /// <summary>
        /// Визначає публічний елемент `public const string K_Address = "Address";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Address = "Address";
        /// <summary>
        /// Визначає публічний елемент `public const string K_Description = "Description";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Description = "Description";

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static IReadOnlyDictionary<string, string> ValidateAll(SaveShopRequest? model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(SaveShopRequest? model)
        {
            
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            
            if (model is null)
                return errors;

            
            AddIfError(errors, K_Name, ValidateName(model.Name));

            
            AddIfError(errors, K_Address, ValidateAddress(model.Address));

            
            
            AddIfError(errors, K_Description, ValidateDescription(model.Description));

            return errors;
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string? ValidateProperty(SaveShopRequest? model, string vmPropertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string? ValidateProperty(SaveShopRequest? model, string vmPropertyName)
        {
            
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            
            return vmPropertyName switch
            {
                K_Name => ValidateName(model.Name),
                K_Address => ValidateAddress(model.Address),
                K_Description => ValidateDescription(model.Description),
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

            if (name.Length > 120)
                return "Name is too long (max 120 chars).";

            return null;
        }

        private static string? ValidateAddress(string? address)
        {
            
            address = (address ?? string.Empty).Trim();

            
            if (address.Length == 0)
                return "Address is required.";

            
            if (address.Length < 3)
                return "Address is too short (min 3 chars).";

            if (address.Length > 200)
                return "Address is too long (max 200 chars).";

            return null;
        }

        private static string? ValidateDescription(string? description)
        {
            
            if (string.IsNullOrWhiteSpace(description))
                return null;

            
            description = description.Trim();

            
            if (description.Length > 2000)
                return "Description is too long (max 2000 chars).";

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
