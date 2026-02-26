using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs.CellStyles;

namespace WebApi.Mappers;

public static class GraphCellStyleMapper
{
    public static GraphCellStyleDto ToGraphCellStyleDto(this ScheduleCellStyleModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        DayOfMonth = model.DayOfMonth,
        EmployeeId = model.EmployeeId,
        BackgroundColorArgb = model.BackgroundColorArgb,
        TextColorArgb = model.TextColorArgb
    };

    public static ScheduleCellStyleModel ToUpsertModel(this UpsertGraphCellStyleRequest request, int graphId) => new()
    {
        ScheduleId = graphId,
        DayOfMonth = request.DayOfMonth,
        EmployeeId = request.EmployeeId,
        BackgroundColorArgb = request.BackgroundColorArgb,
        TextColorArgb = request.TextColorArgb
    };
}
