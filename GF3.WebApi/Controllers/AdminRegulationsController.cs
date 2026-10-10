using System.Security.Claims;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Regulations;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin/regulations")]
[Authorize(Policy = AuthPolicies.SystemManager)]
public sealed class AdminRegulationsController(
    IRegulationService regulationService,
    IWorkflowLogService workflowLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegulationDocumentDto>>> List(CancellationToken cancellationToken)
        => Ok(await regulationService.ListDocumentsAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet("acceptances")]
    public async Task<ActionResult<IReadOnlyList<RegulationAcceptanceDto>>> ListAcceptances(CancellationToken cancellationToken)
        => Ok(await regulationService.ListAcceptancesAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet("{documentId:int}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadPdf(int documentId, CancellationToken cancellationToken)
    {
        var pdf = await regulationService.GetPdfAsync(documentId, false, cancellationToken).ConfigureAwait(false);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HttpPost]
    [RequestSizeLimit(10_600_000)]
    public async Task<ActionResult<RegulationDocumentDto>> Create(
        [FromForm] SaveRegulationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await regulationService.CreateAsync(
            await ToBusinessRequestAsync(request, true, cancellationToken).ConfigureAwait(false),
            GetManagerId(),
            GetDisplayName(),
            cancellationToken).ConfigureAwait(false);
        await workflowLogService.LogAsync(User, $"Created regulation draft {created.Title} ({created.Version}).", cancellationToken).ConfigureAwait(false);
        return Ok(created);
    }

    [HttpPut("{documentId:int}")]
    [RequestSizeLimit(10_600_000)]
    public async Task<ActionResult<RegulationDocumentDto>> Update(
        int documentId,
        [FromForm] SaveRegulationRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await regulationService.UpdateAsync(
            documentId,
            await ToBusinessRequestAsync(request, false, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        await workflowLogService.LogAsync(User, $"Updated regulation draft {updated.Title} ({updated.Version}).", cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpPost("{documentId:int}/publish")]
    public async Task<ActionResult<RegulationDocumentDto>> Publish(int documentId, CancellationToken cancellationToken)
    {
        var published = await regulationService.PublishAsync(documentId, cancellationToken).ConfigureAwait(false);
        await workflowLogService.LogAsync(User, $"Published regulation {published.Title} ({published.Version}).", cancellationToken).ConfigureAwait(false);
        return Ok(published);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int documentId, CancellationToken cancellationToken)
    {
        await regulationService.DeleteAsync(documentId, cancellationToken).ConfigureAwait(false);
        await workflowLogService.LogAsync(User, $"Deleted regulation draft #{documentId}.", cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private int? GetManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) ? managerId : null;

    private string GetDisplayName()
        => User.FindFirstValue("display_name") ?? User.Identity?.Name ?? "Manager";

    private static async Task<SaveRegulationDocumentRequest> ToBusinessRequestAsync(
        SaveRegulationRequest request,
        bool requirePdf,
        CancellationToken cancellationToken)
    {
        if (requirePdf && request.Pdf is null)
        {
            throw BusinessLogicLayer.Common.ValidationException.ForField("pdf", "Attach a PDF file.");
        }

        byte[]? content = null;
        if (request.Pdf is not null)
        {
            await using var stream = new MemoryStream();
            await request.Pdf.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            content = stream.ToArray();
        }

        return new SaveRegulationDocumentRequest
        {
            Title = request.Title,
            Version = request.Version,
            Message = request.Message,
            PdfFileName = request.Pdf?.FileName,
            PdfContent = content,
        };
    }
}
