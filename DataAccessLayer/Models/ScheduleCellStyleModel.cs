using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("schedule_cell_style")]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(EmployeeId), IsUnique = true, Name = "ux_sched_cell_style")]
    public class ScheduleCellStyleModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        public ScheduleModel Schedule { get; set; } = null!;

        [Required]
        [Column("day_of_month")]
        public int DayOfMonth { get; set; }

        [Required]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("background_hex")]
        public string? BackgroundHex { get; set; }

        [Column("foreground_hex")]
        public string? ForegroundHex { get; set; }
    }
}
