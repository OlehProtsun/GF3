using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Communications;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public sealed class CommunicationService : ICommunicationService
{
    private const int MaxTitleLength = 160;
    private const int MaxBodyLength = 4000;
    private const string FallbackManagerName = "Manager";

    private readonly ICommunicationRepository _communicationRepository;

    public CommunicationService(ICommunicationRepository communicationRepository)
    {
        _communicationRepository = communicationRepository;
    }

    public async Task<IReadOnlyList<CommunicationMessageDto>> ListForManagerAsync(CancellationToken ct = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var messages = await _communicationRepository.GetAllForManagerAsync(ct).ConfigureAwait(false);
        return messages.Select(message => ToDto(message, nowUtc)).ToList();
    }

    public async Task<CommunicationMessageDto> CreateAsync(
        CreateCommunicationMessageRequest request,
        int? managerId,
        string? managerDisplayName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nowUtc = DateTimeOffset.UtcNow;
        var title = NormalizeRequiredText(request.Title, "title", "Title is required.", MaxTitleLength, "Title is too long.");
        var body = NormalizeRequiredText(request.Body, "body", "Message is required.", MaxBodyLength, "Message is too long.");
        var visibleFromUtc = request.VisibleFromUtc?.ToUniversalTime()
            ?? throw ValidationException.ForField("visibleFromUtc", "Visible from is required.");
        var deadlineAtUtc = request.DeadlineAtUtc?.ToUniversalTime()
            ?? throw ValidationException.ForField("deadlineAtUtc", "Visible to is required.");

        if (deadlineAtUtc <= nowUtc)
        {
            throw ValidationException.ForField("deadlineAtUtc", "Visible to must be in the future.");
        }

        if (deadlineAtUtc <= visibleFromUtc)
        {
            throw ValidationException.ForField("deadlineAtUtc", "Visible to must be later than visible from.");
        }

        var created = await _communicationRepository
            .AddAsync(
                new CommunicationMessageModel
                {
                    Title = title,
                    Body = body,
                    VisibleFromUtc = visibleFromUtc,
                    DeadlineAtUtc = deadlineAtUtc,
                    CreatedAtUtc = nowUtc,
                    CreatedByManagerId = managerId is > 0 ? managerId : null,
                    CreatedByManagerName = NormalizeCreatorName(managerDisplayName),
                },
                ct)
            .ConfigureAwait(false);

        return ToDto(created, nowUtc);
    }

    public async Task<CommunicationMessageDto> UpdateAsync(
        int communicationId,
        UpdateCommunicationMessageRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await GetRequiredMessageAsync(communicationId, ct).ConfigureAwait(false);
        var nowUtc = DateTimeOffset.UtcNow;
        ApplyEditableFields(existing, request.Title, request.Body, request.VisibleFromUtc, request.DeadlineAtUtc, nowUtc);

        await _communicationRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
        return ToDto(existing, nowUtc);
    }

    public async Task DeleteAsync(int communicationId, CancellationToken ct = default)
    {
        _ = await GetRequiredMessageAsync(communicationId, ct).ConfigureAwait(false);
        await _communicationRepository.DeleteAsync(communicationId, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CommunicationMessageDto>> GetPendingForEmployeeAsync(
        int employeeId,
        CancellationToken ct = default)
    {
        if (employeeId <= 0)
        {
            throw ValidationException.ForField("employeeId", "The employee account is invalid.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var messages = await _communicationRepository
            .GetPendingForEmployeeAsync(employeeId, nowUtc, ct)
            .ConfigureAwait(false);

        return messages.Select(message => ToDto(message, nowUtc)).ToList();
    }

    public async Task DismissForEmployeeAsync(int employeeId, int communicationId, CancellationToken ct = default)
    {
        if (employeeId <= 0)
        {
            throw ValidationException.ForField("employeeId", "The employee account is invalid.");
        }

        if (communicationId <= 0)
        {
            throw ValidationException.ForField("communicationId", "Choose a communication message to dismiss.");
        }

        if (!await _communicationRepository.ExistsAsync(communicationId, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField("communicationId", "This communication message could not be found.");
        }

        if (await _communicationRepository.DismissalExistsAsync(communicationId, employeeId, ct).ConfigureAwait(false))
        {
            return;
        }

        await _communicationRepository
            .AddDismissalAsync(
                new EmployeeCommunicationDismissalModel
                {
                    CommunicationMessageId = communicationId,
                    EmployeeId = employeeId,
                    DismissedAtUtc = DateTimeOffset.UtcNow,
                },
                ct)
            .ConfigureAwait(false);
    }

    private static CommunicationMessageDto ToDto(CommunicationMessageModel message, DateTimeOffset nowUtc) => new()
    {
        Id = message.Id,
        Title = message.Title,
        Body = message.Body,
        VisibleFromUtc = message.VisibleFromUtc ?? message.CreatedAtUtc,
        DeadlineAtUtc = message.DeadlineAtUtc,
        CreatedAtUtc = message.CreatedAtUtc,
        CreatedByManagerId = message.CreatedByManagerId,
        CreatedByManagerName = message.CreatedByManagerName,
        IsActive =
            (message.VisibleFromUtc ?? message.CreatedAtUtc) <= nowUtc &&
            message.DeadlineAtUtc > nowUtc,
    };

    private async Task<CommunicationMessageModel> GetRequiredMessageAsync(int communicationId, CancellationToken ct)
    {
        if (communicationId <= 0)
        {
            throw ValidationException.ForField("communicationId", "Choose a communication message.");
        }

        return await _communicationRepository.GetByIdAsync(communicationId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Communication message with id {communicationId} was not found.");
    }

    private static void ApplyEditableFields(
        CommunicationMessageModel message,
        string? titleValue,
        string? bodyValue,
        DateTimeOffset? visibleFromValue,
        DateTimeOffset? deadlineValue,
        DateTimeOffset nowUtc)
    {
        var title = NormalizeRequiredText(titleValue, "title", "Title is required.", MaxTitleLength, "Title is too long.");
        var body = NormalizeRequiredText(bodyValue, "body", "Message is required.", MaxBodyLength, "Message is too long.");
        var visibleFromUtc = visibleFromValue?.ToUniversalTime()
            ?? throw ValidationException.ForField("visibleFromUtc", "Visible from is required.");
        var deadlineAtUtc = deadlineValue?.ToUniversalTime()
            ?? throw ValidationException.ForField("deadlineAtUtc", "Visible to is required.");

        if (deadlineAtUtc <= nowUtc)
        {
            throw ValidationException.ForField("deadlineAtUtc", "Visible to must be in the future.");
        }

        if (deadlineAtUtc <= visibleFromUtc)
        {
            throw ValidationException.ForField("deadlineAtUtc", "Visible to must be later than visible from.");
        }

        message.Title = title;
        message.Body = body;
        message.VisibleFromUtc = visibleFromUtc;
        message.DeadlineAtUtc = deadlineAtUtc;
    }

    private static string NormalizeRequiredText(
        string? value,
        string fieldName,
        string requiredMessage,
        int maxLength,
        string maxLengthMessage)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw ValidationException.ForField(fieldName, requiredMessage);
        }

        if (normalized.Length > maxLength)
        {
            throw ValidationException.ForField(fieldName, maxLengthMessage);
        }

        return normalized;
    }

    private static string NormalizeCreatorName(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return FallbackManagerName;
        }

        return normalized.Length <= MaxTitleLength ? normalized : normalized[..MaxTitleLength];
    }
}
