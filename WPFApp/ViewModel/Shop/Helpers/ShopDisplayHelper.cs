/*
  Опис файлу: цей модуль містить реалізацію компонента ShopDisplayHelper у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Shops;
﻿
namespace WPFApp.ViewModel.Shop.Helpers
{
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ShopDisplayHelper` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ShopDisplayHelper
    {
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string TextOrDash(string? value)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string TextOrDash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "—";

            return value.Trim();
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string NameOrEmpty(ShopDto? model)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string NameOrEmpty(ShopDto? model)
            => model?.Name?.Trim() ?? string.Empty;
    }
}
