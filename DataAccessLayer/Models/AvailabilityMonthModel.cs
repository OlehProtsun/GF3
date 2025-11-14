using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataAccessLayer.Models
{
    [Table("availability_month")]
    [Index(nameof(EmployeeId), nameof(Year), nameof(Month), IsUnique = true, Name = "ux_availability_month_emp_year_month")]
    [Index(nameof(EmployeeId), nameof(Year), nameof(Month), Name = "ix_avail_month_emp")]
    public class AvailabilityMonthModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        public EmployeeModel Employee { get; set; } = null!;

        [Required]
        [Column("year")]
        public int Year { get; set; }

        [Required]
        [Column("month")]
        public int Month { get; set; }

        [NotMapped]
        public string EmployeeFullName =>
            Employee is null
                ? string.Empty
                : $"{Employee.FirstName} {Employee.LastName}";

        public ICollection<AvailabilityDayModel> Days { get; set; } = new List<AvailabilityDayModel>();
    }
}
