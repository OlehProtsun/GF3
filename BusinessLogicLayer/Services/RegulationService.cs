using System.Security.Cryptography;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

public sealed class RegulationService(IRegulationRepository repository) : IRegulationService
{
    private const int MaxPdfBytes = 10 * 1024 * 1024;

    public async Task<IReadOnlyList<RegulationDocumentDto>> ListDocumentsAsync(CancellationToken ct = default)
        => (await repository.ListDocumentsAsync(ct).ConfigureAwait(false)).Select(ToDocumentDto).ToList();

    public async Task<IReadOnlyList<RegulationDocumentDto>> ListPendingAsync(RegulationSubject subject, CancellationToken ct = default)
    {
        ValidateSubject(subject);
        return (await repository.ListPendingAsync(subject.Role, subject.AccountId, ct).ConfigureAwait(false))
            .Select(ToDocumentDto)
            .ToList();
    }

    public async Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(CancellationToken ct = default)
        => (await repository.ListAcceptancesAsync(ct).ConfigureAwait(false)).Select(ToAcceptanceDto).ToList();

    public async Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(
        string role,
        int accountId,
        CancellationToken ct = default)
    {
        ValidateRoleAndAccount(role, accountId);
        return (await repository.ListAcceptancesAsync(role, accountId, ct).ConfigureAwait(false))
            .Select(ToAcceptanceDto)
            .ToList();
    }

