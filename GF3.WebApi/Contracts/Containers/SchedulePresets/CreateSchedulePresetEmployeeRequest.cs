using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.SchedulePresets;

public sealed class CreateSchedulePresetEmployeeRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int MinHoursMonth { get; set; }
}
