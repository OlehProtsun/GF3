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
                Schedule = Map(schedule),
                Employees = (employees ?? Array.Empty<ScheduleEmployeeModel>())
                    .OrderBy(e => e.EmployeeId)
                    .Select(Map)
                    .ToList(),
                Slots = (slots ?? Array.Empty<ScheduleSlotModel>())
                    .OrderBy(s => s.DayOfMonth)
                    .ThenBy(s => s.FromTime)
                    .Select(Map)
                    .ToList(),
                CellStyles = (cellStyles ?? Array.Empty<ScheduleCellStyleModel>())
                    .Select(Map)
                    .ToList(),
                AvailabilityGroup = availabilityGroup is null ? null : Map(availabilityGroup),
                AvailabilityMembers = (availabilityMembers ?? Array.Empty<AvailabilityGroupMemberModel>())
                    .OrderBy(m => m.EmployeeId)
                    .Select(Map)
                    .ToList(),
                AvailabilityDays = (availabilityDays ?? Array.Empty<AvailabilityGroupDayModel>())
                    .OrderBy(d => d.DayOfMonth)
                    .Select(Map)
                    .ToList()
            };
        }

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
            Note = source.Note
        };

        private static ScheduleEmployeeSqlDto Map(ScheduleEmployeeModel source) => new()
        {
            Id = source.Id,
            ScheduleId = source.ScheduleId,
            EmployeeId = source.EmployeeId,
            MinHoursMonth = (int)source.MinHoursMonth
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
            ToTime = source.ToTime
        };

        private static ScheduleCellStyleSqlDto Map(ScheduleCellStyleModel source) => new()
        {
            Id = source.Id,
            ScheduleId = source.ScheduleId,
            EmployeeId = source.EmployeeId,
            DayOfMonth = source.DayOfMonth,
            BackgroundArgb = source.BackgroundColorArgb,
            ForegroundArgb = source.TextColorArgb
        };

        private static AvailabilityGroupSqlDto Map(AvailabilityGroupModel source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            Month = source.Month,
            Year = source.Year
        };

        private static AvailabilityGroupMemberSqlDto Map(AvailabilityGroupMemberModel source) => new()
        {
            Id = source.Id,
            AvailabilityGroupId = source.AvailabilityGroupId,
            EmployeeId = source.EmployeeId
        };

        private static AvailabilityGroupDaySqlDto Map(AvailabilityGroupDayModel source) => new()
        {
            Id = source.Id,
            AvailabilityGroupMemberId = source.AvailabilityGroupMemberId,
            DayOfMonth = source.DayOfMonth,
            Kind = source.Kind.ToString(),
            IntervalStr = source.IntervalStr
        };
    }
}
