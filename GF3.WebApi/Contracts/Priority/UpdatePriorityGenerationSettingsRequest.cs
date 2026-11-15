using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Priority;

public sealed class UpdatePriorityGenerationSettingsRequest
{
    [Required]
    [Range(1, 12)]
    public int? ShiftHours { get; set; }

    [Required]
    public bool? WeekendsOff { get; set; }

    public bool? RespectAvailability { get; set; }
}
