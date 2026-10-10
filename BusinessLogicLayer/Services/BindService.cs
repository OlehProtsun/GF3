using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Manages availability binds (keyboard shortcut to symbol mappings).
/// The service is intentionally small, but it still centralizes normalization and uniqueness rules
/// so both CRUD and "upsert by key" flows behave identically.
/// </summary>
public class BindService : IBindService
{
    private readonly IBindRepository _bindRepo;

    public BindService(IBindRepository bindRepo)
    {
        _bindRepo = bindRepo;
    }

    public async Task<BindModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _bindRepo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<BindModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_bindRepo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<BindModel> CreateAsync(BindModel entity, CancellationToken ct = default)
    {
        await ValidateAsync(entity, excludeId: null, ct).ConfigureAwait(false);
        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _bindRepo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BindModel entity, CancellationToken ct = default)
    {
        await ValidateAsync(entity, entity.Id, ct).ConfigureAwait(false);
        await _bindRepo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _bindRepo.DeleteAsync(id, ct);

    public async Task<BindModel?> GetAsync(int id, int managerAccountId, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(
            token => _bindRepo.GetByIdAsync(id, managerAccountId, token),
            x => x.ToContract(),
            ct).ConfigureAwait(false);

    public async Task<List<BindModel>> GetAllAsync(int managerAccountId, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(
            token => _bindRepo.GetAllAsync(managerAccountId, token),
            x => x.ToContract(),
            ct).ConfigureAwait(false);

    public async Task<BindModel> CreateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default)
    {
        entity.ManagerAccountId = managerAccountId;
        await ValidateAsync(entity, excludeId: null, managerAccountId, ct).ConfigureAwait(false);
        return await ServiceMappingHelper.CreateMappedAsync(
            entity.ToDal(),
            _bindRepo.AddAsync,
            x => x.ToContract(),
            ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default)
    {
        if (await _bindRepo.GetByIdAsync(entity.Id, managerAccountId, ct).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException($"Availability bind with id {entity.Id} was not found.");
        }

        entity.ManagerAccountId = managerAccountId;
        await ValidateAsync(entity, entity.Id, managerAccountId, ct).ConfigureAwait(false);
        await _bindRepo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, int managerAccountId, CancellationToken ct = default)
        => _bindRepo.DeleteAsync(id, managerAccountId, ct);

    public async Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_bindRepo.GetActiveAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<BindModel>> GetActiveAsync(int managerAccountId, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(
            token => _bindRepo.GetActiveAsync(managerAccountId, token),
            x => x.ToContract(),
            ct).ConfigureAwait(false);

    public async Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _bindRepo.GetByKeyAsync(key, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
    {
        Normalize(model);
        ValidateRequiredFields(model);

        return await ServiceMappingHelper.ExecuteAndMapAsync(
            token => _bindRepo.UpsertByKeyAsync(model.ToDal(), token),
            x => x.ToContract(),
            ct).ConfigureAwait(false);
    }

    private async Task ValidateAsync(BindModel entity, int? excludeId, CancellationToken ct)
    {
        Normalize(entity);
        ValidateRequiredFields(entity);

        var existing = await _bindRepo.GetByKeyAsync(entity.Key, ct).ConfigureAwait(false);
        if (existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value))
        {
            throw ValidationException.ForField(nameof(BindModel.Key), $"A bind with key '{entity.Key}' already exists.");
        }
    }

    private async Task ValidateAsync(BindModel entity, int? excludeId, int managerAccountId, CancellationToken ct)
    {
        Normalize(entity);
        ValidateRequiredFields(entity);

        var existing = await _bindRepo.GetByKeyAsync(entity.Key, managerAccountId, ct).ConfigureAwait(false);
        if (existing is not null && (!excludeId.HasValue || existing.Id != excludeId.Value))
        {
            throw ValidationException.ForField(nameof(BindModel.Key), $"A bind with key '{entity.Key}' already exists.");
        }

        if (await _bindRepo.IsColorKeyInUseAsync(managerAccountId, entity.Key, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField(nameof(BindModel.Key), $"Key '{entity.Key}' is already used by a color bind.");
        }
    }

    private static void ValidateRequiredFields(BindModel entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Key))
            throw ValidationException.ForField(nameof(BindModel.Key), "Bind key is required.");
        if (string.IsNullOrWhiteSpace(entity.Value))
            throw ValidationException.ForField(nameof(BindModel.Value), "Bind value is required.");
    }

    private static void Normalize(BindModel entity)
    {
        entity.Key = (entity.Key ?? string.Empty).Trim();
        entity.Value = (entity.Value ?? string.Empty).Trim();
    }
}
