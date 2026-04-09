using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.SchedulePresets;

/// <summary>
/// Request payload that describes one employee assignment inside a schedule preset.
/// </summary>
public sealed class CreateSchedulePresetEmployeeRequest
{
    /// <summary>
    /// Employee that should be included in the preset.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Minimum monthly hours target that should be carried into generated schedules.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int MinHoursMonth { get; set; }
}
