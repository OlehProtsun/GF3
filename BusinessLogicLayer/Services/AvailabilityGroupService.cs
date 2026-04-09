using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Owns the availability-group aggregate:
/// group metadata, employee membership, and day-by-day availability slots.
/// The main goal of this service is to make nested edits safe by validating ownership,
/// uniqueness, and payload shape before anything reaches the database layer.
/// </summary>
public class AvailabilityGroupService : IAvailabilityGroupService
{
    private sealed record SaveGroupPayloadItem(int EmployeeId, IReadOnlyList<AvailabilityGroupDayModel> Days);

    private readonly IAvailabilityGroupRepository _groupRepo;
    private readonly IAvailabilityGroupMemberRepository _memberRepo;
    private readonly IAvailabilityGroupDayRepository _dayRepo;

    public AvailabilityGroupService(
        IAvailabilityGroupRepository groupRepo,
        IAvailabilityGroupMemberRepository memberRepo,
        IAvailabilityGroupDayRepository dayRepo)
    {
        _groupRepo = groupRepo;
        _memberRepo = memberRepo;
        _dayRepo = dayRepo;
    }

    public async Task<AvailabilityGroupModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _groupRepo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_groupRepo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<AvailabilityGroupModel> CreateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
    {
        await ValidateGroupAsync(entity, excludeId: null, ct).ConfigureAwait(false);
        return await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _groupRepo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
    {
        await ValidateGroupAsync(entity, entity.Id, ct).ConfigureAwait(false);
        await _groupRepo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _groupRepo.DeleteAsync(id, ct);

    public async Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _groupRepo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
        LoadFullAsync(int groupId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        var full = await _groupRepo.GetFullByIdAsync(groupId, readCt).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"AvailabilityGroup with Id={groupId} not found.");

        var members = (full.Members?.Select(m => m.ToContract()).ToList() ?? new List<AvailabilityGroupMemberModel>())
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Employee?.FirstName ?? string.Empty)
            .ThenBy(m => m.Employee?.LastName ?? string.Empty)
            .ThenBy(m => m.EmployeeId)
            .ToList();

        var days = (await _dayRepo.GetByGroupIdAsync(groupId, readCt).ConfigureAwait(false))
            .Select(d => d.ToContract())
            .ToList();

        return (full.ToContract(), members, days);
    }

    public async Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        await EnsureGroupExistsAsync(groupId, readCt).ConfigureAwait(false);

        return (await _memberRepo.GetByGroupIdAsync(groupId, readCt).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();
    }

    public async Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        await EnsureUniqueMemberAsync(groupId, model.EmployeeId, excludeMemberId: null, ct).ConfigureAwait(false);

        model.AvailabilityGroupId = groupId;
        var created = await _memberRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        await EnsureMemberBelongsToGroupAsync(groupId, memberId, ct).ConfigureAwait(false);
        await EnsureUniqueMemberAsync(groupId, model.EmployeeId, memberId, ct).ConfigureAwait(false);

