namespace WebApi.Contracts.AvailabilityGroups;

/// <summary>
/// API representation of one availability group.
/// </summary>
public sealed class AvailabilityGroupDto
{
    /// <summary>
    /// Persistent availability group identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the group applies to.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the group applies to.
    /// </summary>
    public int Month { get; set; }
}
