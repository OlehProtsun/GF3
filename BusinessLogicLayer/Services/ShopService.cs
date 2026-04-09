using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Centralizes shop validation and delete guards so controllers can stay thin and
/// every consumer gets the same business behavior.
/// </summary>
public class ShopService : IShopService
{
    private const string DeleteBlockedMessage = "To delete this shop, first delete all schedules and schedule presets where this shop is used.";
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
        Normalize(entity);
        await ValidateShopAsync(entity, excludeId: null, ct).ConfigureAwait(false);

        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ShopModel entity, CancellationToken ct = default)
    {
        Normalize(entity);
        await ValidateShopAsync(entity, excludeId: entity.Id, ct).ConfigureAwait(false);

        await _repo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var result = await TryDeleteAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Message ?? DeleteBlockedMessage, result.Errors);
        }
    }

    public async Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
    {
        var hasScheduleTask = _repo.HasScheduleReferencesAsync(id, ct);
        var hasPresetTask = _repo.HasSchedulePresetReferencesAsync(id, ct);

        await Task.WhenAll(hasScheduleTask, hasPresetTask).ConfigureAwait(false);

        if (hasScheduleTask.Result || hasPresetTask.Result)
        {
            return DeleteOperationResult.Failure(DeleteBlockedMessage);
        }

        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        return DeleteOperationResult.Success();
    }

    public async Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    private static void Normalize(ShopModel entity)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Address = (entity.Address ?? string.Empty).Trim();
        entity.Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim();
    }

    /// <summary>
    /// The business key for a shop is its display name. Address is mandatory for usability,
    /// but uniqueness is intentionally enforced only on the name because that is how the rest
    /// of the product references and searches shops.
    /// </summary>
    private async Task ValidateShopAsync(ShopModel entity, int? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ValidationException("Shop name is required.");
        if (string.IsNullOrWhiteSpace(entity.Address))
            throw new ValidationException("Shop address is required.");
        if (entity.Name.Length > 200 || entity.Address.Length > 200)
            throw new ValidationException("Shop name or address is too long.");

        if (await _repo.ExistsByNameAsync(entity.Name, excludeId, ct).ConfigureAwait(false))
            throw new ValidationException("A shop with the same name already exists.");
    }
}
