namespace WebApi.Contracts.Containers.SchedulePresets;

/// <summary>
/// API representation of one employee entry inside a schedule preset.
/// </summary>
public sealed class SchedulePresetEmployeeDto
{
    /// <summary>
    /// Persistent preset-employee identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Employee included in the preset.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Minimum monthly hours target stored for the employee.
    /// </summary>
    public int MinHoursMonth { get; set; }
}
