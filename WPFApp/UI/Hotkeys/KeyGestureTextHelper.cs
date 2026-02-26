/*
  Опис файлу: цей модуль містить реалізацію компонента KeyGestureTextHelper у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace WPFApp.UI.Hotkeys
{
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class KeyGestureTextHelper` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class KeyGestureTextHelper
    {
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string? FormatKeyGesture(Key key, ModifierKeys modifiers, CultureInfo? culture = null)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string? FormatKeyGesture(Key key, ModifierKeys modifiers, CultureInfo? culture = null)
        {
            
            
            culture ??= CultureInfo.InvariantCulture;

            
            if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin)
            {
                return null;
            }

            try
            {
                
                var gesture = new KeyGesture(key, modifiers);

                
                return gesture.GetDisplayStringForCulture(culture);
            }
            catch
            {
                
                return null;
            }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryNormalizeKey(string raw, out string normalized, CultureInfo? culture = null)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryNormalizeKey(string raw, out string normalized, CultureInfo? culture = null)
        {
            
            normalized = string.Empty;

            
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            
            culture ??= CultureInfo.InvariantCulture;

            
            raw = raw.Trim();

            
            
            
            if (!raw.Contains('+'))
            {
                normalized = raw.ToUpperInvariant();
                return true;
            }

            
            
            
            
            
            var cleaned = string.Join("+",
                raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p)));

            
            
            if (!cleaned.Contains('+'))
                return false;

            
            var converter = new KeyGestureConverter();

            try
            {
                
                if (converter.ConvertFromString(cleaned) is KeyGesture gesture1)
                {
                    normalized = gesture1.GetDisplayStringForCulture(culture);
                    return true;
                }

                
                if (converter.ConvertFromString(raw) is KeyGesture gesture2)
                {
                    normalized = gesture2.GetDisplayStringForCulture(culture);
                    return true;
                }
            }
            catch
            {
                
                return false;
            }

            
            return false;
        }
    }
}
