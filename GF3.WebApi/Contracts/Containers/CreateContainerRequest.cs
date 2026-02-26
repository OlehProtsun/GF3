using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers;

public sealed class CreateContainerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }
}
