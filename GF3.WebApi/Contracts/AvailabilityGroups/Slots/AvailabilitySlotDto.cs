using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups.Slots;

public sealed class AvailabilitySlotDto
{
    public int Id { get; set; }
    public int AvailabilityGroupMemberId { get; set; }
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}
