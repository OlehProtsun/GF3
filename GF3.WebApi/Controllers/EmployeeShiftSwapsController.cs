using static WebApi.ShiftSwaps.ShiftSwapRules;
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
        var sharedContainerIds = db.Schedules
            .Where(schedule =>
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
            .Select(schedule => schedule.ContainerId);
        var requests = await BuildSwapRequestQuery()
            .Where(request =>
                (request.ArchivedViewsJson != null || request.Schedule.PublicationStatus == SchedulePublicationStatus.Public) &&
                (request.FromEmployeeId == employeeId ||
                 request.TargetEmployeeId == employeeId ||
                 request.AcceptedByEmployeeId == employeeId ||
                 (request.Visibility == ShiftSwapVisibility.Public &&
                  sharedContainerIds.Contains(request.Schedule.ContainerId))))
            .Where(request => request.Status == ShiftSwapStatus.Open ||
                              request.AcceptedByEmployeeId == employeeId ||
                              request.FromEmployeeId == employeeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var years = requests.Select(request => request.Schedule.Year).Distinct().ToList();
        var months = requests.Select(request => request.Schedule.Month).Distinct().ToList();
        IReadOnlyList<ScheduleSlotModel> employeeSlots = requests.Count == 0
            ? []
            : await db.ScheduleSlots
                .AsNoTracking()
                .Include(slot => slot.Schedule)
                .Where(slot =>
                    slot.EmployeeId == employeeId &&
                    slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                    years.Contains(slot.Schedule.Year) &&
                    months.Contains(slot.Schedule.Month))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        var orderedRequests = requests
            .OrderBy(request => request.Status == ShiftSwapStatus.Open ? 0 : 1)
            .ThenByDescending(request => request.CreatedAtUtc)
            .ToList();

        return Ok(orderedRequests.Select(request =>
            ToDto(
                request,
                employeeId,
                scheduleEditLockService.IsLocked(request.Schedule.ContainerId, request.ScheduleId),
                GetPeriodSlots(employeeSlots, request.Schedule.Year, request.Schedule.Month))));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShiftSwapDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ShiftSwapDto>> Create([FromBody] CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var schedule = await LoadEmployeePublishedScheduleAsync(request.ScheduleId, employeeId, cancellationToken)
            .ConfigureAwait(false);
        EnsureSwapAllowed(schedule);
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

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(
                User,
                $"Published a shift swap from schedule \"{created.Schedule.Name}\" at {created.Schedule.Shop?.Name ?? "the assigned shop"} for {created.Schedule.Year}-{created.Schedule.Month:00}-{slot.DayOfMonth:00}, {offeredPeriod.FromTime}-{offeredPeriod.ToTime}; offered to {(created.TargetEmployeeId.HasValue ? GetEmployeeName(created.TargetEmployee, created.TargetEmployeeId.Value) : "all eligible employees") }.",
                cancellationToken)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(created.Schedule.ContainerId, created.ScheduleId, created.ScheduleId, "employee-swap-created", created.Id)).ConfigureAwait(false);

        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            created.Schedule.Year,
            created.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetVisible),
            new { id = created.Id },
            ToDto(created, employeeId, isScheduleLocked: false, employeeMonthSlots));
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

        if (swap.Schedule.PublicationStatus != SchedulePublicationStatus.Public)
        {
            throw new ValidationException("This swap belongs to a schedule that is no longer public.");
        }

        if (swap.Status != ShiftSwapStatus.Open)
            throw new ValidationException("This swap offer is no longer open.");
        EnsureSwapAllowed(swap.Schedule);

        if (swap.Visibility == ShiftSwapVisibility.Public)
        {
            var sharesContainer = await db.Schedules.AnyAsync(schedule =>
                schedule.ContainerId == swap.Schedule.ContainerId &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken).ConfigureAwait(false);
            if (!sharesContainer)
            {
                throw new ValidationException("This swap belongs to another container.");
            }
        }

        EnsureScheduleIsNotLocked(swap.Schedule);
        var slot = swap.Schedule.Slots.First(slot => slot.Id == swap.ScheduleSlotId);
        var offeredPeriod = GetSwapPeriod(swap, slot);
        EnsurePeriodWithinSlot(slot, offeredPeriod);
        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            swap.Schedule.Year,
            swap.Schedule.Month,
            cancellationToken).ConfigureAwait(false);
        var acceptanceUnavailableReason = GetAcceptanceUnavailableReason(
            swap,
            employeeId,
            isScheduleLocked: false,
            employeeMonthSlots,
            slot,
            offeredPeriod);
        if (acceptanceUnavailableReason is not null)
        {
            throw new ValidationException(acceptanceUnavailableReason);
        }
        var acceptingEmployee = await db.Employees
            .AsNoTracking()
            .FirstAsync(employee => employee.Id == employeeId, cancellationToken)
            .ConfigureAwait(false);
        var acceptingEmployeeName = GetEmployeeName(acceptingEmployee, employeeId);
        var manualColumnName = swap.IsManagerCreated
            ? GraphManualColumnLabelResolver.Resolve(swap.Schedule.Note, swap.ManualColumnId)
            : null;
        var beforeSnapshotJson = ShiftSwapHistorySnapshotBuilder.BuildJson(
            swap.Schedule,
            swap.Schedule.Slots,
            swap,
            employeeId,
            acceptingEmployeeName);

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
        await HighlightAcceptedSwapCellsAsync(swap, employeeId, cancellationToken).ConfigureAwait(false);
        swap.Status = ShiftSwapStatus.Accepted;
        swap.AcceptedByEmployeeId = employeeId;
        var acceptedAtUtc = DateTimeOffset.UtcNow;
        swap.AcceptedAtUtc = acceptedAtUtc;

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var afterSlots = await db.ScheduleSlots
                .AsNoTracking()
                .Where(scheduleSlot => scheduleSlot.ScheduleId == swap.ScheduleId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var afterSnapshotJson = ShiftSwapHistorySnapshotBuilder.BuildJson(
                swap.Schedule,
                afterSlots,
                swap,
                employeeId,
                acceptingEmployeeName);

            db.ShiftSwapHistories.Add(new ShiftSwapHistoryModel
            {
                SourceShiftSwapRequestId = swap.Id,
                ScheduleId = swap.ScheduleId,
                ScheduleSlotId = slot.Id,
                ScheduleName = swap.Schedule.Name,
                ContainerName = swap.Schedule.Container?.Name ?? string.Empty,
                ShopName = swap.Schedule.Shop?.Name ?? string.Empty,
                Year = swap.Schedule.Year,
                Month = swap.Schedule.Month,
                DayOfMonth = slot.DayOfMonth,
                FromTime = offeredPeriod.FromTime,
                ToTime = offeredPeriod.ToTime,
                FromEmployeeId = swap.FromEmployeeId,
                FromEmployeeName = manualColumnName ?? GetEmployeeName(swap.FromEmployee, swap.FromEmployeeId),
                TargetEmployeeId = swap.TargetEmployeeId,
                TargetEmployeeName = swap.TargetEmployeeId.HasValue
                    ? GetEmployeeName(swap.TargetEmployee, swap.TargetEmployeeId.Value)
                    : null,
                AcceptedByEmployeeId = employeeId,
                AcceptedByEmployeeName = acceptingEmployeeName,
                CreatedAtUtc = swap.CreatedAtUtc,
                AcceptedAtUtc = acceptedAtUtc,
                IsManagerCreated = swap.IsManagerCreated,
                ManualColumnId = swap.ManualColumnId,
                ManualColumnName = manualColumnName,
                BeforeSnapshotJson = beforeSnapshotJson,
                AfterSnapshotJson = afterSnapshotJson,
            });
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

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(
                User,
                $"Accepted {(accepted.FromEmployeeId.HasValue ? $"{GetEmployeeName(accepted.FromEmployee, accepted.FromEmployeeId.Value)}'s shift" : "an open shift")} from schedule \"{accepted.Schedule.Name}\" for {accepted.Schedule.Year}-{accepted.Schedule.Month:00}-{accepted.ScheduleSlot!.DayOfMonth:00}, {offeredPeriod.FromTime}-{offeredPeriod.ToTime}.",
                cancellationToken)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier
            .NotifyScheduleChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, "employee-swap-accepted")).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, accepted.ScheduleId, "employee-swap-accepted", accepted.Id)).ConfigureAwait(false);

        var updatedEmployeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            accepted.Schedule.Year,
            accepted.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return Ok(ToDto(accepted, employeeId, isScheduleLocked: false, updatedEmployeeMonthSlots));
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

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(
                User,
                $"Cancelled the shift swap from schedule \"{swap.Schedule.Name}\" for {swap.Schedule.Year}-{swap.Schedule.Month:00}-{swap.ScheduleSlot!.DayOfMonth:00}, {swap.OfferedFromTime ?? swap.ScheduleSlot!.FromTime}-{swap.OfferedToTime ?? swap.ScheduleSlot!.ToTime}.",
                cancellationToken)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(swap.Schedule.ContainerId, swap.ScheduleId, swap.ScheduleId, "employee-swap-cancelled", swap.Id)).ConfigureAwait(false);

        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            swap.Schedule.Year,
            swap.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return Ok(ToDto(
            swap,
            employeeId,
            scheduleEditLockService.IsLocked(swap.Schedule.ContainerId, swap.ScheduleId),
            employeeMonthSlots));
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

    private async Task HighlightAcceptedSwapCellsAsync(
        ShiftSwapRequestModel swap,
        int acceptingEmployeeId,
        CancellationToken cancellationToken)
    {
        var highlightColorArgb = ShiftSwapHighlightRules.ToArgb(swap.Schedule.AcceptedSwapHighlightColor);
        var affectedEmployeeIds = new HashSet<int> { acceptingEmployeeId };
        if (swap.FromEmployeeId.HasValue)
        {
            affectedEmployeeIds.Add(swap.FromEmployeeId.Value);
        }

        var existingStyles = await db.ScheduleCellStyles
            .Where(style =>
                style.ScheduleId == swap.ScheduleId &&
                style.DayOfMonth == swap.ScheduleSlot!.DayOfMonth &&
                affectedEmployeeIds.Contains(style.EmployeeId))
            .ToDictionaryAsync(style => style.EmployeeId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var employeeId in affectedEmployeeIds)
        {
            if (existingStyles.TryGetValue(employeeId, out var existingStyle))
            {
                existingStyle.BackgroundColorArgb = highlightColorArgb;
                continue;
            }

            db.ScheduleCellStyles.Add(new ScheduleCellStyleModel
            {
                ScheduleId = swap.ScheduleId,
                DayOfMonth = swap.ScheduleSlot!.DayOfMonth,
                EmployeeId = employeeId,
                BackgroundColorArgb = highlightColorArgb,
            });
        }
    }

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

    private async Task<IReadOnlyList<ScheduleSlotModel>> LoadPublishedEmployeeMonthSlotsAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        return await db.ScheduleSlots
            .AsNoTracking()
            .Include(slot => slot.Schedule)
            .Where(slot =>
                slot.EmployeeId == employeeId &&
                slot.Schedule.Year == year &&
                slot.Schedule.Month == month &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<ScheduleSlotModel> GetPeriodSlots(
        IEnumerable<ScheduleSlotModel> slots,
        int year,
        int month)
        => slots.Where(slot => slot.Schedule.Year == year && slot.Schedule.Month == month).ToList();

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

    private static void EnsureSwapAllowed(ScheduleModel schedule)
    {
        if (!schedule.AllowSwap)
        {
            throw new ValidationException("Swaps are not allowed for this schedule.");
        }
    }

}
