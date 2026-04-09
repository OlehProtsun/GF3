namespace WebApi.Contracts.Containers;

/// <summary>
/// API representation of one container returned to clients.
/// </summary>
public sealed class ContainerDto
{
    /// <summary>
    /// Persistent container identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable container name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional free-form note attached to the container.
    /// </summary>
    public string? Note { get; set; }
}
