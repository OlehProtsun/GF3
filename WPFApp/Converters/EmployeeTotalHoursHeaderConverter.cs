/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeTotalHoursHeaderConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace WPFApp.Converters
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeTotalHoursHeaderConverter : IMultiValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeTotalHoursHeaderConverter : IMultiValueConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo?> MethodCache = new();

        private static readonly Regex HoursRegex =
            new(@"(\d+)\s*h", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex MinutesRegex =
            new(@"(\d+)\s*m", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Визначає публічний елемент `public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            
            
            if (values == null || values.Length < 2)
                return string.Empty;

            var vm = values[0];
            var columnName = values[1] as string;

            if (vm == null || string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            
            var method = MethodCache.GetOrAdd(vm.GetType(),
                t => t.GetMethod("GetEmployeeTotalHoursText", BindingFlags.Instance | BindingFlags.Public,
                                 binder: null, types: new[] { typeof(string) }, modifiers: null));

            if (method == null)
                return string.Empty;

            string raw;
            try
            {
                raw = method.Invoke(vm, new object[] { columnName }) as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            
            int h = 0, m = 0;

            var hm = HoursRegex.Match(raw);
            if (hm.Success) int.TryParse(hm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out h);

            var mm = MinutesRegex.Match(raw);
            if (mm.Success) int.TryParse(mm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out m);

            return $"Total Hours: h: {h} m: {m}";
        }

        /// <summary>
        /// Визначає публічний елемент `public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
