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
}
