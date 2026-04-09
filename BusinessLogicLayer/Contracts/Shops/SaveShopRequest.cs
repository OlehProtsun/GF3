namespace BusinessLogicLayer.Contracts.Shops;

/// <summary>
/// Command-style DTO used by shop create and update flows in the business layer.
/// </summary>
public sealed class SaveShopRequest
{
    /// <summary>
    /// <c>0</c> means create; a positive value means update the existing shop with that identifier.
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
