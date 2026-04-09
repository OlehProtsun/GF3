using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups;

/// <summary>
/// Request payload used to update an existing availability group.
/// </summary>
public sealed class UpdateAvailabilityGroupRequest
{
    /// <summary>
    /// Human-readable group name shown in planner screens.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the group applies to.
    /// </summary>
    [Range(1, 9999)]
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the group applies to.
    /// </summary>
    [Range(1, 12)]
    public int Month { get; set; }
}
