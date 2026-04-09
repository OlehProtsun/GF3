namespace WebApi.Contracts.AvailabilityBinds;

/// <summary>
/// API representation of one availability bind entry.
/// </summary>
public sealed class AvailabilityBindDto
{
    /// <summary>
    /// Persistent bind identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Keyboard shortcut string that triggers the bind.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Availability code inserted by the shortcut.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the bind is currently enabled.
    /// </summary>
    public bool IsActive { get; set; }
}
