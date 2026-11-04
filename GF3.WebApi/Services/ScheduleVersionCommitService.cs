using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Services;

public sealed class ScheduleVersionCommitService(AppDbContext db)
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ScheduleVersionModel?> CommitAsync(
        int scheduleId, int? authorManagerId, string authorName, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.AsNoTracking().AsSplitQuery()
            .Include(schedule => schedule.Employees)
            .Include(schedule => schedule.Slots)
            .Include(schedule => schedule.CellStyles)
            .SingleOrDefaultAsync(schedule => schedule.Id == scheduleId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Schedule with id {scheduleId} was not found.");
        authorName = string.IsNullOrWhiteSpace(authorName) ? "Unknown author" : authorName.Trim();
        if (authorName.Length > 160) authorName = authorName[..160];
        var state = await db.ScheduleVersionStates
            .Include(versionState => versionState.CurrentVersion)
            .SingleOrDefaultAsync(versionState => versionState.ScheduleId == scheduleId, cancellationToken)
            .ConfigureAwait(false);
        var currentVersion = state?.CurrentVersion;
        var nextVersionNumber = (await db.ScheduleVersions
            .Where(version => version.ScheduleId == scheduleId)
            .MaxAsync(version => (int?)version.VersionNumber, cancellationToken)
            .ConfigureAwait(false) ?? 0) + 1;
        var branchName = await ResolveCommitBranchNameAsync(scheduleId, currentVersion, cancellationToken).ConfigureAwait(false);
        var version = CreateVersion(
            schedule,
            currentVersion?.Id,
            nextVersionNumber,
            branchName,
            authorManagerId,
            authorName);

        if (currentVersion is not null && string.Equals(currentVersion.SnapshotJson, version.SnapshotJson, StringComparison.Ordinal))
        {
            return null;
        }

        db.ScheduleVersions.Add(version);
        if (state is null)
        {
            state = new ScheduleVersionStateModel { ScheduleId = scheduleId, CurrentVersion = version };
            db.ScheduleVersionStates.Add(state);
        }
        else
        {
            state.CurrentVersion = version;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return version;
    }

    public async Task EnsureInitialVersionAsync(int scheduleId, CancellationToken cancellationToken)
    {
        if (await db.ScheduleVersionStates.AnyAsync(state => state.ScheduleId == scheduleId, cancellationToken).ConfigureAwait(false))
            return;
        await CommitAsync(scheduleId, null, "Production import", cancellationToken).ConfigureAwait(false);
    }

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
                style.TextColorArgb)).ToList(), schedule.AcceptedSwapHighlightColor);

    internal static ScheduleVersionSnapshot DeserializeSnapshot(string snapshotJson)
        => JsonSerializer.Deserialize<ScheduleVersionSnapshot>(snapshotJson, SnapshotJsonOptions)
            ?? throw new InvalidOperationException("The schedule commit snapshot is invalid.");

}

internal sealed record ScheduleVersionSnapshot(
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
        List<ScheduleVersionCellStyleSnapshot> CellStyles, string? AcceptedSwapHighlightColor = null);

internal sealed record ScheduleVersionEmployeeSnapshot(int EmployeeId, int? MinHoursMonth, int DisplayOrder);
internal sealed record ScheduleVersionSlotSnapshot(
        int DayOfMonth,
        int SlotNo,
        string FromTime,
        string ToTime,
        int? EmployeeId,
        string Status);
internal sealed record ScheduleVersionCellStyleSnapshot(
        int DayOfMonth,
        int EmployeeId,
        int? BackgroundColorArgb,
        int? TextColorArgb);
