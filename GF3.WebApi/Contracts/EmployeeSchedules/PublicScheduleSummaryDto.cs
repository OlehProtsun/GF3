namespace WebApi.Contracts.EmployeeSchedules;

public sealed record PublicScheduleSummaryDto(
    int Id,
    string Name,
    int Year,
    int Month,
    string PublicationStatus,
    DateTimeOffset? LastUpdatedAtUtc);
