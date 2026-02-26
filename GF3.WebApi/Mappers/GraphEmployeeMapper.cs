using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs.Employees;

namespace WebApi.Mappers;

public static class GraphEmployeeMapper
{
    public static GraphEmployeeDto ToGraphEmployeeDto(this ScheduleEmployeeModel model) => new()
    {
        Id = model.Id,
        ScheduleId = model.ScheduleId,
        EmployeeId = model.EmployeeId,
        MinHoursMonth = model.MinHoursMonth
    };

    public static ScheduleEmployeeModel ToAddModel(this AddGraphEmployeeRequest request, int graphId) => new()
    {
        ScheduleId = graphId,
        EmployeeId = request.EmployeeId,
        MinHoursMonth = request.MinHoursMonth
    };

    public static ScheduleEmployeeModel ToUpdateModel(this UpdateGraphEmployeeRequest request, int graphId, int graphEmployeeId) => new()
    {
        Id = graphEmployeeId,
        ScheduleId = graphId,
        EmployeeId = request.EmployeeId,
        MinHoursMonth = request.MinHoursMonth
    };
}
