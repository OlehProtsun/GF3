using DataAccessLayer.Models;

namespace WebApi.Services;

internal static class ScheduleEmployeeEnrollment
{
    internal static void Ensure(ScheduleModel schedule, int employeeId)
    {
        if (schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
        {
            return;
        }

        var nextDisplayOrder = schedule.Employees.Count == 0
            ? 0
            : schedule.Employees.Max(employee => employee.DisplayOrder) + 1;
        schedule.Employees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employeeId,
            DisplayOrder = nextDisplayOrder,
        });
    }
}
