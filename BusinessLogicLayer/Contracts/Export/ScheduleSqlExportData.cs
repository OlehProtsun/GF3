namespace BusinessLogicLayer.Contracts.Export;

/// <summary>
/// Root export payload used to serialize one schedule graph and its related data to SQL.
/// </summary>
public sealed class ScheduleSqlExportData
{
    /// <summary>
    /// Export representation of the schedule graph header.
    /// </summary>
    public ScheduleSqlDto Schedule { get; init; } = new();

    /// <summary>
    /// Employee assignments that belong to the graph.
    /// </summary>
    public IReadOnlyList<ScheduleEmployeeSqlDto> Employees { get; init; } = Array.Empty<ScheduleEmployeeSqlDto>();

    /// <summary>
    /// Concrete graph slots that belong to the export.
    /// </summary>
    public IReadOnlyList<ScheduleSlotSqlDto> Slots { get; init; } = Array.Empty<ScheduleSlotSqlDto>();

    /// <summary>
    /// Optional cell-style overrides persisted for the graph.
    /// </summary>
    public IReadOnlyList<ScheduleCellStyleSqlDto> CellStyles { get; init; } = Array.Empty<ScheduleCellStyleSqlDto>();

    /// <summary>
    /// Optional linked availability group snapshot.
    /// </summary>
    public AvailabilityGroupSqlDto? AvailabilityGroup { get; init; }

    /// <summary>
    /// Member rows of the linked availability group.
    /// </summary>
    public IReadOnlyList<AvailabilityGroupMemberSqlDto> AvailabilityMembers { get; init; } = Array.Empty<AvailabilityGroupMemberSqlDto>();

    /// <summary>
    /// Day entries of the linked availability group.
    /// </summary>
    public IReadOnlyList<AvailabilityGroupDaySqlDto> AvailabilityDays { get; init; } = Array.Empty<AvailabilityGroupDaySqlDto>();
}

/// <summary>
/// SQL-export representation of the schedule graph itself.
/// </summary>
public sealed class ScheduleSqlDto
{
    public int Id { get; init; }
    public int ContainerId { get; init; }
    public int ShopId { get; init; }
    public int? AvailabilityGroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
    public int PeoplePerShift { get; init; }
    public string Shift1 { get; init; } = string.Empty;
    public string Shift2 { get; init; } = string.Empty;
    public int MaxHoursPerEmployee { get; init; }
    public int MaxConsecutiveDays { get; init; }
    public int MaxConsecutiveFullShifts { get; init; }
    public int MaxFullShiftsPerMonth { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// SQL-export representation of one graph employee assignment.
/// </summary>
public sealed class ScheduleEmployeeSqlDto
{
    public int Id { get; init; }
    public int ScheduleId { get; init; }
    public int EmployeeId { get; init; }
    public int MinHoursMonth { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// SQL-export representation of one concrete graph slot.
/// </summary>
public sealed class ScheduleSlotSqlDto
{
    public int Id { get; init; }
    public int ScheduleId { get; init; }
    public int DayOfMonth { get; init; }
    public int SlotNo { get; init; }
    public int? EmployeeId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string FromTime { get; init; } = string.Empty;
    public string ToTime { get; init; } = string.Empty;
}

/// <summary>
/// SQL-export representation of one cell-style override.
/// </summary>
public sealed class ScheduleCellStyleSqlDto
{
    public int Id { get; init; }
    public int ScheduleId { get; init; }
    public int EmployeeId { get; init; }
    public int DayOfMonth { get; init; }
    public int? BackgroundArgb { get; init; }
    public int? ForegroundArgb { get; init; }
}

/// <summary>
/// SQL-export representation of the linked availability group.
/// </summary>
public sealed class AvailabilityGroupSqlDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
}

/// <summary>
/// SQL-export representation of one availability-group member.
/// </summary>
public sealed class AvailabilityGroupMemberSqlDto
{
    public int Id { get; init; }
    public int AvailabilityGroupId { get; init; }
    public int EmployeeId { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// SQL-export representation of one availability-group day entry.
/// </summary>
public sealed class AvailabilityGroupDaySqlDto
{
    public int Id { get; init; }
    public int AvailabilityGroupMemberId { get; init; }
    public int DayOfMonth { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string? IntervalStr { get; init; }
}
