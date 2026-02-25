using BusinessLogicLayer.Contracts.Export;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IScheduleExportDataBuilder
    {
        ScheduleSqlExportData BuildSqlData(
            ScheduleModel schedule,
            IReadOnlyList<ScheduleEmployeeModel> employees,
            IReadOnlyList<ScheduleSlotModel> slots,
            IReadOnlyList<ScheduleCellStyleModel> cellStyles,
            AvailabilityGroupModel? availabilityGroup,
            IReadOnlyList<AvailabilityGroupMemberModel>? availabilityMembers,
            IReadOnlyList<AvailabilityGroupDayModel>? availabilityDays);
    }
}
