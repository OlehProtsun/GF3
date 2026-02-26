using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public class AvailabilityGroupService : IAvailabilityGroupService
{
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
        => await ServiceMappingHelper.CreateMappedAsync(entity.ToDal(), _groupRepo.AddAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public Task UpdateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
        => _groupRepo.UpdateAsync(entity.ToDal(), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _groupRepo.DeleteAsync(id, ct);

    public async Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _groupRepo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
        LoadFullAsync(int groupId, CancellationToken ct = default)
    {
        var full = await _groupRepo.GetFullByIdAsync(groupId, ct).ConfigureAwait(false)
                   ?? throw new InvalidOperationException($"AvailabilityGroup with Id={groupId} not found.");

        var members = full.Members?.Select(m => m.ToContract()).ToList() ?? new List<AvailabilityGroupMemberModel>();
        var days = (await _dayRepo.GetByGroupIdAsync(groupId, ct).ConfigureAwait(false)).Select(d => d.ToContract()).ToList();
        return (full.ToContract(), members, days);
    }

    public async Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        return (await _memberRepo.GetByGroupIdAsync(groupId, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        model.AvailabilityGroupId = groupId;
        var created = await _memberRepo.AddAsync(new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = model.AvailabilityGroupId,
            EmployeeId = model.EmployeeId
        }, ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        var existing = await _memberRepo.GetByIdAsync(memberId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability group member with id {memberId} was not found.");

        if (existing.AvailabilityGroupId != groupId)
            throw new KeyNotFoundException($"Availability group member with id {memberId} was not found in group {groupId}.");

        await _memberRepo.UpdateAsync(new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            Id = memberId,
            AvailabilityGroupId = groupId,
            EmployeeId = model.EmployeeId
        }, ct).ConfigureAwait(false);
    }

    public async Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        var existing = await _memberRepo.GetByIdAsync(memberId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability group member with id {memberId} was not found.");

        if (existing.AvailabilityGroupId != groupId)
            throw new KeyNotFoundException($"Availability group member with id {memberId} was not found in group {groupId}.");

        await _memberRepo.DeleteAsync(memberId, ct).ConfigureAwait(false);
    }

    public async Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        return (await _dayRepo.GetByGroupIdAsync(groupId, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();
    }

    public async Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        await EnsureMemberBelongsToGroupAsync(groupId, model.AvailabilityGroupMemberId, ct).ConfigureAwait(false);

        var created = await _dayRepo.AddAsync(model.ToDal(), ct).ConfigureAwait(false);
        return created.ToContract();
    }

    public async Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)
    {
        await EnsureGroupExistsAsync(groupId, ct).ConfigureAwait(false);
        var existing = await _dayRepo.GetByIdAsync(slotId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability slot with id {slotId} was not found.");

        await EnsureMemberBelongsToGroupAsync(groupId, existing.AvailabilityGroupMemberId, ct).ConfigureAwait(false);
        await EnsureMemberBelongsToGroupAsync(groupId, model.AvailabilityGroupMemberId, ct).ConfigureAwait(false);

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
        if (group is null) throw new ArgumentNullException(nameof(group));
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        group.Name = (group.Name ?? string.Empty).Trim();

        if (await _groupRepo.ExistsByNameAsync(group.Name, group.Year, group.Month, group.Id == 0 ? null : group.Id, ct).ConfigureAwait(false))
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "An availability group with the same name already exists for this month.");

        if (group.Id == 0)
        {
            var created = await _groupRepo.AddAsync(group.ToDal(), ct).ConfigureAwait(false);
            group.Id = created.Id;
        }
        else
        {
            await _groupRepo.UpdateAsync(group.ToDal(), ct).ConfigureAwait(false);
        }

        var groupId = group.Id;
        var existingMembers = await _memberRepo.GetByGroupIdAsync(groupId, ct).ConfigureAwait(false);
        var memberByEmployee = existingMembers.ToDictionary(m => m.EmployeeId);
        var desiredEmployeeIds = payload.Select(x => x.employeeId).Distinct().ToHashSet();

        foreach (var m in existingMembers)
        {
            if (!desiredEmployeeIds.Contains(m.EmployeeId))
                await _memberRepo.DeleteAsync(m.Id, ct).ConfigureAwait(false);
        }

        foreach (var (employeeId, days) in payload)
        {
            if (!memberByEmployee.TryGetValue(employeeId, out var member))
            {
                member = await _memberRepo.AddAsync(new DataAccessLayer.Models.AvailabilityGroupMemberModel
                {
                    Id = 0,
                    AvailabilityGroupId = groupId,
                    EmployeeId = employeeId
                }, ct).ConfigureAwait(false);

                memberByEmployee[employeeId] = member;
            }

            var memberId = member.Id;
            await _dayRepo.DeleteByMemberIdAsync(memberId, ct).ConfigureAwait(false);

            var dalDays = days.Select(d => d.ToDal()).ToList();
            foreach (var d in dalDays)
            {
                d.Id = 0;
                d.AvailabilityGroupMemberId = memberId;
            }

            if (dalDays.Count > 0)
                await _dayRepo.AddRangeAsync(dalDays, ct).ConfigureAwait(false);
        }
    }

    private async Task EnsureGroupExistsAsync(int groupId, CancellationToken ct)
    {
        var exists = await _groupRepo.GetByIdAsync(groupId, ct).ConfigureAwait(false);
        if (exists is null)
            throw new KeyNotFoundException($"Availability group with id {groupId} was not found.");
    }

    private async Task EnsureMemberBelongsToGroupAsync(int groupId, int memberId, CancellationToken ct)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Availability group member with id {memberId} was not found.");

        if (member.AvailabilityGroupId != groupId)
            throw new KeyNotFoundException($"Availability group member with id {memberId} was not found in group {groupId}.");
    }
}
