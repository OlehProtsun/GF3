using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

public interface ICommunicationRepository : IBaseRepository<CommunicationMessageModel>
{
    Task<List<CommunicationMessageModel>> GetAllForManagerAsync(CancellationToken ct = default);

    Task<List<CommunicationMessageModel>> GetPendingForEmployeeAsync(
        int employeeId,
        DateTimeOffset nowUtc,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(int communicationId, CancellationToken ct = default);

    Task<bool> DismissalExistsAsync(int communicationId, int employeeId, CancellationToken ct = default);

    Task AddDismissalAsync(EmployeeCommunicationDismissalModel dismissal, CancellationToken ct = default);
}
