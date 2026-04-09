namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Export operations for schedule graphs and containers in SQL/CSV-oriented formats.
/// </summary>
public interface IGraphExportService
{
    /// <summary>
    /// Exports one persisted graph as a SQL script.
    /// </summary>
    Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);

    /// <summary>
    /// Exports one persisted graph as an Excel-friendly CSV payload.
    /// </summary>
    Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);

    /// <summary>
    /// Exports all graphs of a container as a SQL script.
    /// </summary>
    Task<byte[]> ExportContainerSqlAsync(int containerId, bool includeEmployees, bool includeStyles, CancellationToken ct = default);
}
