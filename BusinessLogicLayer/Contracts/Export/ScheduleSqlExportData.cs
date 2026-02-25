using DataAccessLayer.Models;

namespace BusinessLogicLayer.Contracts.Export
{
    public sealed class ScheduleSqlExportData
    {
        public ScheduleModel Schedule { get; init; } = null!;
        public IReadOnlyList<ScheduleEmployeeModel> Employees { get; init; } = Array.Empty<ScheduleEmployeeModel>();
        public IReadOnlyList<ScheduleSlotModel> Slots { get; init; } = Array.Empty<ScheduleSlotModel>();
        public IReadOnlyList<ScheduleCellStyleModel> CellStyles { get; init; } = Array.Empty<ScheduleCellStyleModel>();
        public AvailabilityGroupModel? AvailabilityGroup { get; init; }
        public IReadOnlyList<AvailabilityGroupMemberModel> AvailabilityMembers { get; init; } = Array.Empty<AvailabilityGroupMemberModel>();
        public IReadOnlyList<AvailabilityGroupDayModel> AvailabilityDays { get; init; } = Array.Empty<AvailabilityGroupDayModel>();
    }
}
