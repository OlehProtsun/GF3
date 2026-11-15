using WebApi.Contracts.AvailabilityGroups.Slots;

namespace WebApi.Contracts.EmployeeAvailability;

public sealed class EmployeeAvailabilityMemberDto
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public IReadOnlyList<AvailabilitySlotDto> Slots { get; set; } = Array.Empty<AvailabilitySlotDto>();
}
