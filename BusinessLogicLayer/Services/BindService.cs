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
        => (await _bindRepo.GetByIdAsync(id, ct).ConfigureAwait(false))?.ToContract();

    public async Task<List<BindModel>> GetAllAsync(CancellationToken ct = default)
        => (await _bindRepo.GetAllAsync(ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task<BindModel> CreateAsync(BindModel entity, CancellationToken ct = default)
        => (await _bindRepo.AddAsync(entity.ToDal(), ct).ConfigureAwait(false)).ToContract();

    public Task UpdateAsync(BindModel entity, CancellationToken ct = default)
        => _bindRepo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _bindRepo.DeleteAsync(id, ct);

    public async Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
        => (await _bindRepo.GetActiveAsync(ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
        => (await _bindRepo.GetByKeyAsync(key, ct).ConfigureAwait(false))?.ToContract();

    public async Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
        => (await _bindRepo.UpsertByKeyAsync(model.ToDal(), ct).ConfigureAwait(false)).ToContract();
}
