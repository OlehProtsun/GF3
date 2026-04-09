namespace WebApi.Contracts.AvailabilityGroups.Members;

/// <summary>
/// API representation of one member that belongs to an availability group.
/// </summary>
public sealed class AvailabilityGroupMemberDto
{
    /// <summary>
    /// Persistent member identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Availability group that owns the member row.
    /// </summary>
    public int AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employee represented by the member row.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    public int DisplayOrder { get; set; }
}
