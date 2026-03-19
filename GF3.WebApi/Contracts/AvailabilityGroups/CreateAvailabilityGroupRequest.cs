using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups;

public sealed class CreateAvailabilityGroupRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 9999)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }
}
