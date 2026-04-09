namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Excel-template export operations for schedule graphs and containers.
/// </summary>
public interface IGraphTemplateExportService
{
    /// <summary>
    /// Exports one persisted graph into an XLSX workbook together with the produced file name.
    /// </summary>
    Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(
        int containerId,
        int graphId,
        bool includeStyles,
        bool includeEmployees,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a whole container into an XLSX workbook together with the produced file name.
    /// </summary>
    Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(
        int containerId,
        bool includeStyles,
        bool includeEmployees,
        CancellationToken ct = default);
}
