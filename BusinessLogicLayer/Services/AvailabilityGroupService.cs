using BusinessLogicLayer.Contracts.Enums;
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

                if (d.Kind != AvailabilityKind.INT.ToDal())
                    d.IntervalStr = null;
            }

            await _dayRepo.AddRangeAsync(dalDays, ct).ConfigureAwait(false);
        }
    }
}
