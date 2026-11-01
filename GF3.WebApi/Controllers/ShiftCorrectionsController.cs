using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using WebApi.Auth;
using WebApi.Contracts.ShiftCorrections;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-shift-corrections")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeShiftCorrectionsController(
    AppDbContext db,
    IScheduleEditLockService scheduleEditLockService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftCorrectionRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShiftCorrectionRequestDto>>> GetMine(CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var requests = await ShiftCorrectionRules.BuildRequestQuery(db)
            .Where(request => request.EmployeeId == employeeId)
            .OrderBy(request => request.Status == ShiftCorrectionStatus.Pending ? 0 : 1)
            .ThenByDescending(request => request.Id)
            .Take(100)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(requests.Select(ShiftCorrectionRules.ToDto).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShiftCorrectionRequestDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ShiftCorrectionRequestDto>> Create(
        [FromBody] CreateShiftCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var schedule = await db.Schedules
            .Include(item => item.Container)
            .Include(item => item.Shop)
            .Include(item => item.Employees)
                .ThenInclude(item => item.Employee)
            .Include(item => item.Slots)
            .FirstOrDefaultAsync(item =>
                item.Id == request.ScheduleId &&
                item.PublicationStatus == SchedulePublicationStatus.Public &&
                item.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw ValidationException.ForField(nameof(request.ScheduleId), "Published schedule was not found for your account.");

        if (scheduleEditLockService.IsLocked(schedule.ContainerId, schedule.Id))
        {
            throw new ValidationException("This schedule is being edited by a manager. Try again after the manager saves changes.");
        }

        if ((request.ScheduleSlotId is null) == (request.DayOfMonth is null) || request.ScheduleSlotId is <= 0)
        {
            throw new ValidationException("Choose either an assigned shift or a day off.");
        }

        var requestedFromTime = ShiftCorrectionRules.NormalizeTime(request.RequestedFromTime, nameof(request.RequestedFromTime));
        var requestedToTime = ShiftCorrectionRules.NormalizeTime(request.RequestedToTime, nameof(request.RequestedToTime));
        ShiftCorrectionRules.ValidatePeriod(requestedFromTime, requestedToTime);

        ScheduleSlotModel? slot = null;
        int day;
        if (request.ScheduleSlotId is { } slotId)
        {
            slot = schedule.Slots.SingleOrDefault(item => item.Id == slotId)
                ?? throw ValidationException.ForField(nameof(request.ScheduleSlotId), "Shift was not found in this schedule.");
            if (slot.EmployeeId != employeeId)
                throw ValidationException.ForField(nameof(request.ScheduleSlotId), "You can only correct your own assigned shift.");
            if (slot.FromTime == requestedFromTime && slot.ToTime == requestedToTime)
                throw new ValidationException("Change at least one boundary before sending the request.");
            day = slot.DayOfMonth;
        }
        else
        {
            day = request.DayOfMonth!.Value;
            await ShiftCorrectionRules.ValidateDayOffAsync(db, schedule, employeeId, day,
                requestedFromTime, requestedToTime, cancellationToken).ConfigureAwait(false);
        }

        await ShiftCorrectionRules.ValidateEmployeeOverlapAsync(
            db, schedule, employeeId, day, slot?.Id, requestedFromTime, requestedToTime, cancellationToken).ConfigureAwait(false);

        var hasPendingRequest = slot is not null && await db.ShiftCorrectionRequests
            .AnyAsync(item =>
                (item.ScheduleSlotId == slot.Id ||
                    (item.ScheduleId == schedule.Id && item.EmployeeId == employeeId &&
                     item.DayOfMonth == day && item.OriginalFromTime == slot.FromTime &&
                     item.OriginalToTime == slot.ToTime)) &&
                item.Status == ShiftCorrectionStatus.Pending,
                cancellationToken)
            .ConfigureAwait(false);
        if (hasPendingRequest)
        {
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "This shift already has a pending correction request.");
        }

        var model = new ShiftCorrectionRequestModel
        {
            ScheduleId = schedule.Id,
            ScheduleSlotId = slot?.Id,
            DayOfMonth = day,
            EmployeeId = employeeId,
            OriginalFromTime = slot?.FromTime,
            OriginalToTime = slot?.ToTime,
            RequestedFromTime = requestedFromTime,
            RequestedToTime = requestedToTime,
            Status = ShiftCorrectionStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.ShiftCorrectionRequests.Add(model);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException error) when (ShiftCorrectionRules.IsPendingDuplicate(error))
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "A correction request is already pending." });
        }

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService.LogAsync(
            User,
            $"Requested a shift correction in schedule \"{schedule.Name}\" for {schedule.Year}-{schedule.Month:00}-{day:00}: {(slot is null ? "day off" : $"{slot.FromTime}-{slot.ToTime}")} to {requestedFromTime}-{requestedToTime}.",
            cancellationToken)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(
            schedule.ContainerId,
            schedule.Id,
            schedule.Id,
            "employee-shift-correction-created",
            model.Id)).ConfigureAwait(false);

        var created = await ShiftCorrectionRules.BuildRequestQuery(db)
            .SingleAsync(item => item.Id == model.Id, cancellationToken)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(GetMine), ShiftCorrectionRules.ToDto(created));
    }

    private int GetRequiredEmployeeId()
        => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) && employeeId > 0
            ? employeeId
            : throw new BadHttpRequestException("The current employee session is invalid.");
}

