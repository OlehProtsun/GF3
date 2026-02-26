using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/containers/{containerId:int}/graphs/{graphId:int}/export")]
public sealed class ExportsController : ControllerBase
{
    private readonly IGraphExportService _graphExportService;

    public ExportsController(IGraphExportService graphExportService)
    {
        _graphExportService = graphExportService;
    }

    [HttpGet("excel")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportExcel(
        int containerId,
        int graphId,
        [FromQuery] bool includeStyles = true,
        [FromQuery] bool includeEmployees = true,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _graphExportService
            .ExportGraphExcelCsvAsync(containerId, graphId, includeEmployees, includeStyles, cancellationToken)
            .ConfigureAwait(false);

        var fileName = $"GF3_Graph_{graphId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("sql")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportSql(
        int containerId,
        int graphId,
        [FromQuery] bool includeStyles = true,
        [FromQuery] bool includeEmployees = true,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _graphExportService
            .ExportGraphSqlAsync(containerId, graphId, includeEmployees, includeStyles, cancellationToken)
            .ConfigureAwait(false);

        var fileName = $"GF3_Graph_{graphId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.sql";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }
}
