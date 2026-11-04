using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for availability groups.
/// Availability data is often consumed as a rich object graph, so this repository exposes both
/// lightweight list queries and fully expanded reads for edit/generation flows.
/// </summary>
public class AvailabilityGroupRepository : GenericRepository<AvailabilityGroupModel>, IAvailabilityGroupRepository
{
    public AvailabilityGroupRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
        => await CreateListQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await GetAllAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);
        var hasNumericValue = int.TryParse(normalized, out var numericValue);

        return await CreateListQuery()
            .Where(group =>
                group.Name.ToLower().Contains(normalized) ||
                group.Members.Any(member =>
                    member.Employee.FirstName.ToLower().Contains(normalized) ||
                    member.Employee.LastName.ToLower().Contains(normalized)) ||
                (hasNumericValue && (group.Year == numericValue || group.Month == numericValue || group.Id == numericValue)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .AsSplitQuery()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee)
            .Include(group => group.Members)
                .ThenInclude(member => member.Days)
            .SingleOrDefaultAsync(group => group.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var groups = await CreatePublishedEmployeeQuery(employeeId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return groups
            .Where(group => HasStartedVisibility(group, nowUtc))
            .OrderBy(group => IsClosedForSubmission(group, nowUtc))
            .ThenBy(group => group.VisibleToUtc)
            .ThenBy(group => group.Year)
            .ThenBy(group => group.Month)
            .ThenBy(group => group.Name)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AvailabilityGroupModel?> GetPublishedForEmployeeByIdAsync(
        int id,
        int employeeId,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        var group = await CreatePublishedEmployeeQuery(employeeId)
            .SingleOrDefaultAsync(group => group.Id == id, ct)
            .ConfigureAwait(false);

        return group is not null && HasStartedVisibility(group, nowUtc) ? group : null;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, int year, int month, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = NormalizeSearchValue(name);

        return _set.AsNoTracking().AnyAsync(group =>
            (!excludeId.HasValue || group.Id != excludeId.Value) &&
            group.Year == year &&
            group.Month == month &&
            group.Name.ToLower().Trim() == normalized,
            ct);
    }

    private IQueryable<AvailabilityGroupModel> CreateListQuery()
        => _set
            .AsNoTracking()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee);

    private IQueryable<AvailabilityGroupModel> CreatePublishedEmployeeQuery(int employeeId)
        => _set
            .AsNoTracking()
            .AsSplitQuery()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee)
            .Include(group => group.Members)
                .ThenInclude(member => member.Days)
            .Where(group =>
                group.PublicationStatus == AvailabilityPublicationStatus.Public &&
                group.Members.Any(member => member.EmployeeId == employeeId));

    private static bool HasStartedVisibility(AvailabilityGroupModel group, DateTimeOffset nowUtc)
    {
        var normalizedNowUtc = nowUtc.ToUniversalTime();
        return !group.VisibleFromUtc.HasValue || group.VisibleFromUtc.Value <= normalizedNowUtc;
    }

    private static bool IsClosedForSubmission(AvailabilityGroupModel group, DateTimeOffset nowUtc)
    {
        var normalizedNowUtc = nowUtc.ToUniversalTime();
        return group.VisibleToUtc.HasValue && group.VisibleToUtc.Value < normalizedNowUtc;
    }

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
