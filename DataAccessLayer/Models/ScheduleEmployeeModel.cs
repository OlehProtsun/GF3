using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models
{
    [Table("schedule_employee")]
    [Index(nameof(ScheduleId), nameof(EmployeeId), IsUnique = true)]
    public class ScheduleEmployeeModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }


        [Required]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }


        public ScheduleModel Schedule { get; set; } = null!;


        [Required]
        [Column("employee_id")]
        public int EmployeeId { get; set; }


        public EmployeeModel Employee { get; set; } = null!;


        [Column("min_hours_month")]
        public int? MinHoursMonth { get; set; }
    }
}
