using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("schedule_cell_style")]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(EmployeeId), IsUnique = true)]
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

        public EmployeeModel Employee { get; set; } = null!;

        [Column("background_color_argb")]
        public int? BackgroundColorArgb { get; set; }

        [Column("text_color_argb")]
        public int? TextColorArgb { get; set; }
    }
}
