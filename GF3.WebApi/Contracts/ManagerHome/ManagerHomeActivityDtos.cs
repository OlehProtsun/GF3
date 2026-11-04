namespace WebApi.Contracts.ManagerHome;

public sealed record ManagerHomeActivityItemDto
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public required string EmployeeName { get; init; }
    public required string Status { get; init; }
    public required bool RequiresApproval { get; init; }
    public required string TargetType { get; init; }
    public string? CounterpartyName { get; init; }
    public string? ScheduleName { get; init; }
    public string? ShopName { get; init; }
    public DateOnly? ShiftDate { get; init; }
    public string? FromTime { get; init; }
    public string? ToTime { get; init; }
    public DateOnly? ReturnShiftDate { get; init; }
    public string? ReturnFromTime { get; init; }
    public string? ReturnToTime { get; init; }
    public IReadOnlyList<DateOnly>? VacationDays { get; init; }
    public int? ContainerId { get; init; }
    public int? GraphId { get; init; }
    public int? AvailabilityGroupId { get; init; }
    public int? ShopId { get; init; }
    public int? EmployeeId { get; init; }
    public int? Year { get; init; }
    public int? Month { get; init; }
}

public sealed record ManagerHomeActivityPageDto(
    IReadOnlyList<ManagerHomeActivityItemDto> Items,
    int AllActivityCount,
    int RequiresApprovalCount,
    bool HasMore,
    int NextOffset);
