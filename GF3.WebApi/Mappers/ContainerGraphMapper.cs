using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs;

namespace WebApi.Mappers;

/// <summary>
/// Thin adapter around <see cref="GraphMapper"/> for container-scoped graph endpoints.
/// The extra methods keep controller code intention-revealing without duplicating mapping logic.
/// </summary>
public static class ContainerGraphMapper
{
    public static GraphDto ToContainerGraphDto(this ScheduleModel model) => model.ToGraphDto();

    public static ScheduleModel ToContainerCreateGraphModel(this CreateGraphRequest request, int containerId)
        => request.ToCreateModel(containerId);

    public static ScheduleModel ToContainerUpdateGraphModel(this UpdateGraphRequest request, int containerId, int graphId)
        => request.ToUpdateModel(containerId, graphId);
}
