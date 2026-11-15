using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Schedule;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using AvailabilityGroupDayModel = BusinessLogicLayer.Contracts.Models.AvailabilityGroupDayModel;
using AvailabilityKind = BusinessLogicLayer.Contracts.Enums.AvailabilityKind;

namespace BusinessLogicLayer.Services;

public sealed class EmploymentContractPolicyService(AppDbContext db) : IEmploymentContractPolicyService
{
    public async Task<EmploymentContractSettingsSnapshot> GetSettingsAsync(CancellationToken ct = default)
        => await db.EmploymentContractSettings.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => new EmploymentContractSettingsSnapshot(x.RestrictSwapsToEightHours,
                x.RequirePickupApproval, x.RequireBelowMinimumGiveApproval, x.AutomaticWeekdayAvailability))
            .SingleOrDefaultAsync(ct).ConfigureAwait(false) ?? new();

    public async Task<string> GetContractTypeAsync(int employeeId, CancellationToken ct = default)
        => await db.Employees.AsNoTracking().Where(x => x.Id == employeeId && !x.IsDeleted)
            .Select(x => x.ContractType).SingleOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The employee profile could not be found.");

    public async Task<IReadOnlyDictionary<int, string>> GetContractTypesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default)
        => await db.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x.ContractType, ct).ConfigureAwait(false);

    public async Task<MinimumHoursResult> GetShopMonthMinimumAsync(int employeeId, int shopId, int year, int month, CancellationToken ct = default)
    {
        if (employeeId <= 0 || shopId <= 0 || year is < 1 or > 9999 || month is < 1 or > 12)
            throw new ValidationException("Invalid employee, shop or calendar month.");
        await GetContractTypeAsync(employeeId, ct).ConfigureAwait(false);
        var minimum = await db.ScheduleEmployees.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.Schedule.ShopId == shopId && x.Schedule.Year == year &&
                x.Schedule.Month == month && x.MinHoursMonth > 0)
            .Select(x => x.MinHoursMonth).MaxAsync(ct).ConfigureAwait(false);
        var slots = await db.ScheduleSlots.AsNoTracking().Where(x => x.EmployeeId == employeeId &&
            x.Status == SlotStatus.ASSIGNED && x.Schedule.ShopId == shopId && x.Schedule.Year == year &&
            x.Schedule.Month == month && x.Schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .Select(x => new { x.Id, x.FromTime, x.ToTime }).Distinct().ToListAsync(ct).ConfigureAwait(false);
        var minutes = 0;
        foreach (var slot in slots)
        {
            if (!ScheduleMatrixEngine.TryParseTime(slot.FromTime, out var from) ||
                !ScheduleMatrixEngine.TryParseTime(slot.ToTime, out var to) || from.TotalMinutes < 0 ||
                to.TotalMinutes > 1440 || to <= from || from.TotalMinutes != (int)from.TotalMinutes ||
                to.TotalMinutes != (int)to.TotalMinutes)
                throw new ValidationException("An assigned shift has an invalid time period.");
            minutes = checked(minutes + (int)(to - from).TotalMinutes);
        }
        return new(minimum.HasValue, minimum.HasValue ? checked(minimum.Value * 60) : null, minutes);
    }

    public IReadOnlyList<AvailabilityGroupDayModel> BuildAutomaticWeekdayDays(int year, int month)
        => Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(day => new AvailabilityGroupDayModel
        {
            DayOfMonth = day,
            Kind = new DateOnly(year, month, day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? AvailabilityKind.NONE : AvailabilityKind.ANY,
        }).ToArray();

    public async Task<bool> IsRuleActiveAsync(int employeeId, EmploymentContractRule rule, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(rule)) throw new ArgumentOutOfRangeException(nameof(rule));
        if (await GetContractTypeAsync(employeeId, ct).ConfigureAwait(false) != EmploymentContractTypes.OPrace) return false;
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        return rule switch
        {
            EmploymentContractRule.RestrictSwapsToEightHours => settings.RestrictSwapsToEightHours,
            EmploymentContractRule.RequirePickupApproval => settings.RequirePickupApproval,
            EmploymentContractRule.RequireBelowMinimumGiveApproval => settings.RequireBelowMinimumGiveApproval,
            EmploymentContractRule.AutomaticWeekdayAvailability => settings.AutomaticWeekdayAvailability,
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };
    }

    public async Task<IReadOnlyCollection<int>> GetAutomaticAvailabilityEmployeeIdsAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default)
    {
        if (!(await GetSettingsAsync(ct).ConfigureAwait(false)).AutomaticWeekdayAvailability) return [];
        return (await GetContractTypesAsync(employeeIds, ct).ConfigureAwait(false))
            .Where(x => x.Value == EmploymentContractTypes.OPrace).Select(x => x.Key).ToArray();
    }
}
