using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models
{
    [Table("schedule_slot")]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(ShiftNo), nameof(SlotNo), IsUnique = true)]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(ShiftNo), Name = "ix_slot_sched_day_shift")]
    [Index(nameof(ScheduleId), nameof(DayOfMonth), nameof(ShiftNo), nameof(EmployeeId), IsUnique = true, Name = "ux_slot_unique_emp_per_shift")]
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
        [Column("shift_no")]
        public int ShiftNo { get; set; } // 1 or 2


        [Required]
        [Column("slot_no")]
        public int SlotNo { get; set; } // >= 1


        [Column("employee_id")]
        public int? EmployeeId { get; set; }


        public EmployeeModel? Employee { get; set; }


        [Required]
        [Column("status")]
        public SlotStatus Status { get; set; } = SlotStatus.UNFURNISHED;
    }
}
