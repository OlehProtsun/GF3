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
            .FirstOrDefaultAsync(account => account.Username == normalizedUsername, ct);
    }

    public Task<bool> ExistsByUsernameAsync(string username, int? excludeEmployeeId = null, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        return _set
            .AsNoTracking()
            .AnyAsync(
                account =>
                    (!excludeEmployeeId.HasValue || account.EmployeeId != excludeEmployeeId.Value) &&
                    account.Username == normalizedUsername,
                ct);
    }

    public async Task RecordSuccessfulLoginAsync(int employeeId, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
        => await _set
            .Where(account => account.EmployeeId == employeeId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(account => account.LastLoginAtUtc, (DateTimeOffset?)occurredAtUtc)
                    .SetProperty(account => account.LastSeenAtUtc, (DateTimeOffset?)occurredAtUtc),
                ct)
            .ConfigureAwait(false);

    public async Task TouchLastSeenAsync(int employeeId, DateTimeOffset seenAtUtc, TimeSpan minInterval, CancellationToken ct = default)
        => await _db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE employee_account
                SET last_seen_at_utc = {seenAtUtc}
                WHERE employee_id = {employeeId}
                  AND (last_seen_at_utc IS NULL OR last_seen_at_utc < {seenAtUtc.Subtract(minInterval)})
                """,
                ct)
            .ConfigureAwait(false);

    public async Task<bool> IncrementSessionVersionAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .Where(account => account.EmployeeId == employeeId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    account => account.SessionVersion,
                    account => account.SessionVersion + 1),
                ct)
            .ConfigureAwait(false) > 0;

    private static string NormalizeUsername(string? username)
        => (username ?? string.Empty).Trim();
}
