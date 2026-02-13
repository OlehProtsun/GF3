using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WPFApp.Converters
{
    public sealed class DataRowViewBoolColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var col = parameter as string;
            if (string.IsNullOrWhiteSpace(col)) return false;

            if (value is DataRowView drv &&
                drv.DataView?.Table?.Columns.Contains(col) == true)
            {
                var cell = drv[col];
                if (cell is DBNull || cell is null) return false;
                if (cell is bool b) return b;
                return bool.TryParse(cell.ToString(), out var parsed) && parsed;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
