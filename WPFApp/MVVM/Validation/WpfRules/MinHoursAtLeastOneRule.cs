/*
  Опис файлу: цей модуль містить реалізацію компонента MinHoursAtLeastOneRule у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Globalization;
using System.Windows.Controls;

namespace WPFApp.MVVM.Validation.WpfRules
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class MinHoursAtLeastOneRule : ValidationRule` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class MinHoursAtLeastOneRule : ValidationRule
    {
        /// <summary>
        /// Визначає публічний елемент `public override ValidationResult Validate(object value, CultureInfo cultureInfo)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is null)
                return new ValidationResult(false, "Min hours must be at least 1.");

            if (value is int i)
                return i >= 1 ? ValidationResult.ValidResult : new ValidationResult(false, "Min hours must be at least 1.");

            if (value is string s && int.TryParse(s, out var n))
                return n >= 1 ? ValidationResult.ValidResult : new ValidationResult(false, "Min hours must be at least 1.");

            return new ValidationResult(false, "Min hours must be a number.");
        }
    }
}
