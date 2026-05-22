using System.Collections.Concurrent;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Coordinates container CRUD and every nested graph-related operation.
/// This service deliberately keeps read paths tolerant by returning <c>null</c> when the
/// parent container/graph is missing, because the API translates that into a clean 404.
/// Mutation paths are stricter: they throw explicit ownership errors so we never update
/// a graph artifact through the wrong container.
/// </summary>
public class ContainerService : IContainerService
{
    private const string DeleteBlockedMessage = "To delete this container, first delete all graphs that belong to it.";
    private const string DuplicateSlotMessage = "Duplicate slot for the same time/day";
    private const string ReplaceSlotsRejectedMessage = "Could not save schedule slots because the database rejected the slot set.";

    private readonly IContainerRepository _repo;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly ISchedulePresetRepository _schedulePresetRepo;
    private readonly IScheduleSlotRepository _slotRepo;
    private readonly IScheduleEmployeeRepository _employeeRepo;
    private readonly IScheduleCellStyleRepository _cellStyleRepo;
    private readonly IAvailabilityGroupRepository _availabilityGroupRepo;
    private readonly IScheduleGenerator _scheduleGenerator;

    /// <summary>
    /// Graph generation rewrites a whole slot set. The per-graph semaphore makes sure that two
    /// concurrent generate requests cannot interleave and leave the graph in a partial state.
    /// </summary>
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> GenerateLocks = new();

    public ContainerService(
        IContainerRepository repo,
        IScheduleRepository scheduleRepo,
        ISchedulePresetRepository schedulePresetRepo,
        IScheduleSlotRepository slotRepo,
        IScheduleEmployeeRepository employeeRepo,
        IScheduleCellStyleRepository cellStyleRepo,
        IAvailabilityGroupRepository availabilityGroupRepo,
        IScheduleGenerator scheduleGenerator)
    {
        _repo = repo;
        _scheduleRepo = scheduleRepo;
        _schedulePresetRepo = schedulePresetRepo;
        _slotRepo = slotRepo;
        _employeeRepo = employeeRepo;
        _cellStyleRepo = cellStyleRepo;
        _availabilityGroupRepo = availabilityGroupRepo;
        _scheduleGenerator = scheduleGenerator;
    }

