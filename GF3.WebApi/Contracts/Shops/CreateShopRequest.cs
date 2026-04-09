using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Shops;

/// <summary>
/// Request payload used to create a new shop.
/// </summary>
public sealed class CreateShopRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
