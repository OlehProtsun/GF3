using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container;

namespace WPFApp.Converters
{
    public sealed class ScheduleMatrixEmployeeTotalHoursConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return string.Empty;

            var header = values[1]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(header))
                return string.Empty;

            return values[0] switch
            {
                ContainerScheduleEditViewModel editVm => editVm.GetEmployeeTotalHoursText(header),
                ContainerScheduleProfileViewModel profileVm => profileVm.GetEmployeeTotalHoursText(header),
                _ => string.Empty
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
