using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs.Slots;

namespace WebApi.Mappers;

/// <summary>
/// Maps graph slot API contracts to the business-layer schedule-slot model and back.
/// </summary>
public static class GraphSlotMapper
{
    public static GraphSlotDto ToGraphSlotDto(this ScheduleSlotModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        SlotNo = model.SlotNo,
        FromTime = model.FromTime,
        ToTime = model.ToTime,
        EmployeeId = model.EmployeeId,
        Status = model.Status,
    };

    public static ScheduleSlotModel ToCreateModel(this CreateGraphSlotRequest request, int graphId)
        => MapSlotModel(graphId, request.DayOfMonth, request.SlotNo, request.FromTime, request.ToTime, request.EmployeeId, request.Status);

    public static ScheduleSlotModel ToUpdateModel(this UpdateGraphSlotRequest request, int graphId, int slotId)
        => MapSlotModel(graphId, request.DayOfMonth, request.SlotNo, request.FromTime, request.ToTime, request.EmployeeId, request.Status, slotId);

    public static List<ScheduleSlotModel> ToReplaceModels(this ReplaceGraphSlotsRequest request, int graphId)
        => request.Slots
            .Select(slot => slot.ToCreateModel(graphId))
            .ToList();

    private static ScheduleSlotModel MapSlotModel(
        int graphId,
        int dayOfMonth,
        int slotNo,
        string fromTime,
        string toTime,
        int? employeeId,
        SlotStatus status,
        int id = 0)
        => new()
        {
            Id = id,
            ScheduleId = graphId,
            DayOfMonth = dayOfMonth,
            SlotNo = slotNo,
            FromTime = fromTime,
            ToTime = toTime,
            EmployeeId = employeeId,
            Status = status,
        };
}
