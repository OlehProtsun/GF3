using BusinessLogicLayer.Contracts.Enums;

namespace BusinessLogicLayer.Contracts.Availability;

public sealed class AvailabilityTransferSourceDayModel
{
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
    public bool CanTransfer { get; set; }
}

public sealed class AvailabilityTransferSourceModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public int EmployeeId { get; set; }
    public List<AvailabilityTransferSourceDayModel> Days { get; set; } = [];
}

public sealed class AvailabilityTransferHintModel
{
    public int EmployeeId { get; set; }
    public int DayOfMonth { get; set; }
    public int TargetGroupId { get; set; }
    public string TargetGroupName { get; set; } = string.Empty;
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}

public sealed class AvailabilityTransferResultModel
{
    public int SourceGroupId { get; set; }
    public string SourceGroupName { get; set; } = string.Empty;
    public int TargetGroupId { get; set; }
    public string TargetGroupName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public List<int> DayOfMonths { get; set; } = [];
}
