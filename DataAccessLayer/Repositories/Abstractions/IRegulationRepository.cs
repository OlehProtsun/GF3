using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

public interface IRegulationRepository : IBaseRepository<RegulationDocumentModel>
{
    Task<List<RegulationDocumentModel>> ListDocumentsAsync(CancellationToken ct = default);
    Task<List<RegulationDocumentModel>> ListPendingAsync(string role, int accountId, CancellationToken ct = default);
    Task<List<RegulationAcceptanceModel>> ListAcceptancesAsync(CancellationToken ct = default);
    Task<List<RegulationAcceptanceModel>> ListAcceptancesAsync(string role, int accountId, CancellationToken ct = default);
    Task<bool> VersionExistsAsync(string version, int? exceptDocumentId = null, CancellationToken ct = default);
    Task<bool> HasAcceptancesAsync(int documentId, CancellationToken ct = default);
    Task<RegulationAcceptanceModel> AddAcceptanceAsync(RegulationAcceptanceModel acceptance, CancellationToken ct = default);
}
