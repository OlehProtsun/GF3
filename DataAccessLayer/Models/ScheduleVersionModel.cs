using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models;

[Table("schedule_version")]
[Index(nameof(ScheduleId), nameof(VersionNumber), IsUnique = true, Name = "ux_schedule_version_number")]
[Index(nameof(ScheduleId), nameof(BranchName), nameof(VersionNumber), Name = "ix_schedule_version_branch")]
public sealed class ScheduleVersionModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    public ScheduleModel Schedule { get; set; } = null!;

    [Column("parent_version_id")]
    public int? ParentVersionId { get; set; }

    public ScheduleVersionModel? ParentVersion { get; set; }

    public ICollection<ScheduleVersionModel> ChildVersions { get; set; } = new List<ScheduleVersionModel>();

    [Column("version_number")]
    public int VersionNumber { get; set; }

    [MaxLength(80)]
    [Column("branch_name")]
    public string BranchName { get; set; } = "main";

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("created_by_manager_id")]
    public int? CreatedByManagerId { get; set; }

    [MaxLength(160)]
    [Column("created_by_manager_name")]
    public string CreatedByManagerName { get; set; } = null!;

    [Column("employee_count")]
    public int EmployeeCount { get; set; }

    [Column("slot_count")]
    public int SlotCount { get; set; }

    [Column("cell_style_count")]
    public int CellStyleCount { get; set; }

    [Column("snapshot_json")]
    public string SnapshotJson { get; set; } = null!;
}
