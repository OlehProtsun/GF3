using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
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
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee)
            .Include(group => group.Members)
                .ThenInclude(member => member.Days)
            .SingleOrDefaultAsync(group => group.Id == id, ct)
            .ConfigureAwait(false);

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

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
