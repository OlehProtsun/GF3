/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewInputHelper у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Linq;
using System.Windows.Input;

namespace WPFApp.View.Availability.Helpers
{
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class AvailabilityViewInputHelper` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class AvailabilityViewInputHelper
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string KeyToBindToken(Key key)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string KeyToBindToken(Key key)
        {
            
            if (key >= Key.A && key <= Key.Z)
                return key.ToString();

            
            if (key >= Key.D0 && key <= Key.D9)
                return ((char)('0' + (key - Key.D0))).ToString();

            
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return ((char)('0' + (key - Key.NumPad0))).ToString();

            
            return key.ToString();
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool IsAllDigits(string? text)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool IsAllDigits(string? text)
        {
            
            if (string.IsNullOrEmpty(text))
                return false;

            
            return text.All(char.IsDigit);
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool IsCommonEditorShortcut(Key key, ModifierKeys modifiers)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool IsCommonEditorShortcut(Key key, ModifierKeys modifiers)
        {
            if (modifiers != ModifierKeys.Control)
                return false;

            return key is Key.C or Key.V or Key.X or Key.Z or Key.Y or Key.A;
        }
    }
}
