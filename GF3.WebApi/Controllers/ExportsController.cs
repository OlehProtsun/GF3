using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public sealed class ExportsController : ControllerBase
{
    private readonly IGraphExportService _graphExportService;
    private readonly IGraphTemplateExportService _graphTemplateExportService;

    public ExportsController(IGraphExportService graphExportService, IGraphTemplateExportService graphTemplateExportService)
    {
        _graphExportService = graphExportService;
        _graphTemplateExportService = graphTemplateExportService;
    }

    [HttpGet("containers/{containerId:int}/graphs/{graphId:int}/export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportExcel(int containerId, int graphId, [FromQuery] bool includeStyles = true, [FromQuery] bool includeEmployees = true, CancellationToken cancellationToken = default)
    {
        var (bytes, fileName) = await _graphTemplateExportService
            .ExportGraphToXlsxAsync(containerId, graphId, includeStyles, includeEmployees, cancellationToken)
            .ConfigureAwait(false);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("containers/{containerId:int}/graphs/{graphId:int}/export/sql")]
    [Produces("text/plain")]
    public async Task<IActionResult> ExportGraphSql(int containerId, int graphId, [FromQuery] bool includeStyles = true, [FromQuery] bool includeEmployees = true, CancellationToken cancellationToken = default)
    {
        var bytes = await _graphExportService
            .ExportGraphSqlAsync(containerId, graphId, includeEmployees, includeStyles, cancellationToken)
            .ConfigureAwait(false);

        var fileName = $"GF3_Graph_{graphId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.sql";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    [HttpGet("containers/{containerId:int}/export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportContainerExcel(int containerId, [FromQuery] bool includeStyles = true, [FromQuery] bool includeEmployees = true, CancellationToken cancellationToken = default)
    {
        var (bytes, fileName) = await _graphTemplateExportService
            .ExportContainerToXlsxAsync(containerId, includeStyles, includeEmployees, cancellationToken)
            .ConfigureAwait(false);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("containers/{containerId:int}/export/sql")]
    [Produces("text/plain")]
    public async Task<IActionResult> ExportContainerSql(int containerId, [FromQuery] bool includeStyles = true, [FromQuery] bool includeEmployees = true, CancellationToken cancellationToken = default)
    {
        var bytes = await _graphExportService
            .ExportContainerSqlAsync(containerId, includeEmployees, includeStyles, cancellationToken)
            .ConfigureAwait(false);

        var fileName = $"GF3_Container_{containerId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.sql";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }
}
