using System.Globalization;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace WebApi.Controllers;

[ApiController]
[Route("api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ShiftSwapLogsController(
    AppDbContext db,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier,
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    [HttpPost("manual")]
    [ProducesResponseType(typeof(ShiftSwapDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ShiftSwapDto>> CreateManualOffer(
        int containerId,
        int graphId,
        [FromBody] CreateManagerShiftSwapRequest request,
        CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var schedule = await db.Schedules
            .Include(schedule => schedule.Container)
            .Include(schedule => schedule.Shop)
            .Include(schedule => schedule.Employees)
                .ThenInclude(employee => employee.Employee)
            .Include(schedule => schedule.Slots)
            .FirstOrDefaultAsync(schedule => schedule.Id == graphId && schedule.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        if (schedule.PublicationStatus != SchedulePublicationStatus.Public)
        {
            throw new ValidationException("Publish this schedule before publishing manual shifts to swap.");
        }

        if (request.ManualColumnId <= 0)
        {
            throw ValidationException.ForField(nameof(request.ManualColumnId), "Choose a manual column shift.");
        }

        var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        if (request.DayOfMonth < 1 || request.DayOfMonth > daysInMonth)
        {
            throw ValidationException.ForField(nameof(request.DayOfMonth), "Choose a valid day in this schedule month.");
        }

        var period = NormalizePeriod(request.FromTime, request.ToTime, nameof(request.FromTime), nameof(request.ToTime));
        var targetEmployeeId = request.TargetEmployeeId is > 0 ? request.TargetEmployeeId.Value : (int?)null;
        if (targetEmployeeId.HasValue)
        {
            var targetExists = await db.Employees
                .AnyAsync(employee => employee.Id == targetEmployeeId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (!targetExists)
            {
                throw ValidationException.ForField(nameof(request.TargetEmployeeId), "The selected employee was not found.");
            }
        }

        var hasOpenManualOffer = await db.ShiftSwapRequests
            .Include(swap => swap.ScheduleSlot)
            .AnyAsync(swap =>
                swap.ScheduleId == graphId &&
                swap.Status == ShiftSwapStatus.Open &&
                swap.ManualColumnId == request.ManualColumnId &&
                swap.ScheduleSlot.DayOfMonth == request.DayOfMonth,
                cancellationToken)
            .ConfigureAwait(false);
        if (hasOpenManualOffer)
        {
            throw ValidationException.ForField(nameof(request.DayOfMonth), "This manual shift already has an open swap offer.");
        }

        var nextSlotNo = schedule.Slots
            .Where(slot =>
                slot.DayOfMonth == request.DayOfMonth &&
                slot.FromTime == period.FromTime &&
                slot.ToTime == period.ToTime)
            .Select(slot => slot.SlotNo)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var slotModel = new ScheduleSlotModel
        {
            ScheduleId = schedule.Id,
            DayOfMonth = request.DayOfMonth,
            SlotNo = nextSlotNo,
            EmployeeId = null,
            Status = SlotStatus.UNFURNISHED,
            FromTime = period.FromTime,
            ToTime = period.ToTime,
        };

        var swapModel = new ShiftSwapRequestModel
        {
            Schedule = schedule,
            ScheduleSlot = slotModel,
            OfferedFromTime = period.FromTime,
            OfferedToTime = period.ToTime,
            FromEmployeeId = null,
            TargetEmployeeId = targetEmployeeId,
            Visibility = targetEmployeeId.HasValue ? ShiftSwapVisibility.Private : ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Open,
            IsManagerCreated = true,
            ManualColumnId = request.ManualColumnId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.ScheduleSlots.Add(slotModel);
        db.ShiftSwapRequests.Add(swapModel);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await db.ShiftSwapRequests
            .Include(swap => swap.Schedule)
                .ThenInclude(item => item.Container)
            .Include(swap => swap.Schedule)
                .ThenInclude(item => item.Shop)
            .Include(swap => swap.ScheduleSlot)
            .Include(swap => swap.TargetEmployee)
            .Include(swap => swap.AcceptedByEmployee)
            .FirstAsync(swap => swap.Id == swapModel.Id, cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(
                User,
                $"Published an open shift from \"{GraphManualColumnLabelResolver.Resolve(schedule.Note, request.ManualColumnId)}\" in schedule \"{schedule.Name}\" at {schedule.Shop?.Name ?? "the assigned shop"} for {schedule.Year}-{schedule.Month:00}-{request.DayOfMonth:00}, {period.FromTime}-{period.ToTime}; offered to {(created.TargetEmployeeId.HasValue ? GetEmployeeName(created.TargetEmployee, created.TargetEmployeeId.Value) : "all eligible employees")}.",
                cancellationToken)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyScheduleChangedAsync(containerId, graphId, "manager-manual-shift-offer-created")
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Schedule, $"{containerId}:{graphId}", "manager-manual-shift-offer-created", containerId, graphId)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, "manager-manual-shift-offer-created", created.Id)
            .ConfigureAwait(false);

        return CreatedAtAction(nameof(GetGraphLog), new { containerId, graphId }, ToLogDto(created));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShiftSwapDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ShiftSwapDto>>> GetGraphLog(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var graphExists = await db.Schedules
            .AnyAsync(schedule => schedule.Id == graphId && schedule.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false);

        if (!graphExists)
        {
            return NotFound(new ProblemDetails
            {
                Type = "not_found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Graph with id {graphId} was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        var openRequests = await db.ShiftSwapRequests
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Container)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Shop)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Slots)
            .Include(request => request.ScheduleSlot)
            .Include(request => request.FromEmployee)
            .Include(request => request.TargetEmployee)
            .Include(request => request.AcceptedByEmployee)
            .Where(request =>
                request.ScheduleId == graphId &&
                request.IsManagerCreated &&
                request.Status == ShiftSwapStatus.Open)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var history = await db.ShiftSwapHistories
            .AsNoTracking()
            .Where(entry => entry.ScheduleId == graphId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = openRequests
            .OrderByDescending(request => request.CreatedAtUtc)
            .Select(ToLogDto)
            .Concat(history.OrderByDescending(entry => entry.AcceptedAtUtc).Select(ToLogDto))
            .ToList();

        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelManualOffer(int containerId, int graphId, int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var swap = await db.ShiftSwapRequests
            .Include(request => request.Schedule)
            .Include(request => request.ScheduleSlot)
            .FirstOrDefaultAsync(request =>
                request.Id == id &&
                request.ScheduleId == graphId &&
                request.Schedule.ContainerId == containerId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift swap with id {id} was not found.");

        if (!swap.IsManagerCreated)
        {
            throw new ValidationException("Only manager-created manual shift offers can be cancelled here.");
        }

        if (swap.Status != ShiftSwapStatus.Open)
        {
            throw new ValidationException("Only open manual shift offers can be cancelled.");
        }

        db.ShiftSwapRequests.Remove(swap);
        if (swap.ScheduleSlot.EmployeeId is null)
        {
            db.ScheduleSlots.Remove(swap.ScheduleSlot);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await workflowLogService
            .LogAsync(
                User,
                $"Cancelled the open shift in schedule \"{swap.Schedule.Name}\" for {swap.Schedule.Year}-{swap.Schedule.Month:00}-{swap.ScheduleSlot.DayOfMonth:00}, {swap.OfferedFromTime ?? swap.ScheduleSlot.FromTime}-{swap.OfferedToTime ?? swap.ScheduleSlot.ToTime}.",
                cancellationToken)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyScheduleChangedAsync(containerId, graphId, "manager-manual-shift-offer-cancelled")
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Schedule, $"{containerId}:{graphId}", "manager-manual-shift-offer-cancelled", containerId, graphId)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, "manager-manual-shift-offer-cancelled", swap.Id)
            .ConfigureAwait(false);

        return NoContent();
    }

    private ActionResult? CreateEditLockConflictResult(int containerId, int graphId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.Schedule(containerId, graphId),
            "This schedule");

    private static ShiftSwapDto ToLogDto(ShiftSwapRequestModel model)
    {
        var slot = model.ScheduleSlot;
        var fromTime = string.IsNullOrWhiteSpace(model.OfferedFromTime) ? slot.FromTime : model.OfferedFromTime;
        var toTime = string.IsNullOrWhiteSpace(model.OfferedToTime) ? slot.ToTime : model.OfferedToTime;
        var shiftHours = Math.Round(GetTimeRangeDurationHours(fromTime!, toTime!), 2);

        var manualColumnName = model.IsManagerCreated
            ? GraphManualColumnLabelResolver.Resolve(model.Schedule.Note, model.ManualColumnId)
            : null;

        return new ShiftSwapDto
        {
            Id = model.Id,
            ScheduleId = model.ScheduleId,
            ScheduleSlotId = model.ScheduleSlotId,
            ScheduleName = model.Schedule.Name,
            ContainerName = model.Schedule.Container?.Name ?? string.Empty,
            ShopName = model.Schedule.Shop?.Name ?? string.Empty,
            Year = model.Schedule.Year,
            Month = model.Schedule.Month,
            DayOfMonth = slot.DayOfMonth,
            FromTime = fromTime!,
            ToTime = toTime!,
            FromEmployeeId = model.FromEmployeeId,
            FromEmployeeName = manualColumnName ?? GetEmployeeName(model.FromEmployee, model.FromEmployeeId),
            TargetEmployeeId = model.TargetEmployeeId,
            TargetEmployeeName = model.TargetEmployeeId.HasValue ? GetEmployeeName(model.TargetEmployee, model.TargetEmployeeId.Value) : null,
            AcceptedByEmployeeId = model.AcceptedByEmployeeId,
            AcceptedByEmployeeName = model.AcceptedByEmployeeId.HasValue
                ? GetEmployeeName(model.AcceptedByEmployee, model.AcceptedByEmployeeId.Value)
                : null,
            Visibility = model.Visibility == ShiftSwapVisibility.Private ? "private" : "public",
            Status = model.Status switch
            {
                ShiftSwapStatus.Accepted => "accepted",
                ShiftSwapStatus.Cancelled => "cancelled",
                _ => "open",
            },
            CreatedAtUtc = model.CreatedAtUtc,
            AcceptedAtUtc = model.AcceptedAtUtc,
            ShiftHours = shiftHours,
            CurrentEmployeeHoursBefore = 0,
            CurrentEmployeeHoursAfter = 0,
            FromEmployeeHoursBefore = 0,
            FromEmployeeHoursAfter = 0,
            IsManagerCreated = model.IsManagerCreated,
            ManualColumnId = model.ManualColumnId,
            ManualColumnName = manualColumnName,
            IsCreatedByCurrentEmployee = false,
            CanAccept = false,
            CanCancel = model.IsManagerCreated && model.Status == ShiftSwapStatus.Open,
        };
    }

    private static ShiftSwapDto ToLogDto(ShiftSwapHistoryModel history)
    {
        var shiftHours = Math.Round(GetTimeRangeDurationHours(history.FromTime, history.ToTime), 2);

        return new ShiftSwapDto
        {
            Id = history.SourceShiftSwapRequestId,
            ScheduleId = history.ScheduleId,
            ScheduleSlotId = history.ScheduleSlotId,
            ScheduleName = history.ScheduleName,
            ContainerName = history.ContainerName,
            ShopName = history.ShopName,
            Year = history.Year,
            Month = history.Month,
            DayOfMonth = history.DayOfMonth,
            FromTime = history.FromTime,
            ToTime = history.ToTime,
            FromEmployeeId = history.FromEmployeeId,
            FromEmployeeName = history.FromEmployeeName,
            TargetEmployeeId = history.TargetEmployeeId,
            TargetEmployeeName = history.TargetEmployeeName,
            AcceptedByEmployeeId = history.AcceptedByEmployeeId,
            AcceptedByEmployeeName = history.AcceptedByEmployeeName,
            Visibility = history.TargetEmployeeId.HasValue ? "private" : "public",
            Status = "accepted",
            CreatedAtUtc = history.CreatedAtUtc,
            AcceptedAtUtc = history.AcceptedAtUtc,
            ShiftHours = shiftHours,
            IsManagerCreated = history.IsManagerCreated,
            ManualColumnId = history.ManualColumnId,
            ManualColumnName = history.ManualColumnName,
            BeforeSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.BeforeSnapshotJson),
            AfterSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.AfterSnapshotJson),
            IsCreatedByCurrentEmployee = false,
            CanAccept = false,
            CanCancel = false,
        };
    }

    private static string GetEmployeeName(EmployeeModel? employee, int? employeeId)
    {
        var fullName = $"{employee?.FirstName} {employee?.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return employeeId.HasValue ? $"Employee #{employeeId.Value}" : "Manual column";
    }

    private static double GetSlotDurationHours(ScheduleSlotModel slot)
        => GetTimeRangeDurationHours(slot.FromTime, slot.ToTime);

    private readonly record struct ShiftSwapPeriod(string FromTime, string ToTime);

    private static ShiftSwapPeriod NormalizePeriod(string fromTime, string toTime, string fromFieldName, string toFieldName)
    {
        var normalizedFrom = NormalizeTimeText(fromTime, fromFieldName);
        var normalizedTo = NormalizeTimeText(toTime, toFieldName);
        var normalizedFromMinutes = ParseTimeMinutes(normalizedFrom)!.Value;
        var normalizedToMinutes = ParseTimeMinutes(normalizedTo)!.Value;

        if (normalizedToMinutes <= normalizedFromMinutes)
        {
            throw ValidationException.ForField(toFieldName, "The end time must be after the start time.");
        }

        return new ShiftSwapPeriod(normalizedFrom, normalizedTo);
    }

    private static string NormalizeTimeText(string value, string fieldName)
    {
        var minutes = ParseTimeMinutes(value);
        if (minutes is null)
        {
            throw ValidationException.ForField(fieldName, "Use HH:mm time format.");
        }

        return $"{minutes.Value / 60:00}:{minutes.Value % 60:00}";
    }

    private static double GetTimeRangeDurationHours(string fromTime, string toTime)
    {
        var fromMinutes = ParseTimeMinutes(fromTime);
        var toMinutes = ParseTimeMinutes(toTime);
        if (fromMinutes is null || toMinutes is null)
        {
            return 0;
        }

        var duration = toMinutes.Value - fromMinutes.Value;
        if (duration <= 0)
        {
            return 0;
        }

        return duration / 60d;
    }

    private static int? ParseTimeMinutes(string value)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minute))
        {
            return null;
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
        {
            return null;
        }

        return hour * 60 + minute;
    }
}
