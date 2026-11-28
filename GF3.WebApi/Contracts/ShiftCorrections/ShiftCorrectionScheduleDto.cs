using WebApi.Contracts.EmployeeSchedules;

namespace WebApi.Contracts.ShiftCorrections;

public sealed class ShiftCorrectionScheduleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public string PublicationStatus { get; init; } = "public";
    public DateTimeOffset? LastUpdatedAtUtc { get; init; }
    public IReadOnlyList<EmployeeScheduleSlotDto> Slots { get; init; } = [];
}
