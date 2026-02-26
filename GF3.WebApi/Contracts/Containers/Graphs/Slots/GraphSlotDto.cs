using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.Containers.Graphs.Slots;

public sealed class GraphSlotDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int DayOfMonth { get; set; }
    public int SlotNo { get; set; }
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public SlotStatus Status { get; set; }
}
