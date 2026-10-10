using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("schedule_version_state")]
public sealed class ScheduleVersionStateModel
{
    [Key]
    [Column("schedule_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ScheduleId { get; set; }

    public ScheduleModel Schedule { get; set; } = null!;

    [Column("current_version_id")]
    public int? CurrentVersionId { get; set; }

    public ScheduleVersionModel? CurrentVersion { get; set; }
}
