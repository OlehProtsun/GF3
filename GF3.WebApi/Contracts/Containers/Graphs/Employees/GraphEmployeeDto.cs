namespace WebApi.Contracts.Containers.Graphs.Employees;

public sealed class GraphEmployeeDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public int? MinHoursMonth { get; set; }
}
