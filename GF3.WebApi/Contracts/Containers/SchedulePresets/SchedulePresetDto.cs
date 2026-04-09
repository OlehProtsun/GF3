namespace WebApi.Contracts.Containers.SchedulePresets;

/// <summary>
/// API representation of one schedule preset returned to clients.
/// </summary>
public sealed class SchedulePresetDto
{
    /// <summary>
    /// Persistent preset identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Container that owns the preset.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Human-readable preset name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default schedule name suggested when the preset is materialized.
    /// </summary>
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Shop or location that the preset targets.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// Default calendar year used when creating a schedule from the preset.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Default calendar month used when creating a schedule from the preset.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts during the whole month.
    /// </summary>
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional availability group used as a default generation constraint.
    /// </summary>
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employees stored inside the preset.
    /// </summary>
    public IReadOnlyList<SchedulePresetEmployeeDto> Employees { get; set; } = Array.Empty<SchedulePresetEmployeeDto>();
}
