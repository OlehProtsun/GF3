using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            // якщо немає провайдера — для фону все одно даємо Transparent (щоб клітинка ловила клік)
            if (values.Length < 3 || values[0] is not IScheduleMatrixStyleProvider provider)
                return Kind == ScheduleMatrixBrushKind.Background
                    ? Brushes.Transparent
                    : DependencyProperty.UnsetValue;

            var row = values[1];
            var columnName = values[2]?.ToString();

            if (!provider.TryBuildCellReference(row, columnName, out var cellRef))
                return Kind == ScheduleMatrixBrushKind.Background
                    ? Brushes.Transparent
                    : DependencyProperty.UnsetValue;

            var brush = Kind == ScheduleMatrixBrushKind.Background
                ? provider.GetCellBackgroundBrush(cellRef)
                : provider.GetCellForegroundBrush(cellRef);

            // ключовий момент:
            // - Background: якщо немає стилю — Transparent (hit-test по всій клітинці)
            // - Foreground: якщо немає стилю — UnsetValue (не ламаємо дефолтний колір тексту)
            if (Kind == ScheduleMatrixBrushKind.Background)
                return brush ?? Brushes.Transparent;

            return brush ?? DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
