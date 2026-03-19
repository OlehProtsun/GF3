using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("schedule_preset_employee")]
    public class SchedulePresetEmployeeModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("schedule_preset_id")]
        public int SchedulePresetId { get; set; }

        public SchedulePresetModel SchedulePreset { get; set; } = null!;

        [Required]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Required]
        [Column("min_hours_month")]
        public int MinHoursMonth { get; set; }
    }
}