    public async Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _repo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_repo.GetSummariesAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
    {
        await ValidateContainerAsync(entity, excludeId: null, ct).ConfigureAwait(false);
        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
    {
        await ValidateContainerAsync(entity, entity.Id, ct).ConfigureAwait(false);
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
        if (await _repo.HasScheduleReferencesAsync(id, ct).ConfigureAwait(false))
        {
            return DeleteOperationResult.Failure(DeleteBlockedMessage);
        }

        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        return DeleteOperationResult.Success();
    }

    public async Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetSummariesByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        if (await TryGetContainerAsync(containerId, readCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        return (await _scheduleRepo.GetByContainerAsync(containerId, null, readCt).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();
    }

    public async Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        var graph = await TryGetOwnedGraphAsync(containerId, graphId, readCt).ConfigureAwait(false);
        return graph?.ToContract();
    }

    public async Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        if (employeeId <= 0)
        {
            throw ValidationException.ForField(nameof(employeeId), "Employee is required.");
        }

        return (await _scheduleRepo
                .GetPublishedForEmployeeAsync(employeeId, ServiceMappingHelper.NormalizeReadCancellationToken(ct))
                .ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();
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

    public async Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        if (await TryGetContainerAsync(containerId, readCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        return (await _schedulePresetRepo.GetByContainerAsync(containerId, readCt).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();
    }

    public async Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);

        NormalizeSchedulePresetModel(model, containerId);
        await EnsureSchedulePresetNameIsUniqueAsync(containerId, model.Name, ct).ConfigureAwait(false);

        var created = await _schedulePresetRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task<GenerateGraphResult> GenerateGraphAsync(
        int containerId,
        int graphId,
        bool overwrite,
        bool dryRun,
        IProgress<int>? progress,
        CancellationToken ct = default)
    {
        return await ExecuteUnderGraphGenerationLockAsync(
            graphId,
            ct,
            async lockToken =>
            {
                var graph = await EnsureGraphOwnershipAsync(containerId, graphId, lockToken).ConfigureAwait(false);
                var schedule = graph.ToContract();
                var employees = await LoadGraphEmployeesAsync(graphId, lockToken).ConfigureAwait(false);
                var generatedSlots = await GenerateSlotsAsync(schedule, employees, progress, lockToken).ConfigureAwait(false);

                var writtenSlotsCount = dryRun
                    ? 0
                    : await ReplaceGeneratedSlotsAsync(graphId, generatedSlots, overwrite, lockToken).ConfigureAwait(false);

                return new GenerateGraphResult
                {
                    ContainerId = containerId,
                    GraphId = graphId,
                    GeneratedSlotsCount = generatedSlots.Count,
                    WrittenSlotsCount = writtenSlotsCount,
                    Slots = generatedSlots
                };
            }).ConfigureAwait(false);
    }

    public async Task<GenerateGraphResult> GenerateGraphPreviewAsync(
        int containerId,
        ScheduleModel model,
        IEnumerable<ScheduleEmployeeModel> employees,
        IProgress<int>? progress,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(employees);

        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);

        model.ContainerId = containerId;
        var employeeList = NormalizePreviewEmployees(employees);
        var generatedSlots = await GenerateSlotsAsync(model, employeeList, progress, ct).ConfigureAwait(false);

        return new GenerateGraphResult
        {
            ContainerId = containerId,
            GraphId = model.Id,
            GeneratedSlotsCount = generatedSlots.Count,
            WrittenSlotsCount = 0,
            Slots = generatedSlots
        };
    }

    public async Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        if (await TryGetOwnedGraphAsync(containerId, graphId, readCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        return await LoadGraphSlotsAsync(graphId, readCt).ConfigureAwait(false);
    }

    public async Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(slots);

        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var normalizedSlots = NormalizeReplacementSlots(slots, graphId);

        try
        {
            await _slotRepo
                .ReplaceForScheduleAsync(graphId, normalizedSlots.Select(slot => slot.ToDal()), overwrite: true, ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException(ReplaceSlotsRejectedMessage);
        }
    }

    public async Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        model.ScheduleId = graphId;
        return await CreateGraphSlotCoreAsync(model, ct).ConfigureAwait(false);
    }

    public async Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await EnsureSlotBelongsToGraphAsync(graphId, slotId, ct).ConfigureAwait(false);

        model.Id = slotId;
        model.ScheduleId = graphId;
        await UpdateGraphSlotCoreAsync(model, ct).ConfigureAwait(false);
    }

    public async Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await EnsureSlotBelongsToGraphAsync(graphId, slotId, ct).ConfigureAwait(false);
        await _slotRepo.DeleteAsync(slotId, ct).ConfigureAwait(false);
    }

    public async Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        if (await TryGetOwnedGraphAsync(containerId, graphId, readCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        return await LoadGraphEmployeesAsync(graphId, readCt).ConfigureAwait(false);
    }

    public async Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await EnsureGraphEmployeeIsUniqueAsync(graphId, model.EmployeeId, excludeGraphEmployeeId: null, ct).ConfigureAwait(false);

        model.ScheduleId = graphId;
        var created = await _employeeRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await EnsureGraphEmployeeBelongsToGraphAsync(graphId, graphEmployeeId, ct).ConfigureAwait(false);
        await EnsureGraphEmployeeIsUniqueAsync(graphId, model.EmployeeId, graphEmployeeId, ct).ConfigureAwait(false);

        model.Id = graphEmployeeId;
        model.ScheduleId = graphId;
        await _employeeRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);
        await EnsureGraphEmployeeBelongsToGraphAsync(graphId, graphEmployeeId, ct).ConfigureAwait(false);
        await _employeeRepo.DeleteAsync(graphEmployeeId, ct).ConfigureAwait(false);
    }

    public async Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        if (await TryGetOwnedGraphAsync(containerId, graphId, readCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        return await LoadGraphCellStylesAsync(graphId, readCt).ConfigureAwait(false);
    }

    public async Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)
    {
        await EnsureGraphOwnershipAsync(containerId, graphId, ct).ConfigureAwait(false);

        var existing = await _cellStyleRepo
            .GetByScheduleCellAsync(graphId, model.DayOfMonth, model.EmployeeId, ct)
            .ConfigureAwait(false);

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
        await EnsureCellStyleBelongsToGraphAsync(graphId, styleId, ct).ConfigureAwait(false);
        await _cellStyleRepo.DeleteAsync(styleId, ct).ConfigureAwait(false);
    }

    private async Task EnsureContainerExistsAsync(int containerId, CancellationToken ct)
    {
        if (await _repo.GetByIdAsync(containerId, ct).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException($"Container with id {containerId} was not found.");
        }
    }

