/*
  Опис файлу: цей модуль містить реалізацію компонента FirstValidationErrorConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі./
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPFApp.Converters.Validation
{
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class FirstValidationErrorConverter : IValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class FirstValidationErrorConverter : IValueConverter
    {
        /// <summary>
        /// Визначає публічний елемент `public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is System.Collections.Generic.IEnumerable<ValidationError> errors)
            {
                var first = errors.FirstOrDefault();
                return first?.ErrorContent?.ToString();
            }

            return null;
        }

        /// <summary>
        /// Визначає публічний елемент `public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
