using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("employment_contract_settings")]
public sealed class EmploymentContractSettingsModel
{
    public const int SingletonId = 1;
    [Key, Column("id")]
    public int Id { get; set; } = SingletonId;
    [Column("restrict_swaps_to_eight_hours")]
    public bool RestrictSwapsToEightHours { get; set; } = true;
    [Column("require_pickup_approval")]
    public bool RequirePickupApproval { get; set; } = true;
    [Column("require_below_minimum_give_approval")]
    public bool RequireBelowMinimumGiveApproval { get; set; } = true;
    [Column("automatic_weekday_availability")]
    public bool AutomaticWeekdayAvailability { get; set; } = true;
}
