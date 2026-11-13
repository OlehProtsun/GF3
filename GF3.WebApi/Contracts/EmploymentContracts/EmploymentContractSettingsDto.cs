namespace WebApi.Contracts.EmploymentContracts;

public sealed record EmploymentContractSettingsDto(bool RestrictSwapsToEightHours, bool RequirePickupApproval,
    bool RequireBelowMinimumGiveApproval, bool AutomaticWeekdayAvailability);
