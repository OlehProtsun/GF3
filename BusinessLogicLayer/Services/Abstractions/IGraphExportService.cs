namespace BusinessLogicLayer.Services.Abstractions;

public interface IGraphExportService
{
    Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);
    Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);
    Task<byte[]> ExportContainerSqlAsync(int containerId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);
}
