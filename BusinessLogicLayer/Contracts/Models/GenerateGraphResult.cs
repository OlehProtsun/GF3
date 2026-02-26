namespace BusinessLogicLayer.Contracts.Models;

public sealed class GenerateGraphResult
{
    public int ContainerId { get; set; }
    public int GraphId { get; set; }
    public int GeneratedSlotsCount { get; set; }
    public int WrittenSlotsCount { get; set; }
    public IList<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();
}
