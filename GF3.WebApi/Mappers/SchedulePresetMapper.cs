using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.SchedulePresets;

namespace WebApi.Mappers;

public static class SchedulePresetMapper
{
    public static SchedulePresetDto ToSchedulePresetDto(this SchedulePresetModel model) => new()
    {
        Id = model.Id,
        ContainerId = model.ContainerId,
        Name = model.Name,
        ScheduleName = model.ScheduleName,
        ShopId = model.ShopId,
        Year = model.Year,
        Month = model.Month,
        PeoplePerShift = model.PeoplePerShift,
        Shift1Time = model.Shift1Time,
        Shift2Time = model.Shift2Time,
        MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = model.MaxConsecutiveDays,
        MaxConsecutiveFull = model.MaxConsecutiveFull,
        MaxFullPerMonth = model.MaxFullPerMonth,
        AvailabilityGroupId = model.AvailabilityGroupId,
        Employees = model.Employees.Select(x => x.ToSchedulePresetEmployeeDto()).ToList()
    };

    public static SchedulePresetEmployeeDto ToSchedulePresetEmployeeDto(this SchedulePresetEmployeeModel model) => new()
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        MinHoursMonth = model.MinHoursMonth
    };

    public static SchedulePresetModel ToCreateModel(this CreateSchedulePresetRequest request, int containerId) => new()
    {
        ContainerId = containerId,
        Name = request.Name,
        ScheduleName = request.ScheduleName,
        ShopId = request.ShopId,
        Year = request.Year,
        Month = request.Month,
        PeoplePerShift = request.PeoplePerShift,
        Shift1Time = request.Shift1Time,
        Shift2Time = request.Shift2Time,
        MaxHoursPerEmpMonth = request.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = request.MaxConsecutiveDays,
        MaxConsecutiveFull = request.MaxConsecutiveFull,
        MaxFullPerMonth = request.MaxFullPerMonth,
        AvailabilityGroupId = request.AvailabilityGroupId,
        Employees = request.Employees
            .Select(x => x.ToCreateModel())
            .ToList()
    };

    public static SchedulePresetEmployeeModel ToCreateModel(this CreateSchedulePresetEmployeeRequest request) => new()
    {
        EmployeeId = request.EmployeeId,
        MinHoursMonth = request.MinHoursMonth
    };
}
