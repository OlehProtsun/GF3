using System.Security.Claims;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/regulations")]
public sealed class RegulationsController(
    IRegulationService regulationService,
    IWorkflowLogService workflowLogService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<RegulationDocumentDto>>> GetPending(CancellationToken cancellationToken)
        => Ok(await regulationService.ListPendingAsync(GetSubject(), cancellationToken).ConfigureAwait(false));

    [HttpGet("history/me")]
    public async Task<ActionResult<IReadOnlyList<RegulationAcceptanceDto>>> GetMyHistory(CancellationToken cancellationToken)
    {
        var subject = GetSubject();
        return Ok(await regulationService.ListAcceptancesAsync(subject.Role, subject.AccountId, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("history/employees/{employeeId:int}")]
    [Authorize(Roles = AuthRoles.Manager)]
    public async Task<ActionResult<IReadOnlyList<RegulationAcceptanceDto>>> GetEmployeeHistory(
        int employeeId,
        CancellationToken cancellationToken)
        => Ok(await regulationService.ListAcceptancesAsync(AuthRoles.Employee, employeeId, cancellationToken).ConfigureAwait(false));

    [HttpGet("{documentId:int}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadPdf(int documentId, CancellationToken cancellationToken)
    {
        var pdf = await regulationService.GetPdfAsync(documentId, true, cancellationToken).ConfigureAwait(false);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HttpPost("{documentId:int}/accept")]
    public async Task<ActionResult<RegulationAcceptanceDto>> Accept(int documentId, CancellationToken cancellationToken)
    {
        var acceptance = await regulationService.AcceptAsync(documentId, GetSubject(), cancellationToken).ConfigureAwait(false);
        await workflowLogService
            .LogAsync(User, $"Accepted regulation {acceptance.RegulationTitle} ({acceptance.RegulationVersion}).", cancellationToken)
            .ConfigureAwait(false);
        return Ok(acceptance);
    }

    private RegulationSubject GetSubject()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var accountIdClaim = role == AuthRoles.Manager ? "manager_id" : "employee_id";
        if (!int.TryParse(User.FindFirstValue(accountIdClaim), out var accountId) || accountId <= 0)
        {
            throw new BadHttpRequestException("The current account session is invalid.");
        }

        return new RegulationSubject
        {
            Role = role,
            AccountId = accountId,
            Username = User.Identity?.Name ?? string.Empty,
            DisplayName = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? string.Empty,
        };
    }
}
