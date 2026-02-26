/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleMatrixCellBrushConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.Converters
{
    /// <summary>
    /// Визначає публічний елемент `public enum ScheduleMatrixBrushKind` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum ScheduleMatrixBrushKind
    {
        Background,
        Foreground
    }

    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleMatrixCellBrushConverter : IMultiValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleMatrixCellBrushConverter : IMultiValueConverter
    {
        /// <summary>
        /// Визначає публічний елемент `public ScheduleMatrixBrushKind Kind { get; set; } = ScheduleMatrixBrushKind.Background;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleMatrixBrushKind Kind { get; set; } = ScheduleMatrixBrushKind.Background;

        /// <summary>
        /// Визначає публічний елемент `public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values[0] is not IScheduleMatrixStyleProvider provider)
                return Kind == ScheduleMatrixBrushKind.Background
                    ? Brushes.Transparent
                    : DependencyProperty.UnsetValue;

            var row = values[1];

            
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

        /// <summary>
        /// Визначає публічний елемент `public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
