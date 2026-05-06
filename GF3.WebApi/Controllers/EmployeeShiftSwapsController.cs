using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
[Route("api/employee-shift-swaps")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeShiftSwapsController(
    AppDbContext db,
    IScheduleEditLockService scheduleEditLockService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier) : ControllerBase
{
    private static readonly Regex GraphNoteMetaRegex = new(
        @"(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:([\s\S]*?)-->|\[\[GF3_GRAPH_META:([\s\S]*?)\]\])$",
        RegexOptions.Compiled);

    [HttpGet("employees")]
    [ProducesResponseType(typeof(IEnumerable<ShiftSwapEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ShiftSwapEmployeeDto>>> GetEmployees(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var employees = await db.Employees
            .AsNoTracking()
            .Where(employee => employee.Id != employeeId)
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.LastName)
            .Select(employee => new ShiftSwapEmployeeDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DisplayName = employee.FirstName + " " + employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(employees);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ShiftSwapDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ShiftSwapDto>>> GetVisible(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var requests = await BuildSwapRequestQuery()
            .Where(request =>
                request.FromEmployeeId == employeeId ||
                request.TargetEmployeeId == employeeId ||
                request.AcceptedByEmployeeId == employeeId ||
                (request.Visibility == ShiftSwapVisibility.Public &&
                 (request.IsManagerCreated ||
                  (request.FromEmployeeId != employeeId &&
                   request.Schedule.Employees.Any(employee => employee.EmployeeId == employeeId)))))
            .Where(request => request.Status == ShiftSwapStatus.Open ||
                              request.AcceptedByEmployeeId == employeeId ||
                              request.FromEmployeeId == employeeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var orderedRequests = requests
            .OrderBy(request => request.Status == ShiftSwapStatus.Open ? 0 : 1)
            .ThenByDescending(request => request.CreatedAtUtc);

        return Ok(orderedRequests.Select(request =>
            ToDto(request, employeeId, scheduleEditLockService.IsLocked(request.Schedule.ContainerId, request.ScheduleId))));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShiftSwapDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ShiftSwapDto>> Create([FromBody] CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var schedule = await LoadEmployeePublishedScheduleAsync(request.ScheduleId, employeeId, cancellationToken)
            .ConfigureAwait(false);
        EnsureScheduleIsNotLocked(schedule);

        var slot = schedule.Slots.FirstOrDefault(slot => slot.Id == request.ScheduleSlotId)
            ?? throw ValidationException.ForField(nameof(request.ScheduleSlotId), "Shift was not found in this schedule.");

        if (slot.EmployeeId != employeeId)
        {
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "You can only offer your own assigned shift.");
        }

        var offeredPeriod = ResolveRequestedPeriod(request, slot);
        var targetEmployeeId = request.TargetEmployeeId is > 0 ? request.TargetEmployeeId.Value : (int?)null;
        if (targetEmployeeId == employeeId)
        {
            throw ValidationException.ForField(nameof(request.TargetEmployeeId), "Choose another employee or publish this swap for everyone.");
        }

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

        var hasOpenRequest = await db.ShiftSwapRequests
            .AnyAsync(swap => swap.ScheduleSlotId == slot.Id && swap.Status == ShiftSwapStatus.Open, cancellationToken)
            .ConfigureAwait(false);
        if (hasOpenRequest)
        {
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "This shift already has an open swap offer.");
        }

        var model = new ShiftSwapRequestModel
        {
            ScheduleId = schedule.Id,
            ScheduleSlotId = slot.Id,
            OfferedFromTime = offeredPeriod.FromTime,
            OfferedToTime = offeredPeriod.ToTime,
            FromEmployeeId = employeeId,
            TargetEmployeeId = targetEmployeeId,
            Visibility = targetEmployeeId.HasValue ? ShiftSwapVisibility.Private : ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.ShiftSwapRequests.Add(model);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await BuildSwapRequestQuery()
            .FirstAsync(swap => swap.Id == model.Id, cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(User, $"Created swap offer for {created.Schedule.Name} on day {slot.DayOfMonth} ({offeredPeriod.FromTime}-{offeredPeriod.ToTime}).", cancellationToken)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyShiftSwapsChangedAsync(created.Schedule.ContainerId, created.ScheduleId, created.ScheduleId, "employee-swap-created")
            .ConfigureAwait(false);

        return CreatedAtAction(nameof(GetVisible), new { id = created.Id }, ToDto(created, employeeId, isScheduleLocked: false));
    }

    [HttpPost("{id:int}/accept")]
    [ProducesResponseType(typeof(ShiftSwapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftSwapDto>> Accept(int id, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var swap = await BuildSwapRequestQuery()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift swap with id {id} was not found.");

        EnsureScheduleIsNotLocked(swap.Schedule);
        EnsureCanAccept(swap, employeeId);
        var slot = swap.Schedule.Slots.First(slot => slot.Id == swap.ScheduleSlotId);
        var offeredPeriod = GetSwapPeriod(swap, slot);
        EnsurePeriodWithinSlot(slot, offeredPeriod);

        if (swap.IsManagerCreated)
        {
            if (slot.EmployeeId.HasValue)
            {
                throw new ValidationException("This open shift is no longer available.");
            }
        }
        else if (slot.EmployeeId != swap.FromEmployeeId)
        {
            throw new ValidationException("This shift is no longer assigned to the employee who opened the swap.");
        }

        if (HasOverlappingShift(swap.Schedule.Slots, slot.DayOfMonth, offeredPeriod.FromTime, offeredPeriod.ToTime, employeeId, slot.Id))
        {
            throw new ValidationException("You already have a shift that overlaps this time.");
        }

        if (swap.IsManagerCreated)
        {
            slot.EmployeeId = employeeId;
            slot.Status = SlotStatus.ASSIGNED;
            if (swap.ManualColumnId.HasValue)
            {
                swap.Schedule.Note = RemoveManualColumnCellFromNote(
                    swap.Schedule.Note,
                    swap.ManualColumnId.Value,
                    slot.DayOfMonth);
            }
        }
        else
        {
            foreach (var remainingSlot in ApplyAcceptedSwapPeriod(slot, swap.FromEmployeeId!.Value, employeeId, offeredPeriod))
            {
                db.ScheduleSlots.Add(remainingSlot);
            }
        }

        EnsureScheduleEmployee(swap.Schedule, employeeId);
        swap.Status = ShiftSwapStatus.Accepted;
        swap.AcceptedByEmployeeId = employeeId;
        swap.AcceptedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException("This swap could not be accepted because the resulting schedule would conflict.");
        }

        var accepted = await BuildSwapRequestQuery()
            .FirstAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(User, $"Accepted swap for {accepted.Schedule.Name} on day {accepted.ScheduleSlot.DayOfMonth}.", cancellationToken)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyScheduleChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, "employee-swap-accepted")
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyShiftSwapsChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, accepted.ScheduleId, "employee-swap-accepted")
            .ConfigureAwait(false);

        return Ok(ToDto(accepted, employeeId, isScheduleLocked: false));
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ShiftSwapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftSwapDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var swap = await BuildSwapRequestQuery()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift swap with id {id} was not found.");

        if (swap.FromEmployeeId != employeeId)
        {
            throw new ValidationException("Only the employee who opened this swap can cancel it.");
        }

        if (swap.Status != ShiftSwapStatus.Open)
        {
            throw new ValidationException("Only open swap offers can be cancelled.");
        }

        swap.Status = ShiftSwapStatus.Cancelled;
        swap.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await workflowLogService
            .LogAsync(User, $"Cancelled swap offer for {swap.Schedule.Name} on day {swap.ScheduleSlot.DayOfMonth}.", cancellationToken)
            .ConfigureAwait(false);
        await realtimeNotifier
            .NotifyShiftSwapsChangedAsync(swap.Schedule.ContainerId, swap.ScheduleId, swap.ScheduleId, "employee-swap-cancelled")
            .ConfigureAwait(false);

        return Ok(ToDto(swap, employeeId, scheduleEditLockService.IsLocked(swap.Schedule.ContainerId, swap.ScheduleId)));
    }

    private IQueryable<ShiftSwapRequestModel> BuildSwapRequestQuery()
        => db.ShiftSwapRequests
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Container)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Shop)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Employees)
                    .ThenInclude(employee => employee.Employee)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Slots)
            .Include(request => request.ScheduleSlot)
            .Include(request => request.FromEmployee)
            .Include(request => request.TargetEmployee)
            .Include(request => request.AcceptedByEmployee);

    private async Task<ScheduleModel> LoadEmployeePublishedScheduleAsync(int scheduleId, int employeeId, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules
            .Include(schedule => schedule.Employees)
                .ThenInclude(employee => employee.Employee)
            .Include(schedule => schedule.Slots)
            .FirstOrDefaultAsync(schedule =>
                schedule.Id == scheduleId &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken)
            .ConfigureAwait(false);

        return schedule ?? throw ValidationException.ForField(nameof(scheduleId), "Published schedule was not found for your account.");
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

    private static void EnsureCanAccept(ShiftSwapRequestModel swap, int employeeId)
    {
        if (swap.Status != ShiftSwapStatus.Open)
        {
            throw new ValidationException("This swap offer is no longer open.");
        }

        if (swap.FromEmployeeId == employeeId)
        {
            throw new ValidationException("You cannot accept your own swap offer.");
        }

        if (swap.TargetEmployeeId.HasValue && swap.TargetEmployeeId.Value != employeeId)
        {
            throw new ValidationException("This private swap offer was sent to another employee.");
        }

        if (swap.Visibility == ShiftSwapVisibility.Public &&
            !swap.IsManagerCreated &&
            !swap.Schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
        {
            throw new ValidationException("You are not assigned to this schedule.");
        }
    }

    private static void EnsureScheduleEmployee(ScheduleModel schedule, int employeeId)
    {
        if (schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
        {
            return;
        }

        var nextDisplayOrder = schedule.Employees.Count == 0
            ? 0
            : schedule.Employees.Max(employee => employee.DisplayOrder) + 1;
        schedule.Employees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employeeId,
            DisplayOrder = nextDisplayOrder,
        });
    }

    private void EnsureScheduleIsNotLocked(ScheduleModel schedule)
    {
        if (scheduleEditLockService.IsLocked(schedule.ContainerId, schedule.Id))
        {
            throw new ValidationException("This schedule is being edited by a manager. Try again after the manager saves changes.");
        }
    }

    private static ShiftSwapDto ToDto(ShiftSwapRequestModel model, int currentEmployeeId, bool isScheduleLocked)
    {
        var slot = model.Schedule.Slots.FirstOrDefault(slot => slot.Id == model.ScheduleSlotId) ?? model.ScheduleSlot;
        var offeredPeriod = GetSwapPeriod(model, slot);
        var shiftHours = GetTimeRangeDurationHours(offeredPeriod.FromTime, offeredPeriod.ToTime);
        var currentHoursBefore = GetEmployeeHours(model.Schedule.Slots, currentEmployeeId);
        var fromHoursBefore = model.FromEmployeeId.HasValue ? GetEmployeeHours(model.Schedule.Slots, model.FromEmployeeId.Value) : 0;
        var isOpen = model.Status == ShiftSwapStatus.Open;
        var isOwner = model.FromEmployeeId == currentEmployeeId;
        var canAccept = isOpen &&
                        !isScheduleLocked &&
                        !isOwner &&
                        (!model.TargetEmployeeId.HasValue || model.TargetEmployeeId.Value == currentEmployeeId) &&
                        (model.Visibility == ShiftSwapVisibility.Private ||
                         model.IsManagerCreated ||
                         model.Schedule.Employees.Any(employee => employee.EmployeeId == currentEmployeeId)) &&
                        !HasOverlappingShift(model.Schedule.Slots, slot.DayOfMonth, offeredPeriod.FromTime, offeredPeriod.ToTime, currentEmployeeId, slot.Id);
        var currentHoursAfter = currentHoursBefore;
        var fromHoursAfter = fromHoursBefore;

        if (isOpen)
        {
            if (isOwner)
            {
                currentHoursAfter = Math.Max(0, currentHoursBefore - shiftHours);
                fromHoursAfter = currentHoursAfter;
            }
            else if (canAccept)
            {
                currentHoursAfter = currentHoursBefore + shiftHours;
                fromHoursAfter = model.IsManagerCreated ? fromHoursBefore : Math.Max(0, fromHoursBefore - shiftHours);
            }
        }
        else if (model.Status == ShiftSwapStatus.Accepted)
        {
            currentHoursAfter = currentHoursBefore;
            fromHoursAfter = fromHoursBefore;
        }

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
            FromTime = offeredPeriod.FromTime,
            ToTime = offeredPeriod.ToTime,
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
            ShiftHours = Math.Round(shiftHours, 2),
            CurrentEmployeeHoursBefore = Math.Round(currentHoursBefore, 2),
            CurrentEmployeeHoursAfter = Math.Round(currentHoursAfter, 2),
            FromEmployeeHoursBefore = Math.Round(fromHoursBefore, 2),
            FromEmployeeHoursAfter = Math.Round(fromHoursAfter, 2),
            IsManagerCreated = model.IsManagerCreated,
            ManualColumnId = model.ManualColumnId,
            ManualColumnName = manualColumnName,
            IsCreatedByCurrentEmployee = isOwner,
            CanAccept = canAccept,
            CanCancel = isOwner && isOpen,
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

    private static double GetEmployeeHours(IEnumerable<ScheduleSlotModel> slots, int employeeId)
        => slots
            .Where(slot => slot.EmployeeId == employeeId)
            .Sum(GetSlotDurationHours);

    private readonly record struct ShiftSwapPeriod(string FromTime, string ToTime);

    private static ShiftSwapPeriod ResolveRequestedPeriod(CreateEmployeeShiftSwapRequest request, ScheduleSlotModel slot)
    {
        var fromTime = string.IsNullOrWhiteSpace(request.FromTime) ? slot.FromTime : request.FromTime;
        var toTime = string.IsNullOrWhiteSpace(request.ToTime) ? slot.ToTime : request.ToTime;
        var period = NormalizePeriod(fromTime!, toTime!, nameof(request.FromTime), nameof(request.ToTime));
        EnsurePeriodWithinSlot(slot, period);

        return period;
    }

    private static ShiftSwapPeriod GetSwapPeriod(ShiftSwapRequestModel model, ScheduleSlotModel slot)
    {
        var fromTime = string.IsNullOrWhiteSpace(model.OfferedFromTime) ? slot.FromTime : model.OfferedFromTime;
        var toTime = string.IsNullOrWhiteSpace(model.OfferedToTime) ? slot.ToTime : model.OfferedToTime;

        return NormalizePeriod(fromTime!, toTime!, nameof(model.OfferedFromTime), nameof(model.OfferedToTime));
    }

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

    private static void EnsurePeriodWithinSlot(ScheduleSlotModel slot, ShiftSwapPeriod period)
    {
        var slotFrom = ParseTimeMinutes(slot.FromTime);
        var slotTo = ParseTimeMinutes(slot.ToTime);
        var periodFrom = ParseTimeMinutes(period.FromTime);
        var periodTo = ParseTimeMinutes(period.ToTime);
        if (slotFrom is null || slotTo is null || periodFrom is null || periodTo is null || slotTo <= slotFrom)
        {
            throw new ValidationException("The selected shift has an invalid time range.");
        }

        if (periodFrom < slotFrom || periodTo > slotTo)
        {
            throw new ValidationException("The offered period must stay inside the selected shift.");
        }
    }

    private static IReadOnlyList<ScheduleSlotModel> ApplyAcceptedSwapPeriod(
        ScheduleSlotModel slot,
        int originalEmployeeId,
        int acceptingEmployeeId,
        ShiftSwapPeriod period)
    {
        var remainingSlots = new List<ScheduleSlotModel>();
        var originalFrom = slot.FromTime;
        var originalTo = slot.ToTime;
        var originalFromMinutes = ParseTimeMinutes(originalFrom)!.Value;
        var originalToMinutes = ParseTimeMinutes(originalTo)!.Value;
        var periodFromMinutes = ParseTimeMinutes(period.FromTime)!.Value;
        var periodToMinutes = ParseTimeMinutes(period.ToTime)!.Value;

        if (periodFromMinutes > originalFromMinutes)
        {
            remainingSlots.Add(CreateRemainingSlot(slot, originalEmployeeId, originalFrom, period.FromTime));
        }

        if (periodToMinutes < originalToMinutes)
        {
            remainingSlots.Add(CreateRemainingSlot(slot, originalEmployeeId, period.ToTime, originalTo));
        }

        slot.FromTime = period.FromTime;
        slot.ToTime = period.ToTime;
        slot.EmployeeId = acceptingEmployeeId;
        slot.Status = SlotStatus.ASSIGNED;

        return remainingSlots;
    }

    private static ScheduleSlotModel CreateRemainingSlot(ScheduleSlotModel sourceSlot, int employeeId, string fromTime, string toTime)
        => new()
        {
            ScheduleId = sourceSlot.ScheduleId,
            DayOfMonth = sourceSlot.DayOfMonth,
            SlotNo = sourceSlot.SlotNo,
            EmployeeId = employeeId,
            Status = SlotStatus.ASSIGNED,
            FromTime = fromTime,
            ToTime = toTime,
        };

    private static double GetSlotDurationHours(ScheduleSlotModel slot)
        => GetTimeRangeDurationHours(slot.FromTime, slot.ToTime);

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

    private static bool HasOverlappingShift(
        IEnumerable<ScheduleSlotModel> slots,
        int dayOfMonth,
        string fromTime,
        string toTime,
        int employeeId,
        int excludedSlotId)
        => slots.Any(slot =>
            slot.Id != excludedSlotId &&
            slot.EmployeeId == employeeId &&
            slot.DayOfMonth == dayOfMonth &&
            TimesOverlap(slot.FromTime, slot.ToTime, fromTime, toTime));

    private static bool TimesOverlap(string leftFrom, string leftTo, string rightFrom, string rightTo)
    {
        var leftStart = ParseTimeMinutes(leftFrom);
        var leftEnd = ParseTimeMinutes(leftTo);
        var rightStart = ParseTimeMinutes(rightFrom);
        var rightEnd = ParseTimeMinutes(rightTo);
        if (leftStart is null || leftEnd is null || rightStart is null || rightEnd is null)
        {
            return false;
        }

        return leftStart.Value < rightEnd.Value && rightStart.Value < leftEnd.Value;
    }

    private static string? RemoveManualColumnCellFromNote(string? rawNote, int manualColumnId, int dayOfMonth)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return rawNote;
        }

        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success || match.Index < 0)
        {
            return rawNote;
        }

        var rawMeta = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
        var meta = ParseGraphNoteMeta(rawMeta);
        if (meta is null)
        {
            return rawNote;
        }

        var changed =
            RemoveCompactManualColumnCell(meta["m"] as JsonArray, manualColumnId, dayOfMonth) ||
            RemoveLegacyManualColumnCell(meta["manualColumns"] as JsonArray, manualColumnId, dayOfMonth);

        if (!changed)
        {
            return rawNote;
        }

        var visibleNote = rawNote[..match.Index].TrimEnd();
        var encodedMeta = $"b64:{EncodeGraphNoteMetaValue(meta.ToJsonString())}";
        var suffix = $"[[GF3_GRAPH_META:{encodedMeta}]]";

        return string.IsNullOrEmpty(visibleNote) ? suffix : $"{visibleNote}\n\n{suffix}";
    }

    private static JsonObject? ParseGraphNoteMeta(string rawMeta)
    {
        var payload = rawMeta.Trim();
        if (payload.Length == 0)
        {
            return null;
        }

        if (payload.StartsWith("b64:", StringComparison.Ordinal))
        {
            return TryParseJsonObject(DecodeGraphNoteMetaValue(payload[4..]));
        }

        return TryParseJsonObject(payload) ?? TryParseJsonObject(Uri.UnescapeDataString(payload));
    }

    private static JsonObject? TryParseJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(value) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string DecodeGraphNoteMetaValue(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight((int)Math.Ceiling(value.Length / 4d) * 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    private static string EncodeGraphNoteMetaValue(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static bool RemoveCompactManualColumnCell(JsonArray? manualColumns, int manualColumnId, int dayOfMonth)
    {
        if (manualColumns is null)
        {
            return false;
        }

        var dayKey = dayOfMonth.ToString(CultureInfo.InvariantCulture);
        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonArray column || GetJsonInt(column.ElementAtOrDefault(0)) != manualColumnId)
            {
                continue;
            }

            if (column.Count < 3 || column[2] is not JsonObject cells || !cells.Remove(dayKey))
            {
                return false;
            }

            if (cells.Count == 0)
            {
                column.RemoveAt(2);
            }

            return true;
        }

        return false;
    }

    private static bool RemoveLegacyManualColumnCell(JsonArray? manualColumns, int manualColumnId, int dayOfMonth)
    {
        if (manualColumns is null)
        {
            return false;
        }

        var dayKey = dayOfMonth.ToString(CultureInfo.InvariantCulture);
        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonObject column || GetJsonInt(column["id"]) != manualColumnId)
            {
                continue;
            }

            return column["cells"] is JsonObject cells && cells.Remove(dayKey);
        }

        return false;
    }

    private static int? GetJsonInt(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
