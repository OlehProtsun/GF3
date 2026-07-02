using BusinessLogicLayer.Contracts.Communications;

namespace BusinessLogicLayer.Services.Abstractions;

public interface ICommunicationService
{
    Task<IReadOnlyList<CommunicationMessageDto>> ListForManagerAsync(CancellationToken ct = default);

    Task<CommunicationMessageDto> CreateAsync(
        CreateCommunicationMessageRequest request,
        int? managerId,
        string? managerDisplayName,
        CancellationToken ct = default);

    Task<CommunicationMessageDto> UpdateAsync(
        int communicationId,
        UpdateCommunicationMessageRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(int communicationId, CancellationToken ct = default);

    Task<IReadOnlyList<CommunicationMessageDto>> GetPendingForEmployeeAsync(
        int employeeId,
        CancellationToken ct = default);

    Task DismissForEmployeeAsync(int employeeId, int communicationId, CancellationToken ct = default);
}
