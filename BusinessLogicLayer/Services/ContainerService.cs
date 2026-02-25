using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public class ContainerService : IContainerService
{
    private readonly IContainerRepository _repo;

    public ContainerService(IContainerRepository repo)
    {
        _repo = repo;
    }

    public async Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
        => (await _repo.GetByIdAsync(id, ct).ConfigureAwait(false))?.ToContract();

    public async Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
        => (await _repo.GetAllAsync(ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
        => (await _repo.AddAsync(entity.ToDal(), ct).ConfigureAwait(false)).ToContract();

    public Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
        => _repo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    public async Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => (await _repo.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
}
