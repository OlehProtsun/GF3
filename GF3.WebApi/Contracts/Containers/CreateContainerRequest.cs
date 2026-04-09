using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers;

/// <summary>
/// Request payload used to create a new container.
/// A container acts as a parent aggregate for schedule graphs and presets.
/// </summary>
public sealed class CreateContainerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }
}
