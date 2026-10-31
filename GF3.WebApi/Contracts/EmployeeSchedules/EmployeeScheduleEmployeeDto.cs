namespace WebApi.Contracts.EmployeeSchedules;

/// <summary>
/// Employee assignment visible inside a published schedule.
/// </summary>
public sealed class EmployeeScheduleEmployeeDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? MinHoursMonth { get; set; }
    public int DisplayOrder { get; set; }
}
