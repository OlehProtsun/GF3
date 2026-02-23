using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPFApp.Converters
{
    public sealed class FirstValidationErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value = ReadOnlyObservableCollection<ValidationError>
            if (value is IEnumerable errors)
            {
                var first = errors.Cast<ValidationError>().FirstOrDefault();
                return first?.ErrorContent?.ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
