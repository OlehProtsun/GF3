using BusinessLogicLayer.Contracts.Availability;
using WebApi.Contracts.AvailabilityGroups.Transfers;

namespace WebApi.Mappers;

public static class AvailabilityGroupTransferMapper
{
    public static AvailabilityTransferSourceDto ToApiDto(this AvailabilityTransferSourceModel model) => new()
    {
        GroupId = model.GroupId,
        GroupName = model.GroupName,
        MemberId = model.MemberId,
        EmployeeId = model.EmployeeId,
        Days = model.Days.Select(day => new AvailabilityTransferSourceDayDto
        {
            DayOfMonth = day.DayOfMonth,
            Kind = day.Kind,
            IntervalStr = day.IntervalStr,
            CanTransfer = day.CanTransfer,
        }).ToList(),
    };

    public static AvailabilityTransferHintDto ToApiDto(this AvailabilityTransferHintModel model) => new()
    {
        EmployeeId = model.EmployeeId,
        DayOfMonth = model.DayOfMonth,
        TargetGroupId = model.TargetGroupId,
        TargetGroupName = model.TargetGroupName,
        Kind = model.Kind,
        IntervalStr = model.IntervalStr,
    };

    public static AvailabilityTransferResultDto ToApiDto(this AvailabilityTransferResultModel model) => new()
    {
        SourceGroupId = model.SourceGroupId,
        SourceGroupName = model.SourceGroupName,
        TargetGroupId = model.TargetGroupId,
        TargetGroupName = model.TargetGroupName,
        EmployeeId = model.EmployeeId,
        DayOfMonths = model.DayOfMonths,
    };
}
