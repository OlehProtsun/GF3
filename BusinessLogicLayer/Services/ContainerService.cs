using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public class ContainerService : IContainerService
{
    private readonly IContainerRepository _repo;
    private readonly IScheduleRepository _scheduleRepo;

    public ContainerService(IContainerRepository repo, IScheduleRepository scheduleRepo)
    {
        _repo = repo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _repo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_repo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
        => await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
        => _repo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    public async Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ScheduleModel>> GetGraphsAsync(int containerId, CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);
        return (await _scheduleRepo.GetByContainerAsync(containerId, null, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);
        model.ContainerId = containerId;
        var created = await _scheduleRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);
        var existing = await _scheduleRepo.GetByIdAsync(graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        if (existing.ContainerId != containerId)
            throw new KeyNotFoundException($"Graph with id {graphId} was not found in container {containerId}.");

        model.Id = graphId;
        model.ContainerId = containerId;
        await _scheduleRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);
        var existing = await _scheduleRepo.GetByIdAsync(graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        if (existing.ContainerId != containerId)
            throw new KeyNotFoundException($"Graph with id {graphId} was not found in container {containerId}.");

        await _scheduleRepo.DeleteAsync(graphId, ct).ConfigureAwait(false);
    }

    private async Task EnsureContainerExistsAsync(int containerId, CancellationToken ct)
    {
        var container = await _repo.GetByIdAsync(containerId, ct).ConfigureAwait(false);
        if (container is null)
            throw new KeyNotFoundException($"Container with id {containerId} was not found.");
    }
}
