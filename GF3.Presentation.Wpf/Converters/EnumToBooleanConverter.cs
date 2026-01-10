using System;
using System.Globalization;
using System.Windows.Data;

namespace GF3.Presentation.Wpf.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null)
            {
                return false;
            }

            var stringValue = parameter.ToString();
            return stringValue is not null && value.ToString() == stringValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is null)
            {
                return Binding.DoNothing;
            }

            var isChecked = value as bool?;
            if (isChecked == true)
            {
                return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
            }

            return Binding.DoNothing;
        }
    }
}
