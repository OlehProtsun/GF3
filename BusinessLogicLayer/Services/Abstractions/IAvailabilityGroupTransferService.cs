using BusinessLogicLayer.Contracts.Availability;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IAvailabilityGroupTransferService
{
    Task<List<AvailabilityTransferSourceModel>> GetSourcesAsync(
        int targetGroupId,
        int targetMemberId,
        CancellationToken ct = default);

    Task<List<AvailabilityTransferSourceModel>> GetPreviewSourcesAsync(
        IReadOnlyCollection<int> employeeIds,
        int year,
        int month,
        int? targetGroupId,
        CancellationToken ct = default);

    Task<List<AvailabilityTransferHintModel>> GetHintsAsync(
        int sourceGroupId,
        CancellationToken ct = default);

    Task<AvailabilityTransferResultModel> TransferDaysAsync(
        int targetGroupId,
        int targetMemberId,
        int sourceGroupId,
        IReadOnlyCollection<int> dayOfMonths,
        CancellationToken ct = default);
}
