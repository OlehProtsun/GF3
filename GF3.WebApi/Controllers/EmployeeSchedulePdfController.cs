using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-schedules")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeSchedulePdfController(
    IGraphTemplateExportService exporter,
    IScheduleWorksheetPdfRenderer renderer,
    ILogger<EmployeeSchedulePdfController> logger) : ControllerBase
{
    [HttpGet("{scheduleId:int}/export/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPdf(int scheduleId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) || employeeId <= 0)
            throw new BadHttpRequestException("The current employee session is invalid.");
        try
        {
            var (workbook, _) = await exporter.ExportPublishedGraphToXlsxAsync(employeeId, scheduleId, cancellationToken).ConfigureAwait(false);
            var pdf = renderer.Render(workbook, cancellationToken);
            return File(pdf, "application/pdf", $"GF3_Graph_{scheduleId}_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception exception) when (exception is not KeyNotFoundException
            and not OperationCanceledException and not BadHttpRequestException
            and not BusinessLogicLayer.Common.ValidationException
            and not System.ComponentModel.DataAnnotations.ValidationException
            and not UnauthorizedAccessException)
        {
            logger.LogError(exception, "PDF generation failed for graph {GraphId}", scheduleId);
            var problem = ApiProblemDetailsFactory.CreateProblem(HttpContext, StatusCodes.Status500InternalServerError,
                "PDF generation failed", "Could not export this schedule to PDF.", "pdf_generation_error");
            return new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
        }
    }
}
