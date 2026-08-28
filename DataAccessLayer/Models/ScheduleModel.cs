using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models;

/// <summary>
/// Root persistence aggregate for a generated or manually edited monthly schedule.
/// The child collections store the enrolled employees, concrete shift slots, and optional cell-level styling.
/// </summary>
[Table("schedule")]
[Index(nameof(ContainerId), Name = "ix_sched_container")]
[Index(nameof(ShopId), nameof(Year), nameof(Month), Name = "ix_sched_shop_month")]
[Index(nameof(ContainerId), nameof(ShopId), Name = "ix_sched_container_shop")]
public class ScheduleModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("container_id")]
    public int ContainerId { get; set; }

    /// <summary>
    /// Container that owns the schedule.
    /// </summary>
    public ContainerModel Container { get; set; } = null!;

    [Required]
    [Column("shop_id")]
    public int ShopId { get; set; }

    /// <summary>
    /// Shop or location the schedule was generated for.
    /// </summary>
    public ShopModel Shop { get; set; } = null!;

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("month")]
    public int Month { get; set; }

    [Required]
    [Column("publication_status")]
    public SchedulePublicationStatus PublicationStatus { get; set; } = SchedulePublicationStatus.Private;

    [Required]
    [Column("allow_swap")]
    public bool AllowSwap { get; set; } = true;

    [Required]
    [Column("accepted_swap_highlight_color")]
    [MaxLength(7)]
    public string AcceptedSwapHighlightColor { get; set; } = "#BBF7D0";

    [Required]
    [Column("people_per_shift")]
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in the canonical <c>HH:mm - HH:mm</c> format.
    /// </summary>
    [Required]
    [Column("shift1_time")]
    public string Shift1Time { get; set; } = null!;

    /// <summary>
    /// Second shift interval in the canonical <c>HH:mm - HH:mm</c> format.
    /// </summary>
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

    [Column("note")]
    public string? Note { get; set; }

    [Column("availability_group_id")]
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Optional availability group that constrained generation of this schedule.
    /// </summary>
    public AvailabilityGroupModel? AvailabilityGroup { get; set; }

    /// <summary>
    /// Employees that participate in the schedule.
    /// </summary>
    public ICollection<ScheduleEmployeeModel> Employees { get; set; } = new List<ScheduleEmployeeModel>();

    /// <summary>
    /// Concrete shift assignments for each day and slot position.
    /// </summary>
    public ICollection<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();

    /// <summary>
    /// Optional cell-level formatting used by the planner UI and exports.
    /// </summary>
    public ICollection<ScheduleCellStyleModel> CellStyles { get; set; } = new List<ScheduleCellStyleModel>();

    public ICollection<ScheduleVersionModel> Versions { get; set; } = new List<ScheduleVersionModel>();

    public ScheduleVersionStateModel? VersionState { get; set; }

    public ICollection<ShiftCorrectionRequestModel> ShiftCorrectionRequests { get; set; } = new List<ShiftCorrectionRequestModel>();
}
