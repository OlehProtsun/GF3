using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Employee-specific queries and dependency checks.
/// Besides the generic CRUD baseline, this repository centralizes the employee lookup rules used
/// by validation and delete-guard flows.
/// </summary>
public class EmployeeRepository : GenericRepository<EmployeeModel>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext db)
        : base(db)
    {
    }

    public async Task SoftDeleteAsync(int employeeId, CancellationToken ct = default)
    {
        var employee = await _set.FindAsync([employeeId], ct).ConfigureAwait(false);
        if (employee is null)
            return;

        EmployeeAccountModel? account = null;
        try
        {
            account = await _db.Set<EmployeeAccountModel>()
                .SingleOrDefaultAsync(value => value.EmployeeId == employeeId, ct).ConfigureAwait(false);
            employee.IsDeleted = true;
            if (account is not null)
                _db.Remove(account);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            if (account is not null)
                _db.Entry(account).State = EntityState.Detached;
            _db.Entry(employee).State = EntityState.Detached;
        }
    }

    public async Task<bool> UpdateActiveProfileAsync(EmployeeModel employee, CancellationToken ct = default)
        => await _set.Where(value => value.Id == employee.Id && !value.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.ContractType, employee.ContractType)
                .SetProperty(value => value.FirstName, employee.FirstName)
                .SetProperty(value => value.LastName, employee.LastName)
                .SetProperty(value => value.Phone, employee.Phone)
                .SetProperty(value => value.Email, employee.Email), ct).ConfigureAwait(false) == 1;

    public async Task<bool> UpdateActiveContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
        => await _set.Where(value => value.Id == employeeId && !value.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.Email, email)
                .SetProperty(value => value.Phone, phone), ct).ConfigureAwait(false) == 1;

    /// <summary>
    /// Performs a case-insensitive search across the employee fields used in the UI.
    /// Empty input returns the full read-only employee list to keep the caller logic simple.
    /// </summary>
    public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await _set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);

        return await _set
            .AsNoTracking()
            .Where(employee =>
                employee.FirstName.ToLower().Contains(normalized) ||
                employee.LastName.ToLower().Contains(normalized) ||
                (employee.Email != null && employee.Email.ToLower().Contains(normalized)) ||
                (employee.Phone != null && employee.Phone.ToLower().Contains(normalized)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether another employee already uses the same first/last name pair.
    /// This supports friendly validation before the request reaches a database constraint.
    /// </summary>
    public Task<bool> ExistsByNameAsync(
        string firstName,
        string lastName,
        int? excludeId = null,
        CancellationToken ct = default)
    {
        var normalizedFirstName = NormalizeSearchValue(firstName);
        var normalizedLastName = NormalizeSearchValue(lastName);

        return _set.AsNoTracking().AnyAsync(employee =>
            !employee.IsDeleted &&
            (!excludeId.HasValue || employee.Id != excludeId.Value) &&
            employee.FirstName.ToLower().Trim() == normalizedFirstName &&
            employee.LastName.ToLower().Trim() == normalizedLastName,
            ct);
    }

    /// <summary>
    /// Returns whether the employee is still referenced by any availability group member.
    /// </summary>
    public Task<bool> HasAvailabilityReferencesAsync(int employeeId, CancellationToken ct = default)
    {
        return _db.Set<AvailabilityGroupMemberModel>()
            .AsNoTracking()
            .AnyAsync(member => member.EmployeeId == employeeId, ct);
    }

    /// <summary>
    /// Returns whether the employee is still referenced by either schedule membership or concrete slots.
    /// Both checks are required because historical data may exist in either table.
    /// </summary>
    public async Task<bool> HasScheduleReferencesAsync(int employeeId, CancellationToken ct = default)
    {
        var hasScheduleEmployees = await _db.Set<ScheduleEmployeeModel>()
            .AsNoTracking()
            .AnyAsync(scheduleEmployee => scheduleEmployee.EmployeeId == employeeId, ct)
            .ConfigureAwait(false);

        if (hasScheduleEmployees)
        {
            return true;
        }

        return await _db.Set<ScheduleSlotModel>()
            .AsNoTracking()
            .AnyAsync(slot => slot.EmployeeId == employeeId, ct)
            .ConfigureAwait(false);
    }

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
