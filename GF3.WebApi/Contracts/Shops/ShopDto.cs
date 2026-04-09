namespace WebApi.Contracts.Shops;

/// <summary>
/// API representation of one shop returned to clients.
/// </summary>
public sealed class ShopDto
{
    /// <summary>
    /// Persistent shop identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable shop name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the shop or location.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional description shown in management screens.
    /// </summary>
    public string? Description { get; set; }
}
