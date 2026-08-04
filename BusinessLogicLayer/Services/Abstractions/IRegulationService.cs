using BusinessLogicLayer.Contracts.Regulations;

namespace BusinessLogicLayer.Services.Abstractions;

public interface IRegulationService
{
    Task<IReadOnlyList<RegulationDocumentDto>> ListDocumentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RegulationDocumentDto>> ListPendingAsync(RegulationSubject subject, CancellationToken ct = default);
    Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(string role, int accountId, CancellationToken ct = default);
    Task<RegulationDocumentDto> CreateAsync(SaveRegulationDocumentRequest request, int? managerId, string managerName, CancellationToken ct = default);
    Task<RegulationDocumentDto> UpdateAsync(int documentId, SaveRegulationDocumentRequest request, CancellationToken ct = default);
    Task<RegulationDocumentDto> PublishAsync(int documentId, CancellationToken ct = default);
    Task DeleteAsync(int documentId, CancellationToken ct = default);
    Task<RegulationAcceptanceDto> AcceptAsync(int documentId, RegulationSubject subject, CancellationToken ct = default);
    Task<RegulationPdfDto> GetPdfAsync(int documentId, bool requirePublished, CancellationToken ct = default);
}
