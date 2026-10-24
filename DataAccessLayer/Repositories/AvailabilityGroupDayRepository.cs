using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for per-day availability rows of one group member.
/// </summary>
public class AvailabilityGroupDayRepository : GenericRepository<AvailabilityGroupDayModel>, IAvailabilityGroupDayRepository
{
    public AvailabilityGroupDayRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupDayModel>> GetByMemberIdAsync(int memberId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(day => day.AvailabilityGroupMemberId == memberId)
            .OrderBy(day => day.DayOfMonth)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task DeleteByMemberIdAsync(int memberId, CancellationToken ct = default)
    {
        await _set
            .Where(day => day.AvailabilityGroupMemberId == memberId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupDayModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
        => await (
            from day in _set.AsNoTracking()
            join member in _db.Set<AvailabilityGroupMemberModel>().AsNoTracking()
                on day.AvailabilityGroupMemberId equals member.Id
            where member.AvailabilityGroupId == groupId
            orderby member.EmployeeId, day.DayOfMonth
            select day)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<AvailabilityGroupDayModel> days, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(days);

        var dayList = days.ToList();
        if (dayList.Count == 0)
        {
            return;
        }

        await _set.AddRangeAsync(dayList, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _db.ChangeTracker.Clear();
    }
}
