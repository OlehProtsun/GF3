using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public class ScheduleEmployeeService : IScheduleEmployeeService
{
    private readonly IScheduleEmployeeRepository _repo;

    public ScheduleEmployeeService(IScheduleEmployeeRepository repo)
    {
        _repo = repo;
    }

    public async Task<ScheduleEmployeeModel?> GetAsync(int id, CancellationToken ct = default)
        => (await _repo.GetByIdAsync(id, ct).ConfigureAwait(false))?.ToContract();

    public async Task<List<ScheduleEmployeeModel>> GetAllAsync(CancellationToken ct = default)
        => (await _repo.GetAllAsync(ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task<ScheduleEmployeeModel> CreateAsync(ScheduleEmployeeModel entity, CancellationToken ct = default)
        => (await _repo.AddAsync(entity.ToDal(), ct).ConfigureAwait(false)).ToContract();

    public Task UpdateAsync(ScheduleEmployeeModel entity, CancellationToken ct = default)
        => _repo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);
}
