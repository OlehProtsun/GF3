namespace BusinessLogicLayer.Contracts.Export
{
    public sealed class ScheduleSqlExportData
    {
        public ScheduleSqlDto Schedule { get; init; } = new();
        public IReadOnlyList<ScheduleEmployeeSqlDto> Employees { get; init; } = Array.Empty<ScheduleEmployeeSqlDto>();
        public IReadOnlyList<ScheduleSlotSqlDto> Slots { get; init; } = Array.Empty<ScheduleSlotSqlDto>();
        public IReadOnlyList<ScheduleCellStyleSqlDto> CellStyles { get; init; } = Array.Empty<ScheduleCellStyleSqlDto>();
        public AvailabilityGroupSqlDto? AvailabilityGroup { get; init; }
        public IReadOnlyList<AvailabilityGroupMemberSqlDto> AvailabilityMembers { get; init; } = Array.Empty<AvailabilityGroupMemberSqlDto>();
        public IReadOnlyList<AvailabilityGroupDaySqlDto> AvailabilityDays { get; init; } = Array.Empty<AvailabilityGroupDaySqlDto>();
    }

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

    public sealed class ScheduleEmployeeSqlDto
    {
        public int Id { get; init; }
        public int ScheduleId { get; init; }
        public int EmployeeId { get; init; }
        public int MinHoursMonth { get; init; }
    }

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

    public sealed class ScheduleCellStyleSqlDto
    {
        public int Id { get; init; }
        public int ScheduleId { get; init; }
        public int EmployeeId { get; init; }
        public int DayOfMonth { get; init; }
        public int? BackgroundArgb { get; init; }
        public int? ForegroundArgb { get; init; }
    }

    public sealed class AvailabilityGroupSqlDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Month { get; init; }
        public int Year { get; init; }
    }

    public sealed class AvailabilityGroupMemberSqlDto
    {
        public int Id { get; init; }
        public int AvailabilityGroupId { get; init; }
        public int EmployeeId { get; init; }
    }

    public sealed class AvailabilityGroupDaySqlDto
    {
        public int Id { get; init; }
        public int AvailabilityGroupMemberId { get; init; }
        public int DayOfMonth { get; init; }
        public string Kind { get; init; } = string.Empty;
        public string? IntervalStr { get; init; }
    }
}
