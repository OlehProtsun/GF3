using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.Communications;
using WebApi.Realtime;
using WebApi.Services;
using BusinessCreateCommunicationMessageRequest = BusinessLogicLayer.Contracts.Communications.CreateCommunicationMessageRequest;
using BusinessCommunicationMessageDto = BusinessLogicLayer.Contracts.Communications.CommunicationMessageDto;
using BusinessUpdateCommunicationMessageRequest = BusinessLogicLayer.Contracts.Communications.UpdateCommunicationMessageRequest;
using CommunicationContentBlock = BusinessLogicLayer.Contracts.Communications.CommunicationContentBlock;
using CommunicationGifAttachment = BusinessLogicLayer.Contracts.Communications.CommunicationGifAttachment;

namespace WebApi.Controllers;

[ApiController]
[Route("api/communications")]
public sealed class CommunicationsController(
    ICommunicationService communicationService,
    IWorkflowLogService workflowLogService,
    CommunicationGifStorage gifStorage,
    ILogger<CommunicationsController> logger,
    IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    private const int MaxBlocks = 20;
    private static readonly JsonSerializerOptions BlockJson = new(JsonSerializerDefaults.Web);

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
    public Task<ActionResult<CommunicationMessageDto>> Create(
        [FromBody] CreateCommunicationMessageRequest request,
        CancellationToken cancellationToken)
        => CreateCoreAsync(new BusinessCreateCommunicationMessageRequest
        {
            Title = request.Title, Body = request.Body,
            VisibleFromUtc = request.VisibleFromUtc, DeadlineAtUtc = request.DeadlineAtUtc,
        }, cancellationToken);

    [HttpPost("with-gif")]
    [Authorize(Roles = AuthRoles.Manager)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CommunicationGifStorage.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CommunicationGifStorage.MaxGifBytes)]
    public async Task<ActionResult<CommunicationMessageDto>> CreateWithGif(
        [FromForm] CommunicationGifForm form, CancellationToken cancellationToken)
    {
        if (form.Blocks is not null)
        {
            var (blocks, uploaded) = await ReadBlocksAsync(form.Blocks, null, cancellationToken).ConfigureAwait(false);
            return await CreateCoreAsync(new BusinessCreateCommunicationMessageRequest
            {
                Title = form.Title, Body = GetBlockBody(blocks), VisibleFromUtc = form.VisibleFromUtc,
                DeadlineAtUtc = form.DeadlineAtUtc, Blocks = blocks,
            }, cancellationToken, uploaded).ConfigureAwait(false);
        }
        var (gif, photo) = await SaveAttachmentsAsync(cancellationToken).ConfigureAwait(false);
        return await CreateCoreAsync(new BusinessCreateCommunicationMessageRequest
        {
            Title = form.Title, Body = form.Body, VisibleFromUtc = form.VisibleFromUtc,
            DeadlineAtUtc = form.DeadlineAtUtc, Gif = gif, Photo = photo,
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ActionResult<CommunicationMessageDto>> CreateCoreAsync(
        BusinessCreateCommunicationMessageRequest request, CancellationToken cancellationToken,
        IReadOnlyList<CommunicationGifAttachment>? uploaded = null)
    {
        var created = await SaveMessageAsync(request.Gif, request.Photo, () =>
                communicationService.CreateAsync(request, GetCurrentManagerId(), GetCurrentDisplayName(), cancellationToken), uploaded)
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
    public Task<ActionResult<CommunicationMessageDto>> Update(
        int communicationId,
        [FromBody] UpdateCommunicationMessageRequest request,
        CancellationToken cancellationToken)
        => UpdateCoreAsync(communicationId, new BusinessUpdateCommunicationMessageRequest
        {
            Title = request.Title, Body = request.Body, VisibleFromUtc = request.VisibleFromUtc,
            DeadlineAtUtc = request.DeadlineAtUtc, RemoveGif = request.RemoveGif, RemovePhoto = request.RemovePhoto,
        }, cancellationToken);

    [HttpPut("{communicationId:int}/with-gif")]
    [Authorize(Roles = AuthRoles.Manager)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CommunicationGifStorage.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CommunicationGifStorage.MaxGifBytes)]
    public async Task<ActionResult<CommunicationMessageDto>> UpdateWithGif(
        int communicationId, [FromForm] CommunicationGifForm form, CancellationToken cancellationToken)
    {
        if (form.Blocks is not null)
        {
            var current = (await communicationService.ListForManagerAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(message => message.Id == communicationId)
                ?? throw new KeyNotFoundException($"Communication message with id {communicationId} was not found.");
            var (blocks, uploaded) = await ReadBlocksAsync(form.Blocks, current, cancellationToken).ConfigureAwait(false);
            return await UpdateCoreAsync(communicationId, new BusinessUpdateCommunicationMessageRequest
            {
                Title = form.Title, Body = GetBlockBody(blocks), VisibleFromUtc = form.VisibleFromUtc,
                DeadlineAtUtc = form.DeadlineAtUtc, Blocks = blocks, RemovePhoto = true, RemoveGif = true,
            }, cancellationToken, uploaded).ConfigureAwait(false);
        }
        var (gif, photo) = await SaveAttachmentsAsync(cancellationToken).ConfigureAwait(false);
        return await UpdateCoreAsync(communicationId, new BusinessUpdateCommunicationMessageRequest
        {
            Title = form.Title, Body = form.Body, VisibleFromUtc = form.VisibleFromUtc,
            DeadlineAtUtc = form.DeadlineAtUtc, Gif = gif, Photo = photo,
            RemovePhoto = form.RemovePhoto, RemoveGif = form.RemoveGif,
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ActionResult<CommunicationMessageDto>> UpdateCoreAsync(
        int communicationId, BusinessUpdateCommunicationMessageRequest request, CancellationToken cancellationToken,
        IReadOnlyList<CommunicationGifAttachment>? uploaded = null)
    {
        var updated = await SaveMessageAsync(request.Gif, request.Photo, () =>
                communicationService.UpdateAsync(communicationId, request, cancellationToken), uploaded)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Updated employee message \"{updated.Title}\"; it is visible from {FormatDateTime(updated.VisibleFromUtc)} until {FormatDateTime(updated.DeadlineAtUtc)}.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyCommunicationsChangedAsync(updated.Id, "manager-communication-updated").ConfigureAwait(false);

        return Ok(ToApiDto(updated));
    }

    [HttpGet("{communicationId:int}/gif/{key:guid}")]
    [Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
    public Task<IActionResult> GetGif(int communicationId, Guid key, CancellationToken cancellationToken)
        => GetAttachmentAsync(communicationId, key, photo: false, cancellationToken);

    [HttpGet("{communicationId:int}/photo/{key:guid}")]
    [Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
    public Task<IActionResult> GetPhoto(int communicationId, Guid key, CancellationToken cancellationToken)
        => GetAttachmentAsync(communicationId, key, photo: true, cancellationToken);

    private async Task<IActionResult> GetAttachmentAsync(int communicationId, Guid key, bool photo, CancellationToken cancellationToken)
    {
        int? employeeId = User.IsInRole(AuthRoles.Manager) ? null : GetRequiredEmployeeId();
        var attachment = await communicationService.GetGifAsync(communicationId, employeeId, cancellationToken, photo, key).ConfigureAwait(false);
        if (attachment is null || attachment.Key != key || gifStorage.Open(key) is not { } stream)
            return NotFound();

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, attachment.ContentType);
    }

    private async Task<(CommunicationGifAttachment? Gif, CommunicationGifAttachment? Photo)> SaveAttachmentsAsync(CancellationToken ct)
    {
        var files = (await Request.ReadFormAsync(ct).ConfigureAwait(false)).Files;
        var gifFile = files.GetFile("File");
        var photoFile = files.GetFile("Photo");
        if ((gifFile is null && photoFile is null) ||
            files.Count != (gifFile is null ? 0 : 1) + (photoFile is null ? 0 : 1))
        {
            throw new BadHttpRequestException("Could not upload the attachment. Please try again.");
        }

        var photo = photoFile is null ? null : await gifStorage.SavePhotoAsync(photoFile, ct).ConfigureAwait(false);
        try
        {
            var gif = gifFile is null ? null : await gifStorage.SaveAsync(gifFile, ct).ConfigureAwait(false);
            return (gif, photo);
        }
        catch
        {
            RemoveUnattached(photo);
            throw;
        }
    }

    private sealed class BlockInput
    {
        public BlockInput() { }

        public Guid Id { get; init; }
        public string? Type { get; init; }
        public string? Text { get; init; }
        public Guid? MediaKey { get; init; }
    }

    private static string GetBlockBody(IEnumerable<CommunicationContentBlock> blocks)
        => string.Join("\n\n", blocks.Where(block => block.Type == "text").Select(block => block.Text));

    private async Task<(IReadOnlyList<CommunicationContentBlock> Blocks, IReadOnlyList<CommunicationGifAttachment> Uploaded)>
        ReadBlocksAsync(string json, BusinessCommunicationMessageDto? current, CancellationToken ct)
    {
        BlockInput?[]? input;
        try { input = JsonSerializer.Deserialize<BlockInput?[]>(json, BlockJson); }
        catch (JsonException) { throw InvalidBlocks(); }

        if (input is null || input.Length is 0 or > MaxBlocks ||
            input.Any(block => block is null || block.Id == Guid.Empty) ||
            input.Select(block => block!.Id).Distinct().Count() != input.Length)
            throw InvalidBlocks();

        var files = (await Request.ReadFormAsync(ct).ConfigureAwait(false)).Files;
        if (files.Sum(file => file.Length) > CommunicationGifStorage.MaxBlockUploadBytes)
            throw new BadHttpRequestException("New attachments cannot exceed 8 MB per save.");

        var remainingFiles = new HashSet<IFormFile>(files);
        var retainedMedia = new HashSet<Guid>();
        var blocks = new List<CommunicationContentBlock>(input.Length);
        var uploaded = new List<CommunicationGifAttachment>();
        try
        {
            foreach (var item in input)
            {
                var block = item!;
                if (block.Type == "text" && !string.IsNullOrWhiteSpace(block.Text) && block.MediaKey is null)
                {
                    blocks.Add(new(block.Id, "text", block.Text.Trim(), null));
                    continue;
                }

                if (block.Type is not ("photo" or "gif") || block.Text is not null)
                    throw InvalidBlocks();

                var file = files.GetFile($"Block-{block.Id:D}");
                CommunicationGifAttachment? media;
                if (file is not null && block.MediaKey is null && remainingFiles.Remove(file))
                {
                    media = block.Type == "photo"
                        ? await gifStorage.SavePhotoAsync(file, ct).ConfigureAwait(false)
                        : await gifStorage.SaveAsync(file, ct).ConfigureAwait(false);
                    uploaded.Add(media);
                }
                else if (file is null && block.MediaKey is Guid key)
                {
                    media = current?.Blocks is { } previous
                        ? previous.FirstOrDefault(candidate => candidate.Id == block.Id && candidate.Type == block.Type)?.Media
                        : block.Type == "photo" ? current?.Photo : current?.Gif;
                    if (media?.Key != key || !retainedMedia.Add(key))
                        throw InvalidBlocks();
                }
                else
                {
                    throw InvalidBlocks();
                }
                blocks.Add(new(block.Id, block.Type, null, media));
            }

            if (remainingFiles.Count != 0)
                throw InvalidBlocks();
            return (blocks, uploaded);
        }
        catch
        {
            foreach (var file in uploaded) RemoveUnattached(file);
            throw;
        }
    }

    private static BadHttpRequestException InvalidBlocks()
        => new("Invalid communication content blocks.");

    private void RemoveUnattached(CommunicationGifAttachment? attachment)
    {
        if (attachment is null) return;
        try { gifStorage.Remove(attachment.Key); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { logger.LogWarning(error, "Could not clean up an unattached communication image."); }
    }

    private async Task<BusinessCommunicationMessageDto> SaveMessageAsync(
        CommunicationGifAttachment? gif, CommunicationGifAttachment? photo, Func<Task<BusinessCommunicationMessageDto>> save,
        IReadOnlyList<CommunicationGifAttachment>? uploaded)
    {
        try { return await save().ConfigureAwait(false); }
        catch (Exception error) when (error is ValidationException or KeyNotFoundException or DbUpdateException)
        {
            RemoveUnattached(gif);
            RemoveUnattached(photo);
            if (uploaded is not null)
                foreach (var file in uploaded) RemoveUnattached(file);
            throw;
        }
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
        Blocks = message.Blocks,
        Gif = message.Gif,
        Photo = message.Photo,
        VisibleFromUtc = message.VisibleFromUtc,
        DeadlineAtUtc = message.DeadlineAtUtc,
        CreatedAtUtc = message.CreatedAtUtc,
        CreatedByManagerId = message.CreatedByManagerId,
        CreatedByManagerName = message.CreatedByManagerName,
        IsActive = message.IsActive,
    };
}
