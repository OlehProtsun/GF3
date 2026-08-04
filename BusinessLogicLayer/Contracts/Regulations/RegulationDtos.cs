namespace BusinessLogicLayer.Contracts.Regulations;

public sealed class RegulationDocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string PdfFileName { get; set; } = string.Empty;
    public string PdfSha256 { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string CreatedByManagerName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int AcceptanceCount { get; set; }
}

public sealed class RegulationAcceptanceDto
{
    public int Id { get; set; }
    public int RegulationDocumentId { get; set; }
    public string RegulationTitle { get; set; } = string.Empty;
    public string RegulationVersion { get; set; } = string.Empty;
    public string PdfSha256 { get; set; } = string.Empty;
    public string AccountRole { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset AcceptedAtUtc { get; set; }
}

public sealed class RegulationPdfDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}

public sealed class SaveRegulationDocumentRequest
{
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Message { get; set; }
    public string? PdfFileName { get; set; }
    public byte[]? PdfContent { get; set; }
}

public sealed class RegulationSubject
{
    public string Role { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
