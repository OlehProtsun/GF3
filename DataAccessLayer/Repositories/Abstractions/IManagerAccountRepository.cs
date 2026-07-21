using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for manager login accounts.
/// </summary>
public interface IManagerAccountRepository : IBaseRepository<ManagerAccountModel>
{
    Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<ManagerAccountModel?> GetSystemManagerAsync(CancellationToken ct = default);

    Task<bool> ExistsByUsernameAsync(string username, int? excludeManagerId = null, CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);

    Task RecordSuccessfulLoginAsync(int managerId, DateTimeOffset occurredAtUtc, CancellationToken ct = default);
}
