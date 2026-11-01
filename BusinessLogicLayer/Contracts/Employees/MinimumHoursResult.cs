namespace BusinessLogicLayer.Contracts.Employees;

public sealed record MinimumHoursResult(bool IsConfigured, int? MinimumMinutes, int CurrentMinutes);