[ApiController]
[Route("api/manager-shift-correction-settings")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerShiftCorrectionSettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ShiftCorrectionSettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftCorrectionSettingDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var color = await db.ManagerShiftCorrectionSettings
            .AsNoTracking()
            .Where(setting => setting.ManagerAccountId == managerId)
            .Select(setting => setting.HighlightColor)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(new ShiftCorrectionSettingDto { HighlightColor = color ?? ShiftCorrectionRules.DefaultHighlightColor });
    }

    [HttpPut]
    [ProducesResponseType(typeof(ShiftCorrectionSettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftCorrectionSettingDto>> Save(
        [FromBody] SaveShiftCorrectionSettingRequest request,
        CancellationToken cancellationToken)
    {
        var color = ShiftCorrectionRules.NormalizeColor(request.HighlightColor, nameof(request.HighlightColor));
        await ShiftCorrectionRules.UpsertSettingAsync(db, GetRequiredManagerId(), color, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new ShiftCorrectionSettingDto { HighlightColor = color });
    }

    private int GetRequiredManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) && managerId > 0
            ? managerId
            : throw new BadHttpRequestException("The current manager session is invalid.");
}

[ApiController]
[Route("api/containers/{containerId:int}/graphs/{graphId:int}/shift-corrections")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerShiftCorrectionsController(
    AppDbContext db,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier,
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftCorrectionRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShiftCorrectionRequestDto>>> GetGraphRequests(
        int containerId,
        int graphId,
        CancellationToken cancellationToken)
    {
        var graphExists = await db.Schedules
            .AnyAsync(item => item.Id == graphId && item.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false);
        if (!graphExists)
        {
            throw new KeyNotFoundException($"Schedule with id {graphId} was not found.");
        }

        var requests = await ShiftCorrectionRules.BuildRequestQuery(db)
            .Where(item => item.ScheduleId == graphId && item.Schedule.ContainerId == containerId)
            .OrderBy(item => item.Status == ShiftCorrectionStatus.Pending ? 0 : 1)
            .ThenByDescending(item => item.Id)
            .Take(100)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(requests.Select(ShiftCorrectionRules.ToDto).ToList());
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(ShiftCorrectionRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftCorrectionRequestDto>> Approve(
        int containerId,
        int graphId,
        int id,
        [FromBody] ApproveShiftCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ApproveCore(containerId, graphId, id, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (ShiftCorrectionRules.IsReviewConflict(error))
        {
            db.ChangeTracker.Clear();
            return Conflict(new ProblemDetails { Status = 409, Title = "The schedule or request changed. Refresh and try again." });
        }
    }

    private async Task<ActionResult<ShiftCorrectionRequestDto>> ApproveCore(
        int containerId, int graphId, int id, ApproveShiftCorrectionRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var correction = await ShiftCorrectionRules.BuildRequestQuery(db)
            .SingleOrDefaultAsync(item =>
                item.Id == id &&
                item.ScheduleId == graphId &&
                item.Schedule.ContainerId == containerId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift correction with id {id} was not found.");
        EnsurePending(correction);

        var isDayOff = correction.OriginalFromTime is null && correction.OriginalToTime is null;
        ScheduleSlotModel slot;
        if (isDayOff)
        {
            if (correction.Schedule.PublicationStatus != SchedulePublicationStatus.Public ||
                !await db.ScheduleEmployees.AnyAsync(item => item.ScheduleId == graphId &&
                    item.EmployeeId == correction.EmployeeId, cancellationToken).ConfigureAwait(false))
                throw new ValidationException("Published schedule was not found for this employee.");

            await ShiftCorrectionRules.ValidateDayOffAsync(db, correction.Schedule, correction.EmployeeId,
                correction.DayOfMonth, correction.RequestedFromTime, correction.RequestedToTime,
                cancellationToken, correction.Id).ConfigureAwait(false);
            ShiftCorrectionRules.ValidatePeriod(
                ShiftCorrectionRules.NormalizeTime(correction.RequestedFromTime, nameof(correction.RequestedFromTime)),
                ShiftCorrectionRules.NormalizeTime(correction.RequestedToTime, nameof(correction.RequestedToTime)));
            await ShiftCorrectionRules.ValidateEmployeeOverlapAsync(db, correction.Schedule, correction.EmployeeId,
                correction.DayOfMonth, null, correction.RequestedFromTime, correction.RequestedToTime,
                cancellationToken).ConfigureAwait(false);
            var lastSlotNo = await db.ScheduleSlots.Where(item => item.ScheduleId == graphId &&
                    item.DayOfMonth == correction.DayOfMonth).MaxAsync(item => (int?)item.SlotNo, cancellationToken)
                .ConfigureAwait(false) ?? 0;
            slot = new ScheduleSlotModel
            {
                ScheduleId = graphId, DayOfMonth = correction.DayOfMonth, EmployeeId = correction.EmployeeId,
                FromTime = correction.RequestedFromTime, ToTime = correction.RequestedToTime,
                Status = SlotStatus.ASSIGNED, SlotNo = lastSlotNo + 1,
            };
            db.ScheduleSlots.Add(slot);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var originalSlot = await db.ScheduleSlots
                .SingleOrDefaultAsync(item => item.Id == correction.ScheduleSlotId && item.ScheduleId == graphId, cancellationToken)
                .ConfigureAwait(false);
            if (originalSlot is null)
            {
                var candidates = await db.ScheduleSlots
                    .Where(item => item.ScheduleId == correction.ScheduleId &&
                        item.EmployeeId == correction.EmployeeId && item.DayOfMonth == correction.DayOfMonth &&
                        item.FromTime == correction.OriginalFromTime && item.ToTime == correction.OriginalToTime)
                    .Take(2).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (candidates.Count == 0)
                {
                    throw new ValidationException("The original shift no longer exists. Reject this request and ask the employee to submit a new one.");
                }
                if (candidates.Count > 1)
                {
                    throw new ValidationException("The original shift cannot be uniquely identified. Reject this request and ask the employee to submit a new one.");
                }
                originalSlot = candidates[0];
            }
            slot = originalSlot;
            if (slot.EmployeeId != correction.EmployeeId ||
                slot.DayOfMonth != correction.DayOfMonth ||
                slot.FromTime != correction.OriginalFromTime ||
                slot.ToTime != correction.OriginalToTime)
            {
                throw new ValidationException("The original shift changed after this request was sent. Reject it and ask the employee to submit a new one.");
            }

            var otherSlots = await db.ScheduleSlots
                .Where(item => item.ScheduleId == graphId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await ShiftCorrectionRules.ValidateEmployeeOverlapAsync(
                db, correction.Schedule, correction.EmployeeId, correction.DayOfMonth, slot.Id, correction.RequestedFromTime,
                correction.RequestedToTime, cancellationToken).ConfigureAwait(false);
            var collidesWithSlotNumber = otherSlots.Any(item =>
                item.Id != slot.Id &&
                item.DayOfMonth == slot.DayOfMonth &&
                item.SlotNo == slot.SlotNo &&
                item.FromTime == correction.RequestedFromTime &&
                item.ToTime == correction.RequestedToTime);
            if (collidesWithSlotNumber)
            {
                throw new ValidationException("The requested time conflicts with another shift position in this schedule.");
            }
        }

        var managerId = GetRequiredManagerId();
        var highlightColor = string.IsNullOrWhiteSpace(request.HighlightColor)
            ? await ShiftCorrectionRules.GetSettingColorAsync(db, managerId, cancellationToken).ConfigureAwait(false)
            : ShiftCorrectionRules.NormalizeColor(request.HighlightColor, nameof(request.HighlightColor));

        correction.ScheduleSlotId = slot.Id;
        slot.FromTime = correction.RequestedFromTime;
        slot.ToTime = correction.RequestedToTime;

        var style = await db.ScheduleCellStyles.SingleOrDefaultAsync(item =>
            item.ScheduleId == graphId &&
            item.EmployeeId == correction.EmployeeId &&
            item.DayOfMonth == correction.DayOfMonth,
            cancellationToken).ConfigureAwait(false);
        if (style is null)
        {
            style = new ScheduleCellStyleModel
            {
                ScheduleId = graphId,
                EmployeeId = correction.EmployeeId,
                DayOfMonth = correction.DayOfMonth,
            };
            db.ScheduleCellStyles.Add(style);
        }
        style.BackgroundColorArgb = ShiftCorrectionRules.ToArgb(highlightColor);

        correction.Status = ShiftCorrectionStatus.Approved;
        correction.ReviewedAtUtc = DateTimeOffset.UtcNow;
        correction.ReviewedByManagerId = managerId;
        await ShiftCorrectionRules.UpsertSettingAsync(db, managerId, highlightColor, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService.LogAsync(
            User,
            $"Approved {correction.Employee.FirstName} {correction.Employee.LastName}'s shift correction in schedule \"{correction.Schedule.Name}\" for {correction.Schedule.Year}-{correction.Schedule.Month:00}-{correction.DayOfMonth:00}: {correction.OriginalFromTime}-{correction.OriginalToTime} to {correction.RequestedFromTime}-{correction.RequestedToTime}.",
            cancellationToken)).ConfigureAwait(false);
        await NotifyChangedAsync(correction, "manager-shift-correction-approved", scheduleChanged: true).ConfigureAwait(false);

        db.ChangeTracker.Clear();
        var approved = await ShiftCorrectionRules.BuildRequestQuery(db)
            .SingleAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return Ok(ShiftCorrectionRules.ToDto(approved));
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(ShiftCorrectionRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftCorrectionRequestDto>> Reject(
        int containerId,
        int graphId,
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RejectCore(containerId, graphId, id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (ShiftCorrectionRules.IsReviewConflict(error))
        {
            db.ChangeTracker.Clear();
            return Conflict(new ProblemDetails { Status = 409, Title = "The request changed. Refresh and try again." });
        }
    }

    private async Task<ActionResult<ShiftCorrectionRequestDto>> RejectCore(
        int containerId, int graphId, int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var correction = await ShiftCorrectionRules.BuildRequestQuery(db)
            .SingleOrDefaultAsync(item =>
                item.Id == id &&
                item.ScheduleId == graphId &&
                item.Schedule.ContainerId == containerId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift correction with id {id} was not found.");
        EnsurePending(correction);
        correction.Status = ShiftCorrectionStatus.Rejected;
        correction.ReviewedAtUtc = DateTimeOffset.UtcNow;
        correction.ReviewedByManagerId = GetRequiredManagerId();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService.LogAsync(
            User,
            $"Rejected {correction.Employee.FirstName} {correction.Employee.LastName}'s shift correction in schedule \"{correction.Schedule.Name}\" for {correction.Schedule.Year}-{correction.Schedule.Month:00}-{correction.DayOfMonth:00}.",
            cancellationToken)).ConfigureAwait(false);
        await NotifyChangedAsync(correction, "manager-shift-correction-rejected", scheduleChanged: false).ConfigureAwait(false);

        db.ChangeTracker.Clear();
        var rejected = await ShiftCorrectionRules.BuildRequestQuery(db)
            .SingleAsync(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return Ok(ShiftCorrectionRules.ToDto(rejected));
    }

    private static void EnsurePending(ShiftCorrectionRequestModel correction)
    {
        if (correction.Status != ShiftCorrectionStatus.Pending)
        {
            throw new ValidationException("Only pending shift corrections can be reviewed.");
        }
    }

    private ActionResult? CreateEditLockConflictResult(int containerId, int graphId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.Schedule(containerId, graphId),
            "This schedule");

    private int GetRequiredManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) && managerId > 0
            ? managerId
            : throw new BadHttpRequestException("The current manager session is invalid.");

    private async Task NotifyChangedAsync(ShiftCorrectionRequestModel correction, string reason, bool scheduleChanged)
    {
        if (scheduleChanged)
        {
            await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyScheduleChangedAsync(correction.Schedule.ContainerId, correction.ScheduleId, reason)).ConfigureAwait(false);
        }
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.Schedule,
            $"{correction.Schedule.ContainerId}:{correction.ScheduleId}",
            reason,
            correction.Schedule.ContainerId,
            correction.ScheduleId)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(
            correction.Schedule.ContainerId,
            correction.ScheduleId,
            correction.ScheduleId,
            reason,
            correction.Id)).ConfigureAwait(false);
    }
}

internal static class ShiftCorrectionRules
{
    internal const string DefaultHighlightColor = "#FDE68A";

    internal static IQueryable<ShiftCorrectionRequestModel> BuildRequestQuery(AppDbContext db)
        => db.ShiftCorrectionRequests
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Container)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Shop)
            .Include(request => request.Employee)
            .Include(request => request.ReviewedByManager);

    internal static ShiftCorrectionRequestDto ToDto(ShiftCorrectionRequestModel model) => new()
    {
        Id = model.Id,
        ContainerId = model.Schedule.ContainerId,
        ScheduleId = model.ScheduleId,
        ScheduleSlotId = model.ScheduleSlotId,
        IsDayOffRequest = model.OriginalFromTime is null && model.OriginalToTime is null,
        ScheduleName = model.Schedule.Name,
        ShopName = model.Schedule.Shop?.Name ?? string.Empty,
        Year = model.Schedule.Year,
        Month = model.Schedule.Month,
        DayOfMonth = model.DayOfMonth,
        EmployeeId = model.EmployeeId,
        EmployeeName = $"{model.Employee.FirstName} {model.Employee.LastName}".Trim(),
        OriginalFromTime = model.OriginalFromTime,
        OriginalToTime = model.OriginalToTime,
        RequestedFromTime = model.RequestedFromTime,
        RequestedToTime = model.RequestedToTime,
        Status = model.Status.ToString().ToLowerInvariant(),
        CreatedAtUtc = model.CreatedAtUtc,
        ReviewedAtUtc = model.ReviewedAtUtc,
        ReviewedByManagerName = model.ReviewedByManager?.DisplayName,
    };

    internal static string NormalizeTime(string? value, string fieldName)
    {
        var formats = new[] { "H:mm", "HH:mm" };
        if (!TimeOnly.TryParseExact(value?.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw ValidationException.ForField(fieldName, "Enter time in HH:mm format.");
        }
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    internal static void ValidatePeriod(string fromTime, string toTime)
    {
        if (string.CompareOrdinal(fromTime, toTime) >= 0)
        {
            throw new ValidationException("Shift start must be earlier than shift end.");
        }
    }

    internal static async Task ValidateEmployeeOverlapAsync(
        AppDbContext db,
        ScheduleModel schedule,
        int employeeId,
        int dayOfMonth,
        int? excludedSlotId,
        string fromTime,
        string toTime,
        CancellationToken cancellationToken)
    {
        var slots = await db.ScheduleSlots.AsNoTracking()
            .Where(slot => (excludedSlotId == null || slot.Id != excludedSlotId) &&
                slot.EmployeeId == employeeId &&
                slot.DayOfMonth == dayOfMonth &&
                slot.Schedule.Year == schedule.Year && slot.Schedule.Month == schedule.Month &&
                (slot.ScheduleId == schedule.Id || slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var overlaps = slots.Any(slot =>
            string.CompareOrdinal(slot.FromTime, toTime) < 0 &&
            string.CompareOrdinal(fromTime, slot.ToTime) < 0);
        if (overlaps)
        {
            throw new ValidationException("The corrected shift cannot overlap another period on the same day.");
        }
    }

    internal static async Task ValidateDayOffAsync(AppDbContext db, ScheduleModel schedule, int employeeId,
        int day, string fromTime, string toTime, CancellationToken cancellationToken, int? excludedRequestId = null)
    {
        if (day < 1 || day > DateTime.DaysInMonth(schedule.Year, schedule.Month))
            throw ValidationException.ForField("DayOfMonth", "Choose a valid day of this month.");

        if (await db.ScheduleSlots.AnyAsync(slot => slot.EmployeeId == employeeId && slot.DayOfMonth == day &&
                slot.Schedule.Year == schedule.Year && slot.Schedule.Month == schedule.Month &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public, cancellationToken).ConfigureAwait(false))
            throw new ValidationException("This date is not a day off. You already have an assigned shift.");

        var pending = await db.ShiftCorrectionRequests.AsNoTracking().Where(item => item.EmployeeId == employeeId &&
                item.DayOfMonth == day && item.Status == ShiftCorrectionStatus.Pending &&
                (excludedRequestId == null || item.Id != excludedRequestId) &&
                item.Schedule.Year == schedule.Year && item.Schedule.Month == schedule.Month &&
                (item.ScheduleId == schedule.Id || item.Schedule.PublicationStatus == SchedulePublicationStatus.Public))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Any(item =>
                (item.ScheduleId == schedule.Id && item.OriginalFromTime is null) ||
                (string.CompareOrdinal(item.RequestedFromTime, toTime) < 0 &&
                 string.CompareOrdinal(fromTime, item.RequestedToTime) < 0)))
            throw new ValidationException("This day already has a pending correction request.");
    }

    internal static bool IsPendingDuplicate(DbUpdateException error)
        => error.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 } sqlite &&
           sqlite.Message.Contains("shift_correction_request.", StringComparison.Ordinal);

    internal static bool IsReviewConflict(Exception error)
        => error is DbUpdateConcurrencyException ||
           error is SqliteException { SqliteErrorCode: 5 or 6 } ||
           error is DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 } } ||
           error is DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 2067 } sqlite } &&
           (sqlite.Message.Contains("schedule_slot.", StringComparison.Ordinal) ||
            sqlite.Message.Contains("schedule_cell_style.", StringComparison.Ordinal) ||
            sqlite.Message.Contains("manager_shift_correction_setting.", StringComparison.Ordinal) ||
            sqlite.Message.Contains("shift_correction_request.", StringComparison.Ordinal));

    internal static string NormalizeColor(string? value, string fieldName)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Regex.IsMatch(normalized, "^#[0-9A-F]{6}$", RegexOptions.CultureInvariant))
        {
            throw ValidationException.ForField(fieldName, "Choose a valid six-digit highlight color.");
        }
        return normalized;
    }

    internal static int ToArgb(string color)
    {
        var rgb = uint.Parse(color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return unchecked((int)(0xFF000000u | rgb));
    }

    internal static async Task<string> GetSettingColorAsync(
        AppDbContext db,
        int managerId,
        CancellationToken cancellationToken)
        => await db.ManagerShiftCorrectionSettings
            .Where(setting => setting.ManagerAccountId == managerId)
            .Select(setting => setting.HighlightColor)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? DefaultHighlightColor;

    internal static async Task UpsertSettingAsync(
        AppDbContext db,
        int managerId,
        string color,
        CancellationToken cancellationToken)
    {
        var setting = await db.ManagerShiftCorrectionSettings
            .SingleOrDefaultAsync(item => item.ManagerAccountId == managerId, cancellationToken)
            .ConfigureAwait(false);
        if (setting is null)
        {
            db.ManagerShiftCorrectionSettings.Add(new ManagerShiftCorrectionSettingModel
            {
                ManagerAccountId = managerId,
                HighlightColor = color,
            });
            return;
        }
        setting.HighlightColor = color;
    }
}
