using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Persistent provenance for one employee/day moved from one availability group to another.
/// The relation is member/day based so it survives the existing replace-all employee save flow.
/// </summary>
[Table("availability_group_day_transfer")]
[Index(nameof(SourceMemberId), nameof(DayOfMonth), IsUnique = true, Name = "ux_avail_transfer_source_day")]
[Index(nameof(TargetMemberId), nameof(DayOfMonth), IsUnique = true, Name = "ux_avail_transfer_target_day")]
public class AvailabilityGroupDayTransferModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("source_member_id")]
    public int SourceMemberId { get; set; }

    public AvailabilityGroupMemberModel SourceMember { get; set; } = null!;

    [Required]
    [Column("target_member_id")]
    public int TargetMemberId { get; set; }

    public AvailabilityGroupMemberModel TargetMember { get; set; } = null!;

    [Required]
    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Required]
    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}
