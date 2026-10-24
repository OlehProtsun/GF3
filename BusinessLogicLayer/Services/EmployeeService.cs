using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System.Net.Mail;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Encapsulates employee-specific validation and safe delete rules.
/// The service keeps CRUD straightforward, but still centralizes the business
/// constraints that the UI and API should not have to duplicate.
/// </summary>
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
        entity.IsDeleted = false;
        NormalizeNameFields(entity);
        await ValidateEmployeeAsync(entity, excludeId: null, ct).ConfigureAwait(false);

        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _repo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
    {
        NormalizeNameFields(entity);
        await ValidateEmployeeAsync(entity, entity.Id, ct).ConfigureAwait(false);

        if (!await _repo.UpdateActiveProfileAsync(entity.ToDal(), ct).ConfigureAwait(false))
            throw new ValidationException("The employee profile is deleted or could not be found.");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await TryDeleteAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
    {
        await _repo.SoftDeleteAsync(id, ct).ConfigureAwait(false);
        return DeleteOperationResult.Success();
    }

    public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _repo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(employeeId, ct).ConfigureAwait(false)
            ?? throw new ValidationException("The employee profile could not be found.");
        if (existing.IsDeleted)
            throw new ValidationException("The employee profile is deleted or could not be found.");

        existing.Email = NormalizeEmail(email);
        existing.Phone = NormalizePhone(phone);

        if (!await _repo.UpdateActiveContactAsync(employeeId, existing.Email, existing.Phone, ct).ConfigureAwait(false))
            throw new ValidationException("The employee profile is deleted or could not be found.");
        return existing.ToContract();
    }

    private static void NormalizeNameFields(EmployeeModel entity)
    {
        entity.FirstName = (entity.FirstName ?? string.Empty).Trim();
        entity.LastName = (entity.LastName ?? string.Empty).Trim();
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmedEmail = email.Trim();
        if (trimmedEmail.Length > 254)
        {
            throw ValidationException.ForField("recoveryEmail", "Recovery email is too long.");
        }

        try
        {
            return new MailAddress(trimmedEmail).Address;
        }
        catch (FormatException)
        {
            throw ValidationException.ForField("recoveryEmail", "Enter a valid recovery email address.");
        }
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmedPhone = phone.Trim();
        if (trimmedPhone.Length > 50)
        {
            throw ValidationException.ForField("phone", "Phone number is too long.");
        }

        return trimmedPhone;
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
