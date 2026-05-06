using WebApi.Contracts.AvailabilityGroups.Slots;

namespace WebApi.Contracts.EmployeeAvailability;

/// <summary>
/// Employee-scoped representation of one published availability group.
/// </summary>
public sealed class EmployeeAvailabilityGroupDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Month { get; set; }

    public DateTimeOffset? VisibleFromUtc { get; set; }

    public DateTimeOffset? VisibleToUtc { get; set; }

    public bool CanSubmit { get; set; }

    public DateTimeOffset? EmployeeLastModifiedAtUtc { get; set; }

    public IReadOnlyList<AvailabilitySlotDto> Slots { get; set; } = Array.Empty<AvailabilitySlotDto>();
}
