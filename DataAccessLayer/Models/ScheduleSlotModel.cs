using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models
{
    [Table("schedule_slot")]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(FromTime), nameof(ToTime), nameof(SlotNo), IsUnique = true)]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(FromTime), nameof(ToTime), nameof(EmployeeId), IsUnique = true, Name = "ux_slot_unique_emp_per_time")]
    public class ScheduleSlotModel
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
        [Column("slot_no")]
        public int SlotNo { get; set; }

        [Column("employee_id")]
        public int? EmployeeId { get; set; }
        public EmployeeModel? Employee { get; set; }

        [Required]
        [Column("status")]
        public SlotStatus Status { get; set; } = SlotStatus.UNFURNISHED;

        // 🔥 нові поля часу
        [Required]
        [Column("from_time")]
        public string FromTime { get; set; } = null!;   // "HH:mm"

        [Required]
        [Column("to_time")]
        public string ToTime { get; set; } = null!;     // "HH:mm"
    }
}
