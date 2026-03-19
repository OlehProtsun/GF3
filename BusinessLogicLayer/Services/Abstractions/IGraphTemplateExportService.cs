namespace BusinessLogicLayer.Services.Abstractions;

public interface IGraphTemplateExportService
{
    Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default);
    Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default);
}