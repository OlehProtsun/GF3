using WebApi.ShiftSwaps;
using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/containers/{containerId:int}/graphs/{graphId:int}/versions")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class GraphVersionsController(
    AppDbContext db,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier,
    ScheduleVersionCommitService versionCommitService,
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GraphVersionTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphVersionTreeDto>> GetTree(
        int containerId,
        int graphId,
        CancellationToken cancellationToken)
    {
        var schedule = await LoadScheduleAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Version history was not found", "The schedule does not exist."));
        }

        await versionCommitService.EnsureInitialVersionAsync(graphId, cancellationToken).ConfigureAwait(false);
        return Ok(await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost]
    [ProducesResponseType(typeof(GraphVersionTreeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GraphVersionTreeDto>> Commit(
        int containerId,
        int graphId,
        CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var schedule = await LoadScheduleAsync(containerId, graphId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Schedule with id {graphId} was not found.");
        var version = await versionCommitService.CommitAsync(graphId, GetManagerId(), GetManagerDisplayName(), cancellationToken).ConfigureAwait(false);
        if (version is null)
        {
            return Ok(await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
        }
        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(User, $"Created schedule commit #{version.VersionNumber} on {version.BranchName} for {schedule.Name}.", cancellationToken)).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetTree),
            new { containerId, graphId },
            await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{versionId:int}/checkout")]
    [ProducesResponseType(typeof(GraphVersionTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GraphVersionTreeDto>> Checkout(
        int containerId,
        int graphId,
        int versionId,
        CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var schedule = await db.Schedules
            .SingleOrDefaultAsync(item => item.Id == graphId && item.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false);
        var version = await db.ScheduleVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == versionId && item.ScheduleId == graphId, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null || version is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Commit was not found", "The selected schedule commit does not exist."));
        }

        var state = await db.ScheduleVersionStates
            .SingleOrDefaultAsync(item => item.ScheduleId == graphId, cancellationToken)
            .ConfigureAwait(false);
        var hasOpenSwaps = await db.ShiftSwapRequests
            .AnyAsync(request => request.ScheduleId == graphId && request.Status == ShiftSwapStatus.Open, cancellationToken)
            .ConfigureAwait(false);
        var hasPendingCorrections = await db.ShiftCorrectionRequests
            .AnyAsync(request => request.ScheduleId == graphId && request.Status == ShiftCorrectionStatus.Pending, cancellationToken)
            .ConfigureAwait(false);
        if (hasOpenSwaps || hasPendingCorrections)
        {
            return Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Checkout is blocked",
                "Resolve the schedule's open shift swaps and pending shift corrections before checking out another commit."));
        }

        var snapshot = ScheduleVersionCommitService.DeserializeSnapshot(version.SnapshotJson);
        var restoreConflict = await ValidateRestoreAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (restoreConflict is not null)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Checkout is blocked", restoreConflict));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await ShiftSwapArchive.PreserveCompletedAsync(db, graphId, cancellationToken).ConfigureAwait(false);
        await db.ScheduleCellStyles.Where(style => style.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.ScheduleSlots.Where(slot => slot.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.ScheduleEmployees.Where(employee => employee.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        foreach (var entry in db.ChangeTracker.Entries().Where(entry =>
            entry.Entity is ScheduleSlotModel slot && slot.ScheduleId == graphId ||
            entry.Entity is ScheduleEmployeeModel employee && employee.ScheduleId == graphId ||
            entry.Entity is ScheduleCellStyleModel style && style.ScheduleId == graphId).ToList())
            entry.State = EntityState.Detached;

        RestoreSchedule(schedule, snapshot);
        db.ScheduleEmployees.AddRange(snapshot.Employees.Select(employee => new ScheduleEmployeeModel
        {
            ScheduleId = graphId,
            EmployeeId = employee.EmployeeId,
            MinHoursMonth = employee.MinHoursMonth,
            DisplayOrder = employee.DisplayOrder,
        }));
        db.ScheduleSlots.AddRange(snapshot.Slots.Select(slot => new ScheduleSlotModel
        {
            ScheduleId = graphId,
            DayOfMonth = slot.DayOfMonth,
            SlotNo = slot.SlotNo,
            FromTime = slot.FromTime,
            ToTime = slot.ToTime,
            EmployeeId = slot.EmployeeId,
            Status = ParseEnum<SlotStatus>(slot.Status, "slot status"),
        }));
        db.ScheduleCellStyles.AddRange(snapshot.CellStyles.Select(style => new ScheduleCellStyleModel
        {
            ScheduleId = graphId,
            DayOfMonth = style.DayOfMonth,
            EmployeeId = style.EmployeeId,
            BackgroundColorArgb = style.BackgroundColorArgb,
            TextColorArgb = style.TextColorArgb,
        }));

        if (state is null)
        {
            state = new ScheduleVersionStateModel { ScheduleId = graphId };
            db.ScheduleVersionStates.Add(state);
        }

        state.CurrentVersionId = versionId;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(User, $"Checked out schedule commit #{version.VersionNumber} ({version.BranchName}) for {schedule.Name}.", cancellationToken)).ConfigureAwait(false);
        await NotifyGraphChangedAsync(containerId, graphId, "manager-schedule-version-checked-out").ConfigureAwait(false);
        return Ok(await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{versionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int containerId,
        int graphId,
        int versionId,
        CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(containerId, graphId) is { } conflict)
        {
            return conflict;
        }

        var scheduleExists = await db.Schedules
            .AnyAsync(schedule => schedule.Id == graphId && schedule.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false);
        var version = await db.ScheduleVersions
            .SingleOrDefaultAsync(item => item.Id == versionId && item.ScheduleId == graphId, cancellationToken)
            .ConfigureAwait(false);
        if (!scheduleExists || version is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Commit was not found", "The selected schedule commit does not exist."));
        }

        var state = await db.ScheduleVersionStates
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ScheduleId == graphId, cancellationToken)
            .ConfigureAwait(false);
        if (state?.CurrentVersionId == versionId)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Current commit cannot be deleted", "Checkout another commit first."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var parentVersionId = version.ParentVersionId;
        await db.ScheduleVersions
            .Where(item => item.ParentVersionId == versionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ParentVersionId, parentVersionId), cancellationToken)
            .ConfigureAwait(false);
        db.ScheduleVersions.Remove(version);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(User, $"Deleted schedule commit #{version.VersionNumber} ({version.BranchName}).", cancellationToken)).ConfigureAwait(false);
        return NoContent();
    }

    private async Task<ScheduleModel?> LoadScheduleAsync(int containerId, int graphId, CancellationToken cancellationToken)
        => await db.Schedules
            .AsNoTracking()
            .AsSplitQuery()
            .Include(schedule => schedule.Employees)
            .Include(schedule => schedule.Slots)
            .Include(schedule => schedule.CellStyles)
            .SingleOrDefaultAsync(schedule => schedule.Id == graphId && schedule.ContainerId == containerId, cancellationToken)
            .ConfigureAwait(false);

    private async Task<GraphVersionTreeDto> BuildTreeAsync(int graphId, CancellationToken cancellationToken)
    {
        var currentVersionId = await db.ScheduleVersionStates
            .Where(state => state.ScheduleId == graphId)
            .Select(state => state.CurrentVersionId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var versions = await db.ScheduleVersions
            .AsNoTracking()
            .Where(version => version.ScheduleId == graphId)
            .OrderBy(version => version.VersionNumber)
            .Select(version => new GraphVersionDto
            {
                Id = version.Id,
                ParentVersionId = version.ParentVersionId,
                VersionNumber = version.VersionNumber,
                BranchName = version.BranchName,
                CreatedAtUtc = version.CreatedAtUtc,
                AuthorName = version.CreatedByManagerName,
                EmployeeCount = version.EmployeeCount,
                SlotCount = version.SlotCount,
                CellStyleCount = version.CellStyleCount,
                IsCurrent = version.Id == currentVersionId,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new GraphVersionTreeDto { CurrentVersionId = currentVersionId, Versions = versions };
    }

    private async Task<string?> ValidateRestoreAsync(ScheduleVersionSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (!await db.Shops.AnyAsync(shop => shop.Id == snapshot.ShopId, cancellationToken).ConfigureAwait(false))
        {
            return "The shop stored in this commit no longer exists.";
        }

        if (snapshot.AvailabilityGroupId.HasValue &&
            !await db.AvailabilityGroups.AnyAsync(group => group.Id == snapshot.AvailabilityGroupId.Value, cancellationToken).ConfigureAwait(false))
        {
            return "The availability group stored in this commit no longer exists.";
        }

        var employeeIds = snapshot.Employees.Select(employee => employee.EmployeeId)
            .Concat(snapshot.Slots.Where(slot => slot.EmployeeId.HasValue).Select(slot => slot.EmployeeId!.Value))
            .Concat(snapshot.CellStyles.Select(style => style.EmployeeId))
            .Distinct()
            .ToArray();
        var existingEmployeeCount = await db.Employees.CountAsync(employee => employeeIds.Contains(employee.Id), cancellationToken).ConfigureAwait(false);
        return existingEmployeeCount == employeeIds.Length
            ? null
            : "One or more employees stored in this commit no longer exist.";
    }

    private static void RestoreSchedule(ScheduleModel schedule, ScheduleVersionSnapshot snapshot)
    {
        schedule.ShopId = snapshot.ShopId;
        schedule.Name = snapshot.Name;
        schedule.Year = snapshot.Year;
        schedule.Month = snapshot.Month;
        schedule.PublicationStatus = ParseEnum<SchedulePublicationStatus>(snapshot.PublicationStatus, "publication status");
        schedule.AllowSwap = snapshot.AllowSwap;
        schedule.AcceptedSwapHighlightColor = snapshot.AcceptedSwapHighlightColor ?? schedule.AcceptedSwapHighlightColor;
        schedule.PeoplePerShift = snapshot.PeoplePerShift;
        schedule.Shift1Time = snapshot.Shift1Time;
        schedule.Shift2Time = snapshot.Shift2Time;
        schedule.MaxHoursPerEmpMonth = snapshot.MaxHoursPerEmpMonth;
        schedule.MaxConsecutiveDays = snapshot.MaxConsecutiveDays;
        schedule.MaxConsecutiveFull = snapshot.MaxConsecutiveFull;
        schedule.MaxFullPerMonth = snapshot.MaxFullPerMonth;
        schedule.Note = snapshot.Note;
        schedule.AvailabilityGroupId = snapshot.AvailabilityGroupId;
    }

    private static TEnum ParseEnum<TEnum>(string value, string label) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"The commit contains an invalid {label}.");

    private int? GetManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) ? managerId : null;

    private string GetManagerDisplayName()
        => User.FindFirstValue("display_name") ?? User.Identity?.Name ?? "Manager";

    private ActionResult? CreateEditLockConflictResult(int containerId, int graphId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.Schedule(containerId, graphId),
            "This schedule");

    private async Task NotifyGraphChangedAsync(int containerId, int graphId, string reason)
    {
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Schedule, $"{containerId}:{graphId}", reason, containerId, graphId)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyScheduleChangedAsync(containerId, graphId, reason)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, reason)).ConfigureAwait(false);
    }

    private ProblemDetails CreateProblem(int status, string title, string detail)
        => new()
        {
            Type = status == StatusCodes.Status404NotFound ? "not_found" : "conflict",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = HttpContext.Request.Path,
        };
}

public sealed class GraphVersionTreeDto
{
    public int? CurrentVersionId { get; init; }
    public IReadOnlyList<GraphVersionDto> Versions { get; init; } = [];
}

public sealed class GraphVersionDto
{
    public int Id { get; init; }
    public int? ParentVersionId { get; init; }
    public int VersionNumber { get; init; }
    public string BranchName { get; init; } = null!;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string AuthorName { get; init; } = null!;
    public int EmployeeCount { get; init; }
    public int SlotCount { get; init; }
    public int CellStyleCount { get; init; }
    public bool IsCurrent { get; init; }
}
