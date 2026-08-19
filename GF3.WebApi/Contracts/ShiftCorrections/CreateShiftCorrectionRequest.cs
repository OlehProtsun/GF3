using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.ShiftCorrections;

public sealed class CreateShiftCorrectionRequest
{
    [Range(1, int.MaxValue)]
    public int ScheduleId { get; init; }

    [Range(1, int.MaxValue)]
    public int ScheduleSlotId { get; init; }

    [Required]
    public string RequestedFromTime { get; init; } = string.Empty;

    [Required]
    public string RequestedToTime { get; init; } = string.Empty;
}
