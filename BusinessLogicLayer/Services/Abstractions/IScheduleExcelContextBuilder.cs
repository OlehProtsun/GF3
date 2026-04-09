using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Export;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Builds template-friendly read models for Excel exports.
/// </summary>
public interface IScheduleExcelContextBuilder
{
    /// <summary>
    /// Builds the export context for one schedule graph.
    /// </summary>
    ScheduleExcelContext BuildScheduleContext(
        ScheduleModel graph,
        ShopModel? shop,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<ScheduleSlotModel> slots);

    /// <summary>
    /// Builds the export context for a whole container.
    /// </summary>
    ContainerExcelContext BuildContainerContext(ContainerModel container, IReadOnlyList<GraphExcelContext> graphs);
}
