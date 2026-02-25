using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogicLayer.Services;

public class EmployeeService : IEmployeeService
{
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
        entity.FirstName = (entity.FirstName ?? "").Trim();
        entity.LastName = (entity.LastName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(entity.FirstName))
            throw new ValidationException("Ім'я є обов'язковим.");
        if (string.IsNullOrWhiteSpace(entity.LastName))
            throw new ValidationException("Прізвище є обов'язковим.");
        if (entity.FirstName.Length > 100 || entity.LastName.Length > 100)
            throw new ValidationException("Ім'я/прізвище занадто довгі.");

        if (await _repo.ExistsByNameAsync(entity.FirstName, entity.LastName, excludeId: null, ct).ConfigureAwait(false))
            throw new ValidationException("An employee with the same first and last name already exists.");

        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
    {
        entity.FirstName = (entity.FirstName ?? "").Trim();
        entity.LastName = (entity.LastName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(entity.FirstName))
            throw new ValidationException("Ім'я є обов'язковим.");
        if (string.IsNullOrWhiteSpace(entity.LastName))
            throw new ValidationException("Прізвище є обов'язковим.");
        if (entity.FirstName.Length > 100 || entity.LastName.Length > 100)
            throw new ValidationException("Ім'я/прізвище занадто довгі.");

        if (await _repo.ExistsByNameAsync(entity.FirstName, entity.LastName, entity.Id, ct).ConfigureAwait(false))
            throw new ValidationException("An employee with the same first and last name already exists.");

        await _repo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var hasAvailability = await _repo.HasAvailabilityReferencesAsync(id, ct).ConfigureAwait(false);
        var hasSchedule = await _repo.HasScheduleReferencesAsync(id, ct).ConfigureAwait(false);

        if (hasAvailability || hasSchedule)
            throw new ValidationException("To delete this employee, first delete all Availability and Schedule entries where this employee is used.");

        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);
}
