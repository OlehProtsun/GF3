using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityBinds;

namespace WebApi.Mappers;

/// <summary>
/// Maps bind API contracts to the business-layer bind model and back.
/// </summary>
public static class AvailabilityBindMapper
{
    public static AvailabilityBindDto ToApiDto(this BindModel model) => new()
    {
        Id = model.Id,
        Key = model.Key,
        Value = model.Value,
        IsActive = model.IsActive,
    };

    public static BindModel ToCreateModel(this CreateAvailabilityBindRequest request)
        => MapBindModel(request.Key, request.Value, request.IsActive);

    public static BindModel ToUpdateModel(this UpdateAvailabilityBindRequest request, int id)
        => MapBindModel(request.Key, request.Value, request.IsActive, id);

    private static BindModel MapBindModel(string key, string value, bool isActive, int id = 0)
        => new()
        {
            Id = id,
            Key = key,
            Value = value,
            IsActive = isActive,
        };
}
