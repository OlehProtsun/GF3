using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Contracts.Availability;

/// <summary>
/// Employee-scoped view of one published availability group.
/// </summary>
public sealed class EmployeeAvailabilityModel
{
    public AvailabilityGroupModel Group { get; init; } = new();

    public AvailabilityGroupMemberModel Member { get; init; } = new();

    public IReadOnlyList<AvailabilityGroupDayModel> Days { get; init; } = Array.Empty<AvailabilityGroupDayModel>();

    public bool CanSubmit { get; init; }
}
