using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for availability groups and their richer read models.
/// </summary>
public interface IAvailabilityGroupRepository : IBaseRepository<AvailabilityGroupModel>
{
    /// <summary>
    /// Searches availability groups by name, related employee names, and numeric year/month/id tokens.
    /// </summary>
    Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns one group with members and member-day entries fully loaded.
    /// </summary>
    Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns employee-visible published groups that include the supplied employee.
    /// </summary>
    Task<List<AvailabilityGroupModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns one employee-visible published group that includes the supplied employee.
    /// </summary>
    Task<AvailabilityGroupModel?> GetPublishedForEmployeeByIdAsync(int id, int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns whether another group already uses the same normalized name within the same month.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, int year, int month, int? excludeId = null, CancellationToken ct = default);
}