    public async Task<RegulationDocumentDto> CreateAsync(
        SaveRegulationDocumentRequest request,
        int? managerId,
        string managerName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var title = NormalizeRequired(request.Title, "title", 160);
        var version = NormalizeRequired(request.Version, "version", 80);
        var message = NormalizeRequired(request.Message, "message", 4000);
        var (fileName, content, hash) = ValidatePdf(request.PdfFileName, request.PdfContent);
        if (await repository.VersionExistsAsync(version, ct: ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField("version", "A regulation with this version already exists.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var document = await repository.AddAsync(new RegulationDocumentModel
        {
            Title = title,
            Version = version,
            Message = message,
            PdfFileName = fileName,
            PdfContent = content,
            PdfSha256 = hash,
            CreatedByManagerId = managerId is > 0 ? managerId : null,
            CreatedByManagerName = NormalizeCreatorName(managerName),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        }, ct).ConfigureAwait(false);

        return ToDocumentDto(document);
    }

    public async Task<RegulationDocumentDto> UpdateAsync(
        int documentId,
        SaveRegulationDocumentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = await GetRequiredAsync(documentId, ct).ConfigureAwait(false);
        if (document.IsPublished)
        {
            throw new ValidationException("Published regulations are immutable. Create a new version instead.");
        }

        var version = NormalizeRequired(request.Version, "version", 80);
        if (await repository.VersionExistsAsync(version, documentId, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField("version", "A regulation with this version already exists.");
        }

        document.Title = NormalizeRequired(request.Title, "title", 160);
        document.Version = version;
        document.Message = NormalizeRequired(request.Message, "message", 4000);
        if (request.PdfContent is { Length: > 0 })
        {
            var pdf = ValidatePdf(request.PdfFileName, request.PdfContent);
            document.PdfFileName = pdf.FileName;
            document.PdfContent = pdf.Content;
            document.PdfSha256 = pdf.Hash;
        }
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(document, ct).ConfigureAwait(false);
        return ToDocumentDto(document);
    }

    public async Task<RegulationDocumentDto> PublishAsync(int documentId, CancellationToken ct = default)
    {
        var document = await GetRequiredAsync(documentId, ct).ConfigureAwait(false);
        if (!document.IsPublished)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            document.IsPublished = true;
            document.PublishedAtUtc = nowUtc;
            document.UpdatedAtUtc = nowUtc;
            await repository.UpdateAsync(document, ct).ConfigureAwait(false);
        }
        return ToDocumentDto(document);
    }

    public async Task DeleteAsync(int documentId, CancellationToken ct = default)
    {
        var document = await GetRequiredAsync(documentId, ct).ConfigureAwait(false);
        if (document.IsPublished || await repository.HasAcceptancesAsync(documentId, ct).ConfigureAwait(false))
        {
            throw new ValidationException("Published or accepted regulations cannot be deleted because their audit history must remain available.");
        }
        await repository.DeleteAsync(documentId, ct).ConfigureAwait(false);
    }

    public async Task<RegulationAcceptanceDto> AcceptAsync(
        int documentId,
        RegulationSubject subject,
        CancellationToken ct = default)
    {
        ValidateSubject(subject);
        var document = await GetRequiredAsync(documentId, ct).ConfigureAwait(false);
        if (!document.IsPublished)
        {
            throw new ValidationException("Only published regulations can be accepted.");
        }

        var acceptance = await repository.AddAcceptanceAsync(new RegulationAcceptanceModel
        {
            RegulationDocumentId = documentId,
            AccountRole = subject.Role,
            AccountId = subject.AccountId,
            UsernameSnapshot = NormalizeRequired(subject.Username, "username", 100),
            DisplayNameSnapshot = NormalizeRequired(subject.DisplayName, "displayName", 160),
            AcceptedAtUtc = DateTimeOffset.UtcNow,
        }, ct).ConfigureAwait(false);
        return ToAcceptanceDto(acceptance);
    }

    public async Task<RegulationPdfDto> GetPdfAsync(int documentId, bool requirePublished, CancellationToken ct = default)
    {
        var document = await GetRequiredAsync(documentId, ct).ConfigureAwait(false);
        if (requirePublished && !document.IsPublished)
        {
            throw new KeyNotFoundException($"Published regulation with id {documentId} was not found.");
        }
        return new RegulationPdfDto { FileName = document.PdfFileName, Content = document.PdfContent };
    }

    private async Task<RegulationDocumentModel> GetRequiredAsync(int documentId, CancellationToken ct)
        => documentId > 0
            ? await repository.GetByIdAsync(documentId, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Regulation with id {documentId} was not found.")
            : throw ValidationException.ForField("documentId", "Choose a regulation.");

    private static (string FileName, byte[] Content, string Hash) ValidatePdf(string? fileNameValue, byte[]? contentValue)
    {
        var fileName = Path.GetFileName(NormalizeRequired(fileNameValue, "pdf", 255));
        var content = contentValue ?? [];
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || content.Length < 5 ||
            content[0] != '%' || content[1] != 'P' || content[2] != 'D' || content[3] != 'F' || content[4] != '-')
        {
            throw ValidationException.ForField("pdf", "Attach a valid PDF file.");
        }
        if (content.Length > MaxPdfBytes)
        {
            throw ValidationException.ForField("pdf", "The PDF file must not exceed 10 MB.");
        }
        return (fileName, content, Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
    }

    private static string NormalizeRequired(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw ValidationException.ForField(field, $"{field} is required.");
        }
        if (normalized.Length > maxLength)
        {
            throw ValidationException.ForField(field, $"{field} is too long.");
        }
        return normalized;
    }

    private static string NormalizeCreatorName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? "Manager" : normalized[..Math.Min(normalized.Length, 160)];
    }

    private static void ValidateSubject(RegulationSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ValidateRoleAndAccount(subject.Role, subject.AccountId);
    }

    private static void ValidateRoleAndAccount(string role, int accountId)
    {
        if (role is not "manager" and not "employee")
        {
            throw ValidationException.ForField("role", "The account role is invalid.");
        }
        if (accountId <= 0)
        {
            throw ValidationException.ForField("accountId", "The account is invalid.");
        }
    }

    private static RegulationDocumentDto ToDocumentDto(RegulationDocumentModel document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Version = document.Version,
        Message = document.Message,
        PdfFileName = document.PdfFileName,
        PdfSha256 = document.PdfSha256,
        IsPublished = document.IsPublished,
        PublishedAtUtc = document.PublishedAtUtc,
        CreatedByManagerName = document.CreatedByManagerName,
        CreatedAtUtc = document.CreatedAtUtc,
        UpdatedAtUtc = document.UpdatedAtUtc,
        AcceptanceCount = document.Acceptances.Count,
    };

    private static RegulationAcceptanceDto ToAcceptanceDto(RegulationAcceptanceModel acceptance) => new()
    {
        Id = acceptance.Id,
        RegulationDocumentId = acceptance.RegulationDocumentId,
        RegulationTitle = acceptance.RegulationDocument.Title,
        RegulationVersion = acceptance.RegulationDocument.Version,
        PdfSha256 = acceptance.RegulationDocument.PdfSha256,
        AccountRole = acceptance.AccountRole,
        AccountId = acceptance.AccountId,
        Username = acceptance.UsernameSnapshot,
        DisplayName = acceptance.DisplayNameSnapshot,
        AcceptedAtUtc = acceptance.AcceptedAtUtc,
    };
}
