using System.Globalization;
using System.Windows.Controls;

namespace WPFApp.MVVM.Validation.WpfRules
{
    public sealed class MinHoursAtLeastOneRule : ValidationRule
    {
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
