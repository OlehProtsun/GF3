using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Reusable template for generating schedules with a known set of rules and employees.
/// Presets let operators capture configuration without creating the full schedule until it is needed.
/// </summary>
[Table("schedule_preset")]
public class SchedulePresetModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("container_id")]
    public int ContainerId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [Column("schedule_name")]
    public string ScheduleName { get; set; } = null!;

    [Required]
    [Column("shop_id")]
    public int ShopId { get; set; }

    [Required]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("month")]
    public int Month { get; set; }

    [Required]
    [Column("people_per_shift")]
    public int PeoplePerShift { get; set; }

    [Required]
    [Column("shift1_time")]
    public string Shift1Time { get; set; } = null!;

    [Required]
    [Column("shift2_time")]
    public string Shift2Time { get; set; } = null!;

    [Required]
    [Column("max_hours_per_emp_month")]
    public int MaxHoursPerEmpMonth { get; set; }

    [Required]
    [Column("max_consecutive_days")]
    public int MaxConsecutiveDays { get; set; }

    [Required]
    [Column("max_consecutive_full")]
    public int MaxConsecutiveFull { get; set; }

    [Required]
    [Column("max_full_per_month")]
    public int MaxFullPerMonth { get; set; }

    [Column("availability_group_id")]
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Employees included in the preset along with preset-specific min-hours targets.
    /// </summary>
    public ICollection<SchedulePresetEmployeeModel> Employees { get; set; } = new List<SchedulePresetEmployeeModel>();
}
