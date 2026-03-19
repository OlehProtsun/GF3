using BusinessLogicLayer.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups.Slots;

public sealed class UpdateAvailabilitySlotRequest
{
    [Range(1, int.MaxValue)]
    public int AvailabilityGroupMemberId { get; set; }

    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    [Required]
    public AvailabilityKind Kind { get; set; }

    [StringLength(200)]
    public string? IntervalStr { get; set; }
}
