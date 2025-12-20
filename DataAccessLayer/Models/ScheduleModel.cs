using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataAccessLayer.Models.Enums;

namespace DataAccessLayer.Models
{
    [Table("schedule")]
    [Index(nameof(ContainerId), Name = "ix_sched_container")]
    [Index(nameof(ShopId), nameof(Year), nameof(Month), Name = "ix_sched_shop_month")]
    [Index(nameof(ContainerId), nameof(ShopId), Name = "ix_sched_container_shop")]
    public class ScheduleModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("container_id")]
        public int ContainerId { get; set; }

        public ContainerModel Container { get; set; } = null!;

        [Required]
        [Column("shop_id")]
        public int ShopId { get; set; }

        public ShopModel Shop { get; set; } = null!;

        [Required]
        [Column("name")]
        public string Name { get; set; } = null!;

        [Required]
        [Column("year")]
        public int Year { get; set; }

        [Required]
        [Column("month")]
        public int Month { get; set; }

        [Required]
        [Column("people_per_shift")]
        public int PeoplePerShift { get; set; }

        [Required]
        [Column("shift1_time")]
        public string Shift1Time { get; set; } = null!; // format "HH:mm - HH:mm"

        [Required]
        [Column("shift2_time")]
        public string Shift2Time { get; set; } = null!; // format "HH:mm - HH:mm"

        [Required]
        [Column("max_hours_per_emp_month")]
        public int MaxHoursPerEmpMonth { get; set; }

        [Required]
        [Column("max_consecutive_days")]
        public int MaxConsecutiveDays { get; set; }

        [Required]
        [Column("max_consecutive_full")]
        public int MaxConsecutiveFull { get; set; }

        [Required]
        [Column("max_full_per_month")]
        public int MaxFullPerMonth { get; set; }

        public ICollection<ScheduleEmployeeModel> Employees { get; set; } = new List<ScheduleEmployeeModel>();
        public ICollection<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();
    }
}
