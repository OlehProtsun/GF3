using System.Globalization;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.EmployeeLaunches;
using WebApi.Realtime;

namespace WebApi.Services;

public sealed class EmployeeLaunchService(AppDbContext db, IEmployeePresenceService presence,
    IRealtimeNotifier realtime, TimeProvider timeProvider,
    ILogger<EmployeeLaunchService> logger)
{
    public async Task RecordAsync(int employeeId, string mode, CancellationToken ct)
    {
        if (mode is not ("browser" or "pwa"))
            throw ValidationException.ForField(nameof(RecordEmployeeLaunchRequest.Mode), "Mode must be browser or pwa.");

        var now = timeProvider.GetUtcNow();
        var account = db.EmployeeAccounts.Where(account => account.EmployeeId == employeeId && !account.Employee.IsDeleted);
        var rows = mode == "pwa"
            ? await account.ExecuteUpdateAsync(update => update.SetProperty(x => x.LastPwaLaunchAtUtc, now), ct)
            : await account.ExecuteUpdateAsync(update => update.SetProperty(x => x.LastBrowserLaunchAtUtc, now), ct);
        if (rows == 0) throw new KeyNotFoundException("The employee account was not found.");

        try
        {
            await realtime.NotifyManagerDataChangedAsync("employee-launches", employeeId.ToString(CultureInfo.InvariantCulture), "employee-launch-recorded");
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Failed to notify managers about employee launch for {EmployeeId}.", employeeId);
        }
    }

    public async Task<IReadOnlyList<EmployeeLaunchUsageDto>> GetUsageAsync(CancellationToken ct)
    {
        var employees = await db.Employees.AsNoTracking().Where(employee => !employee.IsDeleted)
            .OrderBy(employee => employee.LastName).ThenBy(employee => employee.FirstName).ThenBy(employee => employee.Id)
            .Select(employee => new
            {
                employee.Id, employee.FirstName, employee.LastName,
                HasAccount = employee.Account != null,
                Username = employee.Account!.Username,
                employee.Account!.LastLoginAtUtc, employee.Account!.LastSeenAtUtc,
                employee.Account!.LastBrowserLaunchAtUtc, employee.Account!.LastPwaLaunchAtUtc,
            }).ToListAsync(ct);
        var online = presence.GetOnlineStates(employees.Select(employee => employee.Id));
        return employees.Select(employee =>
        {
            var browser = employee.LastBrowserLaunchAtUtc;
            var pwa = employee.LastPwaLaunchAtUtc;
            var lastWasPwa = pwa.HasValue && (!browser.HasValue || pwa >= browser);
            return new EmployeeLaunchUsageDto(employee.Id, $"{employee.FirstName} {employee.LastName}".Trim(),
                employee.Username, employee.HasAccount, online.GetValueOrDefault(employee.Id), employee.LastLoginAtUtc,
                employee.LastSeenAtUtc, browser, pwa, lastWasPwa ? "pwa" : browser.HasValue ? "browser" : null,
                lastWasPwa ? pwa : browser);
        }).ToList();
    }
}
