using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs;

namespace WebApi.Mappers;

public static class ContainerGraphMapper
{
    public static ContainerGraphDto ToGraphDto(this ScheduleModel model) => new()
    {
        Id = model.Id,
        ContainerId = model.ContainerId,
        ShopId = model.ShopId,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
        PeoplePerShift = model.PeoplePerShift,
        Shift1Time = model.Shift1Time,
        Shift2Time = model.Shift2Time,
        MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = model.MaxConsecutiveDays,
        MaxConsecutiveFull = model.MaxConsecutiveFull,
        MaxFullPerMonth = model.MaxFullPerMonth,
        Note = model.Note,
        AvailabilityGroupId = model.AvailabilityGroupId
    };

    public static ScheduleModel ToCreateGraphModel(this CreateContainerGraphRequest request, int containerId) => new()
    {
        ContainerId = containerId,
        ShopId = request.ShopId,
        Name = request.Name,
        Year = request.Year,
        Month = request.Month,
        PeoplePerShift = request.PeoplePerShift,
        Shift1Time = request.Shift1Time,
        Shift2Time = request.Shift2Time,
        MaxHoursPerEmpMonth = request.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = request.MaxConsecutiveDays,
        MaxConsecutiveFull = request.MaxConsecutiveFull,
        MaxFullPerMonth = request.MaxFullPerMonth,
        Note = request.Note,
        AvailabilityGroupId = request.AvailabilityGroupId
    };

    public static ScheduleModel ToUpdateGraphModel(this UpdateContainerGraphRequest request, int containerId, int graphId) => new()
    {
        Id = graphId,
        ContainerId = containerId,
        ShopId = request.ShopId,
        Name = request.Name,
        Year = request.Year,
        Month = request.Month,
        PeoplePerShift = request.PeoplePerShift,
        Shift1Time = request.Shift1Time,
        Shift2Time = request.Shift2Time,
        MaxHoursPerEmpMonth = request.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = request.MaxConsecutiveDays,
        MaxConsecutiveFull = request.MaxConsecutiveFull,
        MaxFullPerMonth = request.MaxFullPerMonth,
        Note = request.Note,
        AvailabilityGroupId = request.AvailabilityGroupId
    };
}
