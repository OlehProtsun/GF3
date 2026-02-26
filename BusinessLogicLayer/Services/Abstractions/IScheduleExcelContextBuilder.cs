using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Export;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IScheduleExcelContextBuilder
{
    ScheduleExcelContext BuildScheduleContext(ScheduleModel graph, ShopModel? shop, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots);
    ContainerExcelContext BuildContainerContext(ContainerModel container, IReadOnlyList<GraphExcelContext> graphs);
}
