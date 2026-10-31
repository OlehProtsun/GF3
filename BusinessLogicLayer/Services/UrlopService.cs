using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Urlop;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services;

public sealed class UrlopService(AppDbContext db, TimeProvider timeProvider) : IUrlopService
{
    public async Task<UrlopEmployeeCalendar> GetEmployeeCalendarAsync(int employeeId, DateOnly currentMonth, CancellationToken ct)
    {
        await EnsureActiveEmployeeAsync(employeeId, ct);
        var from = new DateOnly(currentMonth.Year, currentMonth.Month, 1);
        var to = from.AddMonths(12).AddDays(-1);
        var shopIds = db.ScheduleEmployees.Where(x => x.EmployeeId == employeeId).Select(x => x.Schedule.ShopId);
        var shops = await db.Shops.AsNoTracking().Where(x => shopIds.Contains(x.Id))
            .OrderBy(x => x.Name).Select(x => new UrlopShop(x.Id, x.Name)).ToListAsync(ct);
        var ids = shops.Select(x => x.Id).ToArray();
        var settings = await db.UrlopMonths.AsNoTracking().Where(x => ids.Contains(x.ShopId) &&
            (x.Year > from.Year || x.Year == from.Year && x.Month >= from.Month) &&
            (x.Year < to.Year || x.Year == to.Year && x.Month <= to.Month)).ToListAsync(ct);
        var rows = await db.UrlopDays.AsNoTracking().Where(x => ids.Contains(x.ShopId) && x.Date >= from && x.Date <= to &&
            (x.EmployeeId == employeeId || x.Status == UrlopStatus.Approved)).ToListAsync(ct);
        var names = await GetNamesAsync(rows, ct);
        var requests = GroupRequestRows(rows.Where(x => x.EmployeeId == employeeId), names);
        var months = new List<UrlopCalendarMonth>();
        foreach (var shop in shops)
        for (var offset = 0; offset < 12; offset++)
        {
            var date = from.AddMonths(offset);
            months.Add(new(shop.Id, date.Year, date.Month,
                settings.Any(x => x.ShopId == shop.Id && x.Year == date.Year && x.Month == date.Month && x.IsOpen),
                requests.Where(x => x.ShopId == shop.Id && x.Year == date.Year && x.Month == date.Month).ToArray(),
                rows.Where(x => x.ShopId == shop.Id && x.Date.Year == date.Year && x.Date.Month == date.Month && x.Status == UrlopStatus.Approved)
                    .Select(x => ToApproved(x, names)).ToArray()));
        }
        return new(shops, months);
    }

    public async Task<UrlopManagerMonth> GetManagerMonthAsync(int shopId, int year, int month, CancellationToken ct)
    {
        var from = MonthStart(year, month);
        var to = from.AddDays(DateTime.DaysInMonth(year, month) - 1);
        var shopName = await db.Shops.Where(x => x.Id == shopId).Select(x => x.Name).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Shop not found.");
        var isOpen = await db.UrlopMonths.AnyAsync(x => x.ShopId == shopId && x.Year == year && x.Month == month && x.IsOpen, ct);
        var rows = await db.UrlopDays.AsNoTracking().Where(x => x.ShopId == shopId && x.Date >= from && x.Date <= to).ToListAsync(ct);
        var names = await GetNamesAsync(rows, ct);
        return new(shopId, shopName, year, month, isOpen, GroupRequestRows(rows, names),
            rows.Where(x => x.Status == UrlopStatus.Approved).Select(x => ToApproved(x, names)).ToArray());
    }

