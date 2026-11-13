using System.ComponentModel.DataAnnotations;
using System.Globalization;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for availability groups.
/// Availability data is often consumed as a rich object graph, so this repository exposes both
/// lightweight list queries and fully expanded reads for edit/generation flows.
/// </summary>
public class AvailabilityGroupRepository : GenericRepository<AvailabilityGroupModel>, IAvailabilityGroupRepository
{
    private readonly IAvailabilityGroupTransferRepository? _transferRepository;

    public AvailabilityGroupRepository(AppDbContext db, IAvailabilityGroupTransferRepository? transferRepository = null)
        : base(db)
    {
        _transferRepository = transferRepository;
    }

    /// <inheritdoc />
    public override async Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
        => await CreateListQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await GetAllAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);
        var hasNumericValue = int.TryParse(normalized, out var numericValue);

        return await CreateListQuery()
            .Where(group =>
                group.Name.ToLower().Contains(normalized) ||
                group.Members.Any(member =>
                    member.Employee.FirstName.ToLower().Contains(normalized) ||
                    member.Employee.LastName.ToLower().Contains(normalized)) ||
                (hasNumericValue && (group.Year == numericValue || group.Month == numericValue || group.Id == numericValue)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .AsSplitQuery()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee)
            .Include(group => group.Members)
                .ThenInclude(member => member.Days)
            .SingleOrDefaultAsync(group => group.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<AvailabilityGroupModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default)
    {
        var groups = await CreatePublishedEmployeeQuery(employeeId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return groups
            .Where(group => HasStartedVisibility(group, nowUtc))
            .OrderBy(group => IsClosedForSubmission(group, nowUtc))
            .ThenBy(group => group.VisibleToUtc)
            .ThenBy(group => group.Year)
            .ThenBy(group => group.Month)
            .ThenBy(group => group.Name)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AvailabilityGroupModel?> GetPublishedForEmployeeByIdAsync(
        int id,
        int employeeId,
        DateTimeOffset nowUtc,
        CancellationToken ct = default)
    {
        var group = await CreatePublishedEmployeeQuery(employeeId)
            .SingleOrDefaultAsync(group => group.Id == id, ct)
            .ConfigureAwait(false);

        return group is not null && HasStartedVisibility(group, nowUtc) ? group : null;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, int year, int month, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = NormalizeSearchValue(name);

        return _set.AsNoTracking().AnyAsync(group =>
            (!excludeId.HasValue || group.Id != excludeId.Value) &&
            group.Year == year &&
            group.Month == month &&
            group.Name.ToLower().Trim() == normalized,
            ct);
    }

    public async Task<AvailabilityGroupSnapshotWriteResult> SaveSnapshotAsync(
        AvailabilityGroupModel group,
        IReadOnlyList<AvailabilityGroupSnapshotMember> members,
        IReadOnlyList<AvailabilityGroupSnapshotTransfer> transfers,
        CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var target = group;
            if (group.Id > 0)
            {
                target = await _set.Include(item => item.Members)
                    .SingleOrDefaultAsync(item => item.Id == group.Id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Availability group with id {group.Id} was not found.");
            }

            // Repeat the name check under the write transaction so concurrent creates cannot bypass it.
            if (await ExistsByNameAsync(group.Name, group.Year, group.Month, group.Id, ct).ConfigureAwait(false))
                throw InvalidSnapshot(nameof(group.Name), "An availability group with the same name already exists for this month.");

            var requestedEmployeeIds = members.Select(member => member.EmployeeId).ToHashSet();
            var employeeCount = await _db.Employees.CountAsync(employee => requestedEmployeeIds.Contains(employee.Id), ct)
                .ConfigureAwait(false);
            if (employeeCount != requestedEmployeeIds.Count)
                throw InvalidSnapshot(nameof(AvailabilityGroupMemberModel.EmployeeId), "One or more selected employees were not found.");

            if (group.Id == 0)
            {
                _set.Add(target);
            }
            else
            {
                target.Name = group.Name;
                target.Year = group.Year;
                target.Month = group.Month;
                target.PublicationStatus = group.PublicationStatus;
                target.VisibleFromUtc = group.VisibleFromUtc;
                target.VisibleToUtc = group.VisibleToUtc;
            }

            var membersByEmployee = target.Members.ToDictionary(member => member.EmployeeId);
            for (var displayOrder = 0; displayOrder < members.Count; displayOrder++)
            {
                var employeeId = members[displayOrder].EmployeeId;
                if (!membersByEmployee.TryGetValue(employeeId, out var member))
                {
                    member = new AvailabilityGroupMemberModel { EmployeeId = employeeId };
                    target.Members.Add(member);
                    membersByEmployee.Add(employeeId, member);
                }

                member.DisplayOrder = displayOrder;
            }

            var changed = await _db.SaveChangesAsync(ct).ConfigureAwait(false) > 0;
            var sourceGroupIds = await ApplySnapshotTransfersAsync(target.Id, transfers, membersByEmployee, ct)
                .ConfigureAwait(false);

            var targetDays = await _db.AvailabilityGroupDays
                .Where(day => day.AvailabilityGroupMember.AvailabilityGroupId == target.Id)
                .ToListAsync(ct).ConfigureAwait(false);
            var daysByKey = targetDays.ToDictionary(day => (day.AvailabilityGroupMemberId, day.DayOfMonth));

            var transferredDays = transfers.SelectMany(transfer =>
                transfer.DayOfMonths.Select(day => (transfer.EmployeeId, DayOfMonth: day))).ToHashSet();
            foreach (var member in members)
            {
                var memberId = membersByEmployee[member.EmployeeId].Id;
                foreach (var day in member.Days)
                {
                    var found = daysByKey.Remove((memberId, day.DayOfMonth), out var existing);
                    if (transferredDays.Contains((member.EmployeeId, day.DayOfMonth)) &&
                        (!found || !MatchesTransferredDay(existing!, day)))
                    {
                        throw InvalidSnapshot("dayOfMonths", "The selected source availability changed. Review the transfer before saving.");
                    }

                    if (!found)
                    {
                        day.AvailabilityGroupMemberId = memberId;
                        _db.AvailabilityGroupDays.Add(day);
                    }
                    else
                    {
                        existing!.Kind = day.Kind;
                        existing.IntervalStr = day.IntervalStr;
                    }
                }
            }

            _db.AvailabilityGroupDays.RemoveRange(daysByKey.Values);
            _db.AvailabilityGroupMembers.RemoveRange(
                membersByEmployee.Values.Where(member => !requestedEmployeeIds.Contains(member.EmployeeId)));
            changed |= await _db.SaveChangesAsync(ct).ConfigureAwait(false) > 0;

            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return new AvailabilityGroupSnapshotWriteResult(target.Id, sourceGroupIds, changed || sourceGroupIds.Count > 0);
        }
        catch
        {
            // Earlier SaveChanges calls may have accepted rows that the transaction will roll back.
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<List<int>> ApplySnapshotTransfersAsync(
        int targetGroupId,
        IReadOnlyList<AvailabilityGroupSnapshotTransfer> transfers,
        IReadOnlyDictionary<int, AvailabilityGroupMemberModel> membersByEmployee,
        CancellationToken ct)
    {
        var sourceGroupIds = transfers.Select(transfer => transfer.SourceGroupId).Distinct().ToList();
        if (sourceGroupIds.Count == 0)
            return sourceGroupIds;

        var transferRepository = _transferRepository
            ?? throw new InvalidOperationException("Availability graph transfers require a transfer repository.");
        var existingSourceIds = await _set.Where(group => sourceGroupIds.Contains(group.Id))
            .Select(group => group.Id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var sourceId in sourceGroupIds.Except(existingSourceIds))
            throw new KeyNotFoundException($"Availability group with id {sourceId} was not found.");

        foreach (var transfer in transfers)
        {
            var result = await transferRepository.TransferDaysAsync(
                targetGroupId, membersByEmployee[transfer.EmployeeId].Id, transfer.SourceGroupId,
                transfer.DayOfMonths, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
            if (!result.Succeeded)
                throw InvalidSnapshot("dayOfMonths", result.Error ?? "The selected availability days could not be moved.");
        }

        return sourceGroupIds;
    }

    private static bool MatchesTransferredDay(AvailabilityGroupDayModel actual, AvailabilityGroupDayModel requested)
    {
        if (actual.Kind != requested.Kind)
            return false;
        if (actual.IntervalStr == requested.IntervalStr)
            return true;
        if (actual.Kind != AvailabilityKind.INT || actual.IntervalStr is null || requested.IntervalStr is null)
            return false;

        // Older granular writes can store the same times with different spacing or hour padding.
        var actualTimes = actual.IntervalStr.Split('-', StringSplitOptions.TrimEntries);
        var requestedTimes = requested.IntervalStr.Split('-', StringSplitOptions.TrimEntries);
        return actualTimes.Length == 2 && requestedTimes.Length == 2 &&
            SameTime(actualTimes[0], requestedTimes[0]) && SameTime(actualTimes[1], requestedTimes[1]);
    }

    private static bool SameTime(string actual, string requested)
        => TimeOnly.TryParseExact(actual, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var actualTime) &&
            TimeOnly.TryParseExact(requested, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var requestedTime) &&
            actualTime == requestedTime;

    private static ValidationException InvalidSnapshot(string field, string message)
        => new(new ValidationResult(message, [field]), null, null);

    private IQueryable<AvailabilityGroupModel> CreateListQuery()
        => _set
            .AsNoTracking()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee);

    private IQueryable<AvailabilityGroupModel> CreatePublishedEmployeeQuery(int employeeId)
        => _set
            .AsNoTracking()
            .AsSplitQuery()
            .Include(group => group.Members)
                .ThenInclude(member => member.Employee)
            .Include(group => group.Members)
                .ThenInclude(member => member.Days)
            .Where(group =>
                group.PublicationStatus == AvailabilityPublicationStatus.Public &&
                group.Members.Any(member => member.EmployeeId == employeeId));

    private static bool HasStartedVisibility(AvailabilityGroupModel group, DateTimeOffset nowUtc)
    {
        var normalizedNowUtc = nowUtc.ToUniversalTime();
        return !group.VisibleFromUtc.HasValue || group.VisibleFromUtc.Value <= normalizedNowUtc;
    }

    private static bool IsClosedForSubmission(AvailabilityGroupModel group, DateTimeOffset nowUtc)
    {
        var normalizedNowUtc = nowUtc.ToUniversalTime();
        return group.VisibleToUtc.HasValue && group.VisibleToUtc.Value < normalizedNowUtc;
    }

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
