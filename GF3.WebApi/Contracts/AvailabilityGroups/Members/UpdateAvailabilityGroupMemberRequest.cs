using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups.Members;

/// <summary>
/// Request payload used to update one member inside an availability group.
/// </summary>
public sealed class UpdateAvailabilityGroupMemberRequest
{
    /// <summary>
    /// Employee that should be included in the availability group.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
