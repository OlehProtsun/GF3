using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.ShiftCorrections;

public sealed class CreateShiftCorrectionRequest
{
    [Range(1, int.MaxValue)]
    public int ScheduleId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ScheduleSlotId { get; init; }

    [Range(1, 31)]
    public int? DayOfMonth { get; init; }

    public string? RequestedFromTime { get; init; }

    public string? RequestedToTime { get; init; }

    public string? RequestType { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}
