using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("priority_generation_settings")]
public sealed class PriorityGenerationSettingsModel
{
    public const int SingletonId = 1;

    [Key]
    [Column("id")]
    public int Id { get; set; } = SingletonId;

    [Column("shift_hours")]
    public int ShiftHours { get; set; } = 8;

    [Column("weekends_off")]
    public bool WeekendsOff { get; set; } = true;

    [Column("respect_availability")]
    public bool RespectAvailability { get; set; } = true;
}
