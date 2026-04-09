using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Join entity that enrolls an employee into a schedule and stores schedule-specific metadata such as
/// the employee's display order and optional minimum hours target.
/// </summary>
[Table("schedule_employee")]
[Index(nameof(ScheduleId), nameof(EmployeeId), IsUnique = true)]
public class ScheduleEmployeeModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// Parent schedule that owns the employee enrollment.
    /// </summary>
    public ScheduleModel Schedule { get; set; } = null!;

    [Required]
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Employee participating in the schedule.
    /// </summary>
    public EmployeeModel Employee { get; set; } = null!;

    [Column("min_hours_month")]
    public int? MinHoursMonth { get; set; }

    /// <summary>
    /// Stable visual order used when rendering the schedule grid.
    /// </summary>
    [Required]
    [Column("display_order")]
    public int DisplayOrder { get; set; }
}
