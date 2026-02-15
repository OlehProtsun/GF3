using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPFApp.Infrastructure.Validation
{
    /// <summary>
    /// Safely returns the first ValidationError.ErrorContent (or null).
    /// Avoids (Validation.Errors)[0] which can throw ArgumentOutOfRangeException.
    /// </summary>
    public sealed class FirstValidationErrorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // WPF provides Validation.Errors as ReadOnlyObservableCollection<ValidationError>
            if (value is System.Collections.Generic.IEnumerable<ValidationError> errors)
            {
                var first = errors.FirstOrDefault();
                return first?.ErrorContent?.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
