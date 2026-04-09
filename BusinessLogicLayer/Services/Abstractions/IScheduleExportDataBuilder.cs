using BusinessLogicLayer.Contracts.Export;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Builds the normalized SQL-export payload for one schedule graph.
/// </summary>
public interface IScheduleExportDataBuilder
{
    /// <summary>
    /// Produces a full export payload that combines the schedule graph with optional availability data.
    /// </summary>
    ScheduleSqlExportData BuildSqlData(
        ScheduleModel schedule,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<ScheduleCellStyleModel> cellStyles,
        AvailabilityGroupModel? availabilityGroup,
        IReadOnlyList<AvailabilityGroupMemberModel>? availabilityMembers,
        IReadOnlyList<AvailabilityGroupDayModel>? availabilityDays);
}