    private async Task ValidateContainerAsync(ContainerModel entity, int? excludeId, CancellationToken ct)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();
        entity.Note = string.IsNullOrWhiteSpace(entity.Note) ? null : entity.Note.Trim();

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw ValidationException.ForField(nameof(ContainerModel.Name), "Container name is required.");
        }

        if (await _repo.ExistsByNameAsync(entity.Name, excludeId, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField(nameof(ContainerModel.Name), "A container with the same name already exists.");
        }
    }

    private static void NormalizeSchedulePresetModel(SchedulePresetModel model, int containerId)
    {
        model.ContainerId = containerId;
        model.Name = model.Name.Trim();
        model.ScheduleName = model.ScheduleName.Trim();
        model.Employees = model.Employees
            .Where(x => x.EmployeeId > 0)
            .GroupBy(x => x.EmployeeId)
            .Select(x => x.Last())
            .ToList();
    }

    private async Task EnsureSchedulePresetNameIsUniqueAsync(int containerId, string presetName, CancellationToken ct)
    {
        if (await _schedulePresetRepo.ExistsByNameAsync(containerId, presetName, null, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField(
                nameof(SchedulePresetModel.Name),
                "A preset with this name already exists in the current container.");
        }
    }

    private async Task<List<ScheduleEmployeeModel>> LoadGraphEmployeesAsync(int graphId, CancellationToken ct)
    {
        return (await _employeeRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Employee?.FirstName ?? string.Empty)
            .ThenBy(x => x.Employee?.LastName ?? string.Empty)
            .ThenBy(x => x.EmployeeId)
            .ToList();
    }

    private async Task<List<ScheduleSlotModel>> LoadGraphSlotsAsync(int graphId, CancellationToken ct)
        => (await _slotRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();

    private async Task<List<ScheduleCellStyleModel>> LoadGraphCellStylesAsync(int graphId, CancellationToken ct)
        => (await _cellStyleRepo.GetByScheduleAsync(graphId, ct).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();

    private static List<ScheduleEmployeeModel> NormalizePreviewEmployees(IEnumerable<ScheduleEmployeeModel> employees)
    {
        return employees
            .Where(x => x.EmployeeId > 0)
            .GroupBy(x => x.EmployeeId)
            .Select(group => group
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .First())
            .ToList();
    }

    private async Task<List<AvailabilityGroupModel>> LoadAvailabilityGroupsAsync(ScheduleModel schedule, CancellationToken ct)
    {
        var availabilities = new List<AvailabilityGroupModel>();

        if (schedule.AvailabilityGroupId is not int availabilityGroupId || availabilityGroupId <= 0)
        {
            return availabilities;
        }

        var group = await _availabilityGroupRepo.GetFullByIdAsync(availabilityGroupId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability group with id {availabilityGroupId} was not found.");

        availabilities.Add(group.ToContract());
        return availabilities;
    }

    private async Task<List<ScheduleSlotModel>> GenerateSlotsAsync(
        ScheduleModel schedule,
        IEnumerable<ScheduleEmployeeModel> employees,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        var availabilities = await LoadAvailabilityGroupsAsync(schedule, ct).ConfigureAwait(false);

        return (await _scheduleGenerator
            .GenerateAsync(schedule, availabilities, employees, progress, ct)
            .ConfigureAwait(false))
            .Select(slot =>
            {
                slot.ScheduleId = schedule.Id;
                slot.Id = 0;
                return slot;
            })
            .ToList();
    }

    private async Task<int> ReplaceGeneratedSlotsAsync(
        int graphId,
        IEnumerable<ScheduleSlotModel> generatedSlots,
        bool overwrite,
        CancellationToken ct)
    {
        try
        {
            return await _slotRepo
                .ReplaceForScheduleAsync(graphId, generatedSlots.Select(x => x.ToDal()), overwrite, ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException("Duplicate slot constraint violated. Try overwrite=true.");
        }
    }

    private async Task<T> ExecuteUnderGraphGenerationLockAsync<T>(
        int graphId,
        CancellationToken ct,
        Func<CancellationToken, Task<T>> action)
    {
        var semaphore = GenerateLocks.GetOrAdd(graphId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            return await action(ct).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Manual slot replacement accepts a loosely ordered payload from the editor.
    /// Before persisting we:
    /// 1. normalize schedule ownership and time strings,
    /// 2. group logically identical slots,
    /// 3. drop duplicate employees inside the same time bucket,
    /// 4. rebuild slot numbers so storage stays dense and deterministic.
    /// </summary>
    private static List<ScheduleSlotModel> NormalizeReplacementSlots(IEnumerable<ScheduleSlotModel> slots, int graphId)
    {
        var normalizedInput = slots
            .Where(slot => slot is not null)
            .Select(slot =>
            {
                var employeeId = slot.EmployeeId is > 0 ? slot.EmployeeId : null;

                return new ScheduleSlotModel
                {
                    Id = 0,
                    ScheduleId = graphId,
                    DayOfMonth = slot.DayOfMonth,
                    SlotNo = Math.Max(1, slot.SlotNo),
                    FromTime = slot.FromTime.Trim(),
                    ToTime = slot.ToTime.Trim(),
                    EmployeeId = employeeId,
                    Status = employeeId.HasValue ? SlotStatus.ASSIGNED : SlotStatus.UNFURNISHED,
                };
            })
            .ToList();

        var normalizedSlots = new List<ScheduleSlotModel>(normalizedInput.Count);

        foreach (var group in normalizedInput
            .GroupBy(slot => new { slot.DayOfMonth, slot.FromTime, slot.ToTime })
            .OrderBy(group => group.Key.DayOfMonth)
            .ThenBy(group => group.Key.FromTime, StringComparer.Ordinal)
            .ThenBy(group => group.Key.ToTime, StringComparer.Ordinal))
        {
            var seenEmployeeIds = new HashSet<int>();
            var nextSlotNo = 1;

            foreach (var slot in group
                .OrderBy(slot => slot.SlotNo)
                .ThenBy(slot => slot.EmployeeId ?? int.MaxValue))
            {
                if (slot.EmployeeId is int employeeId && employeeId > 0 && !seenEmployeeIds.Add(employeeId))
                {
                    continue;
                }

                slot.SlotNo = nextSlotNo++;
                normalizedSlots.Add(slot);
            }
        }

        return normalizedSlots;
    }

    private async Task EnsureGraphEmployeeIsUniqueAsync(int graphId, int employeeId, int? excludeGraphEmployeeId, CancellationToken ct)
    {
        if (employeeId <= 0)
        {
            throw ValidationException.ForField(nameof(ScheduleEmployeeModel.EmployeeId), "Employee is required.");
        }

        if (await _employeeRepo
                .ExistsForScheduleAsync(graphId, employeeId, excludeGraphEmployeeId, ct)
                .ConfigureAwait(false))
        {
            throw ValidationException.ForField(nameof(ScheduleEmployeeModel.EmployeeId), "This employee is already added to the graph.");
        }
    }

    private Task<DataAccessLayer.Models.ContainerModel?> TryGetContainerAsync(int containerId, CancellationToken ct)
        => _repo.GetByIdAsync(containerId, ct);

    private async Task<DataAccessLayer.Models.ScheduleModel?> TryGetOwnedGraphAsync(int containerId, int graphId, CancellationToken ct)
    {
        if (await TryGetContainerAsync(containerId, ct).ConfigureAwait(false) is null)
        {
            return null;
        }

        var existing = await _scheduleRepo.GetByIdAsync(graphId, ct).ConfigureAwait(false);
        if (existing is null || existing.ContainerId != containerId)
        {
            return null;
        }

        return existing;
    }

    private async Task<DataAccessLayer.Models.ScheduleModel> EnsureGraphOwnershipAsync(int containerId, int graphId, CancellationToken ct)
    {
        await EnsureContainerExistsAsync(containerId, ct).ConfigureAwait(false);

        var existing = await _scheduleRepo.GetByIdAsync(graphId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        if (existing.ContainerId != containerId)
        {
            throw new KeyNotFoundException("Graph not found in container");
        }

        return existing;
    }

    private async Task EnsureSlotBelongsToGraphAsync(int graphId, int slotId, CancellationToken ct)
    {
        var existing = await _slotRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Slot with id {slotId} was not found.");

        if (existing.ScheduleId != graphId)
        {
            throw new KeyNotFoundException("Slot not found in graph.");
        }
    }

    private async Task EnsureGraphEmployeeBelongsToGraphAsync(int graphId, int graphEmployeeId, CancellationToken ct)
    {
        var existing = await _employeeRepo.GetByIdAsync(graphEmployeeId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph employee with id {graphEmployeeId} was not found.");

        if (existing.ScheduleId != graphId)
        {
            throw new KeyNotFoundException("Graph employee not found in graph.");
        }
    }

    private async Task EnsureCellStyleBelongsToGraphAsync(int graphId, int styleId, CancellationToken ct)
    {
        var existing = await _cellStyleRepo.GetByIdAsync(styleId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Cell style with id {styleId} was not found.");

        if (existing.ScheduleId != graphId)
        {
            throw new KeyNotFoundException("Cell style not found in graph.");
        }
    }

    private async Task<ScheduleSlotModel> CreateGraphSlotCoreAsync(ScheduleSlotModel model, CancellationToken ct)
    {
        try
        {
            var created = await _slotRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
            return created.ToContract();
        }
        catch (DbUpdateException)
        {
            throw new ValidationException(DuplicateSlotMessage);
        }
    }

    private async Task UpdateGraphSlotCoreAsync(ScheduleSlotModel model, CancellationToken ct)
    {
        try
        {
            await _slotRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException(DuplicateSlotMessage);
        }
    }
}
