namespace WebApi.Contracts.Containers.SchedulePresets;

public sealed class SchedulePresetEmployeeDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int MinHoursMonth { get; set; }
}
