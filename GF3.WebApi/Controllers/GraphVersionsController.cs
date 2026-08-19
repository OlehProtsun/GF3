using System.Security.Claims;
using System.Text.Json;
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
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

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

        await EnsureInitialVersionAsync(schedule, cancellationToken).ConfigureAwait(false);
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
        var state = await db.ScheduleVersionStates
            .Include(versionState => versionState.CurrentVersion)
            .SingleOrDefaultAsync(versionState => versionState.ScheduleId == graphId, cancellationToken)
            .ConfigureAwait(false);
        var currentVersion = state?.CurrentVersion;
        var nextVersionNumber = (await db.ScheduleVersions
            .Where(version => version.ScheduleId == graphId)
            .MaxAsync(version => (int?)version.VersionNumber, cancellationToken)
            .ConfigureAwait(false) ?? 0) + 1;
        var branchName = await ResolveCommitBranchNameAsync(graphId, currentVersion, cancellationToken).ConfigureAwait(false);
        var version = CreateVersion(
            schedule,
            currentVersion?.Id,
            nextVersionNumber,
            branchName,
            GetManagerId(),
            GetManagerDisplayName());

        if (currentVersion is not null && string.Equals(currentVersion.SnapshotJson, version.SnapshotJson, StringComparison.Ordinal))
        {
            return Ok(await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
        }

        db.ScheduleVersions.Add(version);
        if (state is null)
        {
            state = new ScheduleVersionStateModel { ScheduleId = graphId, CurrentVersion = version };
            db.ScheduleVersionStates.Add(state);
        }
        else
        {
            state.CurrentVersion = version;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await workflowLogService
            .LogAsync(User, $"Created schedule commit #{version.VersionNumber} on {version.BranchName} for {schedule.Name}.", cancellationToken)
            .ConfigureAwait(false);

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
        if (state?.CurrentVersionId == versionId)
        {
            return Ok(await BuildTreeAsync(graphId, cancellationToken).ConfigureAwait(false));
        }

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

        var snapshot = DeserializeSnapshot(version.SnapshotJson);
        var restoreConflict = await ValidateRestoreAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (restoreConflict is not null)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Checkout is blocked", restoreConflict));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await db.ScheduleCellStyles.Where(style => style.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.ScheduleSlots.Where(slot => slot.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.ScheduleEmployees.Where(employee => employee.ScheduleId == graphId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

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

        await workflowLogService
            .LogAsync(User, $"Checked out schedule commit #{version.VersionNumber} ({version.BranchName}) for {schedule.Name}.", cancellationToken)
            .ConfigureAwait(false);
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

        await workflowLogService
            .LogAsync(User, $"Deleted schedule commit #{version.VersionNumber} ({version.BranchName}).", cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    private async Task EnsureInitialVersionAsync(ScheduleModel schedule, CancellationToken cancellationToken)
    {
        if (await db.ScheduleVersionStates.AnyAsync(state => state.ScheduleId == schedule.Id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var version = CreateVersion(schedule, null, 1, "main", null, "Production import");
        db.ScheduleVersions.Add(version);
        db.ScheduleVersionStates.Add(new ScheduleVersionStateModel
        {
            ScheduleId = schedule.Id,
            CurrentVersion = version,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private static ScheduleVersionModel CreateVersion(
        ScheduleModel schedule,
        int? parentVersionId,
        int versionNumber,
        string branchName,
        int? managerId,
        string managerName)
    {
        var snapshot = CreateSnapshot(schedule);
        return new ScheduleVersionModel
        {
            ScheduleId = schedule.Id,
            ParentVersionId = parentVersionId,
            VersionNumber = versionNumber,
            BranchName = branchName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByManagerId = managerId,
            CreatedByManagerName = managerName,
            EmployeeCount = snapshot.Employees.Count,
            SlotCount = snapshot.Slots.Count,
            CellStyleCount = snapshot.CellStyles.Count,
            SnapshotJson = JsonSerializer.Serialize(snapshot, SnapshotJsonOptions),
        };
    }

    private async Task<string> ResolveCommitBranchNameAsync(
        int graphId,
        ScheduleVersionModel? currentVersion,
        CancellationToken cancellationToken)
    {
        if (currentVersion is null)
        {
            return "main";
        }

        var hasLaterCommitOnBranch = await db.ScheduleVersions.AnyAsync(
            version =>
                version.ScheduleId == graphId &&
                version.BranchName == currentVersion.BranchName &&
                version.VersionNumber > currentVersion.VersionNumber,
            cancellationToken).ConfigureAwait(false);
        if (!hasLaterCommitOnBranch)
        {
            return currentVersion.BranchName;
        }

        var branchNames = await db.ScheduleVersions
            .Where(version => version.ScheduleId == graphId)
            .Select(version => version.BranchName)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var branchNameSet = branchNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var branchNumber = 2;
        while (branchNameSet.Contains($"branch-{branchNumber}"))
        {
            branchNumber++;
        }

        return $"branch-{branchNumber}";
    }

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

    private static ScheduleVersionSnapshot CreateSnapshot(ScheduleModel schedule)
        => new(
            schedule.ShopId,
            schedule.Name,
            schedule.Year,
            schedule.Month,
            schedule.PublicationStatus.ToString(),
            schedule.AllowSwap,
            schedule.PeoplePerShift,
            schedule.Shift1Time,
            schedule.Shift2Time,
            schedule.MaxHoursPerEmpMonth,
            schedule.MaxConsecutiveDays,
            schedule.MaxConsecutiveFull,
            schedule.MaxFullPerMonth,
            schedule.Note,
            schedule.AvailabilityGroupId,
            schedule.Employees.OrderBy(employee => employee.DisplayOrder).Select(employee => new ScheduleVersionEmployeeSnapshot(
                employee.EmployeeId,
                employee.MinHoursMonth,
                employee.DisplayOrder)).ToList(),
            schedule.Slots.OrderBy(slot => slot.DayOfMonth).ThenBy(slot => slot.SlotNo).Select(slot => new ScheduleVersionSlotSnapshot(
                slot.DayOfMonth,
                slot.SlotNo,
                slot.FromTime,
                slot.ToTime,
                slot.EmployeeId,
                slot.Status.ToString())).ToList(),
            schedule.CellStyles.OrderBy(style => style.DayOfMonth).ThenBy(style => style.EmployeeId).Select(style => new ScheduleVersionCellStyleSnapshot(
                style.DayOfMonth,
                style.EmployeeId,
                style.BackgroundColorArgb,
                style.TextColorArgb)).ToList());

    private static ScheduleVersionSnapshot DeserializeSnapshot(string snapshotJson)
        => JsonSerializer.Deserialize<ScheduleVersionSnapshot>(snapshotJson, SnapshotJsonOptions)
            ?? throw new InvalidOperationException("The schedule commit snapshot is invalid.");

    private static void RestoreSchedule(ScheduleModel schedule, ScheduleVersionSnapshot snapshot)
    {
        schedule.ShopId = snapshot.ShopId;
        schedule.Name = snapshot.Name;
        schedule.Year = snapshot.Year;
        schedule.Month = snapshot.Month;
        schedule.PublicationStatus = ParseEnum<SchedulePublicationStatus>(snapshot.PublicationStatus, "publication status");
        schedule.AllowSwap = snapshot.AllowSwap;
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
        await realtimeNotifier
            .NotifyManagerDataChangedAsync(ManagerEditResourceTypes.Schedule, $"{containerId}:{graphId}", reason, containerId, graphId)
            .ConfigureAwait(false);
        await realtimeNotifier.NotifyScheduleChangedAsync(containerId, graphId, reason).ConfigureAwait(false);
        await realtimeNotifier.NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, reason).ConfigureAwait(false);
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

    private sealed record ScheduleVersionSnapshot(
        int ShopId,
        string Name,
        int Year,
        int Month,
        string PublicationStatus,
        bool AllowSwap,
        int PeoplePerShift,
        string Shift1Time,
        string Shift2Time,
        int MaxHoursPerEmpMonth,
        int MaxConsecutiveDays,
        int MaxConsecutiveFull,
        int MaxFullPerMonth,
        string? Note,
        int? AvailabilityGroupId,
        List<ScheduleVersionEmployeeSnapshot> Employees,
        List<ScheduleVersionSlotSnapshot> Slots,
        List<ScheduleVersionCellStyleSnapshot> CellStyles);

    private sealed record ScheduleVersionEmployeeSnapshot(int EmployeeId, int? MinHoursMonth, int DisplayOrder);
    private sealed record ScheduleVersionSlotSnapshot(
        int DayOfMonth,
        int SlotNo,
        string FromTime,
        string ToTime,
        int? EmployeeId,
        string Status);
    private sealed record ScheduleVersionCellStyleSnapshot(
        int DayOfMonth,
        int EmployeeId,
        int? BackgroundColorArgb,
        int? TextColorArgb);
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
