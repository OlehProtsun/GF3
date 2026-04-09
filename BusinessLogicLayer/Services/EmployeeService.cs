using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Encapsulates employee-specific validation and safe delete rules.
/// The service keeps CRUD straightforward, but still centralizes the business
/// constraints that the UI and API should not have to duplicate.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private const string DeleteBlockedMessage = "To delete this employee, first delete all Availability and Schedule entries where this employee is used.";
    private readonly IEmployeeRepository _repo;

    public EmployeeService(IEmployeeRepository repo)
    {
        _repo = repo;
    }

    public async Task<EmployeeModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _repo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<EmployeeModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_repo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<EmployeeModel> CreateAsync(EmployeeModel entity, CancellationToken ct = default)
    {
        NormalizeNameFields(entity);
        await ValidateEmployeeAsync(entity, excludeId: null, ct).ConfigureAwait(false);

        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
    {
        NormalizeNameFields(entity);
        await ValidateEmployeeAsync(entity, entity.Id, ct).ConfigureAwait(false);

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
        var hasAvailabilityTask = _repo.HasAvailabilityReferencesAsync(id, ct);
        var hasScheduleTask = _repo.HasScheduleReferencesAsync(id, ct);

        await Task.WhenAll(hasAvailabilityTask, hasScheduleTask).ConfigureAwait(false);

        if (hasAvailabilityTask.Result || hasScheduleTask.Result)
        {
            return DeleteOperationResult.Failure(DeleteBlockedMessage);
        }

        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        return DeleteOperationResult.Success();
    }

    public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    private static void NormalizeNameFields(EmployeeModel entity)
    {
        entity.FirstName = (entity.FirstName ?? string.Empty).Trim();
        entity.LastName = (entity.LastName ?? string.Empty).Trim();
    }

    /// <summary>
    /// The uniqueness rule is intentionally based on a human-readable full name.
    /// That keeps duplicate employees from being created even when the rest of the
    /// profile data differs or is still incomplete.
    /// </summary>
    private async Task ValidateEmployeeAsync(EmployeeModel entity, int? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(entity.LastName))
            throw new ValidationException("Last name is required.");
        if (entity.FirstName.Length > 100 || entity.LastName.Length > 100)
            throw new ValidationException("First name or last name is too long.");

        if (await _repo.ExistsByNameAsync(entity.FirstName, entity.LastName, excludeId, ct).ConfigureAwait(false))
            throw new ValidationException("An employee with the same first and last name already exists.");
    }
}
