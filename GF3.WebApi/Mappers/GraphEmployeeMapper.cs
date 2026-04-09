using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.Employees;

namespace WebApi.Mappers;

/// <summary>
/// Maps graph employee-assignment contracts between the API layer and the business layer.
/// </summary>
public static class GraphEmployeeMapper
{
    public static GraphEmployeeDto ToGraphEmployeeDto(this ScheduleEmployeeModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        EmployeeId = model.EmployeeId,
        MinHoursMonth = model.MinHoursMonth,
        DisplayOrder = model.DisplayOrder,
    };

    public static ScheduleEmployeeModel ToAddModel(this AddGraphEmployeeRequest request, int graphId)
        => MapGraphEmployeeModel(graphId, request.EmployeeId, request.MinHoursMonth, request.DisplayOrder);

    public static ScheduleEmployeeModel ToUpdateModel(this UpdateGraphEmployeeRequest request, int graphId, int graphEmployeeId)
        => MapGraphEmployeeModel(graphId, request.EmployeeId, request.MinHoursMonth, request.DisplayOrder, graphEmployeeId);

    public static ScheduleEmployeeModel ToPreviewModel(this GenerateGraphPreviewEmployeeRequest request, int graphId)
        => MapGraphEmployeeModel(graphId, request.EmployeeId, request.MinHoursMonth, request.DisplayOrder);

    private static ScheduleEmployeeModel MapGraphEmployeeModel(
        int graphId,
        int employeeId,
        int? minHoursMonth,
        int displayOrder,
        int id = 0)
        => new()
        {
            Id = id,
            ScheduleId = graphId,
            EmployeeId = employeeId,
            MinHoursMonth = minHoursMonth,
            DisplayOrder = displayOrder,
        };
}
