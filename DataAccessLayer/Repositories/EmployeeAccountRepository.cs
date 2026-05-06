using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Employee-account persistence queries used by manager account administration and login flows.
/// </summary>
public sealed class EmployeeAccountRepository : GenericRepository<EmployeeAccountModel>, IEmployeeAccountRepository
{
    public EmployeeAccountRepository(AppDbContext db)
        : base(db)
    {
    }

    public Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.EmployeeId == employeeId, ct);

    public async Task<List<EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
    {
        var idList = employeeIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (idList.Count == 0)
        {
            return [];
        }

        return await _set
            .AsNoTracking()
            .Where(account => idList.Contains(account.EmployeeId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        return _set
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Username.ToLower() == normalizedUsername, ct);
    }

    public Task<bool> ExistsByUsernameAsync(string username, int? excludeEmployeeId = null, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        return _set
            .AsNoTracking()
            .AnyAsync(
                account =>
                    (!excludeEmployeeId.HasValue || account.EmployeeId != excludeEmployeeId.Value) &&
                    account.Username.ToLower() == normalizedUsername,
                ct);
    }

    public async Task RecordSuccessfulLoginAsync(int employeeId, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
    {
        var account = await _set
            .FirstOrDefaultAsync(existingAccount => existingAccount.EmployeeId == employeeId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        account.LastLoginAtUtc = occurredAtUtc;
        account.LastSeenAtUtc = occurredAtUtc;
        await SaveChangesOrResetAsync(ct).ConfigureAwait(false);
    }

    public async Task TouchLastSeenAsync(int employeeId, DateTimeOffset seenAtUtc, TimeSpan minInterval, CancellationToken ct = default)
    {
        var account = await _set
            .FirstOrDefaultAsync(existingAccount => existingAccount.EmployeeId == employeeId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        if (account.LastSeenAtUtc.HasValue && seenAtUtc <= account.LastSeenAtUtc.Value.Add(minInterval))
        {
            return;
        }

        account.LastSeenAtUtc = seenAtUtc;
        await SaveChangesOrResetAsync(ct).ConfigureAwait(false);
    }

    private static string NormalizeUsername(string? username)
        => (username ?? string.Empty).Trim().ToLowerInvariant();

    private async Task SaveChangesOrResetAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            throw;
        }
    }
}
