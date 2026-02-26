using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services;

public class ContainerService : IContainerService
{
    private readonly IContainerRepository _repo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IScheduleSlotRepository _slotRepo;
    private readonly IScheduleEmployeeRepository _employeeRepo;
    private readonly IScheduleCellStyleRepository _cellStyleRepo;

    public ContainerService(
        IContainerRepository repo,
        IScheduleRepository scheduleRepo,
        IScheduleSlotRepository slotRepo,
        IScheduleEmployeeRepository employeeRepo,
        IScheduleCellStyleRepository cellStyleRepo)
    {
        _repo = repo;
        _scheduleRepo = scheduleRepo;
        _slotRepo = slotRepo;
        _employeeRepo = employeeRepo;
        _cellStyleRepo = cellStyleRepo;
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

    public async Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        var graph = await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        return graph.ToContract();
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
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        model.Id = graphId;
        model.ContainerId = containerId;
        await _scheduleRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await _scheduleRepo.DeleteAsync(graphId, ct).ConfigureAwait(false);
    }

    public async Task<List<ScheduleSlotModel>> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        return (await _slotRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        model.ScheduleId = graphId;

        try
        {
            var created = await _slotRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
            return created.ToContract();
        }
        catch (DbUpdateException)
        {
            throw new ValidationException("Duplicate slot for the same time/day");
        }
    }

    public async Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _slotRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Slot with id {slotId} was not found.");

        if (existing.ScheduleId != graphId)
            throw new KeyNotFoundException("Slot not found in graph.");

        model.Id = slotId;
        model.ScheduleId = graphId;

        try
        {
            await _slotRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException("Duplicate slot for the same time/day");
        }
    }

    public async Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _slotRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Slot with id {slotId} was not found.");

        if (existing.ScheduleId != graphId)
            throw new KeyNotFoundException("Slot not found in graph.");

        await _slotRepo.DeleteAsync(slotId, ct).ConfigureAwait(false);
    }

    public async Task<List<ScheduleEmployeeModel>> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        return (await _employeeRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        model.ScheduleId = graphId;
        var created = await _employeeRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _employeeRepo.GetByIdAsync(graphEmployeeId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph employee with id {graphEmployeeId} was not found.");

        if (existing.ScheduleId != graphId)
            throw new KeyNotFoundException("Graph employee not found in graph.");

        model.Id = graphEmployeeId;
        model.ScheduleId = graphId;
        await _employeeRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _employeeRepo.GetByIdAsync(graphEmployeeId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph employee with id {graphEmployeeId} was not found.");

        if (existing.ScheduleId != graphId)
            throw new KeyNotFoundException("Graph employee not found in graph.");

        await _employeeRepo.DeleteAsync(graphEmployeeId, ct).ConfigureAwait(false);
    }

    public async Task<List<ScheduleCellStyleModel>> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        return (await _cellStyleRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = (await _cellStyleRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false))
            .FirstOrDefault(x => x.DayOfMonth == model.DayOfMonth && x.EmployeeId == model.EmployeeId);

        if (existing is null)
        {
            model.ScheduleId = graphId;
            var created = await _cellStyleRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
            return created.ToContract();
        }

        existing.BackgroundColorArgb = model.BackgroundColorArgb;
        existing.TextColorArgb = model.TextColorArgb;

        await _cellStyleRepo.UpdateAsync(existing, ct).ConfigureAwait(false);
        return existing.ToContract();
    }

    public async Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _cellStyleRepo.GetByIdAsync(styleId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Cell style with id {styleId} was not found.");

        if (existing.ScheduleId != graphId)
            throw new KeyNotFoundException("Cell style not found in graph.");

        await _cellStyleRepo.DeleteAsync(styleId, ct).ConfigureAwait(false);
    }

    private async Task EnsureContainerExistsAsync(int containerId, CancellationToken ct)
    {
        var container = await _repo.GetByIdAsync(containerId, ct).ConfigureAwait(false);
        if (container is null)
            throw new KeyNotFoundException($"Container with id {containerId} was not found.");
    }

    private async Task<DataAccessLayer.Models.ScheduleModel> EnsureGraphOwnershipAsync(int containerId, int graphId, CancellationToken ct)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);
        var existing = await _scheduleRepo.GetByIdAsync(graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        if (existing.ContainerId != containerId)
            throw new KeyNotFoundException("Graph not found in container");

        return existing;
    }
}
