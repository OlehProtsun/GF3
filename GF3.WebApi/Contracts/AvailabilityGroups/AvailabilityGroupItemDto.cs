using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups;

public sealed class AvailabilityGroupItemDto
{
    public int MemberId { get; set; }
    public int EmployeeId { get; set; }
    public int DayId { get; set; }
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}
