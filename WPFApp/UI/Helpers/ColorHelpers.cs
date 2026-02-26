/*
  Опис файлу: цей модуль містить реалізацію компонента ColorHelpers у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Media;

namespace WPFApp.UI.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ColorHelpers` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ColorHelpers
    {
        
        
        
        private static readonly ConcurrentDictionary<int, SolidColorBrush> _brushCache = new();

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static int ToArgb(Color color)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static int ToArgb(Color color)
            
            => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static Color FromArgb(int argb)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static Color FromArgb(int argb)
        {
            
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);

            
            return Color.FromArgb(a, r, g, b);
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static SolidColorBrush ToBrush(int argb)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static SolidColorBrush ToBrush(int argb)
        {
            
            
            
            return _brushCache.GetOrAdd(argb, static key =>
            {
                
                var brush = new SolidColorBrush(FromArgb(key));

                
                brush.Freeze();

                return brush;
            });
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static SolidColorBrush ToBrush(Color color)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static SolidColorBrush ToBrush(Color color)
            => ToBrush(ToArgb(color));

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static void ClearBrushCache()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static void ClearBrushCache()
            => _brushCache.Clear();

        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static bool TryParseHexColor(string? hex, out Color color)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool TryParseHexColor(string? hex, out Color color)
        {
            
            color = default;

            
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            
            ReadOnlySpan<char> span = hex.AsSpan().Trim();

            
            if (span.Length > 0 && span[0] == '#')
                span = span.Slice(1);

            
            
            Span<char> buffer = stackalloc char[8];

            
            if (span.Length == 3)
            {
                buffer[0] = 'F';
                buffer[1] = 'F';
                buffer[2] = span[0]; buffer[3] = span[0]; 
                buffer[4] = span[1]; buffer[5] = span[1]; 
                buffer[6] = span[2]; buffer[7] = span[2]; 
            }
            
            else if (span.Length == 4)
            {
                buffer[0] = span[0]; buffer[1] = span[0]; 
                buffer[2] = span[1]; buffer[3] = span[1]; 
                buffer[4] = span[2]; buffer[5] = span[2]; 
                buffer[6] = span[3]; buffer[7] = span[3]; 
            }
            
            else if (span.Length == 6)
            {
                buffer[0] = 'F';
                buffer[1] = 'F';
                span.CopyTo(buffer.Slice(2));
            }
            
            else if (span.Length == 8)
            {
                span.CopyTo(buffer);
            }
            else
            {
                
                return false;
            }

            
            if (!uint.TryParse(buffer, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                return false;

            
            color = FromArgb(unchecked((int)argb));

            return true;
        }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static string ToHex(Color color, bool includeAlpha = true)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static string ToHex(Color color, bool includeAlpha = true)
        {
            
            if (!includeAlpha)
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
