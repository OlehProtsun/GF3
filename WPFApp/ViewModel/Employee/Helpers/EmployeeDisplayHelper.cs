/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeDisplayHelper у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Employees;
using System;

namespace WPFApp.ViewModel.Employee.Helpers
{
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class EmployeeDisplayHelper` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class EmployeeDisplayHelper
    {
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string GetFullName(EmployeeDto? model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string GetFullName(EmployeeDto? model)
        {
            
            if (model is null)
                return string.Empty;

            
            var first = (model.FirstName ?? string.Empty).Trim();
            var last = (model.LastName ?? string.Empty).Trim();

            
            
            var full = $"{first} {last}".Trim();

            
            return full;
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string TextOrDash(string? value)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string TextOrDash(string? value)
        {
            
            if (string.IsNullOrWhiteSpace(value))
                return "—";

            
            return value.Trim();
        }
    }
}
