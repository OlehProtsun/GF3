using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.Converters
{
    public sealed class ScheduleMatrixCellReferenceConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values[0] is not IScheduleMatrixStyleProvider provider)
                return null;

            var row = values[1];

            // 2 = SortMemberPath, 3 = Header (fallback)
            var columnName = values[2]?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
                columnName = values[3]?.ToString();

            return provider.TryBuildCellReference(row, columnName, out var cellRef)
                ? cellRef
                : null;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
