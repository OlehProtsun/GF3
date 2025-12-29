using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models
{
    [Table("employee")]
    public class EmployeeModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }


        [Required]
        [Column("first_name")]
        public string FirstName { get; set; } = null!;


        [Required]
        [Column("last_name")]
        public string LastName { get; set; } = null!;


        [Column("phone")]
        public string? Phone { get; set; }


        [Column("email")]
        public string? Email { get; set; }


        public ICollection<ScheduleEmployeeModel> ScheduleEmployees { get; set; } = new List<ScheduleEmployeeModel>();
        public ICollection<ScheduleSlotModel> ScheduleSlots { get; set; } = new List<ScheduleSlotModel>();
    }
}
