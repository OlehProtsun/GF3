using BusinessLogicLayer.Contracts.Export;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services.Export;

/// <summary>
/// Builds a stable export snapshot from the schedule aggregate and its related collections.
/// Child collections are ordered before serialization so exported SQL stays deterministic, which makes
/// debugging, diff review, and re-import behavior much easier to reason about.
/// </summary>
public sealed class ScheduleExportDataBuilder : IScheduleExportDataBuilder
{
    public ScheduleSqlExportData BuildSqlData(
        ScheduleModel schedule,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<ScheduleCellStyleModel> cellStyles,
        AvailabilityGroupModel? availabilityGroup,
        IReadOnlyList<AvailabilityGroupMemberModel>? availabilityMembers,
        IReadOnlyList<AvailabilityGroupDayModel>? availabilityDays)
    {
        return new ScheduleSqlExportData
        {
            Schedule = Map(schedule),
            Employees = GetOrderedEmployees(employees).Select(Map).ToList(),
            Slots = GetOrderedSlots(slots).Select(Map).ToList(),
            CellStyles = GetCellStyles(cellStyles).Select(Map).ToList(),
            AvailabilityGroup = availabilityGroup is null ? null : Map(availabilityGroup),
            AvailabilityMembers = GetOrderedAvailabilityMembers(availabilityMembers).Select(Map).ToList(),
            AvailabilityDays = GetOrderedAvailabilityDays(availabilityDays).Select(Map).ToList(),
        };
    }

    private static IEnumerable<ScheduleEmployeeModel> GetOrderedEmployees(IReadOnlyList<ScheduleEmployeeModel>? employees) =>
        (employees ?? Array.Empty<ScheduleEmployeeModel>())
            .OrderBy(employee => employee.DisplayOrder)
            .ThenBy(employee => employee.EmployeeId);

    private static IEnumerable<ScheduleSlotModel> GetOrderedSlots(IReadOnlyList<ScheduleSlotModel>? slots) =>
        (slots ?? Array.Empty<ScheduleSlotModel>())
            .OrderBy(slot => slot.DayOfMonth)
            .ThenBy(slot => slot.FromTime);

    private static IEnumerable<ScheduleCellStyleModel> GetCellStyles(IReadOnlyList<ScheduleCellStyleModel>? cellStyles) =>
        cellStyles ?? Array.Empty<ScheduleCellStyleModel>();

    private static IEnumerable<AvailabilityGroupMemberModel> GetOrderedAvailabilityMembers(IReadOnlyList<AvailabilityGroupMemberModel>? members) =>
        (members ?? Array.Empty<AvailabilityGroupMemberModel>())
            .OrderBy(member => member.DisplayOrder)
            .ThenBy(member => member.EmployeeId);

    private static IEnumerable<AvailabilityGroupDayModel> GetOrderedAvailabilityDays(IReadOnlyList<AvailabilityGroupDayModel>? days) =>
        (days ?? Array.Empty<AvailabilityGroupDayModel>())
            .OrderBy(day => day.DayOfMonth);

    private static ScheduleSqlDto Map(ScheduleModel source) => new()
    {
        Id = source.Id,
        ContainerId = source.ContainerId,
        ShopId = source.ShopId,
        AvailabilityGroupId = source.AvailabilityGroupId,
        Name = source.Name,
        Month = source.Month,
        Year = source.Year,
        PeoplePerShift = source.PeoplePerShift,
        Shift1 = source.Shift1Time,
        Shift2 = source.Shift2Time,
        MaxHoursPerEmployee = source.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = source.MaxConsecutiveDays,
        MaxConsecutiveFullShifts = source.MaxConsecutiveFull,
        MaxFullShiftsPerMonth = source.MaxFullPerMonth,
        Note = source.Note,
    };

    private static ScheduleEmployeeSqlDto Map(ScheduleEmployeeModel source) => new()
    {
        Id = source.Id,
        ScheduleId = source.ScheduleId,
        EmployeeId = source.EmployeeId,
        MinHoursMonth = source.MinHoursMonth ?? 0,
        DisplayOrder = source.DisplayOrder,
    };

    private static ScheduleSlotSqlDto Map(ScheduleSlotModel source) => new()
    {
        Id = source.Id,
        ScheduleId = source.ScheduleId,
        DayOfMonth = source.DayOfMonth,
        SlotNo = source.SlotNo,
        EmployeeId = source.EmployeeId,
        Status = source.Status.ToString(),
        FromTime = source.FromTime,
        ToTime = source.ToTime,
    };

    private static ScheduleCellStyleSqlDto Map(ScheduleCellStyleModel source) => new()
    {
        Id = source.Id,
        ScheduleId = source.ScheduleId,
        EmployeeId = source.EmployeeId,
        DayOfMonth = source.DayOfMonth,
        BackgroundArgb = source.BackgroundColorArgb,
        ForegroundArgb = source.TextColorArgb,
    };

    private static AvailabilityGroupSqlDto Map(AvailabilityGroupModel source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Month = source.Month,
        Year = source.Year,
    };

    private static AvailabilityGroupMemberSqlDto Map(AvailabilityGroupMemberModel source) => new()
    {
        Id = source.Id,
        AvailabilityGroupId = source.AvailabilityGroupId,
        EmployeeId = source.EmployeeId,
        DisplayOrder = source.DisplayOrder,
    };

    private static AvailabilityGroupDaySqlDto Map(AvailabilityGroupDayModel source) => new()
    {
        Id = source.Id,
        AvailabilityGroupMemberId = source.AvailabilityGroupMemberId,
        DayOfMonth = source.DayOfMonth,
        Kind = source.Kind.ToString(),
        IntervalStr = source.IntervalStr,
    };
}
