using System.ComponentModel.DataAnnotations;
using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.EmployeeAvailability;

/// <summary>
/// Employee self-service payload for replacing their own availability entries.
/// </summary>
public sealed class UpdateEmployeeAvailabilityRequest
{
    [Required]
    public List<UpdateEmployeeAvailabilitySlotRequest> Slots { get; set; } = [];
}

public sealed class UpdateEmployeeAvailabilitySlotRequest
{
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    public AvailabilityKind Kind { get; set; }

    public string? IntervalStr { get; set; }
}
