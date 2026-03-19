/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerValidationRules у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;

namespace WPFApp.MVVM.Validation.Rules
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ContainerValidationRules` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ContainerValidationRules
    {
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public const string K_Name = "Name";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Name = "Name";
        /// <summary>
        /// Визначає публічний елемент `public const string K_Note = "Note";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public const string K_Note = "Note";

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static IReadOnlyDictionary<string, string> ValidateAll(ContainerModel? model)` та контракт його використання у шарі WPFApp.
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
        /// Визначає публічний елемент `public static string? ValidateProperty(ContainerModel? model, string vmPropertyName)` та контракт його використання у шарі WPFApp.
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

        
        
        

        private static string? ValidateName(string? name)
        {
            name = (name ?? string.Empty).Trim();

            if (name.Length == 0)
                return "Name is required.";

            
            if (name.Length > 100)
                return "Name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateNote(string? note)
        {
            
            if (string.IsNullOrWhiteSpace(note))
                return null;

            
            if (note.Length > 1000)
                return "Note is too long (max 1000 chars).";

            return null;
        }

        private static void AddIfError(Dictionary<string, string> errors, string key, string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            
            if (!errors.ContainsKey(key))
                errors[key] = message;
        }
    }
}
