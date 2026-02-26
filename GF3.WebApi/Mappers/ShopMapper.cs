using BusinessLogicLayer.Contracts.Shops;
using WebApi.Contracts.Shops;

namespace WebApi.Mappers;

public static class ShopMapper
{
    public static ShopDto ToApiDto(this BusinessLogicLayer.Contracts.Shops.ShopDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Address = dto.Address,
        Description = dto.Description
    };

    public static SaveShopRequest ToSaveRequest(this CreateShopRequest request) => new()
    {
        Name = request.Name,
        Address = request.Address,
        Description = request.Description
    };

    public static SaveShopRequest ToSaveRequest(this UpdateShopRequest request, int id) => new()
    {
        Id = id,
        Name = request.Name,
        Address = request.Address,
        Description = request.Description
    };
}
