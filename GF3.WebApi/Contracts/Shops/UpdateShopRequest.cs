using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Shops;

/// <summary>
/// Request payload used to update an existing shop.
/// </summary>
public sealed class UpdateShopRequest
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
