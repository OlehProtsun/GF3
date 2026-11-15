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

    /// <summary>
    /// Publication state exposed to assigned employees.
    /// </summary>
    public string PublicationStatus { get; set; } = "private";

    /// <summary>
    /// UTC moment when assigned employees can start seeing this group.
    /// </summary>
    public DateTimeOffset? VisibleFromUtc { get; set; }

    /// <summary>
    /// UTC moment after which assigned employees no longer see this group.
    /// </summary>
    public DateTimeOffset? VisibleToUtc { get; set; }

}
