using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Stores a single shift cell inside a schedule.
/// <see cref="SlotNo"/> distinguishes multiple positions inside the same time interval when more than one employee
/// is required for a shift.
/// </summary>
[Table("schedule_slot")]
[Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(FromTime), nameof(ToTime), nameof(SlotNo), IsUnique = true)]
public class ScheduleSlotModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// Parent schedule that owns the slot.
    /// </summary>
    public ScheduleModel Schedule { get; set; } = null!;

    [Required]
    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Required]
    [Column("slot_no")]
    public int SlotNo { get; set; }

    [Column("employee_id")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Assigned employee, or <see langword="null"/> when the slot is unassigned.
    /// </summary>
    public EmployeeModel? Employee { get; set; }

    [Required]
    [Column("status")]
    public SlotStatus Status { get; set; } = SlotStatus.UNFURNISHED;

    /// <summary>
    /// Start time in <c>HH:mm</c> format.
    /// </summary>
    [Required]
    [Column("from_time")]
    public string FromTime { get; set; } = null!;

    /// <summary>
    /// End time in <c>HH:mm</c> format.
    /// </summary>
    [Required]
    [Column("to_time")]
    public string ToTime { get; set; } = null!;
}
