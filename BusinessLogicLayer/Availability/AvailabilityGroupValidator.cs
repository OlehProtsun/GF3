using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Availability
{
    public static class AvailabilityGroupValidator
    {
        public static Dictionary<string, string> Validate(AvailabilityGroupModel model)
        {
            var errors = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(model.Name)] = "Indicate group name.";

            if (model.Month < 1 || model.Month > 12)
                errors[nameof(model.Month)] = "Month must be between 1 and 12.";

            if (model.Year < DateTime.Today.Year - 1 || model.Year > DateTime.Today.Year + 5)
                errors[nameof(model.Year)] = "Invalid year.";

            return errors;
        }
    }
}
