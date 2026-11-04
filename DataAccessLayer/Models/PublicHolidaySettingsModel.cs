using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("public_holiday_settings")]
public sealed class PublicHolidaySettingsModel
{
    public const int SingletonId = 1;

    [Key]
    [Column("id")]
    public int Id { get; set; } = SingletonId;

    [Column("show_public_holidays")]
    public bool ShowPublicHolidays { get; set; } = true;
}
