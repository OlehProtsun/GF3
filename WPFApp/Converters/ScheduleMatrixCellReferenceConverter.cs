/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleMatrixCellReferenceConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;

namespace WPFApp.Converters
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleMatrixCellReferenceConverter : IMultiValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleMatrixCellReferenceConverter : IMultiValueConverter
    {
        /// <summary>
        /// Визначає публічний елемент `public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values[0] is not IScheduleMatrixStyleProvider provider)
                return null;

            var row = values[1];

            
            var columnName = values[2]?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
                columnName = values[3]?.ToString();

            return provider.TryBuildCellReference(row, columnName, out var cellRef)
                ? cellRef
                : null;
        }


        /// <summary>
        /// Визначає публічний елемент `public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
