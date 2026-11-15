using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

    public async Task<bool> ApplyFieldPatchesIfUnchangedAsync(
        int scheduleId, IReadOnlyList<ScheduleCellStyleFieldPatchModel> patches, CancellationToken ct = default)
    {
        var keys = new HashSet<(int EmployeeId, int DayOfMonth)>();
        foreach (var patch in patches)
        {
            if (patch.EmployeeId <= 0 || patch.DayOfMonth is < 1 or > 31 ||
                !patch.ChangeBackgroundColor && !patch.ChangeTextColor || !keys.Add((patch.EmployeeId, patch.DayOfMonth)))
                throw new ValidationException("Each valid cell must appear once and change at least one color.");
        }
        if (patches.Count == 0)
            return false;

        var employees = keys.Select(key => key.EmployeeId).Distinct().ToArray();
        var days = keys.Select(key => key.DayOfMonth).Distinct().ToArray();
        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var assigned = await _db.ScheduleEmployees
            .Where(row => row.ScheduleId == scheduleId && employees.Contains(row.EmployeeId))
            .Select(row => row.EmployeeId).ToHashSetAsync(ct).ConfigureAwait(false);
        var existing = await _set.AsTracking()
            .Where(row => row.ScheduleId == scheduleId && employees.Contains(row.EmployeeId) && days.Contains(row.DayOfMonth))
            .ToDictionaryAsync(row => (row.EmployeeId, row.DayOfMonth), ct).ConfigureAwait(false);

        var added = new List<ScheduleCellStyleModel>();
        try
        {
            var unassignedWithoutStyle = patches.Where(patch => !assigned.Contains(patch.EmployeeId) &&
                    !existing.ContainsKey((patch.EmployeeId, patch.DayOfMonth)))
                .Select(patch => patch.EmployeeId).Distinct().ToArray();
            HashSet<int> knownEmployees = unassignedWithoutStyle.Length == 0 ? [] : await _db.Employees.IgnoreQueryFilters()
                .Where(employee => unassignedWithoutStyle.Contains(employee.Id)).Select(employee => employee.Id)
                .ToHashSetAsync(ct).ConfigureAwait(false);
            var changes = new List<(ScheduleCellStyleFieldPatchModel Patch, ScheduleCellStyleModel? Row, int? Background, int? Text)>();
            foreach (var patch in patches)
            {
                existing.TryGetValue((patch.EmployeeId, patch.DayOfMonth), out var row);
                if (!assigned.Contains(patch.EmployeeId) && (row is null && !knownEmployees.Contains(patch.EmployeeId) ||
                    patch.ChangeBackgroundColor && patch.BackgroundColorArgb.HasValue ||
                    patch.ChangeTextColor && patch.TextColorArgb.HasValue))
                    throw new ValidationException("The employee must be assigned to this schedule to set a cell color.");

                var background = ResolveColor(row?.BackgroundColorArgb, patch.ChangeBackgroundColor,
                    patch.ExpectedBackgroundColorArgb, patch.BackgroundColorArgb);
                var text = ResolveColor(row?.TextColorArgb, patch.ChangeTextColor,
                    patch.ExpectedTextColorArgb, patch.TextColorArgb);
                if (background != row?.BackgroundColorArgb || text != row?.TextColorArgb ||
                    row is not null && background is null && text is null)
                    changes.Add((patch, row, background, text));
            }
            if (changes.Count == 0)
                return false;

            foreach (var (patch, row, background, text) in changes)
            {
                var style = row ?? new ScheduleCellStyleModel
                {
                    ScheduleId = scheduleId, EmployeeId = patch.EmployeeId, DayOfMonth = patch.DayOfMonth,
                };
                if (background is null && text is null)
                {
                    _set.Remove(style);
                    continue;
                }
                style.BackgroundColorArgb = background;
                style.TextColorArgb = text;
                if (row is null)
                {
                    _set.Add(style);
                    added.Add(style);
                }
            }
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        finally
        {
            foreach (var style in existing.Values.Concat(added))
                _db.Entry(style).State = EntityState.Detached;
        }
    }

    private static int? ResolveColor(int? current, bool change, int? expected, int? desired)
    {
        if (!change)
            return current;
        if (current != expected && current != desired)
            throw new DbUpdateConcurrencyException("A targeted schedule cell color changed.");
        return desired;
    }

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
