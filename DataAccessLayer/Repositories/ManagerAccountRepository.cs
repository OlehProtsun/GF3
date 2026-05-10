using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Manager-account persistence queries used by authentication and manager profile administration.
/// </summary>
public sealed class ManagerAccountRepository : GenericRepository<ManagerAccountModel>, IManagerAccountRepository
{
    public ManagerAccountRepository(AppDbContext db)
        : base(db)
    {
    }

    public override async Task<List<ManagerAccountModel>> GetAllAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .OrderBy(account => account.DisplayName)
            .ThenBy(account => account.Username)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        return _set
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Username.ToLower() == normalizedUsername, ct);
    }

    public Task<bool> ExistsByUsernameAsync(string username, int? excludeManagerId = null, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);

        return _set
            .AsNoTracking()
            .AnyAsync(
                account =>
                    (!excludeManagerId.HasValue || account.Id != excludeManagerId.Value) &&
                    account.Username.ToLower() == normalizedUsername,
                ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => _set.AsNoTracking().AnyAsync(ct);

    public async Task RecordSuccessfulLoginAsync(int managerId, DateTimeOffset occurredAtUtc, CancellationToken ct = default)
    {
        var account = await _set
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == managerId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        account.LastLoginAtUtc = occurredAtUtc;
        account.UpdatedAtUtc = occurredAtUtc;
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
