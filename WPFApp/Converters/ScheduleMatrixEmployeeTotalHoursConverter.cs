/*
  Опис файлу: цей модуль містить реалізацію компонента ScheduleMatrixEmployeeTotalHoursConverter у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Windows.Data;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.Profile;
using WPFApp.ViewModel.Container.ScheduleEdit;
using WPFApp.ViewModel.Container.ScheduleProfile;

namespace WPFApp.Converters
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class ScheduleMatrixEmployeeTotalHoursConverter : IMultiValueConverter` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ScheduleMatrixEmployeeTotalHoursConverter : IMultiValueConverter
    {
        /// <summary>
        /// Визначає публічний елемент `public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
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

        /// <summary>
        /// Визначає публічний елемент `public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
