namespace WebApi.Contracts.EmployeeSchedules;

/// <summary>
/// Non-interactive hint that an employee works in another schedule on the same day.
/// </summary>
public sealed class EmployeeScheduleRelatedAssignmentDto
{
    public int EmployeeId { get; set; }
    public int DayOfMonth { get; set; }
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
}