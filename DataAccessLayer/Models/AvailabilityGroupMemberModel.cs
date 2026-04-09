using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Join entity that enrolls an employee into an availability group and stores its visual order in the editor.
/// </summary>
[Table("availability_group_member")]
[Index(nameof(AvailabilityGroupId), nameof(EmployeeId), IsUnique = true, Name = "ux_avail_group_member_group_emp")]
public class AvailabilityGroupMemberModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("availability_group_id")]
    public int AvailabilityGroupId { get; set; }

    /// <summary>
    /// Parent availability group that owns this member row.
    /// </summary>
    public AvailabilityGroupModel AvailabilityGroup { get; set; } = null!;

    [Required]
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Employee represented by this group member.
    /// </summary>
    public EmployeeModel Employee { get; set; } = null!;

    /// <summary>
    /// Stable UI ordering used when rendering and exporting the group.
    /// </summary>
    [Required]
    [Column("display_order")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Day-level availability entries for the member.
    /// </summary>
    public ICollection<AvailabilityGroupDayModel> Days { get; set; } = new List<AvailabilityGroupDayModel>();
}
