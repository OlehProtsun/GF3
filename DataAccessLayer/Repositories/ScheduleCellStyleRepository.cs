using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for per-cell style overrides applied to one schedule matrix.
/// </summary>
public class ScheduleCellStyleRepository : GenericRepository<ScheduleCellStyleModel>, IScheduleCellStyleRepository
{
    public ScheduleCellStyleRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public async Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(style => style.ScheduleId == scheduleId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ScheduleCellStyleModel?> GetByScheduleCellAsync(int scheduleId, int dayOfMonth, int employeeId, CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .FirstOrDefaultAsync(
                style =>
                    style.ScheduleId == scheduleId &&
                    style.DayOfMonth == dayOfMonth &&
                    style.EmployeeId == employeeId,
                ct);

    /// <inheritdoc />
    public async Task ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleCellStyleModel> styles, CancellationToken ct = default)
    {
        var styleList = styles?.ToList() ?? [];
        foreach (var style in styleList)
        {
            style.Id = 0;
            style.ScheduleId = scheduleId;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await _set
            .Where(style => style.ScheduleId == scheduleId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        if (styleList.Count > 0)
        {
            await _set.AddRangeAsync(styleList, ct).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        _db.ChangeTracker.Clear();
    }
}
