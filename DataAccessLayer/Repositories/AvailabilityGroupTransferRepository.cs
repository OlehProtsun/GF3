using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Read models and atomic writes for cross-availability day transfers.
/// </summary>
public sealed class AvailabilityGroupTransferRepository(AppDbContext db) : IAvailabilityGroupTransferRepository
{
    public async Task<List<AvailabilityTransferSourceRecord>> GetSourcesAsync(
        int targetGroupId,
        int targetMemberId,
        CancellationToken ct = default)
    {
        var targetMember = await db.AvailabilityGroupMembers
            .AsNoTracking()
            .Include(member => member.AvailabilityGroup)
            .Include(member => member.Days)
            .SingleOrDefaultAsync(
                member => member.Id == targetMemberId && member.AvailabilityGroupId == targetGroupId,
                ct)
            .ConfigureAwait(false);
        if (targetMember is null)
        {
            return [];
        }

        var sourceMembers = await db.AvailabilityGroupMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(member => member.AvailabilityGroup)
            .Include(member => member.Days)
            .Where(member =>
                member.EmployeeId == targetMember.EmployeeId &&
                member.AvailabilityGroupId != targetGroupId &&
                member.AvailabilityGroup.Year == targetMember.AvailabilityGroup.Year &&
                member.AvailabilityGroup.Month == targetMember.AvailabilityGroup.Month)
            .OrderBy(member => member.AvailabilityGroup.Name)
            .ThenBy(member => member.AvailabilityGroupId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sourceMemberIds = sourceMembers.Select(member => member.Id).ToList();
        var relevantMemberIds = sourceMemberIds
            .Append(targetMemberId)
            .Distinct()
            .ToList();
        var transferLinks = await db.Set<AvailabilityGroupDayTransferModel>()
            .AsNoTracking()
            .Where(transfer =>
                relevantMemberIds.Contains(transfer.SourceMemberId) ||
                relevantMemberIds.Contains(transfer.TargetMemberId))
            .Select(transfer => new
            {
                transfer.SourceMemberId,
                transfer.TargetMemberId,
                transfer.DayOfMonth,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var outgoingKeys = transferLinks
            .Where(transfer => sourceMemberIds.Contains(transfer.SourceMemberId))
            .Select(transfer => (transfer.SourceMemberId, transfer.DayOfMonth))
            .ToHashSet();
        var targetLinkedDays = transferLinks
            .Where(transfer => transfer.SourceMemberId == targetMemberId || transfer.TargetMemberId == targetMemberId)
            .Select(transfer => transfer.DayOfMonth)
            .ToHashSet();
        var returnToTargetKeys = transferLinks
            .Where(transfer =>
                transfer.SourceMemberId == targetMemberId &&
                sourceMemberIds.Contains(transfer.TargetMemberId))
            .Select(transfer => (CurrentSourceMemberId: transfer.TargetMemberId, transfer.DayOfMonth))
            .ToHashSet();
        var targetDayByNumber = targetMember.Days.ToDictionary(day => day.DayOfMonth);

        return sourceMembers.Select(sourceMember => new AvailabilityTransferSourceRecord(
            sourceMember.AvailabilityGroupId,
            sourceMember.AvailabilityGroup.Name,
            sourceMember.Id,
            sourceMember.EmployeeId,
            sourceMember.Days
                .OrderBy(day => day.DayOfMonth)
                .Select(day => new AvailabilityTransferSourceDayRecord(
                    day.DayOfMonth,
                    day.Kind,
                    day.IntervalStr,
                    day.Kind != AvailabilityKind.NONE &&
                    !outgoingKeys.Contains((sourceMember.Id, day.DayOfMonth)) &&
                    (!targetLinkedDays.Contains(day.DayOfMonth) ||
                     returnToTargetKeys.Contains((sourceMember.Id, day.DayOfMonth))) &&
                    (!targetDayByNumber.TryGetValue(day.DayOfMonth, out var targetDay) || targetDay.Kind == AvailabilityKind.NONE)))
                .ToList()))
            .ToList();
    }

    public async Task<List<AvailabilityTransferSourceRecord>> GetPreviewSourcesAsync(
        IReadOnlyCollection<int> employeeIds,
        int year,
        int month,
        int? targetGroupId,
        CancellationToken ct = default)
    {
        var normalizedEmployeeIds = employeeIds
            .Where(employeeId => employeeId > 0)
            .Distinct()
            .ToList();
        if (normalizedEmployeeIds.Count == 0)
        {
            return [];
        }

        var targetMembers = targetGroupId.HasValue
            ? await db.AvailabilityGroupMembers
                .AsNoTracking()
                .Include(member => member.Days)
                .Where(member =>
                    member.AvailabilityGroupId == targetGroupId.Value &&
                    normalizedEmployeeIds.Contains(member.EmployeeId))
                .ToListAsync(ct)
                .ConfigureAwait(false)
            : [];

        var sourceMembers = await db.AvailabilityGroupMembers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(member => member.AvailabilityGroup)
            .Include(member => member.Days)
            .Where(member =>
                normalizedEmployeeIds.Contains(member.EmployeeId) &&
                (!targetGroupId.HasValue || member.AvailabilityGroupId != targetGroupId.Value) &&
                member.AvailabilityGroup.Year == year &&
                member.AvailabilityGroup.Month == month)
            .OrderBy(member => member.AvailabilityGroup.Name)
            .ThenBy(member => member.AvailabilityGroupId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sourceMemberIds = sourceMembers.Select(member => member.Id).ToList();
        var targetMemberIds = targetMembers.Select(member => member.Id).ToList();
        var targetMemberIdSet = targetMemberIds.ToHashSet();
        var relevantMemberIds = sourceMemberIds
            .Concat(targetMemberIds)
            .Distinct()
            .ToList();
        var transferLinks = await db.Set<AvailabilityGroupDayTransferModel>()
            .AsNoTracking()
            .Where(transfer =>
                relevantMemberIds.Contains(transfer.SourceMemberId) ||
                relevantMemberIds.Contains(transfer.TargetMemberId))
            .Select(transfer => new
            {
                transfer.SourceMemberId,
                transfer.TargetMemberId,
                transfer.DayOfMonth,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var outgoingKeys = transferLinks
            .Where(transfer => sourceMemberIds.Contains(transfer.SourceMemberId))
            .Select(transfer => (transfer.SourceMemberId, transfer.DayOfMonth))
            .ToHashSet();
        var targetLinkedKeys = transferLinks
            .SelectMany(transfer => new[]
            {
                (MemberId: transfer.SourceMemberId, transfer.DayOfMonth),
                (MemberId: transfer.TargetMemberId, transfer.DayOfMonth),
            })
            .Where(key => targetMemberIdSet.Contains(key.MemberId))
            .ToHashSet();
        var returnToTargetKeys = transferLinks
            .Where(transfer =>
                targetMemberIdSet.Contains(transfer.SourceMemberId) &&
                sourceMemberIds.Contains(transfer.TargetMemberId))
            .Select(transfer => (
                CurrentSourceMemberId: transfer.TargetMemberId,
                TargetMemberId: transfer.SourceMemberId,
                transfer.DayOfMonth))
            .ToHashSet();
        var targetDayByKey = targetMembers
            .SelectMany(member => member.Days)
            .ToDictionary(day => (day.AvailabilityGroupMemberId, day.DayOfMonth));
        var targetMemberByEmployeeId = targetMembers.ToDictionary(member => member.EmployeeId);

        return sourceMembers.Select(sourceMember =>
        {
            targetMemberByEmployeeId.TryGetValue(sourceMember.EmployeeId, out var targetMember);
            var days = sourceMember.Days
                .OrderBy(day => day.DayOfMonth)
                .Select(day =>
                {
                    var targetIsOpen = targetMember is null ||
                        ((!targetLinkedKeys.Contains((targetMember.Id, day.DayOfMonth)) ||
                          returnToTargetKeys.Contains((sourceMember.Id, targetMember.Id, day.DayOfMonth))) &&
                         (!targetDayByKey.TryGetValue((targetMember.Id, day.DayOfMonth), out var targetDay) ||
                          targetDay.Kind == AvailabilityKind.NONE));
                    var canTransfer =
                        day.Kind != AvailabilityKind.NONE &&
                        !outgoingKeys.Contains((sourceMember.Id, day.DayOfMonth)) &&
                        targetIsOpen;

                    return new AvailabilityTransferSourceDayRecord(
                        day.DayOfMonth,
                        day.Kind,
                        day.IntervalStr,
                        canTransfer);
                })
                .ToList();

            return new AvailabilityTransferSourceRecord(
                sourceMember.AvailabilityGroupId,
                sourceMember.AvailabilityGroup.Name,
                sourceMember.Id,
                sourceMember.EmployeeId,
                days);
        }).ToList();
    }

    public async Task<HashSet<int>> GetOutgoingDayNumbersAsync(
        int sourceMemberId,
        CancellationToken ct = default)
        => (await db.Set<AvailabilityGroupDayTransferModel>()
            .AsNoTracking()
            .Where(transfer => transfer.SourceMemberId == sourceMemberId)
            .Select(transfer => transfer.DayOfMonth)
            .ToListAsync(ct)
            .ConfigureAwait(false)).ToHashSet();

    public async Task<List<AvailabilityTransferHintRecord>> GetHintsAsync(
        int sourceGroupId,
        CancellationToken ct = default)
    {
        var transfers = await db.Set<AvailabilityGroupDayTransferModel>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(transfer => transfer.SourceMember)
            .Include(transfer => transfer.TargetMember)
                .ThenInclude(member => member.AvailabilityGroup)
            .Where(transfer => transfer.SourceMember.AvailabilityGroupId == sourceGroupId)
            .OrderBy(transfer => transfer.SourceMember.EmployeeId)
            .ThenBy(transfer => transfer.DayOfMonth)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var targetMemberIds = transfers.Select(transfer => transfer.TargetMemberId).Distinct().ToList();
        var targetDays = await db.AvailabilityGroupDays
            .AsNoTracking()
            .Where(day => targetMemberIds.Contains(day.AvailabilityGroupMemberId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var targetDayByKey = targetDays.ToDictionary(day => (day.AvailabilityGroupMemberId, day.DayOfMonth));

        return transfers.Select(transfer =>
        {
            targetDayByKey.TryGetValue((transfer.TargetMemberId, transfer.DayOfMonth), out var targetDay);
            return new AvailabilityTransferHintRecord(
                transfer.SourceMember.EmployeeId,
                transfer.DayOfMonth,
                transfer.TargetMember.AvailabilityGroupId,
                transfer.TargetMember.AvailabilityGroup.Name,
                targetDay?.Kind ?? AvailabilityKind.NONE,
                targetDay?.IntervalStr);
        }).ToList();
    }

    public async Task<AvailabilityTransferWriteResult> TransferDaysAsync(
        int targetGroupId,
        int targetMemberId,
        int sourceGroupId,
        IReadOnlyCollection<int> dayOfMonths,
        DateTimeOffset createdAtUtc,
        CancellationToken ct = default)
    {
        var normalizedDays = dayOfMonths.Distinct().OrderBy(day => day).ToList();
        await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var targetMember = await db.AvailabilityGroupMembers
            .Include(member => member.AvailabilityGroup)
            .SingleOrDefaultAsync(member => member.Id == targetMemberId && member.AvailabilityGroupId == targetGroupId, ct)
            .ConfigureAwait(false);
        var sourceMember = await db.AvailabilityGroupMembers
            .Include(member => member.AvailabilityGroup)
            .SingleOrDefaultAsync(member => member.AvailabilityGroupId == sourceGroupId && targetMember != null && member.EmployeeId == targetMember.EmployeeId, ct)
            .ConfigureAwait(false);
        var emptyResult = BuildWriteResult(false, "The selected transfer source or target was not found.", sourceMember, targetMember, normalizedDays);
        if (targetMember is null || sourceMember is null)
        {
            return emptyResult;
        }

        if (sourceGroupId == targetGroupId ||
            sourceMember.AvailabilityGroup.Year != targetMember.AvailabilityGroup.Year ||
            sourceMember.AvailabilityGroup.Month != targetMember.AvailabilityGroup.Month)
        {
            return BuildWriteResult(false, "Availability can only be moved between different groups in the same month.", sourceMember, targetMember, normalizedDays);
        }

        var daysInMonth = DateTime.DaysInMonth(targetMember.AvailabilityGroup.Year, targetMember.AvailabilityGroup.Month);
        if (normalizedDays.Count == 0 || normalizedDays.Any(day => day < 1 || day > daysInMonth))
        {
            return BuildWriteResult(false, "Select at least one valid day from this availability month.", sourceMember, targetMember, normalizedDays);
        }

        var sourceDays = await db.AvailabilityGroupDays
            .Where(day => day.AvailabilityGroupMemberId == sourceMember.Id && normalizedDays.Contains(day.DayOfMonth))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var targetDays = await db.AvailabilityGroupDays
            .Where(day => day.AvailabilityGroupMemberId == targetMember.Id && normalizedDays.Contains(day.DayOfMonth))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingLinks = await db.Set<AvailabilityGroupDayTransferModel>()
            .Where(transfer =>
                normalizedDays.Contains(transfer.DayOfMonth) &&
                (transfer.SourceMemberId == sourceMember.Id ||
                 transfer.TargetMemberId == sourceMember.Id ||
                 transfer.SourceMemberId == targetMember.Id ||
                 transfer.TargetMemberId == targetMember.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sourceOutgoingDays = existingLinks
            .Where(transfer => transfer.SourceMemberId == sourceMember.Id)
            .Select(transfer => transfer.DayOfMonth)
            .ToHashSet();
        var blockingTargetDays = existingLinks
            .Where(transfer =>
                (transfer.SourceMemberId == targetMember.Id || transfer.TargetMemberId == targetMember.Id) &&
                !(transfer.SourceMemberId == targetMember.Id && transfer.TargetMemberId == sourceMember.Id))
            .Select(transfer => transfer.DayOfMonth)
            .ToHashSet();

        if (sourceDays.Count != normalizedDays.Count ||
            sourceDays.Any(day => day.Kind == AvailabilityKind.NONE || sourceOutgoingDays.Contains(day.DayOfMonth)))
        {
            return BuildWriteResult(false, "Only filled, available source days can be moved.", sourceMember, targetMember, normalizedDays);
        }
        if (blockingTargetDays.Count > 0 || targetDays.Any(day => day.Kind != AvailabilityKind.NONE))
        {
            return BuildWriteResult(false, "One or more selected days are already filled or transferred in the target availability.", sourceMember, targetMember, normalizedDays);
        }

        var targetDayByNumber = targetDays.ToDictionary(day => day.DayOfMonth);
        foreach (var sourceDay in sourceDays)
        {
            if (!targetDayByNumber.TryGetValue(sourceDay.DayOfMonth, out var targetDay))
            {
                targetDay = new AvailabilityGroupDayModel
                {
                    AvailabilityGroupMemberId = targetMember.Id,
                    DayOfMonth = sourceDay.DayOfMonth,
                };
                db.AvailabilityGroupDays.Add(targetDay);
            }

            targetDay.Kind = sourceDay.Kind;
            targetDay.IntervalStr = sourceDay.IntervalStr;
            sourceDay.Kind = AvailabilityKind.NONE;
            sourceDay.IntervalStr = null;

            var incomingLink = existingLinks.SingleOrDefault(transfer =>
                transfer.TargetMemberId == sourceMember.Id &&
                transfer.DayOfMonth == sourceDay.DayOfMonth);
            if (incomingLink?.SourceMemberId == targetMember.Id)
            {
                db.Set<AvailabilityGroupDayTransferModel>().Remove(incomingLink);
            }
            else if (incomingLink is not null)
            {
                incomingLink.TargetMemberId = targetMember.Id;
                incomingLink.CreatedAtUtc = createdAtUtc.ToUniversalTime();
            }
            else
            {
                db.Set<AvailabilityGroupDayTransferModel>().Add(new AvailabilityGroupDayTransferModel
                {
                    SourceMemberId = sourceMember.Id,
                    TargetMemberId = targetMember.Id,
                    DayOfMonth = sourceDay.DayOfMonth,
                    CreatedAtUtc = createdAtUtc.ToUniversalTime(),
                });
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return BuildWriteResult(true, null, sourceMember, targetMember, normalizedDays);
    }

    private static AvailabilityTransferWriteResult BuildWriteResult(
        bool succeeded,
        string? error,
        AvailabilityGroupMemberModel? sourceMember,
        AvailabilityGroupMemberModel? targetMember,
        IReadOnlyList<int> days)
        => new(
            succeeded,
            error,
            sourceMember?.AvailabilityGroupId ?? 0,
            sourceMember?.AvailabilityGroup?.Name ?? string.Empty,
            targetMember?.AvailabilityGroupId ?? 0,
            targetMember?.AvailabilityGroup?.Name ?? string.Empty,
            targetMember?.EmployeeId ?? sourceMember?.EmployeeId ?? 0,
            days);
}
