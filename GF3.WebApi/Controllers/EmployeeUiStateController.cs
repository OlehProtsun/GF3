using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-ui-state")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeUiStateController(AppDbContext db) : ControllerBase
{
    private const int MaxColumnCount = 200;
    private const int MaxNotificationIdLength = 256;
    private const int MaxReadNotificationIds = 300;
    private static readonly TimeSpan TransientNotificationRetention = EmployeeNotificationRetention.Duration;

    [HttpGet]
    [ProducesResponseType(typeof(EmployeeUiStateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeUiStateDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var storedOrders = await db.EmployeeScheduleColumnPreferences
            .AsNoTracking()
            .Where(preference => preference.EmployeeId == employeeId)
            .Select(preference => new { preference.ScheduleId, preference.ColumnOrderJson })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow.Subtract(TransientNotificationRetention);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM employee_notification_read
            WHERE employee_id = {employeeId} AND julianday(read_at_utc) <= julianday({cutoff})
            """, cancellationToken).ConfigureAwait(false);
        var readNotificationIds = await db.EmployeeNotificationReads
            .FromSqlInterpolated(
                $"""
                SELECT id, employee_id, notification_id, read_at_utc
                FROM employee_notification_read
                WHERE employee_id = {employeeId}
                  AND julianday(read_at_utc) > julianday({cutoff})
                ORDER BY read_at_utc DESC
                """)
            .AsNoTracking()
            .Select(read => read.NotificationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var pinnedSwaps = await db.EmployeePinnedSwaps
            .FromSqlInterpolated(
                $"""
                SELECT id, employee_id, shift_swap_id, pinned_at_utc
                FROM employee_pinned_swap
                WHERE employee_id = {employeeId}
                ORDER BY pinned_at_utc DESC
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var pinnedSwapIds = pinnedSwaps.Select(pin => pin.ShiftSwapId).ToList();

        return Ok(new EmployeeUiStateDto
        {
            ScheduleColumnOrders = storedOrders.ToDictionary(
                item => item.ScheduleId,
                item => DeserializeColumnOrder(item.ColumnOrderJson)),
            ReadNotificationIds = readNotificationIds,
            PinnedSwapIds = pinnedSwapIds,
        });
    }

    [HttpGet("availability-time-presets")]
    public async Task<ActionResult<EmployeeAvailabilityTimePresetsDto>> GetAvailabilityTimePresets(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var account = await db.EmployeeAccounts.AsNoTracking()
            .Where(item => item.EmployeeId == employeeId)
            .Select(item => new { item.AvailabilityTimePresetsJson })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (account is null) return Unauthorized();
        IReadOnlyList<string>? presets = null;
        if (account.AvailabilityTimePresetsJson is not null)
        {
            try { presets = JsonSerializer.Deserialize<string[]>(account.AvailabilityTimePresetsJson); }
            catch (JsonException) { presets = null; }
        }
        return Ok(new EmployeeAvailabilityTimePresetsDto { Presets = presets });
    }

    [HttpPut("availability-time-presets")]
    public async Task<ActionResult<EmployeeAvailabilityTimePresetsDto>> SaveAvailabilityTimePresets(
        SaveEmployeeAvailabilityTimePresetsRequest request, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var normalized = new List<string>();
        if (request.Presets is null || request.Presets.Count is < 3 or > 100)
        {
            ModelState.AddModelError(nameof(request.Presets), "Store between 3 and 100 time presets.");
            return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, modelStateDictionary: ModelState);
        }
        foreach (var preset in request.Presets)
        {
            var parts = preset?.Split('-', StringSplitOptions.TrimEntries);
            if (parts is not { Length: 2 } ||
                !TimeOnly.TryParseExact(parts[0], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
                !TimeOnly.TryParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) || to <= from)
            {
                ModelState.AddModelError(nameof(request.Presets), "Use a valid time range like 09:00 - 15:00.");
                return ValidationProblem(statusCode: StatusCodes.Status400BadRequest, modelStateDictionary: ModelState);
            }
            normalized.Add($"{from.ToString("HH:mm", CultureInfo.InvariantCulture)} - {to.ToString("HH:mm", CultureInfo.InvariantCulture)}");
        }
        var json = JsonSerializer.Serialize(normalized);
        // Import an older device's local values only if no device has saved a server preference yet.
        var updated = request.OnlyIfUnset
            ? await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE employee_account SET availability_time_presets_json = COALESCE(availability_time_presets_json, {json}) WHERE employee_id = {employeeId}", cancellationToken).ConfigureAwait(false)
            : await db.EmployeeAccounts.Where(account => account.EmployeeId == employeeId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(account => account.AvailabilityTimePresetsJson, json), cancellationToken).ConfigureAwait(false);
        if (updated == 0) return Unauthorized();
        return await GetAvailabilityTimePresets(cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("swap-pins/{swapId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PinSwap(int swapId, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        if (!await IsSwapVisibleToEmployeeAsync(swapId, employeeId, cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        var pinnedAtUtc = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO employee_pinned_swap (employee_id, shift_swap_id, pinned_at_utc)
            VALUES ({employeeId}, {swapId}, {pinnedAtUtc})
            ON CONFLICT(employee_id, shift_swap_id) DO UPDATE SET
                pinned_at_utc = excluded.pinned_at_utc
            """,
            cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    [HttpDelete("swap-pins/{swapId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnpinSwap(int swapId, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        await db.EmployeePinnedSwaps
            .Where(pin => pin.EmployeeId == employeeId && pin.ShiftSwapId == swapId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut("schedule-columns/{scheduleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveScheduleColumnOrder(
        int scheduleId,
        [FromBody] SaveEmployeeScheduleColumnOrderRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var isVisibleSchedule = await db.Schedules
            .AsNoTracking()
            .AnyAsync(schedule =>
                schedule.Id == scheduleId &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken)
            .ConfigureAwait(false);
        if (!isVisibleSchedule)
        {
            return NotFound();
        }

        var columnOrder = (request.ColumnOrder ?? [])
            .Where(columnId => columnId != 0)
            .Distinct()
            .Take(MaxColumnCount + 1)
            .ToArray();
        if (columnOrder.Length > MaxColumnCount)
        {
            ModelState.AddModelError(nameof(request.ColumnOrder), $"At most {MaxColumnCount} columns can be stored.");
            return ValidationProblem(ModelState);
        }

        if (columnOrder.Length == 0)
        {
            await db.EmployeeScheduleColumnPreferences
                .Where(preference => preference.EmployeeId == employeeId && preference.ScheduleId == scheduleId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }

        var columnOrderJson = JsonSerializer.Serialize(columnOrder);
        var updatedAtUtc = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO employee_schedule_column_preference (
                employee_id,
                schedule_id,
                column_order_json,
                updated_at_utc
            )
            VALUES ({employeeId}, {scheduleId}, {columnOrderJson}, {updatedAtUtc})
            ON CONFLICT(employee_id, schedule_id) DO UPDATE SET
                column_order_json = excluded.column_order_json,
                updated_at_utc = excluded.updated_at_utc
            """,
            cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("notifications/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkNotificationsRead(
        [FromBody] MarkEmployeeNotificationsReadRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var notificationIds = (request.NotificationIds ?? [])
            .Select(notificationId => notificationId?.Trim() ?? string.Empty)
            .Where(notificationId => notificationId.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxReadNotificationIds + 1)
            .ToArray();
        if (notificationIds.Length > MaxReadNotificationIds || notificationIds.Any(id => id.Length > MaxNotificationIdLength))
        {
            ModelState.AddModelError(
                nameof(request.NotificationIds),
                $"Store at most {MaxReadNotificationIds} notification ids, each up to {MaxNotificationIdLength} characters.");
            return ValidationProblem(ModelState);
        }

        var readAtUtc = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var notificationId in notificationIds)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO employee_notification_read (employee_id, notification_id, read_at_utc)
                VALUES ({employeeId}, {notificationId}, {readAtUtc})
                ON CONFLICT(employee_id, notification_id) DO UPDATE SET
                    read_at_utc = excluded.read_at_utc
                """,
                cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
    }

    private Task<bool> IsSwapVisibleToEmployeeAsync(
        int swapId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var sharedContainerIds = db.Schedules
            .Where(schedule =>
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
            .Select(schedule => schedule.ContainerId);

        return db.ShiftSwapRequests
            .AsNoTracking()
            .AnyAsync(request =>
                request.Id == swapId &&
                ((request.Status != ShiftSwapStatus.Open && request.ScheduleSlotId == null && request.ArchivedViewsJson != null) || request.Schedule.PublicationStatus == SchedulePublicationStatus.Public) &&
                (request.FromEmployeeId == employeeId ||
                 request.TargetEmployeeId == employeeId ||
                 request.AcceptedByEmployeeId == employeeId ||
                 (request.Visibility == ShiftSwapVisibility.Public &&
                  sharedContainerIds.Contains(request.Schedule.ContainerId))) &&
                (request.Status == ShiftSwapStatus.Open ||
                 request.AcceptedByEmployeeId == employeeId ||
                 request.FromEmployeeId == employeeId),
                cancellationToken);
    }

    private static int[] DeserializeColumnOrder(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<int[]>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public sealed class EmployeeUiStateDto
{
    public IReadOnlyDictionary<int, int[]> ScheduleColumnOrders { get; init; } = new Dictionary<int, int[]>();

    public IReadOnlyList<string> ReadNotificationIds { get; init; } = [];

    public IReadOnlyList<int> PinnedSwapIds { get; init; } = [];
}

public sealed class SaveEmployeeScheduleColumnOrderRequest
{
    public IReadOnlyList<int>? ColumnOrder { get; init; }
}

public sealed class MarkEmployeeNotificationsReadRequest
{
    public IReadOnlyList<string>? NotificationIds { get; init; }
}

public sealed class EmployeeAvailabilityTimePresetsDto
{
    public IReadOnlyList<string>? Presets { get; init; }
}

public sealed class SaveEmployeeAvailabilityTimePresetsRequest
{
    public IReadOnlyList<string>? Presets { get; init; }
    public bool OnlyIfUnset { get; init; }
}
