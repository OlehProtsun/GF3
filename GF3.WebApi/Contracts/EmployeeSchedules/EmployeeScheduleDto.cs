namespace WebApi.Contracts.EmployeeSchedules;

/// <summary>
/// Published schedule projection visible to the current employee.
/// </summary>
public sealed class EmployeeScheduleDto
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public string ContainerName { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string PublicationStatus { get; set; } = "public";
    public bool AllowSwap { get; set; } = true;
    public DateTimeOffset? LastUpdatedAtUtc { get; set; }
    public IReadOnlyList<EmployeeScheduleEmployeeDto> Employees { get; set; } = Array.Empty<EmployeeScheduleEmployeeDto>();
    public IReadOnlyList<EmployeeScheduleSlotDto> Slots { get; set; } = Array.Empty<EmployeeScheduleSlotDto>();
    public IReadOnlyList<EmployeeScheduleRelatedAssignmentDto> RelatedScheduleAssignments { get; set; } = Array.Empty<EmployeeScheduleRelatedAssignmentDto>();
}
