using BusinessLogicLayer.Contracts.Shops;
using WebApi.Contracts.Shops;

namespace WebApi.Mappers;

/// <summary>
/// Maps shop DTOs and requests between the API layer and the business layer.
/// </summary>
public static class ShopMapper
{
    public static WebApi.Contracts.Shops.ShopDto ToApiDto(this BusinessLogicLayer.Contracts.Shops.ShopDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Address = dto.Address,
        Description = dto.Description,
    };

    public static SaveShopRequest ToSaveRequest(this CreateShopRequest request)
        => MapSaveRequest(request.Name, request.Address, request.Description);

    public static SaveShopRequest ToSaveRequest(this UpdateShopRequest request, int id)
        => MapSaveRequest(request.Name, request.Address, request.Description, id);

    private static SaveShopRequest MapSaveRequest(string name, string address, string? description, int id = 0)
        => new()
        {
            Id = id,
            Name = name,
            Address = address,
            Description = description,
        };
}
