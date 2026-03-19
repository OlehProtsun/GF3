using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public class ScheduleSlotService : IScheduleSlotService
{
    private readonly IScheduleSlotRepository _repo;

    public ScheduleSlotService(IScheduleSlotRepository repo)
    {
        _repo = repo;
    }

    public async Task<ScheduleSlotModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _repo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ScheduleSlotModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_repo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<ScheduleSlotModel> CreateAsync(ScheduleSlotModel entity, CancellationToken ct = default)
        => await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public Task UpdateAsync(ScheduleSlotModel entity, CancellationToken ct = default)
        => _repo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);
}