    public async Task<IReadOnlyList<ApprovedUrlopDay>> GetApprovedDaysAsync(DateOnly from, DateOnly to, int? shopId, CancellationToken ct)
    {
        if (from > to) throw new ValidationException("Invalid date range.");
        return await db.UrlopDays.AsNoTracking().Where(x => x.Status == UrlopStatus.Approved && x.Date >= from && x.Date <= to &&
            (!shopId.HasValue || x.ShopId == shopId.Value))
            .Select(x => new ApprovedUrlopDay(x.ShopId, x.EmployeeId, x.Date, "")).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ApprovedUrlopDay>> GetEmployeeApprovedMonthAsync(int employeeId, int shopId, int year, int month, CancellationToken ct)
    {
        var from = MonthStart(year, month);
        await EnsureEligibleEmployeeShopAsync(employeeId, shopId, ct);
        return await GetApprovedDaysAsync(from, from.AddDays(DateTime.DaysInMonth(year, month) - 1), shopId, ct);
    }

    public Task<UrlopRequest> SubmitAsync(int employeeId, int shopId, int year, int month, IReadOnlyList<int> days, DateOnly today, CancellationToken ct)
        => WriteAsync(async () =>
        {
            var dates = ValidateMonthAndDates(year, month, days);
            var current = new DateOnly(today.Year, today.Month, 1);
            if (dates.Any(x => x < today || x >= current.AddMonths(12))) throw new ValidationException("Vacation dates must be within the next 12 calendar months and cannot be in the past.");
            await EnsureEligibleEmployeeShopAsync(employeeId, shopId, ct);
            var updated = await db.UrlopMonths.Where(x => x.ShopId == shopId && x.Year == year && x.Month == month && x.IsOpen)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.Revision, x => x.Revision + 1), ct);
            if (updated == 0 || !await db.UrlopMonths.AnyAsync(x => x.ShopId == shopId && x.Year == year && x.Month == month && x.IsOpen, ct))
                throw new UrlopConflictException("urlop_month_closed", "Month closed by manager.");
            return await CreateAsync(null, shopId, employeeId, dates, UrlopStatus.Pending, ct);
        }, ct);

    public Task<UrlopMonth> SetMonthOpenAsync(int shopId, int year, int month, bool isOpen, CancellationToken ct)
        => WriteAsync(async () =>
        {
            _ = MonthStart(year, month);
            await EnsureShopAsync(shopId, ct);
            var setting = await db.UrlopMonths.SingleOrDefaultAsync(x => x.ShopId == shopId && x.Year == year && x.Month == month, ct);
            if (setting is not null) await db.Entry(setting).ReloadAsync(ct);
            if (setting is null)
            {
                setting = new() { ShopId = shopId, Year = year, Month = month };
                db.UrlopMonths.Add(setting);
            }
            setting.IsOpen = isOpen;
            setting.Revision++;
            return new UrlopMonth(shopId, year, month, isOpen);
        }, ct);

    public Task<UrlopRequest> CreateApprovedAsync(int managerId, int shopId, int employeeId, int year, int month, IReadOnlyList<int> days, CancellationToken ct)
        => WriteAsync(async () =>
        {
            var dates = ValidateMonthAndDates(year, month, days);
            await EnsureShopAsync(shopId, ct);
            await EnsureActiveEmployeeAsync(employeeId, ct);
            await EnsureNoAssignedShiftsAsync(shopId, employeeId, dates, ct);
            return await CreateAsync(managerId, shopId, employeeId, dates, UrlopStatus.Approved, ct);
        }, ct);

    public Task<UrlopRequest> ReplaceDaysAsync(int managerId, Guid requestId, IReadOnlyList<int> days, CancellationToken ct)
        => WriteAsync(async () =>
        {
            var rows = await LoadRequestForUpdateAsync(requestId, ct);
            var first = rows[0];
            if (first.Status == UrlopStatus.Rejected) throw new UrlopConflictException("urlop_request_not_pending", "Rejected vacation requests cannot be edited.");
            var dates = ValidateMonthAndDates(first.Date.Year, first.Date.Month, days);
            await EnsureNoDuplicatesAsync(first.ShopId, first.EmployeeId, dates, requestId, ct);
            if (first.Status == UrlopStatus.Approved) await EnsureNoAssignedShiftsAsync(first.ShopId, first.EmployeeId, dates, ct);
            db.UrlopDays.RemoveRange(rows.Where(x => !dates.Contains(x.Date)));
            foreach (var date in dates.Where(date => rows.All(x => x.Date != date)))
                db.UrlopDays.Add(new() { RequestId = requestId, ShopId = first.ShopId, EmployeeId = first.EmployeeId,
                    Date = date, Status = first.Status, CreatedAtUtc = first.CreatedAtUtc, CreatedByManagerId = first.CreatedByManagerId,
                    ReviewedAtUtc = first.ReviewedAtUtc, ReviewedByManagerId = first.ReviewedByManagerId });
            var final = rows.Where(x => dates.Contains(x.Date)).Concat(db.ChangeTracker.Entries<UrlopDayModel>()
                .Where(x => x.State == EntityState.Added && x.Entity.RequestId == requestId).Select(x => x.Entity)).ToList();
            return (await ToRequestsAsync(final, ct)).Single();
        }, ct);

    public Task<UrlopRequest> ApproveAsync(int managerId, Guid requestId, CancellationToken ct)
        => ReviewAsync(managerId, requestId, UrlopStatus.Approved, ct);

    public Task<UrlopRequest> RejectAsync(int managerId, Guid requestId, CancellationToken ct)
        => ReviewAsync(managerId, requestId, UrlopStatus.Rejected, ct);

    public async Task DeleteAsync(int managerId, Guid requestId, CancellationToken ct)
        => await WriteAsync(async () => { db.UrlopDays.RemoveRange(await LoadRequestForUpdateAsync(requestId, ct)); return true; }, ct);

    public async Task EnsureAssignmentsAllowedAsync(int shopId, int year, int month, IEnumerable<(int employeeId, int day)> assignments, CancellationToken ct)
    {
        var keys = assignments.ToHashSet();
        if (keys.Count == 0) return;
        var from = MonthStart(year, month);
        var approved = await GetApprovedDaysAsync(from, from.AddDays(DateTime.DaysInMonth(year, month) - 1), shopId, ct);
        var conflicts = approved.Where(x => keys.Contains((x.EmployeeId, x.Date.Day))).Select(x => x.Date).Distinct().Order().ToArray();
        if (conflicts.Length > 0) throw new UrlopConflictException("urlop_shift_conflict", "Conflict with scheduled shift.", conflicts);
    }

    private Task<UrlopRequest> ReviewAsync(int managerId, Guid requestId, string status, CancellationToken ct)
        => WriteAsync(async () =>
        {
            var rows = await LoadRequestForUpdateAsync(requestId, ct);
            var first = rows[0];
            if (first.Status != UrlopStatus.Pending) throw new UrlopConflictException("urlop_request_not_pending", "Vacation request is no longer pending.");
            if (status == UrlopStatus.Approved) await EnsureNoAssignedShiftsAsync(first.ShopId, first.EmployeeId, rows.Select(x => x.Date).ToArray(), ct);
            var now = timeProvider.GetUtcNow();
            foreach (var row in rows) { row.Status = status; row.ReviewedAtUtc = now; row.ReviewedByManagerId = managerId; }
            return (await ToRequestsAsync(rows, ct)).Single();
        }, ct);

    private async Task<UrlopRequest> CreateAsync(int? managerId, int shopId, int employeeId, DateOnly[] dates, string status, CancellationToken ct)
    {
        await EnsureNoDuplicatesAsync(shopId, employeeId, dates, null, ct);
        var now = timeProvider.GetUtcNow();
        var requestId = Guid.NewGuid();
        var rows = dates.Select(date => new UrlopDayModel { RequestId = requestId, ShopId = shopId, EmployeeId = employeeId,
            Date = date, Status = status, CreatedAtUtc = now, CreatedByManagerId = managerId,
            ReviewedAtUtc = managerId.HasValue ? now : null, ReviewedByManagerId = managerId }).ToList();
        db.UrlopDays.AddRange(rows);
        return (await ToRequestsAsync(rows, ct)).Single();
    }

    private async Task<T> WriteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await action();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is SqliteException { SqliteErrorCode: 5 or 6 or 19 } ||
            ex is DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 or 19 } })
        {
            db.ChangeTracker.Clear();
            throw new UrlopConflictException("urlop_concurrent_change", "Vacation data changed. Refresh and try again.");
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task EnsureNoAssignedShiftsAsync(int shopId, int employeeId, DateOnly[] dates, CancellationToken ct)
    {
        var first = dates[0];
        var days = dates.Select(x => x.Day).ToArray();
        var conflicts = await db.ScheduleSlots.AsNoTracking().Where(x => x.EmployeeId == employeeId &&
            x.Schedule.ShopId == shopId && x.Schedule.Year == first.Year && x.Schedule.Month == first.Month && days.Contains(x.DayOfMonth))
            .Select(x => new { x.ScheduleId, x.DayOfMonth }).ToListAsync(ct);
        if (conflicts.Count > 0) throw new UrlopConflictException("urlop_shift_conflict", "Conflict with scheduled shift.",
            conflicts.Select(x => new DateOnly(first.Year, first.Month, x.DayOfMonth)).Distinct().Order().ToArray(),
            conflicts.Select(x => x.ScheduleId).Distinct().Order().ToArray());
    }

    private async Task EnsureNoDuplicatesAsync(int shopId, int employeeId, DateOnly[] dates, Guid? except, CancellationToken ct)
    {
        if (await db.UrlopDays.AnyAsync(x => x.ShopId == shopId && x.EmployeeId == employeeId && dates.Contains(x.Date) &&
            x.Status != UrlopStatus.Rejected && (!except.HasValue || x.RequestId != except.Value), ct))
            throw new UrlopConflictException("urlop_day_taken", "Vacation already requested for these dates.");
    }

    private async Task EnsureActiveEmployeeAsync(int employeeId, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId && !x.IsDeleted, ct)) throw new KeyNotFoundException("Employee not found.");
    }

    private async Task EnsureShopAsync(int shopId, CancellationToken ct)
    {
        if (!await db.Shops.AnyAsync(x => x.Id == shopId, ct)) throw new KeyNotFoundException("Shop not found.");
    }

    private async Task EnsureEligibleEmployeeShopAsync(int employeeId, int shopId, CancellationToken ct)
    {
        await EnsureActiveEmployeeAsync(employeeId, ct);
        if (!await db.ScheduleEmployees.AnyAsync(x => x.EmployeeId == employeeId && x.Schedule.ShopId == shopId, ct))
            throw new KeyNotFoundException("Shop not found.");
    }

    private async Task<List<UrlopDayModel>> LoadRequestForUpdateAsync(Guid requestId, CancellationToken ct)
    {
        var rows = await db.UrlopDays.Where(x => x.RequestId == requestId).OrderBy(x => x.Date).ToListAsync(ct);
        foreach (var row in rows) await db.Entry(row).ReloadAsync(ct);
        if (rows.Count == 0) throw new KeyNotFoundException("Vacation request not found.");
        _ = GroupRequestRows(rows, new Dictionary<int, string>());
        return rows;
    }

    private static DateOnly MonthStart(int year, int month)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12) throw new ValidationException("Invalid year or month.");
        return new(year, month, 1);
    }

    private static DateOnly[] ValidateMonthAndDates(int year, int month, IReadOnlyList<int> days)
    {
        _ = MonthStart(year, month);
        if (days is null || days.Count == 0 || days.Count != days.Distinct().Count() || days.Any(x => x < 1 || x > DateTime.DaysInMonth(year, month)))
            throw new ValidationException("Select distinct valid vacation days.");
        return days.Order().Select(day => new DateOnly(year, month, day)).ToArray();
    }

    private async Task<Dictionary<int, string>> GetNamesAsync(IEnumerable<UrlopDayModel> rows, CancellationToken ct)
    {
        var ids = rows.Select(x => x.EmployeeId).Distinct().ToArray();
        return await db.Employees.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, ct);
    }

    private async Task<IReadOnlyList<UrlopRequest>> ToRequestsAsync(List<UrlopDayModel> rows, CancellationToken ct)
        => GroupRequestRows(rows, await GetNamesAsync(rows, ct));

    private static ApprovedUrlopDay ToApproved(UrlopDayModel row, IReadOnlyDictionary<int, string> names)
        => new(row.ShopId, row.EmployeeId, row.Date, names.GetValueOrDefault(row.EmployeeId, ""));

    private static IReadOnlyList<UrlopRequest> GroupRequestRows(IEnumerable<UrlopDayModel> rows, IReadOnlyDictionary<int, string> names)
        => rows.GroupBy(x => x.RequestId).Select(group =>
        {
            var first = group.First();
            if (group.Any(x => x.ShopId != first.ShopId || x.EmployeeId != first.EmployeeId || x.Date.Year != first.Date.Year ||
                x.Date.Month != first.Date.Month || x.Status != first.Status || x.CreatedAtUtc != first.CreatedAtUtc ||
                x.CreatedByManagerId != first.CreatedByManagerId || x.ReviewedAtUtc != first.ReviewedAtUtc || x.ReviewedByManagerId != first.ReviewedByManagerId) ||
                group.Select(x => x.Date).Distinct().Count() != group.Count()) throw new ValidationException("Invalid vacation request history.");
            return new UrlopRequest(first.RequestId, first.EmployeeId, names.GetValueOrDefault(first.EmployeeId, ""), first.ShopId,
                first.Date.Year, first.Date.Month, group.Select(x => x.Date.Day).Order().ToArray(), first.Status,
                first.CreatedAtUtc, first.ReviewedAtUtc, first.CreatedByManagerId, first.ReviewedByManagerId);
        }).OrderByDescending(x => x.CreatedAtUtc).ToArray();
}
