using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.EmploymentContracts;

public sealed class UpdateEmploymentContractSettingsRequest
{
    [Required] public bool? RestrictSwapsToEightHours { get; set; }
    [Required] public bool? RequirePickupApproval { get; set; }
    [Required] public bool? RequireBelowMinimumGiveApproval { get; set; }
    [Required] public bool? AutomaticWeekdayAvailability { get; set; }
}
