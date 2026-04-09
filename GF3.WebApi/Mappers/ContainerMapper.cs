using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers;

namespace WebApi.Mappers;

/// <summary>
/// Maps container API contracts to the business-layer container model and back.
/// </summary>
public static class ContainerMapper
{
    public static ContainerDto ToApiDto(this ContainerModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Note = model.Note,
    };

    public static ContainerModel ToCreateModel(this CreateContainerRequest request)
        => MapContainerModel(request.Name, request.Note);

    public static ContainerModel ToUpdateModel(this UpdateContainerRequest request, int id)
        => MapContainerModel(request.Name, request.Note, id);

    private static ContainerModel MapContainerModel(string name, string? note, int id = 0)
        => new()
        {
            Id = id,
            Name = name,
            Note = note,
        };
}
