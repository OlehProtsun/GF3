using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Availability;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public sealed class AvailabilityGroupTransferService(
    IAvailabilityGroupRepository groupRepository,
    IAvailabilityGroupMemberRepository memberRepository,
    IAvailabilityGroupTransferRepository transferRepository) : IAvailabilityGroupTransferService
{
    public async Task<List<AvailabilityTransferSourceModel>> GetSourcesAsync(
        int targetGroupId,
        int targetMemberId,
        CancellationToken ct = default)
    {
        await EnsureTargetMemberAsync(targetGroupId, targetMemberId, ct).ConfigureAwait(false);
        var sources = await transferRepository.GetSourcesAsync(targetGroupId, targetMemberId, ct).ConfigureAwait(false);
        return sources.Select(source => new AvailabilityTransferSourceModel
        {
            GroupId = source.GroupId,
            GroupName = source.GroupName,
            MemberId = source.MemberId,
            EmployeeId = source.EmployeeId,
            Days = source.Days.Select(day => new AvailabilityTransferSourceDayModel
            {
                DayOfMonth = day.DayOfMonth,
                Kind = (AvailabilityKind)(int)day.Kind,
                IntervalStr = day.IntervalStr,
                CanTransfer = day.CanTransfer,
            }).ToList(),
        }).ToList();
    }

    public async Task<List<AvailabilityTransferSourceModel>> GetPreviewSourcesAsync(
        IReadOnlyCollection<int> employeeIds,
        int year,
        int month,
        int? targetGroupId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(employeeIds);
        var normalizedEmployeeIds = employeeIds
            .Where(employeeId => employeeId > 0)
            .Distinct()
            .ToList();
        if (normalizedEmployeeIds.Count == 0)
        {
            return [];
        }
        if (month is < 1 or > 12)
        {
            throw ValidationException.ForField("month", "Month must be between 1 and 12.");
        }
        if (year is < 2000 or > 2100)
        {
            throw ValidationException.ForField("year", "Year must be between 2000 and 2100.");
        }
        if (targetGroupId is int persistedTargetGroupId)
        {
            await EnsureGroupAsync(persistedTargetGroupId, ct).ConfigureAwait(false);
        }

        var sources = await transferRepository
            .GetPreviewSourcesAsync(normalizedEmployeeIds, year, month, targetGroupId, ct)
            .ConfigureAwait(false);
        return sources.Select(source => new AvailabilityTransferSourceModel
        {
            GroupId = source.GroupId,
            GroupName = source.GroupName,
            MemberId = source.MemberId,
            EmployeeId = source.EmployeeId,
            Days = source.Days.Select(day => new AvailabilityTransferSourceDayModel
            {
                DayOfMonth = day.DayOfMonth,
                Kind = (AvailabilityKind)(int)day.Kind,
                IntervalStr = day.IntervalStr,
                CanTransfer = day.CanTransfer,
            }).ToList(),
        }).ToList();
    }

    public async Task<List<AvailabilityTransferHintModel>> GetHintsAsync(
        int sourceGroupId,
        CancellationToken ct = default)
    {
        await EnsureGroupAsync(sourceGroupId, ct).ConfigureAwait(false);
        var hints = await transferRepository.GetHintsAsync(sourceGroupId, ct).ConfigureAwait(false);
        return hints.Select(hint => new AvailabilityTransferHintModel
        {
            EmployeeId = hint.EmployeeId,
            DayOfMonth = hint.DayOfMonth,
            TargetGroupId = hint.TargetGroupId,
            TargetGroupName = hint.TargetGroupName,
            Kind = (AvailabilityKind)(int)hint.Kind,
            IntervalStr = hint.IntervalStr,
        }).ToList();
    }

    public async Task<AvailabilityTransferResultModel> TransferDaysAsync(
        int targetGroupId,
        int targetMemberId,
        int sourceGroupId,
        IReadOnlyCollection<int> dayOfMonths,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dayOfMonths);
        await EnsureTargetMemberAsync(targetGroupId, targetMemberId, ct).ConfigureAwait(false);
        await EnsureGroupAsync(sourceGroupId, ct).ConfigureAwait(false);

        var result = await transferRepository.TransferDaysAsync(
            targetGroupId,
            targetMemberId,
            sourceGroupId,
            dayOfMonths,
            DateTimeOffset.UtcNow,
            ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw ValidationException.ForField("dayOfMonths", result.Error ?? "The selected availability days could not be moved.");
        }

        return new AvailabilityTransferResultModel
        {
            SourceGroupId = result.SourceGroupId,
            SourceGroupName = result.SourceGroupName,
            TargetGroupId = result.TargetGroupId,
            TargetGroupName = result.TargetGroupName,
            EmployeeId = result.EmployeeId,
            DayOfMonths = result.DayOfMonths.ToList(),
        };
    }

    private async Task EnsureGroupAsync(int groupId, CancellationToken ct)
    {
        if (await groupRepository.GetByIdAsync(groupId, ct).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException($"Availability group with id {groupId} was not found.");
        }
    }

    private async Task EnsureTargetMemberAsync(int groupId, int memberId, CancellationToken ct)
    {
        await EnsureGroupAsync(groupId, ct).ConfigureAwait(false);
        var member = await memberRepository.GetByIdAsync(memberId, ct).ConfigureAwait(false);
        if (member is null || member.AvailabilityGroupId != groupId)
        {
            throw new KeyNotFoundException($"Availability group member with id {memberId} was not found in group {groupId}.");
        }
    }
}
