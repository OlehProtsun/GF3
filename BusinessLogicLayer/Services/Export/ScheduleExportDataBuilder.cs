using BusinessLogicLayer.Contracts.Export;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Export
{
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
                Schedule = schedule,
                Employees = (employees ?? Array.Empty<ScheduleEmployeeModel>()).OrderBy(e => e.EmployeeId).ToList(),
                Slots = (slots ?? Array.Empty<ScheduleSlotModel>()).OrderBy(s => s.DayOfWeek).ThenBy(s => s.FromTime).ToList(),
                CellStyles = (cellStyles ?? Array.Empty<ScheduleCellStyleModel>()).ToList(),
                AvailabilityGroup = availabilityGroup,
                AvailabilityMembers = (availabilityMembers ?? Array.Empty<AvailabilityGroupMemberModel>()).OrderBy(m => m.EmployeeId).ToList(),
                AvailabilityDays = (availabilityDays ?? Array.Empty<AvailabilityGroupDayModel>()).OrderBy(d => d.DayOfWeek).ToList()
            };
        }
    }
}
