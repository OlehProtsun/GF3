using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs;

public sealed class CreateContainerGraphRequest
{
    [Range(1, int.MaxValue)]
    public int ShopId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 9999)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(1, int.MaxValue)]
    public int PeoplePerShift { get; set; }

    [Required]
    [MaxLength(50)]
    public string Shift1Time { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Shift2Time { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxHoursPerEmpMonth { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxConsecutiveDays { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxConsecutiveFull { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxFullPerMonth { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    [Range(1, int.MaxValue)]
    public int? AvailabilityGroupId { get; set; }
}
