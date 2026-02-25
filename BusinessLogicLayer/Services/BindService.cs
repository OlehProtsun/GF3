using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

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
        => await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _bindRepo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public Task UpdateAsync(BindModel entity, CancellationToken ct = default)
        => _bindRepo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _bindRepo.DeleteAsync(id, ct);

    public async Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_bindRepo.GetActiveAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _bindRepo.GetByKeyAsync(key, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
        => await ServiceMappingHelper.ExecuteAndMapAsync(token => _bindRepo.UpsertByKeyAsync(model.ToDal(), token), x => x.ToContract(), ct).ConfigureAwait(false);
}
