using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace WPFApp.Converters
{
    public sealed class EmployeeTotalHoursHeaderConverter : IMultiValueConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo?> MethodCache = new();

        private static readonly Regex HoursRegex =
            new(@"(\d+)\s*h", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex MinutesRegex =
            new(@"(\d+)\s*m", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = VM (DataGrid.DataContext)
            // values[1] = columnName (Column.SortMemberPath)
            if (values == null || values.Length < 2)
                return string.Empty;

            var vm = values[0];
            var columnName = values[1] as string;

            if (vm == null || string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            // Шукаємо метод: string GetEmployeeTotalHoursText(string columnName)
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

            // raw типово: "Total hours: 6h 30m"
            int h = 0, m = 0;

            var hm = HoursRegex.Match(raw);
            if (hm.Success) int.TryParse(hm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out h);

            var mm = MinutesRegex.Match(raw);
            if (mm.Success) int.TryParse(mm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out m);

            return $"Total Hours: h: {h} m: {m}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
