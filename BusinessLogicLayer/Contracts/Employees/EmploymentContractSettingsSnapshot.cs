namespace BusinessLogicLayer.Contracts.Employees;

public sealed record EmploymentContractSettingsSnapshot(
    bool RestrictSwapsToEightHours = true,
    bool RequirePickupApproval = true,
    bool RequireBelowMinimumGiveApproval = true,
    bool AutomaticWeekdayAvailability = true);
