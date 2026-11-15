using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.PublicHolidays;

public sealed class UpdatePublicHolidayDisplaySettingsRequest
{
    [Required]
    public bool? ShowPublicHolidays { get; set; }
}
