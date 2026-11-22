using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var employeeId = GetRequiredEmployeeId();
        return await RenderPdfAsync(scheduleId, async () =>
        {
            var (workbook, _) = await exporter.ExportPublishedGraphToXlsxAsync(employeeId, scheduleId, cancellationToken);
            return workbook;
        }, cancellationToken);
    }

    [HttpGet("public/{scheduleId:int}/export/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPublicPdf(
        int scheduleId, [FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var employeeId = GetRequiredEmployeeId();
        var authorized = EmployeeSchedulesController.GetBrowsableSchedules(db, employeeId)
            .Where(schedule => schedule.Id == scheduleId);
        var containerId = await authorized.Select(schedule => (int?)schedule.ContainerId).SingleOrDefaultAsync(cancellationToken);
        if (containerId is not int authorizedContainerId) return NotFound();

        var result = await RenderPdfAsync(scheduleId, async () =>
        {
            var (workbook, _) = await exporter.ExportPublishedPreviewToXlsxAsync(authorizedContainerId, scheduleId, cancellationToken);
            return workbook;
        }, cancellationToken);
        if (result is FileContentResult && !await authorized.AnyAsync(
                schedule => schedule.ContainerId == authorizedContainerId, cancellationToken))
            return NotFound();

        return result;
    }

    private int GetRequiredEmployeeId()
    {
        if (!int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) || employeeId <= 0)
            throw new BadHttpRequestException("The current employee session is invalid.");
        return employeeId;
    }

    private async Task<IActionResult> RenderPdfAsync(int scheduleId, Func<Task<byte[]>> loadWorkbook, CancellationToken cancellationToken)
    {
        try
        {
            var workbook = await loadWorkbook().ConfigureAwait(false);
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
