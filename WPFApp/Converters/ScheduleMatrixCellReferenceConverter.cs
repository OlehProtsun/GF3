using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container;

namespace WPFApp.Converters
{
    public sealed class ScheduleMatrixCellReferenceConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not IScheduleMatrixStyleProvider provider)
                return null;

            var row = values[1];
            var columnName = values[2]?.ToString();

            return provider.TryBuildCellReference(row, columnName, out var cellRef)
                ? cellRef
                : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
