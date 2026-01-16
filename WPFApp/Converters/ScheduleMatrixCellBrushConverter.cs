using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container;

namespace WPFApp.Converters
{
    public enum ScheduleMatrixBrushKind
    {
        Background,
        Foreground
    }

    public sealed class ScheduleMatrixCellBrushConverter : IMultiValueConverter
    {
        public ScheduleMatrixBrushKind Kind { get; set; } = ScheduleMatrixBrushKind.Background;

        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not IScheduleMatrixStyleProvider provider)
                return null;

            var row = values[1];
            var columnName = values[2]?.ToString();

            if (!provider.TryBuildCellReference(row, columnName, out var cellRef))
                return null;

            return Kind == ScheduleMatrixBrushKind.Background
                ? provider.GetCellBackgroundBrush(cellRef)
                : provider.GetCellForegroundBrush(cellRef);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
