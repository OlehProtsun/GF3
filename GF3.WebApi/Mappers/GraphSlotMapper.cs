using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs.Slots;

namespace WebApi.Mappers;

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
        Status = model.Status
    };

    public static ScheduleSlotModel ToCreateModel(this CreateGraphSlotRequest request, int graphId) => new()
    {
        ScheduleId = graphId,
        DayOfMonth = request.DayOfMonth,
        SlotNo = request.SlotNo,
        FromTime = request.FromTime,
        ToTime = request.ToTime,
        EmployeeId = request.EmployeeId,
        Status = request.Status
    };

    public static ScheduleSlotModel ToUpdateModel(this UpdateGraphSlotRequest request, int graphId, int slotId) => new()
    {
        Id = slotId,
        ScheduleId = graphId,
        DayOfMonth = request.DayOfMonth,
        SlotNo = request.SlotNo,
        FromTime = request.FromTime,
        ToTime = request.ToTime,
        EmployeeId = request.EmployeeId,
        Status = request.Status
    };
}
