using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogicLayer.Services;

public class ShopService : IShopService
{
    private readonly IShopRepository _repo;

    public ShopService(IShopRepository repo)
    {
        _repo = repo;
    }

    public async Task<ShopModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _repo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ShopModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_repo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Address = (entity.Address ?? string.Empty).Trim();
        entity.Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim();

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ValidationException("Назва є обов'язковою.");
        if (string.IsNullOrWhiteSpace(entity.Address))
            throw new ValidationException("Адреса є обов'язковою.");
        if (entity.Name.Length > 200 || entity.Address.Length > 200)
            throw new ValidationException("Назва/адреса занадто довгі.");

        if (await _repo.ExistsByNameAsync(entity.Name, excludeId: null, ct).ConfigureAwait(false))
            throw new ValidationException("A shop with the same name already exists.");

        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ShopModel entity, CancellationToken ct = default)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Address = (entity.Address ?? string.Empty).Trim();
        entity.Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim();

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ValidationException("Назва є обов'язковою.");
        if (string.IsNullOrWhiteSpace(entity.Address))
            throw new ValidationException("Адреса є обов'язковою.");
        if (entity.Name.Length > 200 || entity.Address.Length > 200)
            throw new ValidationException("Назва/адреса занадто довгі.");

        if (await _repo.ExistsByNameAsync(entity.Name, excludeId: entity.Id, ct).ConfigureAwait(false))
            throw new ValidationException("A shop with the same name already exists.");

        await _repo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => _repo.DeleteAsync(id, ct);

    public async Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);
}
