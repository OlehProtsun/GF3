using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.SchedulePresets;

/// <summary>
/// Request payload used to create a schedule preset under one container.
/// The preset captures a reusable graph configuration together with employee defaults.
/// </summary>
public sealed class CreateSchedulePresetRequest
{
    /// <summary>
    /// Human-readable preset name shown in management screens.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default schedule name suggested when the preset is materialized.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Shop or location that the preset targets.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int ShopId { get; set; }

    /// <summary>
    /// Default calendar year used when creating a schedule from the preset.
    /// </summary>
    [Required]
    [Range(1, 9999)]
    public int Year { get; set; }

    /// <summary>
    /// Default calendar month used when creating a schedule from the preset.
    /// </summary>
    [Required]
    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts during the whole month.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional availability group used as a default generation constraint.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employee defaults that should be stored together with the preset.
    /// </summary>
    public IReadOnlyList<CreateSchedulePresetEmployeeRequest> Employees { get; set; } = Array.Empty<CreateSchedulePresetEmployeeRequest>();
}
