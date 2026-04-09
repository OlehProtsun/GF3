using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for members that belong to an availability group.
/// </summary>
public class AvailabilityGroupMemberRepository : GenericRepository<AvailabilityGroupMemberModel>, IAvailabilityGroupMemberRepository
{
    public AvailabilityGroupMemberRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<AvailabilityGroupMemberModel>> GetAllAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(member => member.Employee)
            .Include(member => member.AvailabilityGroup)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupMemberModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(member => member.Employee)
            .Where(member => member.AvailabilityGroupId == groupId)
            .OrderBy(member => member.DisplayOrder)
            .ThenBy(member => member.Employee != null ? member.Employee.FirstName : string.Empty)
            .ThenBy(member => member.Employee != null ? member.Employee.LastName : string.Empty)
            .ThenBy(member => member.EmployeeId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<AvailabilityGroupMemberModel?> GetByGroupAndEmployeeAsync(int groupId, int employeeId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .SingleOrDefaultAsync(
                member => member.AvailabilityGroupId == groupId && member.EmployeeId == employeeId,
                ct)
            .ConfigureAwait(false);
}
