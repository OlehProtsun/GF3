using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups.Transfers;

public sealed class AvailabilityTransferSourceDayDto
{
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
    public bool CanTransfer { get; set; }
}

public sealed class AvailabilityTransferSourceDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public int EmployeeId { get; set; }
    public List<AvailabilityTransferSourceDayDto> Days { get; set; } = [];
}

public sealed class AvailabilityTransferHintDto
{
    public int EmployeeId { get; set; }
    public int DayOfMonth { get; set; }
    public int TargetGroupId { get; set; }
    public string TargetGroupName { get; set; } = string.Empty;
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}

public sealed class TransferAvailabilityDaysRequest
{
    public int SourceGroupId { get; set; }
    public List<int> DayOfMonths { get; set; } = [];
}

public sealed class AvailabilityTransferResultDto
{
    public int SourceGroupId { get; set; }
    public string SourceGroupName { get; set; } = string.Empty;
    public int TargetGroupId { get; set; }
    public string TargetGroupName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public List<int> DayOfMonths { get; set; } = [];
}
