/*
  Опис файлу: цей модуль містить реалізацію компонента DataRowViewBoolColumnConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WPFApp.Converters
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class DataRowViewBoolColumnConverter : IValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class DataRowViewBoolColumnConverter : IValueConverter
    {
        /// <summary>
        /// Визначає публічний елемент `public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        /// <summary>
        /// Визначає публічний елемент `public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
