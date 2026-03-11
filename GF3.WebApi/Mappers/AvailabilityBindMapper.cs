using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityBinds;

namespace WebApi.Mappers;

public static class AvailabilityBindMapper
{
    public static AvailabilityBindDto ToApiDto(this BindModel model) => new()
    {
        Id = model.Id,
        Key = model.Key,
        Value = model.Value,
        IsActive = model.IsActive,
    };

    public static BindModel ToCreateModel(this CreateAvailabilityBindRequest request) => new()
    {
        Key = request.Key,
        Value = request.Value,
        IsActive = request.IsActive,
    };

    public static BindModel ToUpdateModel(this UpdateAvailabilityBindRequest request, int id) => new()
    {
        Id = id,
        Key = request.Key,
        Value = request.Value,
        IsActive = request.IsActive,
    };
}
