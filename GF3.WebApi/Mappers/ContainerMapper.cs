using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers;

namespace WebApi.Mappers;

public static class ContainerMapper
{
    public static ContainerDto ToApiDto(this ContainerModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Note = model.Note
    };

    public static ContainerModel ToCreateModel(this CreateContainerRequest request) => new()
    {
        Name = request.Name,
        Note = request.Note
    };

    public static ContainerModel ToUpdateModel(this UpdateContainerRequest request, int id) => new()
    {
        Id = id,
        Name = request.Name,
        Note = request.Note
    };
}
