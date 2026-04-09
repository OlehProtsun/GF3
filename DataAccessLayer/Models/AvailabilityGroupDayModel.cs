using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Per-day availability entry for an employee inside an availability group.
/// The actual meaning of the row is defined by <see cref="Kind"/> and optionally <see cref="IntervalStr"/>.
/// </summary>
[Table("availability_group_day")]
[Index(nameof(AvailabilityGroupMemberId), nameof(DayOfMonth), IsUnique = true, Name = "ux_avail_group_day_member_dom")]
public class AvailabilityGroupDayModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("availability_group_member_id")]
    public int AvailabilityGroupMemberId { get; set; }

    /// <summary>
    /// Member row that owns this day entry.
    /// </summary>
    public AvailabilityGroupMemberModel AvailabilityGroupMember { get; set; } = null!;

    [Required]
    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Required]
    [Column("kind")]
    public AvailabilityKind Kind { get; set; }

    /// <summary>
    /// Optional normalized interval string used only when <see cref="Kind"/> represents interval-based availability.
    /// </summary>
    [Column("interval_str")]
    public string? IntervalStr { get; set; }
}