        model.Id = memberId;
        model.AvailabilityGroupId = groupId;
        await _memberRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        await EnsureMemberBelongsToGroupAsync(groupId, memberId, ct).ConfigureAwait(false);
        await _memberRepo.DeleteAsync(memberId, ct).ConfigureAwait(false);
    }

    public async Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)
    {
        var readCt = ServiceMappingHelper.NormalizeReadCancellationToken(ct);
        await EnsureGroupExistsAsync(groupId, readCt).ConfigureAwait(false);

        return (await _dayRepo.GetByGroupIdAsync(groupId, readCt).ConfigureAwait(false))
            .Select(x => x.ToContract())
            .ToList();
    }

    public async Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        NormalizeDayModel(model);
        ValidateDayModel(model);
        await EnsureMemberBelongsToGroupAsync(groupId, model.AvailabilityGroupMemberId, ct).ConfigureAwait(false);
        await EnsureUniqueDayAsync(model.AvailabilityGroupMemberId, model.DayOfMonth, excludeDayId: null, ct).ConfigureAwait(false);

        var created = await _dayRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        var existing = await _dayRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability slot with id {slotId} was not found.");

        NormalizeDayModel(model);
        ValidateDayModel(model);
        await EnsureMemberBelongsToGroupAsync(groupId, existing.AvailabilityGroupMemberId, ct).ConfigureAwait(false);
        await EnsureMemberBelongsToGroupAsync(groupId, model.AvailabilityGroupMemberId, ct).ConfigureAwait(false);
        await EnsureUniqueDayAsync(model.AvailabilityGroupMemberId, model.DayOfMonth, slotId, ct).ConfigureAwait(false);

        model.Id = slotId;
        await _dayRepo.UpdateAsync(model.ToDal(), ct).ConfigureAwait(false);
    }

    public async Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);

        var existing = await _dayRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability slot with id {slotId} was not found.");

        await EnsureMemberBelongsToGroupAsync(groupId, existing.AvailabilityGroupMemberId, ct).ConfigureAwait(false);
        await _dayRepo.DeleteAsync(slotId, ct).ConfigureAwait(false);
    }

    public async Task SaveGroupAsync(
        AvailabilityGroupModel group,
        IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(payload);

        await ValidateGroupAsync(group, group.Id == 0 ? null : group.Id, ct).ConfigureAwait(false);
        var normalizedPayload = NormalizePayload(payload);
        var groupId = await UpsertGroupAsync(group, ct).ConfigureAwait(false);
        var memberByEmployee = await LoadMembersByEmployeeAsync(groupId, ct).ConfigureAwait(false);

        await RemoveMembersMissingFromPayloadAsync(memberByEmployee.Values, normalizedPayload, ct).ConfigureAwait(false);
        await UpsertMembersAndDaysAsync(groupId, normalizedPayload, memberByEmployee, ct).ConfigureAwait(false);
    }

    private async Task<int> UpsertGroupAsync(AvailabilityGroupModel group, CancellationToken ct)
    {
        if (group.Id == 0)
        {
            var created = await _groupRepo.AddAsync(group.ToDal(), ct).ConfigureAwait(false);
            group.Id = created.Id;
        }
        else
        {
            await _groupRepo.UpdateAsync(group.ToDal(), ct).ConfigureAwait(false);
        }

        return group.Id;
    }

    private async Task<Dictionary<int, DataAccessLayer.Models.AvailabilityGroupMemberModel>> LoadMembersByEmployeeAsync(int groupId, CancellationToken ct)
    {
        var existingMembers = await _memberRepo.GetByGroupIdAsync(groupId, ct).ConfigureAwait(false);
        return existingMembers.ToDictionary(member => member.EmployeeId);
    }

    private async Task RemoveMembersMissingFromPayloadAsync(
        IEnumerable<DataAccessLayer.Models.AvailabilityGroupMemberModel> existingMembers,
        IReadOnlyList<SaveGroupPayloadItem> normalizedPayload,
        CancellationToken ct)
    {
        var desiredEmployeeIds = normalizedPayload.Select(item => item.EmployeeId).ToHashSet();

        foreach (var member in existingMembers)
        {
            if (!desiredEmployeeIds.Contains(member.EmployeeId))
            {
                await _memberRepo.DeleteAsync(member.Id, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// SaveGroup is intentionally destructive for day slots: the incoming payload is the source of truth.
    /// That keeps the update algorithm predictable for the client, because it does not have to calculate
    /// fine-grained add/update/delete patches for nested day entries.
    /// </summary>
    private async Task UpsertMembersAndDaysAsync(
        int groupId,
        IReadOnlyList<SaveGroupPayloadItem> normalizedPayload,
        Dictionary<int, DataAccessLayer.Models.AvailabilityGroupMemberModel> memberByEmployee,
        CancellationToken ct)
    {
        foreach (var (item, displayOrder) in normalizedPayload.Select((value, index) => (value, index)))
        {
            var member = await UpsertMemberAsync(groupId, item.EmployeeId, displayOrder, memberByEmployee, ct).ConfigureAwait(false);
            await ReplaceMemberDaysAsync(member.Id, item.Days, ct).ConfigureAwait(false);
        }
    }

    private async Task<DataAccessLayer.Models.AvailabilityGroupMemberModel> UpsertMemberAsync(
        int groupId,
        int employeeId,
        int displayOrder,
        IDictionary<int, DataAccessLayer.Models.AvailabilityGroupMemberModel> memberByEmployee,
        CancellationToken ct)
    {
        if (!memberByEmployee.TryGetValue(employeeId, out var member))
        {
            member = await _memberRepo.AddAsync(new DataAccessLayer.Models.AvailabilityGroupMemberModel
            {
                Id = 0,
                AvailabilityGroupId = groupId,
                EmployeeId = employeeId,
                DisplayOrder = displayOrder,
            }, ct).ConfigureAwait(false);

            memberByEmployee[employeeId] = member;
            return member;
        }

        if (member.DisplayOrder == displayOrder)
        {
            return member;
        }

        member = new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            Id = member.Id,
            AvailabilityGroupId = groupId,
            EmployeeId = employeeId,
            DisplayOrder = displayOrder,
        };

        memberByEmployee[employeeId] = member;
        await _memberRepo.UpdateAsync(member, ct).ConfigureAwait(false);
        return member;
    }

    private async Task ReplaceMemberDaysAsync(int memberId, IReadOnlyList<AvailabilityGroupDayModel> days, CancellationToken ct)
    {
        await _dayRepo.DeleteByMemberIdAsync(memberId, ct).ConfigureAwait(false);

        var dalDays = days.Select(day => day.ToDal()).ToList();
        foreach (var day in dalDays)
        {
            day.Id = 0;
            day.AvailabilityGroupMemberId = memberId;
        }

        if (dalDays.Count > 0)
        {
            await _dayRepo.AddRangeAsync(dalDays, ct).ConfigureAwait(false);
        }
    }

    private async Task ValidateGroupAsync(AvailabilityGroupModel entity, int? excludeId, CancellationToken ct)
    {
        entity.Name = (entity.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw ValidationException.ForField(nameof(AvailabilityGroupModel.Name), "Availability group name is required.");

        if (entity.Month is < 1 or > 12)
            throw ValidationException.ForField(nameof(AvailabilityGroupModel.Month), "Month must be between 1 and 12.");

        if (await _groupRepo.ExistsByNameAsync(entity.Name, entity.Year, entity.Month, excludeId, ct).ConfigureAwait(false))
            throw ValidationException.ForField(nameof(AvailabilityGroupModel.Name), "An availability group with the same name already exists for this month.");
    }

    private async Task EnsureUniqueMemberAsync(int groupId, int employeeId, int? excludeMemberId, CancellationToken ct)
    {
        if (employeeId <= 0)
            throw ValidationException.ForField(nameof(AvailabilityGroupMemberModel.EmployeeId), "Employee is required.");

        var existing = await _memberRepo.GetByGroupAndEmployeeAsync(groupId, employeeId, ct).ConfigureAwait(false);
        if (existing is not null && (!excludeMemberId.HasValue || existing.Id != excludeMemberId.Value))
        {
            throw ValidationException.ForField(nameof(AvailabilityGroupMemberModel.EmployeeId), "This employee is already added to the availability group.");
        }
    }

    private async Task EnsureUniqueDayAsync(int memberId, int dayOfMonth, int? excludeDayId, CancellationToken ct)
    {
        var existingDays = await _dayRepo.GetByMemberIdAsync(memberId, ct).ConfigureAwait(false);
        var duplicateExists = existingDays.Any(day => day.DayOfMonth == dayOfMonth && (!excludeDayId.HasValue || day.Id != excludeDayId.Value));
        if (duplicateExists)
        {
            throw ValidationException.ForField(nameof(AvailabilityGroupDayModel.DayOfMonth), "A slot for this day already exists for the selected employee.");
        }
    }

    /// <summary>
    /// Converts the loosely typed UI payload into a canonical backend representation.
    /// Every employee may appear only once, and inside that employee each day may appear only once.
    /// We also normalize each day entry before it is ever persisted.
    /// </summary>
    private static List<SaveGroupPayloadItem> NormalizePayload(IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload)
    {
        var seenEmployees = new HashSet<int>();
        var normalized = new List<SaveGroupPayloadItem>(payload.Count);

        foreach (var (employeeId, days) in payload)
        {
            if (employeeId <= 0)
            {
                throw ValidationException.ForField(nameof(AvailabilityGroupMemberModel.EmployeeId), "Employee is required.");
            }

            if (!seenEmployees.Add(employeeId))
            {
                throw ValidationException.ForField(nameof(AvailabilityGroupMemberModel.EmployeeId), "This employee is already added to the availability group.");
            }

            var normalizedDays = new List<AvailabilityGroupDayModel>();
            var seenDays = new HashSet<int>();

            foreach (var day in days ?? Array.Empty<AvailabilityGroupDayModel>())
            {
                ArgumentNullException.ThrowIfNull(day);

                var normalizedDay = new AvailabilityGroupDayModel
                {
                    Id = 0,
                    AvailabilityGroupMemberId = 0,
                    DayOfMonth = day.DayOfMonth,
                    Kind = day.Kind,
                    IntervalStr = day.IntervalStr,
                };

                NormalizeDayModel(normalizedDay);
                ValidateDayModel(normalizedDay);

                if (!seenDays.Add(normalizedDay.DayOfMonth))
                {
                    throw ValidationException.ForField(nameof(AvailabilityGroupDayModel.DayOfMonth), "A slot for this day already exists for the selected employee.");
                }

                normalizedDays.Add(normalizedDay);
            }

            normalized.Add(new SaveGroupPayloadItem(employeeId, normalizedDays));
        }

        return normalized;
    }

    private static void NormalizeDayModel(AvailabilityGroupDayModel model)
    {
        model.IntervalStr = string.IsNullOrWhiteSpace(model.IntervalStr)
            ? null
            : model.IntervalStr.Trim();

        if (model.Kind == AvailabilityKind.ANY)
        {
            model.IntervalStr = null;
        }
    }

    private static void ValidateDayModel(AvailabilityGroupDayModel model)
    {
        if (model.DayOfMonth is < 1 or > 31)
        {
            throw ValidationException.ForField(nameof(AvailabilityGroupDayModel.DayOfMonth), "Day of month must be between 1 and 31.");
        }

        if (model.Kind == AvailabilityKind.INT && string.IsNullOrWhiteSpace(model.IntervalStr))
        {
            throw ValidationException.ForField(nameof(AvailabilityGroupDayModel.IntervalStr), "Interval is required for interval availability.");
        }
    }

    private async Task EnsureGroupExistsAsync(int groupId, CancellationToken ct)
    {
        if (await _groupRepo.GetByIdAsync(groupId, ct).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException($"Availability group with id {groupId} was not found.");
        }
    }

    private async Task EnsureMemberBelongsToGroupAsync(int groupId, int memberId, CancellationToken ct)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability group member with id {memberId} was not found.");

        if (member.AvailabilityGroupId != groupId)
        {
            throw new KeyNotFoundException($"Availability group member with id {memberId} was not found in group {groupId}.");
        }
    }
}
