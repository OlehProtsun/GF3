using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Communications;
using WebApi.Realtime;
using WebApi.Services;
using BusinessCreateCommunicationMessageRequest = BusinessLogicLayer.Contracts.Communications.CreateCommunicationMessageRequest;
using BusinessCommunicationMessageDto = BusinessLogicLayer.Contracts.Communications.CommunicationMessageDto;
using BusinessUpdateCommunicationMessageRequest = BusinessLogicLayer.Contracts.Communications.UpdateCommunicationMessageRequest;

namespace WebApi.Controllers;

[ApiController]
[Route("api/communications")]
public sealed class CommunicationsController(
    ICommunicationService communicationService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AuthRoles.Manager)]
    [ProducesResponseType(typeof(IReadOnlyList<CommunicationMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommunicationMessageDto>>> GetAll(CancellationToken cancellationToken)
    {
        var messages = await communicationService.ListForManagerAsync(cancellationToken).ConfigureAwait(false);
        return Ok(messages.Select(ToApiDto).ToList());
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Manager)]
    [ProducesResponseType(typeof(CommunicationMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommunicationMessageDto>> Create(
        [FromBody] CreateCommunicationMessageRequest request,
        CancellationToken cancellationToken)
    {
        var created = await communicationService
            .CreateAsync(
                new BusinessCreateCommunicationMessageRequest
                {
                    Title = request.Title,
                    Body = request.Body,
                    VisibleFromUtc = request.VisibleFromUtc,
                    DeadlineAtUtc = request.DeadlineAtUtc,
                },
                GetCurrentManagerId(),
                GetCurrentDisplayName(),
                cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Created employee message \"{created.Title}\", visible from {FormatDateTime(created.VisibleFromUtc)} until {FormatDateTime(created.DeadlineAtUtc)}.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyCommunicationsChangedAsync(created.Id, "manager-communication-created").ConfigureAwait(false);

        return Ok(ToApiDto(created));
    }

    [HttpPut("{communicationId:int}")]
    [Authorize(Roles = AuthRoles.Manager)]
    [ProducesResponseType(typeof(CommunicationMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunicationMessageDto>> Update(
        int communicationId,
        [FromBody] UpdateCommunicationMessageRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await communicationService
            .UpdateAsync(
                communicationId,
                new BusinessUpdateCommunicationMessageRequest
                {
                    Title = request.Title,
                    Body = request.Body,
                    VisibleFromUtc = request.VisibleFromUtc,
                    DeadlineAtUtc = request.DeadlineAtUtc,
                },
                cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Updated employee message \"{updated.Title}\"; it is visible from {FormatDateTime(updated.VisibleFromUtc)} until {FormatDateTime(updated.DeadlineAtUtc)}.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyCommunicationsChangedAsync(updated.Id, "manager-communication-updated").ConfigureAwait(false);

        return Ok(ToApiDto(updated));
    }

    [HttpDelete("{communicationId:int}")]
    [Authorize(Roles = AuthRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int communicationId, CancellationToken cancellationToken)
    {
        var existing = (await communicationService.ListForManagerAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(message => message.Id == communicationId)
            ?? throw new KeyNotFoundException($"Communication message with id {communicationId} was not found.");

        await communicationService.DeleteAsync(communicationId, cancellationToken).ConfigureAwait(false);
        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Deleted employee message \"{existing.Title}\".", cancellationToken)
            .ConfigureAwait(false);
        await NotifyCommunicationsChangedAsync(communicationId, "manager-communication-deleted").ConfigureAwait(false);

        return NoContent();
    }

    [HttpGet("pending")]
    [Authorize(Roles = AuthRoles.Employee)]
    [ProducesResponseType(typeof(IReadOnlyList<CommunicationMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommunicationMessageDto>>> GetPending(CancellationToken cancellationToken)
    {
        var messages = await communicationService
            .GetPendingForEmployeeAsync(GetRequiredEmployeeId(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(messages.Select(ToApiDto).ToList());
    }

    [HttpPost("{communicationId:int}/dismiss")]
    [Authorize(Roles = AuthRoles.Employee)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Dismiss(int communicationId, CancellationToken cancellationToken)
    {
        await communicationService
            .DismissForEmployeeAsync(GetRequiredEmployeeId(), communicationId, cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }

    private int? GetCurrentManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var parsed) ? parsed : null;

    private string GetCurrentDisplayName()
        => User.FindFirstValue("display_name") ?? User.Identity?.Name ?? "Manager";

    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
    }

    private Task NotifyCommunicationsChangedAsync(int communicationId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.Communication,
            communicationId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            reason) ?? Task.CompletedTask;

    private static string FormatDateTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);

    private static CommunicationMessageDto ToApiDto(BusinessCommunicationMessageDto message) => new()
    {
        Id = message.Id,
        Title = message.Title,
        Body = message.Body,
        VisibleFromUtc = message.VisibleFromUtc,
        DeadlineAtUtc = message.DeadlineAtUtc,
        CreatedAtUtc = message.CreatedAtUtc,
        CreatedByManagerId = message.CreatedByManagerId,
        CreatedByManagerName = message.CreatedByManagerName,
        IsActive = message.IsActive,
    };
}
