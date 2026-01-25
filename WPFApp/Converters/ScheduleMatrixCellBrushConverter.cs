using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

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

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values[0] is not IScheduleMatrixStyleProvider provider)
                return Kind == ScheduleMatrixBrushKind.Background
                    ? Brushes.Transparent
                    : DependencyProperty.UnsetValue;

            var row = values[1];

            // 2 = SortMemberPath, 3 = Header (fallback)
            var columnName = values[2]?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
                columnName = values[3]?.ToString();

            if (!provider.TryBuildCellReference(row, columnName, out var cellRef))
                return Kind == ScheduleMatrixBrushKind.Background
                    ? Brushes.Transparent
                    : DependencyProperty.UnsetValue;

            var brush = Kind == ScheduleMatrixBrushKind.Background
                ? provider.GetCellBackgroundBrush(cellRef)
                : provider.GetCellForegroundBrush(cellRef);

            if (Kind == ScheduleMatrixBrushKind.Background)
                return brush ?? Brushes.Transparent;

            return brush ?? DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
