using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Repositories.Abstractions;

public sealed record AvailabilityTransferSourceDayRecord(
    int DayOfMonth,
    AvailabilityKind Kind,
    string? IntervalStr,
    bool CanTransfer);

public sealed record AvailabilityTransferSourceRecord(
    int GroupId,
    string GroupName,
    int MemberId,
    int EmployeeId,
    IReadOnlyList<AvailabilityTransferSourceDayRecord> Days);

public sealed record AvailabilityTransferHintRecord(
    int EmployeeId,
    int DayOfMonth,
    int TargetGroupId,
    string TargetGroupName,
    AvailabilityKind Kind,
    string? IntervalStr);

public sealed record AvailabilityTransferWriteResult(
    bool Succeeded,
    string? Error,
    int SourceGroupId,
    string SourceGroupName,
    int TargetGroupId,
    string TargetGroupName,
    int EmployeeId,
    IReadOnlyList<int> DayOfMonths);

public interface IAvailabilityGroupTransferRepository
{
    Task<List<AvailabilityTransferSourceRecord>> GetSourcesAsync(
        int targetGroupId,
        int targetMemberId,
        CancellationToken ct = default);

    Task<List<AvailabilityTransferSourceRecord>> GetPreviewSourcesAsync(
        IReadOnlyCollection<int> employeeIds,
        int year,
        int month,
        int? targetGroupId,
        CancellationToken ct = default);

    Task<HashSet<int>> GetOutgoingDayNumbersAsync(
        int sourceMemberId,
        CancellationToken ct = default);

    Task<List<AvailabilityTransferHintRecord>> GetHintsAsync(
        int sourceGroupId,
        CancellationToken ct = default);

    Task<AvailabilityTransferWriteResult> TransferDaysAsync(
        int targetGroupId,
        int targetMemberId,
        int sourceGroupId,
        IReadOnlyCollection<int> dayOfMonths,
        DateTimeOffset createdAtUtc,
        CancellationToken ct = default);
}
