using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Availability;

/// <summary>
/// Performs fast, field-level validation for the availability group header before the service layer
/// starts more expensive ownership checks or persistence work.
/// </summary>
public static class AvailabilityGroupValidator
{
    private const int AllowedPastYearOffset = 1;
    private const int AllowedFutureYearOffset = 5;

    /// <summary>
    /// Returns field-specific validation messages keyed by property name.
    /// </summary>
    public static Dictionary<string, string> Validate(AvailabilityGroupModel model)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            errors[nameof(model.Name)] = "Indicate group name.";
        }

        if (model.Month is < 1 or > 12)
        {
            errors[nameof(model.Month)] = "Month must be between 1 and 12.";
        }

        if (!IsYearWithinSupportedWindow(model.Year))
        {
            errors[nameof(model.Year)] = "Invalid year.";
        }

        return errors;
    }

    private static bool IsYearWithinSupportedWindow(int year)
    {
        var currentYear = DateTime.Today.Year;
        return year >= currentYear - AllowedPastYearOffset && year <= currentYear + AllowedFutureYearOffset;
    }
}
