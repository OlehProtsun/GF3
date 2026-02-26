using BusinessLogicLayer.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Slots;

public sealed class CreateGraphSlotRequest
{
    [Required]
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    [Required]
    [Range(1, 100)]
    public int SlotNo { get; set; }

    [Required]
    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string FromTime { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string ToTime { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }

    [Required]
    public SlotStatus Status { get; set; }
}
